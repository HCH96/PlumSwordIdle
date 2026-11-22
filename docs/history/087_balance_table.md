# 087_balance_table — 수치 표 BalanceTable 과 성장 곡선 GrowthCurve

## 목표
Core(`PlumSword.Core.Balance`)에 성장 곡선 `GrowthCurve`와 수치 표 `BalanceTable`(기본값 `Default`, 게임 전체가 읽는 `Current`, 검사 `Validate`, 구간 순번 `SubstageIndex`)을 만든다. 이 작업에서는 다른 기능이 아직 표를 읽지 않는다 (088·089·090 이 한다).

## 묶음
balance

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/balance.md ("동작", "경계 상황", "수치", "연결 지점"의 `PlumSword.Core.Balance`)

## 요구사항
- 새 폴더 `Assets/Scripts/Core/Balance/`에 `GrowthCurve.cs`, `BalanceTable.cs`를 만든다. 네임스페이스 `PlumSword.Core.Balance`. 파일 하나에 공개 타입 하나.
- 모양은 사양 "연결 지점"의 `GrowthCurve`, `BalanceTable`을 그대로 따른다.
  - `public GrowthCurve(double baseValue, double linear, double growth)`, `Base`, `Linear`, `Growth`, `public long Evaluate(double n)`.
  - `public static BalanceTable Default { get; }`, `public static BalanceTable Current { get; set; }`, `public void Validate()`, `public double SubstageIndex(int stage, int substage)`, 그리고 사양 "수치" 표의 속성 24개(이름·타입 그대로, 모두 `get; set;`).
- `GrowthCurve.Evaluate(n)` = 내림((Base + Linear × n) × Growth^n). `double`로 계산하고 마지막에 내림해 `long`으로 돌려준다. 결과가 `long.MaxValue` 이상이거나 무한대면 `long.MaxValue`. `n`이 음수·NaN·무한대면 `ArgumentOutOfRangeException`.
- `GrowthCurve` 생성자: `baseValue`, `linear`가 음수·NaN·무한대이거나 `growth`가 1보다 작거나 NaN·무한대면 `ArgumentOutOfRangeException`.
- `BalanceTable.Default`는 부를 때마다 아래 기본값으로 채운 **새 객체**를 돌려준다 (배열도 새로 만든다).
  - `AttackCost`, `HealthCost`, `InnerPowerCost` = 각각 `new GrowthCurve(20, 5, 1.0)`
  - `AttackPerLevel` 2, `HealthPerLevel` 20, `DiscipleBaseAttack` 10, `DiscipleBaseMaxHealth` 100, `DiscipleAttackInterval` 1.0
  - `SkillBasePercent` 200, `SkillPercentPerInnerLevel` 10, `SkillCooldownSeconds` 5.0, `SubstageStep` 0.0
  - `NormalHealth` (30, 10, 1.0), `NormalAttack` (5, 2, 1.0), `BossHealth` (100, 30, 1.0), `BossAttack` (10, 4, 1.0)
  - `NormalAttackInterval` 2.0, `BossAttackInterval` 2.0, `FinalBossHealth` 0, `FinalBossAttack` 0
  - `NormalGold` (10, 2, 1.0), `BossGold` (50, 10, 1.0)
  - `RealmKillsRequired` {30, 50, 70, 100}, `RealmBonusPercent` {0, 10, 20, 30, 40}
- `BalanceTable.Current`의 처음 값은 `Default`로 만든 객체다. 쓸 때 `null`이면 `ArgumentNullException`, `Validate()`에 걸리면 `ArgumentOutOfRangeException`이고 두 경우 모두 `Current`는 그대로다. 통과하면 넣은 객체를 그대로 쓴다 (복사하지 않는다).
- `Validate()` 허용 범위 (벗어나면 `ArgumentOutOfRangeException`, 아니면 아무 일도 없다):
  - 곡선 속성 7개: `null`이면 안 된다 (곡선 값 자체는 생성자가 이미 검사했다).
  - `AttackPerLevel`, `HealthPerLevel` 0 이상. `DiscipleBaseAttack`, `DiscipleBaseMaxHealth` 1 이상.
  - `DiscipleAttackInterval`, `SkillCooldownSeconds`, `NormalAttackInterval`, `BossAttackInterval`: 0보다 크고 유한.
  - `SkillBasePercent`, `SkillPercentPerInnerLevel` 0 이상. `SubstageStep` 0 이상 유한. `FinalBossHealth`, `FinalBossAttack` 0 이상.
  - `RealmKillsRequired`: `null`이 아니고 길이 4, 모두 1 이상. `RealmBonusPercent`: `null`이 아니고 길이 5, 모두 0 이상.
- `SubstageIndex(stage, substage)` = `(stage − 1) + SubstageStep × (substage − 1)`. `stage < 1`이거나 `substage`가 1~7 밖이면 `ArgumentOutOfRangeException`.
- 테스트에서 `Current`를 바꾸면 `[TearDown]`에서 `BalanceTable.Current = BalanceTable.Default`로 되돌린다.
- 구현하다 모양이 사양과 달라지면 `docs/spec/balance.md`의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/Balance/GrowthCurveTests.cs`와 `Assets/Tests/EditMode/Balance/BalanceTableTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 1. `new GrowthCurve(20, 5, 1.0).Evaluate(3)`은 35다.
- 2. `new GrowthCurve(10, 0, 1.15).Evaluate(3)`은 15다.
- 3. `new GrowthCurve(1, 0, 2).Evaluate(2000)`은 `long.MaxValue`다.
- 4. `Evaluate(-1)`, `Evaluate(double.NaN)`은 각각 `ArgumentOutOfRangeException`을 던진다.
- 5. `new GrowthCurve(-1, 0, 1)`, `new GrowthCurve(0, -1, 1)`, `new GrowthCurve(0, 0, 0.9)`는 각각 `ArgumentOutOfRangeException`을 던진다.
- 6. `BalanceTable.Default`의 값은 요구사항의 기본값과 모두 같다 (곡선은 `Base`, `Linear`, `Growth` 셋, 배열은 원소까지).
- 7. `BalanceTable.Default`를 두 번 부르면 서로 다른 객체이고, 하나의 `AttackPerLevel`을 99로 바꿔도 다른 하나와 `BalanceTable.Current`의 `AttackPerLevel`은 2다.
- 8. `BalanceTable.Current = null`은 `ArgumentNullException`을 던지고 `Current`는 그대로다.
- 9. `RealmBonusPercent`의 길이가 4인 표, `DiscipleAttackInterval`이 0인 표, `NormalHealth`가 `null`인 표를 `Current`에 넣으면 각각 `ArgumentOutOfRangeException`을 던지고 `Current`는 그대로다.
- 10. `SubstageStep`이 1/7인 표의 `SubstageIndex(1, 1)`은 0, `SubstageIndex(1, 7)`은 6/7(오차 1e-9), `SubstageIndex(2, 1)`은 1이다.
- 11. `SubstageIndex(1, 0)`과 `SubstageIndex(1, 8)`은 각각 `ArgumentOutOfRangeException`을 던진다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `StatUpgrades`, `Battle`, `EnemyStats`, `StageRewards`, `StageProgress`, `RealmRules`가 표를 읽게 바꾸지 않는다 (088·089 가 한다). `GameRoot`도 고치지 않는다 (090).
- `UnityEngine`을 쓰지 않는다.
- 시뮬레이터(`tools/balance/`)나 JSON 읽기를 만들지 않는다.
- 기본값을 지금 수치와 다르게 정하지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 순수 C# 성장 곡선과 기본값, 현재 표, 유효성 검사, 구간 순번을 구현했다.
- 기본값과 독립성, 유효성 검사, 예외 및 곡선 계산을 확인하는 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/Balance/`의 곡선과 수치 표 및 메타파일.
- `Assets/Tests/EditMode/Balance/`의 테스트와 메타파일.
- `tasks/doing/087_balance_table.md`, `.loop/result.json`.

### 가정
- 밑과 선형 계수가 모두 0인 곡선은 지수 항이 무한대로 넘쳐도 결과를 0으로 취급했다.
- 공개 연결 지점은 사양과 같으며 사양 변경은 없다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았다. 신규 및 기존 테스트의 실행과 판정은 루프 스크립트를 기다린다.


## 루프 기록 (2026-11-22 16:12)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/087_balance_table
- 루프 브랜치: loop/20261122-160726
