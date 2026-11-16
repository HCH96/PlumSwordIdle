"""고정 커밋의 증거 파일을 저장소 밖 임시 경로로 바이트 그대로 꺼낸다."""
from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path, PurePosixPath

sys.dont_write_bytecode = True

from workflow_context import git, project_root, revision


def export(root: Path, ref: str, path: str, output: Path) -> None:
    rel = PurePosixPath(path)
    if not path.startswith("docs/evidence/") or ".." in rel.parts or "\\" in path:
        raise ValueError("docs/evidence/ 아래 파일만 꺼낼 수 있음")
    output = output.resolve()
    if not output.parent.is_dir():
        raise ValueError("출력할 임시 폴더를 먼저 만들어 주세요")
    if git(output.parent, "rev-parse", "--show-toplevel").returncode == 0:
        raise ValueError("출력은 git 저장소 밖 임시 폴더로 지정하세요")
    sha = revision(root, ref)
    result = subprocess.run(["git", "--no-optional-locks", "show", f"{sha}:{path}"],
                            cwd=root, capture_output=True)
    if result.returncode:
        raise ValueError(f"증거 파일을 찾을 수 없음: {sha}:{path}")
    with output.open("xb") as stream:
        stream.write(result.stdout)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--root")
    ap.add_argument("--revision", required=True, help="리뷰 자료에 나온 고정 대상 SHA")
    ap.add_argument("--path", required=True)
    ap.add_argument("--output", required=True, type=Path)
    args = ap.parse_args()
    try:
        export(project_root(args.root), args.revision, args.path, args.output)
    except (ValueError, OSError) as error:
        ap.exit(2, f"실행할 수 없음: {error}\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
