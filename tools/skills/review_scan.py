"""루프 리뷰(review-loop) 보조 스크립트. 읽기만 한다.

루프는 검증(테스트, 관문, 교차 리뷰)을 통과한 묶음을 통합 브랜치에 자동으로 합친다.
이 스크립트는 통합 브랜치에 쌓였지만 아직 main 에 올리지 않은 루프들을 main 과 비교하고,
사람이 main 에 올리기 전에 볼 사실을 마크다운으로 출력한다.
기계가 확실히 알 수 있는 것(바뀐 파일, 규칙 위반 흔적, 새 테스트 이름)만 모은다.
"완료 조건에 맞는 테스트인가", "하지 말 것을 건드렸는가" 같은 판단은 스킬을 따르는 쪽이 한다.

- 깃은 조회 명령만 쓴다 (for-each-ref, merge-base, rev-parse, rev-list, log, diff, show, ls-tree, worktree list, status).
- 파일을 만들거나 고치지 않는다. tasks/ 와 .loop/state.json 은 읽기만 한다. .env 는 읽지 않는다.

사용법 (프로젝트 폴더에서):
  python tools/skills/review_scan.py                     # main 에 아직 안 올린 루프 전체를 훑는다
  python tools/skills/review_scan.py --loop <날짜-시간>   # 합쳐진 루프 하나만 본다
  python tools/skills/review_scan.py --branch <브랜치>    # 합쳐지지 않은 브랜치(rejected/…, loop/…)를 본다
  python tools/skills/review_scan.py --root <폴더>        # 다른 폴더(예행 연습용 복제본)를 본다
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

sys.dont_write_bytecode = True

from workflow_context import project_root, revision

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "loop"))
import loop_gate

DEFAULT_ROOT = Path(__file__).resolve().parents[2]
HISTORY = "docs/history"
TEMPLATE_HEADINGS = ("목표", "묶음", "선행 작업", "참고 문서", "요구사항", "완료 조건", "관문 예외", "하지 말 것")
HEADING_RE = re.compile(r"^##\s+(.+?)\s*$", re.MULTILINE)  # run_loop.py 와 같은 규칙

# 규칙과 기계 검사는 자동 관문을 공통 원본으로 쓴다.
CORE_DIR = loop_gate.CORE_DIR
TEST_DIR = loop_gate.TEST_DIR
TEST_ATTR_RE = loop_gate.TEST_ATTR_RE
METHOD_RE = loop_gate.METHOD_RE
FILE_TOKEN_RE = re.compile(r"[\w가-힣./\\\-]+\.(?:cs|asmdef|asmref|md|json|py|unity|prefab|asset|inputactions|txt)\b")


# ───────── 기본 도구 ─────────


class Git:
    def __init__(self, cwd: Path):
        self.cwd = cwd

    def run(self, *args: str) -> subprocess.CompletedProcess:
        return subprocess.run(
            ["git", "--no-optional-locks", "-c", "core.quotepath=false", *args],
            cwd=self.cwd, capture_output=True, text=True, encoding="utf-8", errors="replace",
        )

    def out(self, *args: str) -> str:
        p = self.run(*args)
        return p.stdout if p.returncode == 0 else ""

    def ok(self, *args: str) -> bool:
        return self.run(*args).returncode == 0

    def lines(self, *args: str) -> list[str]:
        return [x for x in self.out(*args).splitlines() if x.strip()]

    def show(self, rev: str, path: str) -> str | None:
        p = self.run("show", f"{rev}:{path}")
        return p.stdout if p.returncode == 0 else None


def section(text: str, prefix: str) -> str | None:
    matches = list(HEADING_RE.finditer(text))
    for i, m in enumerate(matches):
        if m.group(1).strip().startswith(prefix):
            end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
            return text[m.end():end].strip()
    return None


def bullets(body: str | None) -> list[str]:
    if not body:
        return []
    items: list[str] = []
    for line in body.splitlines():
        s = line.strip()
        if not s:
            continue
        if re.match(r"^([-*]|\d+[.)])\s+", s) or not items:
            items.append(re.sub(r"^([-*]|\d+[.)])\s+", "", s))
        else:
            items[-1] += " " + s  # 이어지는 줄
    return items


def tail_after_template(text: str) -> str:
    """양식 칸 뒤에 덧붙은 부분 (보고서, 보류 사유, 답변, 루프 기록)."""
    for m in HEADING_RE.finditer(text):
        if not m.group(1).strip().startswith(TEMPLATE_HEADINGS):
            return text[m.start():].strip()
    return ""


NONE_WORDS = ("없음", "없다", "해당 없음", "-", "")
REPORT_TITLES = ("한 일", "바뀐 파일", "가정", "사람이 확인할 것", "테스트 결과")  # 루프 프롬프트가 정한 보고서 소제목
MARKS = ("테스트", "컴파일", "플레이 모드", "사람 확인")  # make-tasks 가 완료 조건 앞에 붙이는 표시
MARK_RE = re.compile(r"^\W{0,3}\[\s*(" + "|".join(MARKS) + r")\s*\]\W{0,2}\s*(.*)$", re.DOTALL)


def split_mark(condition: str) -> tuple[str, str]:
    """완료 조건 → (표시, 내용). 표시가 없으면 '' 이고, 테스트가 필요한 조건으로 본다."""
    m = MARK_RE.match(condition)
    return (m.group(1), m.group(2).strip()) if m else ("", condition)


def report_items(tail: str, title: str) -> tuple[list[str], str]:
    """보고서에서 소제목 하나의 항목을 찾는다. 돌려주는 값: (항목, 찾은 방법).

    먼저 정해진 소제목(### <제목>)을 찾고, 없으면 어림으로 찾는다 (제목이 굵은 글씨나 목록 항목으로 적힌 경우).
    찾은 방법은 '소제목', '어림', '없음' 중 하나다.
    """
    report = tail.split("## 루프 기록")[0]
    heads = list(re.finditer(r"^(#{3,5})\s+(.+?)\s*$", report, re.MULTILINE))
    for i, m in enumerate(heads):
        if m.group(2).strip(" *:：").startswith(title):
            nxt = re.search(r"^#{2,5}\s", report[m.end():], re.MULTILINE)
            body = report[m.end():m.end() + nxt.start()] if nxt else report[m.end():]
            return [x for x in bullets(body) if x.strip(" .`*") not in NONE_WORDS], "소제목"
    found = _guess_items(report, title)
    return found or [], ("어림" if found is not None else "없음")


def _guess_items(report: str, title: str) -> list[str] | None:
    """소제목이 없을 때 쓰는 어림 방식. 제목 줄 자체를 못 찾으면 None."""
    lines = report.splitlines()
    found: list[str] = []
    seen = False
    key = re.escape(title.split()[0]) if title != "사람이 확인할 것" else "사람이\\s*확인"
    label = re.compile(r"^\s*(#{2,5}\s*|[-*]\s*)?\**\s*" + key + r"[^:：\n]*\**\s*[:：]?\s*\**\s*(.*)$")
    stop = re.compile(r"^(#{2,5}\s|\*\*|[-*]\s*\**\s*(한 일|바뀐 파일|가정|사람이|테스트|루프|남은|참고))")
    i = 0
    while i < len(lines):
        m = label.match(lines[i])
        if m and not lines[i].startswith((" ", "\t")):
            seen = True
            if m.group(2).strip():
                found.append(m.group(2).strip())
            i += 1
            while i < len(lines) and not stop.match(lines[i]):
                s = lines[i].strip()
                if s:
                    found.append(re.sub(r"^([-*]|\d+[.)])\s+", "", s))
                i += 1
            continue
        i += 1
    if not seen:
        return None
    return [x for x in found if x.strip(" .`*") not in NONE_WORDS]


name_status = loop_gate.name_status


eol_only = loop_gate.eol_only


changed_lines = loop_gate.changed_lines


is_comment = loop_gate.is_comment


test_methods = loop_gate.test_methods


all_tests = loop_gate.all_tests


meta_problems = loop_gate.meta_problems


def md_list(items: list[str], empty: str = "(없음)") -> str:
    return "\n".join(f"- {x}" for x in items) if items else f"- {empty}"


# ───────── 본체 ─────────


def main() -> int:
    ap = argparse.ArgumentParser(description="루프 브랜치 리뷰에 필요한 사실을 모은다 (읽기만 함)")
    ap.add_argument("--root", type=Path, default=DEFAULT_ROOT, help="프로젝트 폴더 (기본: 이 스크립트가 있는 저장소)")
    ap.add_argument("--loop", help="합쳐진 루프 하나만 본다 (루프 ID: 날짜-시간. 태그 이름도 된다)")
    ap.add_argument("--branch", help="합쳐지지 않은 브랜치를 본다 (rejected/<ID>, loop/<ID>)")
    ap.add_argument("--base", help="고정할 비교 기준 커밋 (--head 와 함께 사용)")
    ap.add_argument("--head", help="고정할 리뷰 대상 커밋 (--base 와 함께 사용)")
    args = ap.parse_args()
    if bool(args.base) != bool(args.head) or (args.head and (args.loop or args.branch)) or (args.loop and args.branch):
        ap.error("--base/--head 는 함께 쓰고 --loop/--branch 와 섞지 않는다")
    for stream in (sys.stdout, sys.stderr):
        stream.reconfigure(encoding="utf-8", errors="replace")

    try:
        root = project_root(args.root)
    except (ValueError, OSError) as error:
        ap.exit(2, f"실행할 수 없음: {error}\n")
    git = Git(root)
    out: list[str] = []
    p = out.append

    cfg = {}
    try:
        cfg = json.loads((root / "tools/loop/loop_config.json").read_text(encoding="utf-8"))
        local = root / "tools/loop/loop_config.local.json"
        if local.exists():
            cfg.update(json.loads(local.read_text(encoding="utf-8")))
    except (OSError, ValueError):
        pass
    main_b = cfg.get("base_branch", "main")
    integ = cfg.get("integration_branch") or main_b
    if not git.ok("rev-parse", "--verify", "--quiet", f"refs/heads/{integ}"):
        integ = main_b  # 통합 브랜치가 아직 없다 (루프를 새 방식으로 돌린 적이 없다)
    main_sha = revision(root, main_b)
    integ_sha = revision(root, integ)
    tag_prefix = cfg.get("merge_tag_prefix", "loop-merged/")
    rej_prefix = cfg.get("rejected_branch_prefix", "rejected/")
    base = integ  # 합쳐지지 않은 브랜치를 볼 때의 비교 기준. 아래에서 대상에 따라 바뀐다
    loop_prefix = cfg.get("loop_branch_prefix", "loop/")
    task_prefix = cfg.get("task_branch_prefix", "task/")
    default_bundle = cfg.get("default_bundle", "기본")
    wt = (root / cfg.get("worktree_path", "../PlumSwordIdle_loop")).resolve()

    state: dict = {}
    state_note = ""
    try:
        sp = root / ".loop/state.json"
        if sp.exists():
            state = json.loads(sp.read_text(encoding="utf-8"))
        else:
            state_note = "상태 파일 없음"
    except (OSError, ValueError) as e:
        state_note = f"상태 파일을 읽지 못함: {e}"

    # ── 1. 확인할 대상 ──
    def merged_loops(a: str, b: str) -> list[dict]:
        """a..b 사이에 통합 브랜치에 합쳐진 루프 (병합 커밋의 제목으로 찾는다)."""
        found = []
        for line in git.lines("log", "--first-parent", "--merges", "--reverse", "--format=%H%x09%s", f"{a}..{b}"):
            sha, _, subject = line.partition("\t")
            m = re.match(r"루프 합침: \S*?([0-9]{8}-[0-9]{6})\s*(?:\(묶음 (.+)\))?", subject)
            if m:
                tag = tag_prefix + m.group(1)
                found.append({"id": m.group(1), "bundle": m.group(2) or "-", "sha": sha,
                              "tag": tag if git.ok("rev-parse", "--verify", "--quiet", f"refs/tags/{tag}") else ""})
        return found

    loops = git.lines("for-each-ref", "--format=%(refname:short)", f"refs/heads/{loop_prefix}")
    unmerged = [b for b in loops if not git.ok("merge-base", "--is-ancestor", b, integ_sha)]
    rejected = git.lines("for-each-ref", "--format=%(refname:short)", f"refs/heads/{rej_prefix}")
    pending = merged_loops(main_sha, integ_sha) if integ != main_b else []
    state_active = state.get("status") == "active"

    p("# 루프 리뷰 자료\n")
    p("## 1. 확인할 대상\n")
    p(f"- 사람이 일하는 브랜치: `{main_b}`, 통합 브랜치: `{integ}`" + (" (통합 브랜치가 아직 없다)" if integ == main_b else ""))
    if integ != main_b:
        ahead = git.out("rev-list", "--count", f"{main_sha}..{integ_sha}").strip()
        p(f"- `{integ}` 는 `{main_b}` 보다 커밋 {ahead}개 앞서 있다. 아직 `{main_b}` 에 올리지 않은 루프 {len(pending)}개:")
        p(md_list([f"`{x['tag'] or x['sha'][:7]}` 묶음 {x['bundle']} — 증거 `docs/evidence/{x['id']}/report.md`" for x in pending],
                  "없다"))
    p(f"- 검증에서 떨어져 합쳐지지 않은 루프 브랜치: {', '.join(f'`{b}`' for b in rejected) or '없음'}")
    p(f"- 진행 중이 아닌데 `{integ}` 에 안 합쳐진 루프 브랜치: "
      f"{', '.join(f'`{b}`' for b in unmerged if not (state_active and b == state.get('loop_branch'))) or '없음'}")
    if state:
        p(f"- 상태 파일: 루프 브랜치 `{state.get('loop_branch')}`, 상태 **{state.get('status')}**, 묶음 `{state.get('bundle')}`"
          + (f", 끝난 시각 {state['finished_at']}" if state.get("finished_at") else ""))
        if state.get("last_stop"):
            p(f"- 마지막으로 멈춘 이유: {state['last_stop'].get('at')} — {state['last_stop'].get('reason')}")
    else:
        p(f"- 상태 파일: {state_note}")

    # 대상: (보여 줄 이름, 끝 커밋, 시작 커밋, 종류)
    mode = ""
    branch = tip = mb = ""
    if args.head:
        mode, branch = "all", args.head
        tip, mb = revision(root, args.head), revision(root, args.base)
        base = mb
        if not git.ok("merge-base", "--is-ancestor", mb, tip):
            ap.error("고정 기준은 리뷰 대상의 선행 커밋이어야 한다")
        if not git.ok("merge-base", "--is-ancestor", tip, integ_sha):
            ap.error("반영할 대상은 통합 브랜치에 포함된 커밋이어야 한다. 미병합 브랜치는 --branch 로 본다")
    elif args.loop:
        loop_id = args.loop.removeprefix(tag_prefix).removeprefix(loop_prefix)
        hit = next((x for x in merged_loops(git.out("rev-list", "--max-parents=0", integ_sha).split()[0], integ_sha) if x["id"] == loop_id), None)
        if not hit:
            p(f"\n**루프 `{loop_id}` 의 병합 커밋을 `{integ}` 에서 찾지 못했다.** 위 목록의 ID 를 확인한다.")
            print("\n".join(out))
            return 1
        mode, branch, tip = "loop", hit["tag"] or hit["sha"][:7], hit["sha"]
        mb = git.out("rev-parse", f"{tip}^1").strip()
        base = f"{branch}^1"
    elif args.branch:
        if not git.ok("rev-parse", "--verify", "--quiet", f"refs/heads/{args.branch}"):
            p(f"\n**브랜치 `{args.branch}` 가 없다.**")
            print("\n".join(out))
            return 1
        mode, branch = "branch", args.branch
        tip = git.out("rev-parse", branch).strip()
        mb = git.out("merge-base", integ_sha, tip).strip()
    elif integ_sha != main_sha and not git.ok("merge-base", "--is-ancestor", integ_sha, main_sha):
        mode, branch = "all", integ
        tip = integ_sha
        mb = git.out("merge-base", main_sha, tip).strip()
        base = main_b
    elif len(unmerged) == 1 and not state_active:
        mode, branch = "branch", unmerged[0]
        tip = git.out("rev-parse", branch).strip()
        mb = git.out("merge-base", integ_sha, tip).strip()
    if not mode:
        p("")
        if rejected or unmerged:
            p("**올릴 루프는 없다.** 합쳐지지 않은 브랜치를 보려면 `--branch <브랜치>` 로 다시 실행한다.")
        else:
            p(f"**확인할 것이 없다.** `{main_b}` 에 올리지 않은 루프도, 합쳐지지 않은 루프 브랜치도 없다.")
        if state_active:
            p(f"루프가 진행 중이다 (`{state.get('loop_branch')}`, 묶음 `{state.get('bundle')}`). 묶음이 끝나 통합 브랜치에 합쳐지면 볼 수 있다.")
        print("\n".join(out))
        return 0

    base_tip = integ_sha if mode == "branch" else main_sha
    is_state_loop = mode == "branch" and state.get("loop_branch") == branch
    active = is_state_loop and state_active
    what = {"all": f"`{main_b}` 에 올리지 않은 루프 전체 (`{main_b}`..`{integ}`)",
            "loop": f"합쳐진 루프 하나 (`{branch}`)",
            "branch": f"합쳐지지 않은 브랜치 `{branch}` (`{integ}` 와 비교)"}[mode]
    if args.head:
        what = f"고정한 리뷰 범위 (`{mb}`..`{tip}`). 위 현황 목록 전체가 아니라 이 범위만 확인"
    p(f"- **확인할 대상: {what}** ({mb[:7]}..{tip[:7]})")
    p(f"- 리뷰 기준 SHA: `{mb}`")
    p(f"- 리뷰 대상 SHA: `{tip}`")
    p(f"- 확인 시점 main SHA: `{main_sha}`")
    p(f"- 같은 범위 재조회: `python tools/skills/review_scan.py --base {mb} --head {tip}`" if mode != "branch" else "- 미병합 브랜치 자료이며 main 반영용 승인이 아니다")
    if mode == "branch" and branch.startswith(rej_prefix):
        p("- 이 브랜치는 합치기 전 검증에서 떨어진 루프다. 그 루프의 작업 문서는 보류(blocked)로 가 있고, 보류 사유에 떨어진 이유가 적혀 있다.")
    if mode != "branch" and state_active:
        p(f"- 참고: 루프가 진행 중이다 (`{state.get('loop_branch')}`, 묶음 `{state.get('bundle')}`). 여기서 보는 것은 **이미 합쳐진 루프뿐**이고, 진행 중인 묶음은 들어 있지 않다.")

    if active:
        p("\n### 루프가 아직 진행 중이다 (active)\n")
        p("끝나지 않은 묶음이다. 아래 작업이 남아 있고, 지금 보는 내용은 **중간 상태**다. 계속 볼지 사용자에게 묻는다.\n")
        bundle = state.get("bundle") or default_bundle
        remaining = []
        for folder in ("doing", "todo", "wait", "blocked"):
            d = root / "tasks" / folder
            for doc in sorted(d.glob("*.md")) if d.exists() else []:
                try:
                    text = doc.read_text(encoding="utf-8-sig")
                except OSError:
                    continue
                first = next(iter(bullets(section(text, "묶음"))), "")
                doc_bundle = first.strip("` ") or default_bundle
                if doc_bundle == "없음":
                    doc_bundle = default_bundle
                if doc_bundle == bundle:
                    remaining.append(f"`{doc.name}` ({folder})")
        p(md_list(remaining, "이번 묶음에 남은 문서가 없다. 루프를 한 번 실행하면 끝난 것으로 바뀐다"))
        if state.get("current"):
            p(f"- 처리 중인 작업: `{state['current'].get('doc')}`")

    # worktree
    p("\n### worktree\n")
    wt_ready = False
    if not (wt / ".git").exists():
        p(f"- `{wt}` 가 없다. 테스트를 다시 돌릴 수 없다.")
    else:
        wgit = Git(wt)
        wt_head = wgit.out("rev-parse", "HEAD").strip()
        wt_branch = wgit.out("rev-parse", "--abbrev-ref", "HEAD").strip()
        dirty = wgit.lines("status", "--porcelain")
        p(f"- 위치: `{wt}`")
        p(f"- HEAD: {wt_head[:7]} ({'브랜치 없음(detached)' if wt_branch == 'HEAD' else wt_branch}), "
          f"확인할 대상의 끝과 {'**같다**' if wt_head == tip else '**다르다**'}")
        p(f"- 커밋 안 된 변경: {len(dirty)}건" + (f" (예: `{dirty[0].strip()}`)" if dirty else ""))
        wt_ready = wt_head == tip and not dirty and not state_active
        if state_active:
            p("- 테스트 다시 돌리기: **하지 않는다** (루프 진행 중. 루프가 이 worktree 를 쓰고 있다)")
        elif wt_ready:
            p(f"- 테스트 다시 돌리기: 가능. `python tools/loop/run_unity_tests.py --project \"{wt}\"`")
        else:
            p("- 테스트 다시 돌리기: **지금 상태로는 안 된다** (worktree 가 확인할 대상의 끝이 아니거나 변경이 남아 있음). 돌려도 이 대상의 결과가 아니다.")

    # ── 2. 묶음 요약 ──
    history_added = [path for st, path in name_status(git, mb, tip, HISTORY) if st == "A" and path.endswith(".md")]
    order = git.lines("rev-list", "--topo-order", "--reverse", f"{mb}..{tip}")
    pos = {c: i for i, c in enumerate(order)}
    tasks = []
    for path in history_added:
        commits = git.lines("log", "--diff-filter=A", "--format=%H", f"{mb}..{tip}", "--", path)
        commit = commits[-1] if commits else ""
        tasks.append({"path": path, "name": Path(path).stem, "commit": commit, "text": git.show(tip, path) or ""})
    tasks.sort(key=lambda t: pos.get(t["commit"], 10**9))

    processed = {Path(x.get("doc", "")).stem: x for x in state.get("processed", [])} if is_state_loop else {}
    p("\n## 2. 묶음 요약\n")
    bundle_names = sorted({(bullets(section(t["text"], "묶음")) or [default_bundle])[0] for t in tasks})
    p(f"- 묶음: {', '.join(bundle_names) or state.get('bundle') or '-'}")
    p(f"- 커밋 {len(order)}개, 완료된 작업 {len(tasks)}건")
    stat = git.lines("diff", "--ignore-cr-at-eol", "--shortstat", mb, tip)  # 줄 끝만 바뀐 것은 세지 않는다
    p(f"- `{base}` 대비 변경: {stat[0].strip() if stat else '없음'}")
    if mode == "branch" and mb != base_tip:
        ahead = git.out("rev-list", "--count", f"{mb}..{integ_sha}").strip()
        p(f"- 주의: 이 브랜치를 딴 뒤 `{integ}` 에 커밋 {ahead}개가 더 생겼다.")
    in_range = merged_loops(mb, tip) if mode != "branch" else []
    if in_range:
        p("\n### 이 범위의 루프와 증거\n")
        p("루프마다 스크립트가 남긴 증거가 있다. **먼저 읽는다.** 사람이 볼 것, 가정, 화면 갈무리, 리뷰어가 만든 완료 조건별 테스트 표가 들어 있다.\n")
        p("| 루프 (태그) | 묶음 | 증거 | 화면 갈무리 |")
        p("| --- | --- | --- | --- |")
        for x in in_range:
            report = f"docs/evidence/{x['id']}/report.md"
            has = git.ok("cat-file", "-e", f"{tip}:{report}")
            shots = [f for f in git.lines("ls-tree", "-r", "--name-only", tip, "--", f"docs/evidence/{x['id']}/screenshots") if f.endswith(".png")]
            p(f"| `{x['tag'] or x['sha'][:7]}` | {x['bundle']} | {'`git show ' + tip + ':' + report + '`' if has else '**없다**'} | {len(shots)}장 |")
        p(f"\n- 색인: `git show {tip}:docs/evidence/index.md`")
        p(f'- 화면 갈무리: `python tools/skills/export_evidence.py --revision {tip} --path docs/evidence/<루프>/screenshots/<파일> --output <임시 폴더>/<파일>`')
    p("\n| 순서 | 작업 | 구현 에이전트와 시도 | 리뷰 | 완료 커밋 |")
    p("| --- | --- | --- | --- | --- |")
    for i, t in enumerate(tasks, 1):
        record = section(t["text"], "루프 기록") or ""
        m = re.search(r"작업 에이전트\s*[:：]\s*(.+)", record)
        t["agents"] = m.group(1).strip() if m else (processed.get(t["name"], {}).get("agents") or "기록 없음")
        rv = re.search(r"^- 리뷰\s*[:：]\s*(.+)$", record, re.MULTILINE)
        t["review"] = rv.group(1).strip() if rv else "기록 없음 (리뷰 도입 전이거나 리뷰를 끔)"
        tries = [int(x) for x in re.findall(r"시도 (?:\d+~)?(\d+)", t["agents"])]
        t["max_try"] = max(tries) if tries else 0
        title = (t["text"].splitlines() or [""])[0].lstrip("# ").strip()
        p(f"| {i} | {title or t['name']} | {t['agents']} | {t['review']} | {t['commit'][:7]} |")
    if not tasks:
        p("| - | 완료된 작업이 없다 (`docs/history/` 에 새 문서 없음) | - | - | - |")
    mismatch = sorted(set(processed) ^ {t["name"] for t in tasks}) if is_state_loop else []
    if mismatch:
        p(f"\n- 주의: 상태 파일의 처리 목록과 `docs/history/` 새 문서가 다르다: {', '.join(mismatch)}")
    # 스크립트가 만드는 커밋(작업 완료, 증거, 루프 병합, main 을 통합 브랜치에 합친 병합)이 아닌 것은 누가 만든 것인지 봐야 한다
    script_subject = re.compile(r"^(작업 완료: |증거: |루프 합침: |보류\(작업 중 상태\)|중단 복구\(작업 중 상태\)|.*\(작업 중 상태\): |Merge branch )")
    odd = [x for x in git.lines("log", "--format=%h %s", f"{mb}..{tip}") if not script_subject.match(x.split(" ", 1)[-1])]
    from_main = set(git.lines("log", "--format=%h %s", f"{mb}..{main_sha}")) if mode != "branch" else set()
    odd = [x for x in odd if x not in from_main]
    if odd:
        p(f"\n- 주의: 루프 스크립트가 만든 것으로 보이지 않는 커밋 {len(odd)}개가 이 범위에 있다. `git show` 로 본다:")
        p(md_list([f"`{x}`" for x in odd]))

    p("\n### 폴더별 바뀐 파일 수\n")
    eol = eol_only(git, mb, tip)
    all_changes = [(st, path) for st, path in name_status(git, mb, tip) if path not in eol]
    groups: dict[str, list[int]] = {}
    for st, path in all_changes:
        if path.endswith(".meta"):
            continue
        parts = path.split("/")
        key = "/".join(parts[:3]) if parts[0] == "Assets" and len(parts) > 3 else ("/".join(parts[:2]) if len(parts) > 2 else parts[0] if len(parts) == 1 else parts[0] + "/")
        g = groups.setdefault(key, [0, 0, 0])
        g["AMD".index(st[0]) if st[0] in "AMD" else 1] += 1
    p("| 위치 | 새 파일 | 고침 | 지움 |")
    p("| --- | --- | --- | --- |")
    for key in sorted(groups):
        a, m_, d = groups[key]
        p(f"| `{key}` | {a} | {m_} | {d} |")
    p("\n(.meta 파일과 줄 끝만 바뀐 파일은 세지 않았다)")

    # ── 3. 작업별 ──
    p("\n## 3. 작업별 자료\n")
    p("판단은 직접 한다: 보고서와 실제 변경이 맞는지, 완료 조건마다 테스트가 있는지, 하지 말 것을 건드렸는지.\n")
    prev = mb
    for i, t in enumerate(tasks, 1):
        text, commit = t["text"], t["commit"] or tip
        p(f"### 작업 {i}. `{t['name']}`\n")
        p(f"- 변경 범위: `git diff {prev[:7]} {commit[:7]}` (작업 문서: `git show {tip[:7]}:{t['path']}`)")
        p(f"- 구현: {t['agents']}" + (" — **여러 번 다시 시도했다 (테스트 실패, 관문 위반, 리뷰의 고침 요구가 모두 시도로 세어진다). 테스트를 억지로 맞춘 흔적이 없는지 본다**" if t["max_try"] >= 3 else ""))
        p(f"- 리뷰: {t['review']}" + (" — **같은 에이전트가 리뷰했다 (교차 리뷰가 아니다). 더 꼼꼼히 본다**" if "같은 에이전트" in t["review"] else ""))
        task_eol = eol_only(git, prev, commit)
        changes = [(st, path) for st, path in name_status(git, prev, commit)
                   if path != t["path"] and path not in task_eol and not path.startswith(("docs/evidence/", HISTORY + "/"))]
        t["files"] = {path for _, path in changes}
        p("\n**실제로 바뀐 파일**\n")
        p(md_list([f"{st} `{path}`" for st, path in changes if not path.endswith(".meta")]))
        metas = sum(1 for _, path in changes if path.endswith(".meta"))
        if metas:
            p(f"- (.meta {metas}개는 줄임)")
        if task_eol:
            p(f"- (줄 끝만 바뀐 파일 {len(task_eol)}개는 줄임. 4번 규칙 점검 참고)")

        tail = tail_after_template(text)
        if "보고서" not in "".join(m.group(1) for m in HEADING_RE.finditer(tail)):
            p("\n**[확인 필요] `## 보고서` 절이 없다.**")
        # 보고서에 적힌 파일과 실제 변경 비교 (어림)
        report_only = tail.split("## 루프 기록")[0]
        mentioned = {tok.replace("\\", "/").lstrip("./") for tok in FILE_TOKEN_RE.findall(report_only)}
        real = [path for _, path in changes if not path.endswith(".meta")]
        hit = lambda a, b: a == b or a.endswith("/" + b) or b.endswith("/" + a)
        unreported = [path for path in real if not any(hit(path, tok) for tok in mentioned)]
        phantom = [tok for tok in sorted(mentioned) if not any(hit(path, tok) for path in real)
                   and tok != Path(t["path"]).name and not tok.endswith(t["name"] + ".md")]
        p("\n**보고서와 실제 변경 비교** (파일 이름으로 어림한 것)\n")
        p(md_list([f"보고서에 없는데 바뀜: `{x}`" for x in unreported]
                  + [f"보고서에 나오는데 이 작업에서 바뀌지 않음: `{x}` (참고로만 언급한 것일 수 있다)" for x in phantom],
                  "파일 목록은 맞는다"))

        conds = [split_mark(c) for c in bullets(section(text, "완료 조건"))]
        how = {"": "테스트 필요 (표시 없음)", "테스트": "테스트 필요", "플레이 모드": "플레이 모드 테스트 필요",
               "컴파일": "루프가 판정함, 따로 테스트를 찾지 않는다", "사람 확인": "사람이 본다, 테스트를 찾지 않는다"}
        p("\n**완료 조건**\n")
        p(md_list([f"{n}) {'[' + k + '] ' if k else ''}{c} — _{how[k]}_" for n, (k, c) in enumerate(conds, 1)],
                  "완료 조건 칸이 비어 있다"))
        need = [c for k, c in conds if k in ("", "테스트", "플레이 모드")]
        play = [c for k, c in conds if k == "플레이 모드"]
        t["human"] = [c for k, c in conds if k == "사람 확인"]
        t["need_test"] = len(need)
        before, after = all_tests(git, prev), all_tests(git, commit)
        new_tests = sorted(set(after) - set(before))
        gone = sorted(set(before) - set(after))
        p("\n**이 작업에서 생긴 테스트**\n")
        p(md_list([f"`{x}`" for x in new_tests],
                  "**새 테스트가 없다**" if need else "새 테스트가 없다 (테스트가 필요한 완료 조건도 없다)"))
        if new_tests and len(new_tests) < len(need):
            p(f"- **테스트가 필요한 완료 조건은 {len(need)}개인데 새 테스트는 {len(new_tests)}개다.** 빠진 조건이 있는지 본다")
        if play and not any(path.startswith(TEST_DIR + "PlayMode/") for path in t["files"]):
            p(f"- **[플레이 모드] 조건이 {len(play)}개인데 `{TEST_DIR}PlayMode/` 에 바뀐 파일이 없다**")
        if conds and not any(k == "컴파일" for k, _ in conds) and not need:
            p("- 판정할 수 있는 완료 조건이 없다 ([사람 확인]뿐이다). 작업 문서가 잘못 만들어진 것이다")
        if gone:
            p(md_list([f"**사라진 테스트**: `{x}`" for x in gone]))
        t["new_tests"], t["gone_tests"] = new_tests, gone

        p("\n**하지 말 것**\n")
        p(md_list(bullets(section(text, "하지 말 것")), "적힌 것이 없다"))
        found, found_how = report_items(tail, "가정")
        t["assumptions"] = found
        p(f"\n**가정** ({'보고서 소제목에서 읽음' if found_how == '소제목' else '소제목이 없어 어림으로 찾음' if found_how == '어림' else '항목을 찾지 못함'})\n")
        p(md_list(found, "보고서에 가정이 없다고 적혀 있거나 '가정' 항목을 찾지 못했다. 아래 원문을 직접 확인하고, "
                         "코드에 적히지 않은 결정(임시 수치, 스스로 정한 동작)이 없는지 본다"))
        t["human_report"], human_how = report_items(tail, "사람이 확인할 것")
        p(f"\n**사람이 확인할 것** ({'보고서 소제목에서 읽음' if human_how == '소제목' else '소제목이 없어 어림으로 찾음' if human_how == '어림' else '보고서에 이 항목이 없다'})\n")
        p(md_list(t["human_report"], "없음"))
        if t["human"] and not t["human_report"]:
            p(f"- **완료 조건에 [사람 확인]이 {len(t['human'])}개 있는데 보고서의 \"사람이 확인할 것\"이 비어 있다.** 완료 조건 쪽 항목으로 본다")
        missing_titles = [x for x in REPORT_TITLES if report_items(tail, x)[1] != "소제목"]
        if missing_titles:
            p(f"\n- 참고: 보고서에 정해진 소제목이 없는 것: {', '.join(missing_titles)} (예전 양식이거나 에이전트가 양식을 따르지 않았다)")
        p("\n**보고서와 그 뒤 기록 (원문)**\n")
        p("\n".join("> " + line for line in (tail or "(양식 뒤에 덧붙은 내용이 없다)").splitlines()))
        p("")
        prev = commit

    # ── 4. 규칙 점검 ──
    p("\n## 4. 규칙 점검 (공통 관문)\n")
    p("자동 관문의 검사 함수를 작업 단계별로 호출한다. 관문 예외는 그 작업 기록에 있는 것만 적용한다. "
      "기록에 적힌 예외가 사전 승인된 것인지는 증거와 함께 확인한다.\n")
    counts = {"위반": 0, "확인 필요": 0}
    for a, b, name in loop_gate.loop_steps(loop_gate.Git(root), mb, tip):
        task = next((t for t in tasks if t["commit"] == b), None)
        document = task["text"] if task else None
        result = loop_gate.check(root, a, b, mode="loop", doc_text=document,
                                 exceptions=loop_gate.doc_exceptions(document) if document else None)
        if result["violations"]:
            counts["위반"] += len(result["violations"])
            p(f"**[위반] {name}**\n")
            p(loop_gate.format_violations(result))
        for warning in result["warnings"]:
            counts["확인 필요"] += 1
            p(f"- [확인 필요] {name}: {warning}")
    if not counts["위반"]:
        p("- [이상 없음] 공통 관문 규칙 위반 없음")
    evidence_changes = [f"{st} `{path}`" for st, path in all_changes if path.startswith("docs/evidence/")]
    if evidence_changes:
        counts["확인 필요"] += 1
        p("- [확인 필요] 증거 변경은 루프 스크립트가 만든 커밋인지 커밋별로 확인한다. 파일 위치만으로 생성 주체를 검증한 것은 아니다.")

    def who(path: str) -> str:
        names = [t["name"] for t in tasks if path in t.get("files", ())]
        return f" (작업 {', '.join(names)})" if names else ""

    no_test_tasks = [t["name"] for t in tasks if t.get("need_test") and not t.get("new_tests")]
    if no_test_tasks:
        counts["확인 필요"] += 1
        p(f"- **[확인 필요] 테스트가 필요한 완료 조건이 있는데 새 테스트가 하나도 없는 작업**: {', '.join(no_test_tasks)}")
    skipped = [t["name"] for t in tasks if not t.get("need_test") and not t.get("new_tests")]
    if skipped:
        p(f"- 새 테스트가 없지만 문제 삼지 않은 작업 (완료 조건이 [컴파일]·[사람 확인]뿐): {', '.join(skipped)}")

    # ── 5. 사람이 에디터에서 볼 것 ──
    p("\n## 5. 사람이 직접 볼 후보\n")
    p("테스트로 판정하지 못하는 것이다. 먼저 증거의 화면 갈무리(실행 후 1초·10초·30초)로 보고, 갈무리로 알 수 없는 것(누르는 동작, 움직임, 느낌)은 에디터에서 플레이해 본다.\n")
    p("**완료 조건의 [사람 확인]** (작업 문서를 만들 때 사람이 보기로 정한 것)\n")
    p(md_list([f"`{t['name']}`: {c}" for t in tasks for c in t.get("human", [])]))
    p("\n**보고서의 \"사람이 확인할 것\"** (에이전트가 적은 것)\n")
    p(md_list([f"`{t['name']}`: {c}" for t in tasks for c in t.get("human_report", [])]))
    game = [f"{st} `{path}`{who(path)}" for st, path in all_changes
            if path.startswith("Assets/") and not path.startswith((CORE_DIR, TEST_DIR)) and not path.endswith(".meta")]
    p("\n**바뀐 화면 코드·리소스** (위 목록에 없어도 화면이 달라졌을 수 있다)\n")
    p(md_list(game, "화면 코드·리소스 변경이 없다"))

    # ── 6. 명령 ──
    p("\n## 6. 명령 (docs/dev/loop.md \"사람이 확인하고 main 에 올리기\" 기준)\n")
    if mode != "branch":
        last_tag = in_range[-1]["tag"] if in_range and in_range[-1]["tag"] else ""
        p(f"올릴 때 (프로젝트 폴더의 `{main_b}` 에서. 루프가 돌고 있어도 된다):\n")
        p("```")
        if mode == "all" and mb == main_sha:
            p(f"git merge --ff-only {tip}   # 승인된 리뷰 대상까지만 반영")
        else:
            p(f"python tools/skills/review_scan.py --base {main_sha} --head {tip}")
            p("# 위 누적 범위를 리뷰한 뒤 승인받는다. 이번 부분 리뷰만으로 main 에 반영하지 않는다")
        p("```")
        p("\n푸시는 사용자가 직접 한다 (`git push`). 올린 뒤 에디터에서 플레이해 보고 푸시하는 것을 권한다.\n")
        p("되돌릴 때 (푸시하기 전):\n")
        p("```")
        for x in in_range:
            if x["tag"]:
                p(f"git revert -m 1 {x['tag']}   # ({main_b} 에 올린 뒤) 묶음 {x['bundle']} 만 되돌린다")
        p("```")
        p(f"\n- 뒤 루프가 되돌리는 루프의 코드에 기대고 있으면 `git revert` 가 충돌한다. 그때는 올리기 전에, **루프를 멈춘 뒤** "
          f"`git branch -f {integ} <남길 마지막 루프의 태그>` 로 통합 브랜치를 되돌린다 (되돌릴 수 없는 일이라 사용자가 직접 한다). "
          f"버린 루프의 작업을 다시 하려면 그 문서를 `tasks/done/` 에서 `tasks/todo/` 로 옮긴다.")
        if last_tag:
            p(f"- 이 범위의 마지막 루프 태그: `{last_tag}`")
    else:
        task_branches = git.lines("for-each-ref", "--format=%(refname:short)", f"refs/heads/{task_prefix}")
        mine = [b for b in task_branches if git.ok("merge-base", "--is-ancestor", b, branch)]
        if branch.startswith(rej_prefix):
            p("검증에서 떨어진 루프다. 합치지 않는다. 작업은 보류로 가 있으니 `answer-blocked` 로 답하면 새 루프에서 처음부터 다시 처리한다.\n")
            p("다 확인했으면 브랜치를 지운다 (되돌릴 수 없다. 사용자가 직접 한다):\n")
            p("```")
            p(f"git branch -D {branch}")
            p("```")
        else:
            p(f"통합 브랜치에 합쳐지지 않은 루프 브랜치다. 자동 합치기(`auto_merge`)가 꺼져 있을 때의 흐름이다. 프로젝트 폴더의 `{main_b}` 에서:\n")
            p("```")
            p(f"# 미병합 브랜치는 main 에 직접 합치지 않는다. 공통 절차에 따라 통합·검증 후 리뷰한다")
            p(f"git branch -d {branch}")
            if mine:
                p(f"git branch -d {' '.join(mine)}")
            p("```")
            p("\n합치지 않고 버릴 때 (되돌릴 수 없다. 사용자가 직접 한다):\n")
            p("```")
            p(f"git branch -D {branch}" + (f" {' '.join(mine)}" if mine else ""))
            p("```")
            p("그리고 `.loop/state.json` 과 이 루프의 `tasks/done/` 문서를 지운다: "
              + (", ".join(f"`{t['name']}.md`" for t in tasks) or "(없음)"))

    p("\n## 요약\n")
    p(f"- 기계 검사: 위반 {counts['위반']}종, 확인 필요 {counts['확인 필요']}종")
    p(f"- 가정 {sum(len(t.get('assumptions', [])) for t in tasks)}개 (작업별 자료 참고)")
    p(f"- 대상: {what}")
    p(f"- 루프 상태: {state.get('status', '상태 파일 없음')}" + (" (진행 중인 묶음은 이 자료에 들어 있지 않다)" if state_active and mode != "branch" else ""))
    print("\n".join(out))
    return 0


if __name__ == "__main__":
    sys.exit(main())
