# 016_stage_progress_kills — 일반 몬스터 처치 금화·진행도와 보스 등장

## 목표
`PlumSword.Core` 의 `StageProgress` 에 일반 몬스터 처치 처리를 더한다. 쓰러진 일반 몬스터마다 금화를 주고 처치 수를 올리며, 열 번째 처치에서 보스를 낸다. 진행 바 비율(`BarRatio`)의 보스 체력 쪽과 보스가 맞을 때의 `Changed` 도 만든다. 에딧 모드 테스트로 검사한다.

## 묶음
스테이지

## 선행 작업
- 001_wallet
- 007_enemy_stats
- 009_battle_attack
- 010_battle_upgrade_reentry
- 013_stage_rewards_text
- 014_stage_layout
- 015_stage_progress_begin

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stage.md ("동작" 절의 "적을 가리는 법", "일반 몬스터가 쓰러짐", "진행 바 비율", "보스 체력이 줄어듦", "`Changed` 가 가는 때", "경계 상황" 표, "연결 지점" 절)
- docs/spec/combat.md ("동작" 절의 "한 칸(`Step`)의 순서", "동시에 쓰러짐", "연결 지점" 절)
- docs/spec/currency.md ("연결 지점" 절의 `Wallet.Add`)

## 요구사항
- 고칠 파일: `Assets/Scripts/Core/Stage/StageProgress.cs` (네임스페이스 `PlumSword.Core.Stage`).
- 새 테스트 파일: `Assets/Tests/EditMode/Stage/StageProgressKillTests.cs` (네임스페이스 `PlumSword.Core.Tests.Stage`, 클래스 `StageProgressKillTests`). 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다. 앞 작업의 `StageProgressTests.cs` 의 테스트는 지우거나 약하게 고치지 않는다.
- 선행 작업이 만든 것을 쓴다.
  - 015_stage_progress_begin: `StageProgress` 의 공개 모양 전체, 생성자(`battle.EnemyDefeated`, `battle.DiscipleDefeated`, `battle.EnemyDamaged` 를 구독해 두었고 처리 메서드는 비어 있다), `Begin()`, `HasBegun`, `Phase`, `DefeatedNormalCount`, `Boss`, `IsBossActive`, `BarRatio`(보스가 없을 때 `DefeatedNormalCount / 10.0`), 알림 `Changed`.
  - 013_stage_rewards_text: `StageRewards.NormalGold(int stage)` (1스테이지 10).
  - 014_stage_layout: `StageLayout.BossDistance` (18.0). 테스트용 난수 `MinRandom`.
  - 001_wallet: `Wallet.Add(long amount)`, `long Balance { get; }`, `event Action<long> BalanceChanged`.
  - 007 ~ 010: `Battle` 의 `Enemy AddEnemy(EnemyKind kind, int stage, double position)`, `void Step()`, `double DisciplePosition { get; }`, `long DiscipleHealth { get; }`, `bool IsDiscipleDefeated { get; }`, `IReadOnlyList<Enemy> Enemies { get; }`. `Enemy` 의 `Kind`, `Position`, `Health`, `MaxHealth`. `CombatLayout.HealthRatio(long health, long maxHealth)`. `EnemyDefeated` 를 받는 시점에 `IsDiscipleDefeated` 는 이미 그 칸의 결과이고, 그 안에서 `AddEnemy` 를 불러도 된다 (새 적은 다음 칸부터 행동한다).
- 공개 모양은 바꾸지 않는다 (사양 `docs/spec/stage.md` 의 "연결 지점" 절 그대로다). 구현하다 모양이 사양과 달라지면 사양 문서의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- **무시하는 때**: `HasBegun` 이 `false` 이거나 `Phase` 가 `Playing` 이 아닌 동안에는 전투의 알림(`EnemyDefeated`, `DiscipleDefeated`, `EnemyDamaged`)을 모두 무시한다. 금화도, 처치 수도, 알림도 없다.
- **적을 가리는 법**: 쓰러진 적을 `Enemy.Kind` 로만 가린다. 그 적을 누가 추가했는지(스테이지 배치인지 테스트가 직접 넣었는지)는 가리지 않는다.
- **일반 몬스터가 쓰러짐** (`battle.EnemyDefeated`, `Kind == EnemyKind.Normal`): 아래를 순서대로 한다.
  1. `wallet.Add(StageRewards.NormalGold(StageNumber))`. 제자가 같은 칸에 쓰러졌어도(`battle.IsDiscipleDefeated` 가 `true`) 준다.
  2. `DefeatedNormalCount` 가 10보다 작으면 1 올린다. 이미 10이면 그대로 둔다.
  3. 2번에서 9가 10이 되었고 `battle.IsDiscipleDefeated` 가 `false` 면 보스를 낸다: `Boss = battle.AddEnemy(EnemyKind.Boss, StageNumber, battle.DisciplePosition + StageLayout.BossDistance)`.
  4. `Changed` 를 한 번 보낸다.
- 한 칸에 일반 몬스터 여러 마리가 쓰러지면 `EnemyDefeated` 가 한 마리씩 오므로 한 마리씩 차례로 처리된다 (3마리 → 금화 3번, 처치 수 +3, `Changed` 3번).
- 처치 수가 이미 10인데 일반 몬스터가 또 쓰러지면 금화는 주고, 처치 수는 10 그대로, 보스는 다시 내지 않으며 `Changed` 는 1번 보낸다.
- 열 번째 일반 몬스터와 제자가 같은 칸에 쓰러지면 금화를 주고 처치 수는 10이 되지만 보스를 내지 않는다 (`Boss` 는 `null`).
- **`BarRatio`**: `Boss` 가 `null` 이면 `DefeatedNormalCount / 10.0`. `null` 이 아니면 `CombatLayout.HealthRatio(Boss.Health, Boss.MaxHealth)`.
- **보스 체력이 줄어듦** (`battle.EnemyDamaged`): 그 적이 `Boss` 와 같은 객체면 `Changed` 를 한 번 보낸다. 다른 적이면 아무것도 하지 않는다.
- `Kind` 가 `Boss` 인 적이 쓰러진 것과 `battle.DiscipleDefeated` 는 이 작업에서 처리하지 않는다 (처리 메서드를 비워 둔다. 017_stage_progress_outcome 이 한다).
- 금화는 `long` 이고 시간·난수·저장은 이 작업에서 새로 쓰지 않는다.

## 완료 조건
항목 앞의 `(공격력, 최대 체력)` 은 `upgrades = new StatUpgrades(wallet, 공격력, 최대 체력, 1.0)` 의 값이다. 따로 적지 않으면 `wallet = new Wallet(0)`, `battle = new Battle(upgrades)`, `stage = new StageProgress(wallet, upgrades, battle, new MinRandom())` 로 만든 것이다 (`Begin()` 이 놓는 적 10마리는 좌표 6.0 ~ 33.0 에 있어 첫 칸에는 싸움에 끼지 않는다). "시작한 뒤"는 `stage.Begin()` 을 부른 뒤, "n칸 진행"은 `battle.Step()` 을 n번 부르는 것, "적을 직접 넣는다"는 테스트가 `battle.AddEnemy` 를 부르는 것이다. 금화는 `wallet.Balance` 다. 좌표·비율은 오차 1e-6 안에서 비교한다.

- `Assets/Tests/EditMode/Stage/StageProgressKillTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- (1000, 100) `Begin()` 을 부르지 않고 `Normal` 적 하나를 1스테이지, 좌표 0.8 에 직접 넣어 1칸 진행하면, 금화는 0, `DefeatedNormalCount` 는 0 이고 `Changed` 는 0번 온다.
- (1000, 100) 시작한 뒤 `Normal` 적 3마리를 1스테이지, 좌표 0.8 에 직접 넣고 1칸 진행하면, 금화는 30, `Wallet.BalanceChanged` 의 값은 순서대로 10, 20, 30, `DefeatedNormalCount` 는 3, `BarRatio` 는 0.3, `IsBossActive` 는 `false`, `Changed` 는 (시작 때의 1번 뒤로) 3번 오고, `Phase` 는 `Playing` 이다.
- (1000, 100) `startStage` 를 2로 만들고 시작한 뒤 `Normal` 적 하나를 2스테이지, 좌표 0.8 에 직접 넣고 1칸 진행하면 금화는 12 다.
- (1000, 100) 시작한 뒤 `Normal` 적 10마리를 1스테이지, 좌표 0.8 에 직접 넣고 1칸 진행하면, 금화는 100, `DefeatedNormalCount` 는 10, `IsBossActive` 는 `true`, `Boss.Kind` 는 `Boss`, `Boss.Position` 은 18.0, `Boss.MaxHealth` 는 100, `BarRatio` 는 1.0, `battle.Enemies.Count` 는 11, `battle.DiscipleHealth` 는 50, `Phase` 는 `Playing` 이다.
- (1000, 100) 시작한 뒤 `Normal` 적 11마리를 1스테이지, 좌표 0.8 에 직접 넣고 1칸 진행하면, 금화는 110, `DefeatedNormalCount` 는 10, `battle.Enemies.Count` 는 11 (보스는 하나뿐), `battle.DiscipleHealth` 는 45, `Changed` 는 (시작 때의 1번 뒤로) 11번 온다.
- (1000, 50) 시작한 뒤 `Normal` 적 10마리를 1스테이지, 좌표 0.8 에 직접 넣고 1칸 진행하면 (열 번째 몬스터와 제자가 함께 쓰러짐), 금화는 100, `DefeatedNormalCount` 는 10, `Boss` 는 `null`, `IsBossActive` 는 `false`, `BarRatio` 는 1.0, `battle.Enemies.Count` 는 10 이다.
- (30, 100000) 시작한 뒤 `Normal` 적 10마리를 1스테이지, 좌표 0.8 에 직접 넣고 1칸 진행한 다음, `Boss.Health` 가 100보다 작아질 때까지 최대 5000칸 진행하면, `Boss.Health` 는 70, `BarRatio` 는 0.7, `DefeatedNormalCount` 는 10 이고, 마지막 칸에서 `Changed` 가 1번 이상 온다.
- 기존 테스트가 모두 통과한다. (`StageProgressTests`, `StageRewardsTests`, `StageLayoutTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다. `PlumSword.Core` 는 순수 C# 이다.
- `StageProgress` 의 공개 모양을 바꾸거나 공개 멤버를 더하지 않는다.
- 보스 처치(성공), 제자 패배(실패), 전환 시작(`Phase = FadingOut`, `battle.SetPaused`, `upgrades.SetLocked`), `StageCleared`·`StageFailed` 보내기를 만들지 않는다. 017_stage_progress_outcome 이 한다.
- 페이드 시간 세기와 가려진 순간의 교체를 만들지 않는다. 018_stage_progress_fade 가 한다.
- `StageRewards.cs`, `StageText.cs`, `StageLayout.cs`, `StagePhase.cs` 를 고치지 않는다.
- `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/`, `Assets/Scripts/Core/Combat/` 를 고치지 않는다. `Wallet`, `Battle`, `CombatLayout` 은 공개된 모양으로만 쓴다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. 019_stage_view 가 한다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `StageProgress` 에 일반 몬스터 처치 처리를 더했다: 금화 `wallet.Add(StageRewards.NormalGold(StageNumber))` → 처치 수 +1(10 에서 멈춤) → 9 에서 10 이 되고 제자가 쓰러지지 않았으면 보스를 `DisciplePosition + BossDistance` 에 낸다 → `Changed` 1번.
- `HasBegun` 이 `false` 이거나 `Phase` 가 `Playing` 이 아니면 `EnemyDefeated`·`EnemyDamaged` 를 무시한다 (비공개 속성 `IsListening`).
- `BarRatio` 가 보스가 있으면 `CombatLayout.HealthRatio(Boss.Health, Boss.MaxHealth)` 를 돌려준다.
- `EnemyDamaged` 의 적이 `Boss` 와 같은 객체(`ReferenceEquals`)면 `Changed` 1번.
- 보스 처치와 `DiscipleDefeated` 처리는 비워 두었다 (017 이 한다).
- 에딧 모드 테스트 `StageProgressKillTests` 8개를 더했다 (완료 조건 7개 + 보스가 아닌 적이 맞을 때 `Changed` 없음).

### 바뀐 파일
- `Assets/Scripts/Core/Stage/StageProgress.cs` (수정)
- `Assets/Tests/EditMode/Stage/StageProgressKillTests.cs` (새 파일)
- `Assets/Tests/EditMode/Stage/StageProgressKillTests.cs.meta` (유니티가 만듦)

### 가정
- `EnemyDefeated`·`EnemyDamaged` 에 `null` 적이 오면 무시한다 (전투가 보내지 않는 경우라 방어용).
- 공개 모양은 바꾸지 않았다. 사양 문서는 이미 이 동작을 적고 있어 고치지 않았다.

### 사람이 확인할 것
없음

### 테스트 결과
- `python tools/loop/run_unity_tests.py --project .` → EditMode 249개 중 249개 통과, 0개 실패 (기존 `StageProgressTests`, `StageRewardsTests`, `StageLayoutTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함).


## 루프 기록 (2026-11-16 21:05)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/016_stage_progress_kills
- 루프 브랜치: loop/20261116-205758
