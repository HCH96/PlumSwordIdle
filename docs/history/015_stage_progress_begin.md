# 015_stage_progress_begin — 스테이지 진행의 뼈대와 첫 배치

## 목표
`PlumSword.Core` 에 스테이지 진행 로직 `StageProgress` 의 뼈대를 만든다. 공개 모양 전체, 생성자, 첫 스테이지를 배치하는 `Begin()`, `Tick` 의 인자 검사까지다. 단계 열거형 `StagePhase` 도 함께 만든다. 처치·보스·성공·실패·페이드는 뒤 작업(016 ~ 018)이 채운다. 에딧 모드 테스트로 검사한다.

## 묶음
스테이지

## 선행 작업
- 001_wallet
- 004_stat_upgrades
- 007_enemy_stats
- 008_battle_movement
- 009_battle_attack
- 010_battle_upgrade_reentry
- 014_stage_layout

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stage.md ("동작" 절의 "상태", "시작", "스테이지 배치", "경계 상황" 표, "연결 지점" 절)
- docs/spec/combat.md ("연결 지점" 절의 `Battle`, `Enemy`, `EnemyKind`)
- docs/spec/currency.md ("연결 지점" 절의 `Wallet`)
- docs/spec/stat_upgrade.md ("연결 지점" 절의 `StatUpgrades`)

## 요구사항
- 만들 파일 (네임스페이스 `PlumSword.Core.Stage`): `Assets/Scripts/Core/Stage/StagePhase.cs`, `Assets/Scripts/Core/Stage/StageProgress.cs`.
- 테스트 파일: `Assets/Tests/EditMode/Stage/StageProgressTests.cs` (네임스페이스 `PlumSword.Core.Tests.Stage`, 클래스 `StageProgressTests`). 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다.
- 선행 작업이 만든 것을 쓴다. 내부는 고치지 않는다.
  - 014_stage_layout: `PlumSword.Core.Stage.StageLayout` — `int[] RollGroups(System.Random random)`, `double[] Positions(IReadOnlyList<int> groups)`. 테스트용 난수 `MinRandom` (무리 구성이 `1` 이 10개, 좌표 6.0, 9.0, …, 33.0), `MaxRandom` (무리 구성 `3·3·3·1`). 둘 다 `Assets/Tests/EditMode/Stage/` 에 있다.
  - 008 ~ 010: `PlumSword.Core.Combat.Battle` — `new Battle(StatUpgrades upgrades)`, `void Reset()`, `Enemy AddEnemy(EnemyKind kind, int stage, double position)`, `IReadOnlyList<Enemy> Enemies { get; }`, `bool Paused { get; }`, 알림 `event Action<Enemy> EnemyDefeated`, `event Action DiscipleDefeated`, `event Action<Enemy> EnemyDamaged`. `ResetCompleted` 뒤나 알림 안에서 `AddEnemy` 를 불러도 된다.
  - 007_enemy_stats: `PlumSword.Core.Combat.EnemyKind` (`Normal`, `Boss`), `Enemy` 의 `Kind`, `Position`, `MaxHealth`. 일반 몬스터 체력은 `30 + 10 × (stage − 1)` 이다.
  - 001_wallet: `PlumSword.Core.Currency.Wallet` — `new Wallet(long initialBalance = 0)`.
  - 004_stat_upgrades: `PlumSword.Core.StatUpgrade.StatUpgrades` — `new StatUpgrades(wallet, 공격력, 최대 체력, 공격 간격)`, `bool Locked { get; }`.
- 공개 모양은 사양의 "연결 지점" 절 그대로이고, **이 작업에서 전부 선언한다** (뒤 작업은 공개 멤버를 더하지 않는다).

  ```csharp
  namespace PlumSword.Core.Stage
  {
      public enum StagePhase { Playing = 0, FadingOut = 1, FadingIn = 2 }

      public sealed class StageProgress
      {
          public const double FadeSeconds = 0.5;

          public StageProgress(
              PlumSword.Core.Currency.Wallet wallet,
              PlumSword.Core.StatUpgrade.StatUpgrades upgrades,
              PlumSword.Core.Combat.Battle battle,
              System.Random random,
              int startStage = 1);

          public int StageNumber { get; }
          public StagePhase Phase { get; }
          public bool HasBegun { get; }
          public int DefeatedNormalCount { get; }
          public PlumSword.Core.Combat.Enemy Boss { get; }
          public bool IsBossActive { get; }
          public double BarRatio { get; }
          public double FadeAlpha { get; }

          public void Begin();
          public void Tick(double deltaSeconds);

          public event System.Action<int> StageStarted;
          public event System.Action<int> StageCleared;
          public event System.Action<int> StageFailed;
          public event System.Action Changed;
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/stage.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- **생성자**:
  - `wallet`, `upgrades`, `battle`, `random` 중 하나라도 `null` 이면 `System.ArgumentNullException`.
  - `startStage` 가 1보다 작으면 1로 만든다 (예외 없음).
  - `battle.EnemyDefeated`, `battle.DiscipleDefeated`, `battle.EnemyDamaged` 를 구독한다. 이 작업에서는 세 처리 메서드를 비워 둔다 (016_stage_progress_kills, 017_stage_progress_outcome 이 채운다).
  - 생성자는 전투·지갑·강화를 건드리지 않고 (`battle.Reset`, `AddEnemy` 를 부르지 않는다) 알림을 보내지 않는다.
  - 시작 상태: `StageNumber` 는 `startStage`, `Phase` 는 `Playing`, `HasBegun` 은 `false`, `DefeatedNormalCount` 는 0, `Boss` 는 `null`.
- **`Begin()`**: 처음 불렸을 때만 아래 "스테이지 배치"를 하고 `HasBegun` 을 `true` 로 만든 뒤 `StageStarted(StageNumber)`, `Changed` 를 이 순서로 한 번씩 보낸다. 페이드는 없고, 전투 멈춤(`battle.SetPaused`)과 강화 잠금(`upgrades.SetLocked`)은 건드리지 않는다. 두 번째부터는 아무 일도 없다 (변화 없음, 알림 없음).
- **스테이지 배치** (018_stage_progress_fade 가 다시 쓰므로 `private` 메서드 하나로 뺀다): 아래를 순서대로 한다.
  1. `battle.Reset()`.
  2. `StageLayout.RollGroups(random)` 으로 무리 구성을 새로 정한다.
  3. `StageLayout.Positions(무리 구성)` 의 좌표마다 순서대로 `battle.AddEnemy(EnemyKind.Normal, StageNumber, 좌표)` 를 부른다 (모두 10번).
  4. `DefeatedNormalCount = 0`, `Boss = null`.
- **속성** (이 작업에서의 값):
  - `IsBossActive`: `Boss != null`.
  - `BarRatio`: `Boss` 가 `null` 이면 `DefeatedNormalCount / 10.0`. (보스가 있을 때의 값은 016_stage_progress_kills 가 만든다. 이 작업에서는 `Boss` 가 항상 `null` 이다.)
  - `FadeAlpha`: `Phase` 가 `Playing` 이면 0.0. (페이드 중의 값은 018_stage_progress_fade 가 만든다. 이 작업에서는 `Phase` 가 항상 `Playing` 이다.)
- **`Tick(deltaSeconds)`**: 음수, `NaN`, 무한대면 `System.ArgumentOutOfRangeException` (`Begin` 전이어도, `Playing` 이어도). 그 밖에는 이 작업에서는 아무것도 하지 않는다 (`Begin` 전이거나 `Phase` 가 `Playing` 이면 아무 일도 없는 것이 사양이고, 페이드 처리는 018_stage_progress_fade 가 만든다).
- `StageCleared`, `StageFailed` 는 선언만 하고 이 작업에서는 보내지 않는다. 쓰이지 않는 이벤트 경고(CS0067)가 나도 괜찮다.
- 경과 시간은 `Tick` 의 인자로만 받고, 난수는 생성자로 받은 것만 쓴다. 저장은 하지 않는다 (`startStage` 가 2단계 저장 기능을 위한 자리다).

## 완료 조건
아래에서 따로 적지 않으면 `wallet = new Wallet(0)`, `upgrades = new StatUpgrades(wallet, 10, 100, 1.0)`, `battle = new Battle(upgrades)`, `stage = new StageProgress(wallet, upgrades, battle, new MinRandom())` 로 만든 것이다. 알림 횟수는 `StageProgress` 를 만든 직후 구독해 센 누적 횟수다. 좌표·비율은 오차 1e-6 안에서 비교한다.

- `Assets/Tests/EditMode/Stage/StageProgressTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 새로 만들면 `StageNumber` 는 1, `HasBegun` 은 `false`, `Phase` 는 `StagePhase.Playing`, `DefeatedNormalCount` 는 0, `Boss` 는 `null`, `IsBossActive` 는 `false`, `BarRatio` 는 0.0, `FadeAlpha` 는 0.0 이고, `battle.Enemies.Count` 는 0 이다. `StageProgress.FadeSeconds` 는 0.5 다.
- `startStage` 에 5를 넣으면 `StageNumber` 는 5, 0이나 −3을 넣으면 1이고 예외가 없다.
- 생성자의 `wallet`, `upgrades`, `battle`, `random` 에 각각 `null` 을 넣으면 `ArgumentNullException` 을 던진다.
- `Begin()` 을 부르면 `HasBegun` 은 `true`, `battle.Enemies.Count` 는 10 이고, 적의 `Position` 은 순서대로 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 24.0, 27.0, 30.0, 33.0, 모두 `Kind` 가 `Normal` 이고 `MaxHealth` 가 30 이다. `StageStarted` 가 1을 담아 1번, 그 뒤에 `Changed` 가 1번 온다. `battle.Paused` 와 `upgrades.Locked` 는 `false`, `Phase` 는 `Playing`, `DefeatedNormalCount` 는 0 이다.
- `Begin()` 을 두 번 부르면 `battle.Enemies.Count` 는 10 그대로이고 `StageStarted` 와 `Changed` 는 각각 1번뿐이다.
- `startStage` 를 3으로 만들고 `Begin()` 을 부르면 적 10마리의 `MaxHealth` 는 모두 50 이고 `StageStarted` 가 3을 담아 온다.
- 난수를 `new MaxRandom()` 으로 만들고 `Begin()` 을 부르면 적의 `Position` 은 순서대로 5.7, 6.0, 6.3, 8.7, 9.0, 9.3, 11.7, 12.0, 12.3, 15.0 이다.
- `Begin()` 을 부른 뒤 `Tick(1.0)` 과 `Tick(0)` 을 불러도 `Phase` 는 `Playing`, `StageNumber` 는 1, `battle.Enemies.Count` 는 10 이고 알림은 더 오지 않는다.
- `Tick(-0.1)`, `Tick(double.NaN)`, `Tick(double.PositiveInfinity)` 는 `Begin()` 전에도 뒤에도 각각 `ArgumentOutOfRangeException` 을 던진다. `Begin()` 전의 `Tick(1.0)` 은 예외가 없고 `battle.Enemies.Count` 는 0 이다.
- 기존 테스트가 모두 통과한다. (`StageLayoutTests`, `BattleTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다. `PlumSword.Core` 는 순수 C# 이다.
- 처치 금화 지급, 처치 수 올리기, 보스 등장, 보스 체력 비율을 만들지 않는다. 016_stage_progress_kills 가 한다.
- 성공·실패 판정, 전환 시작(전투 멈춤, 강화 잠금)을 만들지 않는다. 017_stage_progress_outcome 이 한다.
- 페이드 시간 세기, 가려진 순간의 교체, `FadeAlpha` 의 페이드 중 값을 만들지 않는다. 018_stage_progress_fade 가 한다.
- `StageLayout.cs` 와 `MinRandom.cs`, `MaxRandom.cs` 를 고치지 않는다.
- `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/`, `Assets/Scripts/Core/Combat/` 를 고치지 않는다. `Wallet`, `StatUpgrades`, `Battle` 은 공개된 모양으로만 쓴다. `Battle.Tick`, `Battle.Step` 을 `StageProgress` 안에서 부르지 않는다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. 019_stage_view 가 한다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `PlumSword.Core.Stage.StagePhase` 열거형(`Playing = 0`, `FadingOut = 1`, `FadingIn = 2`)을 만들었다.
- `PlumSword.Core.Stage.StageProgress` 뼈대를 만들었다. 공개 모양은 사양 "연결 지점" 그대로 전부 선언했다.
  - 생성자: 네 인자 `null` 검사(`ArgumentNullException`), `startStage < 1` 이면 1, `battle.EnemyDefeated`·`DiscipleDefeated`·`EnemyDamaged` 구독(처리 메서드 `OnEnemyDefeated`·`OnDiscipleDefeated`·`OnEnemyDamaged` 는 빈 `private` 메서드). 전투·지갑·강화를 건드리지 않고 알림도 없다.
  - `Begin()`: 처음 한 번만 `private void PlaceStage()`(Reset → RollGroups → Positions 마다 `AddEnemy(Normal, StageNumber, 좌표)` → 처치 수 0, `Boss = null`)를 부르고 `HasBegun = true`, `StageStarted(StageNumber)` → `Changed`.
  - `Tick`: 음수·NaN·무한대면 `ArgumentOutOfRangeException`. 그 밖에는 `Begin` 전이거나 `Playing` 이면 바로 돌아간다 (페이드 처리 자리는 주석으로 남겼다).
  - `IsBossActive => Boss != null`, `BarRatio => DefeatedNormalCount / 10.0`, `FadeAlpha => 0.0`.
  - `StageCleared`, `StageFailed` 는 선언만 했고 `#pragma warning disable CS0067` 로 경고를 감쌌다.
- `StageProgressTests` 에 완료 조건마다 테스트를 썼다 (테스트 메서드 15개, TestCase 포함 21개).

### 바뀐 파일
- `Assets/Scripts/Core/Stage/StagePhase.cs` (+ `.meta`, 새 파일)
- `Assets/Scripts/Core/Stage/StageProgress.cs` (+ `.meta`, 새 파일)
- `Assets/Tests/EditMode/Stage/StageProgressTests.cs` (+ `.meta`, 새 파일)
- `tasks/doing/015_stage_progress_begin.md` (이 보고서)

### 가정
- `BarRatio` 는 지금 `DefeatedNormalCount / (double)StageLayout.NormalCount` 로 계산한다 (값은 `/ 10.0` 과 같다). 보스가 있을 때의 분기는 016 이 더한다.
- `_wallet`, `_upgrades` 필드는 저장만 하고 이 작업에서는 쓰지 않는다 (016·017 이 쓴다).
- 구독은 끊지 않는다. `Battle` 이 `StatUpgrades` 구독을 끊지 않는 기존 방식과 같다 (게임에 하나씩이고 수명이 같음).
- `StageCleared`/`StageFailed` 의 CS0067 경고는 `#pragma` 로 막았다. 017 에서 이벤트를 보내기 시작하면 이 `#pragma` 를 지워도 된다.
- 사양의 "연결 지점" 은 바꾸지 않았다 (구현 모양이 같다).

### 사람이 확인할 것
- 없음 (완료 조건에 [사람 확인] 항목 없음, 모두 에딧 모드 테스트로 판정함).

### 테스트 결과
- `python tools/loop/run_unity_tests.py` 실행: EditMode 241개 중 241개 통과, 0개 실패. `StageLayoutTests`, `BattleTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함 기존 테스트 모두 통과.


## 루프 기록 (2026-11-16 21:03)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/015_stage_progress_begin
- 루프 브랜치: loop/20261116-205758
