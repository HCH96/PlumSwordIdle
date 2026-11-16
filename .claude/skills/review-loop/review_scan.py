"""기존 Claude 명령의 호환 진입점. 구현은 tools/skills/review_scan.py 하나만 유지한다."""
import runpy
import sys
from pathlib import Path

sys.dont_write_bytecode = True
common = Path(__file__).resolve().parents[3] / "tools" / "skills"
sys.path.insert(0, str(common))
if __name__ == "__main__":
    runpy.run_path(str(common / "review_scan.py"), run_name="__main__")
