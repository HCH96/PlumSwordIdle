"""매화검 키우기 — AI 자동 개발 루프 (2차 버전)

tasks/todo 의 작업 문서를 선행 작업과 묶음 규칙에 따라 하나씩 꺼내, 루프 전용 git worktree 에서
클로드 코드 CLI(한도 초과 시 코덱스 CLI)로 처리한다. 사용법과 규칙은 docs/loop.md 참고.

실행:
  python tools/loop/run_loop.py           한 번 돌고 끝남
  python tools/loop/run_loop.py --watch   끝나지 않고 일정 간격으로 todo 를 확인

종료 코드: 0 정상, 1 오류로 멈춤, 2 사용량 한도로 멈춤, 3 시작 전 점검 실패
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
CONFIG_PATH = TOOL_DIR / "loop_config.json"
LOCAL_CONFIG_PATH = TOOL_DIR / "loop_config.local.json"
TASKS = ROOT / "tasks"
TODO, DOING, WAIT, BLOCKED, DONE = (TASKS / n for n in ("todo", "doing", "wait", "blocked", "done"))
ALL_FOLDERS = {"todo": TODO, "doing": DOING, "wait": WAIT, "blocked": BLOCKED, "done": DONE}
HISTORY_REL = "docs/history"
LOOP_DIR = ROOT / ".loop"
STATE_PATH = LOOP_DIR / "state.json"
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
    changes = []
    for key in sorted(set(before) | set(after)):
        if key not in before:
            if not key.startswith("tasks/todo/"):  # 사람이 실행 중에 새 작업 문서를 넣는 것은 정상
                changes.append(f"{key} (새로 생김)")
        elif key not in after:
            changes.append(f"{key} (사라짐)")
        elif before[key] != after[key]:
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
1. 프로젝트 규칙(CLAUDE.md, AGENTS.md — 같은 내용)을 따른다. docs/index.md 를 먼저 읽고, 작업 문서와 그 "참고 문서"를 읽는다.
2. 계획을 세운 뒤 구현한다. 완료 조건에 맞는 테스트가 필요하면 함께 작성한다.
3. 바뀐 사양·중요한 결정·새 연결 지점을 docs/ 공용 문서에 반영하고, 필요하면 docs/index.md 를 갱신한다.
4. 작업 문서(tasks/doing/{doc_name}) 맨 끝에 "## 보고서" 절을 추가한다: 한 일, 바뀐 파일, 가정, 테스트 결과.
5. 마지막으로 .loop/result.json 을 UTF-8 JSON 으로 쓴다.
   - 완료: {{"status": "done", "summary": "바뀐 내용 요약 (몇 줄)"}}
   - 스스로 판단할 수 없어 진행 불가: {{"status": "blocked", "reason": "막힌 이유", "question": "사람에게 할 질문", "recommendation": "추천안"}}
   애매한 점은 규칙의 "애매한 점 처리"대로 먼저 스스로 정하고, 정말 불가능할 때만 blocked 로 한다.

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
git log {loop_branch}..HEAD 와 git diff {loop_branch}...HEAD 로 이미 된 부분을 확인하고 이어서 완성해라."""


def interrupted_feedback(agent: str, detail: str) -> str:
    return f"""이전 시도({agent})가 끝나지 못하고 중간에 끊겼다: {detail}
작업 폴더에 하던 변경이 커밋 없이 남아 있을 수 있다. git status 와 git diff 로 확인하고 이어서 완성해라.
같은 이유로 다시 끊기지 않게, 오래 걸리는 명령은 피하거나 나눠서 실행해라."""


def retry_prompt(reason: str) -> str:
    return f"""이전 시도가 완료 조건을 통과하지 못했다.

{reason}

원인을 고친 뒤 작업 문서의 "## 보고서" 절을 갱신하고, .loop/result.json 을 다시 써라.
규칙과 "하지 말 것"은 처음 지시와 같다."""


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


def run_claude(cfg: dict, prompt: str, session_id: str | None, log_name: str) -> dict:
    exe = shutil.which(cfg["claude_command"]) or cfg["claude_command"]
    cmd = [exe, "-p", "--output-format", "json", "--permission-mode", cfg["claude_permission_mode"]]
    cmd += ["--settings", str(write_claude_settings())]
    if cfg.get("claude_allowed_tools"):
        cmd += ["--allowedTools", *cfg["claude_allowed_tools"]]
    cmd += ["--disallowedTools", *cfg.get("claude_disallowed_tools", []), *claude_path_rules()]
    if session_id:
        cmd += ["--resume", session_id]

    proc = _run_child(cmd, prompt, cfg["claude_timeout_min"], "클로드")
    _write_agent_log(log_name, proc)
    if proc.returncode is None:
        raise AgentFailure("claude", f"실행 시간 초과 ({cfg['claude_timeout_min']}분)")

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


def run_codex(cfg: dict, prompt: str, log_name: str) -> dict:
    exe = shutil.which(cfg["codex_command"]) or cfg["codex_command"]
    LOG_DIR.mkdir(parents=True, exist_ok=True)
    last_msg_path = LOG_DIR / f"{log_name}.codex_last.txt"
    last_msg_path.unlink(missing_ok=True)
    cmd = [
        exe, "exec",
        "-s", cfg["codex_sandbox"],
        "-c", "approval_policy=never",
        "-C", str(WT),
        "--json", "--color", "never",
        "-o", str(last_msg_path),
        *cfg.get("codex_extra_args", []),
        "-",
    ]  # fmt: skip
    proc = _run_child(cmd, prompt, cfg["codex_timeout_min"], "코덱스")
    _write_agent_log(log_name, proc)
    if proc.returncode is None:
        raise AgentFailure("codex", f"실행 시간 초과 ({cfg['codex_timeout_min']}분)")

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


def read_result() -> dict | None:
    path = result_path()
    if not path.exists():
        return None
    try:
        data = json.loads(path.read_text(encoding="utf-8-sig"))
    except json.JSONDecodeError:
        return None
    return data if isinstance(data, dict) and data.get("status") in ("done", "blocked") else None


def run_tests(cfg: dict) -> tuple[bool, str]:
    command = (cfg.get("test_command") or "").strip()
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
    def base(self) -> str:
        return self.cfg["base_branch"]

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
        git("worktree", "add", "--detach", str(WT), self.base)
        log(f"루프 worktree 생성: {WT}")

    def preflight(self) -> None:
        if git("rev-parse", "--is-inside-work-tree", check=False).returncode != 0:
            raise PreflightError("깃 저장소가 아님")
        if not branch_exists(self.base):
            raise PreflightError(f"기준 브랜치 {self.base} 가 없음")
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
        unmerged = [
            b for b in local_branches(self.cfg["loop_branch_prefix"]) if b != active and not is_ancestor(b, self.base)
        ]
        if unmerged:
            raise PreflightError(
                f"완료됐지만 {self.base} 에 합쳐지지 않은 루프 브랜치가 있음. 확인 후 직접 합치거나 정리해 주세요:\n"
                + "\n".join(f"- {b}" for b in unmerged)
            )
        self.loop_branch = active or ""
        self.bundle = self.state.get("bundle", self.cfg["default_bundle"]) if active else ""

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
    def unmet(self, doc: Doc, ref: str, use_done: bool) -> tuple[list[str], list[str]]:
        """(미완료 선행 작업 '이름(폴더)', 어디에도 없는 선행 작업)"""
        pending, missing = [], []
        for dep in doc.deps:
            if use_done and (DONE / f"{dep}.md").exists():
                continue
            if file_in_branch(ref, f"{HISTORY_REL}/{dep}.md"):
                continue
            folder = find_doc(dep)
            if folder:
                pending.append(f"{dep}({folder})")
            else:
                missing.append(dep)
        return pending, missing

    def triage(self, ref: str, use_done: bool) -> None:
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
            pending, missing = self.unmet(doc, ref, use_done)
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

    def promote(self, ref: str, use_done: bool, bundle: str | None, trigger: str | None = None) -> None:
        """선행 작업이 모두 끝난 wait 문서를 todo 로 올린다."""
        for p in task_docs(WAIT):
            doc = self.doc(p)
            if bundle is not None and doc.bundle != bundle:
                continue
            if trigger is not None and trigger not in doc.deps:
                continue
            pending, missing = self.unmet(doc, ref, use_done)
            if pending or missing:
                continue
            move_doc(p, TODO)
            log(f"대기 해제 → todo: {p.name}")
            self.n.send("LOG", f"대기 해제: {p.name}", f"선행 작업이 모두 끝나 todo 로 올림 ({', '.join(doc.deps)})\n기준: `{ref}`")

    def wait_summary(self, ref: str, use_done: bool) -> str:
        lines = []
        for p in task_docs(WAIT):
            pending, missing = self.unmet(self.doc(p), ref, use_done)
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

        self.promote(self.base, use_done=False, bundle=None)
        self.triage(self.base, use_done=False)
        todo = task_docs(TODO)
        if not todo and not task_docs(DOING):
            waiting = self.wait_summary(self.base, use_done=False)
            log("꺼낼 작업이 없음. 루프를 만들지 않음")
            if waiting:
                self.n.send_once("idle", waiting, "LOG", "꺼낼 작업 없음 (대기 중)", f"**대기 중인 작업**\n{waiting}")
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

    def mark_processed(self, name: str, branch: str, agents: str = "") -> None:
        self.state.setdefault("processed", []).append({"doc": name, "branch": branch, "agents": agents})
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
                    sync_from_wt(doc)
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
                    sync_from_wt(doc)
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
            sync_from_wt(doc)
            self.undo_agent_commits(head_before, tb, agent, name)

            result = read_result()
            if result is None:
                last_failure = ".loop/result.json 이 없거나 형식이 맞지 않음"
                feedback = last_failure + ". 형식에 맞게 다시 써라."
                continue
            if result["status"] == "blocked":
                self.block(doc, tb, result.get("reason", ""), result.get("question", ""), result.get("recommendation", ""), entries)
                return "blocked"

            ok, test_output = run_tests(self.cfg)
            log(f"테스트: {'통과' if ok else '실패'} ({test_output.splitlines()[0] if test_output else ''})")
            if ok:
                self.complete(doc, tb, result.get("summary", ""), entries)
                return "done"
            last_failure = "테스트 실패\n" + tail(test_output, 1500)
            feedback = "테스트가 실패했다.\n" + tail(test_output, 6000)

        self.block(
            doc,
            tb,
            f"재시도 한도({max_retries}회) 초과. 마지막 실패:\n{tail(last_failure, 1500)}",
            "실패 원인을 확인하고 작업 문서나 완료 조건을 고쳐 주세요.",
            "마지막 실패 내용과 기능 브랜치의 작업 중 상태를 보고 판단",
            entries,
        )
        return "blocked"

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

    def complete(self, doc: Path, tb: str, summary: str, entries: list) -> None:
        agents = format_agents(entries)
        append_section(doc, "루프 기록", f"- 작업 에이전트: {agents}\n- 기능 브랜치: {tb}\n- 루프 브랜치: {self.loop_branch}")
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
        self.mark_processed(doc.name, tb, agents)
        log(f"작업 완료: {doc.name} ({agents})")
        self.n.send(
            "DONE",
            f"작업 완료: {doc.name}",
            f"**바뀐 내용**\n{truncate(summary, 2500)}\n\n**작업 에이전트**: {agents}\n"
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
            f"루프 브랜치 `{self.loop_branch}` 는 그대로 둠. 한도가 풀리면 다시 실행하세요.\n\n{truncate(detail, 800)}",
        )

    # ── 루프 끝 ──
    def bundle_docs(self, *folders: Path) -> list[Path]:
        return [p for f in folders for p in task_docs(f) if self.doc(p).bundle == self.bundle]

    def finish(self) -> None:
        if self.bundle_docs(TODO, DOING):
            return  # 종료 요청으로 중간에 멈춤. 루프는 진행 중으로 남는다
        blocked = self.bundle_docs(BLOCKED)
        waiting = self.wait_summary(self.loop_branch, use_done=True)
        if blocked:
            listing = "\n".join(f"- {d.name}" for d in blocked)
            log(f"이번 묶음의 todo 가 비어 종료. 보류 {len(blocked)}건 남음 → 루프는 진행 중으로 유지")
            self.n.send_once(
                "loop-blocked",
                f"{self.loop_branch}|{listing}",
                "LOG",
                f"대기 비어 종료 (루프 진행 중, 묶음 {self.bundle})",
                f"보류 {len(blocked)}건 남음:\n{listing}\n\n루프 브랜치: `{self.loop_branch}`",
            )
            return

        self.state["status"] = "finished"
        self.state["finished_at"] = _dt.datetime.now().isoformat(timespec="seconds")
        save_state(self.state)
        wgit("checkout", "--detach")  # 사용자가 합친 뒤 루프 브랜치를 지울 수 있게 worktree 를 떼어 둔다
        done = self.state.get("processed", [])
        lines = "\n".join(f"- {p['doc']} (`{p['branch']}`, {p.get('agents') or '-'})" for p in done) or "- (없음)"
        others = [p.name for p in task_docs(TODO)]
        extra = ""
        if waiting:
            extra += f"\n\n**대기 중(wait)**\n{waiting}"
        if others:
            extra += f"\n\n**다른 묶음 todo**\n" + "\n".join(f"- {o}" for o in others)
        log(f"루프 종료: {self.loop_branch} (묶음 {self.bundle})")
        self.n.send(
            "DONE",
            f"루프 종료: 묶음 {self.bundle}",
            f"**처리한 작업 {len(done)}건**\n{lines}\n\n**확인할 루프 브랜치**: `{self.loop_branch}`\n"
            f"확인 후 {self.base} 에 직접 합쳐 주세요.{extra}",
        )
        if waiting or others:
            self.n.send(
                "LOG",
                "다음 루프 대기 중인 작업",
                f"루프 브랜치 `{self.loop_branch}` 를 {self.base} 에 합치면 다음 루프에서 처리합니다.{extra}",
            )

    def run_pass(self) -> None:
        self.preflight()
        if not self.open_loop():
            return
        self.reconcile()
        self.promote(self.loop_branch, use_done=True, bundle=self.bundle)
        self.triage(self.loop_branch, use_done=True)
        while not STOP.requested:
            todo = self.bundle_docs(TODO)
            if not todo:
                break
            name = todo[0].stem
            if self.process(todo[0]) == "done":
                self.promote(self.loop_branch, use_done=True, bundle=self.bundle, trigger=name)
            self.triage(self.loop_branch, use_done=True)
        self.finish()


def _sleep(seconds: int) -> None:
    end = time.time() + seconds
    while not STOP.requested and time.time() < end:
        time.sleep(min(1.0, end - time.time()))


def main() -> int:
    global _log_file, WT
    parser = argparse.ArgumentParser(description="매화검 키우기 AI 자동 개발 루프")
    parser.add_argument("--watch", action="store_true", help="끝나지 않고 일정 간격으로 todo 를 확인")
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
    notifier = Notifier(load_env(ROOT / ".env"))
    loop: Loop | None = None
    try:
        cfg = load_config()
        WT = (ROOT / cfg["worktree_path"]).resolve()
        loop = Loop(cfg, notifier, args.watch)
        if not args.watch:
            loop.run_pass()
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
                loop.run_pass()
                notifier.reset("preflight")
                last_sig, repeats = "", 0
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
