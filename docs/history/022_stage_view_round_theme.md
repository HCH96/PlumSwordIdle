# 022_stage_view_round_theme — 스테이지 화면에 회차·테마와 테마별 보스 이름 표시

## 목표
`PlumSword.Game` 의 스테이지 화면 `StageView` 가 스테이지 글자를 `ROUND 1 · STAGE 1 · JONGNAM` 형식으로, 보스 이름을 테마별 이름으로 보여 주게 고친다. 글자는 Core 의 `StageText.RoundStageLabel`, `ThemedBossName` 과 `StageProgress.RoundNumber`, `DisplayStageNumber` 를 그대로 쓴다.

## 묶음
테마회차

## 선행 작업
- 019_stage_view
- 020_stage_round_number
- 021_stage_theme_text

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다" 절)
- docs/spec/stage.md ("화면" 절의 "스테이지 글자", "보스 이름", "변경 기록"의 "2026-11-17 — 다섯 테마와 회차 순환")

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-17 — 다섯 테마와 회차 순환` 항목 중 화면 부분만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. 같은 사양의 "소개 패널"(`2026-11-17 — 스테이지 소개` 항목)은 이 작업에서 만들지 않는다 (025 가 한다).
- 고칠 파일: `Assets/Scripts/Game/Stage/StageView.cs` 하나. `GameRoot.cs` 는 고치지 않는다.
- 선행 작업이 만든 것을 쓴다. 내부는 고치지 않는다.
  - 020_stage_round_number: `PlumSword.Core.Stage.StageProgress` 의 `int RoundNumber { get; }`, `int DisplayStageNumber { get; }`(1~5).
  - 021_stage_theme_text: `PlumSword.Core.Stage.StageText` 의 `string RoundStageLabel(int round, int stage)`, `string ThemedBossName(int stage)`(1~5 밖이면 `ArgumentOutOfRangeException`).
  - 019_stage_view: 지금의 `StageView` 는 스테이지 글자에 `StageText.StageLabel(stage.StageNumber)` 를, 보스 이름에 `StageText.BossName` 을 쓴다. 보스 이름 글자는 만들 때 한 번만 넣는다. 페이드 막, 진행 바, `Changed` 구독, `Update` 의 `Tick`, TMP 기본 리소스가 없을 때의 처리(`WarnOnce`)는 그대로 둔다.
- **스테이지 글자**: 크기를 200×16 에서 600×16 (임시값)으로 바꾼다. 위치·앵커·피벗·글자 크기·색은 그대로다. 글자는 `StageText.RoundStageLabel(stage.RoundNumber, stage.DisplayStageNumber)` 그대로다 (예: `ROUND 1 · STAGE 1 · JONGNAM`).
- **보스 이름**: 크기를 200×16 에서 300×16 (임시값)으로 바꾼다. 위치·앵커·피벗·글자 크기·색은 그대로다. 글자는 `StageText.ThemedBossName(stage.DisplayStageNumber)` 다. 스테이지가 바뀌면 이름도 바뀌어야 하므로 만들 때 한 번이 아니라 다시 그릴 때(`Initialize` 의 첫 그리기와 `stage.Changed` 를 받을 때)마다 넣는다. `stage.IsBossActive` 가 `true` 일 때만 보이는 것은 그대로다.
- 글자를 넣을 때 TMP 오류가 나도 지금처럼 `WarnOnce` 로 경고 한 번만 남기고 게임을 멈추지 않는다.
- 화면 코드에서 회차, 화면상 번호, 테마 이름을 계산하지 않는다. Core 의 값을 그대로 쓴다.
- 이 작업에는 에딧 모드 테스트를 새로 쓰지 않는다 (테스트 어셈블리는 Core 만 참조하고, 글자 판정은 020 의 `StageProgressRoundTests` 와 021 의 `StageTextTests` 가 한다). 플레이 모드 테스트도 새로 쓰지 않는다.
- 2회차 화면(`ROUND 2 · STAGE 1 · JONGNAM`)은 강화 없이 실행해서 보기 어렵다. 확인용 시작 번호 변경이나 치트 키를 넣지 않는다. 2회차 규칙은 020 의 에딧 모드 테스트로 판정한다.

## 완료 조건
- [컴파일] `StageView.cs` 가 컴파일 오류 없이 컴파일되고, `StageView.cs` 에 `StageText.StageLabel(` 와 `StageText.BossName` 이라는 글자가 없으며, 기존 테스트가 모두 통과한다. (플레이 모드의 `GameBootTests`, 에딧 모드의 `StageTextTests`, `StageProgressRoundTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)
- [사람 확인] 실행하고 1초 뒤 화면 위쪽 가운데에 `ROUND 1 · STAGE 1 · JONGNAM` 이 한 줄로 잘리지 않고 보이고, 그 아래에 빈 노란 진행 바가 보인다.
- [사람 확인] 에디터에서 실행해 1스테이지의 10마리를 모두 처치하면 바가 가득 찬 빨간 바로 바뀌고 그 위에 `JONGNAM MASTER` 가 잘리지 않고 보인다.
- [사람 확인] 에디터에서 실행해 1스테이지 보스를 처치하면 금화가 50 늘고, 전투 화면이 가려졌다가 열리며 스테이지 글자가 `ROUND 1 · STAGE 2 · BEGGAR CLAN` 이 된다. 가려진 동안에도 보유 금화와 강화 버튼은 보이고, 버튼은 어둡고 눌리지 않는다. 열린 뒤 바는 빈 노란 바다.
- [사람 확인] 에디터에서 실행해 제자의 체력이 0이 되면 같은 방식으로 가려졌다가 같은 스테이지 글자로 처음부터 다시 시작한다. 금화와 강화 단계는 그대로다.

## 하지 말 것
- `Assets/Scripts/Core/` 를 고치지 않는다. 로직을 바꿔야 하면 고치지 말고 보고서의 가정에 적는다.
- `GameRoot.cs`, `GameBootstrap.cs`, `Assets/Scripts/Game/Combat/`, `Assets/Scripts/Game/Currency/`, `Assets/Scripts/Game/StatUpgrade/` 를 고치지 않는다.
- 소개 패널, 테마별 배경·몬스터 외형, 한글 글자와 한글 글꼴, 성공·실패 글자, 보스 등장 연출을 만들지 않는다.
- 페이드 막, 진행 바의 위치·크기·색을 바꾸지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)과 어셈블리 정의를 고치지 않는다.

## 보고서

### 한 일
- 스테이지 글자와 보스 이름의 너비를 늘리고 Core의 회차·화면상 번호와 테마별 글자를 표시하도록 `StageView`를 변경했다.
- 첫 그리기와 상태 변경 때마다 보스 이름을 갱신하며 기존 예외 경고와 보스 표시 조건을 유지했다.

### 바뀐 파일
- `Assets/Scripts/Game/Stage/StageView.cs`: 화면 글자 크기와 표시 내용 변경.
- `tasks/doing/022_stage_view_round_theme.md`: 이 보고서 추가.
- `.loop/result.json`: 루프 결과를 UTF-8 JSON으로 작성.

### 가정
- 추가 가정이나 임시 수치는 없다. 글자 크기 600×16과 300×16은 작업 문서의 임시값을 그대로 사용했다. 공개 연결 지점은 바꾸지 않았다.

### 사람이 확인할 것
- 실행 1초 뒤 화면 위쪽 가운데에 `ROUND 1 · STAGE 1 · JONGNAM`이 한 줄로 잘리지 않고 보이고 그 아래 빈 노란 진행 바가 보이는지 확인한다.
- 1스테이지의 일반 몬스터 10마리 처치 후 바가 가득 찬 빨간 바로 바뀌고 그 위에 `JONGNAM MASTER`가 잘리지 않고 보이는지 확인한다.
- 1스테이지 보스 처치 후 금화가 50 늘고 전투 화면이 가려졌다 열리며 `ROUND 1 · STAGE 2 · BEGGAR CLAN`이 표시되는지 확인한다. 가려진 동안 금화와 강화 버튼은 보이고 버튼은 어둡고 눌리지 않으며, 열린 뒤 바는 빈 노란 바인지 확인한다.
- 제자의 체력이 0이 되면 같은 방식으로 가려졌다 열리고 같은 스테이지가 처음부터 다시 시작하며 금화와 강화 단계가 유지되는지 확인한다.

### 테스트 결과
- 이전 시도의 테스트와 관문 통과는 전달된 리뷰 지시에서 확인했다. 개별 테스트 결과와 실행 로그는 이번 수정에서 직접 확인하지 않았다.
- 이번 수정에서는 변경된 코드와 금지된 기존 표시 API의 부재를 정적으로 확인했다. Unity 컴파일과 테스트는 직접 다시 실행하지 않았다. 화면의 실제 표시는 사람 확인이 필요하다.



## 루프 기록 (2026-11-17 11:58)

- 작업 에이전트: codex(시도 1~2)
- 리뷰: claude (교차) 통과, 고침 1회
- 기능 브랜치: task/022_stage_view_round_theme
- 루프 브랜치: loop/20261117-113918
