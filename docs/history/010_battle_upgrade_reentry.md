# 010_battle_upgrade_reentry — 전투의 체력 강화 반영과 알림 안 재호출 처리

## 목표
`PlumSword.Core` 의 `Battle` 에 두 가지를 더해 전투 로직을 완성한다. (1) 체력 강화가 성공하면 제자의 현재 체력을 늘어난 최대 체력만큼 늘린다. (2) 알림을 받는 쪽이 그 안에서 `AddEnemy`, `SetPaused`, `Reset` 을 다시 불러도 예외 없이 정해진 대로 동작한다. 에딧 모드 테스트로 검사한다.

## 묶음
전투

## 선행 작업
- 001_wallet
- 004_stat_upgrades
- 009_battle_attack

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/combat.md ("동작" 절의 "강화 반영"과 "알림 안에서 전투를 다시 부를 때", "경계 상황", "연결 지점" 절)
- docs/spec/stat_upgrade.md ("연결 지점" 절의 `StatUpgrades.Upgraded`, `StatType`)

## 요구사항
- 고칠 파일: `Assets/Scripts/Core/Combat/Battle.cs` (네임스페이스 `PlumSword.Core.Combat`). 필요하면 `Assets/Scripts/Core/Combat/Enemy.cs`.
- 새 테스트 파일: `Assets/Tests/EditMode/Combat/BattleUpgradeTests.cs`. 네임스페이스 `PlumSword.Core.Tests.Combat`, 클래스 `BattleUpgradeTests`. 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다. 앞 작업의 `BattleTests.cs`, `BattleAttackTests.cs` 의 테스트는 지우거나 약하게 고치지 않는다.
- 선행 작업이 만든 것:
  - 009_battle_attack 까지: `Battle` 의 `Step()` 은 "대기 감소 → 이동 → 공격 → 쓰러짐 판정"을 모두 한다. 알림 `EnemyAdded`, `EnemyDamaged`, `DiscipleDamaged`, `EnemyDefeated`, `DiscipleDefeated`, `ResetCompleted` 가 모두 동작한다. 한 칸의 알림 순서는 맞은 적마다 `EnemyDamaged`(추가된 순서) → 공격한 적마다 `DiscipleDamaged` → 쓰러진 적마다 `EnemyDefeated`(추가된 순서) → 제자가 패배했으면 `DiscipleDefeated` 한 번이다. `EnemyDefeated` 를 보내기 전에 쓰러진 적은 이미 `Enemies` 에서 빠져 있고 `IsDiscipleDefeated` 는 이번 칸의 결과다.
  - 004_stat_upgrades: `PlumSword.Core.StatUpgrade.StatUpgrades` — `long MaxHealth { get; }`, `bool TryUpgrade(StatType stat)`, `event System.Action<StatType> Upgraded` (강화가 성공해 단계가 오른 뒤에 한 번, 오른 능력치를 담아 온다. 받는 시점에 `MaxHealth` 는 새 값이다). `PlumSword.Core.StatUpgrade.StatType` (`Attack`, `Health`, `AttackSpeed`). 체력 1단계 → `MaxHealth` +20. 강화 비용은 `20 + 5 × 단계` 다.
  - 001_wallet: `new Wallet(1000)` (테스트에서 `StatUpgrades` 를 만들 때 쓴다).
- 공개 모양은 바꾸지 않는다 (사양 `docs/spec/combat.md` 의 "연결 지점" 절 그대로다). 구현하다 모양이 사양과 달라지면 사양 문서의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- **체력 강화 반영**:
  - `Battle` 의 생성자에서 `upgrades.Upgraded` 를 구독한다. `Battle` 은 "직전 최대 체력"을 기억해 둔다 (생성자와 `Reset` 에서 `upgrades.MaxHealth` 로 맞춘다).
  - `Upgraded` 에서 `StatType.Health` 를 받으면 `늘어난 양 = upgrades.MaxHealth − 기억해 둔 직전 최대 체력` 을 구하고 기억을 새 값으로 바꾼다. 제자가 패배한 상태(`IsDiscipleDefeated`)가 아니면 현재 체력(`DiscipleHealth`)을 늘어난 양만큼 늘린다 (예: `40/100 → 60/120`).
  - 패배한 상태면 현재 체력은 0 그대로 두고 기억만 바꾼다.
  - 다른 능력치(`Attack`, `AttackSpeed`)의 `Upgraded` 에는 아무것도 하지 않는다.
  - 멈춘 동안(`Paused`)에도 체력 강화는 똑같이 반영한다. 이때 전투의 알림은 보내지 않는다.
  - 구독을 끊는 공개 메서드는 만들지 않는다 (게임에 `Battle` 과 `StatUpgrades` 는 하나씩이고 수명이 같다).
- **알림 안에서 전투를 다시 부를 때**: 알림을 받는 쪽이 그 안에서 `AddEnemy`, `SetPaused`, `Reset` 을 불러도 예외(`InvalidOperationException` 등) 없이 동작해야 한다. 한 칸 동안 도는 적 목록과 알림을 보낼 대상 목록은 미리 따로 떠 둔 것을 쓴다.
  - `AddEnemy`: 정상 추가되고 `EnemyAdded` 가 간다. 새 적은 **다음 칸부터** 움직이고 공격한다 (추가된 그 칸에서는 이동·공격·쓰러짐 판정의 대상이 아니다).
  - `SetPaused(true)`: 진행 중인 칸은 끝까지 처리하고 (남은 공격, 쓰러짐 판정, 남은 알림 모두), 다음 칸부터 멈춘다. `Tick` 이 여러 칸을 도는 중이었다면 그 칸까지만 한다.
  - `Reset`: 그 칸의 남은 처리(남은 공격, 대기 시간 설정, 쓰러짐 판정)와 남은 알림을 모두 그만둔다. `Reset` 이 만든 상태(제자 좌표 0, 체력 가득, 적 없음)가 그 칸이 끝난 뒤에도 그대로여야 한다. `Tick` 이 여러 칸을 도는 중이었다면 모아 둔 시간이 0이 되므로 더 진행하지 않는다. (구현 예: `Reset` 마다 1씩 느는 내부 번호를 두고, 알림을 하나 보낼 때마다 번호가 바뀌었으면 `Step` 을 바로 끝낸다.)
  - `ResetCompleted`, `EnemyAdded` 를 받는 쪽이 그 안에서 `AddEnemy` 를 부르는 것도 예외 없이 동작해야 한다 (스테이지 기능이 `Reset()` 뒤 `AddEnemy` 를 10번 부른다).
- 시간은 `Tick`/`Step` 으로만 흐른다. 난수와 저장은 쓰지 않는다.

## 완료 조건
아래에서 따로 적지 않으면 `wallet = new Wallet(1000)`, `upgrades = new StatUpgrades(wallet, 10, 100, 1.0)`, `battle = new Battle(upgrades)` 로 만든 것이다. "n칸 뒤"는 적을 추가한 뒤부터 `Step()` 을 모두 n번 부른 뒤다. 알림 횟수는 `Battle` 을 만든 직후 구독해 센 누적 횟수다. `Normal`, `Boss` 는 `EnemyKind` 의 값이다.

- `Assets/Tests/EditMode/Combat/BattleUpgradeTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고(`DiscipleHealth` 90) `upgrades.TryUpgrade(StatType.Health)` 를 부르면 `DiscipleHealth` 는 110, `DiscipleMaxHealth` 는 120이다. 이어서 `TryUpgrade(StatType.Attack)` 과 `TryUpgrade(StatType.AttackSpeed)` 를 불러도 `DiscipleHealth` 는 110 그대로다.
- `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고(`DiscipleHealth` 90) `upgrades.TryUpgrade(StatType.Health)` 를 2번 부르면 `DiscipleHealth` 는 130, `DiscipleMaxHealth` 는 140이다.
- 새로 만든 직후 `upgrades.TryUpgrade(StatType.Health)` 를 부르면 `DiscipleHealth` 는 120이다. 이어서 `Reset()` 을 부르면 `DiscipleHealth` 는 120이고, 한 번 더 `TryUpgrade(StatType.Health)` 를 부르면 140이다.
- `SetPaused(true)` 상태에서 `upgrades.TryUpgrade(StatType.Health)` 를 부르면 `DiscipleHealth` 는 120이다.
- `new StatUpgrades(wallet, 10, 5, 1.0)` 으로, `AddEnemy(Normal, 1, 0.8)` 뒤 1칸 뒤(`DiscipleHealth` 0, `IsDiscipleDefeated` `true`) `upgrades.TryUpgrade(StatType.Health)` 를 부르면 `DiscipleHealth` 는 0 그대로이고 `DiscipleMaxHealth` 는 25다. 이어서 `Reset()` 을 부르면 `DiscipleHealth` 는 25다.
- `new StatUpgrades(wallet, 30, 100, 1.0)` 으로, `AddEnemy(Normal, 1, 0.8)` 뒤 `EnemyDefeated` 안에서 `AddEnemy(Boss, 1, DisciplePosition + 5.0)` 을 부르게 하면, 1칸 뒤 예외 없이 `Enemies.Count` 는 1이고 그 적의 `Kind` 는 `Boss`, `Health` 는 100, `Index` 는 1이며 `EnemyAdded` 는 모두 2번이다.
- `new StatUpgrades(wallet, 30, 100, 1.0)` 으로, `AddEnemy(Normal, 1, 0.8)` 뒤 `EnemyDefeated` 안에서 `AddEnemy(Boss, 1, DisciplePosition + 1.0)` 을 부르게 하면: 1칸 뒤 보스의 `Health` 는 100이고 `DiscipleHealth` 는 95다 (새 적은 추가된 칸에 공격하지 않는다). 2칸 뒤 `DiscipleHealth` 는 85이고 보스의 `Health` 는 100이다. 50칸 뒤 보스의 `Health` 는 100, 51칸 뒤 70이다.
- `new StatUpgrades(wallet, 30, 5, 1.0)` 으로, `AddEnemy(Normal, 1, 0.8)` 뒤 `EnemyDefeated` 안에서 `Reset()` 을 부르게 하면, 1칸 뒤 예외 없이 `DiscipleDefeated` 는 0번 오고 `ResetCompleted` 는 1번, `IsDiscipleDefeated` 는 `false`, `DiscipleHealth` 는 5, `Enemies.Count` 는 0이다.
- `AddEnemy(Normal, 1, 0.8)` 을 3번 부른 뒤 `DiscipleDamaged` 안에서 `Reset()` 을 부르게 하면, 1칸 뒤 예외 없이 `DiscipleDamaged` 는 1번만 오고 `DiscipleHealth` 는 100, `Enemies.Count` 는 0, `DiscipleAttackCooldown` 은 0, `EnemyDefeated` 와 `DiscipleDefeated` 는 0번 온다.
- `AddEnemy(Normal, 1, 0.8)` 뒤 `EnemyDamaged` 안에서 `SetPaused(true)` 를 부르게 하면, 1칸 뒤 `Paused` 는 `true`, 적의 `Health` 는 20, `DiscipleHealth` 는 95이고 `DiscipleDamaged` 는 1번이다 (진행 중인 칸은 끝까지 처리한다). 이어서 `Step()` 을 5번 더 불러도 `DiscipleHealth` 는 95, 적의 `Health` 는 20이다.
- 적이 없을 때 `ResetCompleted` 안에서 `AddEnemy(Normal, 1, 5.0)` 을 부르게 하고 `Reset()` 을 부르면, 예외 없이 `Enemies.Count` 는 1이고 그 적의 `Index` 는 0이다.
- 기존 테스트가 모두 통과한다. (`BattleTests`, `BattleAttackTests`, `EnemyStatsTests`, `CombatLayoutTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다. `PlumSword.Core` 는 순수 C# 이다.
- `Battle`, `Enemy` 의 공개 모양을 바꾸거나 공개 멤버를 더하지 않는다.
- 009_battle_attack 까지 정해진 한 칸의 순서, 피해 계산, 알림 순서를 바꾸지 않는다.
- `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/` 와 `EnemyKind.cs`, `EnemyStats.cs`, `CombatLayout.cs` 를 고치지 않는다. `StatUpgrades` 는 공개된 모양으로만 쓴다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. 011_combat_view 가 한다.
- 경지 돌파 보너스, 스킬, 처치 시 체력 회복, 저장을 만들지 않는다. 사양의 "범위 밖"이다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `Battle` 생성자에서 `StatUpgrades.Upgraded` 를 구독하고, 직전 최대 체력(`_lastMaxHealth`)을 생성자와 `Reset` 에서 `upgrades.MaxHealth` 로 맞춘다. `StatType.Health` 를 받으면 늘어난 양을 구해 기억을 바꾸고, 패배한 상태가 아니면 현재 체력을 그만큼 늘린다. 멈춘 동안에도 반영하고 전투 알림은 보내지 않는다. 다른 능력치는 무시한다.
- 알림 안에서 다시 부를 때: `Reset` 마다 1씩 느는 내부 번호(`_resetGeneration`)를 두고, `Step` 이 `EnemyDamaged`·`DiscipleDamaged`·`EnemyDefeated` 를 하나 보낼 때마다 번호가 바뀌었으면 그 칸을 바로 끝낸다 (남은 공격, 대기 설정, 쓰러짐 판정, 남은 알림 모두 생략). `Tick` 은 `Reset` 이 모아 둔 시간을 0으로 만들어 더 진행하지 않는다.
- 한 칸 동안은 칸 시작 때 떠 둔 적 목록 복사본을 돌므로 알림 안에서 추가된 적은 다음 칸부터 움직이고 공격한다. `SetPaused(true)` 는 진행 중인 칸을 끝까지 처리하고 `Tick` 의 다음 칸부터 멈춘다 (기존 동작 그대로).
- 에딧 모드 테스트 `BattleUpgradeTests` 16개를 더했다.
- 사양 `docs/spec/combat.md` 의 결정 기록에 내부 번호 방식을 한 줄 더했다. 공개 모양(연결 지점)은 바뀌지 않았다.

### 바뀐 파일
- `Assets/Scripts/Core/Combat/Battle.cs` (수정)
- `Assets/Tests/EditMode/Combat/BattleUpgradeTests.cs` (새 파일)
- `Assets/Tests/EditMode/Combat/BattleUpgradeTests.cs.meta` (새 파일)
- `docs/spec/combat.md` (결정 기록 한 줄 추가)

### 가정
- `Reset` 으로 그 칸을 그만둘 때, 알림을 보낸 직후의 대기 설정도 하지 않는다: `EnemyDamaged` 안의 `Reset` 이면 제자의 다음 공격 대기를 정하지 않고(0 유지), `DiscipleDamaged` 안의 `Reset` 이면 그 적의 대기를 정하지 않는다 (그 적은 이미 목록에서 빠졌다). 009 의 "알림 → 대기 설정" 순서는 그대로 두었다.
- 이벤트 대리자는 C# 에서 불변이라 `Invoke` 시점의 구독자 목록이 그대로 쓰이므로, "알림 대상 목록을 따로 떠 두기"는 별도 복사 없이 이것으로 충족한다고 보았다.
- 알림 안에서 `Reset` 이 불린 뒤 같은 알림의 나머지 구독자는 그대로 그 알림을 받는다 (한 번의 `Invoke` 안을 끊을 수 없다). 끊는 것은 그다음 알림부터다.
- `Upgraded` 구독은 끊지 않는다 (작업 문서대로).

### 사람이 확인할 것
- 완료 조건에 [사람 확인] 항목은 없다.
- 없음

### 테스트 결과
- `python tools/loop/run_unity_tests.py --project .` → EditMode 191개 중 191개 통과, 0개 실패 (기존 `BattleTests`, `BattleAttackTests`, `EnemyStatsTests`, `CombatLayoutTests`, `CoreAssemblyTests` 포함).


## 루프 기록 (2026-11-16 20:48)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/010_battle_upgrade_reentry
- 루프 브랜치: loop/20261116-164139
