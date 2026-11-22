# 082_stage_clear_wait — 마지막 적을 이긴 뒤 전환 전 대기 단계

## 목표
Core(`PlumSword.Core.Stage.StageProgress`)에 "결판 뒤 대기"(`StagePhase.ClearWait`) 단계를 더한다. 구간의 마지막 적이나 보스를 이긴 뒤 바로 페이드아웃하지 않는다. 정해진 시간 동안 전투를 멈추지 않고 제자만 제자리에 세워 두어, 동작과 피해 숫자가 자연스럽게 끝나게 한다.

## 묶음
clear_wait

## 선행 작업
- 081_battle_hold_position

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stage.md ("동작", "수치", "연결 지점")
- docs/spec/combat.md ("연결 지점"의 Battle. 081이 `IsHoldingPosition`, `SetHoldPosition(bool)`을 더했다)

## 요구사항
- `StagePhase`에 `ClearWait = 5`를 더한다. 설명: "마지막 적을 이긴 뒤 전환까지 기다리는 중. 전투는 멈추지 않고 제자는 제자리에 선다."
- `StageProgress` 생성자의 맨 끝에 선택 인자 `double clearWaitSeconds = 0.0`을 더한다: `StageProgress(Wallet wallet, StatUpgrades upgrades, Battle battle, Random random, int startStage = 1, int seenIntroStage = 5, double clearWaitSeconds = 0.0)`. 음수·NaN·무한대이면 `ArgumentOutOfRangeException`을 던진다. 읽기용 `public double ClearWaitSeconds { get; }`와 게임이 쓸 값 `public const double DefaultClearWaitSeconds = 1.5;`(임시값)를 더한다.
- **`clearWaitSeconds`가 0이면 지금과 완전히 같다** (`ClearWait` 단계가 생기지 않는다). 그래서 기존 테스트는 바뀌지 않는다.
- **`clearWaitSeconds`가 0보다 크면**, 성공 전환을 시작하는 두 곳에서 바로 `FadingOut`으로 가지 않고 `ClearWait`로 간다.
  - 일반 구간의 현재 무리를 모두 이겼을 때(제자가 쓰러지지 않음): 보상·처치 수는 지금처럼 바로 준다. `Phase = ClearWait`, 대기 시간 0, `battle.SetHoldPosition(true)`, 강화 잠금(`upgrades.SetLocked(true)`)을 한다. 전투는 멈추지 않는다. `Changed`를 보낸다.
  - 보스를 혼자 이겼을 때: 보스 금화를 바로 주고 다음 번호를 기억한다. 위와 같이 `ClearWait`로 간 뒤 `StageCleared(끝난 번호)`, `Changed` 순서로 보낸다 (이때 `Phase`는 `ClearWait`).
  - 실패(제자 쓰러짐)는 바뀌지 않는다. 바로 `FadingOut`이다.
- `ClearWait` 동안:
  - `FadeAlpha`는 0이다. `IsBossActive`와 `Boss`는 지금처럼 다음 배치 때까지 그대로다.
  - 전투 알림(`EnemyDefeated`, `DiscipleDefeated`, `EnemyDamaged`)은 무시한다. `Playing`이 아니므로 지금의 `IsListening` 규칙 그대로다. 보스 구간에서 남은 종남 검기에 제자가 쓰러져도 실패하지 않는다. 다음 큰 스테이지 배치의 `battle.Reset()`이 되살린다.
- `Tick(deltaSeconds)`에서 `ClearWait`이면 대기 시간을 더한다. 대기 시간이 `ClearWaitSeconds − 1e-9` 이상이 되면 `Phase = FadingOut`으로 바꾼다. 페이드 시간은 남는 시간(대기 시간 − `ClearWaitSeconds`)으로 시작하고, `battle.SetPaused(true)`를 부른 뒤 `Changed`를 보낸다. 같은 `Tick` 안에서 페이드아웃의 가려진 순간·페이드인 끝으로 이어질 수 있는 것은 지금의 페이드 처리와 같다 (남는 시간을 넘긴다).
- 가려진 순간의 교체(구간 교체와 큰 스테이지 교체 모두)에서 `battle.SetHoldPosition(false)`를 부른다.
- `StageText` 등 다른 표시와 저장은 바꾸지 않는다.
- 연결 지점이 이 문서와 달라지면 `docs/spec/stage.md`의 연결 지점 절을 고치고 보고서의 가정에 적는다. 끝나면 `docs/spec/stage.md`의 "동작"(성공 전환), "수치"(결판 뒤 대기 1.5초, 임시), "연결 지점"(생성자, `ClearWaitSeconds`, `DefaultClearWaitSeconds`, `StagePhase.ClearWait = 5`)을 고친다.
- 083은 `StagePhase.ClearWait`, `StageProgress.DefaultClearWaitSeconds`, `ClearWaitSeconds`를 그대로 쓴다.

## 완료 조건
- `Assets/Tests/EditMode/Stage/StageProgressClearWaitTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 공통 준비: `wallet = new Wallet(0)`, `upgrades = new StatUpgrades(wallet, 1000, 10000)`, `battle = new Battle(upgrades)`, `stage = new StageProgress(wallet, upgrades, battle, new MinRandom(), 2, 5, 1.5)`. `Begin()` 뒤 `Tick(1.2)`로 구간 소개를 끝내고, "결판까지"는 `Phase`가 `Playing`이 아닐 때까지 `battle.Step()`을 부르는 것(최대 5000번)이다. `MinRandom`은 기존 `Assets/Tests/EditMode/Stage/MinRandom.cs`(구간마다 2명)다.
- 생성자에 `clearWaitSeconds`로 −0.1, `double.NaN`, `double.PositiveInfinity`를 주면 각각 `ArgumentOutOfRangeException`을 던진다.
- 인자를 주지 않은 `StageProgress`의 `ClearWaitSeconds`는 0이고, `StageProgress.DefaultClearWaitSeconds`는 1.5다.
- 공통 준비 뒤 결판까지 진행하면 `Phase`는 `ClearWait`이다.
- 그때 `FadeAlpha`는 0이다.
- 그때 `battle.Paused`는 `false`다.
- 그때 `battle.IsHoldingPosition`은 `true`다.
- 그때 `upgrades.Locked`는 `true`다.
- 그때 `wallet` 잔액은 24다 (2스테이지 일반 상대 12 × 2).
- 결판 뒤 `Tick(1.4)`이면 `Phase`는 `ClearWait` 그대로다.
- 결판 뒤 `Tick(1.4)`, `Tick(0.2)`이면 `Phase`는 `FadingOut`이고 `FadeAlpha`는 0.2(오차 1e-6)이다.
- 같은 상황에서 `battle.Paused`는 `true`다.
- 결판 뒤 `ClearWait`에서 `FadingOut`으로 바뀌는 `Tick` 안에서 `Changed`가 1번 이상 온다.
- 결판 뒤 `Tick(1.6)`, `Tick(0.4)`이면 `SubstageNumber`는 2이고 `Phase`는 `FadingIn`이다.
- 같은 상황에서 `battle.IsHoldingPosition`은 `false`다.
- 결판 뒤 `ClearWait`에서 `battle.AddEnemy(EnemyKind.Normal, 2, battle.DisciplePosition + 0.8)`을 두고 그 적이 쓰러질 때까지 `battle.Step()`을 부르면 잔액은 24 그대로이고 `Phase`는 `ClearWait`이다.
- 공통 준비에서 구간 6개를 모두 이기고(구간마다 결판까지 진행 → `Tick(1.5)` → `Tick(1.0)` → `Tick(1.2)`) 보스까지 결판을 내면 `Phase`는 `ClearWait`이다.
- 같은 상황에서 `StageCleared`는 1번, 값 2로 왔고, 그 알림 안에서 읽은 `Phase`는 `ClearWait`이다.
- 같은 상황에서 잔액은 204다 (일반 12 × 12 + 보스 60).
- 이어서 `Tick(1.5)`, `Tick(0.5)`이면 `StageNumber`는 3이고 `SubstageNumber`는 1이다.
- 같은 상황에서 `battle.IsHoldingPosition`은 `false`다.
- `clearWaitSeconds`를 주지 않은 `StageProgress`(나머지는 공통 준비와 같음)에서 결판까지 진행하면 `Phase`는 `FadingOut`이고 `battle.Paused`는 `true`다.
- 공통 준비를 `new StatUpgrades(wallet, 1, 1)`로 바꿔 제자가 쓰러질 때까지 진행하면 `Phase`는 `FadingOut`이고 `StageFailed`가 1번 온다 (실패는 기다리지 않는다).
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 전투 코드(`Assets/Scripts/Core/Combat/`)를 고치지 않는다. 081이 만든 `SetHoldPosition`, `IsHoldingPosition`을 쓰기만 한다.
- 화면 코드와 `GameRoot`(`Assets/Scripts/Game/`)를 고치지 않는다. 083이 한다.
- 페이드·소개 시간, 보상, 실패 전환, 저장 값을 바꾸지 않는다.
- 기존 `StageProgress` 테스트의 기대값을 바꾸지 않는다 (기본값 0이라 바뀌지 않아야 한다).
- `UnityEngine`을 쓰지 않는다.

## 보고서

### 한 일
- 일반 구간과 보스 성공에 선택적인 결판 뒤 대기를 추가하고, 남은 시간을 페이드에 전달하며 가려진 순간 제자리 유지를 해제했다.
- 생성자 검증, 보상과 이벤트 순서, 대기·전환·실패 및 기존 기본값을 검증하는 에딧 모드 테스트를 추가했다.
- 스테이지 사양의 동작, 수치와 연결 지점을 갱신했다.

### 바뀐 파일
- `Assets/Scripts/Core/Stage/StagePhase.cs`
- `Assets/Scripts/Core/Stage/StageProgress.cs`
- `Assets/Tests/EditMode/Stage/StageProgressClearWaitTests.cs` 및 `.meta`
- `docs/spec/stage.md`
- `tasks/doing/082_stage_clear_wait.md`, `.loop/result.json`

### 가정
- 없음. 공개 연결 지점은 작업 문서에 지정된 형태를 따랐다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 실행과 판정을 기다린다.
- `git diff --check` 통과. 추가된 테스트를 포함한 Unity 컴파일 및 실행 결과는 아직 확인하지 않았다.


## 루프 기록 (2026-11-22 13:49)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/082_stage_clear_wait
- 루프 브랜치: loop/20261122-133943
