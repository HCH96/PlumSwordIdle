# 009_battle_attack — 전투의 공격, 피해, 쓰러짐 판정과 알림

## 목표
`PlumSword.Core` 의 `Battle.Step` 에 "공격"과 "쓰러짐 판정" 단계를 채운다. 제자와 적이 서로 공격해 체력이 줄고, 체력이 0인 적이 `Enemies` 에서 빠지고, 제자가 패배하면 전투가 멈추며, `EnemyDamaged`·`DiscipleDamaged`·`EnemyDefeated`·`DiscipleDefeated` 알림이 정해진 순서로 간다. 에딧 모드 테스트로 검사한다.

## 묶음
전투

## 선행 작업
- 001_wallet
- 004_stat_upgrades
- 007_enemy_stats
- 008_battle_movement

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/combat.md ("동작", "경계 상황", "수치", "연결 지점" 절)
- docs/spec/stat_upgrade.md ("연결 지점" 절의 `StatUpgrades`)

## 요구사항
- 고칠 파일: `Assets/Scripts/Core/Combat/Battle.cs`, 필요하면 `Assets/Scripts/Core/Combat/Enemy.cs` (둘 다 008_battle_movement 가 만들었다. 네임스페이스 `PlumSword.Core.Combat`).
- 새 테스트 파일: `Assets/Tests/EditMode/Combat/BattleAttackTests.cs`. 네임스페이스 `PlumSword.Core.Tests.Combat`, 클래스 `BattleAttackTests`. 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다. 008 이 만든 `BattleTests.cs` 의 테스트는 지우거나 약하게 고치지 않는다.
- 선행 작업이 만든 것:
  - 008_battle_movement: `Battle` 과 `Enemy` 의 공개 모양 전체가 이미 선언되어 있다. `Step()` 은 "대기 감소 → 이동"까지 한다. `Tick`, `AddEnemy`, `Reset`, `SetPaused` 는 완성되어 있다. `IsDiscipleDefeated` 와 `Enemy.IsDead` 는 아직 항상 `false` 이고, `EnemyDamaged`, `DiscipleDamaged`, `EnemyDefeated`, `DiscipleDefeated` 는 선언만 되어 있다. 이 작업이 채운다.
  - 004_stat_upgrades: `PlumSword.Core.StatUpgrade.StatUpgrades` — `long Attack { get; }`, `long MaxHealth { get; }`, `double AttackInterval { get; }`, `bool TryUpgrade(StatType stat)` (금화가 충분하면 단계를 1 올리고 `true`. 첫 강화 비용 20), `PlumSword.Core.StatUpgrade.StatType` (`Attack`, `Health`, `AttackSpeed`). 공격력 1단계 → `Attack` +2, 공격 속도 1단계 → `AttackInterval` −0.05.
  - 007_enemy_stats: `EnemyStats.AttackRange(EnemyKind kind)` (`Normal` 0.8, `Boss` 1.0), `EnemyKind`.
  - 001_wallet: `new Wallet(1000)` (테스트에서 `StatUpgrades` 를 만들 때 쓴다).
- 공개 모양은 바꾸지 않는다 (사양 `docs/spec/combat.md` 의 "연결 지점" 절 그대로다). 구현하다 모양이 사양과 달라지면 사양 문서의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- **한 칸(`Step`)의 순서**는 "1. 대기 감소 → 2. 이동 → 3. 공격 → 4. 쓰러짐 판정"이다. 1, 2는 이미 있다. 멈춤 상태이거나 `IsDiscipleDefeated` 면 `Step` 은 아무것도 하지 않는다.
- **3. 공격** (이동이 끝난 좌표로 판정한다. `Epsilon` = 1e-9):
  - 제자: 공격 대기 시간이 `Epsilon` 이하이고, 거리(적 좌표 − 제자 좌표)가 `DiscipleAttackRange + Epsilon`(= 1.0 + 1e-9) 이하인 적이 하나라도 있으면 공격한다. 이때 `upgrades.Attack` 을 한 번 읽어, 범위 안의 모든 적의 체력을 그만큼 줄인다 (0에서 멈춘다. 음수가 되지 않는다). 그 뒤 맞은 적마다 `EnemyDamaged` 를 그 적을 담아 추가된 순서대로 보낸다. 그 뒤 제자의 공격 대기 시간을 그 순간의 `upgrades.AttackInterval` 로 정한다. 범위 안에 적이 없으면 공격하지 않고 대기 시간도 바꾸지 않는다. 범위 밖의 적은 맞지 않는다 (`EnemyDamaged` 없음).
  - 적(추가된 순서대로, 제자의 공격 다음에): 자기 공격 대기 시간이 `Epsilon` 이하이고 제자까지의 거리가 `EnemyStats.AttackRange(Kind) + Epsilon` 이하이면 공격한다. 제자의 체력을 자기 `Attack` 만큼 줄이고 (0에서 멈춘다) `DiscipleDamaged` 를 한 번 보낸 뒤, 자기 대기 시간을 자기 `AttackInterval` 로 정한다.
  - **이번 칸에 제자의 공격으로 체력이 0이 된 적도 이 칸의 공격을 한다.** 쓰러짐은 모두가 공격한 뒤에 판정한다.
  - 제자의 체력이 이미 0이 된 뒤에도 같은 칸의 다른 적은 공격한다. 체력은 0 그대로이고 `DiscipleDamaged` 는 공격한 적 수만큼 간다.
- **강화 반영**: 피해를 줄 때마다 `upgrades.Attack` 을 읽는다 (값을 따로 저장해 두지 않는다). `upgrades.AttackInterval` 은 공격한 직후 다음 대기를 정할 때만 읽는다. 이미 흐르는 대기의 남은 시간은 강화로 바뀌지 않는다.
- **4. 쓰러짐 판정**: 체력이 0인 적을 모두 `Enemies` 에서 빼고 `IsDead` 를 `true` 로 만든다. 제자의 체력이 0이면 `IsDiscipleDefeated` 를 `true` 로 만든다. **이 값들을 모두 바꾼 뒤에** 알림을 보낸다: 쓰러진 적마다 `EnemyDefeated`(추가된 순서대로), 그다음 제자가 패배했으면 `DiscipleDefeated` 한 번.
  - 같은 칸에 제자와 적이 함께 쓰러지면 둘 다 처리된다. `EnemyDefeated` 를 받는 시점에 `IsDiscipleDefeated` 는 이미 `true` 다 (스테이지 기능이 이것으로 동시 처치를 가려낸다).
  - 전투는 성공·실패와 보상을 판정하지 않는다. 몬스터를 처치해도 제자의 체력은 회복하지 않는다. 체력이 가득 차는 것은 `Reset` 뿐이다.
- **패배 뒤**: `Reset` 이 불릴 때까지 `Step` 과 `Tick` 은 아무것도 하지 않는다 (변화 없음, 알림 없음. `Tick` 은 시간도 모으지 않는다). 남아 있는 적은 그대로 `Enemies` 에 있다. 패배한 상태에서도 `AddEnemy` 는 정상 추가된다 (`EnemyAdded` 1번). `Reset` 은 `IsDiscipleDefeated` 를 `false` 로 되돌린다 (008 이 이미 그렇게 만들었다. 그대로 동작하는지 확인한다).
- 새로 추가된 적은 공격 대기 시간이 0이라, 범위 안이면 그 칸에 바로 공격한다. 이동 중에도 대기 시간은 줄어든다.
- 구독자가 없어도 알림을 보내는 곳에서 예외가 나지 않아야 한다.
- 알림을 받는 쪽이 그 안에서 `AddEnemy`, `Reset`, `SetPaused` 를 다시 부르는 경우의 처리는 이 작업의 범위가 아니다 (010_battle_upgrade_reentry 가 한다). 다만 그 작업이 쉽도록, 한 칸 동안 적 목록을 돌 때는 `Enemies` 를 직접 돌지 말고 칸을 시작할 때 떠 둔 복사본을 돈다.
- 시간은 `Tick`/`Step` 으로만 흐른다. 난수와 저장은 쓰지 않는다.

## 완료 조건
아래에서 따로 적지 않으면 `wallet = new Wallet(1000)`, `upgrades = new StatUpgrades(wallet, 10, 100, 1.0)`, `battle = new Battle(upgrades)` 로 만든 것이다. "n칸 뒤"는 적을 추가한 뒤부터 `Step()` 을 모두 n번 부른 뒤다. 좌표·시간은 오차 1e-6 안에서 비교한다. 알림 횟수는 `Battle` 을 만든 직후 구독해 센 누적 횟수다. `Normal`, `Boss` 는 `EnemyKind` 의 값이다.

- `Assets/Tests/EditMode/Combat/BattleAttackTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `AddEnemy(Normal, 1, 5.0)` 뒤: 82칸 뒤 적의 `Health` 는 30, 83칸 뒤 20이고 `EnemyDamaged` 는 1번, `DisciplePosition` 은 3.32, 적의 `Position` 은 4.32, `DiscipleAttackCooldown` 은 1.0이다 (제자는 거리 1.0에서 멈춰 바로 공격한다).
- `AddEnemy(Normal, 1, 5.0)` 뒤: 92칸 뒤 `DiscipleHealth` 는 100, 93칸 뒤 95이고 `DiscipleDamaged` 는 1번, 적의 `Position` 은 4.12, 적의 `AttackCooldown` 은 2.0이다 (몬스터는 거리 0.8에서 멈춰 바로 공격한다).
- `AddEnemy(Normal, 1, 5.0)` 뒤: 133칸 뒤 적의 `Health` 는 10, 182칸 뒤 10, 183칸 뒤 0이고 `EnemyDefeated` 가 그 적을 담아 1번 오며, 적의 `IsDead` 는 `true`, `Enemies.Count` 는 0, `DiscipleHealth` 는 95다 (처치해도 체력은 회복하지 않는다). 184칸 뒤 `DisciplePosition` 은 3.36이다.
- `AddEnemy(Normal, 1, 0.8)` 을 3번 부른 뒤 1칸 뒤: 세 적의 `Health` 는 모두 20, `DiscipleHealth` 는 85, `EnemyDamaged` 는 3번(`Index` 0, 1, 2 순서), `DiscipleDamaged` 는 3번 오고, 알림 전체의 순서는 `EnemyDamaged` 3번 다음에 `DiscipleDamaged` 3번이다.
- `AddEnemy(Normal, 1, 0.8)` 을 3번 부른 뒤: 51칸 뒤 세 적의 `Health` 는 모두 10이고 `DiscipleHealth` 는 85다. 101칸 뒤 `Enemies.Count` 는 0, `EnemyDefeated` 는 3번(`Index` 0, 1, 2 순서) 오고, `DiscipleDamaged` 는 모두 6번, `DiscipleHealth` 는 70, `IsDiscipleDefeated` 는 `false` 다 (같은 칸에 쓰러진 몬스터도 그 칸의 공격을 한다).
- `AddEnemy(Normal, 1, 0.8)` 과 `AddEnemy(Boss, 1, 1.5)` 뒤 1칸 뒤: 일반 몬스터의 `Health` 는 20, 보스의 `Health` 는 100이고 `EnemyDamaged` 는 1번 온다 (범위 밖의 적은 맞지 않는다).
- `AddEnemy(Boss, 1, 1.0)` 뒤: 1칸 뒤 보스의 `Health` 는 90, `DiscipleHealth` 는 90이다. 450칸 뒤 보스의 `Health` 는 10, 451칸 뒤 0이고 `EnemyDefeated` 가 1번 오며 `DiscipleHealth` 는 50이다.
- `AddEnemy(Boss, 1, 5.0)` 뒤: 99칸 뒤 보스의 `Health` 는 100이고 `DiscipleHealth` 는 100, 100칸 뒤 보스의 `Health` 는 90이고 `DiscipleHealth` 는 90, `DisciplePosition` 은 4.0, 보스의 `Position` 은 5.0이다.
- `new StatUpgrades(wallet, 30, 100, 1.0)` 으로, `AddEnemy(Normal, 1, 0.8)` 과 `AddEnemy(Boss, 1, 2.0)` 뒤: 1칸 뒤 `Enemies.Count` 는 1, `DiscipleHealth` 는 95다. 26칸 뒤 `DisciplePosition` 은 1.0, 보스의 `Health` 는 100, `DiscipleAttackCooldown` 은 0.5, `DiscipleHealth` 는 85다. 50칸 뒤 보스의 `Health` 는 100, 51칸 뒤 70이다 (이동 중에도 대기가 흐르고, 남은 시간만큼 기다린다).
- `new StatUpgrades(wallet, 30, 100, 1.0)` 으로, `AddEnemy(Normal, 1, 0.8)` 과 `AddEnemy(Boss, 1, 4.0)` 뒤: 75칸 뒤 보스의 `Health` 는 100, 76칸 뒤 70이다 (도착했을 때 대기가 끝나 있으면 바로 공격한다).
- `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고 `upgrades.TryUpgrade(StatType.Attack)` 을 부르면, 처음부터 51칸 뒤 보스의 `Health` 는 78이다 (90 − 12).
- `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고 `upgrades.TryUpgrade(StatType.AttackSpeed)` 를 부르면, 처음부터 50칸 뒤 보스의 `Health` 는 90, 51칸 뒤 80, 98칸 뒤 80, 99칸 뒤 70이다 (기다리는 중인 공격은 그대로이고 다음 대기부터 0.95초다).
- `new StatUpgrades(wallet, 30, 5, 1.0)` 으로, `AddEnemy(Normal, 1, 0.8)` 뒤 1칸 뒤: `EnemyDefeated` 1번과 `DiscipleDefeated` 1번이 이 순서로 오고, `EnemyDefeated` 를 받는 시점에 읽은 `IsDiscipleDefeated` 는 `true`, `Enemies.Count` 는 0, 그 적의 `IsDead` 는 `true` 다. 끝난 뒤 `DiscipleHealth` 는 0, `Enemies.Count` 는 0이다.
- `new StatUpgrades(wallet, 100, 10, 1.0)` 으로, `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 뒤: 보스의 `EnemyDefeated` 를 받는 시점에 읽은 `IsDiscipleDefeated` 는 `true` 이고 `DiscipleDefeated` 가 1번 온다.
- `new StatUpgrades(wallet, 10, 5, 1.0)` 으로, `AddEnemy(Normal, 1, 0.8)` 뒤 1칸 뒤: `DiscipleHealth` 는 0, `IsDiscipleDefeated` 는 `true`, `DiscipleDefeated` 는 1번, 적의 `Health` 는 20이고 `Enemies.Count` 는 1이다. 이어서 `Step()` 100번과 `Tick(0.1)` 을 불러도 적의 `Health` 는 20, `DisciplePosition` 은 0이고 알림은 더 오지 않는다.
- 바로 위 조건의 패배 상태에서 `AddEnemy(Normal, 1, 5.0)` 은 예외 없이 추가되어 `EnemyAdded` 가 오고 `Enemies.Count` 는 2다. 이어서 `Reset()` 을 부르면 `IsDiscipleDefeated` 는 `false`, `DiscipleHealth` 는 5, `Enemies.Count` 는 0이고, 1칸 뒤 `DisciplePosition` 은 0.04다.
- `new StatUpgrades(wallet, 10, 12, 1.0)` 으로, `AddEnemy(Normal, 1, 0.8)` 을 3번 부른 뒤 1칸 뒤: `DiscipleHealth` 는 0 (음수가 아니다), `DiscipleDamaged` 는 3번, `DiscipleDefeated` 는 1번 온다.
- `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고(`DiscipleHealth` 90, `DiscipleAttackCooldown` 1.0) `Reset()` 을 부르면 `DisciplePosition` 은 0, `DiscipleHealth` 는 100, `DiscipleAttackCooldown` 은 0, `IsDiscipleDefeated` 는 `false`, `Enemies.Count` 는 0이고, `ResetCompleted` 는 1번, `EnemyDefeated` 는 0번 온다.
- 기존 테스트가 모두 통과한다. (`BattleTests`, `EnemyStatsTests`, `CombatLayoutTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다. `PlumSword.Core` 는 순수 C# 이다.
- `Battle`, `Enemy` 의 공개 모양을 바꾸거나 공개 멤버를 더하지 않는다.
- 체력 강화 때 현재 체력을 늘리는 처리(`upgrades.Upgraded` 구독)와, 알림 안에서 `AddEnemy`·`Reset`·`SetPaused` 를 다시 부르는 경우의 처리·테스트를 만들지 않는다. 010_battle_upgrade_reentry 가 한다.
- `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/` 와 `EnemyKind.cs`, `EnemyStats.cs`, `CombatLayout.cs` 를 고치지 않는다. 공개된 모양으로만 쓴다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. 011_combat_view 가 한다.
- 성공·실패 판정, 재도전, 금화 보상, 처치 시 체력 회복을 만들지 않는다. `Battle` 은 `Wallet` 을 쓰지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `Battle.Step` 에 3단계 "공격"(제자 → 적 순서)과 4단계 "쓰러짐 판정"을 채웠다.
  - 제자: 대기 ≤ Epsilon 이고 거리 ≤ 1.0 + Epsilon 인 적이 있으면 `upgrades.Attack` 을 한 번 읽어 범위 안의 모든 적에게 피해(0에서 멈춤)를 주고, 맞은 적마다 추가된 순서대로 `EnemyDamaged`, 그 뒤 대기를 `upgrades.AttackInterval` 로 정한다.
  - 적: 추가된 순서대로, 대기 ≤ Epsilon 이고 거리 ≤ `EnemyStats.AttackRange(Kind)` + Epsilon 이면 제자 체력을 줄이고(0에서 멈춤) `DiscipleDamaged`, 대기를 자기 `AttackInterval` 로 정한다. 이번 칸에 체력이 0이 된 적도 공격한다.
  - 쓰러짐 판정: 체력 0인 적을 `Enemies` 에서 빼고 `IsDead = true`, 제자 체력 0이면 `IsDiscipleDefeated = true` 로 모두 바꾼 뒤 `EnemyDefeated`(추가된 순서) → `DiscipleDefeated` 를 보낸다.
- 한 칸 동안 적 목록은 칸 시작 때 뜬 복사본(`snapshot`)을 돈다 (대기 감소·이동도 같은 복사본을 쓰도록 바꿨다). 010 의 재진입 처리를 위한 준비.
- `BattleAttackTests.cs` 를 새로 만들어 완료 조건을 모두 검사했다 (테스트 20개).

### 바뀐 파일
- `Assets/Scripts/Core/Combat/Battle.cs` — 공격·쓰러짐 판정 구현, 칸 단위 적 목록 복사본.
- `Assets/Tests/EditMode/Combat/BattleAttackTests.cs` (새 파일), `BattleAttackTests.cs.meta` (새 파일)
- `Assets/Tests/EditMode/Combat/BattleTests.cs` — `Step_WithEnemies_HealthDoesNotChange` 한 개를 `Step_WithEnemies_TradeHitsUntilEnemyDefeated` 로 바꿨다 (아래 가정 참고).
- `Enemy.cs` 와 사양 문서는 바꾸지 않았다 (공개 모양·동작이 사양 그대로다).

### 가정
- 008 이 만든 `BattleTests.Step_WithEnemies_HealthDoesNotChange` 는 "공격이 아직 없다"는 임시 상태(200칸 뒤 체력 그대로, 처치 0번)를 검사하던 테스트라, 이 작업의 요구사항(공격 구현)과 함께 통과할 수 없다. 지우거나 약하게 만들지 않고, 같은 설정(일반 몬스터 0.8, 200칸)에서 실제 결과를 더 엄격히 검사하도록 바꿨다: 제자 체력 90, 적 체력 0, `IsDead` true, 패배 아님, `EnemyDefeated` 1번. 이름도 내용에 맞게 `Step_WithEnemies_TradeHitsUntilEnemyDefeated` 로 바꿨다.
- 새 임시 수치는 없다. 모든 수치는 사양과 작업 문서의 값이다.
- 공개 멤버 추가·모양 변경 없음. 내부 도우미(`AttackByDisciple`, `AttackByEnemies`, `ResolveDefeats`, `ReduceHealth`)는 모두 private.

### 사람이 확인할 것
- `BattleTests.Step_WithEnemies_HealthDoesNotChange` 를 위 가정대로 바꾼 것이 괜찮은지.
- 완료 조건에 [사람 확인] 항목은 없다.

### 테스트 결과
- `python tools/loop/run_unity_tests.py --project .` → EditMode: 175개 중 175개 통과, 0개 실패 (`BattleAttackTests` 20개, 기존 `BattleTests`, `EnemyStatsTests`, `CombatLayoutTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함).


## 루프 기록 (2026-11-16 20:45)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/009_battle_attack
- 루프 브랜치: loop/20261116-164139
