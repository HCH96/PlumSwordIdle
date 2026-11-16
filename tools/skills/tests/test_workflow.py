"""실제 프로젝트 상태를 쓰지 않는 공통 흐름 회귀 검사."""
from __future__ import annotations

import copy
from contextlib import ExitStack
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

ROOT = Path(__file__).resolve().parents[3]
sys.path[:0] = [str(ROOT / "tools/skills"), str(ROOT / "tools/loop")]
import workflow_context as context
import export_evidence
import loop_status
import loop_gate
import run_loop


def task(name="001_sample", refs="docs/spec/example.md", condition="[컴파일] 컴파일 성공", extra=""):
    return (f"# {name} — 테스트\n\n## 목표\n기능 확인\n\n## 묶음\n검사\n\n"
            f"## 선행 작업\n없음\n\n## 참고 문서\n- docs/index.md\n- docs/dev/architecture.md\n- {refs}\n\n"
            f"## 요구사항\n- 기능 구현\n\n## 완료 조건\n- {condition}\n\n{extra}## 하지 말 것\n- 다른 기능 변경\n")


class RepoCase(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix="plumsword-workflow-")
        self.addCleanup(self.tmp.cleanup)
        self.area = Path(self.tmp.name)
        self.repo = self.area / "project"
        self.repo.mkdir()
        self.git("init", "-b", "main")
        self.git("config", "user.name", "Workflow Test")
        self.git("config", "user.email", "workflow@example.invalid")
        self.git("config", "commit.gpgsign", "false")
        self.git("config", "core.autocrlf", "false")
        self.git("config", "core.hooksPath", str(self.area / "no-hooks"))
        self.write(".gitignore", "tasks/\n.loop/\n")
        for p in (ROOT / "tools/loop").glob("*.py"):
            self.write("tools/loop/" + p.name, p.read_text(encoding="utf-8"))
        self.write("tools/loop/loop_config.json", json.dumps({"base_branch": "main", "integration_branch": "integration", "default_bundle": "기본", "worktree_path": "../worktree"}))
        for path in ("docs/index.md", "docs/dev/architecture.md", "docs/spec/example.md"):
            self.write(path, "기준 문서\n")
        self.write("AGENTS.md", "규칙\n")
        self.write("CLAUDE.md", "규칙\n")
        self.base = self.commit("initial")
        self.git("branch", "integration")

    def git(self, *args):
        result = subprocess.run(["git", *args], cwd=self.repo, capture_output=True,
                                text=True, encoding="utf-8", errors="replace")
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout.strip()

    def write(self, path, text):
        dest = self.repo / path
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_text(text, encoding="utf-8", newline="\n")
        return dest

    def commit(self, message):
        self.git("add", "-A")
        self.git("commit", "-m", message)
        return self.git("rev-parse", "HEAD")

    def cli(self, script, *args, expected=0):
        result = subprocess.run([sys.executable, "-B", str(ROOT / script), "--root", str(self.repo), *args],
                                cwd=self.area, capture_output=True, text=True, encoding="utf-8", errors="replace")
        self.assertEqual(result.returncode, expected, result.stdout + result.stderr)
        return result.stdout

    def merge_loop(self, suffix="000001", doc=None, extra_path=None):
        loop_id = "20261117-" + suffix
        self.git("checkout", "-b", "loop/" + loop_id, "integration")
        name = "001_sample" if suffix == "000001" else "002_next"
        self.write(f"docs/history/{name}.md", doc or task(name))
        if extra_path:
            self.write(extra_path, "changed\n")
        self.commit("작업 완료: " + name)
        self.write(f"docs/evidence/{loop_id}/report.md", "증거\n")
        self.commit("루프 증거: " + loop_id)
        self.git("checkout", "integration")
        self.git("merge", "--no-ff", "loop/" + loop_id, "-m", "루프 합침: loop/" + loop_id + " (묶음 검사)")
        sha = self.git("rev-parse", "HEAD")
        self.git("tag", "loop-merged/" + loop_id)
        self.git("checkout", "main")
        return sha


class ContextTests(RepoCase):
    def test_linked_worktree_uses_original_state_board(self):
        wt = self.area / "worktree"
        self.git("worktree", "add", "--detach", str(wt), "main")
        self.assertEqual(context.project_root(wt / "docs"), self.repo.resolve())
        self.assertEqual(context.project_root(self.repo / "docs"), self.repo.resolve())

    def test_integrated_only_reference_and_unmerged_done(self):
        self.git("checkout", "integration")
        self.write("docs/spec/integrated.md", "통합에만 있음\n")
        self.write("docs/history/001_previous.md", task("001_previous"))
        tip = self.commit("integrated")
        self.git("checkout", "main")
        self.write("tasks/done/002_unmerged.md", task("002_unmerged"))
        self.assertEqual(context.reference_errors(self.repo, "docs/spec/integrated.md", self.base, tip, "검사", {})[0], [])
        self.assertEqual(context.committed_history(self.repo, tip), {"001_previous"})
        drafts = self.area / "drafts"
        drafts.mkdir()
        (drafts / "003_new.md").write_text(task("003_new", "docs/spec/integrated.md").replace("## 선행 작업\n없음", "## 선행 작업\n- 002_unmerged"), encoding="utf-8")
        out = self.cli("tools/skills/check_tasks.py", str(drafts))
        self.assertIn("기존 작업을 기다리는", out)

    def test_merged_history_wins_over_done_folder(self):
        self.git("checkout", "integration")
        self.write("docs/history/001_previous.md", task("001_previous"))
        self.commit("integrated")
        self.git("checkout", "main")
        self.write("tasks/done/001_previous.md", task("001_previous"))
        drafts = self.area / "drafts"
        drafts.mkdir()
        (drafts / "002_new.md").write_text(task("002_new").replace("## 선행 작업\n없음", "## 선행 작업\n- 001_previous"), encoding="utf-8")
        out = self.cli("tools/skills/check_tasks.py", str(drafts))
        self.assertNotIn("기존 작업을 기다리는", out)

    def test_uncommitted_spec_is_rejected(self):
        self.write("docs/spec/example.md", "미커밋 변경\n")
        errors, _ = context.reference_errors(self.repo, "docs/spec/example.md", self.base, self.base, "검사", {})
        self.assertTrue(errors)

    def test_new_main_spec_not_available_to_active_bundle(self):
        self.git("branch", "loop/active", "integration")
        self.write("docs/spec/new.md", "새 사양\n")
        main = self.commit("spec")
        state = {"status": "active", "bundle": "검사", "loop_branch": "loop/active"}
        errors, _ = context.reference_errors(self.repo, "docs/spec/new.md", main, self.base, "검사", state)
        self.assertTrue(errors)
        errors, warnings = context.reference_errors(self.repo, "docs/spec/new.md", main, self.base, "다음", state)
        self.assertFalse(errors)
        self.assertTrue(warnings)

    def test_old_and_new_cli_paths_match(self):
        for skill, script, args in (("make-tasks", "check_tasks", ["--next-number"]),
                                     ("answer-blocked", "blocked_info", ["list"]),
                                     ("review-loop", "review_scan", [])):
            with self.subTest(script=script):
                common = self.cli(f"tools/skills/{script}.py", *args)
                legacy = self.cli(f".claude/skills/{skill}/{script}.py", *args)
                self.assertEqual(common, legacy)
        (self.repo / "tasks").mkdir(exist_ok=True)
        self.assertEqual(self.cli("tools/skills/loop_status.py", "--no-process"),
                         self.cli(".claude/skills/loop-status/loop_status.py", "--no-process"))

    def test_active_bundle_accepts_integrated_revision_but_not_new_main_revision(self):
        self.git("checkout", "integration")
        self.write("docs/spec/example.md", "구현에 맞춰 바뀐 연결 지점\n")
        tip = self.commit("updated interface")
        self.git("branch", "loop/active")
        self.git("checkout", "main")
        state = {"status": "active", "bundle": "검사", "loop_branch": "loop/active"}
        errors, warnings = context.reference_errors(self.repo, "docs/spec/example.md", self.base, tip, "검사", state)
        self.assertFalse(errors)
        self.assertTrue(warnings)
        self.write("docs/spec/example.md", "main의 다음 기획\n")
        main = self.commit("next design")
        errors, _ = context.reference_errors(self.repo, "docs/spec/example.md", main, tip, "검사", state)
        self.assertTrue(errors)


class ReviewTests(unittest.TestCase):
    def setUp(self):
        self.conditions = [("", "재화가 1 증가한다"), ("컴파일", "컴파일 성공"), ("사람 확인", "화면 확인")]
        self.valid = {"verdict": "pass", "summary": "요구사항과 근거 확인", "question": "", "recommendation": "",
                      "criteria": [{"id": i, "condition": c, "test": t, "verdict": v}
                                   for i, ((_, c), t, v) in enumerate(zip(self.conditions, ["MoneyTests.Add", "컴파일 결과", "화면 갈무리"], ["있음", "루프가 판정", "사람 확인"]), 1)],
                      "findings": []}

    def parse(self, data):
        return run_loop.parse_review(json.dumps(data, ensure_ascii=False), self.conditions)

    def test_complete_pass_fix_human(self):
        self.assertIsNotNone(self.parse(self.valid))
        for verdict in ("fix", "human"):
            data = copy.deepcopy(self.valid)
            data.update(verdict=verdict, question="판단?", recommendation="추천")
            data["findings"] = [{"severity": verdict, "text": "파일:1 근거"}]
            self.assertIsNotNone(self.parse(data))

    def test_empty_missing_duplicate_and_wrong_types_rejected(self):
        cases = [{"verdict": "pass"}, {**self.valid, "criteria": []}, {**self.valid, "criteria": {}},
                 {**self.valid, "findings": "bad"}, {**self.valid, "criteria": [None]},
                 {**self.valid, "criteria": self.valid["criteria"][:-1]},
                 {**self.valid, "criteria": [self.valid["criteria"][0]] * 3}]
        for data in cases:
            with self.subTest(data=data):
                self.assertIsNone(self.parse(data))
        raw = json.dumps(self.valid)
        self.assertIsNone(run_loop.parse_review(raw + raw, self.conditions))

    def test_fenced_or_prefixed_json_accepted(self):
        raw = json.dumps(self.valid, ensure_ascii=False)
        for text in ("```json\n" + raw + "\n```", "```\n" + raw + "\n```",
                     "판정입니다.\n" + raw, raw + "\n이상입니다.", "판정:\n```json\n" + raw + "\n```"):
            with self.subTest(text=text[:20]):
                self.assertIsNotNone(run_loop.parse_review(text, self.conditions))
        self.assertIsNone(run_loop.parse_review("판정 JSON 없음", self.conditions))
        self.assertIsNone(run_loop.parse_review("```json\n" + raw + "\n```\n" + raw, self.conditions))

    def test_condition_text_minor_differences_accepted(self):
        data = copy.deepcopy(self.valid)
        data["criteria"][0]["condition"] = " `재화가`  1 증가한다. "
        parsed = self.parse(data)
        self.assertIsNotNone(parsed)
        self.assertEqual(parsed["criteria"][0]["condition"], "재화가 1 증가한다")

    def test_loop_judged_only_for_compile_or_full_tests(self):
        conditions = [("", "기존 테스트가 모두 통과한다"), ("", "재화가 1 증가한다")]
        data = {**self.valid, "criteria": [
            {"id": 1, "condition": conditions[0][1], "test": "전체 테스트 실행", "verdict": "루프가 판정"},
            {"id": 2, "condition": conditions[1][1], "test": "MoneyTests.Add", "verdict": "있음"}]}
        self.assertIsNotNone(run_loop.parse_review(json.dumps(data, ensure_ascii=False), conditions))
        data["criteria"][1]["verdict"] = "루프가 판정"
        self.assertIsNone(run_loop.parse_review(json.dumps(data, ensure_ascii=False), conditions))

    def test_contradictory_pass_and_missing_evidence_rejected(self):
        for severity in ("fix", "human", "unknown"):
            data = copy.deepcopy(self.valid)
            data["findings"] = [{"severity": severity, "text": "문제"}]
            self.assertIsNone(self.parse(data))
        for key, value in (("test", "-"), ("verdict", "빠짐"), ("verdict", "사람 확인"), ("condition", "다른 조건"), ("id", True)):
            data = copy.deepcopy(self.valid)
            data["criteria"][0][key] = value
            self.assertIsNone(self.parse(data))
        data = copy.deepcopy(self.valid)
        data["verdict"] = "human"
        self.assertIsNone(self.parse(data))

    def test_reviewer_selection_and_fresh_session(self):
        for agents, implementer, expected in ((["claude", "codex"], "claude", "codex"),
                                               (["claude", "codex"], "codex", "claude"),
                                               (["codex"], "codex", "codex")):
            with self.subTest(agents=agents, implementer=implementer), tempfile.TemporaryDirectory() as directory, ExitStack() as stack:
                doc = Path(directory) / "001_sample.md"
                doc.write_text(task(condition="재화가 1 증가한다").replace("- 재화가 1 증가한다", "- 재화가 1 증가한다\n- [컴파일] 컴파일 성공\n- [사람 확인] 화면 확인"), encoding="utf-8")
                runner = run_loop.Loop.__new__(run_loop.Loop)
                runner.cfg = {"agents": agents}
                runner.limited_at = {}
                runner.loop_branch = "loop/test"
                runner.current_agent = Mock()
                runner.check_root = Mock()
                runner.undo_agent_commits = Mock()
                runner.n = Mock()
                stack.enter_context(patch.object(run_loop, "wgit", return_value=Mock(stdout="tree")))
                stack.enter_context(patch.object(run_loop, "root_snapshot", return_value={}))
                stack.enter_context(patch.object(run_loop, "log"))
                claude = stack.enter_context(patch.object(run_loop, "run_claude", return_value={"result": json.dumps(self.valid)}))
                codex = stack.enter_context(patch.object(run_loop, "run_codex", return_value={"result": json.dumps(self.valid)}))
                result = runner.review(doc, "task/test", "snap", "tree", {"warnings": []}, [(implementer, 1)], doc.stem, 1)
                self.assertEqual(result["reviewer"], expected)
                self.assertEqual(result["cross"], expected != implementer)
                chosen = claude if expected == "claude" else codex
                self.assertTrue(chosen.call_args.kwargs["review"])
                if expected == "claude":
                    self.assertIsNone(chosen.call_args.args[2])
                chosen.return_value = {"result": '{"verdict":"pass"}'}
                chosen.reset_mock()
                self.assertEqual(runner.review(doc, "task/test", "snap", "tree", {"warnings": []}, [(implementer, 1)], doc.stem, 1)["verdict"], "error")
                self.assertEqual(chosen.call_count, 2)

    def test_process_lookup_failure_is_unknown_even_with_end_log(self):
        with patch.object(loop_status, "stamp", return_value="time"), patch.object(loop_status, "ago", return_value="ago"):
            log = {"path": Path("old.log"), "started": 0, "mtime": 0, "watch": False, "ended": True, "last": "ended"}
            self.assertEqual(loop_status.judge_running(None, "권한 없음", log, {})[0], "unknown")


class ScanTests(RepoCase):
    def test_pin_survives_integration_advancing(self):
        tip = self.merge_loop()
        first = self.cli("tools/skills/review_scan.py")
        self.assertIn(f"git merge --ff-only {tip}", first)
        self.assertIn(f"git show {tip}:docs/evidence/", first)
        self.merge_loop("000002")
        fixed = self.cli("tools/skills/review_scan.py", "--base", self.base, "--head", tip)
        self.assertIn(f"git merge --ff-only {tip}", fixed)
        self.assertNotIn("git merge --ff-only integration", fixed)
        # 반영 동작도 임시 저장소에서만 시험한다.
        self.git("merge", "--ff-only", tip)
        self.assertEqual(self.git("rev-parse", "HEAD"), tip)
        self.assertNotEqual(self.git("rev-parse", "integration"), tip)

    def test_partial_loop_does_not_offer_merge(self):
        self.merge_loop()
        out = self.cli("tools/skills/review_scan.py", "--loop", "20261117-000001")
        self.assertNotIn("git merge --ff-only", out)
        self.assertIn("누적 범위를 리뷰", out)

    def test_changed_main_requires_new_review(self):
        tip = self.merge_loop()
        self.write("docs/spec/next.md", "다음 사양\n")
        self.commit("new main")
        out = self.cli("tools/skills/review_scan.py", "--base", self.base, "--head", tip)
        self.assertNotIn("git merge --ff-only", out)

    def test_common_gate_protects_codex_and_shared_helpers(self):
        for path in (".agents/skills/x/SKILL.md", "tools/skills/helper.py", ".codex/config.toml", "docs/dev/workflow.md"):
            with self.subTest(path=path):
                self.write(path, "changed")
                end = self.commit("change " + path)
                result = loop_gate.check(self.repo, self.base, end)
                self.assertTrue(any(path in v["text"] for v in result["violations"]))

    def test_integration_changes_without_loop_merge_are_still_shown(self):
        self.git("checkout", "integration")
        self.write("tools/skills/unexpected.py", "unexpected")
        tip = self.commit("unexpected manual change")
        self.git("checkout", "main")
        out = self.cli("tools/skills/review_scan.py")
        self.assertIn(tip, out)
        self.assertIn("[위반]", out)
        self.assertIn("tools/skills/unexpected.py", out)

    def test_scan_honors_only_current_task_exception(self):
        doc = task(extra="## 관문 예외\n- settings\n\n")
        first = self.merge_loop(doc=doc, extra_path="ProjectSettings/test.txt")
        out = self.cli("tools/skills/review_scan.py", "--base", self.base, "--head", first)
        self.assertNotIn("[위반]", out)
        self.assertIn("관문 예외로 허용됨", out)
        second = self.merge_loop("000002", extra_path="ProjectSettings/other.txt")
        out = self.cli("tools/skills/review_scan.py", "--base", self.base, "--head", second)
        self.assertIn("[위반]", out)
        self.assertIn("ProjectSettings/other.txt", out)

    def test_unmerged_branch_is_not_promotable(self):
        self.git("checkout", "-b", "rejected/test")
        self.write("docs/history/001_sample.md", task())
        self.commit("rejected")
        self.git("checkout", "main")
        out = self.cli("tools/skills/review_scan.py", "--branch", "rejected/test")
        self.assertNotIn("git merge --ff-only", out)

    def test_binary_evidence_export_and_no_overwrite(self):
        path = "docs/evidence/test/screenshots/test.png"
        dest = self.repo / path
        dest.parent.mkdir(parents=True)
        raw = b"\x89PNG\r\n\x1a\n\x00\xff\x80binary\r\n"
        dest.write_bytes(raw)
        tip = self.commit("evidence")
        output = self.area / "test.png"
        export_evidence.export(self.repo, tip, path, output)
        self.assertEqual(output.read_bytes(), raw)
        with self.assertRaises(FileExistsError):
            export_evidence.export(self.repo, tip, path, output)
        with self.assertRaises(ValueError):
            export_evidence.export(self.repo, tip, path, self.repo / "test.png")


if __name__ == "__main__":
    unittest.main()
