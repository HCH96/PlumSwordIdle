# 065_stage_view_final — FINAL 표시, 보스 구간 표식 숨김, 최종 보스 쓰러짐 대기

## 목표
`PlumSword.Game`의 스테이지 화면(`StageView`, `SubstagePreview`, `SubstageIntroBanner`)이 구간 번호를 `StageText.SubstageLabel`로 표시하고(5스테이지는 `FINAL`), 보스 구간에서 진행 표식을 숨기며, 5스테이지 최종 보스 쓰러짐 25프레임을 기다린 뒤 페이드하게 한다.

## 묶음
stage45_final

## 선행 작업
- 063_stage_final_label
- 064_stage_final_progress

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("테스트" 절 포함)
- docs/spec/stage.md ("화면" 절, "검증" 절의 완료 조건 47·48·50, "변경 기록" 절)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-22 — 4·5스테이지 진행 구체화` 항목 중 화면 절의 번호 글자·표식 숨김·최종 보스 쓰러짐 대기(완료 조건 47·48·50)만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. 5스테이지 배치와 4스테이지 일반 상대 대기는 064가 했고, 보스 체력 틀은 066이 한다.
- 063이 만든 `StageText.SubstageLabel(int stage, int substage)`(5스테이지는 `"FINAL"`, 아니면 `"{stage}-{substage}"`)와 `StageLayout.FinalDisplayStage`(5)를 쓴다.
- 번호 글자 (`Assets/Scripts/Game/Stage/`):
  - `SubstagePreview.SetProgress(int stage, int substage)`의 번호 글자를 `stage + "-" + substage` 대신 `StageText.SubstageLabel(stage, substage)`로 만든다.
  - `SubstageIntroBanner.Show(int stage, int substage)`의 번호 글자도 `StageText.SubstageLabel(stage, substage)`로 만든다.
  - `StageView`의 스테이지 글자(`_stageLabel`, `RoundStageLabel(...) + " · " + 번호`)의 번호 부분도 `StageText.SubstageLabel(_stage.DisplayStageNumber, _stage.SubstageNumber)`로 만든다.
- 표식 숨김: 1~5스테이지 모두 `StageProgress.IsBossActive`인 동안 `SubstagePreview`의 `Marker1`~`Marker6`와 `BossMarker` 오브젝트를 비활성화하고, 일반 구간에서는 다시 활성화한다. 진행 표시(`SubstagePreview` 오브젝트), 번호 글자(`SubstageNumber`), 보스 이름(`BossName`), 붉은 체력 바(`StageBar`)는 그대로 보인다. 방법(예: `SubstagePreview`에 `public void SetMarkersVisible(bool visible)`을 추가하고 `StageView`가 `IsBossActive`로 부름)은 직접 정하고 보고서의 가정에 적는다.
- 최종 보스 쓰러짐 대기: `StageView.OnChanged`의 페이드아웃 시작(성공 전환) 판단에, `_stage.DisplayStageNumber == StageLayout.FinalDisplayStage`이고 보스 구간(`SubstageNumber > StageLayout.NormalSubstageCount`)이며 `Art/Enemies/tianmo_final_boss_down`이 104×104 칸 25개 시트(2600×104)로 있을 때 `25 * JongnamSprites.DownFrameSeconds`(1.875초)를 기다리는 분기를 추가한다. 시트가 없거나 크기가 다르면 기다리지 않는다. 기존 `HasDownSheet(path, size, count)`를 쓴다.
- 테스트에서 시트를 없앤 경우를 만들 수 있도록 경로를 `StageView` 안의 `private static string FinalBossDownPath = "Art/Enemies/tianmo_final_boss_down";` 필드로 둔다 (readonly 아님). 테스트는 리플렉션(`BindingFlags.NonPublic | BindingFlags.Static`)으로 없는 경로를 넣고 `finally`에서 원래 값으로 되돌린다.
- 기존 테스트 `StageHudTests.ProgressHud_ChangesFromProgressToBossHealth`(064에서 `[TestCase(9, 4)]`로 바뀐 상태)의 본문에서, 보스 구간이 된 뒤의 검사(`Assert.That(stage.IsBossActive, Is.True);` 다음)에 `Marker1`~`Marker6`와 `BossMarker`의 `activeSelf`가 거짓이라는 검사를 추가한다. 이름과 기존 검사는 그대로 두고 검사 수는 줄이지 않는다. 일반 구간 반복 안의 `Marker{n}` 스프라이트·위치 검사는 비활성 오브젝트에서도 읽히므로 그대로 둔다.
- 새 플레이 모드 테스트는 `Assets/Tests/PlayMode/Stage/StageFinalHudTests.cs`(클래스 `StageFinalHudTests`, 네임스페이스 `PlumSword.Game.Tests.Stage`)에 쓴다. 만드는 법은 기존 `StageHudTests`와 같다 (`new GameObject(..., typeof(Canvas))`, `StageView.Initialize(stage, canvas)`, `stage.Begin()`, 강화 공격력 1000·최대 체력 10000, `new System.Random(1)`). 글자는 `TMPro.TextMeshProUGUI`를 기존 테스트처럼 리플렉션으로 읽는다. 쓰러짐 대기 테스트는 기존 `TianmoSpriteTests.LastNormalOpponent_StageFourWaitsForDownBeforeFade`처럼 `[UnityTest]`와 `WaitForSeconds`를 쓴다.

## 완료 조건
- [플레이 모드] `Assets/Tests/PlayMode/Stage/StageFinalHudTests.cs`에 아래 [플레이 모드] 조건(마지막 하나 제외)마다 테스트가 하나 이상 있고 모두 통과한다.
- [플레이 모드] 시작 번호 5로 `Begin()` 하면 `SubstagePreview/SubstageNumber` 글자는 `"FINAL"`이다.
- [플레이 모드] 위 상황에서 `SubstageIntroBanner/SubstageNumber` 글자는 `"FINAL"`이다.
- [플레이 모드] 위 상황에서 `SubstagePreview`는 활성이고 `Marker1`~`Marker6`와 `BossMarker`는 모두 비활성이다.
- [플레이 모드] 위 상황에서 `BossName`은 활성이고 글자는 `"HEAVENLY DEMON"`이다.
- [플레이 모드] 위 상황에서 `StageBar/Fill`의 `anchorMax.x`는 1이다.
- [플레이 모드] 시작 번호 1·2·3·4·9 각각에서 `Begin()` 직후(일반 구간) `Marker1`~`Marker6`와 `BossMarker`는 모두 활성이다.
- [플레이 모드] 시작 번호 1·2·3·4·9 각각에서 여섯 구간을 마쳐 보스 구간이 되면 `Marker1`~`Marker6`와 `BossMarker`는 모두 비활성이고 `SubstagePreview`는 활성이다.
- [플레이 모드] 위 보스 구간에서 `SubstagePreview/SubstageNumber` 글자는 `"{n}-7"`(n은 화면상 스테이지 번호: 1·2·3·4·4)이고 `BossName`과 `StageBar`는 활성이다.
- [플레이 모드] 시작 번호 5에서 최종 보스를 처치하면(`tianmo_final_boss_down` 시트가 있을 때) 1.5초 뒤 `FadeAlpha`는 0이다.
- [플레이 모드] 위 상황에서 2.5초 뒤 `FadeAlpha`는 0보다 크다.
- [플레이 모드] `FinalBossDownPath`를 없는 경로로 바꾸고 시작 번호 5에서 최종 보스를 처치하면 0.5초 뒤 `FadeAlpha`는 0보다 크다.
- [플레이 모드] 기존 `StageHudTests.ProgressHud_ChangesFromProgressToBossHealth`가 표식 비활성 검사를 더한 채 시작 번호 1·2·3·4·9·7 모두 통과한다.
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다.
- [사람 확인] 실행하고 10초 뒤 화면 위쪽의 진행 표시에 `1-1` 같은 번호와 매화 표식 6개·보스 표식이 겹치지 않고 보인다.

## 하지 말 것
- `Assets/Scripts/Core/`는 고치지 않는다 (063·064가 끝냈다).
- 보스 체력 틀(`BossHealthFrame`)과 보스 구간 `StageBar` 위치·크기는 고치지 않는다 (066이 한다).
- 064가 고친 4스테이지 일반 상대 대기 분기와 다른 스테이지의 쓰러짐 대기 분기는 바꾸지 않는다.
- `CombatView`·`JongnamSprites`는 고치지 않는다 (067이 한다).
- 기존 테스트 메서드를 지우거나 이름을 바꾸지 않는다. 요구사항에 적은 `ProgressHud_ChangesFromProgressToBossHealth` 검사 추가 말고는 기존 테스트를 고치지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 바꾸지 않는다. 쓰러짐 시트(`Assets/Resources/Art/Enemies/tianmo_final_boss_down.png`)와 `.meta`는 이미 있으니 새로 만들거나 고치지 않는다.
- `Assets/Tests/PlayMode/Capture/`는 고치지 않는다.

## 보고서

### 한 일
- 구간 번호 세 곳에 공통 번호 형식을 적용하고 보스 구간에서 진행 표식을 숨기도록 했다.
- 최종 보스 쓰러짐 시트가 지정 크기일 때 25프레임 대기 후 페이드하도록 했다.
- 최종 스테이지와 일반·보스 구간 표시 및 시트 유무별 대기 테스트를 추가하고 기존 HUD 테스트에 표식 검사를 더했다.

### 바뀐 파일
- `Assets/Scripts/Game/Stage/StageView.cs`, `SubstagePreview.cs`, `SubstageIntroBanner.cs`
- `Assets/Tests/PlayMode/Stage/StageHudTests.cs`, `StageFinalHudTests.cs`, `StageFinalHudTests.cs.meta`
- `docs/spec/stage.md`
- `tasks/doing/065_stage_view_final.md`, `.loop/result.json`

### 가정
- `SubstagePreview.SetMarkersVisible`이 표식 오브젝트의 활성 상태를 바꾸고 `StageView`가 화면 갱신마다 보스 여부를 전달한다. 다른 기능의 공개 연결 지점은 바뀌지 않았다.

### 사람이 확인할 것
- 실행하고 10초 뒤 화면 위쪽의 진행 표시에 `1-1` 같은 번호와 매화 표식 6개·보스 표식이 겹치지 않고 보인다.

### 테스트 결과
- `git diff --check` 통과. Unity 테스트와 컴파일은 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-22 04:05)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/065_stage_view_final
- 루프 브랜치: loop/20261122-035241
