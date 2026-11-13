"""make-tasks 스킬: 작업 문서 초안 검사.

초안 폴더의 작업 문서를 루프 스크립트(tools/loop/run_loop.py)의 Doc 클래스로 직접 읽어,
루프가 읽게 될 묶음·선행 작업을 보여 주고 문제를 찾는다. 아무 파일도 쓰거나 옮기지 않는다.

사용:
  python .claude/skills/make-tasks/check_tasks.py <초안 폴더>
  python .claude/skills/make-tasks/check_tasks.py --next-number      다음에 쓸 번호만 출력

옵션:
  --root <폴더>   프로젝트 폴더 (기본: 이 파일 기준 세 단계 위). tasks/ 와 docs/history/ 를 여기서 찾는다

종료 코드: 0 문제 없음(경고만 있을 수 있음), 1 오류 있음, 2 실행할 수 없음
"""

from __future__ import annotations

import argparse
import importlib.util
import json
import re
import subprocess
import sys
from pathlib import Path

HEADINGS = ["목표", "묶음", "선행 작업", "참고 문서", "요구사항", "완료 조건", "하지 말 것"]
TASK_FOLDERS = ["todo", "wait", "doing", "blocked", "done"]
UNFINISHED = ["todo", "wait", "doing", "blocked"]
FILE_RE = re.compile(r"^(\d{3,})_[a-z0-9]+(?:_[a-z0-9]+)*\.md$")
VAGUE = ["잘 동작", "잘 작동", "정상적으로", "정상 동작", "문제없이", "문제 없이", "올바르게", "제대로", "적절히", "자연스럽게"]
HEADING_RE = re.compile(r"^##\s+(.+?)\s*$", re.MULTILINE)
BUNDLE_RE = re.compile(r"[0-9A-Za-z가-힣_\-]+")
MD_PATH_RE = re.compile(r"[A-Za-z0-9_./\-가-힣]+\.md")

sys.dont_write_bytecode = True  # run_loop.py 를 불러올 때 tools/loop 에 캐시 파일을 남기지 않는다


def load_loop(root: Path):
    path = root / "tools" / "loop" / "run_loop.py"
    if not path.exists():
        print(f"실행할 수 없음: {path} 가 없다")
        sys.exit(2)
    spec = importlib.util.spec_from_file_location("run_loop_for_check", path)
    mod = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = mod
    spec.loader.exec_module(mod)
    return mod


def load_cfg(root: Path) -> dict:
    cfg = {"base_branch": "main", "default_bundle": "기본"}
    for name in ("loop_config.json", "loop_config.local.json"):
        p = root / "tools" / "loop" / name
        if p.exists():
            try:
                cfg.update(json.loads(p.read_text(encoding="utf-8-sig")))
            except (OSError, ValueError):
                pass
    return cfg


def git_lines(root: Path, *args: str) -> list[str] | None:
    try:
        r = subprocess.run(
            ["git", "-c", "core.quotepath=off", *args],
            cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace",
        )
    except OSError:
        return None
    return r.stdout.splitlines() if r.returncode == 0 else None


def existing_docs(root: Path, base: str) -> dict[str, str]:
    """이미 있는 작업 문서 이름 → 있는 곳"""
    found: dict[str, str] = {}
    hist = root / "docs" / "history"
    if hist.is_dir():
        for p in hist.glob("*.md"):
            found[p.stem] = "history"
    for line in git_lines(root, "ls-tree", "-r", "--name-only", base, "--", "docs/history") or []:
        if line.endswith(".md"):
            found.setdefault(Path(line).stem, "history")
    for folder in TASK_FOLDERS:
        d = root / "tasks" / folder
        if d.is_dir():
            for p in d.glob("*.md"):
                found[p.stem] = folder
    return found


def number_of(name: str) -> int | None:
    m = re.match(r"(\d+)", name)
    return int(m.group(1)) if m else None


def next_number(existing: dict[str, str]) -> int:
    nums = [n for n in (number_of(name) for name in existing) if n is not None]
    return (max(nums) if nums else 0) + 1


def sections(text: str) -> list[tuple[str, str]]:
    ms = list(HEADING_RE.finditer(text))
    out = []
    for i, m in enumerate(ms):
        end = ms[i + 1].start() if i + 1 < len(ms) else len(text)
        out.append((m.group(1).strip(), text[m.end():end]))
    return out


def body_lines(body: str) -> list[str]:
    return [ln.strip() for ln in body.splitlines() if ln.strip()]


def main() -> int:
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8")
        except (AttributeError, ValueError):
            pass
    ap = argparse.ArgumentParser(description="작업 문서 초안 검사")
    ap.add_argument("drafts", nargs="?", help="초안 폴더")
    ap.add_argument("--root", default=None, help="프로젝트 폴더")
    ap.add_argument("--next-number", action="store_true", help="다음에 쓸 번호만 출력")
    args = ap.parse_args()

    root = Path(args.root).resolve() if args.root else Path(__file__).resolve().parents[3]
    cfg = load_cfg(root)
    base = cfg["base_branch"]
    existing = existing_docs(root, base)
    nxt = next_number(existing)

    if args.next_number:
        print(f"{nxt:03d}")
        return 0
    if not args.drafts:
        ap.error("초안 폴더를 적어 주세요")
    drafts_dir = Path(args.drafts).resolve()
    tasks_dir = (root / "tasks").resolve()
    if drafts_dir == tasks_dir or tasks_dir in drafts_dir.parents:
        print("실행할 수 없음: 초안 폴더가 tasks/ 안에 있다. 초안은 tasks/ 밖에 둔다")
        return 2
    files = sorted(p for p in drafts_dir.glob("*.md") if p.is_file())
    if not files:
        print(f"실행할 수 없음: {drafts_dir} 에 .md 파일이 없다")
        return 2

    loop = load_loop(root)
    errors: list[str] = []
    warns: list[str] = []
    docs = {}
    texts = {}
    no_spec: list[str] = []

    tracked = git_lines(root, "ls-tree", "-r", "--name-only", base)
    tracked_set = set(tracked) if tracked is not None else None

    for p in files:
        name = p.stem
        tag = p.name
        text = p.read_text(encoding="utf-8-sig")
        texts[name] = text
        doc = loop.Doc(p, cfg["default_bundle"])
        docs[name] = doc

        # 파일 이름과 번호
        m = FILE_RE.match(p.name)
        if not m:
            errors.append(f"{tag}: 파일 이름은 '번호(세 자리)_영문소문자_이름.md' 여야 한다")
        else:
            if int(m.group(1)) < nxt:
                errors.append(f"{tag}: 번호가 이미 쓰인 번호 이하다. {nxt:03d} 부터 쓴다")
        if name in existing:
            errors.append(f"{tag}: 같은 이름의 문서가 이미 있다 ({existing[name]})")

        # 제목 줄
        first = next((ln for ln in text.splitlines() if ln.strip()), "")
        if not first.startswith(f"# {name}"):
            errors.append(f"{tag}: 첫 줄은 '# {name} — 한 줄 제목' 이어야 한다")

        # 칸 제목
        secs = sections(text)
        titles = [t for t, _ in secs]
        if titles != HEADINGS:
            errors.append(f"{tag}: 칸 제목이 양식과 다르다. 있어야 할 것: {HEADINGS} / 실제: {titles}")
        by_title = dict(secs)
        for h in HEADINGS:
            if h in by_title and not body_lines(by_title[h]):
                errors.append(f"{tag}: '## {h}' 칸이 비어 있다")

        # 묶음: 한 줄, 이름만
        if "묶음" in by_title:
            bl = body_lines(by_title["묶음"])
            if len(bl) > 1:
                errors.append(f"{tag}: '## 묶음' 칸에는 묶음 이름 한 줄만 쓴다 (루프는 첫 줄 전체를 이름으로 읽는다)")
            if doc.bundle_missing:
                errors.append(f"{tag}: 묶음을 읽지 못했다. 묶음 이름을 적는다")
            elif not BUNDLE_RE.fullmatch(doc.bundle):
                errors.append(f"{tag}: 묶음 이름 '{doc.bundle}' 에 띄어쓰기·괄호·설명이 있다. 한글·영문·숫자로 된 한 단어만 쓴다")

        # 선행 작업: 줄마다 '- 이름' 또는 '없음' 만
        if "선행 작업" in by_title:
            dl = body_lines(by_title["선행 작업"])
            plain = [ln.lstrip("-*").strip().strip("`").strip() for ln in dl]
            if plain == ["없음"]:
                pass
            else:
                for raw, val in zip(dl, plain):
                    if val == "없음":
                        errors.append(f"{tag}: 선행 작업 칸에 '없음'과 문서 이름을 함께 쓰지 않는다")
                    elif not re.fullmatch(r"[0-9A-Za-z가-힣_\-]+", val):
                        errors.append(
                            f"{tag}: 선행 작업 줄 '{raw}' 에 문서 이름 말고 다른 글자가 있다. "
                            "설명·쉼표·마침표·.md 는 쓰지 않는다 (루프가 엉뚱한 이름으로 읽는다)"
                        )
            if sorted(doc.deps) != sorted(v for v in plain if v != "없음") and plain != ["없음"]:
                errors.append(f"{tag}: 루프가 읽은 선행 작업 {doc.deps} 이 칸에 적힌 것과 다르다")

        # 참고 문서
        if "참고 문서" in by_title:
            refs = MD_PATH_RE.findall(by_title["참고 문서"])
            for must in ("docs/index.md", "docs/dev/architecture.md"):
                if must not in refs:
                    warns.append(f"{tag}: 참고 문서에 {must} 가 없다")
            if not any(r.startswith("docs/spec/") for r in refs):
                no_spec.append(tag)
            for r in refs:
                if r.startswith(("docs/design/", "docs/project/", "docs/archive/")):
                    errors.append(f"{tag}: 참고 문서 {r} 는 사람용 문서다. 구현하는 에이전트는 읽지 않는다. 필요한 내용은 사양이나 요구사항 칸에 옮겨 적는다")
                    continue
                if not (root / r).exists():
                    errors.append(f"{tag}: 참고 문서 {r} 가 없다. 있는 문서만 적는다 (연결 지점은 그 기능의 사양 문서 docs/spec/… 를 가리킨다)")
                elif tracked_set is not None and r not in tracked_set:
                    errors.append(f"{tag}: 참고 문서 {r} 가 {base} 에 커밋되어 있지 않다. 루프는 {base} 에서 딴 worktree 에서 일해서 이 문서를 읽지 못한다")

        # 완료 조건
        if "완료 조건" in by_title:
            cond = by_title["완료 조건"]
            for w in VAGUE:
                if w in cond:
                    warns.append(f"{tag}: 완료 조건에 '{w}' 가 있다. 테스트로 참/거짓을 가릴 수 있는 문장으로 바꾼다")
            if len([ln for ln in body_lines(cond) if ln.startswith("-")]) > 12:
                warns.append(f"{tag}: 완료 조건이 12개를 넘는다. 작업을 더 쪼갤지 본다")

    # 선행 작업이 실제로 있는지, 번호 순서, 묶음
    for name, doc in docs.items():
        own = number_of(name)
        for dep in doc.deps:
            if dep == name:
                errors.append(f"{name}.md: 자기 자신을 선행 작업으로 적었다")
            elif dep in docs:
                dn = number_of(dep)
                if own is not None and dn is not None and dn > own:
                    warns.append(f"{name}.md: 선행 작업 {dep} 의 번호가 더 크다. 먼저 할 작업에 작은 번호를 준다")
                if docs[dep].bundle != doc.bundle:
                    warns.append(
                        f"{name}.md: 선행 작업 {dep} 이 다른 묶음({docs[dep].bundle})이다. "
                        f"그 묶음이 {base} 에 합쳐진 뒤에야 이 작업이 시작된다. 의도한 것인지 확인한다"
                    )
            elif dep in existing:
                where = existing[dep]
                if where in UNFINISHED:
                    other = loop.Doc(root / "tasks" / where / f"{dep}.md", cfg["default_bundle"])
                    if other.bundle != doc.bundle:
                        warns.append(f"{name}.md: 선행 작업 {dep}({where}) 이 다른 묶음({other.bundle})이다. 그 묶음이 {base} 에 합쳐진 뒤에야 시작된다")
                    if where == "blocked":
                        warns.append(f"{name}.md: 선행 작업 {dep} 이 보류(blocked) 상태다. 풀릴 때까지 이 작업은 기다린다")
            else:
                errors.append(f"{name}.md: 선행 작업 {dep} 이 초안에도, tasks/ 에도, docs/history/ 에도 없다")

    # 순환 (초안 + 아직 안 끝난 기존 문서)
    graph = {name: list(doc.deps) for name, doc in docs.items()}
    for folder in UNFINISHED:
        d = root / "tasks" / folder
        if d.is_dir():
            for p in d.glob("*.md"):
                graph.setdefault(p.stem, loop.Doc(p, cfg["default_bundle"]).deps)
    cycles = loop.cycle_members(graph)
    for name, path in cycles.items():
        if name in docs:
            errors.append(f"{name}.md: 선행 작업 순환 ({' → '.join(path)})")

    # 묶음 안에서 지금 바로 시작할 수 있는 작업이 있는지
    bundles: dict[str, list[str]] = {}
    for name, doc in docs.items():
        bundles.setdefault(doc.bundle, []).append(name)
    finished = {n for n, w in existing.items() if w in ("done", "history")}
    for bundle, names in bundles.items():
        if not any(all(d in finished for d in docs[n].deps) for n in names):
            warns.append(f"묶음 '{bundle}': 선행 작업 없이 바로 시작할 수 있는 초안이 없다. 기존 작업을 기다리는 것이 맞는지 확인한다")

    # 출력
    print(f"프로젝트 폴더: {root}")
    print(f"이미 있는 문서의 가장 큰 번호 다음: {nxt:03d}\n")
    print("루프가 읽은 내용 (run_loop.py 의 Doc 클래스)")
    for name in sorted(docs, key=lambda n: (number_of(n) or 10**9, n)):
        d = docs[name]
        print(f"  {name}  | 묶음: {d.bundle}  | 선행 작업: {', '.join(d.deps) or '없음'}")
    print()
    if no_spec:
        warns.append(
            f"참고 문서에 사양 문서(docs/spec/…)가 없는 초안 {len(no_spec)}개: {', '.join(no_spec)}. "
            "사양 문서 없이 만든 것이면 요구사항 칸에 필요한 내용이 다 있는지 확인한다"
        )
    for w in warns:
        print(f"[경고] {w}")
    for e in errors:
        print(f"[오류] {e}")
    print(f"\n오류 {len(errors)}건, 경고 {len(warns)}건")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
