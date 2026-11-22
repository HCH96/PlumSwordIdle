# 수치 표 사양

- 기능 이름: balance (네임스페이스 `PlumSword.Core.Balance`)
- 작성일: 2026-11-22

## 목표

밸런스에 닿는 수치(강화 비용·레벨당 상승·시작 능력치, 스킬, 몬스터 능력치, 처치 보상, 천마, 경지)를 수치 표 `BalanceTable` 하나에 모으고, 게임 전체가 `BalanceTable.Current`를 읽게 한다. 시뮬레이터(범위 밖)가 이 값을 바꿔 가며 20분 플레이를 맞춘다. 기본값 `BalanceTable.Default`는 지금 게임의 수치와 똑같아서, 이 사양을 구현해도 게임과 기존 테스트의 결과는 바뀌지 않는다. 실제 새 수치는 시뮬레이터로 정한 뒤 별도로 넣는다.

출처: `docs/design/milestone_4.md` "목표와 범위"(임시 수치 조정), 2026-11-22 대화.

## 동작

- `BalanceTable`은 값을 담는 클래스다. 모든 값은 읽고 쓸 수 있는 속성이다. `BalanceTable.Default`는 부를 때마다 지금 수치로 채운 **새 객체**를 돌려준다 (서로 공유하지 않는다).
- `BalanceTable.Current`는 게임 전체가 읽는 하나의 표다. 처음 값은 `Default`다. 쓸 때 `Validate()`로 검사하고, 통과하면 그 객체를 그대로 쓴다. 게임 코드는 `Current`를 바꾸지 않는다. 바꾸는 쪽은 시뮬레이터와 테스트뿐이다. 테스트가 바꿨으면 끝날 때(`TearDown`) `BalanceTable.Current = BalanceTable.Default`로 되돌린다.
- 수치를 쓰는 기능은 계산할 때마다 `BalanceTable.Current`를 읽는다 (값을 따로 저장해 두지 않는다). 아래 표의 기능이 읽는 값이다.

| 기능 | 지금 쓰는 상수·식 | 바뀐 뒤 읽는 값 |
| --- | --- | --- |
| 강화 비용 `StatUpgrades.GetCost` | `20 + 5 × 단계` | `AttackCost` / `HealthCost` / `InnerPowerCost`의 `Evaluate(단계)` |
| 레벨당 상승 `StatUpgrades.Attack`, `MaxHealth` | 2, 20 | `AttackPerLevel`, `HealthPerLevel` |
| 시작 능력치 (`GameRoot`가 `StatUpgrades`에 넣음) | `Battle.DiscipleBaseAttack` 10, `DiscipleBaseMaxHealth` 100 | `DiscipleBaseAttack`, `DiscipleBaseMaxHealth` |
| 제자 공격 간격 (`Battle`의 앞 동작·공격 뒤 대기, `DiscipleAttackInterval`) | `Battle.DiscipleBaseAttackInterval` 1.0 | `DiscipleAttackInterval` |
| 스킬 피해 `Battle.CalculateSkillDamage` | 200 + 10 × 내공 | `SkillBasePercent`, `SkillPercentPerInnerLevel` |
| 스킬 재사용 대기 | `Battle.SkillCooldownSeconds` 5.0 | `SkillCooldownSeconds` |
| 일반 상대 체력·공격력 `EnemyStats` | `30 + 10(s−1)`, `5 + 2(s−1)` | `NormalHealth`, `NormalAttack`의 `Evaluate(n)` |
| 보스 체력·공격력 `EnemyStats` | `100 + 30(s−1)`, `10 + 4(s−1)` | `BossHealth`, `BossAttack`의 `Evaluate(n)`, 천마는 아래 |
| 적 공격 간격 `EnemyStats.AttackInterval` | 일반 2.0, 보스 2.0 | `NormalAttackInterval`, `BossAttackInterval` |
| 처치 보상 `StageRewards` | `10 + 2(s−1)`, `50 + 10(s−1)` | `NormalGold`, `BossGold`의 `Evaluate(n)` |
| 경지 `RealmRules` | 처치 30·50·70·100, 보너스 0·10·20·30·40 | `RealmKillsRequired`, `RealmBonusPercent` |

- **성장 곡선** `GrowthCurve.Evaluate(n)` = `내림((Base + Linear × n) × Growth^n)`. `double`로 계산하고 마지막에 내림해 `long`으로 돌려준다.
- **구간 순번** `n`: 몬스터와 보상은 `n = (stage − 1) + SubstageStep × (substage − 1)`. `stage`는 지금처럼 전체 진행 번호(`StageProgress.StageNumber`), `substage`는 구간 번호(1~6, 보스는 7)다. 기본값 `SubstageStep = 0`이면 구간과 상관없이 지금 값이 나온다. 예: `SubstageStep = 1/7`이면 1-1의 n은 0, 1-7(보스)은 6/7, 2-1은 1.
- **천마**: 화면상 스테이지 5(`(stage − 1) % 5 + 1 == 5`)의 보스는 `FinalBossHealth`가 0보다 크면 체력이 그 값이고, 0이면 보스 공식이다. 공격력도 `FinalBossAttack`으로 같다. 기본값은 둘 다 0이라 지금과 같다.
- 몬스터와 보상에 구간을 넘기기 위해, `EnemyStats`와 `StageRewards`에 구간을 받는 메서드를 더하고 `Battle.AddEnemy`에 구간 인자를 더한다 ("연결 지점"). 구간을 받지 않는 기존 메서드는 구간 1로 계산한다. `StageProgress`는 적을 만들 때와 보상을 줄 때 지금 구간(`SubstageNumber`, 보스는 7)을 넘긴다. 오프라인 보상은 지금처럼 `StageRewards.NormalGold(stage)`(구간 1)를 쓴다.
- 지금 있는 공개 상수(`Battle.DiscipleBaseAttack`, `SkillBasePercent` 등)는 지우지 않는다. 기본값의 설명으로 남기고, 계산에는 쓰지 않는다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| `GrowthCurve.Evaluate`의 결과가 `long.MaxValue` 이상이거나 무한대 | `long.MaxValue` (예외 없음) | 없음 |
| `GrowthCurve.Evaluate(n)`의 `n`이 음수·NaN·무한대 | `ArgumentOutOfRangeException` | 없음 |
| `new GrowthCurve`의 `baseValue`나 `linear`가 음수·NaN·무한대, `growth`가 1보다 작거나 NaN·무한대 | `ArgumentOutOfRangeException` | 없음 |
| `BalanceTable.Current = null` | `ArgumentNullException` | `Current` 그대로 |
| 검사에 걸리는 표를 `Current`에 넣음 (아래 "수치"의 허용 범위) | `ArgumentOutOfRangeException` | `Current` 그대로 |
| `Validate()`를 직접 부름 | 허용 범위 밖이면 `ArgumentOutOfRangeException`, 아니면 아무 일도 없다 | 없음 |
| `substage`가 1~7 밖 (`EnemyStats`, `StageRewards`, `Battle.AddEnemy`) | `ArgumentOutOfRangeException` | 적을 만들지 않음 |
| `Default`로 얻은 객체를 고침 | 그 객체만 바뀐다 | 다음 `Default`와 `Current`는 그대로 |
| 아주 큰 단계 (`int.MaxValue`)의 비용, 기본값 | 지금과 같은 10,737,418,255 | 없음 |

## 수치

| 항목 | 타입 | 기본값 (`Default`) | 허용 범위 |
| --- | --- | --- | --- |
| `AttackCost`, `HealthCost`, `InnerPowerCost` | `GrowthCurve` | 각각 (20, 5, 1.0) | 곡선 검사 |
| `AttackPerLevel` | `long` | 2 | 0 이상 |
| `HealthPerLevel` | `long` | 20 | 0 이상 |
| `DiscipleBaseAttack` | `long` | 10 | 1 이상 |
| `DiscipleBaseMaxHealth` | `long` | 100 | 1 이상 |
| `DiscipleAttackInterval` | `double` (초) | 1.0 | 0보다 큼, 유한 |
| `SkillBasePercent` | `int` | 200 | 0 이상 |
| `SkillPercentPerInnerLevel` | `int` | 10 | 0 이상 |
| `SkillCooldownSeconds` | `double` (초) | 5.0 | 0보다 큼, 유한 |
| `SubstageStep` | `double` | 0.0 | 0 이상, 유한 |
| `NormalHealth` | `GrowthCurve` | (30, 10, 1.0) | 곡선 검사 |
| `NormalAttack` | `GrowthCurve` | (5, 2, 1.0) | 곡선 검사 |
| `BossHealth` | `GrowthCurve` | (100, 30, 1.0) | 곡선 검사 |
| `BossAttack` | `GrowthCurve` | (10, 4, 1.0) | 곡선 검사 |
| `NormalAttackInterval`, `BossAttackInterval` | `double` (초) | 2.0, 2.0 | 0보다 큼, 유한 |
| `FinalBossHealth`, `FinalBossAttack` | `long` | 0, 0 (보스 공식) | 0 이상 |
| `NormalGold` | `GrowthCurve` | (10, 2, 1.0) | 곡선 검사 |
| `BossGold` | `GrowthCurve` | (50, 10, 1.0) | 곡선 검사 |
| `RealmKillsRequired` | `int[]` 4개 (삼류→이류, 이류→일류, 일류→절정, 절정→초절정) | {30, 50, 70, 100} | 길이 4, 모두 1 이상 |
| `RealmBonusPercent` | `int[]` 5개 (삼류~초절정) | {0, 10, 20, 30, 40} | 길이 5, 모두 0 이상 |

- 곡선 검사: `Base`, `Linear`는 0 이상 유한, `Growth`는 1 이상 유한. 곡선 속성이 `null`이어도 `Validate()`는 `ArgumentOutOfRangeException`을 던진다. 배열이 `null`이거나 길이가 다를 때도 같다.
- 기본값은 모두 지금 게임의 임시값이다. 새 값은 시뮬레이터로 정해 따로 넣는다.
- 예: 기본값의 `AttackCost.Evaluate(3)` = 35, `NormalHealth.Evaluate(1)` = 40 (2스테이지 일반 상대), `BossGold.Evaluate(4)` = 90 (1회차 천마).
- 예: `new GrowthCurve(10, 0, 1.15).Evaluate(3)` = 내림(15.20875) = 15. `new GrowthCurve(1, 0, 2).Evaluate(2000)` = `long.MaxValue`.

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.Balance
{
    public sealed class GrowthCurve
    {
        // baseValue, linear: 0 이상 유한. growth: 1 이상 유한. 아니면 ArgumentOutOfRangeException.
        public GrowthCurve(double baseValue, double linear, double growth);
        public double Base { get; }
        public double Linear { get; }
        public double Growth { get; }

        // 내림((Base + Linear * n) * Growth^n). n 이 음수·NaN·무한대면 ArgumentOutOfRangeException.
        // 결과가 long.MaxValue 이상이거나 무한대면 long.MaxValue.
        public long Evaluate(double n);
    }

    public sealed class BalanceTable
    {
        // 지금 게임의 수치로 채운 새 객체. 부를 때마다 새로 만든다.
        public static BalanceTable Default { get; }

        // 게임 전체가 읽는 표. 처음 값은 Default.
        // null 이면 ArgumentNullException, Validate() 에 걸리면 ArgumentOutOfRangeException (둘 다 Current 그대로).
        public static BalanceTable Current { get; set; }

        // 허용 범위("수치" 표)를 벗어난 값이 있으면 ArgumentOutOfRangeException.
        public void Validate();

        public GrowthCurve AttackCost { get; set; }
        public GrowthCurve HealthCost { get; set; }
        public GrowthCurve InnerPowerCost { get; set; }
        public long AttackPerLevel { get; set; }
        public long HealthPerLevel { get; set; }
        public long DiscipleBaseAttack { get; set; }
        public long DiscipleBaseMaxHealth { get; set; }
        public double DiscipleAttackInterval { get; set; }
        public int SkillBasePercent { get; set; }
        public int SkillPercentPerInnerLevel { get; set; }
        public double SkillCooldownSeconds { get; set; }
        public double SubstageStep { get; set; }
        public GrowthCurve NormalHealth { get; set; }
        public GrowthCurve NormalAttack { get; set; }
        public GrowthCurve BossHealth { get; set; }
        public GrowthCurve BossAttack { get; set; }
        public double NormalAttackInterval { get; set; }
        public double BossAttackInterval { get; set; }
        public long FinalBossHealth { get; set; }
        public long FinalBossAttack { get; set; }
        public GrowthCurve NormalGold { get; set; }
        public GrowthCurve BossGold { get; set; }
        public int[] RealmKillsRequired { get; set; }
        public int[] RealmBonusPercent { get; set; }

        // 구간 순번 n = (stage - 1) + SubstageStep * (substage - 1).
        // stage < 1 이거나 substage 가 1~7 밖이면 ArgumentOutOfRangeException.
        public double SubstageIndex(int stage, int substage);
    }
}
```

다른 기능에 더하는 것 (기존 메서드는 모양 그대로 두고 구간 1로 계산한다):

```csharp
namespace PlumSword.Core.Combat
{
    public static class EnemyStats
    {
        // 구간을 받는 새 메서드. substage 1~7 밖이면 ArgumentOutOfRangeException. stage·kind 검사는 기존과 같다.
        // 화면상 스테이지 5의 보스는 FinalBossHealth / FinalBossAttack 이 0 보다 크면 그 값.
        public static long MaxHealth(EnemyKind kind, int stage, int substage);
        public static long Attack(EnemyKind kind, int stage, int substage);
        // 기존 MaxHealth(kind, stage), Attack(kind, stage) 는 substage 1 로 계산한다.
        // AttackInterval(kind) 는 NormalAttackInterval / BossAttackInterval 을 돌려준다.
    }

    public sealed class Battle
    {
        // substage 인자를 끝에 더한다 (기본 1). 1~7 밖이면 ArgumentOutOfRangeException (적을 만들지 않는다).
        public Enemy AddEnemy(EnemyKind kind, int stage, double position, int substage = 1);
    }
}

namespace PlumSword.Core.Stage
{
    public static class StageRewards
    {
        // 구간을 받는 새 메서드. substage 1~7 밖이면 ArgumentOutOfRangeException.
        public static long NormalGold(int stage, int substage);
        public static long BossGold(int stage, int substage);
        // 기존 NormalGold(stage), BossGold(stage) 는 substage 1 로 계산한다.
    }
}
```

- 누가 만들고 누가 들고 있는지: `BalanceTable.Current`는 정적 속성이라 만들 필요가 없다. `GameRoot`도 바꾸지 않는다 (게임은 늘 `Default`). 시뮬레이터가 시작할 때 JSON으로 만든 표를 넣는다.
- `StageProgress`는 일반 상대를 만들 때 `AddEnemy(Normal, StageNumber, 위치, SubstageNumber)`, 보스는 `AddEnemy(Boss, StageNumber, 위치, 7)`, 일반 보상은 `NormalGold(StageNumber, SubstageNumber)`, 보스 보상은 `BossGold(끝난 번호, 7)`을 쓴다.
- `GameRoot`는 `StatUpgrades`를 만들 때 시작 능력치로 `BalanceTable.Current.DiscipleBaseAttack`, `DiscipleBaseMaxHealth`를 넣는다.

### 필요한 것

- 강화(`docs/spec/stat_upgrade.md`), 전투(`docs/spec/combat.md`), 스테이지(`docs/spec/stage.md`), 경지(`docs/spec/realm.md`)의 기존 공개 타입. 이 사양은 그 안의 계산이 표를 읽게만 바꾸고 연결 지점의 모양은 위의 추가 말고는 바꾸지 않는다.

## 화면

해당 없음. 표시가 없는 수치 기능이다.

## 저장

해당 없음. 수치 표는 저장하지 않는다. 게임은 늘 `Default`로 시작한다.

- 오프라인 보상: 금액 식(`NormalGold(stage)`)이 표를 읽게 될 뿐 규칙은 그대로다.

## 범위 밖

- 시뮬레이터(`tools/balance/`): 루프 작업이 아니다. 대화에서 만든다. Core 소스를 그대로 컴파일해 `BalanceTable.Current`를 JSON 값으로 바꾸고 20분 플레이를 돌려 CSV를 남긴다.
- 새 수치 정하기와 반영: 시뮬레이터로 정한 뒤 별도 작업으로 `Default`를 바꾼다. 그때 바뀌는 기존 테스트의 기대값은 그 작업이 다룬다.
- 회귀·엔딩·자동 강화: `docs/spec/regression.md`, `docs/spec/auto_upgrade.md`.
- 전투 좌표·이동 속도·사거리·스킬 타수 같은 동작 수치, 페이드·소개 시간: 표에 넣지 않는다.
- 게임 안에서 수치를 바꾸는 화면이나 파일 읽기: 하지 않는다.

## 완료 조건 초안

수치 표와 곡선 (`Assets/Tests/EditMode/Balance/`). 표를 바꾸는 테스트는 `TearDown`에서 `BalanceTable.Current = BalanceTable.Default`로 되돌린다.

1. `new GrowthCurve(20, 5, 1.0).Evaluate(3)`은 35다.
2. `new GrowthCurve(10, 0, 1.15).Evaluate(3)`은 15다.
3. `new GrowthCurve(1, 0, 2).Evaluate(2000)`은 `long.MaxValue`다.
4. `Evaluate(-1)`, `Evaluate(double.NaN)`은 각각 `ArgumentOutOfRangeException`을 던진다.
5. `new GrowthCurve(-1, 0, 1)`, `new GrowthCurve(0, -1, 1)`, `new GrowthCurve(0, 0, 0.9)`는 각각 `ArgumentOutOfRangeException`을 던진다.
6. `BalanceTable.Default`의 값은 "수치" 표의 기본값과 모두 같다 (곡선은 `Base`, `Linear`, `Growth` 셋, 배열은 원소까지).
7. `BalanceTable.Default`를 두 번 부르면 서로 다른 객체이고, 하나의 `AttackPerLevel`을 99로 바꿔도 다른 하나와 `BalanceTable.Current`의 `AttackPerLevel`은 2다.
8. `BalanceTable.Current = null`은 `ArgumentNullException`을 던지고 `Current`는 그대로다.
9. `RealmBonusPercent`의 길이가 4인 표, `DiscipleAttackInterval`이 0인 표, `NormalHealth`가 `null`인 표를 `Current`에 넣으면 각각 `ArgumentOutOfRangeException`을 던지고 `Current`는 그대로다.
10. `SubstageStep`이 1/7인 표의 `SubstageIndex(1, 1)`은 0, `SubstageIndex(1, 7)`은 6/7(오차 1e-9), `SubstageIndex(2, 1)`은 1이다.
11. `SubstageIndex(1, 0)`과 `SubstageIndex(1, 8)`은 각각 `ArgumentOutOfRangeException`을 던진다.

기능이 표를 읽는지 (`Assets/Tests/EditMode/Balance/BalanceTableUsageTests.cs`). 각 조건은 `Current`를 바꾼 표로 확인한다.

12. `AttackCost`를 (100, 0, 2)로 바꾸면 `new StatUpgrades(wallet, 10, 100, 3)`의 `GetCost(Attack)`은 800이고 `GetCost(Health)`는 20이다.
13. `AttackPerLevel`을 5로, `HealthPerLevel`을 50으로 바꾸면 `new StatUpgrades(wallet, 10, 100, 2, 2)`의 `Attack`은 20, `MaxHealth`는 200이다.
14. `SkillBasePercent`를 300으로, `SkillPercentPerInnerLevel`을 20으로 바꾸면 `Battle.CalculateSkillDamage(10, 2)`는 34다.
15. `SubstageStep`을 1로, `NormalHealth`를 (30, 0, 2)로 바꾸면 `EnemyStats.MaxHealth(EnemyKind.Normal, 1, 3)`은 120이고, `EnemyStats.MaxHealth(EnemyKind.Normal, 1)`은 30이다.
16. 15번과 같은 표에서 `battle.AddEnemy(EnemyKind.Normal, 1, 1.0, 3)`으로 만든 적의 `MaxHealth`는 120이다.
17. `FinalBossHealth`를 5000으로, `FinalBossAttack`을 300으로 바꾸면 `EnemyStats.MaxHealth(EnemyKind.Boss, 5)`와 `MaxHealth(EnemyKind.Boss, 10)`은 5000, `Attack(EnemyKind.Boss, 5)`는 300이고, `MaxHealth(EnemyKind.Boss, 4)`는 190이다.
18. `SubstageStep`을 1로, `NormalGold`를 (10, 0, 2)로 바꾸면 `StageRewards.NormalGold(1, 3)`은 40이고 `NormalGold(1)`은 10이다.
19. `RealmKillsRequired`를 {1, 2, 3, 4}로, `RealmBonusPercent`를 {0, 50, 60, 70, 80}으로 바꾸면 `RealmRules.KillsRequired(RealmLevel.ThirdRate)`는 1, `RealmRules.BonusPercent(RealmLevel.SecondRate)`는 50이다.
20. `NormalAttackInterval`을 3.0으로 바꾸면 `EnemyStats.AttackInterval(EnemyKind.Normal)`은 3.0이다.
21. `EnemyStats.MaxHealth(EnemyKind.Normal, 1, 0)`, `StageRewards.NormalGold(1, 8)`, `battle.AddEnemy(EnemyKind.Normal, 1, 1.0, 0)`은 각각 `ArgumentOutOfRangeException`을 던지고, 마지막 경우 `battle.Enemies.Count`는 0 그대로다.
22. 기본 표에서 `StageProgress`가 2스테이지 3구간에 만든 일반 상대의 `MaxHealth`는 40이다 (`SubstageStep` 0이라 구간과 무관).
23. `SubstageStep`을 1로, `NormalHealth`를 (30, 0, 2)로 바꾼 표에서 `StageProgress`(시작 번호 1)가 1구간을 이긴 뒤 2구간에 만든 일반 상대의 `MaxHealth`는 60이다.
24. 기존 테스트가 모두 통과한다 (기본 표는 지금 수치와 같다).

## 결정 기록

결정 기록은 [records/balance.md](records/balance.md)의 "결정 기록"에 있다. 구현에 필요한 규칙은 위 절들에 모두 있다.
