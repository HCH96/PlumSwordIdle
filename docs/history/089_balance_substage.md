# 089_balance_substage — 몬스터 능력치·처치 보상의 구간 인자와 천마 고정값

## 목표
Core 의 `EnemyStats`(체력·공격력)와 `StageRewards`가 수치 표의 성장 곡선과 구간 순번을 읽게 바꾸고, 구간을 받는 메서드와 `Battle.AddEnemy`의 구간 인자를 더한다. `StageProgress`가 적을 만들고 보상을 줄 때 지금 구간을 넘긴다. 천마(화면상 스테이지 5의 보스) 고정값도 넣는다.

## 묶음
balance

## 선행 작업
- 088_balance_usage

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/balance.md ("동작"의 구간 순번·천마, "경계 상황", "연결 지점"의 다른 기능에 더하는 것)
- docs/spec/combat.md ("연결 지점" 절)
- docs/spec/stage.md ("동작", "연결 지점" 절)

## 요구사항
- 구간 순번은 087 이 만든 `BalanceTable.Current.SubstageIndex(stage, substage)` = `(stage − 1) + SubstageStep × (substage − 1)`을 쓴다. `stage`는 전체 진행 번호(`StageProgress.StageNumber`), `substage`는 구간 번호 1~6, 보스는 7.
- `Assets/Scripts/Core/Combat/EnemyStats.cs`에 더한다 (기존 메서드는 모양 그대로 두고 `substage` 1로 계산한다):
  - `public static long MaxHealth(EnemyKind kind, int stage, int substage)`: 일반 `Current.NormalHealth.Evaluate(n)`, 보스 `Current.BossHealth.Evaluate(n)`.
  - `public static long Attack(EnemyKind kind, int stage, int substage)`: 일반 `Current.NormalAttack.Evaluate(n)`, 보스 `Current.BossAttack.Evaluate(n)`.
  - **천마**: `kind`가 보스이고 화면상 스테이지가 5(`(stage − 1) % 5 + 1 == 5`)이면, `Current.FinalBossHealth`가 0보다 크면 체력이 그 값, 0이면 보스 공식이다. 공격력도 `Current.FinalBossAttack`으로 같다.
  - `substage`가 1~7 밖이면 `ArgumentOutOfRangeException`. `stage < 1`과 정의되지 않은 `kind`의 예외는 지금과 같다. 검사 순서는 자유다.
  - 기존 `MaxHealth(kind, stage)`, `Attack(kind, stage)`는 새 메서드에 `substage` 1을 넘긴다.
- `Assets/Scripts/Core/Stage/StageRewards.cs`에 더한다 (기존 `NormalGold(stage)`, `BossGold(stage)`는 `substage` 1로 계산한다):
  - `public static long NormalGold(int stage, int substage)` = `Current.NormalGold.Evaluate(n)`.
  - `public static long BossGold(int stage, int substage)` = `Current.BossGold.Evaluate(n)`.
  - `substage`가 1~7 밖이면 `ArgumentOutOfRangeException`. `stage < 1`의 예외는 지금과 같다.
- `Assets/Scripts/Core/Combat/Battle.cs`: `public Enemy AddEnemy(EnemyKind kind, int stage, double position, int substage = 1)`. 능력치는 `EnemyStats.MaxHealth(kind, stage, substage)`, `EnemyStats.Attack(kind, stage, substage)`로 계산한다. `substage`가 1~7 밖이면 `ArgumentOutOfRangeException`이고 적을 만들지 않는다 (`Enemies`, `EnemyAdded` 모두 그대로). 종남 보스 판정(`isJongnamBoss`)은 지금과 같다.
- `Assets/Scripts/Core/Stage/StageProgress.cs`:
  - 일반 상대: `AddEnemy(EnemyKind.Normal, StageNumber, 위치, SubstageNumber)`.
  - 보스: `AddEnemy(EnemyKind.Boss, StageNumber, 위치, 7)`.
  - 일반 처치 보상: `StageRewards.NormalGold(StageNumber, SubstageNumber)`.
  - 보스 처치 보상: `StageRewards.BossGold(끝난 번호, 7)`.
- 오프라인 보상(`OfflineRewardCalculator`)은 지금처럼 `StageRewards.NormalGold(stage)`(구간 1)를 쓴다. 고치지 않는다.
- 기본 표(`SubstageStep` 0, 곡선 배율 1.0, 천마 고정값 0)는 지금 수치와 같으므로 기대값이 바뀌는 기존 테스트는 없다. 기존 테스트는 고치지 않는다.
- XML 주석의 식을 "수치 표의 …"로 고친다.
- 구현하다 모양이 사양과 달라지면 `docs/spec/balance.md`의 "연결 지점" 절을 고치고 보고서의 가정에 적는다. 끝나면 `docs/spec/combat.md`·`docs/spec/stage.md`의 "연결 지점" 절에 새 메서드(`EnemyStats`·`StageRewards`의 구간 메서드, `AddEnemy`의 `substage` 인자)를 한 줄씩 더한다.
- 테스트는 `BalanceTable.Current`를 바꾼 표로 확인하고, `[TearDown]`에서 `BalanceTable.Current = BalanceTable.Default`로 되돌린다.
- 22·23번의 `StageProgress` 테스트는 `new Wallet(0)`, 강한 강화 `new StatUpgrades(wallet, 100000, 100000)`, `new Battle(upgrades)`, 기존 `Assets/Tests/EditMode/Stage/MinRandom.cs`로 만들고, `Begin()` 뒤 `Tick`과 `battle.Step()`으로 원하는 구간까지 진행한다 (방법은 자유).

## 완료 조건
- `Assets/Tests/EditMode/Balance/BalanceTableUsageTests.cs`(088 이 만든 파일)에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 15. `SubstageStep`을 1로, `NormalHealth`를 (30, 0, 2)로 바꾸면 `EnemyStats.MaxHealth(EnemyKind.Normal, 1, 3)`은 120이고, `EnemyStats.MaxHealth(EnemyKind.Normal, 1)`은 30이다.
- 16. 15번과 같은 표에서 `battle.AddEnemy(EnemyKind.Normal, 1, 1.0, 3)`으로 만든 적의 `MaxHealth`는 120이다.
- 17. `FinalBossHealth`를 5000으로, `FinalBossAttack`을 300으로 바꾸면 `EnemyStats.MaxHealth(EnemyKind.Boss, 5)`와 `MaxHealth(EnemyKind.Boss, 10)`은 5000, `Attack(EnemyKind.Boss, 5)`는 300이고, `MaxHealth(EnemyKind.Boss, 4)`는 190이다.
- 18. `SubstageStep`을 1로, `NormalGold`를 (10, 0, 2)로 바꾸면 `StageRewards.NormalGold(1, 3)`은 40이고 `NormalGold(1)`은 10이다.
- 21. `EnemyStats.MaxHealth(EnemyKind.Normal, 1, 0)`, `StageRewards.NormalGold(1, 8)`, `battle.AddEnemy(EnemyKind.Normal, 1, 1.0, 0)`은 각각 `ArgumentOutOfRangeException`을 던지고, 마지막 경우 `battle.Enemies.Count`는 0 그대로다.
- 22. 기본 표에서 `StageProgress`가 2스테이지 3구간에 만든 일반 상대의 `MaxHealth`는 40이다 (`SubstageStep` 0이라 구간과 무관).
- 23. `SubstageStep`을 1로, `NormalHealth`를 (30, 0, 2)로 바꾼 표에서 `StageProgress`(시작 번호 1)가 1구간을 이긴 뒤 2구간에 만든 일반 상대의 `MaxHealth`는 60이다.
- 24. 기존 테스트가 모두 통과한다 (기본 표는 지금 수치와 같다).

## 하지 말 것
- 088 이 바꾼 강화·스킬·공격 간격·경지 계산을 다시 고치지 않는다.
- `OfflineRewardCalculator`를 고치지 않는다.
- `GameRoot`와 `Assets/Scripts/Game/`은 고치지 않는다 (090).
- 기존 테스트를 고치지 않는다.
- 회귀·엔딩 규칙(`docs/spec/regression.md`)을 만들지 않는다.
- `UnityEngine`을 쓰지 않는다. 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 수치 표의 구간별 성장 곡선과 천마 고정값을 적 능력치에 적용하고, 적 생성과 처치 보상에 현재 구간을 전달했다.
- 기존 호출의 1구간 호환, 범위 검사, 구간 진행과 보상에 대한 에딧 모드 테스트를 추가하고 전투·스테이지 연결 지점을 갱신했다.

### 바뀐 파일
- `Assets/Scripts/Core/Combat/EnemyStats.cs`, `Battle.cs`
- `Assets/Scripts/Core/Stage/StageRewards.cs`, `StageProgress.cs`
- `Assets/Tests/EditMode/Balance/BalanceTableUsageTests.cs`
- `docs/spec/combat.md`, `docs/spec/stage.md`
- `tasks/doing/089_balance_substage.md`, `.loop/result.json`

### 가정
- 없음. 공개 연결 지점은 밸런스 사양과 같다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- `git diff --check` 통과. Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 실행과 판정을 기다린다.


## 루프 기록 (2026-11-22 16:25)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/089_balance_substage
- 루프 브랜치: loop/20261122-160726
