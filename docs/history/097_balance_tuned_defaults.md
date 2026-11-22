# 097_balance_tuned_defaults — 수치 표 기본값을 시뮬레이터로 정한 값으로 바꾸기

## 목표
Core(`PlumSword.Core.Balance.BalanceTable.Default`)의 보스 공격력 곡선과 천마 고정값을 시뮬레이터로 정한 값으로 바꾼다. 그리고 이 수치를 가정한 기존 테스트의 기대값과 사양 문서를 함께 고친다.

## 묶음
balance_tuning

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("순수 C# 규칙", "테스트")
- docs/spec/balance.md ("목표", "동작"의 천마 항목, "수치" 표)
- docs/spec/combat.md ("수치"의 보스 공격력 줄)
- docs/spec/stage.md (최종 보스 능력치 문장, 완료 조건 41·44)
- docs/spec/regression.md ("목표")

## 요구사항
- 배경: 자동 강화를 켠 새 게임에서 천마를 약 21분, 회귀 3번 만에 처음 잡도록 시뮬레이터(`tools/balance/`, 2026-11-22, 5 seed)로 정한 값이다. 결과는 천마 첫 처치 중앙값 21:29(21:21~21:31), 평균 회귀 3.0이다. 첫 판의 1스테이지 보스 패배는 없다. 시뮬레이터는 이 작업에서 고치지도 실행하지도 않는다.
- `Assets/Scripts/Core/Balance/BalanceTable.cs`의 `Default`에서 아래 세 값만 바꾼다. 나머지 값은 그대로다.
  - `BossAttack`: `new GrowthCurve(10, 4, 1)` → `new GrowthCurve(4, 4, 1)`. 스테이지 1~4의 보스 공격력은 10/14/18/22에서 4/8/12/16이 된다.
  - `FinalBossHealth`: `0` → `1500`. 화면상 스테이지 5(전체 번호 5, 10, 15 …)의 보스 체력이 1500이다.
  - `FinalBossAttack`: `0` → `80`.
- 공식과 연결 지점은 바꾸지 않는다. `GrowthCurve`, `EnemyStats`, `StageRewards`, `Battle`, `StageProgress`의 코드는 고치지 않는다. 그래서 `EnemyStats.Attack(Boss, s)` = 내림((4 + 4 × (s − 1)) × 1^(s−1))이다. 단, 화면상 스테이지 5이면 80이다. `EnemyStats.MaxHealth(Boss, s)`는 화면상 스테이지 5이면 1500이고, 아니면 지금처럼 100 + 30 × (s − 1)이다.
- 새 테스트: `Assets/Tests/EditMode/Balance/BalanceTunedDefaultsTests.cs` (네임스페이스 `PlumSword.Core.Tests.Balance`). 기본 표(`BalanceTable.Current`를 바꾸지 않은 상태)로 아래 "완료 조건"을 검사한다. 다른 테스트가 `Current`를 바꿨을 수 있으니 `SetUp`에서 `BalanceTable.Current = BalanceTable.Default`로 맞추고, `TearDown`에서도 같은 값으로 되돌린다.
- 기존 테스트는 **이름과 검사 수를 그대로 두고** 아래 값만 바꾼다. 줄 번호는 main `15c7737` 기준이다. 모두 새 기본값에서 통과하는 것을 Unity 밖에서 같은 소스로 확인한 값이다.
  - `BalanceTableTests.Default_AllProperties_MatchDefaults`: `Curve(t.BossAttack, 10, 4, 1)` → `Curve(t.BossAttack, 4, 4, 1)`. `Assert.That(t.FinalBossHealth, Is.Zero)` → `Is.EqualTo(1500)`. `Assert.That(t.FinalBossAttack, Is.Zero)` → `Is.EqualTo(80)`.
  - `EnemyStatsTests.Attack_BossByStage_IncreasesByFour`: TestCase `(1, 10L)`, `(2, 14L)`, `(3, 18L)` → `(1, 4L)`, `(2, 8L)`, `(3, 12L)`. (4씩 오르는 것은 그대로라 이름이 맞다)
  - `EnemyStatsTests.Attack_BossAtIntMaxStage_DoesNotOverflow`: `8589934594L` → `8589934588L`. (int.MaxValue 는 화면상 스테이지 2라 천마가 아니다. 4 + 4 × 2147483646)
  - `BattleTests.AddEnemy_NormalThenBoss_CreatesEnemiesWithStats`: `boss.Attack` 기대값 `10L` → `4L`.
  - `BattleAttackTests.BossInRange_TradesHitsUntilDefeated`: `DiscipleHealth` `90L` → `96L`, `50L` → `80L`.
  - `BattleAttackTests.DiscipleWalksToBoss_BossAttacksOnArrivalDiscipleAfterWindUp`: `86L` → `92L`.
  - `BattleAttackTests.CooldownRunsWhileMoving_WaitsRemainingTime`: `81L` → `87L`.
  - `BattleAttackTests.ResetAfterCombat_RestoresStartStateWithoutEnemyDefeated`: `90L` → `96L`.
  - `BattleAttackTests.DiscipleAndBossDefeatedSameStep_DiscipleDefeatedFlagIsTrueAtEnemyDefeated`: 준비값 `Create(100, 10)` → `Create(100, 4)`. 1스테이지 보스 공격력이 4가 되어서, 제자 체력을 4로 해야 같은 칸에 함께 쓰러지는 상황이 그대로 만들어진다. 검사는 그대로다.
  - `BattleHoldPositionTests.Step_HoldingWithBossInRange_EnemyAttacks`: `86L` → `92L`.
  - `BattleRealmTests`의 도우미 `TakeBossHit()`: `Assert.AreEqual(90L, …)` → `96L`. 이 도우미를 쓰는 테스트의 값도 바꾼다:
    - `Breakthrough_AfterDamage_IncreasesCurrentHealthByMaxDifference`: `100L` → `106L`.
    - `HealthUpgrade_AfterBreakthrough_UsesUpdatedMaxHealth`: `122L` → `128L`.
    - `Breakthrough_WithoutRealmSubscription_LeavesCurrentHealthUnchanged`: `90L` → `96L`.
  - `BattleSkillTests.SkillAttack_Boss_CooldownsAndSubsequentTurns`: `DiscipleHealth` `90` → `96`.
  - `BattleUpgradeTests.HealthUpgradeWhileDamaged_RaisesCurrentHealthByIncrease`: `90` → `96`, `110` → `116`.
  - `BattleUpgradeTests.AttackUpgrade_DoesNotChangeCurrentHealth`: `110` → `116`.
  - `BattleUpgradeTests.HealthUpgradeTwice_RaisesCurrentHealthTwice`: `90` → `96`, `130` → `136`.
  - `BattleUpgradeTests.EnemyAddedInsideEnemyDefeated_ActsFromNextStep`: `85` → `91`.
  - `JongnamBossBehaviorTests.ThirdAttack_LaunchesProjectile_DamageOccursOnlyOnImpact`: `980` 두 곳 → `992`, `970` → `988`.
  - `SfxTriggersTests.BossAndDiscipleDefeatedTogether_RequestsOnlyPlayerOutcome`: 준비값 `Create(attack: 100, health: 10)` → `Create(attack: 100, health: 4)`. 이유는 위의 함께 쓰러지는 테스트와 같다.
  - `StageProgressFadeTests.ArrangedStates_MatchPreconditions`: `DiscipleHealth` `90` → `96`.
  - `StageProgressOutcomeTests.BossDefeated_StageClearedAndTransitionStarted`: `DiscipleHealth` `90` → `96`.
  - `StageProgressOutcomeTests.BossAndDiscipleDefeatedTogether_FailedWithoutBossGold`: 준비값 `Create(100, 10)` → `Create(100, 4)`. 이유는 위와 같다.
  - `StageProgressFinalTests.Begin_FifthStage_PlacesOnlyFinalBossAndStartsSubstageIntro`: `boss.MaxHealth` `220` → `1500`, `boss.Attack` `26` → `80`.
  - `StageProgressFinalTests.TenthStage_BossUsesTotalStageStatsAndReward`: `MaxHealth` `370` → `1500`, `Attack` `46` → `80`. 보상 `140`은 그대로다 (`BossGold`는 바뀌지 않는다).
  - `StageProgressFinalTests.FifthStage_DiscipleDefeated_RetriesWithFreshBossWithoutThemeIntro`: `MaxHealth`, `Health` `220` → `1500`.
  - `StageProgressIntroTickTests.FifthStageCleared_SecondRoundHasNoIntro`: 준비값 `Create(start: 5, seen: 4)` → `Create(attack: 1500, start: 5, seen: 4)`. 천마 체력이 1500이 되어서, 한 칸에 쓰러뜨리려면 공격력이 1500이어야 한다. 검사는 그대로다.
  - `StageProgressRoundTests.FifthStageCleared_AdvancesToSecondRoundAtCoveredTransition`: 준비값 `Create(1000, 100, 5)` → `Create(1500, 100, 5)`. 이유는 위와 같다. 금화 `90`은 그대로다.
  - `StageProgressRoundTests.SixthStageEnemies_UseTotalStageNumberForStats`: `boss.Attack` `30` → `24`. `boss.MaxHealth` `250`은 그대로다.
  - `StageProgressRoundTests.SixthStageFailed_RetriesSameRoundAndResetsBattle`: 준비값 `Create(10, 30, 6)` → `Create(10, 24, 6)` (보스 한 대에 쓰러지게 한다). 끝의 `DiscipleHealth` `30` → `24`. `boss.Health` `240`은 그대로다.
  - 플레이 모드 `JongnamBossSpriteTests.StageOne_BossAnimatesNormalAndSkillWithoutChangingDamage`: `DiscipleHealth` `970` → `988` (1스테이지 보스 공격 3번 × 4).
- 바뀌지 않는다고 본 플레이 모드 흐름 테스트: `FullRoundFlowTests.FreshLaunch_DefeatFinalBoss_ShowsEndingAndClearsSave`(공격력 410, 체력 4100으로 천마 1500/80을 이긴다. 금화 범위 974~1286은 보상 곡선이 그대로라 그대로다), `SaveReloadFlowTests`, `GameFlowTests`, `OfflineRewardFlowTests`, `TianmoFinalBossSpriteTests`. 기대값을 고치지 않는다. 이 테스트가 떨어지면 기대값을 맞춰 고치지 말고 `blocked`로 보고한다 (실패 메시지와 실제 값을 적는다).
- 위 목록 밖의 기존 테스트가 떨어지면 기대값을 고치지 말고 `blocked`로 보고한다.
- 사양 문서를 고친다:
  - `docs/spec/balance.md`: "수치" 표의 `BossAttack` 기본값을 (4, 4, 1.0)으로, `FinalBossHealth`, `FinalBossAttack` 기본값을 1500, 80으로 고친다. "목표"와 "수치"에서 "기본값은 지금 게임 수치와 같다"는 문장은 "기본값은 2026-11-22 시뮬레이터로 정한 값이다 (자동 강화 기준 천마 첫 처치 약 21분 30초, 회귀 3회)"로 고친다. "동작"의 천마 항목에서 "기본값은 둘 다 0이라 지금과 같다"를 "기본값은 1500, 80이다"로 고친다. 예시 문장 중 바뀐 값이 있으면 맞춘다. "동작" 표의 "지금 쓰는 상수·식" 칸은 옛 상수의 기록이라 그대로 둔다.
  - `docs/spec/combat.md` "수치"의 보스 공격력 줄: `4 + 4 × (stage − 1)`, 예 1·2·3스테이지 → 4·8·12. 화면상 스테이지 5는 `FinalBossAttack`(80)이라고 적는다.
  - `docs/spec/stage.md`: 최종 보스 문장을 "체력 1500, 공격력 80(`BalanceTable.FinalBossHealth`, `FinalBossAttack`), 보상은 기존 공식(1회차 90, 2회차 140)"으로 고치고, 완료 조건 41·44의 220·26을 1500·80으로 맞춘다.
  - 사양을 고쳤으면 보고서의 "가정"에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/Balance/BalanceTunedDefaultsTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 기본 표의 `BossAttack.Evaluate(0)`, `Evaluate(1)`, `Evaluate(2)`, `Evaluate(3)`은 각각 4, 8, 12, 16이다.
- 기본 표에서 `EnemyStats.Attack(EnemyKind.Boss, 4)`는 16이다.
- 기본 표에서 `EnemyStats.MaxHealth(EnemyKind.Boss, 4)`는 190이다 (보스 체력 곡선은 그대로).
- 기본 표에서 `EnemyStats.MaxHealth(EnemyKind.Boss, 5)`는 1500이다.
- 기본 표에서 `EnemyStats.Attack(EnemyKind.Boss, 5)`는 80이다.
- 기본 표에서 `EnemyStats.MaxHealth(EnemyKind.Boss, 10)`은 1500이고 `EnemyStats.Attack(EnemyKind.Boss, 10)`은 80이다.
- 기본 표에서 `EnemyStats.Attack(EnemyKind.Boss, 6)`은 24이다 (2회차 1스테이지 보스는 천마가 아니다).
- 기본 표에서 `EnemyStats.Attack(EnemyKind.Normal, 4)`는 11이고 `StageRewards.BossGold(5)`는 90이다 (바꾸지 않은 값은 그대로).
- 요구사항에 적은 기존 테스트는 모두 이름이 그대로이고, 적힌 값만 바뀐 채 통과한다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `BalanceTable.Default`의 세 값(`BossAttack`, `FinalBossHealth`, `FinalBossAttack`) 말고 다른 값을 바꾸지 않는다.
- `GrowthCurve`, `EnemyStats`, `StageRewards`, `Battle`, `StageProgress`, `GameRoot` 등 다른 코드를 고치지 않는다.
- 기존 테스트를 지우거나 이름을 바꾸거나 검사를 줄이지 않는다. 요구사항 목록에 없는 기대값을 바꾸지 않는다.
- `tools/balance/`(시뮬레이터)를 고치거나 실행하지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)와 `docs/spec/records/`를 고치지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/`를 건드리지 않는다.

## 보고서

### 한 일
- 보스 공격력 곡선과 천마 체력·공격력 기본값을 지정된 값으로 변경했다.
- 지정된 기존 테스트의 기대값과 준비값을 수정하고, 기본값의 곡선·천마·변하지 않은 수치를 검증하는 에딧 모드 테스트를 추가했다.
- 수치 표, 전투, 스테이지 사양에 변경된 수치를 반영했다.

### 바뀐 파일
- `Assets/Scripts/Core/Balance/BalanceTable.cs`
- `Assets/Tests/EditMode/Balance/BalanceTunedDefaultsTests.cs` 및 `.meta`, `BalanceTableTests.cs`
- `Assets/Tests/EditMode/Combat/`의 지정된 전투·적 능력치 테스트, `Assets/Tests/EditMode/Audio/SfxTriggersTests.cs`
- `Assets/Tests/EditMode/Stage/`의 지정된 스테이지 테스트, `Assets/Tests/PlayMode/Combat/JongnamBossSpriteTests.cs`
- `docs/spec/balance.md`, `docs/spec/combat.md`, `docs/spec/stage.md`
- `tasks/doing/097_balance_tuned_defaults.md`, `.loop/result.json`

### 가정
- 작업 요구사항에 따라 세 사양 문서의 현재 기본값과 완료 조건을 갱신했다. 공개 연결 지점과 계산식은 바꾸지 않았다.
- 추가 가정이나 임시 수치는 없다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- `git diff --check` 통과. 기존 테스트의 변경 내용을 지정된 값과 대조했다.
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 전체 테스트 판정을 기다린다. 시뮬레이터도 실행하지 않았다.


## 루프 기록 (2026-11-22 22:54)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/097_balance_tuned_defaults
- 루프 브랜치: loop/20261122-224840
