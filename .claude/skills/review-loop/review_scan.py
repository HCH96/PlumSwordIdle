"""루프 리뷰(review-loop) 보조 스크립트. 읽기만 한다.

루프는 검증(테스트, 관문, 교차 리뷰)을 통과한 묶음을 통합 브랜치에 자동으로 합친다.
이 스크립트는 통합 브랜치에 쌓였지만 아직 main 에 올리지 않은 루프들을 main 과 비교하고,
사람이 main 에 올리기 전에 볼 사실을 마크다운으로 출력한다.
기계가 확실히 알 수 있는 것(바뀐 파일, 규칙 위반 흔적, 새 테스트 이름)만 모은다.
"완료 조건에 맞는 테스트인가", "하지 말 것을 건드렸는가" 같은 판단은 스킬을 따르는 쪽이 한다.

- 깃은 조회 명령만 쓴다 (for-each-ref, merge-base, rev-parse, rev-list, log, diff, show, ls-tree, worktree list, status).
- 파일을 만들거나 고치지 않는다. tasks/ 와 .loop/state.json 은 읽기만 한다. .env 는 읽지 않는다.

사용법 (프로젝트 폴더에서):
  python .claude/skills/review-loop/review_scan.py                     # main 에 아직 안 올린 루프 전체를 훑는다
  python .claude/skills/review-loop/review_scan.py --loop <날짜-시간>   # 합쳐진 루프 하나만 본다
  python .claude/skills/review-loop/review_scan.py --branch <브랜치>    # 합쳐지지 않은 브랜치(rejected/…, loop/…)를 본다
  python .claude/skills/review-loop/review_scan.py --root <폴더>        # 다른 폴더(예행 연습용 복제본)를 본다
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

DEFAULT_ROOT = Path(__file__).resolve().parents[3]
HISTORY = "docs/history"
TEMPLATE_HEADINGS = ("목표", "묶음", "선행 작업", "참고 문서", "요구사항", "완료 조건", "하지 말 것")
HEADING_RE = re.compile(r"^##\s+(.+?)\s*$", re.MULTILINE)  # run_loop.py 와 같은 규칙

# PlumSword.Core 에 들어오면 안 되는 것 (docs/dev/architecture.md "순수 C# 규칙이 지켜지는 방식")
CORE_DIR = "Assets/Scripts/Core/"
CORE_BANNED = [
    (r"\bUnityEngine\b", "UnityEngine 참조"),
    (r"\bUnityEditor\b", "UnityEditor 참조"),
    (r"\busing\s+(Unity|TMPro)\b", "유니티 패키지 using"),
    (r"\b(MonoBehaviour|ScriptableObject)\b", "유니티 기반 클래스"),
    (r"\[SerializeField\]", "SerializeField"),
    (r"\bDebug\.Log", "Debug.Log"),
    (r"\bTime\.(deltaTime|time|unscaledDeltaTime|fixedDeltaTime)\b", "Time.* (경과 시간은 인자로 받는다)"),
    (r"\bDateTime\.(Now|UtcNow|Today)\b", "현재 시각 직접 호출 (인자로 받는다)"),
    (r"\b(PlayerPrefs|JsonUtility)\b", "저장 API (읽기·쓰기는 Game 이 한다)"),
    (r"\b(Mathf|Vector[234](Int)?)\b", "Mathf/Vector (System.Math 를 쓴다)"),
]
SCENE_EXTS = (".unity", ".prefab", ".asset")
# 루프 자체를 바꾸는 경로
LOOP_SELF = ("tools/loop/", ".claude/", "CLAUDE.md", "AGENTS.md", ".gitignore", ".env")
LOOP_DOCS = ("docs/dev/loop.md", "docs/dev/task_template.md", "docs/dev/architecture.md")
TEST_DIR = "Assets/Tests/"
TEST_ATTR_RE = re.compile(r"\[\s*(Test|TestCase|TestCaseSource|UnityTest|Theory)\b[^\]]*\]")
METHOD_RE = re.compile(r"\b(?:public|internal|private|protected)\s+(?:static\s+|async\s+)*[\w<>\[\],.]+\s+(\w+)\s*\(")
WEAKEN_ADDED = [
    (r"\[\s*(Ignore|Explicit)\b", "테스트를 건너뛰게 함 ([Ignore]/[Explicit])"),
    (r"\bAssert\.(Pass|Ignore|Inconclusive)\b", "무조건 통과·건너뛰기 (Assert.Pass 등)"),
    (r"\bAssert\.(IsTrue|True)\(\s*true\s*\)|Assert\.That\(\s*true\s*[,)]", "항상 참인 검사"),
]
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


def name_status(git: Git, a: str, b: str, *paths: str) -> list[tuple[str, str]]:
    rows = []
    for line in git.lines("diff", "--no-renames", "--name-status", a, b, "--", *(paths or ["."])):
        st, _, path = line.partition("\t")
        rows.append((st.strip(), path.strip()))
    return rows


def eol_only(git: Git, a: str, b: str) -> set[str]:
    """내용은 같고 줄 끝(CRLF/LF)만 바뀐 파일."""
    modified = {path for st, path in name_status(git, a, b) if st == "M"}
    real = {line.split("\t")[-1] for line in git.lines("diff", "--no-renames", "--ignore-cr-at-eol", "--numstat", a, b)}
    return modified - real


def changed_lines(git: Git, a: str, b: str, *paths: str) -> dict[str, dict[str, list[tuple[int, str]]]]:
    """파일별로 더해진 줄(+)과 지워진 줄(-)을 줄 번호와 함께 돌려준다."""
    result: dict[str, dict[str, list[tuple[int, str]]]] = {}
    path, old_no, new_no = None, 0, 0
    old_path = None
    for line in git.out("diff", "--no-renames", "--no-color", "--ignore-cr-at-eol", "-U0", a, b, "--", *(paths or ["."])).splitlines():
        if line.startswith("--- "):
            old_path = line[6:] if line.startswith("--- a/") else None
        elif line.startswith("+++ "):
            path = line[6:] if line.startswith("+++ b/") else old_path
            result.setdefault(path, {"+": [], "-": []})
        elif line.startswith("@@"):
            m = re.match(r"@@ -(\d+)(?:,\d+)? \+(\d+)", line)
            if m:
                old_no, new_no = int(m.group(1)), int(m.group(2))
        elif path and line.startswith("+"):
            result[path]["+"].append((new_no, line[1:]))
            new_no += 1
        elif path and line.startswith("-"):
            result[path]["-"].append((old_no, line[1:]))
            old_no += 1
    return result


def is_comment(code: str) -> bool:
    return code.strip().startswith(("//", "*", "/*"))


def test_methods(text: str | None) -> dict[str, str]:
    """테스트 파일에서 [Test] 류가 붙은 메서드 이름 → 본문."""
    methods: dict[str, str] = {}
    if not text:
        return methods
    for attr in TEST_ATTR_RE.finditer(text):
        m = METHOD_RE.search(text, attr.end())
        if not m or m.group(1) in methods:
            continue
        start = text.find("{", m.end())
        arrow = text.find("=>", m.end())
        if start < 0 or (0 <= arrow < start):
            end = text.find(";", m.end())
            methods[m.group(1)] = text[m.end():end if end >= 0 else len(text)]
            continue
        depth, i = 0, start
        while i < len(text):
            if text[i] == "{":
                depth += 1
            elif text[i] == "}":
                depth -= 1
                if depth == 0:
                    break
            i += 1
        methods[m.group(1)] = text[start:i + 1]
    return methods


def all_tests(git: Git, rev: str) -> dict[str, str]:
    """rev 의 Assets/Tests 아래 모든 테스트: '파일이름.메서드' → 본문."""
    tests: dict[str, str] = {}
    for path in git.lines("ls-tree", "-r", "--name-only", rev, "--", TEST_DIR):
        if path.endswith(".cs"):
            for name, body in test_methods(git.show(rev, path)).items():
                tests[f"{Path(path).stem}.{name}"] = body
    return tests


def meta_problems(git: Git, rev: str) -> set[str]:
    files = set(git.lines("ls-tree", "-r", "--name-only", rev, "--", "Assets"))
    dirs = set()
    for f in files:
        parts = f.split("/")
        for i in range(2, len(parts)):
            dirs.add("/".join(parts[:i]))
    problems = set()
    for item in sorted((files | dirs)):
        if item.endswith(".meta"):
            target = item[:-5]
            if target not in files and target not in dirs:
                problems.add(f"`{item}` — 짝이 되는 파일·폴더가 없는 .meta")
        elif item + ".meta" not in files:
            kind = "폴더" if item in dirs else "파일"
            problems.add(f"`{item}` — .meta 가 없는 {kind}")
    return problems


def md_list(items: list[str], empty: str = "(없음)") -> str:
    return "\n".join(f"- {x}" for x in items) if items else f"- {empty}"


# ───────── 본체 ─────────


def main() -> int:
    ap = argparse.ArgumentParser(description="루프 브랜치 리뷰에 필요한 사실을 모은다 (읽기만 함)")
    ap.add_argument("--root", type=Path, default=DEFAULT_ROOT, help="프로젝트 폴더 (기본: 이 스크립트가 있는 저장소)")
    ap.add_argument("--loop", help="합쳐진 루프 하나만 본다 (루프 ID: 날짜-시간. 태그 이름도 된다)")
    ap.add_argument("--branch", help="합쳐지지 않은 브랜치를 본다 (rejected/<ID>, loop/<ID>)")
    args = ap.parse_args()
    for stream in (sys.stdout, sys.stderr):
        stream.reconfigure(encoding="utf-8", errors="replace")

    root: Path = args.root.resolve()
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
    unmerged = [b for b in loops if not git.ok("merge-base", "--is-ancestor", b, integ)]
    rejected = git.lines("for-each-ref", "--format=%(refname:short)", f"refs/heads/{rej_prefix}")
    pending = merged_loops(main_b, integ) if integ != main_b else []
    state_active = state.get("status") == "active"

    p("# 루프 리뷰 자료\n")
    p("## 1. 확인할 대상\n")
    p(f"- 사람이 일하는 브랜치: `{main_b}`, 통합 브랜치: `{integ}`" + (" (통합 브랜치가 아직 없다)" if integ == main_b else ""))
    if integ != main_b:
        ahead = git.out("rev-list", "--count", f"{main_b}..{integ}").strip()
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
    if args.loop:
        loop_id = args.loop.removeprefix(tag_prefix).removeprefix(loop_prefix)
        hit = next((x for x in merged_loops(git.out("rev-list", "--max-parents=0", integ).split()[0], integ) if x["id"] == loop_id), None)
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
        mb = git.out("merge-base", integ, branch).strip()
    elif pending:
        mode, branch = "all", integ
        tip = git.out("rev-parse", integ).strip()
        mb = git.out("merge-base", main_b, integ).strip()
        base = main_b
    elif len(unmerged) == 1 and not state_active:
        mode, branch = "branch", unmerged[0]
        tip = git.out("rev-parse", branch).strip()
        mb = git.out("merge-base", integ, branch).strip()
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

    base_tip = git.out("rev-parse", integ if mode == "branch" else main_b).strip()
    is_state_loop = mode == "branch" and state.get("loop_branch") == branch
    active = is_state_loop and state_active
    what = {"all": f"`{main_b}` 에 올리지 않은 루프 전체 (`{main_b}`..`{integ}`)",
            "loop": f"합쳐진 루프 하나 (`{branch}`)",
            "branch": f"합쳐지지 않은 브랜치 `{branch}` (`{integ}` 와 비교)"}[mode]
    p(f"- **확인할 대상: {what}** ({mb[:7]}..{tip[:7]})")
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
        ahead = git.out("rev-list", "--count", f"{mb}..{integ}").strip()
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
            p(f"| `{x['tag'] or x['sha'][:7]}` | {x['bundle']} | {'`git show ' + integ + ':' + report + '`' if has else '**없다**'} | {len(shots)}장 |")
        p(f"\n- 색인: `git show {integ}:docs/evidence/index.md`")
        p("- 화면 갈무리를 직접 보려면 파일로 꺼낸다: `git show <통합 브랜치>:docs/evidence/<루프>/screenshots/<파일> > <임시 폴더>/<파일>` (작업 폴더를 바꾸지 않는다)")
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
    from_main = set(git.lines("log", "--format=%h %s", f"{mb}..{main_b}")) if mode != "branch" else set()
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
    steps: list[tuple[str, str, str]] = []  # (이전 커밋, 완료 커밋, 작업 이름)
    for i, t in enumerate(tasks, 1):
        text, commit = t["text"], t["commit"] or tip
        p(f"### 작업 {i}. `{t['name']}`\n")
        p(f"- 변경 범위: `git diff {prev[:7]} {commit[:7]}` (작업 문서: `git show {tip[:7]}:{t['path']}`)")
        p(f"- 구현: {t['agents']}" + (" — **여러 번 다시 시도했다 (테스트 실패, 관문 위반, 리뷰의 고침 요구가 모두 시도로 세어진다). 테스트를 억지로 맞춘 흔적이 없는지 본다**" if t["max_try"] >= 3 else ""))
        p(f"- 리뷰: {t['review']}" + (" — **같은 에이전트가 리뷰했다 (교차 리뷰가 아니다). 더 꼼꼼히 본다**" if "같은 에이전트" in t["review"] else ""))
        steps.append((prev, commit, t["name"]))
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
    p("\n## 4. 규칙 점검 (기계 검사)\n")
    if mode != "branch":
        p("합쳐진 루프는 작업마다, 그리고 합치기 직전에 관문(`tools/loop/loop_gate.py`)을 통과했다. 아래는 같은 종류의 검사를 **이 범위 전체**에 다시 돌린 것이다. "
          f"`[위반]` 이 나오면 관문이 놓쳤거나, 작업 문서의 관문 예외로 허용됐거나, 루프 밖에서 들어온 변경이다. 직접 돌려 볼 때: `python tools/loop/loop_gate.py --base {main_b} --head {integ}`\n")
    p("`[위반]` 은 규칙에 어긋난 것이 확실한 것, `[확인 필요]` 는 사람이 보고 판단할 것, `[이상 없음]` 은 흔적이 없는 것이다.\n")
    diff = changed_lines(git, mb, tip)
    counts = {"위반": 0, "확인 필요": 0}
    if tasks and (tasks[-1]["commit"] or tip) != tip:
        steps.append((tasks[-1]["commit"], tip, "작업 완료 뒤의 커밋"))
    if not steps:
        steps.append((mb, tip, "작업 구분 없음"))

    def who(path: str) -> str:
        names = [t["name"] for t in tasks if path in t.get("files", ())]
        return f" (작업 {', '.join(names)})" if names else ""

    def check(title: str, level: str, items: list[str]) -> None:
        if items:
            counts[level] += 1
            p(f"**[{level}] {title}**\n")
            p(md_list(items) + "\n")
        else:
            p(f"**[이상 없음] {title}**\n")

    # 사람이 main 에서 직접 고친 파일(통합 브랜치가 main 을 합치며 들어온 것)은 루프의 변경이 아니다
    human_changed = set(git.lines("log", "--format=", "--name-only", f"{mb}..{main_b}")) if mode != "branch" else set()

    core_hits = []
    for path, ch in diff.items():
        if path and path.startswith(CORE_DIR) and path.endswith(".cs"):
            for no, code in ch["+"]:
                if is_comment(code):
                    continue
                for pat, label in CORE_BANNED:
                    if re.search(pat, code):
                        core_hits.append(f"`{path}:{no}` {label} — `{code.strip()[:100]}`{who(path)}")
                        break
    check("PlumSword.Core 에 유니티 의존", "위반", core_hits)

    asm = [f"{st} `{path}`{who(path)}" for st, path in all_changes if path.endswith((".asmdef", ".asmref", "csc.rsp"))]
    core_asm = git.show(tip, CORE_DIR + "PlumSword.Core.asmdef") or ""
    if not re.search(r'"noEngineReferences"\s*:\s*true', core_asm):
        asm.insert(0, "**`PlumSword.Core.asmdef` 의 `noEngineReferences` 가 켜져 있지 않다.** Core 의 순수 C# 규칙을 강제하는 장치가 꺼졌다")
    check("어셈블리 정의 변경 (docs/dev/architecture.md 를 함께 고쳤는지, 작업 문서에 근거가 있는지 본다)", "위반", asm)

    scene = [f"{st} `{path}`{who(path)}" for st, path in all_changes
             if path.endswith(SCENE_EXTS) or path.startswith(("ProjectSettings/", "Packages/"))]
    check(".unity / .prefab / .asset, ProjectSettings, Packages 변경", "위반", scene)

    check("Assets 아래 .meta 짝", "위반", sorted(meta_problems(git, tip) - meta_problems(git, mb)))

    loop_self = [f"{st} `{path}`{who(path)}" for st, path in all_changes if path.startswith(LOOP_SELF) and path not in human_changed]
    hist_bad = [f"{st} `{path}` (이번 루프가 새로 넣은 기록이 아닌데 바뀜)" for st, path in all_changes
                if path.startswith(HISTORY + "/") and st != "A"]
    check("루프 자체를 바꾸는 변경 (tools/loop, CLAUDE.md, AGENTS.md, .claude, .gitignore, 지난 기록)", "위반", loop_self + hist_bad)
    rules_same = git.show(tip, "CLAUDE.md") == git.show(tip, "AGENTS.md")
    if not rules_same and (git.show(mb, "CLAUDE.md") == git.show(mb, "AGENTS.md")):
        check("CLAUDE.md 와 AGENTS.md 내용이 달라짐", "위반", ["두 파일은 같은 내용을 유지해야 한다"])
    check("규칙 문서 변경 (작업이 요구한 변경인지 본다)", "확인 필요",
          [f"{st} `{path}`" for st, path in all_changes if path in LOOP_DOCS])

    weak = []
    for a, b, step in steps:
        tag = f" (작업 {step})" if len(steps) > 1 else ""
        for st, path in name_status(git, a, b, TEST_DIR):
            if st.startswith("D") and not path.endswith(".meta"):
                weak.append(f"`{path}` — 테스트 폴더의 파일을 지움{tag}")
        s_before, s_after = all_tests(git, a), all_tests(git, b)
        weak += [f"`{x}` — 있던 테스트가 사라짐{tag}" for x in sorted(set(s_before) - set(s_after))]
        for path, ch in changed_lines(git, a, b, TEST_DIR).items():
            if not (path and path.endswith(".cs")):
                continue
            for no, code in ch["+"]:
                for pat, label in WEAKEN_ADDED:
                    if re.search(pat, code) and not is_comment(code):
                        weak.append(f"`{path}:{no}` {label} — `{code.strip()[:100]}`{tag}")
            removed = [code.strip() for _, code in ch["-"] if re.search(r"\bAssert\.", code)]
            if removed and git.show(a, path) is not None:
                added = [code.strip() for _, code in ch["+"] if re.search(r"\bAssert\.", code)]
                weak.append(f"`{path}` — 있던 검사(Assert) {len(removed)}줄을 지우거나 바꿈{tag}. `git diff {a[:7]} {b[:7]} -- {path}` 로 본다"
                            + "".join(f"\n  - 지워짐: `{x[:110]}`" for x in removed)
                            + "".join(f"\n  - 새로 들어옴: `{x[:110]}`" for x in added))
        for key in sorted(set(s_after)):
            if s_before.get(key) != s_after[key] and not re.search(r"\bAssert\b|\bThrows\b|\bShould\b|\bExpect\b", s_after[key]):
                weak.append(f"`{key}` — 검사(Assert)가 하나도 없는 테스트{tag}")
    before, after = all_tests(git, mb), all_tests(git, tip)
    check("테스트를 지우거나 약하게 만든 변경", "확인 필요", weak)
    eol_items = [f"`{x}`" for x in sorted(eol)]
    if len(eol_items) > 10:
        eol_items = eol_items[:10] + [f"… 그 밖에 {len(eol) - 10}개 (`git diff --stat {mb[:7]} {tip[:7]}` 와 `--ignore-cr-at-eol` 을 붙인 결과를 비교한다)"]
    check("내용은 같고 줄 끝(CRLF/LF)만 바뀐 파일 (위 점검과 목록에서는 뺐다. 합치면 main 의 파일도 통째로 바뀐다)", "확인 필요", eol_items)
    p(f"- 테스트 수: `{base}` 쪽 {len(before)}개 → 확인할 대상 {len(after)}개")
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
        if mode == "all":
            p(f"git merge --ff-only {integ}            # 전부 올린다")
        else:
            p(f"git merge --ff-only {branch}   # 이 루프까지만 올린다 (앞 루프도 함께 올라간다)")
            p(f"git merge --ff-only {integ}            # 뒤 루프까지 전부 올린다")
        for x in in_range[:-1] if mode == "all" else []:
            if x["tag"]:
                p(f"git merge --ff-only {x['tag']}   # 여기(묶음 {x['bundle']})까지만 올린다")
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
            p(f"git merge {branch}")
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
