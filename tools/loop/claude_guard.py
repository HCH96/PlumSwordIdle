"""클로드 도구 호출 검사 훅 (PreToolUse).

루프가 클로드 코드를 실행할 때 모든 도구 호출 전에 이 스크립트가 먼저 돈다 (run_loop.py 가 --settings 로 등록).
윈도우에서는 클로드 코드 샌드박스를 쓸 수 없어서, 호출 내용을 검사해 아래를 막는다.

- 파일 도구: worktree 밖 쓰기, 사용자 폴더 접근, .env 접근
- Bash·PowerShell: 깃 상태를 바꾸는 명령, 사용자 폴더 경로나 .env 가 들어간 명령

막을 때는 이유를 stderr 로 내고 종료 코드 2 로 끝낸다 (클로드에게 이유가 전달된다). 막은 기록은 --log 파일에 남긴다.
검사 중 오류가 나도 종료 코드 2 로 막는다. 규칙과 한계는 docs/loop.md "클로드 권한" 참고.
"""

from __future__ import annotations

import argparse
import datetime as _dt
import json
import os
import re
import sys

WRITE_TOOLS = {"Edit", "Write", "MultiEdit", "NotebookEdit"}
READ_TOOLS = {"Read", "Glob", "Grep"}
SHELL_TOOLS = {"Bash", "PowerShell", "Monitor"}
PATH_KEYS = ("file_path", "notebook_path", "path")

# 깃 상태를 바꾸는 하위 명령. 조회용(log, diff, status, show)과 add 는 허용한다
GIT_BLOCKED = (
    "commit", "push", "pull", "checkout", "switch", "merge", "rebase", "reset", "branch", "stash", "tag",
    "worktree", "cherry-pick", "revert", "am", "update-ref", "symbolic-ref",
)  # fmt: skip
_GIT_ARG = r"(?:\"[^\"]*\"|'[^']*'|\S+)"
GIT_RE = re.compile(
    r"(?<![\w-])git(?:\.exe)?[\"']?"
    # git 과 하위 명령 사이의 전역 옵션: -C <경로>, -c <설정>, --git-dir <경로>, --no-pager 등
    r"(?:\s+(?:-[cC]\s+" + _GIT_ARG + r"|--(?:git-dir|work-tree|namespace|exec-path|config-env)\s+" + _GIT_ARG
    + r"|--?[\w-]+(?:=" + _GIT_ARG + r")?))*"
    r"\s+[\"']?(" + "|".join(GIT_BLOCKED) + r")(?![\w-])",
    re.IGNORECASE,
)
ENV_RE = re.compile(r"(?<![\w.-])\.env(?![\w.-])", re.IGNORECASE)


class Deny(Exception):
    pass


def _to_native(path: str) -> str:
    """깃 배시식 경로(/c/Users/..)를 윈도우 경로로 바꾼다."""
    if os.name == "nt":
        m = re.match(r"^/(?:mnt/)?([A-Za-z])(/.*)?$", path)
        if m:
            return f"{m.group(1).upper()}:{m.group(2) or '/'}"
    return path


def _resolve(path: str, cwd: str) -> str:
    path = os.path.expanduser(_to_native(path.strip()))
    if not os.path.isabs(path):
        path = os.path.join(cwd, path)
    return os.path.normcase(os.path.realpath(path))


def _inside(path: str, folder: str) -> bool:
    try:
        return os.path.commonpath([path, folder]) == folder
    except ValueError:  # 드라이브가 다름
        return False


def root_mention_re(root: str) -> re.Pattern:
    """명령 안에서 사용자 폴더를 가리키는 표기. 절대 경로·~·환경 변수 표기는 '<부모>/<이름>', 상대 경로는 '../<이름>' 으로 잡는다."""
    name = os.path.basename(root)
    parent = os.path.basename(os.path.dirname(root))
    heads = [r"\.\."] + ([re.escape(parent)] if parent else [])
    return re.compile(r"(?:" + "|".join(heads) + r")[\\/]+" + re.escape(name) + r"(?![\w.-])", re.IGNORECASE)


def check_file_tool(tool: str, tool_input: dict, cwd: str, worktree: str, root: str) -> None:
    for key in PATH_KEYS:
        raw = tool_input.get(key)
        if not isinstance(raw, str) or not raw.strip():
            continue
        path = _resolve(raw, cwd)
        if os.path.basename(path) == ".env":
            raise Deny(f".env 는 읽거나 고칠 수 없음: {raw}")
        if _inside(path, root):
            raise Deny(f"사용자 폴더는 접근할 수 없음 (작업은 worktree 안에서만): {raw}")
        if tool in WRITE_TOOLS and not _inside(path, worktree):
            raise Deny(f"worktree 밖에는 쓸 수 없음: {raw}")


def check_shell(command: str, root_re: re.Pattern) -> None:
    m = GIT_RE.search(command)
    if m:
        raise Deny(f"깃 상태를 바꾸는 명령은 쓸 수 없음 (git {m.group(1).lower()}). 커밋·브랜치는 루프 스크립트가 한다")
    if ENV_RE.search(command):
        raise Deny(".env 가 들어간 명령은 쓸 수 없음")
    if root_re.search(command):
        raise Deny("사용자 폴더 경로가 들어간 명령은 쓸 수 없음 (작업은 worktree 안에서만)")


def write_log(log_path: str | None, tool: str, reason: str, detail: str) -> None:
    if not log_path:
        return
    try:
        os.makedirs(os.path.dirname(log_path), exist_ok=True)
        detail = " ".join(detail.split())
        if len(detail) > 300:
            detail = detail[:300] + "…"
        with open(log_path, "a", encoding="utf-8") as f:
            f.write(f"[{_dt.datetime.now():%Y-%m-%d %H:%M:%S}] 막음 {tool}: {reason} | {detail}\n")
    except OSError:
        pass  # 기록 실패로 판정을 바꾸지 않는다


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--worktree", required=True)
    parser.add_argument("--root", required=True)
    parser.add_argument("--log")
    args = parser.parse_args()
    try:
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except AttributeError:
        pass

    tool, detail = "?", ""
    try:
        event = json.loads(sys.stdin.buffer.read().decode("utf-8-sig"))
        tool = str(event.get("tool_name", ""))
        tool_input = event.get("tool_input") or {}
        if not isinstance(tool_input, dict):
            tool_input = {}
        worktree = os.path.normcase(os.path.realpath(args.worktree))
        root = os.path.normcase(os.path.realpath(args.root))
        cwd = str(event.get("cwd") or worktree)

        if tool in WRITE_TOOLS or tool in READ_TOOLS:
            detail = " ".join(str(tool_input.get(k)) for k in PATH_KEYS if tool_input.get(k))
            check_file_tool(tool, tool_input, cwd, worktree, root)
        elif tool in SHELL_TOOLS:
            detail = str(tool_input.get("command", ""))
            check_shell(detail, root_mention_re(root))
        return 0  # 판정 없음: 나머지는 클로드의 권한 규칙이 정한다
    except Deny as e:
        write_log(args.log, tool, str(e), detail)
        print(f"[루프 검사 훅] {e}", file=sys.stderr)
        return 2
    except Exception as e:  # 검사하지 못한 호출은 통과시키지 않는다
        write_log(args.log, tool, f"검사 오류 {type(e).__name__}: {e}", detail)
        print(f"[루프 검사 훅] 검사 중 오류가 나서 막음: {type(e).__name__}: {e}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
