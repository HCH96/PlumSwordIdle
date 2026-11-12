"""유니티 테스트를 화면 없이 실행하고, 결과를 사람과 에이전트가 읽을 수 있게 요약해 출력한다.

루프의 test_command 가 이 스크립트를 부른다. 에이전트가 작업 중에 직접 돌려 봐도 된다.

  python tools/loop/run_unity_tests.py [--project <프로젝트 폴더>] [--platform EditMode] [--unity <Unity.exe>]

- 종료 코드: 0 통과, 1 실패(컴파일 오류, 테스트 실패, 테스트 0개, 시간 초과),
  2 실행할 수 없음(유니티를 못 찾음, 에디터가 같은 폴더를 열고 있음, 로그인 문제 등 코드로 고칠 수 없는 것)
- 결과 파일과 로그는 <프로젝트>/.loop/test/ 에 남는다 (깃에서 제외된 폴더).
- 테스트 종류를 정하지 않으면 EditMode 를 돌리고, Assets/Tests/PlayMode 폴더가 있으면 PlayMode 도 돌린다.
- 같은 프로젝트 폴더를 에디터로 열어 둔 상태에서는 실행할 수 없다.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import time
import xml.etree.ElementTree as ET
from pathlib import Path

MAX_FAILURES = 20  # 출력에 싣는 실패 테스트 수
MAX_COMPILE_ERRORS = 30
MAX_MESSAGE = 600  # 실패 메시지 한 건의 글자 수
COMPILE_ERROR_RE = re.compile(r"\berror [A-Z]{2,4}\d+\b")
FATAL_HINTS = (
    "another Unity instance is running",
    "No valid Unity Editor license",
    "Scripts have compiler errors",
    "Aborting batchmode due to failure",
    "No tests were executed",
)


def find_unity(project: Path, explicit: str | None) -> Path | None:
    """--unity 인자 → UNITY_EXE 환경 변수 → 프로젝트 버전에 맞는 유니티 허브 설치 순으로 찾는다."""
    for candidate in (explicit, os.environ.get("UNITY_EXE")):
        if candidate:
            path = Path(candidate)
            return path if path.exists() else None

    version_file = project / "ProjectSettings" / "ProjectVersion.txt"
    m = re.search(r"m_EditorVersion:\s*(\S+)", version_file.read_text(encoding="utf-8")) if version_file.exists() else None
    if not m:
        return None
    version = m.group(1)

    roots: list[Path] = []
    if os.name == "nt":
        secondary = Path(os.environ.get("APPDATA", "")) / "UnityHub" / "secondaryInstallPath.json"
        try:
            extra = json.loads(secondary.read_text(encoding="utf-8"))
            if isinstance(extra, str) and extra:
                roots.append(Path(extra))
        except (OSError, ValueError):
            pass
        for key in ("ProgramFiles", "ProgramW6432"):
            if os.environ.get(key):
                roots.append(Path(os.environ[key]) / "Unity" / "Hub" / "Editor")
        tail = Path("Editor") / "Unity.exe"
    elif sys.platform == "darwin":
        roots.append(Path("/Applications/Unity/Hub/Editor"))
        tail = Path("Unity.app") / "Contents" / "MacOS" / "Unity"
    else:
        roots.append(Path.home() / "Unity" / "Hub" / "Editor")
        tail = Path("Editor") / "Unity"
    for root in roots:
        exe = root / version / tail
        if exe.exists():
            return exe
    return None


def _clip(text: str, limit: int) -> str:
    text = (text or "").strip()
    return text if len(text) <= limit else text[:limit] + " …(잘림)"


def _run(cmd: list[str], timeout_sec: float) -> int | None:
    """종료 코드를 돌려준다. 시간 초과면 하위 프로세스까지 끝내고 None."""
    proc = subprocess.Popen(cmd, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        return proc.wait(timeout=timeout_sec)
    except subprocess.TimeoutExpired:
        if os.name == "nt":
            subprocess.run(["taskkill", "/PID", str(proc.pid), "/T", "/F"], capture_output=True)
        proc.kill()
        return None
    except BaseException:
        proc.kill()
        raise


def read_log_problems(log_path: Path) -> tuple[list[str], list[str]]:
    """(컴파일 오류 줄, 그 밖의 원인으로 보이는 줄)"""
    if not log_path.exists():
        return [], []
    errors: list[str] = []
    hints: list[str] = []
    for raw in log_path.read_text(encoding="utf-8", errors="replace").splitlines():
        line = raw.strip()
        if COMPILE_ERROR_RE.search(line):
            if line not in errors:
                errors.append(line)
        elif any(h.lower() in line.lower() for h in FATAL_HINTS) and line not in hints:
            hints.append(line)
    return errors, hints


def read_results(results_path: Path) -> tuple[dict, list[str]]:
    """(합계, 실패한 테스트 설명 목록)"""
    root = ET.parse(results_path).getroot()
    run = root if root.tag == "test-run" else root.find(".//test-run")
    counts = {k: int(run.get(k, "0")) for k in ("total", "passed", "failed", "inconclusive", "skipped")}
    counts["result"] = run.get("result", "")

    failures: list[str] = []
    for case in root.iter("test-case"):
        if case.get("result") != "Failed":
            continue
        message = case.findtext("failure/message") or ""
        stack = (case.findtext("failure/stack-trace") or "").strip().splitlines()
        where = f"\n    위치: {stack[0].strip()}" if stack else ""
        failures.append(f"- {case.get('fullname')}\n    {_clip(message, MAX_MESSAGE)}{where}")
    if counts["failed"] and not failures:  # 준비 단계(OneTimeSetUp 등)에서 실패한 경우
        for suite in root.iter("test-suite"):
            message = suite.findtext("failure/message")
            if suite.get("result") == "Failed" and message and "One or more child tests had errors" not in message:
                failures.append(f"- {suite.get('fullname')}\n    {_clip(message, MAX_MESSAGE)}")
    return counts, failures


def run_platform(unity: Path, project: Path, platform: str, out_dir: Path, timeout_min: float) -> tuple[bool | None, str]:
    """(통과 여부, 요약). 통과 여부가 None 이면 실행 환경 문제라 판정하지 못한 것."""
    results = out_dir / f"results_{platform}.xml"
    log_path = out_dir / f"unity_{platform}.log"
    for stale in (results, log_path):
        stale.unlink(missing_ok=True)
    cmd = [
        str(unity), "-batchmode", "-projectPath", str(project), "-runTests", "-testPlatform", platform,
        "-testResults", str(results), "-logFile", str(log_path),
    ]  # fmt: skip
    started = time.time()
    code = _run(cmd, timeout_min * 60)
    took = f"{time.time() - started:.0f}초"
    head = f"[유니티 테스트] {platform}"

    if code is None:
        return False, f"{head}: 실패 — 시간 초과 ({timeout_min:g}분). 로그: {log_path}"

    if not results.exists():
        errors, hints = read_log_problems(log_path)
        if errors:
            shown = "\n".join(errors[:MAX_COMPILE_ERRORS])
            more = f"\n…외 {len(errors) - MAX_COMPILE_ERRORS}건" if len(errors) > MAX_COMPILE_ERRORS else ""
            return False, f"{head}: 실패 — 컴파일 오류 {len(errors)}건 ({took})\n{shown}{more}"
        detail = "\n".join(hints[:10]) or "원인을 로그에서 찾지 못함"
        text = f"{head}: 실행할 수 없음 — 컴파일 오류 없이 테스트 결과 파일이 만들어지지 않음 (유니티 종료 코드 {code}, {took})\n"
        return None, text + f"{detail}\n로그: {log_path}"

    try:
        counts, failures = read_results(results)
    except (ET.ParseError, AttributeError, ValueError) as e:
        return False, f"{head}: 실패 — 결과 파일을 읽을 수 없음 ({type(e).__name__}: {e}). 파일: {results}"

    summary = f"{counts['total']}개 중 {counts['passed']}개 통과, {counts['failed']}개 실패"
    if counts["skipped"] or counts["inconclusive"]:
        summary += f", 건너뜀 {counts['skipped']}개, 판정 없음 {counts['inconclusive']}개"
    if counts["total"] == 0:
        return False, f"{head}: 실패 — 테스트가 0개 ({took}). 테스트가 없으면 통과로 보지 않는다"
    if counts["failed"] or code != 0 or not counts["result"].startswith("Passed"):
        shown = "\n".join(failures[:MAX_FAILURES])
        more = f"\n…외 {len(failures) - MAX_FAILURES}건" if len(failures) > MAX_FAILURES else ""
        return False, f"{head}: 실패 — {summary} ({took})\n{shown}{more}"
    return True, f"{head}: 통과 — {summary} ({took})"


def main() -> int:
    parser = argparse.ArgumentParser(description="유니티 테스트를 화면 없이 실행하고 결과를 요약한다")
    parser.add_argument("--project", default=".", help="유니티 프로젝트 폴더 (기본: 현재 폴더)")
    parser.add_argument("--platform", action="append", choices=["EditMode", "PlayMode"], help="테스트 종류 (여러 번 줄 수 있음)")
    parser.add_argument("--unity", help="Unity 실행 파일 경로 (기본: 프로젝트 버전에 맞는 유니티 허브 설치)")
    parser.add_argument("--timeout-min", type=float, default=25, help="테스트 종류 하나의 시간 제한(분)")
    args = parser.parse_args()
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass

    project = Path(args.project).resolve()
    if not (project / "Assets").is_dir() or not (project / "ProjectSettings").is_dir():
        print(f"[유니티 테스트] 실행할 수 없음 — 유니티 프로젝트 폴더가 아님: {project}")
        return 2
    unity = find_unity(project, args.unity)
    if unity is None:
        print("[유니티 테스트] 실행할 수 없음 — 유니티 실행 파일을 찾지 못함. --unity 인자나 UNITY_EXE 환경 변수로 경로를 알려 주세요")
        return 2

    platforms = args.platform or (["EditMode", "PlayMode"] if (project / "Assets" / "Tests" / "PlayMode").is_dir() else ["EditMode"])
    out_dir = project / ".loop" / "test"
    out_dir.mkdir(parents=True, exist_ok=True)

    outcomes = []
    for platform in platforms:
        ok, text = run_platform(unity, project, platform, out_dir, args.timeout_min)
        print(text, flush=True)
        outcomes.append(ok)
    if False in outcomes:  # 고칠 수 있는 실패가 하나라도 있으면 그것을 먼저 알린다
        print("[유니티 테스트] 결과: 실패")
        return 1
    if None in outcomes:
        print("[유니티 테스트] 결과: 실행할 수 없음 (에디터가 같은 폴더를 열고 있는지, 유니티 로그인 상태를 확인)")
        return 2
    print("[유니티 테스트] 결과: 통과")
    return 0


if __name__ == "__main__":
    sys.exit(main())
