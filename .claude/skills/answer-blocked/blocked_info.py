"""보류 답하기(answer-blocked) 보조 스크립트.

보류 문서를 읽어 보여 주고(조회만), 사용자가 확인한 뒤 blocked 문서 하나를 옮긴다.
깃은 조회 명령(rev-parse, log, diff, for-each-ref)만 쓴다. 체크아웃·커밋·병합은 하지 않는다.

사용법 (프로젝트 폴더에서):
  python .claude/skills/answer-blocked/blocked_info.py list
  python .claude/skills/answer-blocked/blocked_info.py show <문서이름>
  python .claude/skills/answer-blocked/blocked_info.py stamp
  python .claude/skills/answer-blocked/blocked_info.py move <문서이름> todo
  python .claude/skills/answer-blocked/blocked_info.py move <문서이름> <tasks 밖 폴더>

  --root <폴더>  tasks/, .loop/ 를 찾을 폴더 (기본: 이 스크립트 기준 프로젝트 폴더). 예행 연습용.
"""

from __future__ import annotations

import argparse
import datetime as dt
import json
import re
import shutil
import subprocess
import sys
from pathlib import Path

DEFAULT_ROOT = Path(__file__).resolve().parents[3]
FOLDERS = ("todo", "wait", "doing", "blocked", "done")
HEADING_RE = re.compile(r"^##\s+(.+?)\s*$", re.MULTILINE)


# ───────── 문서 읽기 ─────────


def order_key(p: Path) -> tuple[int, str]:
    m = re.match(r"(\d+)", p.name)
    return (int(m.group(1)) if m else 10**9, p.name)


def docs_in(folder: Path) -> list[Path]:
    return sorted((p for p in folder.glob("*.md") if p.is_file()), key=order_key) if folder.exists() else []


def sections(text: str) -> list[tuple[str, str]]:
    """(제목, 본문) 목록. 같은 제목이 여러 번 나오면 모두 들어간다."""
    ms = list(HEADING_RE.finditer(text))
    out = []
    for i, m in enumerate(ms):
        end = ms[i + 1].start() if i + 1 < len(ms) else len(text)
        out.append((m.group(1).strip(), text[m.end() : end].strip()))
    return out


def first(secs, prefix: str) -> str | None:
    """루프 스크립트와 같은 규칙: 제목이 prefix 로 시작하는 첫 절."""
    for title, body in secs:
        if title.startswith(prefix):
            return body
    return None


def last(secs, prefix: str) -> tuple[str, str] | None:
    found = None
    for title, body in secs:
        if title.startswith(prefix):
            found = (title, body)
    return found


def parse_block(body: str) -> dict[str, str]:
    """'- 사유: ...' 형식을 키별로 나눈다. 사유는 여러 줄일 수 있다."""
    keys = ("사유", "질문", "추천안", "기능 브랜치", "작업 에이전트")
    out: dict[str, str] = {}
    cur = None
    for line in body.splitlines():
        m = re.match(r"^- (사유|질문|추천안|기능 브랜치|작업 에이전트): ?(.*)$", line)
        if m and m.group(1) in keys:
            cur = m.group(1)
            out[cur] = m.group(2)
        elif cur:
            out[cur] += "\n" + line
    return {k: v.strip() for k, v in out.items()}


def kind_of(reason: str) -> str:
    if reason.startswith("재시도 한도("):
        return "재시도 한도 초과"
    if reason.startswith("선행 작업이 서로를 가리키는 순환"):
        return "선행 작업 순환"
    if reason.startswith("done 폴더에 있지만 docs/history 에 기록이 없음"):
        return "done 인데 기록이 없음"
    return "에이전트의 질문"


def deps_of(text: str) -> list[str]:
    body = first(sections(text), "선행 작업") or ""
    names = []
    for token in re.split(r"[\n,]", body):
        t = token.strip().lstrip("-*").strip().strip("`").strip()
        m = re.match(r"[0-9A-Za-z가-힣_.\-]+", t)
        if not m:
            continue
        n = m.group(0)
        n = n[:-3] if n.lower().endswith(".md") else n
        if n and n != "없음" and n not in names:
            names.append(n)
    return names


def bundle_of(text: str) -> str:
    body = first(sections(text), "묶음") or ""
    for line in body.splitlines():
        v = line.strip().lstrip("-*").strip().strip("`").strip()
        if v:
            return "기본" if v == "없음" else v
    return "기본"


def where(root: Path, name: str) -> str | None:
    for f in FOLDERS:
        if (root / "tasks" / f / f"{name}.md").exists():
            return f
    return None


# ───────── 깃 조회 (읽기만) ─────────


def git(root: Path, *args: str) -> str | None:
    try:
        r = subprocess.run(["git", "-C", str(root), *args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    except FileNotFoundError:
        return None
    return r.stdout.strip("\n") if r.returncode == 0 else None


def branch_ok(root: Path, name: str) -> bool:
    return git(root, "rev-parse", "--verify", "--quiet", f"refs/heads/{name}") is not None


def safe_branch(stem: str) -> str:
    safe = re.sub(r"[^0-9A-Za-z가-힣_.-]+", "-", stem).strip("-.") or "task"
    return "task/" + safe


def load_state(root: Path) -> dict:
    p = root / ".loop" / "state.json"
    try:
        return json.loads(p.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return {}


# ───────── 로그 (읽기만) ─────────


def run_segments(logs: Path, name: str) -> list[tuple[Path, list[str]]]:
    """실행 로그에서 '작업 시작: <name>' 부터 다음 '작업 시작:' 앞까지 (이 문서를 처리한 구간)."""
    out = []
    for r in sorted(logs.glob("run_*.log")):
        cur: list[str] | None = None
        for ln in r.read_text(encoding="utf-8", errors="replace").splitlines():
            if "작업 시작: " in ln:
                if cur:
                    out.append((r, cur))
                cur = [ln] if f"작업 시작: {name}.md" in ln else None
            elif cur is not None:
                cur.append(ln)
        if cur:
            out.append((r, cur))
    return out


def agent_message(p: Path, limit: int = 1200) -> str:
    """시도 로그에서 에이전트의 마지막 메시지. 클로드는 JSON 의 result, 코덱스는 .codex_last.txt."""
    text = p.read_text(encoding="utf-8", errors="replace")
    msg = ""
    if p.name.endswith(".codex_last.txt"):
        msg = text
    else:
        stdout = text.split("--- stdout ---", 1)[-1].split("--- stderr ---", 1)[0].strip()
        for cand in (stdout, stdout.splitlines()[-1] if stdout else ""):
            try:
                data = json.loads(cand)
            except json.JSONDecodeError:
                continue
            if isinstance(data, dict) and "result" in data:
                msg = str(data["result"])
                break
        if not msg:
            msg = "(메시지를 찾지 못함. 파일을 직접 본다) " + text.splitlines()[0] if text else "(빈 파일)"
    msg = msg.strip()
    return msg if len(msg) <= limit else "…" + msg[-limit:]


# ───────── 명령 ─────────


def cmd_list(root: Path) -> int:
    state = load_state(root)
    blocked = docs_in(root / "tasks" / "blocked")
    print(f"루프 상태: {state.get('status', '(상태 파일 없음)')}, 루프 브랜치: {state.get('loop_branch', '-')}, 묶음: {state.get('bundle', '-')}")
    cur = state.get("current")
    if cur:
        print(f"주의: 루프가 지금 {cur.get('doc')} 를 처리 중입니다 (에이전트 실행 중일 수 있음).")
    if not blocked:
        print("보류 문서 없음")
        return 0
    print(f"보류 문서 {len(blocked)}건:")
    for p in blocked:
        text = p.read_text(encoding="utf-8-sig")
        lb = last(sections(text), "보류 사유")
        reason = parse_block(lb[1]).get("사유", "") if lb else ""
        n_blocks = sum(1 for t, _ in sections(text) if t.startswith("보류 사유"))
        n_ans = sum(1 for t, _ in sections(text) if t.startswith("답변"))
        extra = f", 보류 {n_blocks}번째" if n_blocks > 1 else ""
        extra += f", 이전 답변 {n_ans}개" if n_ans else ""
        print(f"- {p.stem}  [{kind_of(reason) if lb else '보류 사유 없음'}]  묶음 {bundle_of(text)}{extra}")
    return 0


def cmd_show(root: Path, name: str) -> int:
    name = name[:-3] if name.endswith(".md") else name
    path = root / "tasks" / "blocked" / f"{name}.md"
    if not path.exists():
        print(f"tasks/blocked/{name}.md 가 없음 (지금 위치: {where(root, name) or '없음'})")
        return 1
    text = path.read_text(encoding="utf-8-sig")
    secs = sections(text)
    state = load_state(root)

    print(f"# {name}")
    print(f"묶음: {bundle_of(text)}")
    print(f"목표: {(first(secs, '목표') or '(목표 칸 없음)').strip()}")
    deps = deps_of(text)
    if deps:
        print("선행 작업: " + ", ".join(f"{d}({where(root, d) or '기록 확인 필요'})" for d in deps))
    else:
        print("선행 작업: 없음")

    lb = last(secs, "보류 사유")
    info = parse_block(lb[1]) if lb else {}
    print(f"\n## 마지막 {lb[0] if lb else '보류 사유'} [{kind_of(info.get('사유', '')) if lb else '없음'}]")
    for k in ("사유", "질문", "추천안", "기능 브랜치", "작업 에이전트"):
        if k in info:
            print(f"- {k}: {info[k]}")

    olds = [t for t, _ in secs if t.startswith("보류 사유")][:-1]
    answers = [(t, b) for t, b in secs if t.startswith("답변")]
    if olds:
        print(f"\n이전 보류 {len(olds)}번: " + ", ".join(olds))
    for t, b in answers:
        print(f"\n## 이전 {t}\n{b}")

    reports = [(t, b) for t, b in secs if t.startswith("보고서")]
    if reports:
        t, b = reports[-1]
        print(f"\n## 에이전트 보고서 (마지막 것, {len(reports)}개 중)\n{b[:3000]}" + ("\n…(뒤 생략, 문서에서 직접 보기)" if len(b) > 3000 else ""))
    else:
        print("\n에이전트 보고서: 없음")

    # 기능 브랜치
    tb = (info.get("기능 브랜치", "").split(" ")[0] or safe_branch(name)).strip()
    loop_b = state.get("loop_branch") if state.get("status") == "active" else None
    base = loop_b if loop_b and branch_ok(root, loop_b) else "main"
    print(f"\n## 기능 브랜치 {tb} (기준 {base})")
    if not branch_ok(root, tb):
        print("브랜치 없음")
    else:
        log_ = git(root, "log", "--oneline", f"{base}..{tb}")
        stat = git(root, "diff", "--stat", f"{base}...{tb}")
        print("커밋:\n" + (log_ or "(기준 대비 커밋 없음)"))
        print("바뀐 파일:\n" + (stat or "(없음)"))

    # 로그
    logs = root / ".loop" / "logs"
    attempts = sorted(logs.glob(f"{name}_attempt*"), key=lambda p: (p.stat().st_mtime, p.name)) if logs.exists() else []
    print("\n## 로그")
    if attempts:
        print("에이전트 출력 (오래된 것부터. 시도 번호는 처리할 때마다 1부터 다시 세서 이전 보류 때 것은 덮어써진다):")
        for p in attempts:
            print(f"- {p.relative_to(root).as_posix()}  ({dt.datetime.fromtimestamp(p.stat().st_mtime):%Y-%m-%d %H:%M})")
        lastp = attempts[-1]
        if lastp.name.endswith("_codex.out.txt"):
            alt = lastp.with_name(lastp.name.replace(".out.txt", ".codex_last.txt"))
            lastp = alt if alt.exists() else lastp
        print(f"마지막 시도의 에이전트 마지막 메시지 ({lastp.name}):")
        print("  " + agent_message(lastp).replace("\n", "\n  "))
    else:
        print("에이전트 출력 로그 없음")
    segs = run_segments(logs, name) if logs.exists() else []
    if segs:
        r, seg = segs[-1]
        print(f"이 문서를 마지막으로 처리한 실행 로그: {r.relative_to(root).as_posix()} (이 문서 부분만)")
        for ln in seg[-30:]:
            print("  " + ln)

    # 이 문서를 기다리는 문서
    waiting = []
    for f in FOLDERS:
        for p in docs_in(root / "tasks" / f):
            if p.stem != name and name in deps_of(p.read_text(encoding="utf-8-sig")):
                waiting.append(f"{p.stem}({f})")
    print("\n이 문서를 선행 작업으로 가리키는 문서: " + (", ".join(waiting) or "없음"))
    if state.get("current"):
        print(f"\n주의: 루프가 지금 {state['current'].get('doc')} 를 처리 중입니다.")
    return 0


def cmd_stamp() -> int:
    print(f"{dt.datetime.now():%Y-%m-%d %H:%M}")
    return 0


def cmd_move(root: Path, name: str, dest: str) -> int:
    name = name[:-3] if name.endswith(".md") else name
    src = root / "tasks" / "blocked" / f"{name}.md"
    if not src.exists():
        print(f"거부: tasks/blocked/{name}.md 가 없음. 이 스크립트는 blocked 문서만 옮긴다.")
        return 1
    tasks = (root / "tasks").resolve()
    if dest == "todo":
        target_dir = tasks / "todo"
        others = [f for f in FOLDERS if f != "blocked" and (tasks / f / f"{name}.md").exists()]
        if others:
            print(f"거부: 같은 이름의 문서가 tasks/{', tasks/'.join(others)} 에 이미 있음. 옮기지 않았다.")
            return 1
    else:
        target_dir = Path(dest)
        if not target_dir.is_absolute():
            target_dir = (root / target_dir)
        target_dir = target_dir.resolve()
        if target_dir == tasks or tasks in target_dir.parents:
            print("거부: 버릴 문서는 tasks/ 밖으로 옮겨야 한다.")
            return 1
    target = target_dir / src.name
    if target.exists():
        print(f"거부: {target} 가 이미 있음. 옮기지 않았다.")
        return 1
    target_dir.mkdir(parents=True, exist_ok=True)
    shutil.move(str(src), str(target))
    print(f"옮김: tasks/blocked/{src.name} → {target}")
    return 0


def main() -> int:
    for s in (sys.stdout, sys.stderr):
        try:
            s.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=str(DEFAULT_ROOT))
    sub = ap.add_subparsers(dest="cmd", required=True)
    sub.add_parser("list")
    sp = sub.add_parser("show")
    sp.add_argument("name")
    sub.add_parser("stamp")
    mp = sub.add_parser("move")
    mp.add_argument("name")
    mp.add_argument("dest", help="todo, 또는 tasks 밖 폴더 경로")
    a = ap.parse_args()
    root = Path(a.root).resolve()
    if a.cmd == "list":
        return cmd_list(root)
    if a.cmd == "show":
        return cmd_show(root, a.name)
    if a.cmd == "stamp":
        return cmd_stamp()
    return cmd_move(root, a.name, a.dest)


if __name__ == "__main__":
    sys.exit(main())
