"""루프 현황(loop-status) 보조 스크립트. 읽기만 한다.

tasks/ 다섯 폴더, .loop/state.json, .loop/logs/, 깃 브랜치, 루프 worktree 를 읽어
"지금 사용자가 해야 할 일"과 상세 현황을 마크다운으로 출력한다.

- 문서를 옮기거나 고치지 않는다. .env 는 읽지 않는다.
- 깃은 조회 명령만 쓴다 (for-each-ref, merge-base --is-ancestor, cat-file -e, worktree list, status, rev-list).
  status 는 --no-optional-locks 로 실행해 인덱스도 건드리지 않는다.
- 루프가 실제로 돌고 있는지는 프로세스 목록과 로그 시각을 근거로 판단하고, 근거가 부족하면 "확실하지 않음"으로 적는다.

사용법 (프로젝트 폴더에서):
  python .claude/skills/loop-status/loop_status.py
  python .claude/skills/loop-status/loop_status.py --root <폴더>   # 다른 폴더(예행 연습용 복제본)를 본다
  python .claude/skills/loop-status/loop_status.py --no-process    # 프로세스 확인을 건너뛴다 (예행 연습용)
"""

from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import re
import subprocess
import sys
from pathlib import Path

DEFAULT_ROOT = Path(__file__).resolve().parents[3]
FOLDERS = ("todo", "wait", "doing", "blocked", "done")
HEADING_RE = re.compile(r"^##\s+(.+?)\s*$", re.MULTILINE)  # run_loop.py 와 같은 규칙
NAME_RE = re.compile(r"[0-9A-Za-z가-힣_.\-]+")
LOG_LINE_RE = re.compile(r"^\[(\d\d):(\d\d):(\d\d)\] (.*)$")
GUARD_LINE_RE = re.compile(r"^\[(\d{4}-\d\d-\d\d \d\d:\d\d:\d\d)\] (.*)$")

# 실행 로그 마지막 줄이 이것으로 시작하면 그 실행은 끝난 것이다 (run_loop.py 의 log() 문구)
END_MARKERS = (
    "실행 끝",  # 모든 실행의 마지막 줄 (강제 종료되면 남지 않는다)
    "감시 모드 종료",
    "시작 전 점검 실패:",  # 한 번 돌기. 감시 모드는 "시작 전 점검 실패, 기다림" 이라 다르다
    "오류로 멈춤",
    "사용자가 즉시 중단함",
    "일시적 오류로 멈춤",
    "같은 일시적 오류가",
    "모든 에이전트 사용량 한도 초과로 중단",
)
NOW = dt.datetime.now()


# ───────── 기본 도구 ─────────


def ago(t: dt.datetime | None) -> str:
    if t is None:
        return "알 수 없음"
    sec = int((NOW - t).total_seconds())
    if sec < 0:
        return "방금"
    if sec < 60:
        return f"{sec}초 전"
    if sec < 3600:
        return f"{sec // 60}분 전"
    if sec < 86400:
        return f"{sec // 3600}시간 {sec % 3600 // 60}분 전"
    return f"{sec // 86400}일 전"


def stamp(t: dt.datetime | None) -> str:
    return f"{t:%Y-%m-%d %H:%M}" if t else "-"


def parse_iso(s: str | None) -> dt.datetime | None:
    try:
        return dt.datetime.fromisoformat(s) if s else None
    except ValueError:
        return None


def one_line(s: str, limit: int = 120) -> str:
    s = " ".join((s or "").split())
    return s if len(s) <= limit else s[: limit - 1] + "…"


def sub_items(items: list[str]) -> str:
    """번호 목록 항목 아래에 붙는 하위 목록."""
    return "".join(f"\n   - {x}" for x in items)


class Git:
    def __init__(self, cwd: Path):
        self.cwd = cwd

    def run(self, *args: str, cwd: Path | None = None) -> subprocess.CompletedProcess:
        return subprocess.run(
            ["git", "--no-optional-locks", *args],
            cwd=str(cwd or self.cwd),
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
        )

    def ok(self, *args: str) -> bool:
        return self.run(*args).returncode == 0

    def out(self, *args: str, cwd: Path | None = None) -> str:
        p = self.run(*args, cwd=cwd)
        return p.stdout.strip() if p.returncode == 0 else ""

    def branches(self, prefix: str) -> list[str]:
        return [b for b in self.out("for-each-ref", "--format=%(refname:short)", f"refs/heads/{prefix}").splitlines() if b]

    def exists(self, branch: str) -> bool:
        return self.ok("show-ref", "--verify", "--quiet", f"refs/heads/{branch}")

    def merged(self, branch: str, into: str) -> bool:
        return self.ok("merge-base", "--is-ancestor", branch, into)

    def has_file(self, ref: str, rel: str) -> bool:
        return bool(ref) and self.ok("cat-file", "-e", f"{ref}:{rel}")

    def ahead(self, base: str, branch: str) -> int:
        try:
            return int(self.out("rev-list", "--count", f"{base}..{branch}") or 0)
        except ValueError:
            return 0


# ───────── 설정과 상태 ─────────


def load_config(root: Path) -> dict:
    cfg: dict = {}
    for name in ("loop_config.json", "loop_config.local.json"):  # run_loop.py 와 같은 덮어쓰기 규칙
        p = root / "tools" / "loop" / name
        if not p.exists():
            continue
        try:
            data = json.loads(p.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            continue
        for k, v in data.items():
            cfg[k] = {**cfg[k], **v} if isinstance(v, dict) and isinstance(cfg.get(k), dict) else v
    cfg.setdefault("base_branch", "main")
    cfg.setdefault("loop_branch_prefix", "loop/")
    cfg.setdefault("task_branch_prefix", "task/")
    cfg.setdefault("worktree_path", "../PlumSwordIdle_loop")
    cfg.setdefault("default_bundle", "기본")
    cfg.setdefault("watch_interval_sec", 60)
    cfg.setdefault("claude_timeout_min", 60)
    return cfg


def load_state(root: Path) -> tuple[dict, str]:
    p = root / ".loop" / "state.json"
    if not p.exists():
        return {}, "없음"
    try:
        return json.loads(p.read_text(encoding="utf-8")), "있음"
    except (OSError, json.JSONDecodeError) as e:
        return {}, f"읽을 수 없음 ({type(e).__name__})"


# ───────── 작업 문서 ─────────


def order_key(p: Path) -> tuple[int, str]:
    m = re.match(r"(\d+)", p.name)
    return (int(m.group(1)) if m else 10**9, p.name)


def sections(text: str) -> list[tuple[str, str]]:
    ms = list(HEADING_RE.finditer(text))
    return [
        (m.group(1).strip(), text[m.end() : (ms[i + 1].start() if i + 1 < len(ms) else len(text))])
        for i, m in enumerate(ms)
    ]


def first_section(secs, prefix: str) -> str | None:
    for title, body in secs:
        if title.startswith(prefix):
            return body
    return None


def clean_name(token: str) -> str | None:
    t = token.strip().lstrip("-*").strip().strip("`").strip()
    m = NAME_RE.match(t)
    if not m:
        return None
    name = m.group(0)
    name = name[:-3] if name.lower().endswith(".md") else name
    return None if name in ("", "없음") else name


class Doc:
    def __init__(self, path: Path, folder: str, default_bundle: str):
        self.path = path
        self.folder = folder
        self.name = path.stem
        m = re.match(r"(\d+)", path.name)
        self.number = m.group(1) if m else "-"
        try:
            text = path.read_text(encoding="utf-8-sig")
        except OSError:
            text = ""
        self.secs = sections(text)
        self.deps = self._deps(first_section(self.secs, "선행 작업"))
        self.bundle = self._first_value(first_section(self.secs, "묶음")) or default_bundle
        self.mtime = dt.datetime.fromtimestamp(path.stat().st_mtime)

    @staticmethod
    def _deps(section: str | None) -> list[str]:
        names: list[str] = []
        for token in re.split(r"[\n,]", section or ""):
            n = clean_name(token)
            if n and n not in names:
                names.append(n)
        return names

    @staticmethod
    def _first_value(section: str | None) -> str | None:
        for line in (section or "").splitlines():
            v = line.strip().lstrip("-*").strip().strip("`").strip()
            if v:
                return None if v == "없음" else v
        return None

    def block_info(self) -> dict:
        """마지막 '## 보류 사유' 절의 사유·종류, 그 뒤에 '## 답변' 이 있는지."""
        idx = None
        for i, (title, _) in enumerate(self.secs):
            if title.startswith("보류 사유"):
                idx = i
        if idx is None:
            return {"kind": "보류 사유 없음", "reason": "문서에 '## 보류 사유' 절이 없음 (사람이 직접 옮긴 문서일 수 있음)", "answered": False, "at": ""}
        title, body = self.secs[idx]
        m = re.search(r"^- 사유: ?(.*)$", body, re.MULTILINE)
        reason = m.group(1).strip() if m else one_line(body)
        if reason.startswith("재시도 한도("):
            kind = "재시도 한도 초과"
            # "재시도 한도(5회) 초과. 마지막 실패:" 다음 줄에 실패 내용이 있다
            rest = body[m.end():] if m else ""
            detail = next((ln.strip() for ln in rest.splitlines() if ln.strip() and not ln.startswith("- ")), "")
            if detail:
                reason = f"{reason} {detail}"
        elif reason.startswith("선행 작업이 서로를 가리키는 순환"):
            kind = "선행 작업 순환"
        elif reason.startswith("done 폴더에 있지만 docs/history 에 기록이 없음"):
            kind = "done 인데 기록 없음"
        else:
            kind = "질문"
        answered = any(t.startswith("답변") for t, _ in self.secs[idx + 1 :])
        at = title[len("보류 사유") :].strip(" ()")
        return {"kind": kind, "reason": one_line(reason, 140), "answered": answered, "at": at}


def load_docs(root: Path, default_bundle: str) -> dict[str, list[Doc]]:
    out: dict[str, list[Doc]] = {}
    for f in FOLDERS:
        d = root / "tasks" / f
        paths = sorted((p for p in d.glob("*.md") if p.is_file()), key=order_key) if d.exists() else []
        out[f] = [Doc(p, f, default_bundle) for p in paths]
    return out


def cycle_members(graph: dict[str, list[str]]) -> dict[str, list[str]]:
    """run_loop.py 와 같은 순환 찾기."""
    found: dict[str, list[str]] = {}
    for start in graph:
        stack, seen = [(start, [start])], set()
        while stack:
            node, path = stack.pop()
            hit = False
            for nxt in graph.get(node, []):
                if nxt == start:
                    found[start] = path + [start]
                    hit = True
                    break
                if nxt in graph and nxt not in seen:
                    seen.add(nxt)
                    stack.append((nxt, path + [nxt]))
            if hit:
                break
    return found


# ───────── 루프 실행 여부 ─────────


def find_processes(root: Path) -> tuple[list[dict] | None, str]:
    """run_loop.py 를 실행 중인 파이썬 프로세스. 확인할 수 없으면 (None, 이유)."""
    try:
        if os.name == "nt":
            ps = (
                "Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like '*run_loop.py*' "
                "-and $_.Name -match '^(python|pythonw|py)[0-9.]*\\.exe$' } | "
                "ForEach-Object { \"$($_.ProcessId)`t$($_.CreationDate.ToString('s'))`t$($_.CommandLine)\" }"
            )
            p = subprocess.run(
                ["powershell", "-NoProfile", "-NonInteractive", "-Command", ps],
                capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=30,
            )
            if p.returncode != 0:
                return None, f"프로세스 목록을 읽지 못함 ({one_line(p.stderr, 80)})"
            rows = []
            for line in p.stdout.splitlines():
                parts = line.split("\t", 2)
                if len(parts) == 3:
                    rows.append({"pid": parts[0], "started": parse_iso(parts[1]), "cmd": parts[2]})
        else:
            p = subprocess.run(["ps", "-eo", "pid=,lstart=,args="], capture_output=True, text=True, timeout=30)
            if p.returncode != 0:
                return None, "프로세스 목록을 읽지 못함"
            rows = []
            for line in p.stdout.splitlines():
                if "run_loop.py" in line and "python" in line:
                    pid, _, rest = line.strip().partition(" ")
                    rows.append({"pid": pid, "started": None, "cmd": rest[24:].strip() if len(rest) > 24 else rest})
    except (OSError, subprocess.SubprocessError) as e:
        return None, f"프로세스 목록을 읽지 못함 ({type(e).__name__})"
    # py.exe 런처는 python.exe 자식을 띄우므로 같은 명령이 두 번 보일 수 있다
    seen, uniq = set(), []
    for r in rows:
        key = r["cmd"].replace("py.exe", "").strip()
        if key in seen:
            continue
        seen.add(key)
        norm_cmd = os.path.normcase(r["cmd"].replace("/", "\\") if os.name == "nt" else r["cmd"])
        mine = os.path.normcase(str(root / "tools" / "loop" / "run_loop.py")) in norm_cmd
        r["this_project"] = True if mine else None  # None: 상대 경로로 실행돼 어느 프로젝트인지 모름
        r["watch"] = "--watch" in r["cmd"]
        uniq.append(r)
    return uniq, ""


def latest_run_log(root: Path) -> dict | None:
    logs = sorted((root / ".loop" / "logs").glob("run_*.log"))
    if not logs:
        return None
    p = logs[-1]
    m = re.match(r"run_(\d{8}-\d{6})\.log$", p.name)
    started = dt.datetime.strptime(m.group(1), "%Y%m%d-%H%M%S") if m else None
    try:
        lines = p.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError:
        lines = []
    msgs = []
    for ln in lines:
        mm = LOG_LINE_RE.match(ln)
        if mm:
            if mm.group(4).startswith("알림["):  # 디스코드 전송 결과 줄은 루프 진행과 상관없다
                msgs.append(None)
                continue
            msgs.append(mm.group(4))
        elif msgs and msgs[-1] is not None:  # 여러 줄 메시지(브랜치 목록, 오류 내용)
            msgs[-1] += "\n" + ln
    msgs = [x for x in msgs if x is not None]
    last_msg = msgs[-1] if msgs else ""
    return {
        "path": p,
        "started": started,
        "mtime": dt.datetime.fromtimestamp(p.stat().st_mtime),
        "lines": lines,
        "watch": any(x.startswith("감시 모드 시작") for x in msgs),
        "last": last_msg,
        "ended": last_msg.startswith(END_MARKERS),
        "count": len(logs),
    }


def read_running(root: Path) -> dict | None:
    """run_loop.py 가 실행 중에 두는 기록(.loop/running.json). 없거나 읽을 수 없으면 None."""
    try:
        rec = json.loads((root / ".loop" / "running.json").read_text(encoding="utf-8"))
        return rec if isinstance(rec, dict) and rec.get("pid") else None
    except (OSError, ValueError):
        return None


def judge_running(procs, proc_note: str, log: dict | None, cfg: dict, rec: dict | None = None) -> tuple[str, list[str]]:
    """('running' | 'stopped' | 'unknown', 근거 목록)"""
    ev: list[str] = []
    if rec:
        desc = f"실행 중 기록 `.loop/running.json`: PID {rec.get('pid')}, 모드 {rec.get('mode')}, 시작 {rec.get('started_at')}, 로그 {rec.get('log_file')}"
        if procs is None:
            ev.append(desc + " (그 프로세스가 살아 있는지는 확인하지 못함)")
        elif any(str(p["pid"]) == str(rec.get("pid")) for p in procs):
            return "running", [desc, "→ 기록된 PID 가 run_loop.py 프로세스로 살아 있음"]
        else:
            ev.append(desc + " → 그 PID 의 run_loop.py 프로세스가 없음. 강제 종료로 남은 기록 (다음 실행이 덮어쓴다)")
    if log:
        ev.append(
            f"가장 최근 실행 로그 `{log['path'].name}`: 시작 {stamp(log['started'])}, 마지막 기록 {stamp(log['mtime'])} ({ago(log['mtime'])})"
            + (", 감시 모드" if log["watch"] else "")
        )
        if log["ended"]:
            ev.append(f"그 로그의 마지막 줄이 종료 표시임: \"{one_line(log['last'], 80)}\"")
    else:
        ev.append("실행 로그(`.loop/logs/run_*.log`)가 없음")

    if procs is None:
        ev.append(f"프로세스 확인 못 함: {proc_note}")
        if log and log["ended"]:
            return "stopped", ev + ["→ 로그로는 끝난 것으로 보이지만, 프로세스를 확인하지 못했으므로 확실하지 않음"]
        return "unknown", ev + ["→ 로그 시각만으로는 판단할 수 없음 (에이전트 실행 중에는 최대 수십 분간 로그가 안 쓰임)"]

    mine = [p for p in procs if p["this_project"]]
    maybe = [p for p in procs if p["this_project"] is None]
    for p in procs:
        where = "이 프로젝트" if p["this_project"] else "상대 경로로 실행 (어느 프로젝트인지 명령줄로는 모름)"
        ev.append(f"run_loop.py 프로세스 PID {p['pid']}, 시작 {stamp(p['started'])}, {'감시 모드' if p['watch'] else '한 번 돌기'}, {where}")
    if mine:
        return "running", ev
    if maybe:
        # 실행 로그가 프로세스 시작 무렵에 만들어졌으면 이 프로젝트 것으로 본다
        if log and log["started"] and any(
            p["started"] and abs((p["started"] - log["started"]).total_seconds()) < 120 for p in maybe
        ):
            return "running", ev + ["→ 프로세스 시작 시각과 이 프로젝트의 실행 로그 시작 시각이 맞음"]
        return "unknown", ev + ["→ run_loop.py 프로세스는 있지만 이 프로젝트 것인지 확인되지 않음"]
    ev.append("run_loop.py 를 실행 중인 파이썬 프로세스 없음")
    return "stopped", ev


# ───────── 판단 ─────────


class Report:
    def __init__(self, root: Path, check_process: bool):
        self.root = root
        self.cfg = load_config(root)
        self.git = Git(root)
        self.base = self.cfg["base_branch"]
        self.state, self.state_note = load_state(root)
        self.docs = load_docs(root, self.cfg["default_bundle"])
        self.by_name = {d.name: d for docs in self.docs.values() for d in docs}
        self.active_branch = self.state.get("loop_branch") if self.state.get("status") == "active" else None
        self.active_ok = bool(self.active_branch) and self.git.exists(self.active_branch)
        self.bundle = self.state.get("bundle") if self.active_branch else None
        self.log = latest_run_log(root)
        procs, note = find_processes(root) if check_process else (None, "--no-process 로 건너뜀")
        self.running, self.run_evidence = judge_running(procs, note, self.log, self.cfg, read_running(root))
        self.wt = self.worktree_info()
        self.todo_items: list[tuple[int, str]] = []  # (우선순위, 내용)

    # ── 선행 작업 위치 ──
    def dep_where(self, dep: str) -> tuple[str, bool]:
        """(설명, 완료 여부). run_loop.py 의 완료 판정: done 폴더 또는 기준 브랜치(루프 안에서는 루프 브랜치)의 docs/history."""
        rel = f"docs/history/{dep}.md"
        if (self.root / "tasks" / "done" / f"{dep}.md").exists():
            return "완료(done)", True
        if self.git.has_file(self.base, rel):
            return f"완료({self.base} 기록)", True
        if self.active_ok and self.git.has_file(self.active_branch, rel):
            return "완료(루프 브랜치 기록)", True
        d = self.by_name.get(dep)
        if d:
            return d.folder, False
        return "어디에도 없음", False

    # ── worktree ──
    def worktree_info(self) -> dict:
        wt = (self.root / self.cfg["worktree_path"]).resolve()
        info = {"path": wt, "exists": wt.exists(), "listed": False, "branch": "", "dirty": [], "error": ""}
        cur = None
        for line in self.git.out("worktree", "list", "--porcelain").splitlines():
            if line.startswith("worktree "):
                cur = line[len("worktree ") :].strip()
            elif cur and os.path.normcase(os.path.abspath(cur)) == os.path.normcase(str(wt)):
                info["listed"] = True
                if line.startswith("branch "):
                    info["branch"] = line[len("branch refs/heads/") :]
                elif line == "detached":
                    info["branch"] = "(detached)"
        if info["exists"] and info["listed"]:
            p = self.git.run("status", "--porcelain", cwd=wt)
            if p.returncode == 0:
                info["dirty"] = [ln for ln in p.stdout.splitlines() if ln.strip()]
            else:
                info["error"] = one_line(p.stderr, 100)
        return info

    def add(self, prio: int, text: str) -> None:
        self.todo_items.append((prio, text))

    # ── 해야 할 일 판단 ──
    def decide(self) -> None:
        d = self.docs
        running = self.running
        run_cmd = "`python tools/loop/run_loop.py` (한 번) 또는 `python tools/loop/run_loop.py --watch` (감시 모드)"
        not_running_phrase = {"stopped": "루프가 돌고 있지 않으므로", "unknown": "루프가 돌고 있지 않다면 (실행 여부 확실하지 않음)"}

        unmerged = [b for b in self.git.branches(self.cfg["loop_branch_prefix"]) if b != self.active_branch and not self.git.merged(b, self.base)]
        self.unmerged = unmerged
        rerun: list[str] = []  # 루프를 다시 실행하면 풀리는 것들. 마지막에 한 항목으로 모은다

        # 1. 상태 파일 이상
        if self.state_note.startswith("읽을 수 없음"):
            self.add(0, f"`.loop/state.json` 을 {self.state_note}. 루프가 다음 실행에서 멈춘다. 파일 내용을 직접 확인한다.")
        if self.active_branch and not self.active_ok:
            self.add(0, f"상태 파일의 진행 중인 루프 브랜치 `{self.active_branch}` 가 없다. 루프가 시작 전 점검에서 멈춘다. 브랜치를 지웠다면 `.loop/state.json` 을 정리한다 (docs/loop.md \"루프가 끝난 뒤 main에 합치기\").")

        # 2. worktree 의 정리 안 된 변경 (자동 복구 대상이 아니면 루프가 시작하지 않음)
        wt = self.wt
        if wt["exists"] and not wt["listed"]:
            self.add(0, f"`{wt['path']}` 폴더가 있지만 이 저장소의 worktree 가 아니다. 루프가 시작하지 않는다. 확인 후 지우거나 `worktree_path` 를 바꾼다.")
        if wt["dirty"] and running != "running":
            doing_branches = {self.task_branch(x.name) for x in d["doing"]}
            cur = (self.state.get("current") or {}).get("branch")
            recoverable = bool(self.active_branch) and wt["branch"].startswith(self.cfg["task_branch_prefix"]) and (wt["branch"] in doing_branches or wt["branch"] == cur)
            if recoverable:
                rerun.append(f"루프 worktree 의 `{wt['branch']}` 에 커밋 안 된 변경 {len(wt['dirty'])}건이 남아 있다 (중간에 멈춤). 다시 실행하면 \"중단 복구\" 커밋으로 넣고 이어서 한다.")
            else:
                self.add(0, f"루프 worktree(`{wt['path']}`, 브랜치 {wt['branch'] or '?'})에 커밋 안 된 변경 {len(wt['dirty'])}건이 있고 자동 복구 대상이 아니다. 루프가 시작하지 않는다. worktree 에서 직접 확인하고 정리한다.")

        # 3. doing 에 남은 문서
        if d["doing"] and running != "running":
            names = ", ".join(x.path.name for x in d["doing"])
            rerun.append(f"`doing` 에 {names} 가 남아 있다. {not_running_phrase[running]} 중간에 멈춘 것이다. 다시 실행하면 todo 로 되돌려(완료 커밋이 있으면 done 으로) 이어서 처리한다.")
        if rerun and self.state.get("last_stop"):
            ls = self.state["last_stop"]
            rerun.append(f"마지막으로 멈춘 이유 ({ls.get('at', '?')}): {one_line(ls.get('reason', ''), 120)}")

        # 4. 합쳐지지 않은 완료 루프
        if unmerged:
            lst = ", ".join(f"`{b}`" for b in unmerged)
            self.add(1, f"끝났지만 {self.base} 에 합쳐지지 않은 루프 브랜치가 있다: {lst}. **이것이 남아 있으면 새 루프가 시작되지 않는다.** `review-loop` 스킬로 확인한 뒤 프로젝트 폴더(main)에서 `git merge <브랜치>` 로 합친다 (버리려면 docs/loop.md \"루프가 끝난 뒤 main에 합치기\").")

        # 5. 보류
        blocked = d["blocked"]
        if blocked:
            answered = [x for x in blocked if x.block_info()["answered"]]
            unanswered = [x for x in blocked if not x.block_info()["answered"]]
            if unanswered:
                kinds: dict[str, int] = {}
                for x in unanswered:
                    k = x.block_info()["kind"]
                    kinds[k] = kinds.get(k, 0) + 1
                kind_txt = ", ".join(f"{k} {n}건" for k, n in kinds.items())
                same = [x for x in unanswered if self.bundle and x.bundle == self.bundle]
                extra = f" 이 중 {len(same)}건은 진행 중인 루프의 묶음({self.bundle})이라, 답하기 전까지 이 루프는 끝나지 않고 다른 묶음도 시작되지 않는다." if same else ""
                self.add(1, f"보류 {len(unanswered)}건 ({kind_txt}): `answer-blocked` 스킬로 답하고 todo 로 되돌린다.{extra}")
            if answered:
                names = ", ".join(x.path.name for x in answered)
                self.add(2, f"보류 문서 {names} 에 답변이 적혀 있지만 아직 blocked 에 있다. `answer-blocked` 스킬로 확인하고 todo 로 옮긴다.")

        # 6. 선행 작업이 어디에도 없는 wait
        missing = []
        for x in d["wait"]:
            gone = [dep for dep in x.deps if self.dep_where(dep) == ("어디에도 없음", False)]
            if gone:
                missing.append(f"{x.path.name} ← {', '.join(gone)}")
        if missing:
            self.add(1, "선행 작업이 tasks 어디에도 없고 기록도 없는 wait 문서가 있다 (루프가 영원히 기다린다): "
                     + "; ".join(missing)
                     + ". 선행 작업 문서를 `make-tasks` 로 만들어 todo 에 넣거나, 그 문서의 `## 선행 작업` 칸을 고친다.")

        # 7. 순환 (다음 실행 때 blocked 로 감)
        graph = {x.name: x.deps for f in ("todo", "wait", "blocked", "doing") for x in d[f]}
        cyc = {n: p for n, p in cycle_members(graph).items() if self.by_name[n].folder in ("todo", "wait")}
        if cyc:
            routes = sorted({" → ".join(p) for p in cyc.values()})
            self.add(2, f"선행 작업 순환이 있다: {'; '.join(routes)}. 다음 실행 때 blocked 로 간다. 미리 고치려면 한 문서의 `## 선행 작업` 칸을 고친다.")

        # 8. 루프를 돌려야 하는가
        todo = d["todo"]
        wait_ready = [x for x in d["wait"] if x.deps and all(self.dep_where(dep)[1] for dep in x.deps)]
        if running == "running":
            self.add(3, "루프가 돌고 있다. 기다리면 된다. (진행 상황은 아래 \"루프 상태\"와 \"최근 실행\")")
        elif unmerged and (todo or wait_ready or rerun):
            what = f"todo {len(todo)}건" + (", 중간에 멈춘 작업" if rerun else "")
            self.add(3, f"{what}이 기다리고 있지만, 합쳐지지 않은 루프 브랜치를 먼저 합쳐야 루프가 시작된다. 합친 뒤 {run_cmd}." + sub_items(rerun))
        elif todo or wait_ready or rerun:
            what = ", ".join(([f"todo {len(todo)}건"] if todo else []) + ([f"선행 작업이 끝나 올라갈 wait {len(wait_ready)}건"] if wait_ready else []) + (["중간에 멈춘 작업"] if rerun else []))
            note = ""
            other = [x for x in todo if self.bundle and x.bundle != self.bundle]
            if other and len(other) == len(todo) and not wait_ready and not rerun:
                note = f" 단, todo 는 모두 다른 묶음이라 진행 중인 루프(묶음 {self.bundle})가 끝나고 main 에 합친 뒤에 처리된다."
            elif other:
                note = f" 이번 루프(묶음 {self.bundle})는 그 묶음 문서만 처리하고, 다른 묶음 {len(other)}건({', '.join(x.name for x in other)})은 이 루프를 main 에 합친 뒤 처리된다."
            prefix = "루프가 돌고 있지 않다." if running == "stopped" else "루프가 돌고 있는지 확실하지 않다. 돌고 있지 않다면"
            self.add(2 if rerun else 3, f"{prefix} {what}이 기다리고 있으니 루프를 실행한다: {run_cmd}.{note}" + sub_items(rerun))
        elif self.active_branch and self.active_ok and not d["doing"] and not any(x.bundle == self.bundle for x in blocked) and running != "running":
            self.add(3, f"상태 파일은 진행 중(active)인데 이번 묶음({self.bundle})에 남은 작업이 없다. 루프를 한 번 실행하면 루프를 끝내고 \"루프 종료\" 알림을 보낸다: `python tools/loop/run_loop.py`")

        # 9. 할 일이 없음
        if not self.todo_items:
            if running == "unknown":
                self.add(3, "루프가 돌고 있는지는 확실하지 않다 (아래 근거).")
            self.add(3, "대기 중인 작업이 없다. `plan-feature` 로 다음 기능을 기획하고 `make-tasks` 로 작업 문서를 만들어 todo 에 넣는다.")

        # 10. 검사 훅 기록 (참고)
        g = self.guard_info()
        if g and g["recent"]:
            self.add(4, f"(참고) 검사 훅이 최근 24시간에 호출 {g['recent']}건을 막았다. 정상 작업이 막힌 것인지 `.loop/logs/guard.log` 를 확인한다 (docs/loop.md \"클로드가 정상 작업 중에 막힐 때\").")

    def task_branch(self, doc_name: str) -> str:
        stem = Path(doc_name).stem
        safe = re.sub(r"[^0-9A-Za-z가-힣_.-]+", "-", stem).strip("-.") or "task"
        return self.cfg["task_branch_prefix"] + safe

    def guard_info(self) -> dict | None:
        p = self.root / ".loop" / "logs" / "guard.log"
        if not p.exists():
            return None
        try:
            lines = [ln for ln in p.read_text(encoding="utf-8", errors="replace").splitlines() if ln.strip()]
        except OSError:
            return None
        recent, last_t, last_line = 0, None, ""
        for ln in lines:
            m = GUARD_LINE_RE.match(ln)
            if not m:
                continue
            t = dt.datetime.strptime(m.group(1), "%Y-%m-%d %H:%M:%S")
            if (NOW - t).total_seconds() <= 86400:
                recent += 1
            last_t, last_line = t, m.group(2)
        return {"total": len(lines), "recent": recent, "last_t": last_t, "last": last_line}

    # ── 출력 ──
    def render(self) -> str:
        self.decide()
        o: list[str] = []
        o.append(f"# 루프 현황 ({NOW:%Y-%m-%d %H:%M} 기준)")
        o.append(f"\n프로젝트 폴더: `{self.root}`")

        o.append("\n## 지금 해야 할 일\n")
        for i, (_, t) in enumerate(sorted(self.todo_items, key=lambda x: x[0]), 1):
            o.append(f"{i}. {t}")

        label = {"running": "돌고 있음", "stopped": "돌고 있지 않음", "unknown": "확실하지 않음"}[self.running]
        o.append(f"\n## 루프가 지금 돌고 있는가: {label}\n")
        o += [f"- {e}" for e in self.run_evidence]

        o.append("\n## 폴더별 작업\n")
        o.append("| 폴더 | 수 |\n| --- | --- |")
        o += [f"| {f} | {len(self.docs[f])} |" for f in FOLDERS]
        for f in FOLDERS:
            docs = self.docs[f]
            if not docs:
                continue
            o.append(f"\n### {f} ({len(docs)})\n")
            if f == "wait":
                o.append("| 번호 | 이름 | 묶음 | 기다리는 선행 작업 (지금 위치) |\n| --- | --- | --- | --- |")
                for x in docs:
                    parts = []
                    for dep in x.deps:
                        where, _ = self.dep_where(dep)
                        parts.append(f"**{dep} (어디에도 없음)**" if where == "어디에도 없음" else f"{dep} ({where})")
                    if x.deps and all(self.dep_where(dep)[1] for dep in x.deps):
                        parts.append("→ 모두 완료, 다음 실행 때 todo 로 올라감")
                    o.append(f"| {x.number} | {x.name} | {x.bundle} | {', '.join(parts) or '(선행 작업 칸 비어 있음)'} |")
            elif f == "blocked":
                o.append("| 번호 | 이름 | 묶음 | 종류 | 사유 | 답변 |\n| --- | --- | --- | --- | --- | --- |")
                for x in docs:
                    b = x.block_info()
                    o.append(f"| {x.number} | {x.name} | {x.bundle} | {b['kind']} | {b['reason'].replace('|', '/')} | {'적힘 (todo 로 옮기면 됨)' if b['answered'] else '없음'} |")
            elif f == "doing":
                o.append("| 번호 | 이름 | 묶음 | 상태 |\n| --- | --- | --- | --- |")
                for x in docs:
                    st = {"running": "처리 중 (루프가 돌고 있음)", "stopped": "**중간에 멈춘 것** (루프가 돌고 있지 않음)", "unknown": "루프가 안 돌고 있다면 중간에 멈춘 것 (확실하지 않음)"}[self.running]
                    o.append(f"| {x.number} | {x.name} | {x.bundle} | {st}, 문서 수정 {stamp(x.mtime)} |")
            elif f == "todo":
                o.append("| 번호 | 이름 | 묶음 | 메모 |\n| --- | --- | --- | --- |")
                for x in docs:
                    notes = []
                    pend = [f"{dep}({self.dep_where(dep)[0]})" for dep in x.deps if not self.dep_where(dep)[1]]
                    if pend:
                        notes.append("선행 작업 미완료 → 다음 실행 때 wait 로 감: " + ", ".join(pend))
                    if self.bundle and x.bundle != self.bundle:
                        notes.append(f"진행 중인 루프의 묶음({self.bundle})이 아님 → 그 루프를 합친 뒤 처리")
                    o.append(f"| {x.number} | {x.name} | {x.bundle} | {'; '.join(notes) or '-'} |")
            else:
                o.append("| 번호 | 이름 | 묶음 |\n| --- | --- | --- |")
                o += [f"| {x.number} | {x.name} | {x.bundle} |" for x in docs]

        o.append("\n## 루프 상태 (`.loop/state.json`)\n")
        s = self.state
        if not s:
            o.append(f"- 상태 파일: {self.state_note} (아직 루프를 시작한 적이 없거나 정리됨)")
        else:
            o.append(f"- 상태: **{s.get('status', '?')}** ({'진행 중' if s.get('status') == 'active' else '완료' if s.get('status') == 'finished' else '알 수 없음'})")
            lb = s.get("loop_branch", "-")
            exists = self.git.exists(lb) if lb != "-" else False
            if not exists:
                where = " (브랜치 없음)"
            elif self.git.ahead(self.base, lb) == 0:
                where = f" ({self.base} 에 합쳐짐)" if self.git.ahead(lb, self.base) else f" ({self.base} 와 같은 위치, 새 커밋 없음)"
            else:
                where = f" ({self.base} 보다 커밋 {self.git.ahead(self.base, lb)}개 앞섬)"
            o.append(f"- 루프 브랜치: `{lb}`{where}")
            o.append(f"- 묶음: {s.get('bundle', '-')}")
            o.append(f"- 시작: {s.get('started_at', '-')}" + (f", 끝남: {s['finished_at']}" if s.get("finished_at") else ""))
            processed = s.get("processed") or []
            o.append(f"- 처리한 작업 {len(processed)}건" + (": " + ", ".join(f"{p.get('doc')} ({p.get('agents') or '-'})" for p in processed) if processed else ""))
            cur = s.get("current")
            o.append(f"- 처리 중인 작업: {cur.get('doc')} (`{cur.get('branch')}`)" if cur else "- 처리 중인 작업: 없음")
            ls = s.get("last_stop")
            o.append(f"- 마지막으로 멈춘 이유: {ls.get('at', '?')} — {one_line(ls.get('reason', ''), 200)}" if ls else "- 마지막으로 멈춘 이유: 기록 없음 (다음 실행이 복구에 쓰고 지움)")

        o.append("\n## 브랜치\n")
        loops = self.git.branches(self.cfg["loop_branch_prefix"])
        tasks = self.git.branches(self.cfg["task_branch_prefix"])
        if not loops and not tasks:
            o.append("- `loop/*`, `task/*` 브랜치 없음")
        for b in loops:
            new = self.git.ahead(self.base, b)
            if b == self.active_branch:
                st = f"진행 중인 루프 ({self.base} 보다 커밋 {new}개 앞섬)"
            elif self.git.merged(b, self.base):
                st = f"{self.base} 에 합쳐짐 (지워도 됨: `git branch -d {b}`)"
            else:
                st = f"**{self.base} 에 합쳐지지 않음 (커밋 {new}개) → 이게 남아 있으면 새 루프가 시작되지 않음**"
            o.append(f"- `{b}`: {st}")
        for b in tasks:
            doc = next((x for x in self.by_name.values() if self.task_branch(x.name) == b), None)
            where = f"문서는 {doc.folder}" if doc else "문서 없음"
            live = doc is not None and doc.folder in ("todo", "doing", "blocked", "wait")  # 아직 끝나지 않은 작업의 브랜치
            n = self.ahead(b)
            if n:
                st = f"`{self.compare_ref()}` 에 안 합쳐진 커밋 {n}개" + (" (보류된 작업 중 상태, 지우지 말 것)" if doc and doc.folder == "blocked" else "")
            elif live:
                st = "아직 새 커밋 없음 (작업이 끝나지 않았으니 지우지 말 것)"
            elif self.git.merged(b, self.base):
                st = f"{self.base} 에 합쳐짐 (지워도 됨: `git branch -d {b}`)"
            else:
                st = f"`{self.compare_ref()}` 에 합쳐짐 (루프를 {self.base} 에 합친 뒤 지우면 됨)"
            o.append(f"- `{b}`: {where}, {st}")

        o.append("\n## worktree\n")
        wt = self.wt
        if not wt["exists"]:
            o.append(f"- `{wt['path']}`: 없음 (다음 실행 때 루프가 만든다)")
        elif not wt["listed"]:
            o.append(f"- `{wt['path']}`: 폴더는 있지만 이 저장소의 worktree 가 아님")
        else:
            o.append(f"- 위치: `{wt['path']}`")
            o.append(f"- 브랜치: {wt['branch'] or '?'}")
            if wt["error"]:
                o.append(f"- 변경 확인 실패: {wt['error']}")
            elif wt["dirty"]:
                o.append(f"- 커밋 안 된 변경 {len(wt['dirty'])}건" + (" (루프가 돌고 있으면 작업 중인 정상 상태)" if self.running != "stopped" else ""))
                o += [f"  - `{ln}`" for ln in wt["dirty"][:10]]
                if len(wt["dirty"]) > 10:
                    o.append(f"  - … 외 {len(wt['dirty']) - 10}건")
            else:
                o.append("- 커밋 안 된 변경: 없음")

        o.append("\n## 최근 실행\n")
        lg = self.log
        if not lg:
            o.append("- 실행 로그 없음")
        else:
            o.append(f"- 로그 `{lg['path'].relative_to(self.root).as_posix()}` (전체 {lg['count']}개 중 가장 최근), 마지막 기록 {stamp(lg['mtime'])} ({ago(lg['mtime'])})")
            useful = [ln for ln in lg["lines"] if "] 알림[" not in ln]  # 알림 전송 줄은 뺀다
            o.append("\n```")
            o += useful[-12:]
            o.append("```")
        g = self.guard_info()
        if g is None:
            o.append("\n- 검사 훅 기록(`guard.log`): 없음")
        else:
            o.append(f"\n- 검사 훅 기록(`guard.log`): 전체 {g['total']}건, 최근 24시간 {g['recent']}건"
                     + (f". 마지막: {stamp(g['last_t'])} {one_line(g['last'], 120)}" if g["last_t"] else ""))
        return "\n".join(o) + "\n"

    def compare_ref(self) -> str:
        """기능 브랜치를 비교할 기준: 상태 파일의 루프 브랜치(진행 중이든 끝났든), 없으면 기준 브랜치."""
        lb = self.state.get("loop_branch")
        return lb if lb and self.git.exists(lb) else self.base

    def ahead(self, branch: str) -> int:
        return self.git.ahead(self.compare_ref(), branch)


def main() -> int:
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass
    ap = argparse.ArgumentParser(description="자동 개발 루프 현황 (읽기만 함)")
    ap.add_argument("--root", type=Path, default=DEFAULT_ROOT, help="프로젝트 폴더 (기본: 이 스크립트 기준)")
    ap.add_argument("--no-process", action="store_true", help="프로세스 확인을 건너뜀")
    args = ap.parse_args()
    root = args.root.resolve()
    if not (root / "tasks").exists():
        print(f"{root} 에 tasks/ 폴더가 없음. 프로젝트 폴더에서 실행하거나 --root 를 확인해 주세요.")
        return 1
    print(Report(root, not args.no_process).render())
    return 0


if __name__ == "__main__":
    sys.exit(main())
