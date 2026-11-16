"""루프 증거: 루프 하나가 통합 브랜치에 합쳐질 때 남기는 사람용 기록을 쓴다.

docs/evidence/<루프 ID>/report.md      사람이 읽는 보고서 (판정, 테스트, 완료 조건별 테스트, 가정, 화면 갈무리)
docs/evidence/<루프 ID>/results.json   같은 내용의 원자료
docs/evidence/<루프 ID>/screenshots/   플레이 모드 테스트가 남긴 화면 갈무리
docs/evidence/index.md                 루프마다 한 줄. 사람은 이 문서부터 본다

루프 스크립트(run_loop.py)만 부른다. 에이전트는 이 폴더를 고치지 않는다 (관문이 막는다).
"""

from __future__ import annotations

import datetime as _dt
import json
import re
import shutil
from pathlib import Path

EVIDENCE_REL = "docs/evidence"
INDEX_HEAD = """# 루프 증거 색인

자동 개발 루프가 통합 브랜치에 합친 루프마다 한 줄씩 쌓인다. **루프 스크립트만 쓴다.** 사람이 main 에 올리기 전에 이 문서부터 본다.

- 줄마다 보고서(`<루프>/report.md`)에 판정 근거, 완료 조건별 테스트, 에이전트가 스스로 정한 가정, 화면 갈무리가 있다.
- 루프 하나는 통합 브랜치의 병합 커밋 하나이고, `{tag_prefix}<루프>` 태그가 그 커밋을 가리킨다.
- 확인하고 올리기, 되돌리기는 `docs/dev/loop.md` "사람이 확인하고 main 에 올리기" 절을 본다.

| 루프 | 묶음 | 합친 시각 | 작업 | 테스트 (통과/전체) | 리뷰 | 사람이 볼 것 | 보고서 |
| --- | --- | --- | --- | --- | --- | --- | --- |
"""


def _cell(text: str) -> str:
    return re.sub(r"\s+", " ", str(text or "")).replace("|", "\\|").strip()


def _tests_text(summary: dict) -> str:
    parts = []
    for platform in ("EditMode", "PlayMode"):
        c = summary.get(platform)
        if c and "total" in c:
            parts.append(f"{platform} {c.get('passed', 0)}/{c.get('total', 0)}")
    return ", ".join(parts) or "기록 없음"


def _review_text(task: dict) -> str:
    r = task.get("review") or {}
    if not r:
        return "기록 없음"
    if r.get("skipped"):
        return "리뷰 안 함 (설정)"
    who = f"{r.get('reviewer', '?')} ({'교차' if r.get('cross') else '같은 에이전트'})"
    rounds = f", 고침 {r['fix_rounds']}회" if r.get("fix_rounds") else ""
    return f"{who}: {'통과' if r.get('verdict') == 'pass' else r.get('verdict', '?')}{rounds}"


def write(
    wt: Path,
    *,
    loop_id: str,
    loop_branch: str,
    bundle: str,
    base: str,
    main: str,
    tag: str,
    tag_prefix: str,
    tasks: list[dict],
    gate: dict,
    test_summary: dict,
    screenshots: list[Path],
    discarded: str = "",
    blocked: list[str] | None = None,
) -> str:
    """증거를 worktree 에 쓰고 보고서의 저장소 기준 경로를 돌려준다. 다시 불러도 같은 결과가 되게 덮어쓴다."""
    folder = wt / EVIDENCE_REL / loop_id
    shot_dir = folder / "screenshots"
    if shot_dir.exists():
        shutil.rmtree(shot_dir)
    folder.mkdir(parents=True, exist_ok=True)
    shot_names = []
    if screenshots:
        shot_dir.mkdir()
        for src in screenshots:
            shutil.copyfile(src, shot_dir / src.name)
            shot_names.append(src.name)

    now = f"{_dt.datetime.now():%Y-%m-%d %H:%M}"
    humans: list[str] = []
    assumptions: list[str] = []
    for t in tasks:
        name = Path(t.get("doc", "")).stem
        seen = set()
        for item in (t.get("human_conditions") or []) + (t.get("human_report") or []):
            key = re.sub(r"\W+", "", item)[:60]
            if key not in seen:
                seen.add(key)
                humans.append(f"`{name}`: {item}")
        assumptions += [f"`{name}`: {a}" for a in t.get("assumptions") or []]
        for f in (t.get("review") or {}).get("findings") or []:
            if f.get("severity") == "human":
                humans.append(f"`{name}` (리뷰): {f.get('text', '')}")

    L: list[str] = []
    p = L.append
    p(f"# 루프 증거: {loop_branch} (묶음 {bundle})\n")
    p(f"- 판정: **통합 브랜치(`{base}`)에 합침** — 관문 위반 0건, 합치기 전 전체 테스트 통과")
    p(f"- 합친 시각: {now}")
    p(f"- 병합 커밋: 태그 `{tag}` (`git show {tag}`)")
    p(f"- 작업 {len(tasks)}건, 바뀐 파일 {gate['stats']['files_changed']}개 (.meta 제외)")
    p(f"- 테스트 수: {gate['stats']['tests_before']} → {gate['stats']['tests_after']}개. 합치기 전 실행: {_tests_text(test_summary)}")
    if blocked:
        p(f"- 이 묶음에서 **보류로 남아 합쳐지지 않은 작업**: {', '.join(blocked)}")
    p("")
    p("이 문서는 루프 스크립트가 썼다. 테스트 결과와 관문은 스크립트가 직접 판정한 것이고, "
      "\"완료 조건별 테스트\"와 리뷰 의견은 교차 리뷰 에이전트의 판단이다. 가정과 화면은 사람이 본다.\n")

    p("## 사람이 볼 것\n")
    p("테스트로 판정하지 못한 것이다. 아래 화면 갈무리와 함께 본다.\n")
    p("\n".join(f"- {x}" for x in humans) if humans else "- 없음")
    p("\n## 에이전트가 스스로 정한 것 (가정)\n")
    p("사양과 작업 문서에 없어 에이전트가 정한 것 전부다. 마음에 들지 않으면 후속 작업으로 고친다.\n")
    p("\n".join(f"- {x}" for x in assumptions) if assumptions else "- 없음")

    p("\n## 화면 갈무리\n")
    if shot_names:
        p("합치기 직전의 코드로 게임을 실행해 찍은 것이다 (플레이 모드 테스트, 파일 이름의 숫자는 실행 후 지난 게임 시간).\n")
        for name in shot_names:
            p(f"**{name}**\n\n![{name}](screenshots/{name})\n")
    else:
        p("- 없음 (플레이 모드 갈무리가 실행되지 않았거나 화면을 만들지 못했다)")

    p("\n## 작업 요약\n")
    p("| 작업 | 구현 | 리뷰 | 새 테스트 | 바뀐 화면 코드 |")
    p("| --- | --- | --- | --- | --- |")
    for t in tasks:
        p(f"| `{Path(t.get('doc', '')).stem}` | {_cell(t.get('agents') or '기록 없음')} | {_cell(_review_text(t))} "
          f"| {len(t.get('new_tests') or [])} | {len(t.get('screen_files') or [])} |")

    p("\n## 관문 (스크립트 판정)\n")
    p(f"- 위반: {len(gate['violations'])}건")
    if gate["warnings"]:
        p("- 확인 필요:")
        for w in gate["warnings"]:
            p("  - " + w.replace("\n", "\n  "))
    else:
        p("- 확인 필요: 없음")
    if discarded:
        p("- 합치기 전 테스트 실행이 만든 변경은 버렸다 (합쳐지지 않음):\n")
        p("  ```\n  " + discarded.replace("\n", "\n  ") + "\n  ```")

    p("\n## 작업별 기록\n")
    for t in tasks:
        name = Path(t.get("doc", "")).stem
        r = t.get("review") or {}
        p(f"### {name}\n")
        p(f"- 구현: {t.get('agents') or '기록 없음'}")
        p(f"- 리뷰: {_review_text(t)}")
        if t.get("summary"):
            p(f"- 바뀐 내용: {_cell(t['summary'])}")
        if r.get("summary"):
            p(f"- 리뷰 의견: {_cell(r['summary'])}")
        p(f"- 작업 문서 원본: `docs/history/{name}.md`")
        crit = r.get("criteria") or []
        if crit:
            p("\n| 완료 조건 | 대응하는 테스트 | 판정 |")
            p("| --- | --- | --- |")
            for c in crit:
                p(f"| {_cell(c.get('condition'))} | {_cell(c.get('test') or '-')} | {_cell(c.get('verdict'))} |")
        notes = [f for f in r.get("findings") or [] if f.get("text")]
        if notes:
            p("\n리뷰 지적 (합친 뒤에도 남은 것):\n")
            for f in notes:
                p(f"- [{f.get('severity', 'note')}] {_cell(f.get('text'))}")
        if t.get("new_tests"):
            p(f"\n새 테스트 {len(t['new_tests'])}개: " + ", ".join(f"`{x}`" for x in t["new_tests"][:40])
              + (" …" if len(t["new_tests"]) > 40 else ""))
        if t.get("gate_warnings"):
            p("\n관문 확인 필요:\n")
            for w in t["gate_warnings"]:
                p("- " + w.replace("\n", "\n  "))
        p("")

    p("## 확인하고 되돌리는 방법\n")
    p("```")
    p(f"git diff {tag}^1 {tag}            # 이 루프가 바꾼 것 전부")
    p(f"git merge --ff-only {tag}         # ({main} 에서) 이 루프까지만 올린다")
    p(f"git revert -m 1 {tag}             # ({main} 에 올린 뒤) 이 루프만 되돌린다")
    p("```")
    (folder / "report.md").write_text("\n".join(L) + "\n", encoding="utf-8", newline="\n")

    (folder / "results.json").write_text(
        json.dumps(
            {"loop": loop_branch, "bundle": bundle, "merged_at": now, "tag": tag, "base": base,
             "tests": test_summary, "gate": gate, "tasks": tasks, "screenshots": shot_names, "blocked": blocked or []},
            ensure_ascii=False, indent=2,
        ),
        encoding="utf-8", newline="\n",
    )  # fmt: skip

    index = wt / EVIDENCE_REL / "index.md"
    text = index.read_text(encoding="utf-8") if index.exists() else INDEX_HEAD.replace("{tag_prefix}", tag_prefix)
    lines = [x for x in text.rstrip("\n").splitlines() if not x.startswith(f"| `{loop_id}` |")]
    reviews = sorted({_review_text(t).split(":")[0] for t in tasks})
    lines.append(
        f"| `{loop_id}` | {_cell(bundle)} | {now} | {len(tasks)}건 | {_tests_text(test_summary)} | {_cell(', '.join(reviews))} "
        f"| {len(humans)}건 | [report.md]({loop_id}/report.md) |"
    )
    index.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
    return f"{EVIDENCE_REL}/{loop_id}/report.md"
