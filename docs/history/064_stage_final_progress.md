# 064_stage_final_progress — 5스테이지는 최종 보스 구간만 배치

## 목표
`PlumSword.Core`의 `StageProgress`가 화면상 5스테이지에서 일반 구간 없이 7구간 최종 보스 한 명만 배치하게 한다. 이 변경으로 기대값이 깨지는 기존 플레이 모드 테스트 세 개의 케이스 데이터와, 그중 하나를 통과시키는 데 필요한 `StageView`의 대기 조건 한 줄을 함께 고친다.

## 묶음
stage45_final

## 선행 작업
- 063_stage_final_label

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stage.md ("동작" 절, "화면" 절의 4스테이지 쓰러짐 대기 줄, "검증" 절의 완료 조건 41~46·49, "변경 기록" 절)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-22 — 4·5스테이지 진행 구체화` 항목 중 5스테이지 배치 규칙(완료 조건 41~46)과 4스테이지 마지막 일반 상대 대기(완료 조건 49)만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. 화면 표시(FINAL 글자, 표식 숨김, 최종 보스 쓰러짐 대기, 보스 체력 틀)는 065·066, 전투 그림은 067이 한다.
- `Assets/Scripts/Core/Stage/StageProgress.cs`:
  - 큰 스테이지를 배치할 때(`Begin()`과 성공·실패 전환의 가려진 순간 모두. 지금의 `PlaceStage()`) `DisplayStageNumber == StageLayout.FinalDisplayStage`이면 `SubstageNumber = 7`, `DefeatedNormalCount = 0`으로 두고 보스 한 명만 `_battle.AddEnemy(EnemyKind.Boss, StageNumber, _battle.DisciplePosition + StageLayout.BossDistance)`로 배치한다. 일반 상대를 만들지 않고 `StageLayout.SubstagePositions`를 부르지 않는다 (무리 난수를 뽑지 않는다).
  - 그 뒤 순서(테마 소개 조건, 1.2초 구간 소개, 전투)는 다른 보스 구간과 같다. 능력치·보상은 기존 공식에 `StageNumber`를 넣은 값이다 (따로 붙는 보너스 없음).
  - `BarRatio`는 보스가 나타난 뒤 규칙(보스 체력 비율)을 그대로 따르므로 5스테이지에서는 처음부터 보스 체력 비율이다.
  - 5스테이지에서 패배하면 같은 5스테이지를 다시 배치한다 (체력이 가득한 새 보스, 테마 소개는 이미 본 것이므로 다시 나오지 않음). 최종 보스를 혼자 이기면 기존 규칙대로 다음 번호(다음 회차 1스테이지 1구간)로 간다.
  - 1~4스테이지의 배치는 바꾸지 않는다.
- 새 테스트는 `Assets/Tests/EditMode/Stage/StageProgressFinalTests.cs`(클래스 `StageProgressFinalTests`)에 쓴다. 지갑 `new Wallet(0)`, 조건마다 적은 공격력·최대 체력의 강화(`new StatUpgrades(wallet, 공격력, 최대 체력)`), 기본 전투 `new Battle(upgrades)`, 기존 `MinRandom`(`Next(min, max)`가 항상 `min`)으로 `StageProgress`를 만든다. 좌표·비율은 오차 1e-6 안에서 비교한다. "제자가 혼자 이긴다"는 공격력 1000·최대 체력 10000, "제자가 쓰러진다"는 공격력 1·최대 체력 1로 둔다 (임시 테스트값. 보스를 실제로 만나게 하려면 `Battle.Step()`을 반복한다).
- 이 변경으로 기대값이 깨지는 기존 플레이 모드 테스트를 아래처럼 고친다. 이름은 그대로 두고 검사 수는 줄이지 않는다.
  - `Assets/Tests/PlayMode/Stage/StageHudTests.cs`의 `StageHudTests.AllThemes_ShareTheStageOneProgressHud`: `[TestCase(5, "5-1")]`을 `[TestCase(4, "4-1")]`로 바꾼다. 다른 케이스와 본문은 그대로다.
  - `StageHudTests.ProgressHud_ChangesFromProgressToBossHealth`: `[TestCase(5, 5)]`를 `[TestCase(9, 4)]`로 바꾼다. 본문은 이 작업에서 고치지 않는다 (보스 구간 표식 비활성 검사는 065가 추가한다).
  - `Assets/Tests/PlayMode/Combat/TianmoSpriteTests.cs`의 `TianmoSpriteTests.LastNormalOpponent_StageFourWaitsForDownBeforeFade`: `new StageProgress(wallet, upgrades, battle, new System.Random(1), 5, 5)`를 `new StageProgress(wallet, upgrades, battle, new System.Random(1), 4, 4)`로 바꾼다. 나머지 본문과 기대값(0.5초 뒤 `FadeAlpha` 0, 1.5초 뒤 0보다 큼)은 그대로다.
- 위 마지막 테스트가 통과하도록 `Assets/Scripts/Game/Stage/StageView.cs`의 `OnChanged`에서 천마신교 일반 상대 쓰러짐 대기 분기의 조건 `_stage.DisplayStageNumber == 5`를 `_stage.DisplayStageNumber == 4`로 바꾼다 (`tianmo_swordsman_down`·`tianmo_guard_down` 두 시트가 모두 있을 때 `JongnamSprites.DownDuration` 대기, 일반 구간일 때만). 4스테이지 보스 분기를 포함해 다른 분기는 고치지 않는다. `StageView`의 다른 부분은 065·066이 고친다.
- 아래 기존 테스트는 고치지 않고 계속 통과해야 한다: `StageProgressRoundTests.FifthStageCleared_AdvancesToSecondRoundAtCoveredTransition`, `StageProgressIntroTickTests.FifthStageCleared_SecondRoundHasNoIntro` (배치된 최종 보스는 제자 앞 24유닛이라 첫 칸에는 닿지 않는다), `TianmoBossSpriteTests`, `StageBackgroundTests`. 이 밖의 기존 테스트가 5스테이지 일반 구간을 가정해서 깨지면 고치지 말고 blocked 로 보고한다.

## 완료 조건
- `Assets/Tests/EditMode/Stage/StageProgressFinalTests.cs`에 아래 조건(마지막 두 개 제외)마다 테스트가 하나 이상 있고 모두 통과한다.
- 공격력 1000·최대 체력 10000, 시작 번호 5로 `Begin()` 하면 `SubstageNumber`는 7이다.
- 위 상황에서 `IsBossActive`는 참이고 `Battle.Enemies`는 보스 1명뿐이다.
- 위 상황에서 그 보스의 최대 체력은 220, 공격력은 26이다.
- 위 상황에서 보스 위치는 `Battle.DisciplePosition + 24`다.
- 위 상황에서 `DefeatedNormalCount`는 0, `BarRatio`는 1.0, `Phase`는 `SubstageIntro`, 받은 `StageStarted` 번호는 [5]이다.
- 시작 번호 10으로 `Begin()` 하면 보스 1명의 최대 체력은 370, 공격력은 46이다.
- 시작 번호 10에서 그 보스를 제자가 혼자 이기면 금화가 140 는다.
- 시작 번호 5에서 보스를 제자가 혼자 이기면 금화가 90 늘고 받은 `StageCleared` 번호는 [5]이다.
- 그 뒤 `Tick(1.0)`을 하면 `StageNumber` 6, `DisplayStageNumber` 1, `SubstageNumber` 1이고 일반 상대 2명이 배치되어 있다.
- 공격력 1·최대 체력 1, 시작 번호 5에서 제자가 쓰러지면 받은 `StageFailed` 번호는 [5]이다.
- 그 뒤 `Tick(1.0)`을 하면 `StageNumber` 5, `SubstageNumber` 7이고, 적은 최대 체력·현재 체력이 220인 보스 1명뿐이며, `Phase`는 `SubstageIntro`(테마 소개 없음)이다.
- 시작 번호 4로 `Begin()` 하면 `SubstageNumber` 1이고 일반 상대 2명이 있다.
- 시작 번호 4에서 일반 구간 세 개를 마치면(`Tick`으로 소개·페이드를 넘기며) `BarRatio`는 0.5다.
- 시작 번호 4에서 일반 구간 여섯 개를 마치면 `SubstageNumber` 7이고 보스 1명이 있다.
- 시작 번호 5·본 소개 번호 4로 `Begin()` 하면 `Phase`는 `Intro`이고 적은 처음부터 보스 1명뿐이다.
- 위 상황에서 `Tick(3.0)` 뒤 `Phase`는 `SubstageIntro`이고, 이어서 `Tick(1.2)` 뒤 `Playing`이다.
- 시작 번호 5의 `Begin()`은 무리 난수를 뽑지 않는다 (`Next` 호출 수를 세는 난수로 확인: 0회).
- [플레이 모드] `TianmoSpriteTests.LastNormalOpponent_StageFourWaitsForDownBeforeFade`(시작 번호 4로 고친 것)가 통과한다: 마지막 일반 상대를 처치하면 0.5초 뒤 `FadeAlpha` 0이고 1.5초 뒤 0보다 크다.
- 기존 테스트가 모두 통과한다 (`StageHudTests`의 두 테스트는 위 요구사항대로 케이스 데이터만 바꾼 상태로).

## 하지 말 것
- 1~4스테이지의 배치·보상·전환 규칙을 바꾸지 않는다.
- 저장 형식과 `docs/spec/save.md`의 동작을 바꾸지 않는다 (불러오기는 `Begin()`의 배치를 그대로 탄다).
- `StageView`에서는 요구사항의 대기 조건 한 줄만 고친다. 표식·번호 글자·최종 보스 대기(065), 보스 체력 틀(066)은 하지 않는다.
- `CombatView`·`JongnamSprites`·`SubstagePreview`·`SubstageIntroBanner`는 고치지 않는다 (065·067이 한다).
- 기존 테스트 메서드를 지우거나 이름을 바꾸지 않는다. 요구사항에 적은 세 테스트 말고는 기존 테스트를 고치지 않는다.
- Core 에서 `UnityEngine`을 쓰지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 화면상 5스테이지를 배치할 때 곧바로 7구간 보스 한 명을 배치하도록 변경했다. 시작과 성공·실패 전환에서 같은 배치를 사용한다.
- 4스테이지 마지막 일반 상대의 쓰러짐 대기 조건과 지정된 플레이 모드 테스트의 케이스 데이터를 수정했다.
- 최종 보스의 배치, 능력치, 보상, 성공·실패 전환, 소개, 난수 호출 및 4스테이지 진행을 확인하는 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/Stage/StageProgress.cs`
- `Assets/Scripts/Game/Stage/StageView.cs`
- `Assets/Tests/EditMode/Stage/StageProgressFinalTests.cs` 및 `.meta`
- `Assets/Tests/PlayMode/Stage/StageHudTests.cs`
- `Assets/Tests/PlayMode/Combat/TianmoSpriteTests.cs`
- `tasks/doing/064_stage_final_progress.md`, `.loop/result.json`

### 가정
없음. 기존 사양과 공개 연결 지점은 변경하지 않았다.

### 사람이 확인할 것
없음.

### 테스트 결과
- `git diff --check` 통과.
- Unity 에딧 모드와 플레이 모드 테스트는 지시에 따라 직접 실행하지 않았다. 완료 보고 후 루프 스크립트의 테스트 판정을 기다린다.


## 루프 기록 (2026-11-22 04:00)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/064_stage_final_progress
- 루프 브랜치: loop/20261122-035241
