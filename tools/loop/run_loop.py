"""매화검 키우기 — AI 자동 개발 루프 (1차 버전)

tasks/todo 의 작업 문서를 이름 순서대로 하나씩 꺼내 클로드 코드 CLI(헤드리스)로 처리한다.
todo 가 비면 종료한다. 사용법과 규칙은 docs/loop.md 참고.

실행: python tools/loop/run_loop.py
종료 코드: 0 정상, 1 오류로 멈춤, 2 사용량 한도로 멈춤, 3 시작 전 점검 실패
"""

from __future__ import annotations

import datetime as _dt
import json
import os
import re
import shutil
import subprocess
import sys
import traceback
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CONFIG_PATH = Path(__file__).resolve().parent / "loop_config.json"
TASKS = ROOT / "tasks"
TODO, DOING, BLOCKED, DONE = (TASKS / n for n in ("todo", "doing", "blocked", "done"))
HISTORY_REL = "docs/history"
LOOP_DIR = ROOT / ".loop"
STATE_PATH = LOOP_DIR / "state.json"
RESULT_PATH = LOOP_DIR / "result.json"
LOG_DIR = LOOP_DIR / "logs"

WEBHOOK_KEYS = {
    "DONE": "DISCORD_WEBHOOK_DONE",
    "BLOCKED": "DISCORD_WEBHOOK_BLOCKED",
    "STOPPED": "DISCORD_WEBHOOK_STOPPED",
    "LOG": "DISCORD_WEBHOOK_LOG",
}
WEBHOOK_COLORS = {"DONE": 0x2ECC71, "BLOCKED": 0xF1C40F, "STOPPED": 0xE74C3C, "LOG": 0x95A5A6}

USAGE_LIMIT_RE = re.compile(
    r"usage limit|limit reached|hit your limit|out of extra usage|rate[_ ]limit|429", re.IGNORECASE
)


class LoopError(Exception):
    """예상 못 한 오류. 상태를 건드리지 않고 멈춘다."""


class UsageLimit(Exception):
    """클로드 사용량 한도 초과."""


class PreflightError(Exception):
    """시작 전 점검 실패."""


# ───────────────────────── 공통 도구 ─────────────────────────

_log_file: Path | None = None


def log(msg: str) -> None:
    line = f"[{_dt.datetime.now():%H:%M:%S}] {msg}"
    print(line, flush=True)
    if _log_file:
        with _log_file.open("a", encoding="utf-8") as f:
            f.write(line + "\n")


def load_config() -> dict:
    with CONFIG_PATH.open(encoding="utf-8") as f:
        return json.load(f)


def load_env(path: Path) -> dict[str, str]:
    """KEY=VALUE 형식만 읽는다. 값은 절대 출력하지 않는다."""
    env: dict[str, str] = {}
    if not path.exists():
        return env
    for raw in path.read_text(encoding="utf-8-sig").splitlines():
        line = raw.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, _, value = line.partition("=")
        key = key.strip().removeprefix("export ").strip()
        value = value.strip()
        if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
            value = value[1:-1]
        env[key] = value
    return env


def truncate(text: str, limit: int) -> str:
    text = text or ""
    return text if len(text) <= limit else text[: limit - 20] + "\n…(잘림)"


def tail(text: str, limit: int) -> str:
    text = text or ""
    return text if len(text) <= limit else "…(앞부분 생략)\n" + text[-limit:]


class Notifier:
    def __init__(self, env: dict[str, str]):
        self.urls = {ch: env.get(key, "").strip() for ch, key in WEBHOOK_KEYS.items()}

    def send(self, channel: str, title: str, body: str) -> None:
        url = self.urls.get(channel, "")
        if not url:
            log(f"알림[{channel}] 웹훅 비어 있음, 건너뜀: {title}")
            return
        payload = {
            "embeds": [
                {
                    "title": truncate(title, 250),
                    "description": truncate(body, 4000),
                    "color": WEBHOOK_COLORS[channel],
                    "timestamp": _dt.datetime.now(_dt.timezone.utc).isoformat(),
                }
            ]
        }
        req = urllib.request.Request(
            url,
            data=json.dumps(payload).encode("utf-8"),
            headers={"Content-Type": "application/json", "User-Agent": "PlumSwordIdleLoop/1.0"},
            method="POST",
        )
        try:
            with urllib.request.urlopen(req, timeout=15) as resp:
                log(f"알림[{channel}] 전송 완료 (HTTP {resp.status}): {title}")
        except urllib.error.HTTPError as e:
            log(f"알림[{channel}] 전송 실패 (HTTP {e.code}): {title}")
        except Exception as e:  # 알림 실패로 루프를 멈추지 않는다
            log(f"알림[{channel}] 전송 실패 ({type(e).__name__}): {title}")


# ───────────────────────── git ─────────────────────────


def git(*args: str, check: bool = True, input_text: str | None = None) -> subprocess.CompletedProcess:
    proc = subprocess.run(
        ["git", *args],
        cwd=ROOT,
        input=input_text,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if check and proc.returncode != 0:
        raise LoopError(f"git {' '.join(args)} 실패 (코드 {proc.returncode}):\n{proc.stderr.strip() or proc.stdout.strip()}")
    return proc


def branch_exists(name: str) -> bool:
    return git("show-ref", "--verify", "--quiet", f"refs/heads/{name}", check=False).returncode == 0


def is_ancestor(a: str, b: str) -> bool:
    return git("merge-base", "--is-ancestor", a, b, check=False).returncode == 0


def file_in_branch(branch: str, rel_path: str) -> bool:
    return git("cat-file", "-e", f"{branch}:{rel_path}", check=False).returncode == 0


def is_dirty() -> bool:
    return bool(git("status", "--porcelain").stdout.strip())


def current_branch() -> str:
    return git("rev-parse", "--abbrev-ref", "HEAD").stdout.strip()


def checkout(branch: str) -> None:
    git("checkout", branch)


def commit_all(message: str) -> bool:
    """변경이 있으면 전부 커밋하고 True. 변경이 없으면 False."""
    git("add", "-A")
    if not git("diff", "--cached", "--name-only").stdout.strip():
        return False
    git("commit", "-F", "-", input_text=message)
    return True


def local_branches(prefix: str) -> list[str]:
    out = git("for-each-ref", "--format=%(refname:short)", f"refs/heads/{prefix}").stdout
    return [b.strip() for b in out.splitlines() if b.strip()]


# ───────────────────────── 상태 파일 ─────────────────────────


def load_state() -> dict:
    if STATE_PATH.exists():
        return json.loads(STATE_PATH.read_text(encoding="utf-8"))
    return {}


def save_state(state: dict) -> None:
    LOOP_DIR.mkdir(exist_ok=True)
    tmp = STATE_PATH.with_suffix(".tmp")
    tmp.write_text(json.dumps(state, ensure_ascii=False, indent=2), encoding="utf-8")
    tmp.replace(STATE_PATH)


# ───────────────────────── 작업 문서 ─────────────────────────


def task_docs(folder: Path) -> list[Path]:
    return sorted(p for p in folder.glob("*.md") if p.is_file())


def task_branch_name(cfg: dict, doc_name: str) -> str:
    stem = Path(doc_name).stem
    safe = re.sub(r"[^0-9A-Za-z가-힣_.-]+", "-", stem).strip("-.") or "task"
    return cfg["task_branch_prefix"] + safe


def move_doc(doc: Path, folder: Path) -> Path:
    folder.mkdir(parents=True, exist_ok=True)
    target = folder / doc.name
    if target.exists():
        raise LoopError(f"{target.relative_to(ROOT)} 가 이미 있어 옮길 수 없음")
    shutil.move(str(doc), str(target))
    return target


def append_section(doc: Path, title: str, body: str) -> None:
    stamp = f"{_dt.datetime.now():%Y-%m-%d %H:%M}"
    with doc.open("a", encoding="utf-8") as f:
        f.write(f"\n\n## {title} ({stamp})\n\n{body.strip()}\n")


# ───────────────────────── 클로드 실행 ─────────────────────────


def first_prompt(doc_name: str, branch: str) -> str:
    return f"""너는 자동 개발 루프 안에서 작업 문서 하나를 처리하는 에이전트다. 사람은 자리에 없어서 질문해도 답할 수 없다.

작업 문서: tasks/doing/{doc_name}
현재 브랜치: {branch} (루프 스크립트가 만들었다)

순서:
1. CLAUDE.md 규칙을 따른다. docs/index.md 를 먼저 읽고, 작업 문서와 그 "참고 문서"를 읽는다.
2. 계획을 세운 뒤 구현한다. 완료 조건에 맞는 테스트가 필요하면 함께 작성한다.
3. 바뀐 사양·중요한 결정·새 연결 지점을 docs/ 공용 문서에 반영하고, 필요하면 docs/index.md 를 갱신한다.
4. 작업 문서(tasks/doing/{doc_name}) 맨 끝에 "## 보고서" 절을 추가한다: 한 일, 바뀐 파일, 가정, 테스트 결과.
5. 마지막으로 .loop/result.json 을 UTF-8 JSON 으로 쓴다.
   - 완료: {{"status": "done", "summary": "바뀐 내용 요약 (몇 줄)"}}
   - 스스로 판단할 수 없어 진행 불가: {{"status": "blocked", "reason": "막힌 이유", "question": "사람에게 할 질문", "recommendation": "추천안"}}
   애매한 점은 CLAUDE.md 의 "애매한 점 처리" 규칙대로 먼저 스스로 정하고, 정말 불가능할 때만 blocked 로 한다.

하지 말 것:
- git commit, push, checkout, switch, merge, rebase, reset, branch, stash, tag 등 깃 상태를 바꾸는 명령 (스크립트가 한다)
- tasks/ 폴더 문서의 이동·삭제·이름 변경 (작업 문서에 보고서를 덧붙이는 것만 허용)
- docs/history/ 수정
- .env 읽기·출력·수정
- 작업 문서의 "하지 말 것" 범위
"""


def retry_prompt(reason: str) -> str:
    return f"""이전 시도가 완료 조건을 통과하지 못했다.

{reason}

원인을 고친 뒤 작업 문서의 "## 보고서" 절을 갱신하고, .loop/result.json 을 다시 써라.
규칙과 "하지 말 것"은 처음 지시와 같다."""


def run_claude(cfg: dict, prompt: str, session_id: str | None, log_name: str) -> dict:
    exe = shutil.which(cfg["claude_command"]) or cfg["claude_command"]
    cmd = [exe, "-p", "--output-format", "json", "--permission-mode", cfg["claude_permission_mode"]]
    if cfg.get("claude_allowed_tools"):
        cmd += ["--allowedTools", *cfg["claude_allowed_tools"]]
    if cfg.get("claude_disallowed_tools"):
        cmd += ["--disallowedTools", *cfg["claude_disallowed_tools"]]
    if session_id:
        cmd += ["--resume", session_id]

    env = os.environ.copy()
    for key in ("CLAUDECODE", "CLAUDE_CODE_ENTRYPOINT"):  # 다른 클로드 세션 안에서 실행할 때 대비
        env.pop(key, None)

    try:
        proc = subprocess.run(
            cmd,
            cwd=ROOT,
            input=prompt,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            env=env,
            timeout=cfg["claude_timeout_min"] * 60,
        )
    except subprocess.TimeoutExpired:
        raise LoopError(f"클로드 실행 시간 초과 ({cfg['claude_timeout_min']}분)")
    except FileNotFoundError:
        raise LoopError(f"클로드 CLI 를 찾을 수 없음: {cfg['claude_command']}")

    LOG_DIR.mkdir(parents=True, exist_ok=True)
    (LOG_DIR / f"{log_name}.out.txt").write_text(
        f"$ exit {proc.returncode}\n--- stdout ---\n{proc.stdout}\n--- stderr ---\n{proc.stderr}", encoding="utf-8"
    )

    data: dict = {}
    out = proc.stdout.strip()
    for candidate in (out, out.splitlines()[-1] if out else ""):
        try:
            parsed = json.loads(candidate)
        except json.JSONDecodeError:
            continue
        if isinstance(parsed, dict):
            data = parsed
            break

    result_text = str(data.get("result", ""))
    failed = proc.returncode != 0 or data.get("is_error") or not data
    if failed:
        blob = "\n".join([result_text, proc.stdout[-2000:], proc.stderr[-2000:]])
        if USAGE_LIMIT_RE.search(blob):
            raise UsageLimit(tail(result_text or proc.stderr or proc.stdout, 500))
        raise LoopError(
            f"클로드 실행 실패 (코드 {proc.returncode}): {tail(result_text or proc.stderr or proc.stdout, 800)}"
        )
    return data


def read_result() -> dict | None:
    if not RESULT_PATH.exists():
        return None
    try:
        data = json.loads(RESULT_PATH.read_text(encoding="utf-8-sig"))
    except json.JSONDecodeError:
        return None
    return data if isinstance(data, dict) and data.get("status") in ("done", "blocked") else None


def run_tests(cfg: dict) -> tuple[bool, str]:
    command = (cfg.get("test_command") or "").strip()
    if not command:
        return True, "테스트 명령이 비어 있어 건너뜀"
    try:
        proc = subprocess.run(
            command,
            cwd=ROOT,
            shell=True,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=cfg["test_timeout_min"] * 60,
        )
    except subprocess.TimeoutExpired:
        return False, f"테스트 시간 초과 ({cfg['test_timeout_min']}분)"
    output = f"명령: {command}\n종료 코드: {proc.returncode}\n{proc.stdout}\n{proc.stderr}"
    return proc.returncode == 0, output


# ───────────────────────── 루프 본체 ─────────────────────────


class Loop:
    def __init__(self, cfg: dict, notifier: Notifier):
        self.cfg = cfg
        self.n = notifier
        self.state: dict = {}
        self.loop_branch = ""

    # ── 시작 전 점검 ──
    def preflight(self) -> None:
        if git("rev-parse", "--is-inside-work-tree", check=False).returncode != 0:
            raise PreflightError("깃 저장소가 아님")
        if is_dirty():
            files = git("status", "--porcelain").stdout.strip()
            raise PreflightError(f"커밋 안 된 변경이 있음:\n{truncate(files, 1500)}")

        base = self.cfg["base_branch"]
        if not branch_exists(base):
            raise PreflightError(f"기준 브랜치 {base} 가 없음")

        self.state = load_state()
        active = self.state.get("loop_branch") if self.state.get("status") == "active" else None
        if active and not branch_exists(active):
            raise PreflightError(f"상태 파일의 진행 중인 루프 브랜치 {active} 가 없음 (.loop/state.json 확인)")

        unmerged = [
            b for b in local_branches(self.cfg["loop_branch_prefix"]) if b != active and not is_ancestor(b, base)
        ]
        if unmerged:
            raise PreflightError(
                "완료됐지만 main 에 합쳐지지 않은 루프 브랜치가 있음. 확인 후 직접 합치거나 정리해 주세요:\n"
                + "\n".join(f"- {b}" for b in unmerged)
            )
        self.loop_branch = active or ""

    # ── 루프 브랜치 준비 ──
    def open_loop(self) -> bool:
        for d in (TODO, DOING, BLOCKED, DONE):
            d.mkdir(parents=True, exist_ok=True)
        if self.loop_branch:
            checkout(self.loop_branch)
            log(f"진행 중인 루프 이어서 실행: {self.loop_branch}")
            return True
        if not task_docs(TODO) and not task_docs(DOING):
            log("대기 중인 작업이 없음. 루프를 만들지 않고 종료")
            return False
        run_id = f"{_dt.datetime.now():%Y%m%d-%H%M%S}"
        self.loop_branch = self.cfg["loop_branch_prefix"] + run_id
        git("branch", self.loop_branch, self.cfg["base_branch"])
        checkout(self.loop_branch)
        self.state = {
            "loop_branch": self.loop_branch,
            "status": "active",
            "started_at": _dt.datetime.now().isoformat(timespec="seconds"),
            "processed": [],
        }
        save_state(self.state)
        log(f"새 루프 시작: {self.loop_branch}")
        self.n.send("LOG", "루프 시작", f"루프 브랜치: `{self.loop_branch}`\n대기 작업 {len(task_docs(TODO))}건")
        return True

    def mark_processed(self, name: str, branch: str) -> None:
        self.state.setdefault("processed", []).append({"doc": name, "branch": branch})
        save_state(self.state)

    def ff_merge_into_loop(self, task_branch: str) -> None:
        checkout(self.loop_branch)
        git("merge", "--ff-only", task_branch)

    # ── 재시작 시 상태 맞추기 ──
    def reconcile(self) -> None:
        for doc in task_docs(DOING):
            tb = task_branch_name(self.cfg, doc.name)
            rel = f"{HISTORY_REL}/{doc.name}"
            if branch_exists(tb) and file_in_branch(tb, rel):
                log(f"점검: {doc.name} 는 완료 커밋까지 끝나 있음 → 루프 브랜치에 합치고 done 으로 이동")
                self.ff_merge_into_loop(tb)
                move_doc(doc, DONE)
                self.mark_processed(doc.name, tb)
            else:
                log(f"점검: {doc.name} 가 doing 에 남아 있음 → todo 로 되돌림")
                move_doc(doc, TODO)

        for doc in task_docs(DONE):
            rel = f"{HISTORY_REL}/{doc.name}"
            if file_in_branch(self.loop_branch, rel):
                continue
            tb = task_branch_name(self.cfg, doc.name)
            if branch_exists(tb) and file_in_branch(tb, rel):
                log(f"점검: {doc.name} 의 기록이 루프 브랜치에 없음 → {tb} 를 합침")
                self.ff_merge_into_loop(tb)
                self.mark_processed(doc.name, tb)
                continue
            log(f"점검: {doc.name} 는 done 인데 기록이 없음 → blocked 로 이동")
            reason = "done 폴더에 있지만 docs/history 에 기록이 없음. 확인 필요."
            append_section(doc, "보류 사유", f"- 사유: {reason}\n- 질문: 이 작업을 다시 처리할까요?\n- 추천안: 내용 확인 후 todo 로 옮겨 다시 처리")
            moved = move_doc(doc, BLOCKED)
            self.n.send("BLOCKED", f"보류: {moved.name}", f"**막힌 이유**\n{reason}\n\n**루프 브랜치**: `{self.loop_branch}`")

    # ── 작업 하나 처리 ──
    def prepare_task_branch(self, doc_name: str) -> str:
        tb = task_branch_name(self.cfg, doc_name)
        checkout(self.loop_branch)
        if not branch_exists(tb):
            git("checkout", "-b", tb, self.loop_branch)
            return tb

        checkout(tb)
        merge = git("merge", "--no-edit", self.loop_branch, check=False)
        if merge.returncode == 0:
            log(f"기존 기능 브랜치 {tb} 에 루프 브랜치를 합쳐 최신으로 맞춤")
            return tb

        git("merge", "--abort", check=False)
        checkout(self.loop_branch)
        git("branch", "-D", tb)
        git("checkout", "-b", tb, self.loop_branch)
        log(f"{tb} 에 루프 브랜치를 합치다 충돌 → 기존 브랜치를 지우고 루프 브랜치에서 새로 시작")
        self.n.send("LOG", f"기능 브랜치 새로 시작: {doc_name}", f"`{tb}` 에서 루프 브랜치 병합 충돌 → 기존 브랜치 삭제 후 `{self.loop_branch}` 에서 새로 만듦")
        return tb

    def process(self, todo_doc: Path) -> None:
        doc = move_doc(todo_doc, DOING)
        name = doc.name
        log(f"작업 시작: {name}")
        tb = self.prepare_task_branch(name)
        self.n.send("LOG", f"작업 시작: {name}", f"기능 브랜치: `{tb}`\n루프 브랜치: `{self.loop_branch}`")

        max_retries = int(self.cfg["max_retries"])
        session_id: str | None = None
        feedback = ""
        last_failure = ""
        result: dict | None = None
        stem = Path(name).stem

        for attempt in range(1, max_retries + 1):
            if attempt > 1:
                log(f"재시도 {attempt - 1}/{max_retries - 1}: {name}")
                self.n.send("LOG", f"재시도 {attempt - 1}회째: {name}", truncate(last_failure, 1500))
            RESULT_PATH.unlink(missing_ok=True)
            prompt = first_prompt(name, tb) if attempt == 1 or not session_id else retry_prompt(feedback)
            if attempt > 1 and not session_id:
                prompt += "\n\n" + retry_prompt(feedback)

            try:
                data = run_claude(self.cfg, prompt, session_id, f"{stem}_attempt{attempt}")
            except UsageLimit as e:
                self.stop_for_usage_limit(doc, tb, str(e))
                raise
            session_id = data.get("session_id") or session_id

            if current_branch() != tb:
                raise LoopError(f"클로드 실행 뒤 브랜치가 {tb} 가 아님 (현재 {current_branch()})")
            if not doc.exists():
                raise LoopError(f"클로드 실행 뒤 작업 문서 {doc.relative_to(ROOT)} 가 사라짐")

            result = read_result()
            if result is None:
                last_failure = ".loop/result.json 이 없거나 형식이 맞지 않음"
                feedback = last_failure + ". 형식에 맞게 다시 써라."
                continue
            if result["status"] == "blocked":
                self.block(doc, tb, result.get("reason", ""), result.get("question", ""), result.get("recommendation", ""))
                return

            ok, test_output = run_tests(self.cfg)
            log(f"테스트: {'통과' if ok else '실패'} ({test_output.splitlines()[0] if test_output else ''})")
            if ok:
                self.complete(doc, tb, result.get("summary", ""))
                return
            last_failure = "테스트 실패\n" + tail(test_output, 1500)
            feedback = "테스트가 실패했다.\n" + tail(test_output, 6000)

        self.block(
            doc,
            tb,
            f"재시도 한도({max_retries}회) 초과. 마지막 실패:\n{tail(last_failure, 1500)}",
            "실패 원인을 확인하고 작업 문서나 완료 조건을 고쳐 주세요.",
            "마지막 실패 내용과 기능 브랜치의 작업 중 상태를 보고 판단",
        )

    def complete(self, doc: Path, tb: str, summary: str) -> None:
        history_dir = ROOT / HISTORY_REL
        history_dir.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(doc, history_dir / doc.name)
        RESULT_PATH.unlink(missing_ok=True)
        message = f"작업 완료: {doc.stem}\n\n{summary.strip()}\n\n작업 문서: {HISTORY_REL}/{doc.name}\n"
        commit_all(message)  # 실패하면 LoopError → done 으로 옮기지 않음
        if not file_in_branch(tb, f"{HISTORY_REL}/{doc.name}"):
            raise LoopError(f"{tb} 에 기록 커밋이 확인되지 않음")
        self.ff_merge_into_loop(tb)
        move_doc(doc, DONE)
        self.mark_processed(doc.name, tb)
        log(f"작업 완료: {doc.name}")
        self.n.send(
            "DONE",
            f"작업 완료: {doc.name}",
            f"**바뀐 내용**\n{truncate(summary, 2500)}\n\n**기능 브랜치**: `{tb}`\n**루프 브랜치**: `{self.loop_branch}`",
        )

    def block(self, doc: Path, tb: str, reason: str, question: str, recommendation: str) -> None:
        RESULT_PATH.unlink(missing_ok=True)
        committed = commit_all(f"보류(작업 중 상태): {doc.stem}\n\n{truncate(reason, 1000)}\n")
        append_section(
            doc,
            "보류 사유",
            f"- 사유: {reason}\n- 질문: {question}\n- 추천안: {recommendation}\n- 기능 브랜치: {tb}"
            + (" (작업 중 상태 커밋됨)" if committed else ""),
        )
        moved = move_doc(doc, BLOCKED)
        checkout(self.loop_branch)
        log(f"보류: {moved.name}")
        self.n.send(
            "BLOCKED",
            f"보류: {moved.name}",
            f"**막힌 이유**\n{truncate(reason, 1500)}\n\n**질문**\n{truncate(question, 1000)}\n\n"
            f"**추천안**\n{truncate(recommendation, 1000)}\n\n**기능 브랜치**: `{tb}`",
        )

    def stop_for_usage_limit(self, doc: Path, tb: str, detail: str) -> None:
        RESULT_PATH.unlink(missing_ok=True)
        commit_all(f"사용량 한도로 중단(작업 중 상태): {doc.stem}\n")
        move_doc(doc, TODO)
        checkout(self.loop_branch)
        log(f"사용량 한도 초과로 중단: {doc.name}")
        self.n.send(
            "STOPPED",
            "루프 멈춤: 사용량 한도 초과",
            f"작업 `{doc.name}` 은 todo 로 되돌림. 작업 중 상태는 `{tb}` 에 커밋됨.\n"
            f"루프 브랜치 `{self.loop_branch}` 는 그대로 둠. 한도가 풀리면 다시 실행하세요.\n\n{truncate(detail, 800)}",
        )

    # ── 루프 끝 ──
    def finish(self) -> None:
        blocked = task_docs(BLOCKED)
        if task_docs(TODO) or task_docs(DOING):
            return
        if blocked:
            log(f"대기 작업이 비어 종료. 보류 {len(blocked)}건 남음 → 루프는 진행 중으로 유지")
            self.n.send(
                "LOG",
                "대기 비어 종료 (루프 진행 중)",
                f"보류 {len(blocked)}건 남음:\n" + "\n".join(f"- {d.name}" for d in blocked)
                + f"\n\n루프 브랜치: `{self.loop_branch}`",
            )
            return
        self.state["status"] = "finished"
        self.state["finished_at"] = _dt.datetime.now().isoformat(timespec="seconds")
        save_state(self.state)
        done = self.state.get("processed", [])
        lines = "\n".join(f"- {p['doc']} (`{p['branch']}`)" for p in done) or "- (없음)"
        log(f"루프 종료: {self.loop_branch}")
        self.n.send(
            "DONE",
            "루프 종료",
            f"**처리한 작업 {len(done)}건**\n{lines}\n\n**확인할 루프 브랜치**: `{self.loop_branch}`\n"
            f"확인 후 main 에 직접 합쳐 주세요.",
        )

    def run(self) -> None:
        self.preflight()
        if not self.open_loop():
            return
        self.reconcile()
        while True:
            todo = task_docs(TODO)
            if not todo:
                break
            self.process(todo[0])
        self.finish()


def main() -> int:
    global _log_file
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass
    LOG_DIR.mkdir(parents=True, exist_ok=True)
    _log_file = LOG_DIR / f"run_{_dt.datetime.now():%Y%m%d-%H%M%S}.log"

    notifier = Notifier(load_env(ROOT / ".env"))
    loop: Loop | None = None
    try:
        cfg = load_config()
        loop = Loop(cfg, notifier)
        loop.run()
        return 0
    except PreflightError as e:
        log(f"시작 전 점검 실패: {e}")
        notifier.send("STOPPED", "루프 시작 안 함: 시작 전 점검 실패", str(e))
        return 3
    except UsageLimit:
        return 2
    except KeyboardInterrupt:
        log("사용자가 중단함")
        notifier.send("STOPPED", "루프 멈춤: 사용자 중단", "상태는 그대로 둠. 다시 실행하면 점검 후 이어서 진행")
        return 1
    except Exception as e:
        detail = traceback.format_exc()
        log(f"오류로 멈춤:\n{detail}")
        branch = loop.loop_branch if loop and loop.loop_branch else "(없음)"
        notifier.send(
            "STOPPED",
            "루프 멈춤: 오류",
            f"{type(e).__name__}: {truncate(str(e), 1500)}\n\n상태는 건드리지 않음.\n루프 브랜치: `{branch}`\n"
            f"로그: .loop/logs/{_log_file.name}",
        )
        return 1


if __name__ == "__main__":
    sys.exit(main())
