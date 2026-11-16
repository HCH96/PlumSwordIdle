# Claude와 Codex 공통 개발 흐름

기획 → 사양 → 작업 문서 → 구현·테스트·관문·리뷰 → 증거·integration 병합 → 사람 리뷰 → main 반영 순서다. 자세한 루프 동작은 [loop.md](loop.md), 작업 양식은 [task_template.md](task_template.md)를 따른다.

## 스킬과 공통 프로그램

- 다섯 스킬의 본문 원본은 `.claude/skills/<이름>/SKILL.md`다: `plan-feature`, `make-tasks`, `loop-status`, `answer-blocked`, `review-loop`.
- Claude는 기존 경로로 읽는다. Codex는 `.agents/skills/<이름>/SKILL.md`에서 같은 원본을 읽는다. 절차 본문이나 템플릿을 복제하지 않는다.
- 본문의 명령·저장소 경로는 원본 프로젝트 루트 기준이다. 마크다운 상대 링크는 원본 문서 위치를 기준으로 해석한다. `plan-feature`의 템플릿은 `.claude/skills/plan-feature/spec_template.md`다.
- 보조 프로그램의 구현은 `tools/skills/`에만 있다. 기존 `.claude/skills/`의 Python 파일은 공통 프로그램을 호출하는 호환 진입점이다.
- 질문 도구가 없으면 일반 대화로 질문한다. 사용자의 기존 답변·승인은 양쪽에서 같은 의미다. 모델을 바꿨다는 이유로 작업을 다시 승인받지 않는다.

## 원본 프로젝트와 worktree

스킬은 원본 checkout에서 사용한다. `tasks/`와 `.loop/`는 원본 프로젝트의 로컬 상태판 하나만 쓴다. 공통 프로그램은 `git --git-common-dir`로 원본 checkout을 확인하므로 하위 폴더나 linked worktree에서 실행해도 상태판이 달라지지 않는다. 다른 복제본은 `--root <그 프로젝트>`로 지정한다. bare 저장소나 특수한 git-dir 배치는 지원하지 않으며 루트를 추측하지 않고 오류로 알린다.

루프 실행은 원본 프로젝트에서 기존 `python tools/loop/run_loop.py` 명령을 사용한다. 대화 스킬, 루프를 관리하는 Python 프로세스, 구현 에이전트, 읽기 전용 리뷰어의 권한은 서로 다르다. 구현 에이전트에 루프 관리용 Git 권한을 주지 않는다. 기존 Claude hook과 Codex sandbox 설정을 각각 유지한다.

## 사양과 완료 판정

- 새 기획·사양은 main 작업 폴더에 작성하고 커밋한 뒤 사용한다. 스킬 자체는 따로 지시받지 않으면 커밋하지 않는다.
- 이미 구현된 연결 지점·사양·이력은 integration의 고정 SHA에서도 확인한다. main과 다르면 차이를 설명하고 정리한다. integration을 원본 폴더에 체크아웃하지 않는다.
- 참고 문서는 main 또는 integration의 커밋에서 읽을 수 있어야 한다. 로컬의 미커밋 내용을 에이전트가 읽을 수 있다고 가정하지 않는다.
- 새 루프 시작 때 main이 동기화된다. 같은 묶음의 루프가 진행 중이고 새 사양을 아직 읽을 수 없다면 새 묶음으로 분리해 다음 루프에서 처리한다.
- 선행 작업 완료는 integration의 `docs/history/`가 기준이다. `tasks/done/`에만 있는 것은 통합 완료가 아니다. 같은 루프 안의 선행 작업 판정은 루프 스크립트가 맡는다.

## 단독 운영과 리뷰

기본 설정은 Claude와 Codex다. 한쪽만 쓸 때는 기존 로컬 설정의 `agents`를 `["claude"]` 또는 `["codex"]`로 정한다. `review_enabled`는 켜 둔다. 단독 운영은 같은 모델의 새 세션 리뷰이며, 다른 모델의 교차 리뷰와 증거에서 구분한다. CLI 설치·로그인·실행 권한은 사용하는 에이전트에 맞게 확인한다.

자동 리뷰는 완료 조건별 번호·원문·테스트 근거를 모두 받아 검사한다. 필드 누락, 조건 누락·중복, 근거 없는 통과, `pass`와 `fix`/`human` 지적의 모순은 리뷰 실패로 처리한다. 이는 형식과 일관성 검사이며 코드가 실제로 올바르다는 증명은 아니다.

## 사람 리뷰와 main 반영

`python tools/skills/review_scan.py`가 출력한 기준 SHA·대상 SHA·main SHA를 기록한다. 코드·이력·증거는 이 고정 커밋에서 읽는다. `--loop`의 부분 리뷰는 main 반영 승인으로 쓰지 않는다. main부터 대상까지 누적 범위를 리뷰한다.

```
python tools/skills/review_scan.py --base <main SHA> --head <대상 SHA>
```

사용자가 그 범위를 올리라고 지시한 뒤, main HEAD가 리뷰 때와 같은지와 대상이 여전히 integration에 포함되는지 확인하고 `git merge --ff-only <승인한 대상 SHA>`로 반영한다. main이 바뀌면 다시 리뷰한다. integration이 전진해도 새 커밋은 승인 범위에 추가하지 않는다. 푸시는 사람이 한다.

화면 갈무리는 셸의 바이너리 리다이렉션에 의존하지 않고 다음 명령으로 저장소 밖 임시 폴더에 꺼낸다. 기존 파일은 덮어쓰지 않는다.

```
python tools/skills/export_evidence.py --revision <대상 SHA> --path docs/evidence/<루프>/screenshots/game_030s.png --output <임시 폴더>/game_030s.png
```

## 공통 도구를 수정할 때 검증

실행 중인 루프가 없는 유지보수 시점에 수정한다. `python -B -m unittest discover -s tools/skills/tests -v`는 임시 저장소와 모의 응답으로 경로·작업 검사·리뷰 계약·고정 커밋·증거 추출을 검사하며 실제 상태판과 브랜치를 바꾸지 않는다. 이는 실제 CLI 호출·Unity 실행·스킬 자동 선택 검증을 대신하지 않는다. 실제 양쪽 스킬 호출과 CLI 리뷰·화면 갈무리는 별도 격리 환경에서 확인한다.
