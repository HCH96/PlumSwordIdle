"""루프 관문: 두 커밋 사이의 변경이 프로젝트 규칙을 어기는지 기계로 검사한다. 읽기만 한다.

루프 스크립트가 두 번 부른다.
- 작업 하나가 테스트를 통과한 직후 (mode="task"): 루프 브랜치 끝 → 작업 중 상태의 스냅숏
- 루프를 통합 브랜치에 합치기 직전 (mode="loop"): 통합 브랜치 → 루프 브랜치 끝

위반이 하나라도 있으면 합치지 않는다. 에이전트가 판단하는 것이 아니라 스크립트가 판정하므로,
에이전트는 이 검사를 말로 넘어갈 수 없다. 판단이 필요한 것(테스트가 완료 조건을 정말 검사하는지 등)은
교차 리뷰 에이전트가 본다 (docs/dev/loop.md "검증").

직접 돌려 볼 때 (프로젝트 폴더에서):
  python tools/loop/loop_gate.py --base main --head integration
  python tools/loop/loop_gate.py --base <커밋> --head <커밋> --doc <작업 문서> --mode task

종료 코드: 0 위반 없음, 1 위반 있음, 2 실행할 수 없음
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

HISTORY = "docs/history"
EVIDENCE = "docs/evidence"
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
# 루프 자체와 그 검사 장치. 작업 문서로도 예외를 줄 수 없다 (사람이 직접 고친다)
LOOP_SELF = ("tools/loop/", "tools/skills/", ".claude/", ".codex/", ".agents/", ".github/", "CLAUDE.md", "AGENTS.md", ".gitignore", ".env")
LOOP_DOCS = ("docs/dev/loop.md", "docs/dev/task_template.md")
HUMAN_DOCS = ("docs/design/", "docs/project/", "docs/archive/")
TEST_DIR = "Assets/Tests/"
TEST_ATTR_RE = re.compile(r"\[\s*(Test|TestCase|TestCaseSource|UnityTest|Theory)\b[^\]]*\]")
METHOD_RE = re.compile(r"\b(?:public|internal|private|protected)\s+(?:static\s+|async\s+)*[\w<>\[\],.]+\s+(\w+)\s*\(")
ASSERT_RE = re.compile(r"\bAssert\s*\.|\bThrows\b|\bShould\b|\bExpect\b|\bLogAssert\s*\.")
WEAKEN_ADDED = [
    (r"\[\s*(Ignore|Explicit)\b", "테스트를 건너뛰게 함 ([Ignore]/[Explicit])"),
    (r"\bAssert\.(Pass|Ignore|Inconclusive)\b", "무조건 통과·건너뛰기 (Assert.Pass 등)"),
    (r"\bAssert\.(IsTrue|True)\(\s*true\s*\)|Assert\.That\(\s*true\s*[,)]", "항상 참인 검사"),
]
MARKS = ("테스트", "컴파일", "플레이 모드", "사람 확인")  # make-tasks 가 완료 조건 앞에 붙이는 표시
MARK_RE = re.compile(r"^\W{0,3}\[\s*(" + "|".join(MARKS) + r")\s*\]\W{0,2}\s*(.*)$", re.DOTALL)

# 작업 문서의 "## 관문 예외" 칸에 적을 수 있는 이름. 사람이 승인한 작업 문서만 예외를 줄 수 있다
EXCEPTION_IDS = {
    "asmdef": "어셈블리 정의(.asmdef, .asmref, csc.rsp) 변경",
    "scene": "씬·프리팹·에셋 파일(.unity, .prefab, .asset) 변경",
    "settings": "ProjectSettings/, Packages/ 변경",
}


class Git:
    def __init__(self, cwd: Path):
        self.cwd = cwd

    def run(self, *args: str) -> subprocess.CompletedProcess:
        return subprocess.run(
            ["git", "--no-optional-locks", "-c", "core.quotepath=false", *args],
            cwd=self.cwd, capture_output=True, text=True, encoding="utf-8", errors="replace",
        )  # fmt: skip

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


# ───────── 작업 문서 읽기 ─────────


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


def split_mark(condition: str) -> tuple[str, str]:
    """완료 조건 → (표시, 내용). 표시가 없으면 '' 이고, 테스트가 필요한 조건으로 본다."""
    m = MARK_RE.match(condition)
    return (m.group(1), m.group(2).strip()) if m else ("", condition)


def conditions(doc_text: str) -> list[tuple[str, str]]:
    return [split_mark(c) for c in bullets(section(doc_text, "완료 조건"))]


def doc_exceptions(doc_text: str) -> set[str]:
    """작업 문서의 "## 관문 예외" 칸에 적힌 예외 이름. 모르는 이름은 버린다."""
    found = set()
    for item in bullets(section(doc_text, "관문 예외")):
        word = re.split(r"[\s:：(,]", item.strip("` "), maxsplit=1)[0].strip("` ").lower()
        if word in EXCEPTION_IDS:
            found.add(word)
    return found


def report_items(doc_text: str, title: str) -> list[str]:
    """보고서의 소제목(### <제목>) 하나에 적힌 항목. '없음'은 뺀다."""
    report = section(doc_text, "보고서") or ""
    heads = list(re.finditer(r"^(#{3,5})\s+(.+?)\s*$", report, re.MULTILINE))
    for m in heads:
        if m.group(2).strip(" *:：").startswith(title):
            nxt = re.search(r"^#{2,5}\s", report[m.end():], re.MULTILINE)
            body = report[m.end():m.end() + nxt.start()] if nxt else report[m.end():]
            return [x for x in bullets(body) if x.strip(" .`*") not in ("없음", "없다", "해당 없음", "-", "")]
    return []


# ───────── 변경 읽기 ─────────


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
    diff = git.out("diff", "--no-renames", "--no-color", "--ignore-cr-at-eol", "-U0", a, b, "--", *(paths or ["."]))
    for line in diff.splitlines():
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
    for item in sorted(files | dirs):
        if item.endswith(".meta"):
            target = item[:-5]
            if target not in files and target not in dirs:
                problems.add(f"`{item}` — 짝이 되는 파일·폴더가 없는 .meta")
        elif item + ".meta" not in files:
            kind = "폴더" if item in dirs else "파일"
            problems.add(f"`{item}` — .meta 가 없는 {kind}")
    return problems


# ───────── 검사 ─────────


def _weakened(git: Git, a: str, b: str, tag: str = "") -> tuple[list[str], list[str]]:
    """a → b 사이에 테스트가 약해진 흔적. (위반, 확인 필요)"""
    bad: list[str] = []
    look: list[str] = []
    for st, path in name_status(git, a, b, TEST_DIR):
        if st.startswith("D") and path.endswith(".cs"):
            bad.append(f"`{path}` — 테스트 파일을 지움{tag}")
    before, after = all_tests(git, a), all_tests(git, b)
    bad += [f"`{x}` — 있던 테스트가 사라짐{tag}" for x in sorted(set(before) - set(after))]
    for path, ch in changed_lines(git, a, b, TEST_DIR).items():
        if not (path and path.endswith(".cs")):
            continue
        for no, code in ch["+"]:
            for pat, label in WEAKEN_ADDED:
                if re.search(pat, code) and not is_comment(code):
                    bad.append(f"`{path}:{no}` {label} — `{code.strip()[:100]}`{tag}")
        removed = [code.strip() for _, code in ch["-"] if ASSERT_RE.search(code)]
        if removed and git.show(a, path) is not None:
            added = [code.strip() for _, code in ch["+"] if ASSERT_RE.search(code)]
            look.append(
                f"`{path}` — 있던 검사(Assert) {len(removed)}줄을 지우거나 바꿈{tag}"
                + "".join(f"\n  - 지워짐: `{x[:110]}`" for x in removed[:8])
                + "".join(f"\n  - 새로 들어옴: `{x[:110]}`" for x in added[:8])
            )
    for key in sorted(after):
        body = after[key]
        if before.get(key) == body:
            continue
        n_after = len(ASSERT_RE.findall(body))
        if n_after == 0:
            bad.append(f"`{key}` — 검사(Assert)가 하나도 없는 테스트{tag}")
        elif key in before and n_after < len(ASSERT_RE.findall(before[key])):
            bad.append(f"`{key}` — 있던 테스트의 검사 수가 줄어듦 ({len(ASSERT_RE.findall(before[key]))} → {n_after}){tag}")
    return bad, look


def check(repo: Path, base: str, head: str, *, mode: str = "loop", doc_text: str | None = None,
          exceptions: set[str] | None = None) -> dict:  # fmt: skip
    """base → head 의 변경을 검사한다.

    mode="task": 작업 하나의 변경. doc_text 는 그 작업 문서. docs/history 는 건드리면 안 된다.
    mode="loop": 루프 전체의 변경. docs/history 에는 새 기록 추가만 허용한다.
    exceptions: 작업 문서의 "## 관문 예외"로 사람이 미리 승인한 이름 (EXCEPTION_IDS).

    돌려주는 값: {"violations": [{"rule", "text"}], "warnings": [str], "stats": {...}}
    """
    git = Git(repo)
    exceptions = set(exceptions or ())
    violations: list[dict] = []
    warnings: list[str] = []

    def bad(rule: str, items: list[str]) -> None:
        violations.extend({"rule": rule, "text": x} for x in items)

    eol = eol_only(git, base, head)
    changes = [(st, path) for st, path in name_status(git, base, head) if path not in eol]
    diff = changed_lines(git, base, head)

    # 루프 자체, 사람용 문서, 기록
    bad("루프 자체를 바꿈", [f"{st} `{p}`" for st, p in changes if p.startswith(LOOP_SELF) or p in LOOP_DOCS])
    bad("사람용 문서를 바꿈", [f"{st} `{p}`" for st, p in changes if p.startswith(HUMAN_DOCS)])
    if mode == "task":
        bad("증거 폴더를 바꿈 (루프 스크립트만 쓴다)", [f"{st} `{p}`" for st, p in changes if p.startswith(EVIDENCE + "/")])
        bad("작업 기록을 바꿈 (루프 스크립트만 쓴다)", [f"{st} `{p}`" for st, p in changes if p.startswith(HISTORY + "/")])
    else:
        bad("지난 작업 기록을 바꿈", [f"{st} `{p}`" for st, p in changes if p.startswith(HISTORY + "/") and st != "A"])
    if git.show(head, "CLAUDE.md") != git.show(head, "AGENTS.md") and git.show(base, "CLAUDE.md") == git.show(base, "AGENTS.md"):
        bad("루프 자체를 바꿈", ["`CLAUDE.md` 와 `AGENTS.md` 의 내용이 달라짐"])

    # 구조 규칙
    core_hits = []
    for path, ch in diff.items():
        if path and path.startswith(CORE_DIR) and path.endswith(".cs"):
            for no, code in ch["+"]:
                if is_comment(code):
                    continue
                for pat, label in CORE_BANNED:
                    if re.search(pat, code):
                        core_hits.append(f"`{path}:{no}` {label} — `{code.strip()[:100]}`")
                        break
    bad("PlumSword.Core 에 유니티 의존", core_hits)

    asm = [f"{st} `{p}`" for st, p in changes if p.endswith((".asmdef", ".asmref", "csc.rsp"))]
    core_asm = git.show(head, CORE_DIR + "PlumSword.Core.asmdef")
    if core_asm is not None and not re.search(r'"noEngineReferences"\s*:\s*true', core_asm):
        bad("PlumSword.Core 에 유니티 의존", ["`PlumSword.Core.asmdef` 의 `noEngineReferences` 가 켜져 있지 않다"])
    if "asmdef" not in exceptions:
        bad("어셈블리 정의를 바꿈", asm)
    elif asm:
        warnings.append("어셈블리 정의 변경 (작업 문서의 관문 예외로 허용됨): " + ", ".join(asm))

    scene = [f"{st} `{p}`" for st, p in changes if p.endswith(SCENE_EXTS) and not p.startswith(("ProjectSettings/", "Packages/"))]
    if "scene" not in exceptions:
        bad("씬·프리팹·에셋 파일을 바꿈", scene)
    elif scene:
        warnings.append("씬·프리팹·에셋 변경 (작업 문서의 관문 예외로 허용됨): " + ", ".join(scene))
    settings = [f"{st} `{p}`" for st, p in changes if p.startswith(("ProjectSettings/", "Packages/"))]
    if "settings" not in exceptions:
        bad("ProjectSettings·Packages 를 바꿈", settings)
    elif settings:
        warnings.append("ProjectSettings·Packages 변경 (작업 문서의 관문 예외로 허용됨): " + ", ".join(settings))

    bad(".meta 짝이 맞지 않음", sorted(meta_problems(git, head) - meta_problems(git, base)))

    # 테스트
    look: list[str] = []
    if mode == "loop":  # 작업 단계마다 비교해, 앞 작업이 쓴 테스트를 뒤 작업이 약하게 만든 것도 잡는다
        steps = loop_steps(git, base, head)
        for a, b, name in steps:
            w_bad, w_look = _weakened(git, a, b, f" (작업 {name})" if len(steps) > 1 else "")
            bad("테스트를 지우거나 약하게 만듦", w_bad)
            look += w_look
    else:
        w_bad, look = _weakened(git, base, head)
        bad("테스트를 지우거나 약하게 만듦", w_bad)
    warnings += look

    tests_before, tests_after = all_tests(git, base), all_tests(git, head)
    new_tests = sorted(set(tests_after) - set(tests_before))
    need: list[str] = []
    human: list[str] = []
    if doc_text is not None:
        conds = conditions(doc_text)
        need = [c for k, c in conds if k in ("", "테스트", "플레이 모드")]
        human = [c for k, c in conds if k == "사람 확인"]
        play = [c for k, c in conds if k == "플레이 모드"]
        if mode == "task":
            if need and not new_tests:
                bad("테스트가 필요한 완료 조건이 있는데 새 테스트가 없음", [f"테스트로 판정할 완료 조건 {len(need)}개, 새 테스트 0개"])
            if play and not any(p.startswith(TEST_DIR + "PlayMode/") for _, p in changes):
                bad("테스트가 필요한 완료 조건이 있는데 새 테스트가 없음", [f"[플레이 모드] 조건이 {len(play)}개인데 `{TEST_DIR}PlayMode/` 에 바뀐 파일이 없다"])
            if conds and not need and not any(k == "컴파일" for k, _ in conds):
                warnings.append("판정할 수 있는 완료 조건이 없다 ([사람 확인]뿐이다)")

    if "docs/dev/architecture.md" in {p for _, p in changes}:
        warnings.append("`docs/dev/architecture.md` 가 바뀜 (작업이 요구한 변경인지 본다)")
    if eol:
        shown = ", ".join(f"`{x}`" for x in sorted(eol)[:10])
        warnings.append(f"내용은 같고 줄 끝(CRLF/LF)만 바뀐 파일 {len(eol)}개: {shown}")

    code_files = [p for _, p in changes if not p.endswith(".meta")]
    return {
        "violations": violations,
        "warnings": warnings,
        "stats": {
            "files_changed": len(code_files),
            "files": code_files,
            "tests_before": len(tests_before),
            "tests_after": len(tests_after),
            "new_tests": new_tests,
            "need_test_conditions": len(need),
            "human_conditions": human,
            "screen_files": [p for p in code_files if p.startswith("Assets/") and not p.startswith((CORE_DIR, TEST_DIR))],
        },
    }


def loop_steps(git: Git, base: str, head: str) -> list[tuple[str, str, str]]:
    """루프 브랜치를 작업 완료 커밋(docs/history 에 문서를 더한 커밋)마다 끊는다: (이전 커밋, 완료 커밋, 작업 이름)."""
    mb = git.out("merge-base", base, head).strip() or base
    order = git.lines("rev-list", "--topo-order", "--reverse", f"{mb}..{head}")
    pos = {c: i for i, c in enumerate(order)}
    done = []
    for st, path in name_status(git, mb, head, HISTORY):
        if st == "A" and path.endswith(".md"):
            commits = git.lines("log", "--diff-filter=A", "--format=%H", f"{mb}..{head}", "--", path)
            if commits:
                done.append((pos.get(commits[-1], 10**9), commits[-1], Path(path).stem))
    steps, prev = [], mb
    for _, commit, name in sorted(done):
        steps.append((prev, commit, name))
        prev = commit
    tip = git.out("rev-parse", head).strip()
    if prev != tip:
        steps.append((prev, tip, "작업 완료 뒤의 커밋" if steps else "작업 구분 없음"))
    return steps


def format_violations(result: dict) -> str:
    """위반을 규칙별로 묶은 글. 에이전트에게 되돌려 주거나 보고서에 쓴다."""
    groups: dict[str, list[str]] = {}
    for v in result["violations"]:
        groups.setdefault(v["rule"], []).append(v["text"])
    lines = []
    for rule, items in groups.items():
        lines.append(f"[위반] {rule}")
        lines += [f"  - {x}" for x in items[:20]]
        if len(items) > 20:
            lines.append(f"  - … 외 {len(items) - 20}건")
    return "\n".join(lines)


def main() -> int:
    ap = argparse.ArgumentParser(description="두 커밋 사이의 변경이 프로젝트 규칙을 어기는지 검사한다 (읽기만 함)")
    ap.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2], help="저장소 폴더")
    ap.add_argument("--base", required=True, help="비교 기준 (예: main)")
    ap.add_argument("--head", required=True, help="검사할 쪽 (예: integration)")
    ap.add_argument("--mode", choices=["loop", "task"], default="loop")
    ap.add_argument("--doc", type=Path, help="작업 문서 (mode=task 일 때 완료 조건과 관문 예외를 읽는다)")
    args = ap.parse_args()
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass
    git = Git(args.root.resolve())
    for rev in (args.base, args.head):
        if not git.ok("rev-parse", "--verify", "--quiet", f"{rev}^{{commit}}"):
            print(f"[관문] 실행할 수 없음 — 커밋을 찾을 수 없음: {rev}")
            return 2
    doc_text = args.doc.read_text(encoding="utf-8-sig") if args.doc else None
    result = check(args.root.resolve(), args.base, args.head, mode=args.mode, doc_text=doc_text,
                   exceptions=doc_exceptions(doc_text) if doc_text else None)  # fmt: skip
    s = result["stats"]
    print(f"[관문] {args.base} → {args.head}: 바뀐 파일 {s['files_changed']}개, 테스트 {s['tests_before']} → {s['tests_after']}개")
    if result["violations"]:
        print(format_violations(result))
    for w in result["warnings"]:
        print(f"[확인 필요] {w}")
    print(f"[관문] 결과: {'위반 ' + str(len(result['violations'])) + '건' if result['violations'] else '위반 없음'}")
    return 1 if result["violations"] else 0


if __name__ == "__main__":
    sys.exit(main())
