# 078_battle_skill_hits — 스킬 피해를 13타로 나누고 스킬 중 제자리 유지

## 목표
Core(`PlumSword.Core.Combat.Battle`)의 이십사수매화검법을 한 번에 피해를 주는 방식에서, 정해진 칸마다 13번 나누어 때리는 방식으로 바꾼다. 스킬이 이어지는 동안 제자는 전진하지 않고 기본 공격도 하지 않는다.

## 묶음
skill_hits

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/combat.md ("동작"의 3. 공격, "경계 상황", "수치", "연결 지점"의 Battle, "완료 조건 초안"의 스킬 항목 46~57)

## 요구사항
- 사양(`docs/spec/combat.md`)에는 아직 이 변경이 없다. 아래가 새 규칙이고, 사양의 기존 스킬 규칙(발동 조건 `IsSkillReady`·공격 차례, 범위 2.0, 재사용 대기 5.0초, 피해 공식 `CalculateSkillDamage`)은 그대로다.
- **발동 칸(0번째 칸)**: 공격 차례에 스킬이 준비돼 있으면, 지금처럼 그 칸에서 `SkillDamage`를 **한 번 읽어 이번 스킬의 총 피해로 저장**한다. 그리고 `IsSkillActive`를 `true`로 만든 뒤 첫 타(0번 타)를 준다. 그다음 `SkillUsed`를 보내고, 스킬 대기를 5.0초, 공격 대기를 1.0초로 정한다. 알림 순서(`EnemyDamaged`들 → `SkillUsed`)와 "알림을 받을 때 `SkillCooldown`은 아직 0"이라는 점은 지금과 같다.
- **타 일정**: 발동 칸을 0번째 칸으로 세고, 아래 칸에서 차례로 0~12번 타를 준다.
  - `public static readonly IReadOnlyList<int> SkillHitSteps` = {0, 3, 5, 9, 13, 19, 23, 28, 36, 38, 42, 44, 46}
  - 이 값은 스킬 시트(24fps)의 검격 프레임 2, 3, 4, 6, 8, 11, 13, 15, 19, 20, 22, 23, 24를 프레임 2를 0으로 하여 칸(0.02초)으로 바꾼 값 `ceil((프레임 − 2) / 24 / 0.02)`이다.
  - `public const int SkillHitCount = 13;`
- **타 피해**: `public static long CalculateSkillHitDamage(long totalDamage, int hitIndex)`. 0~11번 타는 `totalDamage / 13`(정수 나눗셈), 12번 타는 `totalDamage − 12 × (totalDamage / 13)`이다. 그래서 13타의 합은 총 피해와 같다. `totalDamage`가 음수이거나 `hitIndex`가 0~12 밖이면 `ArgumentOutOfRangeException`을 던진다.
- **각 타의 대상**: 그 칸의 이동이 끝난 좌표에서 제자 앞 `SkillRange`(2.0) + `Epsilon` 안에 있는 모든 적이다 (칸 시작 때 떠 둔 적 목록 기준, 지금과 같음). 체력을 줄이고(0에서 멈춤) 추가된 순서대로 `EnemyDamaged`를 보낸다. 범위 안에 적이 없으면 그 타는 빗나간다 (알림 없음). 1번 타부터는 `SkillUsed`를 보내지 않는다. 쓰러짐 판정은 지금처럼 그 칸 끝에서 한다.
- **스킬이 이어지는 동안** (`IsSkillActive`가 `true`인 칸):
  - 제자는 전진하지 않는다 (2. 이동의 제자 이동을 건너뛴다). 적이 없어도 전진하지 않는다.
  - 기본 공격과 새 스킬 발동을 하지 않는다. 대기 감소(공격 대기·스킬 대기·적 대기)는 평소처럼 한다.
  - 적의 이동·공격, 종남 검기는 평소처럼 한다.
- **끝**: 발동 칸을 0번째로 세어 62번째 칸까지 `IsSkillActive`는 `true`다. 63번째 칸에서 `false`가 되고, 그 칸부터는 평소대로 이동·기본 공격을 한다. 즉 `public const int SkillActiveSteps = 63;`이고, 스킬 시트의 프레임 2~31(30프레임, 1.25초)에 맞춘 값이다.
- `public bool IsSkillActive { get; }`: 스킬이 꺼져 있으면 늘 `false`.
- `EnemyDamaged`를 받는 동안 `IsSkillActive`를 읽으면, 스킬 타(0번 타 포함)에서는 `true`, 기본 공격에서는 `false`다. 화면은 이것으로 스킬 피해 숫자를 가린다.
- `Reset()`은 `IsSkillActive`를 `false`로 만들고 남은 타를 모두 버린다. 멈춤(`Paused`)·패배 상태에서는 `Step`이 아무것도 하지 않으므로 타 진행도 멈춘다.
- 스킬이 이어지는 동안 강화로 `SkillDamage`가 바뀌어도, 이번 스킬의 남은 타는 발동 때 저장한 총 피해로 계산한다.
- **기존 테스트** (`Assets/Tests/EditMode/Combat/BattleSkillTests.cs`): 이름은 그대로 두고 아래 값만 바꾼다. 어서트 수는 줄이지 않는다. 적힌 것 외의 어서트는 바꾸지 않는다.
  - `EnabledSkill_StepAndReset_DoNotActivateSkillYet`: `enemy.MaxHealth - 20` → `enemy.MaxHealth - 1`.
  - `SkillAttack_Boss_CooldownsAndSubsequentTurns`: 첫 `Assert.AreEqual(80, boss.Health)` → 99. `Steps(battle, 50)` 뒤의 70 → 80. 마지막 `battle.Step();`을 `Steps(battle, 14);`로 바꾸고, 그 뒤의 20 → 39. 마지막 `skill` 개수 2는 그대로다.
  - `SkillAttack_AffectsAllEnemiesInRange_InOrder`: `first.Health` 10 → 29, `second.Health` 80 → 99.
  - `SkillReady_OutsideAttackRange_WaitsForWindUp`: 마지막 `boss.Health` 110 → 129.
  - `SkillCooldown_ExpiresDuringMovement_WaitsForNextAttack`: `normal.Health` 10 → 29. `Enemies.Count` 1 → 2. `DisciplePosition` 7.0 → 5.4432. 두 번째 `battle.Step()` 뒤의 120 → 130. `Steps(battle, 25)` → `Steps(battle, 38)`. 마지막 80 → 99. 나머지(130, `IsSkillReady` true, 100, 100, `used` 2)는 그대로다.
  - `SkillAttack_SimultaneousDefeat_NotifiesInOrder`: `SkillBattle(15, 5)` → `SkillBattle(200, 5)`. 어서트는 그대로다 (공격력 200이면 총 피해 400, 첫 타 30으로 일반 몬스터가 쓰러진다).
  - `InnerPowerUpgrade_PreservesCooldown_ChangesNextSkillDamage`: `Steps(battle, 250)` → `Steps(battle, 309)`. 기대값 19는 그대로다.
  - `Reset_AfterSkill_EnablesImmediateSkillAgain`: 80 → 99.
  - `SkillUsed_ObservesDamageBeforeCooldown`: `health` 80 → 99.
  - 위 값은 이 문서의 규칙으로 계산한 것이다. 규칙대로 구현했는데 값이 다르면, 규칙을 따른 값으로 고치고 보고서의 가정에 "테스트 기대값 정정"으로 적는다. 규칙을 바꿔서 위 값에 맞추지 않는다.
- 이 묶음에서 화면(`080`)은 `IsSkillActive`, `SkillHitSteps`, `SkillActiveSteps`를, 소리(`079`)는 `IsSkillActive`를 쓴다. 이름을 바꾸지 않는다.
- 끝나면 `docs/spec/combat.md`의 "동작"(3. 공격), "경계 상황", "수치", "연결 지점"(Battle), "완료 조건 초안"을 이 규칙으로 고친다. 구현하다 모양이 이 문서와 달라지면 사양 문서의 연결 지점 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/Combat/BattleSkillHitTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다. "스킬 전투"는 `new Battle(new StatUpgrades(new Wallet(1000), 10, 100), null, true)`이고, "n칸 뒤"는 처음부터 `Step()`을 n번 부른 뒤다.
- `Battle.CalculateSkillHitDamage(20, i)`는 i가 0~11이면 1이고, `(20, 12)`는 8이다.
- `Battle.CalculateSkillHitDamage(26, 0)`은 2이고 `(26, 12)`는 2다.
- `Battle.CalculateSkillHitDamage(12, 0)`은 0이고 `(12, 12)`는 12다.
- `Battle.CalculateSkillHitDamage(-1, 0)`, `(20, -1)`, `(20, 13)`은 각각 `ArgumentOutOfRangeException`을 던진다.
- `Battle.SkillHitSteps`는 {0, 3, 5, 9, 13, 19, 23, 28, 36, 38, 42, 44, 46}이고, `SkillHitCount`는 13, `SkillActiveSteps`는 63이다.
- 스킬 전투에서 `AddEnemy(Boss, 2, 1.0)`(체력 130, 움직이지 않음) 뒤 1칸 뒤 보스 `Health`는 129이고 3칸 뒤에도 129다.
- 같은 상황에서 4칸 뒤 보스 `Health`는 128이고 10칸 뒤 126이다.
- 같은 상황에서 47칸 뒤 보스 `Health`는 110이다 (13타 합 20).
- 같은 상황에서 63칸 뒤 보스 `Health`는 110이고 `IsSkillActive`는 `true`다.
- 같은 상황에서 64칸 뒤 보스 `Health`는 100(기본 공격)이고 `IsSkillActive`는 `false`다.
- 스킬 전투를 새로 만들면 `IsSkillActive`는 `false`다. 기본 전투(`skillEnabled` false)에서 `AddEnemy(Boss, 2, 1.0)` 뒤 64칸 동안 `IsSkillActive`는 한 번도 `true`가 아니다.
- 스킬 전투에서 보스와 일반 몬스터 순서로 `AddEnemy(Boss, 2, 1.0)`, `AddEnemy(Normal, 1, 2.6)` 뒤 47칸 뒤 보스 `Health`는 110이고 일반 몬스터 `Health`는 18이다 (일반 몬스터는 30번째 칸부터 범위 2.0에 들어와 36·38·42·44·46번째 칸의 5타만 맞는다).
- 공격력 200 스킬 전투(`new StatUpgrades(new Wallet(1000), 200, 100)`)에서 `AddEnemy(Normal, 1, 0.8)` 뒤 1칸 뒤 `Enemies.Count`는 0이다.
- 같은 상황에서 63칸 뒤 `DisciplePosition`은 0이고 `EnemyDamaged`는 모두 1번이다 (남은 타는 빗나가고 제자는 전진하지 않는다).
- 같은 상황에서 64칸 뒤 `DisciplePosition`은 0.1296이다.
- 스킬 전투에서 `AddEnemy(Boss, 2, 1.0)` 뒤 47칸 동안 받은 모든 `EnemyDamaged` 안에서 읽은 `IsSkillActive`는 `true`이고, 64칸째의 `EnemyDamaged` 안에서 읽은 값은 `false`다.
- 스킬 전투에서 `AddEnemy(Boss, 2, 1.0)` 뒤 1칸 진행하고 `SetPaused(true)` 후 `Step()`을 10번 부르면 보스 `Health`는 129이고 `IsSkillActive`는 `true`다.
- 스킬 전투에서 `AddEnemy(Boss, 2, 1.0)` 뒤 10칸 진행하고 `Reset()`을 부르면 `IsSkillActive`는 `false`다.
- 스킬 전투에서 `AddEnemy(Boss, 2, 1.0)` 뒤 1칸 진행하고 `upgrades.TryUpgrade(StatType.Attack)`을 불러도 47칸 뒤 보스 `Health`는 110이다.
- `BattleSkillTests`의 기존 테스트가 요구사항에 적은 값으로 모두 통과한다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 화면 코드(`Assets/Scripts/Game/`)와 소리 코드(`Assets/Scripts/Core/Audio/`)를 고치지 않는다. 각각 080, 079가 한다.
- 스킬 범위·재사용 대기·피해 공식·발동 조건을 바꾸지 않는다.
- `BattleSkillTests`의 테스트를 지우거나 이름을 바꾸지 않는다. 요구사항에 적지 않은 어서트를 바꾸지 않는다.
- `UnityEngine`을 쓰지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 스킬 발동 시 총 피해를 저장하고 13타 일정에 따라 범위 안 적에게 나누어 적용했다. 활성 중 제자의 전진과 공격을 막고 63번째 칸부터 재개한다.
- 초기화, 멈춤, 강화 및 알림 중 상태를 처리하고 기존 기대값과 신규 에딧 모드 테스트를 작성했다.
- 전투 사양에 타격 일정과 공개 연결 지점을 반영했다.

### 바뀐 파일
- `Assets/Scripts/Core/Combat/Battle.cs`
- `Assets/Tests/EditMode/Combat/BattleSkillTests.cs`
- `Assets/Tests/EditMode/Combat/BattleSkillHitTests.cs` 및 `.meta`
- `docs/spec/combat.md`
- `tasks/doing/078_battle_skill_hits.md`, `.loop/result.json`

### 가정
- 없음.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 테스트는 직접 실행하지 않았으며 루프 스크립트의 실행과 판정을 기다린다.
- `git diff --check` 통과.


## 루프 기록 (2026-11-22 13:24)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/078_battle_skill_hits
- 루프 브랜치: loop/20261122-131718
