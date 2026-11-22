# 088_balance_usage — 강화·스킬·공격 간격·경지가 수치 표를 읽게 바꾸기

## 목표
Core 의 강화(`StatUpgrades`), 전투(`Battle`의 스킬·제자 공격 간격, `EnemyStats.AttackInterval`), 경지(`RealmRules`)가 상수 대신 `BalanceTable.Current`를 읽게 바꾼다. 기본 표는 지금 수치와 같아서 기존 테스트의 결과는 바뀌지 않는다.

## 묶음
balance

## 선행 작업
- 087_balance_table

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/balance.md ("동작"의 표, "연결 지점")
- docs/spec/stat_upgrade.md ("연결 지점" 절)
- docs/spec/combat.md ("연결 지점" 절)
- docs/spec/realm.md ("연결 지점" 절)

## 요구사항
- 087 이 만든 `PlumSword.Core.Balance.BalanceTable.Current`를 **계산할 때마다** 읽는다. 값을 필드에 저장해 두지 않는다 (테스트가 `Current`를 바꾸면 이미 만든 객체에도 바로 반영돼야 한다).
- `Assets/Scripts/Core/StatUpgrade/StatUpgrades.cs`
  - `GetCost(stat)`: 무력 `Current.AttackCost.Evaluate(단계)`, 체력 `Current.HealthCost.Evaluate(단계)`, 내공 `Current.InnerPowerCost.Evaluate(단계)`. 단계는 `GetLevel(stat)` 그대로 `double`로 넘긴다.
  - `Attack` = `(baseAttack + Current.AttackPerLevel × 무력 단계) × (100 + 경지 보너스) / 100`, `MaxHealth` = `(baseMaxHealth + Current.HealthPerLevel × 체력 단계) × (100 + 경지 보너스) / 100`. 정수 계산 방식(`long`, 정수 나눗셈)은 지금과 같다.
  - 지금의 private 상수 `BaseCost`, `CostPerLevel`, `AttackPerLevel`, `MaxHealthPerLevel`은 계산에 쓰지 않는다 (지워도 된다. 공개가 아니다).
- `Assets/Scripts/Core/Combat/Battle.cs`
  - `CalculateSkillDamage(attack, innerPowerLevel)` = `attack × (Current.SkillBasePercent + Current.SkillPercentPerInnerLevel × 내공 단계) / 100`.
  - 스킬을 쓸 때 재사용 대기를 `Current.SkillCooldownSeconds`로 둔다 (지금 `SkillCooldownSeconds` 상수를 넣는 자리).
  - 제자 공격 간격: `DiscipleAttackInterval` 속성과 앞 동작·공격 뒤 대기(지금 `DiscipleBaseAttackInterval`을 쓰는 자리 모두)를 `Current.DiscipleAttackInterval`로 바꾼다. 앞 동작 비율 `DiscipleAttackWindUpRatio`는 그대로다.
  - 공개 상수 `DiscipleBaseAttack`, `DiscipleBaseMaxHealth`, `DiscipleBaseAttackInterval`, `SkillCooldownSeconds`, `SkillBasePercent`, `SkillPercentPerInnerLevel`은 **지우지 않는다** (값도 그대로). 기본값의 설명으로 남기고 계산에는 쓰지 않는다. 이 상수를 확인하는 기존 테스트(`BattleTests`, `BattleSkillTests`)는 그대로 통과해야 한다.
- `Assets/Scripts/Core/Combat/EnemyStats.cs`: `AttackInterval(kind)`는 일반 `Current.NormalAttackInterval`, 보스 `Current.BossAttackInterval`. 정의되지 않은 `kind`의 예외는 지금과 같다. 체력·공격력 계산은 이 작업에서 바꾸지 않는다 (089).
- `Assets/Scripts/Core/Realm/RealmRules.cs`
  - `BonusPercent(realm)` = `Current.RealmBonusPercent[(int)realm]`.
  - `KillsRequired(from)`: 삼류~절정은 `Current.RealmKillsRequired[(int)from]`, 초절정은 지금처럼 0. 정의되지 않은 값의 예외는 지금과 같다.
- 바꾼 메서드의 XML 주석에 적힌 식(예: `20 + 5 * 단계`)을 "수치 표의 …" 로 고친다.
- 기존 테스트: 기본 표가 지금 수치와 같으므로 기대값이 바뀌는 기존 테스트는 없다. 기존 테스트는 고치지 않는다.
- 끝나면 `docs/spec/stat_upgrade.md`, `docs/spec/combat.md`, `docs/spec/realm.md`의 "연결 지점" 절에서 공식을 적은 주석이 있으면 "수치 표(`BalanceTable.Current`)의 값"으로 고친다. 연결 지점의 모양(메서드 이름·인자)은 바꾸지 않는다.
- 테스트는 `BalanceTable.Current`를 바꾼 표로 확인하고, `[TearDown]`에서 `BalanceTable.Current = BalanceTable.Default`로 되돌린다. 표는 `var table = BalanceTable.Default; table.… = …; BalanceTable.Current = table;`처럼 만든다.

## 완료 조건
- `Assets/Tests/EditMode/Balance/BalanceTableUsageTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다 (089 가 같은 파일에 테스트를 더한다).
- 12. `AttackCost`를 (100, 0, 2)로 바꾸면 `new StatUpgrades(wallet, 10, 100, 3)`의 `GetCost(StatType.Attack)`은 800이고 `GetCost(StatType.Health)`는 20이다.
- 13. `AttackPerLevel`을 5로, `HealthPerLevel`을 50으로 바꾸면 `new StatUpgrades(wallet, 10, 100, 2, 2)`의 `Attack`은 20, `MaxHealth`는 200이다.
- 14. `SkillBasePercent`를 300으로, `SkillPercentPerInnerLevel`을 20으로 바꾸면 `Battle.CalculateSkillDamage(10, 2)`는 34다.
- 19. `RealmKillsRequired`를 {1, 2, 3, 4}로, `RealmBonusPercent`를 {0, 50, 60, 70, 80}으로 바꾸면 `RealmRules.KillsRequired(RealmLevel.ThirdRate)`는 1, `RealmRules.BonusPercent(RealmLevel.SecondRate)`는 50이다.
- 20. `NormalAttackInterval`을 3.0으로 바꾸면 `EnemyStats.AttackInterval(EnemyKind.Normal)`은 3.0이다.
- (추가) `DiscipleAttackInterval`을 2.0으로 바꾸면 `new Battle(new StatUpgrades(new Wallet(0), 10, 100)).DiscipleAttackInterval`은 2.0이다.
- (추가) `SkillCooldownSeconds`를 3.0으로 바꾸면, 스킬이 켜진 전투 `new Battle(new StatUpgrades(new Wallet(1000), 10, 100), null, true)`에 `AddEnemy(EnemyKind.Boss, 1, 0.8)`을 두고 `Step()` 1번 뒤 `SkillCooldown`은 3.0이다 (기본 표에서는 5.0이다).
- 24. 기존 테스트가 모두 통과한다 (기본 표는 지금 수치와 같다).

## 하지 말 것
- `EnemyStats.MaxHealth`·`Attack`, `StageRewards`, `Battle.AddEnemy`, `StageProgress`는 고치지 않는다 (089 가 구간 인자와 함께 한다).
- `GameRoot`와 `Assets/Scripts/Game/`은 고치지 않는다 (090).
- `Battle`의 공개 상수를 지우거나 값을 바꾸지 않는다.
- 기존 테스트를 고치지 않는다.
- 전투 좌표·이동 속도·사거리·스킬 타수 같은 동작 수치를 표로 옮기지 않는다.
- `UnityEngine`을 쓰지 않는다. 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 강화 비용과 능력치, 스킬 피해와 재사용 대기, 제자와 적의 공격 간격, 경지 처치 수와 보너스가 계산 시 현재 수치 표를 읽도록 변경했다.
- 바뀐 표와 기존 객체, 종류별 값, 앞 동작과 공격 후 대기, 예외를 검증하는 에딧 모드 테스트를 추가하고 관련 사양을 갱신했다.

### 바뀐 파일
- `Assets/Scripts/Core/StatUpgrade/StatUpgrades.cs`, `Assets/Scripts/Core/Combat/Battle.cs`, `Assets/Scripts/Core/Combat/EnemyStats.cs`, `Assets/Scripts/Core/Realm/RealmRules.cs`
- `Assets/Tests/EditMode/Balance/BalanceTableUsageTests.cs` 및 `.meta`
- `docs/spec/stat_upgrade.md`, `docs/spec/combat.md`, `docs/spec/realm.md`
- `tasks/doing/088_balance_usage.md`, `.loop/result.json`

### 가정
- 없음. 공개 연결 지점의 모양과 기본 상수는 유지했다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 새 테스트와 기존 전체 테스트의 결과는 루프 판정을 기다린다.
- `git diff --check` 통과.


## 루프 기록 (2026-11-22 16:19)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/088_balance_usage
- 루프 브랜치: loop/20261122-160726
