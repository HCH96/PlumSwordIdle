# 008_battle_movement — 전투 뼈대: 시간, 이동, 적 추가, 초기화, 멈춤

## 목표
`PlumSword.Core` 에 적 하나를 나타내는 `Enemy` 와 전투 로직 `Battle` 의 뼈대를 만든다. 이 작업은 공개 모양 전체, 시간 진행(`Tick`, `Step`), 대기 감소, 제자와 몬스터의 이동, 적 추가(`AddEnemy`), 초기화(`Reset`), 멈춤(`SetPaused`)까지 만들고 에딧 모드 테스트로 검사한다. 공격·피해·쓰러짐은 만들지 않는다 (009_battle_attack 이 한다).

## 묶음
전투

## 선행 작업
- 001_wallet
- 004_stat_upgrades
- 007_enemy_stats

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/combat.md ("동작", "경계 상황", "수치", "연결 지점" 절)
- docs/spec/stat_upgrade.md ("연결 지점" 절의 `StatUpgrades`)

## 요구사항
- 만들 파일 (파일 하나에 공개 타입 하나, 네임스페이스 `PlumSword.Core.Combat`):
  - `Assets/Scripts/Core/Combat/Enemy.cs`
  - `Assets/Scripts/Core/Combat/Battle.cs`
- 테스트 파일: `Assets/Tests/EditMode/Combat/BattleTests.cs`. 네임스페이스 `PlumSword.Core.Tests.Combat`, 클래스 `BattleTests`. 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다.
- 선행 작업이 만든 것을 쓴다. 내부는 고치지 않는다.
  - 001_wallet: `PlumSword.Core.Currency.Wallet` — `new Wallet(long initialBalance = 0)`. 테스트에서 `StatUpgrades` 를 만들 때만 쓴다.
  - 004_stat_upgrades: `PlumSword.Core.StatUpgrade.StatUpgrades` — `new StatUpgrades(Wallet wallet, long baseAttack, long baseMaxHealth, double baseAttackInterval)`, `long MaxHealth { get; }` (시작 100 기준 100). 이 작업에서 `Battle` 이 읽는 것은 `MaxHealth` 뿐이다.
  - 007_enemy_stats: `PlumSword.Core.Combat.EnemyKind` (`Normal = 0`, `Boss = 1`) 과 `PlumSword.Core.Combat.EnemyStats` — `long MaxHealth(EnemyKind kind, int stage)`, `long Attack(EnemyKind kind, int stage)`, `double AttackInterval(EnemyKind kind)`, `double AttackRange(EnemyKind kind)`, `const double NormalReactRange = 3.0`, `const double NormalMoveSpeed = 1.0`. `stage < 1` 이나 정의되지 않은 `kind` 에는 `ArgumentOutOfRangeException` 을 던진다.
- 공개 모양은 사양의 "연결 지점" 절 그대로다. **공개 멤버는 이 작업에서 전부 선언한다** (뒤 작업은 동작만 채운다).

  ```csharp
  namespace PlumSword.Core.Combat
  {
      // 적 하나. Battle.AddEnemy 만 만든다 (생성자는 internal). 값은 Battle 만 바꾼다 (설정은 internal).
      public sealed class Enemy
      {
          public EnemyKind Kind { get; }
          public int Index { get; }
          public double Position { get; }
          public long MaxHealth { get; }
          public long Health { get; }
          public long Attack { get; }
          public double AttackInterval { get; }
          public double AttackCooldown { get; }
          public bool IsDead { get; }
      }

      public sealed class Battle
      {
          public const double StepSeconds = 0.02;
          public const double MaxTickSeconds = 0.25;
          public const double Epsilon = 1e-9;
          public const long DiscipleBaseAttack = 10;
          public const long DiscipleBaseMaxHealth = 100;
          public const double DiscipleBaseAttackInterval = 1.0;
          public const double DiscipleMoveSpeed = 2.0;
          public const double DiscipleAttackRange = 1.0;

          public Battle(PlumSword.Core.StatUpgrade.StatUpgrades upgrades);

          public double DisciplePosition { get; }
          public long DiscipleHealth { get; }
          public long DiscipleMaxHealth { get; }
          public double DiscipleAttackCooldown { get; }
          public bool IsDiscipleDefeated { get; }
          public bool Paused { get; }
          public System.Collections.Generic.IReadOnlyList<Enemy> Enemies { get; }

          public void Tick(double deltaSeconds);
          public void Step();
          public Enemy AddEnemy(EnemyKind kind, int stage, double position);
          public void Reset();
          public void SetPaused(bool paused);

          public event System.Action<Enemy> EnemyAdded;
          public event System.Action<Enemy> EnemyDamaged;
          public event System.Action DiscipleDamaged;
          public event System.Action<Enemy> EnemyDefeated;
          public event System.Action DiscipleDefeated;
          public event System.Action ResetCompleted;
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/combat.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 사양에 없는 공개 멤버를 더하지 않는다.
- **이 작업에서 동작을 채우지 않는 것** (선언만 한다. 009_battle_attack, 010_battle_upgrade_reentry 가 채운다): `EnemyDamaged`, `DiscipleDamaged`, `EnemyDefeated`, `DiscipleDefeated` 는 보내지 않는다. `IsDiscipleDefeated` 는 항상 `false`, `Enemy.IsDead` 는 항상 `false` 다. 체력은 줄지 않는다. `upgrades.Upgraded` 는 구독하지 않는다. 쓰이지 않는 이벤트 때문에 컴파일 경고(CS0067)가 나는 것은 괜찮다. 다만 뒤 작업이 `Step` 에 "공격"과 "쓰러짐 판정" 단계를 넣기 쉽게 `Step` 을 단계별 private 메서드로 나눠 둔다.
- **좌표**: 전투는 한 줄(진행 방향 좌표 하나) 위에서 일어난다. 제자는 좌표가 커지는 쪽으로 전진한다. 적은 항상 제자와 같거나 더 큰 좌표에 있다. 좌표·거리·시간은 `double`(유닛, 초), 체력·공격력은 `long` 이다. 반올림하지 않는다.
- **생성자** `Battle(upgrades)`: `upgrades` 가 `null` 이면 `System.ArgumentNullException`. 시작 상태는 제자 좌표 0, 제자 체력 = `upgrades.MaxHealth`, 공격 대기 0, 적 없음, 멈추지 않음, 패배 아님. 알림을 보내지 않는다.
- `DiscipleMaxHealth` 는 부를 때마다 `upgrades.MaxHealth` 를 그대로 돌려준다.
- `Enemies` 는 살아 있는 적을 추가된 순서로 담는다.
- **적 추가** `AddEnemy(kind, stage, position)`:
  - `stage < 1`, 정의되지 않은 `kind`(예: `(EnemyKind)99`), `position` 이 `NaN`·무한대(양·음)이거나 `DisciplePosition` 보다 작으면 `System.ArgumentOutOfRangeException`. 이때 아무것도 바꾸지 않고 알림도 없다 (다음 `Index` 도 늘지 않는다).
  - `position` 이 `DisciplePosition` 과 같으면 정상 추가다.
  - 적을 만들어 `Enemies` 끝에 넣고, `EnemyAdded` 를 그 적을 담아 한 번 보낸 뒤, 그 적을 돌려준다.
  - 새 적: `MaxHealth = Health = EnemyStats.MaxHealth(kind, stage)`, `Attack = EnemyStats.Attack(kind, stage)`, `AttackInterval = EnemyStats.AttackInterval(kind)`, `AttackCooldown = 0`, `IsDead = false`, `Position = position`.
  - `Index` 는 `Reset` 뒤(또는 새로 만든 뒤) 0부터, `AddEnemy` 가 성공할 때마다 1씩 는다.
  - 멈춘 동안에도 정상 추가된다 (`EnemyAdded` 1번).
- **한 칸** `Step()`: 정확히 0.02초(`StepSeconds`, 임시값)를 진행한다. 멈춤 상태(`Paused`)이거나 `IsDiscipleDefeated` 면 아무것도 하지 않는다. 순서는 사양대로 "대기 감소 → 이동 → 공격 → 쓰러짐 판정"이고, 이 작업은 앞의 둘만 만든다.
  1. **대기 감소**: 제자와 모든 적의 공격 대기 시간을 0.02 줄인다. 0보다 작아지지 않는다.
  2. **이동**:
     - 제자: 가장 가까운 적까지의 거리 `d`(적 좌표 − 제자 좌표의 최솟값)를 본다. 적이 없으면 `DiscipleMoveSpeed × StepSeconds`(= 0.04)만큼 전진한다. `d ≤ DiscipleAttackRange + Epsilon`(= 1.0 + 1e-9)이면 멈춘다. 아니면 `min(0.04, d − 1.0)` 만큼 전진한다 (사거리를 넘지 않게 딱 맞춰 멈춘다).
     - 일반 몬스터(`Normal`, 추가된 순서대로, 제자가 움직인 뒤의 좌표로 계산): 제자까지의 거리 `d` 가 `d ≤ EnemyStats.NormalReactRange + Epsilon`(= 3.0 + 1e-9)이고 `d > 0.8 + Epsilon`(0.8 = `EnemyStats.AttackRange(EnemyKind.Normal)`)이면 `min(EnemyStats.NormalMoveSpeed × StepSeconds, d − 0.8)`(한 칸에 최대 0.02)만큼 제자 쪽으로(좌표가 줄어드는 쪽으로) 다가온다. 그 밖에는 움직이지 않는다.
     - 보스(`Boss`)는 움직이지 않는다.
- **시간** `Tick(deltaSeconds)`:
  - 0이면 아무 일도 없다. 음수·`NaN`·무한대(양·음)면 `System.ArgumentOutOfRangeException` (변화 없음).
  - 멈췄거나 `IsDiscipleDefeated` 면 아무것도 하지 않고 받은 시간도 모으지 않고 버린다.
  - `deltaSeconds` 가 `MaxTickSeconds`(0.25, 임시값)보다 크면 0.25로 잘라낸다 (예외 없음).
  - 받은 시간을 모아 두었다가, 모은 시간이 `StepSeconds − Epsilon`(= 0.02 − 1e-9) 이상인 동안 `Step` 을 한 번 하고 0.02를 뺀다 (0보다 작아지면 0). 남은 시간은 다음 `Tick` 으로 넘긴다. 예: `Tick(1.0)` → 12칸 진행하고 0.01초 남김. `Tick(0.01)` 두 번 → 1칸.
  - 도는 중에 멈춤이나 패배 상태가 되면 그 칸까지만 하고 더 진행하지 않는다. 이때 남은 모은 시간은 그대로 둔다.
- **초기화** `Reset()`: 제자 좌표 0, 제자 체력 = `upgrades.MaxHealth`, 제자 공격 대기 0, `IsDiscipleDefeated = false`, 모든 적 제거(`EnemyDefeated` 는 보내지 않는다), 다음 `Index` 0, 모아 둔 시간 0으로 만든 뒤 `ResetCompleted` 를 한 번 보낸다. `Paused` 는 바꾸지 않는다. 멈춘 동안에도 정상 동작한다. 연달아 부르면 부를 때마다 같은 결과이고 `ResetCompleted` 가 부른 횟수만큼 온다.
- **멈춤** `SetPaused(paused)`: `Paused` 를 바꾼다. 같은 값이면 아무 일도 없다. 알림은 없다. 처음에는 멈춰 있지 않다. 멈춘 동안 `Step` 과 `Tick` 은 아무것도 하지 않는다.
- 구독자가 없어도 알림을 보내는 곳에서 예외가 나지 않아야 한다.
- 시간은 `Tick` 의 인자로만 받는다 (`UnityEngine.Time`, `DateTime.Now` 를 쓰지 않는다). 난수와 저장은 쓰지 않는다. 전투에는 저장할 값이 없다.

## 완료 조건
아래에서 따로 적지 않으면 `wallet = new Wallet(1000)`, `upgrades = new StatUpgrades(wallet, 10, 100, 1.0)`, `battle = new Battle(upgrades)` 로 만든 것이다. "n칸 뒤"는 만든 뒤(적을 추가했다면 추가한 뒤)부터 `Step()` 을 모두 n번 부른 뒤다. 좌표·시간은 오차 1e-6 안에서 비교한다. 알림 횟수는 `Battle` 을 만든 직후 구독해 센 누적 횟수다.

- `Assets/Tests/EditMode/Combat/BattleTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 새로 만들면 `DisciplePosition` 은 0, `DiscipleHealth` 는 100, `DiscipleMaxHealth` 는 100, `DiscipleAttackCooldown` 은 0, `IsDiscipleDefeated` 는 `false`, `Paused` 는 `false`, `Enemies.Count` 는 0이다.
- `Battle` 의 상수는 `StepSeconds` 0.02, `MaxTickSeconds` 0.25, `Epsilon` 1e-9, `DiscipleBaseAttack` 10, `DiscipleBaseMaxHealth` 100, `DiscipleBaseAttackInterval` 1.0, `DiscipleMoveSpeed` 2.0, `DiscipleAttackRange` 1.0이다.
- `new Battle(null)` 은 `ArgumentNullException` 을 던진다.
- 적이 없을 때 1칸 뒤 `DisciplePosition` 은 0.04, 50칸 뒤 2.0이고 `DiscipleAttackCooldown` 은 0이다.
- `AddEnemy(Normal, 2, 5.0)` 이 돌려준 적은 `Kind` 가 `Normal`, `Index` 0, `Position` 5.0, `MaxHealth` 와 `Health` 40, `Attack` 7, `AttackInterval` 2.0, `AttackCooldown` 0, `IsDead` `false` 이고, `EnemyAdded` 가 그 적을 담아 1번 오며 `Enemies.Count` 는 1이고 `Enemies[0]` 은 그 적이다. 이어서 `AddEnemy(Boss, 1, 8.0)` 이 돌려준 적의 `Index` 는 1, `MaxHealth` 는 100, `Attack` 은 10이고 `Enemies[1]` 은 그 적이다.
- `AddEnemy(Normal, 0, 5.0)`, `AddEnemy((EnemyKind)99, 1, 5.0)`, `AddEnemy(Normal, 1, double.NaN)`, `AddEnemy(Normal, 1, double.PositiveInfinity)`, `AddEnemy(Normal, 1, -0.1)` 은 각각 `ArgumentOutOfRangeException` 을 던지고 `Enemies.Count` 는 0, `EnemyAdded` 는 0번 온다. 이어서 `AddEnemy(Normal, 1, 0.0)` 은 예외 없이 추가되고 그 적의 `Index` 는 0이다.
- 적이 없을 때 50칸 뒤(`DisciplePosition` 2.0) `AddEnemy(Normal, 1, 1.9)` 는 `ArgumentOutOfRangeException` 을 던지고, `AddEnemy(Normal, 1, 2.0)` 은 예외 없이 추가된다.
- `AddEnemy(Normal, 1, 10.0)` 뒤 1칸 뒤에 그 적의 `Position` 은 10.0이고 `DisciplePosition` 은 0.04다 (반응 거리 밖의 몬스터는 기다린다).
- `AddEnemy(Normal, 1, 5.0)` 뒤: 49칸 뒤 적의 `Position` 은 5.0이고 `DisciplePosition` 은 1.96, 50칸 뒤 적의 `Position` 은 4.98이고 `DisciplePosition` 은 2.0이다 (제자가 3 유닛 안으로 오면 다가온다).
- `AddEnemy(Normal, 1, 5.0)` 뒤: 82칸 뒤 `DisciplePosition` 은 3.28이고 적의 `Position` 은 4.34, 83칸 뒤 `DisciplePosition` 은 3.32이고 적의 `Position` 은 4.32다 (제자는 거리 1.0에서 멈춘다). 93칸 뒤 적의 `Position` 은 4.12이고, 100칸 뒤에도 `DisciplePosition` 은 3.32, 적의 `Position` 은 4.12다 (몬스터는 거리 0.8에서 멈춘다).
- `AddEnemy(Boss, 1, 5.0)` 뒤: 100칸 뒤 `DisciplePosition` 은 4.0이고 보스의 `Position` 은 5.0, 150칸 뒤에도 `DisciplePosition` 은 4.0, 보스의 `Position` 은 5.0이다 (보스는 움직이지 않는다).
- `AddEnemy(Boss, 1, 3.0)` 과 `AddEnemy(Boss, 1, 2.0)` 을 이 순서로 부른 뒤 100칸 뒤 `DisciplePosition` 은 1.0이다 (가장 가까운 적 앞에서 멈춘다).
- `AddEnemy(Normal, 1, 5.0)` 뒤 10칸 진행하고(`DisciplePosition` 0.4) `Reset()` 을 부르면 `DisciplePosition` 은 0, `DiscipleHealth` 는 100, `DiscipleAttackCooldown` 은 0, `IsDiscipleDefeated` 는 `false`, `Enemies.Count` 는 0이고, `ResetCompleted` 는 1번, `EnemyDefeated` 는 0번 온다. 이어서 `AddEnemy(Normal, 1, 5.0)` 의 `Index` 는 0이다. `Reset()` 을 한 번 더 부르면 `ResetCompleted` 는 모두 2번이다.
- 적이 없을 때 `Tick(0.01)` 뒤 `Reset()` 을 부르고 `Tick(0.01)` 을 부르면 `DisciplePosition` 은 0이다 (모아 둔 시간도 0이 된다). 이어서 `Tick(0.01)` 을 한 번 더 부르면 0.04다.
- `SetPaused(true)` 뒤 `Paused` 는 `true` 이고, `Step()` 10번과 `Tick(0.1)` 을 불러도 `DisciplePosition` 은 0이다. 이어서 `SetPaused(false)` 뒤 1칸 뒤 `DisciplePosition` 은 0.04다 (멈춘 동안의 `Tick` 시간은 버린다).
- `SetPaused(true)` 상태에서 `AddEnemy(Normal, 1, 5.0)` 은 추가되고 `EnemyAdded` 가 1번 온다. `Reset()` 을 부르면 `Enemies.Count` 는 0이고 `Paused` 는 `true` 그대로이며 `ResetCompleted` 가 1번 온다. `SetPaused(true)` 를 한 번 더 불러도 예외가 없다.
- 적이 없을 때 `Tick(0.1)` 뒤 `DisciplePosition` 은 0.2다 (5칸).
- 적이 없을 때 `Tick(0.01)` 1번 뒤 `DisciplePosition` 은 0, 2번 뒤 0.04다.
- 적이 없을 때 `Tick(0.05)` 뒤 `DisciplePosition` 은 0.08이고, 이어서 `Tick(0.01)` 뒤 0.12다 (남은 시간을 넘긴다).
- 적이 없을 때 `Tick(1.0)` 뒤 `DisciplePosition` 은 0.48이다 (0.25초로 잘라내 12칸).
- `Tick(0)` 뒤 `DisciplePosition` 은 0이다. `Tick(-0.1)`, `Tick(double.NaN)`, `Tick(double.PositiveInfinity)` 는 각각 `ArgumentOutOfRangeException` 을 던지고 `DisciplePosition` 은 0 그대로다.
- 기존 테스트가 모두 통과한다. (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다. `PlumSword.Core` 는 순수 C# 이다.
- 공격, 피해, 쓰러짐 판정, 패배 상태, `EnemyDamaged`·`DiscipleDamaged`·`EnemyDefeated`·`DiscipleDefeated` 알림을 만들지 않는다. 009_battle_attack 이 한다.
- 체력 강화 반영(`upgrades.Upgraded` 구독)과 알림 안에서 전투를 다시 부르는 경우의 처리를 만들지 않는다. 010_battle_upgrade_reentry 가 한다.
- `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/` 와 `EnemyKind.cs`, `EnemyStats.cs`, `CombatLayout.cs` 를 고치지 않는다. 공개된 모양으로만 쓴다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. 011_combat_view 가 한다.
- 몬스터 배치, 성공·실패 판정, 금화 보상을 만들지 않는다. 스테이지 기능의 일이다. `Battle` 은 `Wallet` 을 쓰지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `PlumSword.Core.Combat.Enemy` 를 만들었다. 생성자는 internal, 바뀌는 값(`Position`, `Health`, `AttackCooldown`, `IsDead`)의 설정은 internal 이다.
- `PlumSword.Core.Combat.Battle` 을 만들었다. 사양 "연결 지점"의 공개 멤버를 전부 선언하고, 생성자·`Tick`(모으기, 0.25초 잘라내기, 남은 시간 넘기기, 멈춤·패배 시 버리기)·`Step`(대기 감소 → 제자 이동 → 일반 몬스터 이동)·`AddEnemy`·`Reset`·`SetPaused` 를 구현했다.
- `Step` 은 `ReduceCooldowns`, `MoveDisciple`, `MoveEnemies` private 메서드로 나누고, 공격·쓰러짐 판정이 들어갈 자리를 주석으로 남겼다 (009 가 채운다).
- `EnemyDamaged`·`DiscipleDamaged`·`EnemyDefeated`·`DiscipleDefeated` 는 선언만 하고 보내지 않는다. `upgrades.Upgraded` 는 구독하지 않는다.
- `BattleTests` 에 완료 조건마다 테스트를 하나 이상 썼다 (28개).
- 사양 `docs/spec/combat.md` 의 `AddEnemy` 좌표 검사에 허용 오차를 반영했다 (아래 가정).

### 바뀐 파일
- `Assets/Scripts/Core/Combat/Enemy.cs` (+ `.meta`) — 새 파일
- `Assets/Scripts/Core/Combat/Battle.cs` (+ `.meta`) — 새 파일
- `Assets/Tests/EditMode/Combat/BattleTests.cs` (+ `.meta`) — 새 파일
- `docs/spec/combat.md` — "연결 지점"의 `AddEnemy` 주석, "경계 상황" 표 한 줄

### 가정
- `AddEnemy` 의 "제자보다 뒤" 판정은 `position < DisciplePosition − Epsilon` 이다. 0.04 를 50번 더한 좌표가 2.0000000000000004 가 되어 완료 조건의 `AddEnemy(Normal, 1, 2.0)`(같은 좌표 → 정상 추가)이 거부되었기 때문이다. 1.9 는 여전히 거부된다. 사양의 "연결 지점"과 "경계 상황"에 적었다. 공개 모양은 바뀌지 않았다.
- `AddEnemy` 의 검사 순서: 좌표를 먼저 검사하고, 그다음 `EnemyStats` 호출로 `stage`·`kind` 를 검사한다. 상태는 모든 계산이 끝난 뒤에 바꾸므로 어느 쪽 예외에서도 변화·알림이 없다.
- `Tick` 은 인자 검사(예외)를 멈춤·패배 검사보다 먼저 한다. 즉 멈춘 동안에도 음수·NaN·무한대에는 예외가 난다.
- `Tick` 의 반복 중 멈춤·패배 확인은 매 칸 `Step` 뒤에 하고, 그때 남은 모은 시간은 그대로 둔다 (작업 문서대로).
- `Enemy.MaxHealth`, `Attack`, `AttackInterval`, `Kind`, `Index` 는 읽기 전용(설정 없음)으로 두었다. 바뀌지 않는 값이기 때문이다.
- `.meta` 파일은 기존 파일과 같은 형식(`fileFormatVersion`, `guid`)으로 직접 만들었다.

### 사람이 확인할 것
- 완료 조건에 [사람 확인] 항목은 없다.
- `AddEnemy` 좌표 검사의 1e-9 허용 오차가 의도에 맞는지 (위 가정 첫 번째).
- 쓰이지 않는 이벤트 4개에 대한 CS0067 경고가 나는 것은 작업 문서가 허용한 것이다.

### 테스트 결과
- `python tools/loop/run_unity_tests.py`: EditMode 155개 중 155개 통과, 0개 실패 (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함).
- 처음 실행에서 `AddEnemy_BehindAdvancedDisciple_ThrowsButSamePositionIsAllowed` 가 소수 오차로 실패했고, 위 가정대로 고친 뒤 전부 통과했다.


## 루프 기록 (2026-11-16 20:41)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/008_battle_movement
- 루프 브랜치: loop/20261116-164139
