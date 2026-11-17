"""매화검 키우기 — AI 자동 개발 루프 (3차 버전)

tasks/todo 의 작업 문서를 선행 작업과 묶음 규칙에 따라 하나씩 꺼내, 루프 전용 git worktree 에서
클로드 코드 CLI(한도 초과 시 코덱스 CLI)로 처리한다. 작업마다 테스트, 관문 검사, 교차 리뷰를 통과해야 완료되고,
묶음이 끝나면 증거(docs/evidence)를 남기고 통합 브랜치에 합친 뒤 다음 묶음으로 넘어간다.
main 에 올리는 것과 푸시는 사람이 한다. 사용법과 규칙은 docs/dev/loop.md 참고.

실행:
  python tools/loop/run_loop.py           꺼낼 작업이 없을 때까지 돌고 끝남
  python tools/loop/run_loop.py --watch   끝나지 않고 일정 간격으로 todo 를 확인

종료 코드: 0 정상, 1 오류로 멈춤, 2 사용량 한도로 멈춤, 3 시작 전 점검 실패, 4 같은 프로젝트에서 이미 실행 중
"""

from __future__ import annotations

import argparse
import datetime as _dt
import json
import os
import re
import shutil
import signal
import subprocess
import sys
import time
import traceback
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]  # 사용자 폴더 (항상 main). tasks, .env, .loop 는 여기 것을 쓴다
TOOL_DIR = Path(__file__).resolve().parent
if str(TOOL_DIR) not in sys.path:  # 다른 스크립트가 이 모듈을 불러 쓸 때도 옆의 모듈을 찾게 한다
    sys.path.insert(0, str(TOOL_DIR))

import loop_evidence  # noqa: E402
import loop_gate  # noqa: E402

CONFIG_PATH = TOOL_DIR / "loop_config.json"
LOCAL_CONFIG_PATH = TOOL_DIR / "loop_config.local.json"
TASKS = ROOT / "tasks"
TODO, DOING, WAIT, BLOCKED, DONE = (TASKS / n for n in ("todo", "doing", "wait", "blocked", "done"))
ALL_FOLDERS = {"todo": TODO, "doing": DOING, "wait": WAIT, "blocked": BLOCKED, "done": DONE}
HISTORY_REL = "docs/history"
LOOP_DIR = ROOT / ".loop"
STATE_PATH = LOOP_DIR / "state.json"
RUNNING_PATH = LOOP_DIR / "running.json"  # 지금 실행 중인 루프 프로세스 기록. 실행이 끝나면 지운다
LOG_DIR = LOOP_DIR / "logs"
WT: Path = ROOT  # 루프 전용 worktree. main() 에서 설정값으로 정한다

WEBHOOK_KEYS = {
    "DONE": "DISCORD_WEBHOOK_DONE",
    "BLOCKED": "DISCORD_WEBHOOK_BLOCKED",
    "STOPPED": "DISCORD_WEBHOOK_STOPPED",
    "LOG": "DISCORD_WEBHOOK_LOG",
}
WEBHOOK_COLORS = {"DONE": 0x2ECC71, "BLOCKED": 0xF1C40F, "STOPPED": 0xE74C3C, "LOG": 0x95A5A6}

# 자식 프로세스(클로드, 코덱스, 깃)는 별도 프로세스 그룹으로 띄워 콘솔의 Ctrl+C 가 전달되지 않게 한다
CHILD_KW: dict = (
    {"creationflags": subprocess.CREATE_NEW_PROCESS_GROUP} if os.name == "nt" else {"start_new_session": True}
)


class LoopError(Exception):
    """예상 못 한 오류. 상태를 건드리지 않고 멈춘다."""


class UsageLimit(Exception):
    """에이전트 사용량 한도 초과."""

    def __init__(self, agent: str, detail: str):
        super().__init__(detail)
        self.agent = agent
        self.detail = detail


class AllAgentsLimited(Exception):
    """모든 에이전트가 사용량 한도에 걸려 루프를 멈췄다 (정리는 이미 끝남)."""


class AgentFailure(Exception):
    """에이전트 시간 초과·실행 실패. 실패한 시도 1회로 센다. transient 면 일시적 오류(네트워크 등)."""

    def __init__(self, agent: str, detail: str, transient: bool = False):
        super().__init__(detail)
        self.agent = agent
        self.detail = detail
        self.transient = transient


class TransientError(Exception):
    """일시적 오류로 이번 실행을 접었다 (정리는 이미 끝남). 감시 모드는 간격을 두고 다시 시도한다."""


class PreflightError(Exception):
    """시작 전 점검 실패."""


# ───────────────────────── 종료 요청 (Ctrl+C) ─────────────────────────


class _Stop:
    requested = False


STOP = _Stop()


def _on_signal(signum, frame) -> None:
    if STOP.requested:
        raise KeyboardInterrupt
    STOP.requested = True
    log("종료 요청을 받음: 지금 하던 작업까지만 끝내고 종료합니다 (한 번 더 누르면 즉시 중단)")


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
        cfg = json.load(f)
    if LOCAL_CONFIG_PATH.exists():
        with LOCAL_CONFIG_PATH.open(encoding="utf-8") as f:
            local = json.load(f)
        for key, value in local.items():
            if isinstance(value, dict) and isinstance(cfg.get(key), dict):
                cfg[key] = {**cfg[key], **value}
            else:
                cfg[key] = value
    return cfg


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
        self._once: dict[str, str] = {}

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
            headers={"Content-Type": "application/json", "User-Agent": "PlumSwordIdleLoop/2.0"},
            method="POST",
        )
        try:
            with urllib.request.urlopen(req, timeout=15) as resp:
                log(f"알림[{channel}] 전송 완료 (HTTP {resp.status}): {title}")
        except urllib.error.HTTPError as e:
            log(f"알림[{channel}] 전송 실패 (HTTP {e.code}): {title}")
        except Exception as e:  # 알림 실패로 루프를 멈추지 않는다
            log(f"알림[{channel}] 전송 실패 ({type(e).__name__}): {title}")

    def send_once(self, key: str, signature: str, channel: str, title: str, body: str) -> None:
        """같은 key 로 같은 내용(signature)을 이미 보냈으면 다시 보내지 않는다 (감시 모드 반복 방지)."""
        if self._once.get(key) == signature:
            return
        self._once[key] = signature
        self.send(channel, title, body)

    def reset(self, key: str) -> None:
        self._once.pop(key, None)


# ───────────────────────── git ─────────────────────────


def run_git(cwd: Path, *args: str, check: bool = True, input_text: str | None = None) -> subprocess.CompletedProcess:
    proc = subprocess.run(
        ["git", *args],
        cwd=cwd,
        input=input_text,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        **CHILD_KW,
    )
    if check and proc.returncode != 0:
        raise LoopError(
            f"git {' '.join(args)} 실패 (코드 {proc.returncode}, {cwd}):\n{proc.stderr.strip() or proc.stdout.strip()}"
        )
    return proc


def git(*args: str, **kw) -> subprocess.CompletedProcess:
    """사용자 폴더에서 실행. 조회와 브랜치 생성만 한다."""
    return run_git(ROOT, *args, **kw)


def wgit(*args: str, **kw) -> subprocess.CompletedProcess:
    """루프 worktree 에서 실행. 체크아웃·커밋·병합은 여기서만 한다."""
    return run_git(WT, *args, **kw)


def branch_exists(name: str) -> bool:
    return git("show-ref", "--verify", "--quiet", f"refs/heads/{name}", check=False).returncode == 0


def is_ancestor(a: str, b: str) -> bool:
    return git("merge-base", "--is-ancestor", a, b, check=False).returncode == 0


def file_in_branch(branch: str, rel_path: str) -> bool:
    return git("cat-file", "-e", f"{branch}:{rel_path}", check=False).returncode == 0


def local_branches(prefix: str) -> list[str]:
    out = git("for-each-ref", "--format=%(refname:short)", f"refs/heads/{prefix}").stdout
    return [b.strip() for b in out.splitlines() if b.strip()]


def wt_dirty() -> str:
    return wgit("status", "--porcelain").stdout.strip()


def wt_branch() -> str:
    return wgit("rev-parse", "--abbrev-ref", "HEAD").stdout.strip()


def wt_commit_all(message: str) -> bool:
    """worktree 의 변경을 전부 커밋하고 True. 변경이 없으면 False."""
    wgit("add", "-A")
    if not wgit("diff", "--cached", "--name-only").stdout.strip():
        return False
    wgit("commit", "-F", "-", input_text=message)
    return True


def _norm(p: str | Path) -> str:
    return os.path.normcase(os.path.abspath(str(p)))


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


def record_stop(reason: str) -> None:
    """루프가 멈춘 이유를 상태 파일에 남긴다. 다음 실행이 남은 변경을 복구할 때 커밋 메시지와 알림에 쓴다."""
    try:
        state = load_state()
        if state.get("status") != "active":
            return
        state["last_stop"] = {"at": _dt.datetime.now().isoformat(timespec="seconds"), "reason": truncate(reason, 500)}
        save_state(state)
    except Exception:
        pass  # 기록 실패로 종료 처리를 막지 않는다


# ───────────────────────── 실행 중 기록 (.loop/running.json) ─────────────────────────


def _pid_alive(pid: int) -> bool:
    if pid <= 0:
        return False
    if os.name == "nt":  # 윈도우의 os.kill 은 프로세스를 끝내 버리므로 쓰지 않는다
        import ctypes

        kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
        handle = kernel32.OpenProcess(0x1000, False, pid)  # PROCESS_QUERY_LIMITED_INFORMATION
        if not handle:
            return ctypes.get_last_error() == 5  # 접근 거부 = 프로세스는 있음
        try:
            code = ctypes.c_ulong()
            return bool(kernel32.GetExitCodeProcess(handle, ctypes.byref(code))) and code.value == 259  # STILL_ACTIVE
        finally:
            kernel32.CloseHandle(handle)
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    except PermissionError:
        return True
    return True


def _pid_cmdline(pid: int) -> str | None:
    """프로세스 명령줄. 알 수 없으면 None."""
    try:
        if os.name == "nt":
            proc = subprocess.run(
                ["powershell", "-NoProfile", "-NonInteractive", "-Command",
                 f"(Get-CimInstance Win32_Process -Filter 'ProcessId={pid}').CommandLine"],
                capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=30, **CHILD_KW,
            )  # fmt: skip
            out = proc.stdout.strip()
            return out if proc.returncode == 0 and out else None
        cmdline = Path(f"/proc/{pid}/cmdline")
        if cmdline.exists():
            return cmdline.read_bytes().replace(bytes(1), b" ").decode("utf-8", "replace").strip() or None
        proc = subprocess.run(["ps", "-o", "args=", "-p", str(pid)], capture_output=True, text=True, timeout=30)
        return proc.stdout.strip() or None
    except Exception:
        return None


def running_owner() -> dict | None:
    """기록된 루프 프로세스가 아직 돌고 있으면 그 기록을 돌려준다. 강제 종료로 남은 기록이면 None."""
    try:
        info = json.loads(RUNNING_PATH.read_text(encoding="utf-8"))
        pid = int(info.get("pid", 0))
    except FileNotFoundError:
        return None
    except Exception:
        return None  # 깨진 기록은 남은 기록으로 본다
    if pid == os.getpid() or not _pid_alive(pid):
        return None
    cmdline = _pid_cmdline(pid)
    if cmdline is not None and "run_loop" not in cmdline:
        return None  # PID 가 다른 프로그램에 재사용됨
    info["cmdline_known"] = cmdline is not None
    return info


def claim_running(mode: str) -> dict | None:
    """실행 중 기록을 만든다. 다른 루프가 이미 돌고 있으면 만들지 않고 그 기록을 돌려준다."""
    LOOP_DIR.mkdir(exist_ok=True)
    record = {
        "pid": os.getpid(),
        "mode": mode,
        "started_at": _dt.datetime.now().isoformat(timespec="seconds"),
        "log_file": _log_file.name if _log_file else None,
        "root": str(ROOT),
        "script": str(Path(__file__).resolve()),
    }
    for _ in range(2):
        try:
            fd = os.open(RUNNING_PATH, os.O_WRONLY | os.O_CREAT | os.O_EXCL)
        except FileExistsError:
            owner = running_owner()
            if owner:
                return owner
            stale = RUNNING_PATH.read_text(encoding="utf-8", errors="replace") if RUNNING_PATH.exists() else ""
            log(f"남은 실행 중 기록을 덮어씀 (기록된 루프 프로세스가 없음, 강제 종료 추정): {truncate(stale.strip(), 300)}")
            RUNNING_PATH.unlink(missing_ok=True)
            continue
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            json.dump(record, f, ensure_ascii=False, indent=2)
        return None
    raise LoopError("실행 중 기록(.loop/running.json)을 만들지 못함")


def release_running() -> None:
    """내가 만든 실행 중 기록만 지운다."""
    try:
        info = json.loads(RUNNING_PATH.read_text(encoding="utf-8"))
        if int(info.get("pid", 0)) == os.getpid():
            RUNNING_PATH.unlink()
    except Exception:
        pass  # 지우지 못해도 다음 실행이 PID 를 보고 덮어쓴다


# ───────────────────────── 사용자 폴더 감시 ─────────────────────────


def root_snapshot() -> dict[str, tuple[int, int]]:
    """에이전트가 건드리면 안 되는 사용자 폴더 파일(tasks, .env)의 크기와 수정 시각."""
    snap: dict[str, tuple[int, int]] = {}
    files = [p for folder in ALL_FOLDERS.values() if folder.exists() for p in folder.iterdir() if p.is_file()]
    files.append(ROOT / ".env")
    for p in files:
        try:
            st = p.stat()
        except OSError:
            continue
        snap[p.relative_to(ROOT).as_posix()] = (st.st_size, st.st_mtime_ns)
    return snap


def snapshot_changes(before: dict, after: dict) -> list[str]:
    """에이전트 실행 전후 스냅숏의 차이. 사람이 실행 중에 하는 정상 작업은 뺀다:
    todo 에 새 문서 넣기, blocked 문서에 답 적기, blocked 문서를 todo 나 tasks 밖으로 옮기기.
    처리 중인 문서(doing)와 .env 의 변경, 그 밖의 폴더 변경은 그대로 보고한다."""
    changes = []
    for key in sorted(set(before) | set(after)):
        if key not in before:
            if not key.startswith("tasks/todo/"):
                changes.append(f"{key} (새로 생김)")
        elif key not in after:
            if not key.startswith("tasks/blocked/"):
                changes.append(f"{key} (사라짐)")
        elif before[key] != after[key]:
            if not key.startswith("tasks/blocked/"):
                changes.append(f"{key} (바뀜)")
    return changes


# ───────────────────────── 작업 문서 ─────────────────────────

_HEADING_RE = re.compile(r"^##\s+(.+?)\s*$", re.MULTILINE)
_NAME_RE = re.compile(r"[0-9A-Za-z가-힣_.\-]+")


def _order_key(p: Path) -> tuple[int, str]:
    m = re.match(r"(\d+)", p.name)
    return (int(m.group(1)) if m else 10**9, p.name)


def task_docs(folder: Path) -> list[Path]:
    return sorted((p for p in folder.glob("*.md") if p.is_file()), key=_order_key)


def _section(text: str, prefix: str) -> str | None:
    matches = list(_HEADING_RE.finditer(text))
    for i, m in enumerate(matches):
        if m.group(1).strip().startswith(prefix):
            end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
            return text[m.end() : end]
    return None


class Doc:
    def __init__(self, path: Path, default_bundle: str):
        self.path = path
        self.name = path.stem
        text = path.read_text(encoding="utf-8-sig")
        self.deps = self._parse_deps(_section(text, "선행 작업"))
        bundle = self._first_value(_section(text, "묶음"))
        self.bundle_missing = bundle is None
        self.bundle = bundle or default_bundle

    @staticmethod
    def _clean(token: str) -> str | None:
        t = token.strip().lstrip("-*").strip().strip("`").strip()
        m = _NAME_RE.match(t)
        if not m:
            return None
        name = m.group(0)
        name = name[:-3] if name.lower().endswith(".md") else name
        return None if name in ("", "없음") else name

    @classmethod
    def _parse_deps(cls, section: str | None) -> list[str]:
        if not section:
            return []
        names = []
        for token in re.split(r"[\n,]", section):
            name = cls._clean(token)
            if name and name not in names:
                names.append(name)
        return names

    @classmethod
    def _first_value(cls, section: str | None) -> str | None:
        if not section:
            return None
        for line in section.splitlines():
            value = line.strip().lstrip("-*").strip().strip("`").strip()
            if value:
                return None if value == "없음" else value
        return None


def task_branch_name(cfg: dict, doc_name: str) -> str:
    stem = Path(doc_name).stem
    safe = re.sub(r"[^0-9A-Za-z가-힣_.-]+", "-", stem).strip("-.") or "task"
    return cfg["task_branch_prefix"] + safe


def find_doc(name: str) -> str | None:
    for folder_name, folder in ALL_FOLDERS.items():
        if (folder / f"{name}.md").exists():
            return folder_name
    return None


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


_TAIL_TITLES = ("보고서", "보류 사유", "답변", "루프 기록")  # 양식 칸 뒤에 덧붙는 절


def doc_head(text: str) -> str:
    """작업 문서에서 사람이 승인한 부분(양식 칸). 보고서·보류 사유·답변·루프 기록 앞까지다."""
    for m in _HEADING_RE.finditer(text):
        if m.group(1).strip().startswith(_TAIL_TITLES):
            return text[: m.start()]
    return text


def restore_doc_head(doc: Path, original_head: str) -> bool:
    """에이전트가 작업 문서의 양식 칸(요구사항, 완료 조건, 관문 예외 등)을 고쳤으면 원래대로 되돌린다."""
    text = doc.read_text(encoding="utf-8-sig")
    head = doc_head(text)
    if head.strip() == original_head.strip():
        return False
    doc.write_text(original_head.rstrip("\n") + "\n\n" + text[len(head) :].lstrip("\n"), encoding="utf-8")
    return True


def cycle_members(graph: dict[str, list[str]]) -> dict[str, list[str]]:
    """순환에 걸린 문서 → 순환 경로. graph 는 아직 끝나지 않은 문서끼리의 선행 작업 관계."""
    found: dict[str, list[str]] = {}

    def path_back(start: str) -> list[str] | None:
        stack = [(start, [start])]
        seen: set[str] = set()
        while stack:
            node, path = stack.pop()
            for nxt in graph.get(node, []):
                if nxt == start:
                    return path + [start]
                if nxt in graph and nxt not in seen:
                    seen.add(nxt)
                    stack.append((nxt, path + [nxt]))
        return None

    for node in graph:
        p = path_back(node)
        if p:
            found[node] = p
    return found


# ───────────────────────── worktree 의 작업 문서 사본 ─────────────────────────


def wt_doc_path(name: str) -> Path:
    return WT / "tasks" / "doing" / name


def result_path() -> Path:
    return WT / ".loop" / "result.json"


def sync_to_wt(doc: Path) -> None:
    folder = WT / "tasks" / "doing"
    folder.mkdir(parents=True, exist_ok=True)
    for stale in folder.glob("*.md"):
        if stale.name != doc.name:
            stale.unlink()
    shutil.copyfile(doc, wt_doc_path(doc.name))


def sync_from_wt(doc: Path) -> None:
    src = wt_doc_path(doc.name)
    if not src.exists():
        raise LoopError(f"worktree 의 작업 문서 사본 {src} 가 사라짐")
    shutil.copyfile(src, doc)


def clear_wt_doc(name: str) -> None:
    wt_doc_path(name).unlink(missing_ok=True)
    result_path().unlink(missing_ok=True)


# ───────────────────────── 에이전트 실행 ─────────────────────────


def first_prompt(doc_name: str, branch: str) -> str:
    return f"""너는 자동 개발 루프 안에서 작업 문서 하나를 처리하는 에이전트다. 사람은 자리에 없어서 질문해도 답할 수 없다.

작업 폴더: 루프 전용 git worktree (현재 폴더)
작업 문서: tasks/doing/{doc_name}
현재 브랜치: {branch} (루프 스크립트가 만들었다)

순서:
1. 프로젝트 규칙(CLAUDE.md, AGENTS.md — 같은 내용)을 따른다. docs/index.md(최상위 색인)를 먼저 읽고, 작업 문서와 그 "참고 문서"를 읽는다.
   docs/design/, docs/project/, docs/archive/ 는 사람용 문서라 읽지 않는다. 필요한 내용이 사양과 작업 문서에 없으면 가정으로 정하거나 blocked 로 보고한다.
   작업 문서 끝에 "## 답변 (날짜 시각)" 절이 있으면, 그것은 사람이 바로 앞의 "## 보류 사유"에 답한 것이다. 앞의 보류 사유·보고서와 다르면 답변을 따른다 (답변이 여러 개면 마지막 것이 최신).
2. 계획을 세운 뒤 구현한다. 완료 조건에 맞는 테스트가 필요하면 함께 작성한다.
3. 바뀐 사양·중요한 결정을 docs/spec/ 이나 docs/dev/ 의 문서에 반영하고, 필요하면 그 폴더의 index.md 를 갱신한다.
   연결 지점(다른 기능이 부르는 공개 타입·메서드)의 원본은 사양 문서 docs/spec/<기능>.md 의 "연결 지점" 절이다. 구현하다 모양이 사양과 달라졌으면 그 절을 고치고 보고서의 가정에 적는다.
4. 작업 문서(tasks/doing/{doc_name}) 맨 끝에 "## 보고서" 절을 추가한다. 아래 소제목 다섯 개를 글자와 순서 그대로 쓴다 (리뷰 도구가 이 제목으로 찾는다). 내용이 없으면 "없음"이라고 적는다.
   ### 한 일
   ### 바뀐 파일
   ### 가정          (작업 문서와 사양에 없어 스스로 정한 것 전부. 임시 수치 포함)
   ### 사람이 확인할 것  (완료 조건의 [사람 확인] 항목을 옮겨 적고, 테스트로 판정하지 못한 것을 더한다)
   ### 테스트 결과
5. 마지막으로 .loop/result.json 을 UTF-8 JSON 으로 쓴다.
   - 완료: {{"status": "done", "summary": "바뀐 내용 요약 (몇 줄)"}}
   - 스스로 판단할 수 없어 진행 불가: {{"status": "blocked", "reason": "막힌 이유", "question": "사람에게 할 질문", "recommendation": "추천안"}}
   애매한 점은 규칙의 "애매한 점 처리"대로 먼저 스스로 정하고, 정말 불가능할 때만 blocked 로 한다.
6. 보고서와 result.json 은 파일을 직접 편집하는 도구로 UTF-8 그대로 쓰고, 줄바꿈은 실제 줄바꿈으로 쓴다.
   윈도 PowerShell 에서 한글을 파이프(`@'...'@ | python -` 등)나 명령줄 인자로 넘기면 '?' 로 깨진다. 깨진 글은 스크립트가 받아들이지 않는다.
   `\\n` 두 글자를 줄바꿈 대신 쓰면 제목이 인식되지 않아 보고서가 지워진다.

하지 말 것:
- git commit, push, checkout, switch, merge, rebase, reset, branch, stash, tag 등 깃 상태를 바꾸는 명령 (스크립트가 한다)
- tasks/ 폴더 문서의 이동·삭제·이름 변경 (작업 문서에 보고서를 덧붙이는 것만 허용)
- docs/history/ 수정
- .env 읽기·출력·수정
- 현재 폴더(worktree) 밖에 쓰기, 원래 프로젝트 폴더 접근
- 작업 문서의 "하지 말 것" 범위

위 금지 사항은 검사 장치가 막는다. 막힌 호출을 다른 방법으로 우회하지 말고, 그것 없이는 진행할 수 없으면 blocked 로 보고해라.
"""


def wip_note(loop_branch: str) -> str:
    return f"""참고: 이 기능 브랜치에는 이전에 중단되거나 보류된 실행이 남긴 작업 중 커밋이 있다.
git log {loop_branch}..HEAD 와 git diff {loop_branch}...HEAD 로 이미 된 부분을 확인하고 이어서 완성해라.
작업 문서에 "## 답변" 절이 있으면 그 답에 맞게 이어간다. 이전 작업이 답변과 어긋나면 답변대로 고친다."""


def interrupted_feedback(agent: str, detail: str) -> str:
    return f"""이전 시도({agent})가 끝나지 못하고 중간에 끊겼다: {detail}
작업 폴더에 하던 변경이 커밋 없이 남아 있을 수 있다. git status 와 git diff 로 확인하고 이어서 완성해라.
같은 이유로 다시 끊기지 않게, 오래 걸리는 명령은 피하거나 나눠서 실행해라."""


def retry_prompt(reason: str) -> str:
    return f"""이전 시도가 완료 조건을 통과하지 못했다.

{reason}

원인을 고친 뒤 작업 문서의 "## 보고서" 절을 갱신하고, .loop/result.json 을 다시 써라.
규칙과 "하지 말 것"은 처음 지시와 같다."""


def gate_feedback(violations: str, loop_branch: str) -> str:
    return f"""테스트는 통과했지만 관문 검사(스크립트가 하는 규칙 검사)에 걸렸다. 이 상태로는 합쳐지지 않는다.

{violations}

위반을 없애라.
- 바꾸면 안 되는 파일을 바꿨으면 원래 내용으로 되돌린다. 깃으로 되돌리는 명령은 막혀 있으니
  `git show {loop_branch}:<경로>` 로 원래 내용을 보고 고쳐 쓴다. 새로 만든 파일이면 지운다 (.meta 도 같이).
- 테스트를 지우거나 약하게 만들었으면 되살린다. 완료 조건마다 그것을 실제로 검사하는 테스트가 있어야 한다.
- 작업 문서가 그 변경을 꼭 요구해서 없앨 수 없으면, 우회하지 말고 blocked 로 보고해라 (사람의 승인이 필요하다)."""


def review_feedback(review: dict) -> str:
    items = [f for f in review.get("findings") or [] if f.get("severity") in ("fix", "human")]
    listing = "\n".join(f"- {f.get('text', '')}" for f in items) or "- (지적 목록이 비어 있다. 요약을 보고 판단해라)"
    return f"""테스트와 관문은 통과했지만, 다른 에이전트의 리뷰가 아래 문제를 찾았다. 이 상태로는 합쳐지지 않는다.

리뷰 요약: {review.get("summary", "")}

고칠 것:
{listing}

하나씩 확인하고 고쳐라. 지적이 틀렸다고 판단하면 고치지 말고, 왜 틀렸는지를 보고서의 "### 가정" 아래에 근거(파일과 줄)와 함께 적어라.
리뷰어는 그 설명을 읽고 다시 판단한다."""


def review_prompt(doc_name: str, loop_branch: str, snapshot: str, gate_warnings: list[str], implementer: str,
                  expected_conditions: list[tuple[str, str]]) -> str:
    warnings = "\n".join("- " + w for w in gate_warnings) or "- 없음"
    expected = json.dumps([{"id": i, "mark": k, "condition": c} for i, (k, c) in enumerate(expected_conditions, 1)], ensure_ascii=False)
    return f"""너는 자동 개발 루프의 리뷰어다. 구현 에이전트({implementer})가 작업 문서 하나를 구현했고, 테스트와 기계 검사(관문)는 통과했다.
네 판정이 "pass" 면 사람의 확인 없이 통합 브랜치에 합쳐진다. 사람은 자리에 없다. 구현한 에이전트의 보고서를 믿지 말고 코드를 직접 읽어라.

작업 폴더: 루프 전용 git worktree (현재 폴더). **아무것도 고치지 않는다. 읽기만 한다.**
작업 문서: tasks/doing/{doc_name} (끝의 "## 보고서"는 구현한 에이전트가 쓴 것이다)
이 작업의 변경: `git diff {loop_branch} {snapshot}` (파일 목록은 `--stat`). 파일 전체는 작업 폴더에서 그대로 읽으면 된다.
규칙: AGENTS.md (CLAUDE.md 와 같은 내용), docs/dev/architecture.md, 작업 문서의 "참고 문서"에 적힌 사양(docs/spec/). docs/design/, docs/project/, docs/archive/ 는 읽지 않는다.

기계 검사가 "사람이 볼 것"으로 남긴 항목:
{warnings}

확인할 것:
1. 완료 조건마다 그것을 실제로 검사하는 테스트가 있는가. 이름만 보지 말고 본문의 검사(Assert)가 그 조건의 상황과 기대 결과(숫자 포함)를 확인하는지 읽는다.
   [테스트]·[플레이 모드]·표시 없는 조건은 테스트가 있어야 한다. [컴파일]은 "루프가 판정", [사람 확인]은 "사람 확인"으로 적고 문제 삼지 않는다.
   "기존 테스트가 모두 통과한다" 같은 조건도 "루프가 판정"이다.
2. 요구사항이 빠짐없이 구현됐는가. 보고서에 했다고 적힌 것이 코드에 정말 있는가.
3. 테스트를 통과시키려고 테스트나 기대값을 구현에 맞춰 바꾸지 않았는가. 위 기계 검사 항목에 지워진 검사가 있으면 뜻이 약해졌는지 읽는다.
4. 작업 문서의 "하지 말 것"을 건드리지 않았는가. 다른 기능의 내부 코드를 직접 고치지 않았는가 (연결 지점으로만 쓴다).
5. 계산이 화면 코드(Assets/Scripts/Game)에 들어가지 않았는가. 화면 코드는 입력 전달과 표시만 한다.
6. 보고서의 "가정"이 사양(docs/spec/)과 어긋나지 않는가. 보고서에 적히지 않은 임시 수치나 스스로 정한 동작이 코드에 있는가.
7. 작업 문서에 "## 답변" 절이 있으면 사람의 답대로 구현됐는가.

판정:
- "pass": 위 항목에 문제가 없다. 사소한 지적(이름, 주석, 정리)은 severity "note" 로 남기고 pass 로 한다.
- "fix": 구현한 에이전트가 고칠 수 있는 문제가 있다 (테스트 없는 완료 조건, 약한 테스트, 빠진 요구사항, 하지 말 것 침범, 화면 코드의 계산).
- "human": 사람의 결정이 필요하다 (사양과 어긋나는 가정, 다른 기능의 연결 지점을 바꿈, 사양이 서로 모순됨, 작업 문서대로 하면 규칙을 어기게 됨).
확신이 없으면 pass 로 하지 않는다. 다만 취향 차이나 작업 범위 밖의 개선을 이유로 fix 를 주지 않는다. 구현한 에이전트가 보고서에 지적이 틀린 이유를 적었으면 그 근거를 확인하고 다시 판단한다.

완료 조건 목록: {expected}
criteria 에 위 목록의 id와 condition을 그대로 쓰고, 모든 조건을 정확히 한 번씩 포함한다.
[컴파일]은 루프가 판정, [사람 확인]은 사람 확인으로 적는다. "루프가 판정"은 [컴파일] 조건과 "기존 테스트/전체 테스트 … 통과" 조건에만 쓴다. 그 밖의 조건은 있음/약함/빠짐 중 하나로 적는다. test에는 근거가 되는 테스트 이름이나 판정 방법을 쓴다.
fix/human 지적 또는 약함/빠짐 조건이 있으면 pass 로 하지 않는다.

마지막 메시지는 아래 JSON 하나만 쓴다 (설명이나 코드 블록 표시 없이). 글은 한국어로 쓴다.
{{"verdict": "pass" | "fix" | "human",
  "summary": "한두 문장",
  "criteria": [{{"id": 1, "condition": "목록의 condition 원문", "test": "테스트 클래스.메서드 (없으면 -)", "verdict": "있음" | "약함" | "빠짐" | "루프가 판정" | "사람 확인"}}],
  "findings": [{{"severity": "fix" | "human" | "note", "text": "무엇이 문제인지와 위치 (파일:줄)"}}],
  "question": "verdict 가 human 일 때 사람에게 할 질문 (아니면 빈 문자열)",
  "recommendation": "verdict 가 human 일 때 추천안 (아니면 빈 문자열)"}}
"""


def _review_object(text: str) -> dict | None:
    """마지막 메시지에서 판정 JSON 객체 하나를 꺼낸다.

    코드 블록 표시나 앞뒤 설명 한두 줄은 너그럽게 넘기지만, 최상위 판정 객체가 둘 이상이면
    어느 것이 판정인지 알 수 없으므로 받지 않는다.
    """
    if not isinstance(text, str):
        return None
    body = text.strip()
    fenced = re.fullmatch(r"```[A-Za-z]*\s*\n?(.*?)\n?\s*```", body, re.DOTALL)
    if fenced:
        body = fenced.group(1).strip()
    try:
        whole = json.loads(body)
        return whole if isinstance(whole, dict) else None
    except ValueError:
        pass
    decoder = json.JSONDecoder()
    found: list[dict] = []
    i = body.find("{")
    while i != -1:
        try:
            obj, end = decoder.raw_decode(body, i)
        except ValueError:
            i = body.find("{", i + 1)
            continue
        if isinstance(obj, dict) and "verdict" in obj:
            found.append(obj)
        i = body.find("{", end)  # 꺼낸 객체 안쪽(criteria 등)은 다시 보지 않는다
    return found[0] if len(found) == 1 else None


def _same_condition(a: str, b: str) -> bool:
    """완료 조건 원문 비교. 리뷰어가 옮겨 적으며 바꾸기 쉬운 표시(백틱·따옴표·공백·끝 문장부호)는 무시한다."""
    def norm(s: str) -> str:
        s = re.sub(r"[`\"'“”‘’*]", "", s)
        s = re.sub(r"\s+", " ", s).strip()
        return s.rstrip(" .。")
    return norm(a) == norm(b)


def _loop_judged(mark: str, condition: str) -> bool:
    """리뷰어가 "루프가 판정"으로 적을 수 있는 조건: [컴파일], 또는 기존/전체 테스트 통과 조건."""
    return mark == "컴파일" or (("기존 테스트" in condition or "전체 테스트" in condition) and "통과" in condition)


def parse_review(text: str, expected_conditions: list[tuple[str, str]] | None = None) -> dict | None:
    """완결된 판정 하나만 받는다. 빠진 근거·모순된 판정은 재시도 대상으로 돌린다."""
    found = _review_object(text)
    if not isinstance(found, dict) or found.get("verdict") not in ("pass", "fix", "human"):
        return None
    if any(not isinstance(found.get(k), str) for k in ("summary", "question", "recommendation")):
        return None
    if not found["summary"].strip():
        return None
    criteria, findings = found.get("criteria"), found.get("findings")
    if not isinstance(criteria, list) or not criteria or not isinstance(findings, list):
        return None
    seen = set()
    weak = False
    for c in criteria:
        if not isinstance(c, dict) or type(c.get("id")) is not int or c["id"] < 1 or c["id"] in seen:
            return None
        seen.add(c["id"])
        if any(not isinstance(c.get(k), str) or not c[k].strip() for k in ("condition", "test", "verdict")):
            return None
        if c["verdict"] not in ("있음", "약함", "빠짐", "루프가 판정", "사람 확인"):
            return None
        if c["verdict"] == "있음" and c["test"].strip() in ("-", "없음"):
            return None
        if expected_conditions is not None:
            if c["id"] > len(expected_conditions):
                return None
            mark, condition = expected_conditions[c["id"] - 1]
            if not _same_condition(c["condition"], condition):
                return None
            c["condition"] = condition  # 증거에는 작업 문서의 원문을 싣는다
            if mark == "사람 확인" and c["verdict"] != "사람 확인":
                return None
            if mark != "사람 확인" and c["verdict"] == "사람 확인":
                return None
            if c["verdict"] == "루프가 판정" and not _loop_judged(mark, condition):
                return None
            if mark == "컴파일" and c["verdict"] != "루프가 판정":
                return None
        weak |= c["verdict"] in ("약함", "빠짐")
    if expected_conditions is not None and seen != set(range(1, len(expected_conditions) + 1)):
        return None
    severities = set()
    for f in findings:
        if not isinstance(f, dict) or f.get("severity") not in ("fix", "human", "note"):
            return None
        if not isinstance(f.get("text"), str) or not f["text"].strip():
            return None
        severities.add(f["severity"])
    verdict = found["verdict"]
    if verdict == "pass" and (weak or severities & {"fix", "human"}):
        return None
    if "human" in severities and verdict != "human":
        return None
    if verdict == "fix" and not (weak or "fix" in severities):
        return None
    if verdict == "human" and not (found["question"].strip() and found["recommendation"].strip()):
        return None
    return {k: found[k] for k in ("verdict", "summary", "criteria", "findings", "question", "recommendation")}


def handoff_note(prev_agent: str) -> str:
    return f"""참고: 이 작업은 이전 에이전트({prev_agent})가 사용량 한도로 중단한 것을 이어받은 것이다.
작업 폴더에 이전 에이전트가 하던 변경이 커밋 없이 남아 있을 수 있고, 작업 문서에 보고서가 일부 적혀 있을 수 있다.
git status 와 git diff 로 남은 변경을 확인하고 이어서 완성해라."""


def _limit_hit(cfg: dict, agent: str, text: str) -> bool:
    patterns = cfg.get("usage_limit_patterns", {}).get(agent, [])
    return any(re.search(p, text or "", re.IGNORECASE) for p in patterns)


def _is_transient(cfg: dict, text: str) -> bool:
    return any(re.search(p, text or "", re.IGNORECASE) for p in cfg.get("transient_error_patterns", []))


def _kill_tree(proc: subprocess.Popen) -> None:
    """자식과 그 하위 프로세스까지 끝낸다 (에이전트가 띄운 프로세스가 남아 worktree 를 계속 고치지 않게)."""
    try:
        if os.name == "nt":
            subprocess.run(["taskkill", "/PID", str(proc.pid), "/T", "/F"], capture_output=True, timeout=30)
        else:
            os.killpg(proc.pid, signal.SIGKILL)
    except Exception:
        pass
    try:
        proc.kill()
    except Exception:
        pass


def _run_proc(
    cmd: list[str] | str, input_text: str | None, timeout_sec: float, shell: bool = False, env: dict | None = None
) -> tuple[int | None, str, str]:
    """worktree 에서 실행하고 (종료 코드, stdout, stderr) 를 돌려준다. 시간 초과면 종료 코드가 None."""
    proc = subprocess.Popen(
        cmd,
        cwd=WT,
        shell=shell,
        stdin=subprocess.PIPE if input_text is not None else subprocess.DEVNULL,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
        errors="replace",
        env=env,
        **CHILD_KW,
    )
    try:
        out, err = proc.communicate(input_text, timeout=timeout_sec)
        return proc.returncode, out or "", err or ""
    except subprocess.TimeoutExpired:
        _kill_tree(proc)
        try:
            out, err = proc.communicate(timeout=20)
        except Exception:
            out, err = "", ""
        return None, out or "", err or ""
    except BaseException:  # 즉시 중단(Ctrl+C 두 번) 포함
        _kill_tree(proc)
        raise


def _run_child(cmd: list[str], prompt: str, timeout_min: float, label: str) -> subprocess.CompletedProcess:
    """에이전트 CLI 실행. 시간 초과면 returncode 가 None."""
    env = os.environ.copy()
    for key in ("CLAUDECODE", "CLAUDE_CODE_ENTRYPOINT"):  # 다른 클로드 세션 안에서 실행할 때 대비
        env.pop(key, None)
    # 에이전트가 부르는 파이썬이 파일·표준 입출력을 UTF-8 로 다루게 한다 (윈도 기본은 cp949)
    env.setdefault("PYTHONUTF8", "1")
    env.setdefault("PYTHONIOENCODING", "utf-8")
    try:
        code, out, err = _run_proc(cmd, prompt, timeout_min * 60, env=env)
    except FileNotFoundError:
        raise LoopError(f"{label} CLI 를 찾을 수 없음: {cmd[0]}")
    return subprocess.CompletedProcess(cmd, code, out, err)


def _write_agent_log(log_name: str, proc: subprocess.CompletedProcess) -> None:
    LOG_DIR.mkdir(parents=True, exist_ok=True)
    code = "시간 초과" if proc.returncode is None else proc.returncode
    (LOG_DIR / f"{log_name}.out.txt").write_text(
        f"$ exit {code}\n--- stdout ---\n{proc.stdout}\n--- stderr ---\n{proc.stderr}", encoding="utf-8"
    )


def _rule_path(p: Path) -> str:
    """클로드 권한 규칙의 절대 경로 표기: C:\\Users\\a → //c/Users/a"""
    s = p.resolve().as_posix()
    if re.match(r"^[A-Za-z]:/", s):
        s = "/" + s[0].lower() + s[2:]
    return "/" + s


def claude_path_rules() -> list[str]:
    """사용자 폴더와 그 .env 를 실제 경로 기준으로 막는 금지 규칙."""
    root = _rule_path(ROOT)
    return [f"Read({root}/.env)", f"Edit({root}/.env)", f"Edit({root}/**)"]


def write_claude_settings() -> Path:
    """검사 훅(claude_guard.py)을 등록한 설정 파일을 만들고 경로를 돌려준다.

    훅은 worktree 가 아니라 사용자 폴더의 사본을 쓴다 (에이전트가 worktree 에서 훅을 고쳐 무력화하지 못하게).
    """
    guard = TOOL_DIR / "claude_guard.py"
    if not guard.exists():
        raise LoopError(f"검사 훅 {guard} 가 없음")
    settings = {
        "hooks": {
            "PreToolUse": [
                {
                    "matcher": "*",
                    "hooks": [
                        {
                            "type": "command",
                            "command": sys.executable,
                            "args": [
                                str(guard),
                                "--worktree", str(WT),
                                "--root", str(ROOT),
                                "--log", str(LOG_DIR / "guard.log"),
                            ],
                            "timeout": 30,
                            "onFailure": "block",
                        }
                    ],
                }
            ]
        }
    }  # fmt: skip
    LOOP_DIR.mkdir(exist_ok=True)
    path = LOOP_DIR / "claude_settings.json"
    path.write_text(json.dumps(settings, ensure_ascii=False, indent=2), encoding="utf-8")
    return path


def run_claude(cfg: dict, prompt: str, session_id: str | None, log_name: str, review: bool = False) -> dict:
    """review=True 면 리뷰어로 실행한다: 파일을 고치는 도구를 뺀 허용 목록을 쓴다."""
    exe = shutil.which(cfg["claude_command"]) or cfg["claude_command"]
    cmd = [exe, "-p", "--output-format", "json", "--permission-mode", cfg["claude_permission_mode"]]
    cmd += ["--settings", str(write_claude_settings())]
    if cfg.get("claude_model"):  # 비우면 CLI 기본 모델
        cmd += ["--model", cfg["claude_model"]]
    if cfg.get("claude_effort"):
        cmd += ["--effort", cfg["claude_effort"]]
    allowed = cfg.get("claude_review_allowed_tools") if review else cfg.get("claude_allowed_tools")
    if allowed:
        cmd += ["--allowedTools", *allowed]
    cmd += ["--disallowedTools", *cfg.get("claude_disallowed_tools", []), *claude_path_rules()]
    if session_id:
        cmd += ["--resume", session_id]

    timeout_min = cfg.get("review_timeout_min", 30) if review else cfg["claude_timeout_min"]
    proc = _run_child(cmd, prompt, timeout_min, "클로드")
    _write_agent_log(log_name, proc)
    if proc.returncode is None:
        raise AgentFailure("claude", f"실행 시간 초과 ({timeout_min}분)")

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
    failed = proc.returncode != 0 or bool(data.get("is_error")) or not data
    scan = "\n".join([result_text, proc.stdout[-3000:], proc.stderr[-3000:]]) if failed else result_text
    if _limit_hit(cfg, "claude", scan):
        raise UsageLimit("claude", tail(result_text or proc.stderr or proc.stdout, 500))
    denials = data.get("permission_denials")
    if isinstance(denials, list) and denials:
        names = ", ".join(str(d.get("tool_name", "?")) if isinstance(d, dict) else "?" for d in denials[:10])
        log(f"클로드 권한 거부 {len(denials)}건 ({names}). 자세한 내용: .loop/logs/{log_name}.out.txt, .loop/logs/guard.log")
    if failed:
        detail = tail(result_text or proc.stderr or proc.stdout, 800)
        raise AgentFailure("claude", f"실행 실패 (코드 {proc.returncode}): {detail}", _is_transient(cfg, detail))
    return data


def run_codex(cfg: dict, prompt: str, log_name: str, review: bool = False) -> dict:
    """review=True 면 리뷰어로 실행한다: 읽기 전용 샌드박스를 쓴다."""
    exe = shutil.which(cfg["codex_command"]) or cfg["codex_command"]
    LOG_DIR.mkdir(parents=True, exist_ok=True)
    last_msg_path = LOG_DIR / f"{log_name}.codex_last.txt"
    last_msg_path.unlink(missing_ok=True)
    model_args: list[str] = []  # 비우면 ~/.codex/config.toml 값
    if cfg.get("codex_model"):
        model_args += ["-m", cfg["codex_model"]]
    if cfg.get("codex_effort"):
        model_args += ["-c", f"model_reasoning_effort={cfg['codex_effort']}"]
    cmd = [
        exe, "exec",
        "-s", cfg.get("codex_review_sandbox", "read-only") if review else cfg["codex_sandbox"],
        "-c", "approval_policy=never",
        "-C", str(WT),
        "--json", "--color", "never",
        "-o", str(last_msg_path),
        *model_args,
        *cfg.get("codex_extra_args", []),
        "-",
    ]  # fmt: skip
    timeout_min = cfg.get("review_timeout_min", 30) if review else cfg["codex_timeout_min"]
    proc = _run_child(cmd, prompt, timeout_min, "코덱스")
    _write_agent_log(log_name, proc)
    if proc.returncode is None:
        raise AgentFailure("codex", f"실행 시간 초과 ({timeout_min}분)")

    last_msg = last_msg_path.read_text(encoding="utf-8", errors="replace") if last_msg_path.exists() else ""
    turn_failed = False
    error_texts: list[str] = []
    for line in proc.stdout.splitlines():
        try:
            event = json.loads(line)
        except json.JSONDecodeError:
            continue
        if not isinstance(event, dict):
            continue
        etype = str(event.get("type", ""))
        if etype == "turn.failed":
            turn_failed = True
        if etype in ("error", "turn.failed"):
            error_texts.append(json.dumps(event, ensure_ascii=False))

    failed = proc.returncode != 0 or turn_failed
    errors = "\n".join(error_texts)
    scan = "\n".join([last_msg, errors, proc.stderr[-3000:], proc.stdout[-3000:]]) if failed else last_msg
    if _limit_hit(cfg, "codex", scan):
        raise UsageLimit("codex", tail(errors or proc.stderr or last_msg, 500))
    if failed:
        detail = tail(errors or proc.stderr or last_msg, 800)
        raise AgentFailure("codex", f"실행 실패 (코드 {proc.returncode}): {detail}", _is_transient(cfg, detail))
    return {"result": last_msg}


_HANGUL_RE = re.compile(r"[가-힣]")


def garbled_text(text: str) -> bool:
    """한글이 '?' 나 대체 문자로 깨진 글인지 어림한다.
    윈도 PowerShell 5.1 은 파이프로 넘기는 글을 ASCII 로 바꿔 한글을 '?' 로 만든다 (023·024 보류에서 실제로 일어났다).
    대체 문자(U+FFFD)가 있거나, 한글이 하나도 없는데 '??' 가 이어 나오고 '?' 가 4개 이상이면 깨진 것으로 본다."""
    if not text:
        return False
    if "�" in text:
        return True
    return not _HANGUL_RE.search(text) and "??" in text and text.count("?") >= 4


def garbled_output(result: dict, doc: Path) -> list[str]:
    """결과 파일의 글과 이번 시도에 에이전트가 덧붙인 절 중 한글이 깨진 곳의 이름.
    마지막 보류 사유·답변 뒤의 절(루프 기록 제외)만 본다. 지난 기록이 깨져 있어도 이번 시도 탓이 아니다.
    제목까지 깨진 절('## ???')도 여기에 들어간다."""
    bad = [f".loop/result.json 의 {k}" for k, v in result.items() if isinstance(v, str) and garbled_text(v)]
    text = doc.read_text(encoding="utf-8-sig")
    tail_text = text[len(doc_head(text)) :]
    recent: list[str] = []
    for m in re.finditer(r"^## .*$(?:\n(?!## ).*)*", tail_text, re.M):
        section = m.group(0)
        title = section.splitlines()[0][3:].strip()
        if title.startswith(("보류 사유", "답변")):
            recent = []
        elif not title.startswith("루프 기록"):
            recent.append(section)
    bad += [f"작업 문서의 '{s.splitlines()[0].strip()}' 절" for s in recent if garbled_text(s)]
    return bad


def garbled_feedback(places: list[str]) -> str:
    listing = "\n".join(f"- {p}" for p in places)
    return f"""아래에 쓴 한글이 '?' 로 깨졌다. 이 상태로는 사람이 읽을 수 없어 받아들이지 않는다.

{listing}

윈도 PowerShell 에서 한글을 파이프(`@'...'@ | python -` 등)나 명령줄 인자로 다른 프로그램에 넘기면 '?' 로 바뀐다.
파일을 직접 편집하는 도구(apply_patch 등)로 UTF-8 그대로 다시 써라. 깨진 절은 지우고 새로 쓴다.
줄바꿈은 실제 줄바꿈으로 쓴다 (`\\n` 두 글자를 쓰면 제목이 인식되지 않아 보고서가 지워진다)."""


def read_result() -> dict | None:
    path = result_path()
    if not path.exists():
        return None
    try:
        data = json.loads(path.read_text(encoding="utf-8-sig"))
    except json.JSONDecodeError:
        return None
    return data if isinstance(data, dict) and data.get("status") in ("done", "blocked") else None


def test_dir() -> Path:
    return WT / ".loop" / "test"


def read_test_summary() -> dict:
    """테스트 래퍼가 남긴 합계 (.loop/test/summary.json). 없으면 빈 값."""
    try:
        data = json.loads((test_dir() / "summary.json").read_text(encoding="utf-8"))
        return data if isinstance(data, dict) else {}
    except (OSError, ValueError):
        return {}


def run_tests(cfg: dict, final: bool = False) -> tuple[bool, str]:
    """final=True 면 루프를 합치기 직전의 전체 테스트다 (final_test_command. 화면 갈무리도 남긴다)."""
    command = (cfg.get("final_test_command") if final else "") or cfg.get("test_command") or ""
    command = command.strip()
    (test_dir() / "summary.json").unlink(missing_ok=True)
    if not command:
        return True, "테스트 명령이 비어 있어 건너뜀"
    # {python}: 이 스크립트를 실행한 파이썬, {loop_tools}: 사용자 폴더의 tools/loop.
    # 테스트 도구는 worktree 가 아니라 사용자 폴더의 사본을 쓴다 (에이전트가 worktree 에서 고쳐 통과시키지 못하게).
    command = command.replace("{python}", f'"{sys.executable}"').replace("{loop_tools}", str(TOOL_DIR))
    code, out, err = _run_proc(command, None, cfg["test_timeout_min"] * 60, shell=True)
    if code is None:
        return False, f"테스트 시간 초과 ({cfg['test_timeout_min']}분)\n{tail(out + err, 3000)}"
    output = f"명령: {command}\n종료 코드: {code}\n{out}\n{err}"
    if code == 2:  # 테스트를 실행할 수 없는 환경 문제. 에이전트가 고칠 수 없으므로 재시도하지 않고 멈춘다
        raise LoopError(f"테스트를 실행할 수 없음 (환경 문제, 종료 코드 2):\n{tail(out + err, 1500)}")
    return code == 0, output


def format_agents(entries: list[tuple[str, int | None]]) -> str:
    """[(agent, 시도 번호 또는 None=한도 초과)] → 'claude(한도 초과로 전환), codex(시도 1~2)'"""
    groups: list[list] = []  # [agent, first, last] 또는 [agent, None, None]
    for agent, n in entries:
        if n is None:
            groups.append([agent, None, None])
        elif groups and groups[-1][0] == agent and groups[-1][1] is not None:
            groups[-1][2] = n
        else:
            groups.append([agent, n, n])
    parts = []
    for agent, first, last in groups:
        if first is None:
            parts.append(f"{agent}(한도 초과로 전환)")
        else:
            parts.append(f"{agent}(시도 {first})" if first == last else f"{agent}(시도 {first}~{last})")
    return ", ".join(parts) or "(없음)"


# ───────────────────────── 루프 본체 ─────────────────────────


class Loop:
    def __init__(self, cfg: dict, notifier: Notifier, watch: bool):
        self.cfg = cfg
        self.n = notifier
        self.watch = watch
        self.state: dict = {}
        self.loop_branch = ""
        self.bundle = ""
        self.limited_at: dict[str, float] = {}  # 에이전트 → 한도에 걸린 시각

    @property
    def main(self) -> str:
        """사람이 일하는 브랜치. 스크립트는 읽기만 한다."""
        return self.cfg["base_branch"]

    @property
    def base(self) -> str:
        """루프 브랜치를 따고 합치는 기준: 통합 브랜치. 설정이 비어 있으면 main 과 같다."""
        return self.cfg.get("integration_branch") or self.cfg["base_branch"]

    @property
    def auto_merge(self) -> bool:
        return bool(self.cfg.get("auto_merge", True))

    @property
    def loop_id(self) -> str:
        return self.loop_branch[len(self.cfg["loop_branch_prefix"]) :]

    def doc(self, path: Path) -> Doc:
        return Doc(path, self.cfg["default_bundle"])

    # ── 에이전트 선택 ──
    def current_agent(self) -> str | None:
        for agent in self.cfg["agents"]:
            hit = self.limited_at.get(agent)
            if hit is None:
                return agent
            # 감시 모드에서는 일정 시간이 지나면 다시 먼저 시도한다. 한 번 실행에서는 끝까지 건너뛴다
            if self.watch and time.time() - hit >= self.cfg["limit_retry_after_min"] * 60:
                del self.limited_at[agent]
                log(f"{agent} 한도 대기 시간이 지나 다시 시도함")
                return agent
        return None

    # ── 시작 전 점검 ──
    def ensure_worktree(self) -> None:
        listed = [
            line[len("worktree ") :].strip()
            for line in git("worktree", "list", "--porcelain").stdout.splitlines()
            if line.startswith("worktree ")
        ]
        if _norm(WT) == _norm(ROOT):
            raise PreflightError("worktree_path 가 사용자 폴더와 같음. 별도 폴더로 정해 주세요")
        if WT.exists():
            if _norm(WT) not in {_norm(p) for p in listed}:
                raise PreflightError(f"{WT} 가 이미 있지만 이 저장소의 worktree 가 아님. 확인 후 지우거나 worktree_path 를 바꿔 주세요")
            return
        git("worktree", "prune")
        git("worktree", "add", "--detach", str(WT), self.main)
        log(f"루프 worktree 생성: {WT}")

    def preflight(self) -> None:
        if git("rev-parse", "--is-inside-work-tree", check=False).returncode != 0:
            raise PreflightError("깃 저장소가 아님")
        if not branch_exists(self.main):
            raise PreflightError(f"기준 브랜치 {self.main} 가 없음")
        if self.auto_merge and self.base == self.main:
            raise PreflightError(
                f"auto_merge 가 켜져 있는데 통합 브랜치가 {self.main} 과 같음. {self.main} 은 사용자 폴더에 체크아웃되어 있어 "
                "스크립트가 합칠 수 없습니다. integration_branch 를 정하거나 auto_merge 를 끄세요"
            )
        self.ensure_worktree()
        self.state = load_state()
        active = self.state.get("loop_branch") if self.state.get("status") == "active" else None
        if active and not branch_exists(active):
            raise PreflightError(f"상태 파일의 진행 중인 루프 브랜치 {active} 가 없음 (.loop/state.json 확인)")
        dirty = wt_dirty()
        if dirty:
            self.recover_dirty(dirty, active)
        if self.state.pop("last_stop", None) is not None:
            save_state(self.state)  # 멈춘 이유는 바로 다음 실행의 복구에만 쓴다
        self.sync_integration(moving=not active)
        unmerged = [
            b for b in local_branches(self.cfg["loop_branch_prefix"]) if b != active and not is_ancestor(b, self.base)
        ]
        if unmerged:
            raise PreflightError(
                f"끝났지만 {self.base} 에 합쳐지지 않은 루프 브랜치가 있음. 확인 후 직접 합치거나 정리해 주세요:\n"
                + "\n".join(f"- {b}" for b in unmerged)
            )
        self.loop_branch = active or ""
        self.bundle = self.state.get("bundle", self.cfg["default_bundle"]) if active else ""

    def sync_integration(self, moving: bool) -> None:
        """통합 브랜치가 없으면 main 에서 만들고, main 에 새 커밋(사양 문서 등)이 있으면 통합 브랜치에 합친다.

        루프가 진행 중일 때(moving=False)는 만들기만 하고 합치지 않는다. 기준이 루프 도중에 움직이지 않게 한다.
        """
        integ, main = self.base, self.main
        if integ == main:
            return
        if not branch_exists(integ):
            git("branch", integ, main)
            log(f"통합 브랜치 생성: {integ} ({main} 에서)")
            return
        if not moving or is_ancestor(main, integ):
            return
        if wgit("checkout", integ, check=False).returncode != 0:
            raise PreflightError(
                f"통합 브랜치 {integ} 를 루프 worktree 에서 체크아웃할 수 없음. 다른 폴더(사용자 폴더 등)에서 "
                f"{integ} 를 체크아웃하고 있으면 {main} 으로 돌아가 주세요"
            )
        merged = wgit("merge", "--no-edit", main, check=False)
        if merged.returncode != 0:
            wgit("merge", "--abort", check=False)
            wgit("checkout", "--detach")
            raise PreflightError(
                f"{main} 의 새 커밋을 통합 브랜치 {integ} 에 합치다 충돌함. 직접 풀어 주세요 "
                f"(루프 worktree 에서 git checkout {integ} → git merge {main}):\n{truncate(merged.stdout + merged.stderr, 800)}"
            )
        wgit("checkout", "--detach")
        log(f"{main} 의 새 커밋을 통합 브랜치 {integ} 에 합침")

    def recover_dirty(self, dirty: str, active: str | None) -> None:
        """이전 실행이 중간에 멈춰 worktree 에 남은 변경을 기능 브랜치에 작업 중 커밋으로 넣는다.

        진행 중인 루프의 기능 브랜치(doing 문서 또는 상태 파일의 처리 중 작업)에 남은 변경만 복구한다.
        그 밖의 변경은 출처를 알 수 없으므로 시작하지 않는다.
        """
        branch = wt_branch()
        current = self.state.get("current") or {}
        doing = {task_branch_name(self.cfg, p.name): p.name for p in task_docs(DOING)}
        known = branch in doing or branch == current.get("branch")
        if not (active and branch.startswith(self.cfg["task_branch_prefix"]) and known):
            raise PreflightError(
                f"루프 worktree({WT})에 커밋 안 된 변경이 있음 (현재 브랜치 {branch}, 자동 복구 대상이 아님):\n"
                f"{truncate(dirty, 1500)}"
            )
        stop = self.state.get("last_stop") or {}
        reason = stop.get("reason") or "기록 없음 (루프 프로세스가 강제 종료된 것으로 추정)"
        when = stop.get("at") or "알 수 없음"
        doc_name = doing.get(branch) or current.get("doc") or branch
        wt_commit_all(
            f"중단 복구(작업 중 상태): {Path(doc_name).stem}\n\n"
            f"이전 실행이 중간에 멈춰 worktree 에 남은 변경을 그대로 커밋함.\n멈춘 이유: {reason}\n멈춘 시각: {when}\n"
        )
        self.state.pop("current", None)
        save_state(self.state)
        log(f"중단 복구: {doc_name} 의 남은 변경을 {branch} 에 작업 중 커밋으로 넣음 (멈춘 이유: {reason})")
        self.n.send(
            "LOG",
            f"중단 복구: {doc_name}",
            f"이전 실행이 중간에 멈춰 worktree 에 남은 변경을 `{branch}` 에 작업 중 커밋으로 넣고 이어서 진행합니다.\n\n"
            f"**멈춘 이유**\n{truncate(reason, 1000)}\n\n**멈춘 시각**: {when}\n\n**남아 있던 변경**\n{truncate(dirty, 1000)}",
        )

    # ── 선행 작업 판단 ──
    def unmet(self, doc: Doc, ref: str) -> tuple[list[str], list[str]]:
        """(미완료 선행 작업 '이름(폴더)', 어디에도 없는 선행 작업)

        완료 판정은 ref(통합 브랜치, 루프 안에서는 루프 브랜치)의 docs/history 기록뿐이다.
        tasks/done 에 있어도 합쳐지지 않은 작업은 끝난 것으로 보지 않는다.
        """
        pending, missing = [], []
        for dep in doc.deps:
            if file_in_branch(ref, f"{HISTORY_REL}/{dep}.md"):
                continue
            folder = find_doc(dep)
            if folder:
                pending.append(f"{dep}({'done, 아직 합쳐지지 않음' if folder == 'done' else folder})")
            else:
                missing.append(dep)
        return pending, missing

    def triage(self, ref: str) -> None:
        """순환은 blocked 로, 선행 작업이 안 끝난 todo 문서는 wait 로 옮긴다."""
        graph: dict[str, list[str]] = {}
        for folder in (TODO, WAIT, BLOCKED, DOING):
            for p in task_docs(folder):
                graph[p.stem] = self.doc(p).deps
        for name, path in cycle_members(graph).items():
            folder = find_doc(name)
            if folder not in ("todo", "wait"):
                continue
            src = ALL_FOLDERS[folder] / f"{name}.md"
            route = " → ".join(path)
            append_section(
                src,
                "보류 사유",
                f"- 사유: 선행 작업이 서로를 가리키는 순환이 있음 ({route})\n"
                f"- 질문: 순환을 어떻게 풀까요?\n- 추천안: 순환에 걸린 문서 중 하나의 선행 작업 칸을 고친 뒤 todo 로 옮겨 주세요",
            )
            move_doc(src, BLOCKED)
            log(f"순환 → blocked: {name} ({route})")
            self.n.send("BLOCKED", f"보류: {name}.md (선행 작업 순환)", f"**막힌 이유**\n선행 작업 순환: {route}\n\n**추천안**\n한 문서의 선행 작업 칸을 고친 뒤 todo 로 옮겨 주세요")

        for p in task_docs(TODO):
            doc = self.doc(p)
            pending, missing = self.unmet(doc, ref)
            if not pending and not missing:
                continue
            move_doc(p, WAIT)
            if missing:
                log(f"대기(선행 작업 없음): {p.name} ← {', '.join(missing)}")
                body = f"**선행 작업이 아직 없음**\n{', '.join(missing)}\n\ntasks 어디에도 없고 기록도 없습니다. 선행 작업 문서를 넣거나 선행 작업 칸을 고쳐 주세요."
                if pending:
                    body += f"\n\n그 밖에 기다리는 선행 작업: {', '.join(pending)}"
                self.n.send("BLOCKED", f"대기: {p.name} (선행 작업이 아직 없음)", body)
            else:
                log(f"대기: {p.name} ← {', '.join(pending)}")
                self.n.send("LOG", f"대기로 이동: {p.name}", f"기다리는 선행 작업: {', '.join(pending)}")

    def promote(self, ref: str, bundle: str | None, trigger: str | None = None) -> None:
        """선행 작업이 모두 끝난 wait 문서를 todo 로 올린다."""
        for p in task_docs(WAIT):
            doc = self.doc(p)
            if bundle is not None and doc.bundle != bundle:
                continue
            if trigger is not None and trigger not in doc.deps:
                continue
            pending, missing = self.unmet(doc, ref)
            if pending or missing:
                continue
            move_doc(p, TODO)
            log(f"대기 해제 → todo: {p.name}")
            self.n.send("LOG", f"대기 해제: {p.name}", f"선행 작업이 모두 끝나 todo 로 올림 ({', '.join(doc.deps)})\n기준: `{ref}`")

    def wait_summary(self, ref: str) -> str:
        lines = []
        for p in task_docs(WAIT):
            pending, missing = self.unmet(self.doc(p), ref)
            why = ", ".join(pending + [f"{m}(없음)" for m in missing]) or "(선행 작업 완료, 다음 확인 때 올라감)"
            lines.append(f"- {p.name} ← {why}")
        return "\n".join(lines)

    # ── 루프 브랜치 준비 ──
    def open_loop(self) -> bool:
        for folder in ALL_FOLDERS.values():
            folder.mkdir(parents=True, exist_ok=True)
        if self.loop_branch:
            wgit("checkout", self.loop_branch)
            log(f"진행 중인 루프 이어서 실행: {self.loop_branch} (묶음 {self.bundle})")
            return True

        self.promote(self.base, bundle=None)
        self.triage(self.base)
        todo = task_docs(TODO)
        if not todo and not task_docs(DOING):
            waiting = self.wait_summary(self.base)
            log("꺼낼 작업이 없음. 루프를 만들지 않음")
            if waiting:
                self.n.send_once("idle", waiting, "LOG", "꺼낼 작업 없음 (대기 중)", f"**대기 중인 작업**\n{waiting}")
            self.notify_all_done(waiting)
            return False
        self.n.reset("idle")

        first = self.doc(todo[0]) if todo else self.doc(task_docs(DOING)[0])
        self.bundle = first.bundle
        run_id = f"{_dt.datetime.now():%Y%m%d-%H%M%S}"
        self.loop_branch = self.cfg["loop_branch_prefix"] + run_id
        git("branch", self.loop_branch, self.base)
        wgit("checkout", self.loop_branch)
        self.state = {
            "loop_branch": self.loop_branch,
            "status": "active",
            "bundle": self.bundle,
            "started_at": _dt.datetime.now().isoformat(timespec="seconds"),
            "processed": [],
        }
        save_state(self.state)
        targets = [p.name for p in todo if self.doc(p).bundle == self.bundle]
        log(f"새 루프 시작: {self.loop_branch} (묶음 {self.bundle})")
        self.n.send(
            "LOG",
            f"루프 시작: 묶음 {self.bundle}",
            f"루프 브랜치: `{self.loop_branch}`\n이번 묶음 대상: {', '.join(targets) or '(없음)'}",
        )
        return True

    def notify_all_done(self, waiting: str) -> None:
        """꺼낼 작업이 없고 통합 브랜치가 main 보다 앞서 있으면, 사람이 확인할 차례라고 한 번 알린다."""
        if self.base == self.main or not branch_exists(self.base):
            return
        ahead = git("rev-list", "--count", f"{self.main}..{self.base}").stdout.strip()
        if ahead in ("", "0"):
            return
        merges = git("log", "--first-parent", "--merges", "--format=- %s", f"{self.main}..{self.base}").stdout.strip()
        blocked = "\n".join(f"- {p.name}" for p in task_docs(BLOCKED))
        tip = git("rev-parse", self.base).stdout.strip()
        body = (
            f"todo 가 비었습니다. 통합 브랜치 `{self.base}` 가 `{self.main}` 보다 커밋 {ahead}개 앞서 있습니다.\n\n"
            f"**합쳐진 루프**\n{merges or '- (루프 병합 없음)'}\n\n"
            f"**확인**: `git show {self.base}:{loop_evidence.EVIDENCE_REL}/index.md` (루프별 증거 색인)\n"
            f"**올리기**: {self.main} 에서 `git merge --ff-only {self.base}` → 확인 후 푸시"
        )
        if blocked:
            body += f"\n\n**보류 (답이 필요함)**\n{blocked}"
        if waiting:
            body += f"\n\n**대기 중**\n{waiting}"
        log(f"꺼낼 작업 없음. {self.base} 가 {self.main} 보다 커밋 {ahead}개 앞섬 (사람이 확인할 차례)")
        self.n.send_once("all-done", f"{tip}|{blocked}", "DONE", "모든 작업 끝: 확인할 차례", body)

    def mark_processed(self, name: str, branch: str, agents: str = "", extra: dict | None = None) -> None:
        self.state.setdefault("processed", []).append({"doc": name, "branch": branch, "agents": agents, **(extra or {})})
        save_state(self.state)

    def ff_merge_into_loop(self, task_branch: str) -> None:
        wgit("checkout", self.loop_branch)
        wgit("merge", "--ff-only", task_branch)

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
            clear_wt_doc(doc.name)

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
        wgit("checkout", self.loop_branch)
        if not branch_exists(tb):
            wgit("checkout", "-b", tb, self.loop_branch)
            return tb

        wgit("checkout", tb)
        merge = wgit("merge", "--no-edit", self.loop_branch, check=False)
        if merge.returncode == 0:
            log(f"기존 기능 브랜치 {tb} 에 루프 브랜치를 합쳐 최신으로 맞춤")
            return tb

        wgit("merge", "--abort", check=False)
        wgit("checkout", self.loop_branch)
        wgit("branch", "-D", tb)
        wgit("checkout", "-b", tb, self.loop_branch)
        log(f"{tb} 에 루프 브랜치를 합치다 충돌 → 기존 브랜치를 지우고 루프 브랜치에서 새로 시작")
        self.n.send("LOG", f"기능 브랜치 새로 시작: {doc_name}", f"`{tb}` 에서 루프 브랜치 병합 충돌 → 기존 브랜치 삭제 후 `{self.loop_branch}` 에서 새로 만듦")
        return tb

    def process(self, todo_doc: Path) -> str:
        """작업 하나를 처리하고 'done' 또는 'blocked' 를 돌려준다."""
        doc = move_doc(todo_doc, DOING)
        name = doc.name
        log(f"작업 시작: {name}")
        tb = self.prepare_task_branch(name)
        self.state["current"] = {"doc": name, "branch": tb}
        save_state(self.state)
        has_wip = wgit("rev-list", "--count", f"{self.loop_branch}..{tb}").stdout.strip() not in ("", "0")
        agent = self.current_agent()
        self.n.send("LOG", f"작업 시작: {name}", f"에이전트: {agent}\n기능 브랜치: `{tb}`\n루프 브랜치: `{self.loop_branch}`")

        max_retries = int(self.cfg["max_retries"])
        max_fix_rounds = int(self.cfg.get("review_max_fix_rounds", 2))
        fix_rounds = 0
        # 사람이 승인한 부분(양식 칸)과 거기 적힌 관문 예외. 에이전트가 고쳐도 이것을 기준으로 한다
        original_head = doc_head(doc.read_text(encoding="utf-8-sig"))
        exceptions = loop_gate.doc_exceptions(original_head)
        entries: list[tuple[str, int | None]] = []
        session_id: str | None = None
        feedback = ""
        last_failure = ""
        handoff_from: str | None = None
        just_switched = False
        attempt = 0
        stem = Path(name).stem

        while attempt < max_retries + 1:  # 처음 1회 + 재시도 max_retries 회
            agent = self.current_agent()
            if agent is None:
                self.stop_for_usage_limit(doc, tb, entries)
                raise AllAgentsLimited()
            attempt += 1
            if attempt > 1 and not just_switched:
                log(f"재시도 {attempt - 1}/{max_retries} ({agent}): {name}")
                self.n.send("LOG", f"재시도 {attempt - 1}/{max_retries}회째: {name}", f"에이전트: {agent}\n{truncate(last_failure, 1500)}")
            just_switched = False

            sync_to_wt(doc)
            result_path().unlink(missing_ok=True)
            if agent == "claude" and session_id:
                prompt = retry_prompt(feedback)
            else:
                prompt = first_prompt(name, tb)
                if has_wip:
                    prompt += "\n\n" + wip_note(self.loop_branch)
                if handoff_from:
                    prompt += "\n\n" + handoff_note(handoff_from)
                if feedback:
                    prompt += "\n\n" + retry_prompt(feedback)

            head_before = wgit("rev-parse", "HEAD").stdout.strip()
            root_before = root_snapshot()
            try:
                if agent == "claude":
                    data = run_claude(self.cfg, prompt, session_id, f"{stem}_attempt{attempt}_claude")
                    session_id = data.get("session_id") or session_id
                elif agent == "codex":
                    run_codex(self.cfg, prompt, f"{stem}_attempt{attempt}_codex")
                else:
                    raise LoopError(f"알 수 없는 에이전트: {agent}")
            except UsageLimit as e:
                self.check_root(root_before, agent, name)
                self.undo_agent_commits(head_before, tb, agent, name)
                attempt -= 1  # 한도로 끊긴 실행은 재시도 횟수에 넣지 않는다
                self.limited_at[agent] = time.time()
                entries.append((agent, None))
                if wt_doc_path(name).exists():
                    self.pull_doc(doc, original_head)
                nxt = self.current_agent()
                if nxt is None:
                    self.stop_for_usage_limit(doc, tb, entries, e.detail)
                    raise AllAgentsLimited()
                log(f"{agent} 사용량 한도 → {nxt} 로 전환: {name}")
                self.n.send("LOG", f"에이전트 전환: {agent} → {nxt}", f"작업: {name}\n{agent} 사용량 한도 초과로 {nxt} 가 이어서 진행합니다.\n\n{truncate(e.detail, 500)}")
                handoff_from = agent
                session_id = None
                just_switched = True
                continue
            except AgentFailure as e:
                self.check_root(root_before, agent, name)
                self.undo_agent_commits(head_before, tb, agent, name)
                if wt_doc_path(name).exists():
                    self.pull_doc(doc, original_head)
                if e.transient:  # 일시적 오류는 시도 횟수에 넣지 않고, 정리한 뒤 나중에 다시 시도한다
                    self.park(doc, tb, "일시적 오류로 중단", f"{agent} {e.detail}")
                    raise TransientError(f"{agent} {e.detail}")
                entries.append((agent, attempt))
                log(f"{agent} {e.detail.splitlines()[0]} → 실패한 시도로 처리: {name}")
                last_failure = f"{agent} {e.detail}"
                feedback = interrupted_feedback(agent, e.detail)
                session_id = None  # 끊긴 세션은 이어 쓰지 않고 새로 시작한다
                continue

            self.check_root(root_before, agent, name)  # 사본을 되돌려 복사하기 전에 비교한다
            entries.append((agent, attempt))
            self.pull_doc(doc, original_head)
            self.undo_agent_commits(head_before, tb, agent, name)

            result = read_result()
            if result is None:
                last_failure = ".loop/result.json 이 없거나 형식이 맞지 않음"
                feedback = last_failure + ". 형식에 맞게 다시 써라."
                continue
            garbled = garbled_output(result, doc)
            if garbled:
                log(f"한글 깨짐 → 다시 시도: {name} ({', '.join(garbled)})")
                last_failure = "한글이 깨짐: " + ", ".join(garbled)
                feedback = garbled_feedback(garbled)
                continue
            if result["status"] == "blocked":
                self.block(doc, tb, result.get("reason", ""), result.get("question", ""), result.get("recommendation", ""), entries)
                return "blocked"

            ok, test_output = run_tests(self.cfg)
            log(f"테스트: {'통과' if ok else '실패'} ({test_output.splitlines()[0] if test_output else ''})")
            if not ok:
                last_failure = "테스트 실패\n" + tail(test_output, 1500)
                feedback = "테스트가 실패했다.\n" + tail(test_output, 6000)
                continue
            tests = read_test_summary()

            # 관문: 스크립트가 하는 규칙 검사. 작업 중 상태를 스냅숏 커밋으로 만들어 루프 브랜치와 비교한다
            snap, tree = self.snapshot()
            doc_text = doc.read_text(encoding="utf-8-sig")
            gate = loop_gate.check(WT, self.loop_branch, snap, mode="task", doc_text=doc_text, exceptions=exceptions)
            if gate["violations"]:
                listing = loop_gate.format_violations(gate)
                log(f"관문: 위반 {len(gate['violations'])}건 → 다시 시도: {name}\n{truncate(listing, 1500)}")
                last_failure = "관문 검사 위반\n" + truncate(listing, 1500)
                feedback = gate_feedback(truncate(listing, 5000), self.loop_branch)
                continue
            log(f"관문: 위반 없음 (확인 필요 {len(gate['warnings'])}건, 새 테스트 {len(gate['stats']['new_tests'])}개)")

            # 교차 리뷰: 구현하지 않은 쪽 에이전트가 읽기만 하며 확인한다
            review = self.review(doc, tb, snap, tree, gate, entries, stem, attempt)
            if review is None:
                self.stop_for_usage_limit(doc, tb, entries, "리뷰할 에이전트가 모두 사용량 한도에 걸림")
                raise AllAgentsLimited()
            review["fix_rounds"] = fix_rounds
            who = f"{review.get('reviewer', '-')}{'' if review.get('cross', True) else ' (같은 에이전트)'}"
            log(f"리뷰: {review['verdict']} ({who}) {truncate(review.get('summary', ''), 300)}")
            problems = "\n".join(f"- [{f['severity']}] {f['text']}" for f in review.get("findings", []) if f["severity"] != "note")
            if review["verdict"] == "fix":
                fix_rounds += 1
                if fix_rounds > max_fix_rounds:
                    self.block(
                        doc, tb,
                        f"교차 리뷰({who})를 {max_fix_rounds}번 고친 뒤에도 통과하지 못함.\n리뷰 요약: {review.get('summary', '')}\n{problems}",
                        "리뷰 지적이 맞는지 확인하고, 맞으면 작업 문서를 고치거나 어떻게 고칠지 알려 주세요. 틀렸으면 그대로 진행하라고 답해 주세요.",
                        "지적이 사소하면 '리뷰 지적은 무시하고 진행'이라고 답한다. 답변은 리뷰어에게도 전달된다",
                        entries,
                    )  # fmt: skip
                    return "blocked"
                last_failure = f"교차 리뷰({who})가 고칠 것을 찾음 ({fix_rounds}/{max_fix_rounds})\n{truncate(problems, 1500)}"
                feedback = review_feedback(review)
                continue
            if review["verdict"] == "human":
                self.block(
                    doc, tb,
                    f"교차 리뷰({who})가 사람의 결정이 필요하다고 판정함.\n리뷰 요약: {review.get('summary', '')}\n{problems}",
                    review.get("question") or "위 지적을 어떻게 처리할까요?",
                    review.get("recommendation") or "지적을 확인하고 방향을 정해 답한다",
                    entries,
                )  # fmt: skip
                return "blocked"
            if review["verdict"] != "pass":
                self.block(
                    doc, tb,
                    f"교차 리뷰를 실행하지 못함: {review.get('summary', '')}",
                    "리뷰 에이전트가 왜 실패했는지 확인해 주세요 (.loop/logs 의 *_review*_ 로그).",
                    "원인을 해결한 뒤 todo 로 옮긴다. 리뷰 없이 진행하려면 loop_config.local.json 에 review_enabled: false",
                    entries,
                )  # fmt: skip
                return "blocked"

            evidence = {
                "summary": result.get("summary", ""),
                "review": review,
                "tests": tests,
                "new_tests": gate["stats"]["new_tests"],
                "screen_files": gate["stats"]["screen_files"],
                "human_conditions": gate["stats"]["human_conditions"],
                "gate_warnings": gate["warnings"],
                "assumptions": loop_gate.report_items(doc_text, "가정"),
                "human_report": loop_gate.report_items(doc_text, "사람이 확인할 것"),
                "exceptions": sorted(exceptions),
            }
            self.complete(doc, tb, result.get("summary", ""), entries, evidence)
            return "done"

        self.block(
            doc,
            tb,
            f"재시도 한도({max_retries}회) 초과. 마지막 실패:\n{tail(last_failure, 1500)}",
            "실패 원인을 확인하고 작업 문서나 완료 조건을 고쳐 주세요.",
            "마지막 실패 내용과 기능 브랜치의 작업 중 상태를 보고 판단",
            entries,
        )
        return "blocked"

    def pull_doc(self, doc: Path, original_head: str) -> None:
        """worktree 의 작업 문서 사본을 사용자 폴더로 되돌려 복사한다. 양식 칸이 바뀌었으면 원래대로 되돌린다."""
        sync_from_wt(doc)
        if restore_doc_head(doc, original_head):
            sync_to_wt(doc)
            log(f"에이전트가 작업 문서의 양식 칸을 고침 → 원래대로 되돌림: {doc.name}")
            self.n.send("LOG", f"작업 문서 되돌림: {doc.name}", "에이전트가 작업 문서의 양식 칸(요구사항, 완료 조건 등)을 고쳐서 원래 내용으로 되돌렸습니다. 보고서는 그대로 둡니다.")

    def snapshot(self) -> tuple[str, str]:
        """작업 트리의 지금 상태를 브랜치를 움직이지 않는 스냅숏 커밋으로 만든다. (커밋, 트리)"""
        wgit("add", "-A")
        tree = wgit("write-tree").stdout.strip()
        commit = wgit("commit-tree", tree, "-p", "HEAD", "-m", "관문 스냅숏 (브랜치에 들어가지 않음)").stdout.strip()
        return commit, tree

    def review(self, doc: Path, tb: str, snap: str, tree: str, gate: dict, entries: list, stem: str, attempt: int) -> dict | None:
        """교차 리뷰를 실행하고 판정을 돌려준다. 쓸 수 있는 에이전트가 없으면 None.

        리뷰어는 구현한 에이전트가 아닌 쪽을 먼저 고른다. 리뷰어가 작업 폴더를 고쳤으면 스냅숏으로 되돌린다.
        """
        if not self.cfg.get("review_enabled", True):
            return {"verdict": "pass", "skipped": True, "summary": "", "criteria": [], "findings": []}
        implementer = next((a for a, n in reversed(entries) if n is not None), "")
        expected_conditions = loop_gate.conditions(doc.read_text(encoding="utf-8-sig"))
        prompt = review_prompt(doc.name, self.loop_branch, snap, gate["warnings"], implementer or "?", expected_conditions)
        failures, last_error = 0, ""
        while failures < 2:
            self.current_agent()  # 감시 모드에서 한도 대기 시간이 지난 에이전트를 다시 쓸 수 있게 한다
            available = [a for a in self.cfg["agents"] if a not in self.limited_at]
            if not available:
                return None
            reviewer = next((a for a in available if a != implementer), available[0])
            log_name = f"{stem}_attempt{attempt}_review{failures + 1}_{reviewer}"
            log(f"리뷰 시작 ({reviewer}{'' if reviewer != implementer else ', 같은 에이전트'}): {doc.name}")
            head_before = wgit("rev-parse", "HEAD").stdout.strip()
            root_before = root_snapshot()
            error: Exception | None = None
            data: dict = {}
            try:
                if reviewer == "claude":
                    data = run_claude(self.cfg, prompt, None, log_name, review=True)
                elif reviewer == "codex":
                    data = run_codex(self.cfg, prompt, log_name, review=True)
                else:
                    raise LoopError(f"알 수 없는 에이전트: {reviewer}")
            except (UsageLimit, AgentFailure) as e:
                error = e
            self.check_root(root_before, reviewer, doc.name)
            self.undo_agent_commits(head_before, tb, reviewer, doc.name)
            wgit("add", "-A")
            if wgit("write-tree").stdout.strip() != tree:
                wgit("read-tree", "--reset", "-u", tree)
                log(f"리뷰어({reviewer})가 작업 폴더를 고침 → 리뷰 전 상태로 되돌림: {doc.name}")
                self.n.send("LOG", f"리뷰어 변경 되돌림: {doc.name}", f"{reviewer} 가 리뷰 중에 작업 폴더를 고쳐서 리뷰 전 상태로 되돌렸습니다.")
            if isinstance(error, UsageLimit):
                self.limited_at[reviewer] = time.time()
                log(f"{reviewer} 사용량 한도 (리뷰) → 다른 에이전트로 리뷰: {doc.name}")
                continue
            if isinstance(error, AgentFailure):
                if error.transient:
                    self.park(doc, tb, "일시적 오류로 중단", f"{reviewer}(리뷰) {error.detail}")
                    raise TransientError(f"{reviewer}(리뷰) {error.detail}")
                failures += 1
                last_error = f"{reviewer} {error.detail}"
                log(f"리뷰 실패 ({failures}/2): {last_error.splitlines()[0]}")
                continue
            parsed = parse_review(str(data.get("result", "")), expected_conditions)
            if parsed is None:
                failures += 1
                last_error = f"{reviewer} 의 판정 JSON 이 불완전하거나 완료 조건 근거·판정이 모순됨"
                log(f"리뷰 실패 ({failures}/2): {last_error}")
                continue
            parsed["reviewer"] = reviewer
            parsed["cross"] = reviewer != implementer
            return parsed
        return {"verdict": "error", "summary": last_error, "criteria": [], "findings": []}

    def check_root(self, before: dict, agent: str, name: str) -> None:
        """에이전트 실행 중 사용자 폴더의 tasks·.env 가 바뀌었으면 경고한다 (검사 훅을 비켜 간 쓰기 감지)."""
        changes = snapshot_changes(before, root_snapshot())
        if not changes:
            return
        listing = "\n".join(f"- {c}" for c in changes[:30])
        log(f"경고: {agent} 실행 중 사용자 폴더가 바뀜 ({name}):\n{listing}")
        self.n.send(
            "STOPPED",
            f"경고: 에이전트 실행 중 사용자 폴더가 바뀜 ({name})",
            f"{agent} 가 실행되는 동안 아래 파일이 바뀌었습니다. 직접 바꾼 것이 아니면 에이전트가 worktree 밖에 쓴 것입니다.\n"
            f"루프는 계속 진행합니다.\n\n{listing}",
        )

    def undo_agent_commits(self, head_before: str, tb: str, agent: str, name: str) -> None:
        """에이전트가 규칙을 어기고 브랜치를 바꾸거나 커밋했으면 되돌린다 (변경 내용은 작업 트리에 남긴다)."""
        if wt_branch() != tb:
            raise LoopError(f"{agent} 실행 뒤 브랜치가 {tb} 가 아님 (현재 {wt_branch()})")
        head_after = wgit("rev-parse", "HEAD").stdout.strip()
        if head_after == head_before:
            return
        wgit("reset", "--soft", head_before)
        log(f"{agent} 가 직접 커밋함 → 커밋만 되돌리고 변경은 남김: {name}")
        self.n.send("LOG", f"에이전트 커밋 되돌림: {name}", f"{agent} 가 규칙을 어기고 커밋해서 `{head_before[:7]}` 로 되돌렸습니다 (변경 내용은 유지).")

    def complete(self, doc: Path, tb: str, summary: str, entries: list, evidence: dict | None = None) -> None:
        agents = format_agents(entries)
        review = (evidence or {}).get("review") or {}
        if review.get("skipped") or not review:
            review_line = "리뷰 안 함"
        else:
            review_line = f"{review.get('reviewer')} ({'교차' if review.get('cross') else '같은 에이전트'}) 통과"
            review_line += f", 고침 {review['fix_rounds']}회" if review.get("fix_rounds") else ""
        append_section(
            doc, "루프 기록",
            f"- 작업 에이전트: {agents}\n- 리뷰: {review_line}\n- 기능 브랜치: {tb}\n- 루프 브랜치: {self.loop_branch}",
        )  # fmt: skip
        history_dir = WT / HISTORY_REL
        history_dir.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(doc, history_dir / doc.name)
        clear_wt_doc(doc.name)
        message = f"작업 완료: {doc.stem}\n\n{summary.strip()}\n\n작업 에이전트: {agents}\n작업 문서: {HISTORY_REL}/{doc.name}\n"
        wt_commit_all(message)  # 실패하면 LoopError → done 으로 옮기지 않음
        if not file_in_branch(tb, f"{HISTORY_REL}/{doc.name}"):
            raise LoopError(f"{tb} 에 기록 커밋이 확인되지 않음")
        self.ff_merge_into_loop(tb)
        move_doc(doc, DONE)
        self.state.pop("current", None)
        self.mark_processed(doc.name, tb, agents, evidence)
        log(f"작업 완료: {doc.name} ({agents}, 리뷰: {review_line})")
        self.n.send(
            "DONE",
            f"작업 완료: {doc.name}",
            f"**바뀐 내용**\n{truncate(summary, 2500)}\n\n**작업 에이전트**: {agents}\n**리뷰**: {review_line}\n"
            f"**기능 브랜치**: `{tb}`\n**루프 브랜치**: `{self.loop_branch}`",
        )

    def block(self, doc: Path, tb: str, reason: str, question: str, recommendation: str, entries: list) -> None:
        clear_wt_doc(doc.name)
        committed = wt_commit_all(f"보류(작업 중 상태): {doc.stem}\n\n{truncate(reason, 1000)}\n")
        append_section(
            doc,
            "보류 사유",
            f"- 사유: {reason}\n- 질문: {question}\n- 추천안: {recommendation}\n- 기능 브랜치: {tb}"
            + (" (작업 중 상태 커밋됨)" if committed else "")
            + f"\n- 작업 에이전트: {format_agents(entries)}",
        )
        moved = move_doc(doc, BLOCKED)
        wgit("checkout", self.loop_branch)
        self.state.pop("current", None)
        save_state(self.state)
        log(f"보류: {moved.name}")
        self.n.send(
            "BLOCKED",
            f"보류: {moved.name}",
            f"**막힌 이유**\n{truncate(reason, 1500)}\n\n**질문**\n{truncate(question, 1000)}\n\n"
            f"**추천안**\n{truncate(recommendation, 1000)}\n\n**기능 브랜치**: `{tb}`",
        )

    def park(self, doc: Path, tb: str, title: str, reason: str) -> None:
        """작업을 접는다: 작업 중 상태를 기능 브랜치에 커밋하고 문서를 todo 로 되돌린다. 다음 실행이 이어서 한다."""
        clear_wt_doc(doc.name)
        wt_commit_all(f"{title}(작업 중 상태): {doc.stem}\n\n멈춘 이유: {truncate(reason, 1000)}\n")
        move_doc(doc, TODO)
        wgit("checkout", self.loop_branch)
        self.state.pop("current", None)
        save_state(self.state)
        log(f"{title}: {doc.name} → todo 로 되돌림 (작업 중 상태는 {tb})")

    def stop_for_usage_limit(self, doc: Path, tb: str, entries: list, detail: str = "") -> None:
        self.park(doc, tb, "사용량 한도로 중단", detail or "모든 에이전트 사용량 한도 초과")
        log(f"모든 에이전트 사용량 한도 초과로 중단: {doc.name}")
        self.n.send(
            "STOPPED",
            "루프 멈춤: 사용량 한도 초과",
            f"에이전트 {', '.join(self.cfg['agents'])} 모두 한도에 걸림 ({format_agents(entries)}).\n"
            f"작업 `{doc.name}` 은 todo 로 되돌림. 작업 중 상태는 `{tb}` 에 커밋됨.\n"
            f"루프 브랜치 `{self.loop_branch}` 는 그대로 둠. "
            + (f"감시 모드: {self.cfg['limit_retry_after_min']}분 뒤 다시 시도합니다." if self.watch else "한도가 풀리면 다시 실행하세요.")
            + f"\n\n{truncate(detail, 800)}",
        )

    # ── 루프 끝 ──
    def bundle_docs(self, *folders: Path) -> list[Path]:
        return [p for f in folders for p in task_docs(f) if self.doc(p).bundle == self.bundle]

    def finish(self) -> bool:
        """이번 묶음의 todo 가 비었으면 루프를 닫는다: 검증 → 증거 → 통합 브랜치에 합침. 닫았으면 True.

        보류(blocked)가 남아 있어도 닫는다. 보류 작업과 그것을 기다리는 작업은 답을 받은 뒤 다음 루프에서 처리한다.
        """
        if self.bundle_docs(TODO, DOING):
            return False  # 종료 요청으로 중간에 멈춤. 루프는 진행 중으로 남는다
        processed = self.state.get("processed", [])
        blocked = [d.name for d in self.bundle_docs(BLOCKED)]
        if not processed:
            return self.close_empty(blocked)
        if not self.auto_merge:
            return self.finish_manual(blocked)

        tag = self.cfg.get("merge_tag_prefix", "loop-merged/") + self.loop_id
        report_rel = f"{loop_evidence.EVIDENCE_REL}/{self.loop_id}/report.md"
        if is_ancestor(self.loop_branch, self.base):  # 이전 실행이 합친 직후에 끊김. 뒷정리만 한다
            wgit("checkout", "--detach")
            return self.after_merge(tag, report_rel, processed, blocked)

        wgit("checkout", self.loop_branch)
        log(f"루프 검증 시작: {self.loop_branch} (작업 {len(processed)}건)")
        exceptions = {e for p in processed for e in p.get("exceptions", [])}
        gate = loop_gate.check(ROOT, self.base, self.loop_branch, mode="loop", exceptions=exceptions)
        if gate["violations"]:
            return self.reject("관문 검사 위반", loop_gate.format_violations(gate), processed)

        ok, output = run_tests(self.cfg, final=True)
        log(f"합치기 전 전체 테스트: {'통과' if ok else '실패'}")
        if not ok:
            return self.reject("합치기 전 전체 테스트 실패", tail(output, 3000), processed)
        tests = read_test_summary()
        shots = sorted((test_dir() / "screenshots").glob("*.png"))
        wgit("add", "-A")  # 줄 끝만 바뀐 파일은 여기서 걸러진다
        discarded = wgit("diff", "--cached", "--name-status", "HEAD").stdout.strip()
        wgit("reset", "--hard")
        wgit("clean", "-fd")
        if discarded:  # 테스트 실행이 만든 변경(유니티가 저절로 고친 설정 등)은 합치지 않는다
            log(f"합치기 전 테스트 실행이 만든 변경을 버림:\n{truncate(discarded, 800)}")

        loop_evidence.write(
            WT, loop_id=self.loop_id, loop_branch=self.loop_branch, bundle=self.bundle, base=self.base, main=self.main,
            tag=tag, tag_prefix=self.cfg.get("merge_tag_prefix", "loop-merged/"), tasks=processed, gate=gate,
            test_summary=tests, screenshots=shots, discarded=discarded, blocked=blocked,
        )  # fmt: skip
        wt_commit_all(f"증거: {self.loop_branch}\n\n{report_rel}\n")

        names = "\n".join(f"- {Path(p['doc']).stem}" for p in processed)
        wgit("checkout", self.base)
        merged = wgit(
            "merge", "--no-ff", "-m", f"루프 합침: {self.loop_branch} (묶음 {self.bundle})",
            "-m", f"작업 {len(processed)}건\n{names}\n\n증거: {report_rel}", self.loop_branch, check=False,
        )  # fmt: skip
        if merged.returncode != 0:
            wgit("merge", "--abort", check=False)
            wgit("checkout", self.loop_branch)
            raise LoopError(f"{self.loop_branch} 를 {self.base} 에 합치지 못함:\n{truncate(merged.stdout + merged.stderr, 1000)}")
        git("tag", "-f", tag, wgit("rev-parse", "HEAD").stdout.strip())
        wgit("checkout", "--detach")
        return self.after_merge(tag, report_rel, processed, blocked)

    def after_merge(self, tag: str, report_rel: str, processed: list, blocked: list[str]) -> bool:
        self.state.update(
            status="finished", finished_at=_dt.datetime.now().isoformat(timespec="seconds"),
            merged_into=self.base, tag=tag, evidence=report_rel,
        )  # fmt: skip
        self.state.pop("current", None)
        save_state(self.state)
        for branch in [self.loop_branch] + [p.get("branch", "") for p in processed]:
            if branch and branch_exists(branch) and is_ancestor(branch, self.base):
                git("branch", "-D", branch)  # 합쳐진 브랜치는 지운다. 병합 커밋은 태그가 가리킨다
        lines = "\n".join(f"- {p['doc']} ({p.get('agents') or '-'})" for p in processed) or "- (없음)"
        extra = f"\n\n**보류로 남은 작업 (답이 필요함)**\n" + "\n".join(f"- {b}" for b in blocked) if blocked else ""
        waiting = self.wait_summary(self.base)
        if waiting:
            extra += f"\n\n**대기 중(wait)**\n{waiting}"
        log(f"루프 합침: {self.loop_branch} → {self.base} (묶음 {self.bundle}, 태그 {tag})")
        self.n.send(
            "DONE",
            f"루프 합침: 묶음 {self.bundle}",
            f"**처리한 작업 {len(processed)}건**\n{lines}\n\n**통합 브랜치**: `{self.base}` (병합 커밋 태그 `{tag}`)\n"
            f"**증거**: `{report_rel}`\n관문·전체 테스트를 통과하고 설정된 리뷰 절차를 마쳐 합쳤습니다. {self.main} 에는 올리지 않았습니다.{extra}",
        )
        return True

    def reject(self, title: str, detail: str, processed: list) -> bool:
        """합치기 전 검증에서 떨어진 루프: 합치지 않고, 브랜치는 이름을 바꿔 남기고, 작업은 보류로 보낸다."""
        wgit("checkout", "--detach")
        kept = self.cfg.get("rejected_branch_prefix", "rejected/") + self.loop_id
        git("branch", "-M", self.loop_branch, kept)
        moved = []
        for p in processed:
            done_doc = DONE / p["doc"]
            if done_doc.exists():
                append_section(
                    done_doc,
                    "보류 사유",
                    f"- 사유: 이 작업이 들어 있던 루프({self.loop_branch})가 합치기 전 검증에서 떨어짐 — {title}\n"
                    f"- 질문: 원인을 확인하고, 이 작업을 다시 처리할지 알려 주세요.\n"
                    f"- 추천안: 아래 내용을 보고 작업 문서를 고친 뒤 todo 로 옮기면 처음부터 다시 처리합니다\n"
                    f"- 남은 브랜치: {kept} (이 루프의 변경 전체. 통합 브랜치에는 합쳐지지 않음)\n\n"
                    f"```\n{truncate(detail, 2500)}\n```",
                )
                moved.append(move_doc(done_doc, BLOCKED).name)
            branch = p.get("branch", "")
            if branch and branch_exists(branch):
                git("branch", "-D", branch)  # 변경은 남긴 브랜치에 들어 있다. 다시 처리할 때는 새로 시작한다
        self.state.update(
            status="rejected", finished_at=_dt.datetime.now().isoformat(timespec="seconds"), rejected_branch=kept, reject_reason=title
        )  # fmt: skip
        self.state.pop("current", None)
        save_state(self.state)
        log(f"루프를 합치지 않음: {self.loop_branch} → {kept} ({title})\n{truncate(detail, 1500)}")
        self.n.send(
            "BLOCKED",
            f"루프를 합치지 않음: 묶음 {self.bundle} ({title})",
            f"합치기 전 검증에서 떨어져 통합 브랜치에 합치지 않았습니다.\n\n```\n{truncate(detail, 2000)}\n```\n\n"
            f"**남긴 브랜치**: `{kept}`\n**보류로 옮긴 작업**\n" + ("\n".join(f"- {m}" for m in moved) or "- (없음)"),
        )
        return True

    def close_empty(self, blocked: list[str]) -> bool:
        """완료된 작업이 하나도 없는 루프: 합칠 것이 없으므로 브랜치만 지우고 닫는다."""
        wgit("checkout", "--detach")
        if is_ancestor(self.loop_branch, self.base):
            git("branch", "-D", self.loop_branch)
        self.state.update(status="finished", finished_at=_dt.datetime.now().isoformat(timespec="seconds"), merged_into=None)
        self.state.pop("current", None)
        save_state(self.state)
        listing = "\n".join(f"- {b}" for b in blocked) or "- (없음)"
        log(f"루프 닫음 (완료된 작업 없음, 합칠 것 없음): {self.loop_branch}, 보류 {len(blocked)}건")
        self.n.send("LOG", f"루프 닫음: 묶음 {self.bundle} (합칠 것 없음)", f"완료된 작업이 없습니다.\n\n**보류**\n{listing}")
        return True

    def finish_manual(self, blocked: list[str]) -> bool:
        """auto_merge 가 꺼져 있을 때: 합치지 않고 사람이 확인하길 기다린다 (다음 루프는 합친 뒤에 시작한다)."""
        self.state["status"] = "finished"
        self.state["finished_at"] = _dt.datetime.now().isoformat(timespec="seconds")
        save_state(self.state)
        wgit("checkout", "--detach")  # 사용자가 합친 뒤 루프 브랜치를 지울 수 있게 worktree 를 떼어 둔다
        done = self.state.get("processed", [])
        lines = "\n".join(f"- {p['doc']} (`{p['branch']}`, {p.get('agents') or '-'})" for p in done) or "- (없음)"
        extra = f"\n\n**보류**\n" + "\n".join(f"- {b}" for b in blocked) if blocked else ""
        log(f"루프 종료 (자동 합치기 꺼짐): {self.loop_branch} (묶음 {self.bundle})")
        self.n.send(
            "DONE",
            f"루프 종료: 묶음 {self.bundle}",
            f"**처리한 작업 {len(done)}건**\n{lines}\n\n**확인할 루프 브랜치**: `{self.loop_branch}`\n"
            f"확인 후 {self.main} 에 직접 합쳐 주세요. 합쳐야 다음 루프가 시작됩니다.{extra}",
        )
        return False

    def run_pass(self) -> bool:
        """루프 하나를 진행한다. 루프를 닫았으면(합침·합치지 않음·합칠 것 없음) True: 바로 다음 묶음을 볼 수 있다."""
        self.preflight()
        if not self.open_loop():
            return False
        self.reconcile()
        self.promote(self.loop_branch, bundle=self.bundle)
        self.triage(self.loop_branch)
        while not STOP.requested:
            todo = self.bundle_docs(TODO)
            if not todo:
                break
            name = todo[0].stem
            if self.process(todo[0]) == "done":
                self.promote(self.loop_branch, bundle=self.bundle, trigger=name)
            self.triage(self.loop_branch)
        return self.finish()


def _sleep(seconds: int) -> None:
    end = time.time() + seconds
    while not STOP.requested and time.time() < end:
        time.sleep(min(1.0, end - time.time()))


def main() -> int:
    global _log_file
    parser = argparse.ArgumentParser(description="매화검 키우기 AI 자동 개발 루프")
    parser.add_argument("--watch", action="store_true", help="끝나지 않고 일정 간격으로 todo 를 확인")
    parser.add_argument(
        "--first",
        choices=["claude", "codex"],
        help="이번 실행에서 구현을 먼저 맡길 에이전트. 나머지는 설정(agents)의 순서를 따른다. 빼면 설정 순서 그대로",
    )
    args = parser.parse_args()

    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass
    signal.signal(signal.SIGINT, _on_signal)
    if hasattr(signal, "SIGBREAK"):
        signal.signal(signal.SIGBREAK, _on_signal)

    LOG_DIR.mkdir(parents=True, exist_ok=True)
    _log_file = LOG_DIR / f"run_{_dt.datetime.now():%Y%m%d-%H%M%S}.log"
    mode = "watch" if args.watch else "once"
    log(f"실행 시작 (PID {os.getpid()}, 모드 {mode})")
    code = 1
    try:
        owner = claim_running(mode)
        if owner:
            note = "" if owner.get("cmdline_known") else " (명령줄은 확인하지 못함. 그 루프가 아니라면 .loop/running.json 을 지우고 다시 실행)"
            log(
                f"이 프로젝트에서 루프가 이미 실행 중이라 시작하지 않음: PID {owner.get('pid')}, 모드 {owner.get('mode')}, "
                f"시작 {owner.get('started_at')}, 로그 {owner.get('log_file')}{note}"
            )
            code = 4
        else:
            try:
                code = _run(args)
            finally:
                release_running()
        return code
    except BaseException:
        code = 1
        raise
    finally:
        log(f"실행 끝 (종료 코드 {code})")


def _run(args: argparse.Namespace) -> int:
    global WT
    notifier = Notifier(load_env(ROOT / ".env"))
    loop: Loop | None = None
    try:
        cfg = load_config()
        if args.first:
            cfg["agents"] = [args.first] + [a for a in cfg["agents"] if a != args.first]
        log(f"에이전트 순서: {', '.join(cfg['agents'])}")
        WT = (ROOT / cfg["worktree_path"]).resolve()
        loop = Loop(cfg, notifier, args.watch)
        if not args.watch:
            passes = 0
            while loop.run_pass() and not STOP.requested:
                passes += 1
                if passes >= int(cfg.get("max_loops_per_run", 50)):
                    log(f"한 번 실행에서 닫은 루프가 {passes}개에 이르러 멈춤 (max_loops_per_run)")
                    break
            if STOP.requested:
                notifier.send("LOG", "루프 종료 (요청)", "종료 요청에 따라 하던 작업까지 끝내고 멈췄습니다.")
            return 0

        interval = int(cfg["watch_interval_sec"])
        log(f"감시 모드 시작: {interval}초마다 확인 (worktree {WT})")
        notifier.send("LOG", "감시 모드 시작", f"{interval}초마다 todo 를 확인합니다.")
        max_repeats = int(cfg.get("transient_max_repeats", 3))
        retry_wait = int(cfg.get("transient_retry_wait_sec", 300))
        last_sig, repeats = "", 0
        while not STOP.requested:
            try:
                closed = loop.run_pass()
                notifier.reset("preflight")
                last_sig, repeats = "", 0
                if closed:
                    continue  # 루프 하나를 닫았으면 기다리지 않고 다음 묶음을 본다
            except AllAgentsLimited:
                wait_min = int(cfg["limit_retry_after_min"])
                log(f"모든 에이전트 사용량 한도. {wait_min}분 뒤 다시 시도")
                _sleep(wait_min * 60)
                continue
            except TransientError as e:
                sig = re.sub(r"[0-9a-f]{8,}|\d+", "#", str(e).lower())[:200]  # 요청 번호 같은 숫자는 무시하고 비교
                repeats = repeats + 1 if sig == last_sig else 1
                last_sig = sig
                if repeats >= max_repeats:
                    log(f"같은 일시적 오류가 {repeats}회 연속 → 멈춤: {e}")
                    notifier.send(
                        "STOPPED",
                        "루프 멈춤: 같은 오류가 계속됨",
                        f"일시적 오류로 보고 다시 시도했지만 같은 오류가 {repeats}회 연속으로 났습니다.\n\n{truncate(str(e), 1500)}\n\n"
                        f"작업은 todo 로 되돌렸고 작업 중 상태는 기능 브랜치에 커밋했습니다. 원인을 해결한 뒤 다시 실행하세요.\n"
                        f"루프 브랜치: `{loop.loop_branch or '(없음)'}`",
                    )
                    return 1
                log(f"일시적 오류 ({repeats}/{max_repeats}), {retry_wait}초 뒤 다시 시도: {e}")
                notifier.send(
                    "LOG",
                    f"일시적 오류, 다시 시도 예정 ({repeats}/{max_repeats})",
                    f"{truncate(str(e), 1500)}\n\n{retry_wait}초 뒤 다시 시도합니다. 같은 오류가 {max_repeats}회 연속이면 멈춥니다.",
                )
                _sleep(retry_wait)
                continue
            except PreflightError as e:
                log(f"시작 전 점검 실패, 기다림: {e}")
                notifier.send_once(
                    "preflight", str(e), "STOPPED", "루프 대기: 시작 전 점검 실패",
                    f"{e}\n\n감시 모드: 조건이 풀리면 자동으로 이어서 진행합니다. (같은 내용은 다시 알리지 않음)",
                )  # fmt: skip
            _sleep(interval)
        log("감시 모드 종료 (요청)")
        notifier.send("LOG", "감시 모드 종료 (요청)", "종료 요청에 따라 하던 작업까지 끝내고 멈췄습니다.")
        return 0
    except PreflightError as e:
        log(f"시작 전 점검 실패: {e}")
        notifier.send("STOPPED", "루프 시작 안 함: 시작 전 점검 실패", str(e))
        return 3
    except AllAgentsLimited:
        return 2
    except TransientError as e:  # 한 번 돌기: 정리는 끝났으므로 다시 실행하면 이어진다
        log(f"일시적 오류로 멈춤: {e}")
        notifier.send(
            "STOPPED",
            "루프 멈춤: 일시적 오류",
            f"{truncate(str(e), 1500)}\n\n작업은 todo 로 되돌렸고 작업 중 상태는 기능 브랜치에 커밋했습니다. 다시 실행하면 이어서 진행합니다.",
        )
        return 1
    except KeyboardInterrupt:
        log("사용자가 즉시 중단함")
        record_stop("사용자 즉시 중단 (Ctrl+C 두 번)")
        notifier.send("STOPPED", "루프 멈춤: 사용자 즉시 중단", "상태는 그대로 둠. 다시 실행하면 남은 변경을 작업 중 커밋으로 넣고 이어서 진행")
        return 1
    except Exception as e:
        detail = traceback.format_exc()
        log(f"오류로 멈춤:\n{detail}")
        record_stop(f"오류 {type(e).__name__}: {e}")
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
