"""스킬 공통 경로와 커밋 조회. 상태판은 항상 원본 checkout의 것을 쓴다."""
from __future__ import annotations

import json
import subprocess
from pathlib import Path


def git(root: Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(
        ["git", "--no-optional-locks", "-c", "core.quotepath=false", *args],
        cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace",
    )


def project_root(value: str | Path | None = None) -> Path:
    candidate = Path(value).resolve() if value else Path(__file__).resolve().parents[2]
    result = git(candidate, "rev-parse", "--path-format=absolute", "--git-common-dir")
    if result.returncode:
        raise ValueError(f"프로젝트 저장소를 찾을 수 없음: {candidate}")
    common = Path(result.stdout.strip()).resolve()
    if common.name != ".git":
        raise ValueError("원본 checkout의 .git 경로를 판별할 수 없음. --root 로 원본 프로젝트를 지정하세요")
    root = common.parent
    if not (root / "tools/loop/loop_config.json").is_file():
        raise ValueError(f"루프 설정이 없는 프로젝트: {root}")
    return root


def revision(root: Path, ref: str) -> str:
    result = git(root, "rev-parse", "--verify", "--end-of-options", f"{ref}^{{commit}}")
    if result.returncode:
        raise ValueError(f"커밋을 찾을 수 없음: {ref}")
    return result.stdout.strip()


def committed_history(root: Path, ref: str) -> set[str]:
    result = git(root, "ls-tree", "-r", "--name-only", ref, "--", "docs/history")
    if result.returncode:
        raise ValueError(f"완료 이력을 읽을 수 없음: {ref}")
    return {Path(p).stem for p in result.stdout.splitlines() if p.endswith(".md")}


def reference_errors(root: Path, path: str, main: str, integration: str,
                     bundle: str, state: dict) -> tuple[list[str], list[str]]:
    """커밋된 참고 문서만 인정. 같은 묶음의 진행 중 루프에 아직 없으면 거부한다."""
    errors, warnings = [], []
    rel = Path(path)
    if rel.is_absolute() or ".." in rel.parts or not path.startswith("docs/"):
        return [f"허용되지 않은 참고 문서 경로: {path}"], []
    versions = []
    for ref in dict.fromkeys((main, integration)):
        result = git(root, "show", f"{ref}:{path}")
        if result.returncode == 0:
            versions.append(result.stdout)
    if not versions:
        return [f"참고 문서 {path} 가 main/통합 기준 커밋에 없다. 먼저 사양을 커밋해야 한다"], []
    if len(set(versions)) > 1:
        warnings.append(f"참고 문서 {path}: main 과 통합 커밋의 내용이 다르다. 구현된 연결 지점을 통합 SHA에서 확인하고 차이를 정리한다")
    local = root / path
    if local.exists() and local.read_text(encoding="utf-8-sig").replace("\r\n", "\n") not in versions:
        errors.append(f"참고 문서 {path} 의 로컬 내용이 커밋된 내용과 다르다. 미커밋 사양은 루프가 읽지 못한다")
    active = state.get("status") == "active" and state.get("bundle") == bundle
    if active:
        ref = state.get("loop_branch", "")
        result = git(root, "show", f"{ref}:{path}") if ref else None
        if result is None or result.returncode or result.stdout not in versions:
            errors.append(f"참고 문서 {path} 를 진행 중인 같은 묶음에서 읽을 수 없거나 내용이 다르다. 새 묶음으로 분리해 다음 루프에서 main 동기화 후 처리한다")
        elif (local.exists()
              and local.read_text(encoding="utf-8-sig").replace("\r\n", "\n") != result.stdout
              and git(root, "merge-base", "--is-ancestor", main, ref).returncode):
            errors.append(f"참고 문서 {path} 의 로컬 내용과 진행 중인 같은 묶음의 내용이 다르다. 다음 루프에서 처리한다")
    elif git(root, "show", f"{integration}:{path}").returncode:
        warnings.append(f"참고 문서 {path}: 새 루프에서 main 을 통합 브랜치에 동기화한 뒤 사용 가능")
    return errors, warnings


def read_state(root: Path) -> dict:
    path = root / ".loop/state.json"
    if not path.exists():
        return {}
    # 상태를 못 읽으면 새 루프로 추측하지 않는다.
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(data, dict):
        raise ValueError("루프 상태 형식이 올바르지 않음")
    return data
