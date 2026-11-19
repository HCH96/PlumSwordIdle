# 054_battle_skill_attack — 공격 차례에 이십사수매화검법 발동

## 목표
`PlumSword.Core.Combat.Battle`이 공격 차례에 스킬이 준비돼 있으면 기본 공격 대신 이십사수매화검법을 쓰고, 스킬 대기를 세고, `SkillUsed`를 보내게 한다. Core 작업이다.

## 묶음
combat_m3

## 선행 작업
- 053_battle_skill_state

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/combat.md ("동작"의 한 칸 순서·"스킬 켜기"·알림 안에서 전투를 다시 부를 때, "경계 상황", "수치", "연결 지점", "완료 조건 초안"의 49~62, "변경 기록")
- docs/spec/stat_upgrade.md ("연결 지점" 절)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-19 — 3단계: 이십사수매화검법, 돌파 때 현재 체력 증가, 공격 속도 제거 반영` 항목 중 **스킬 발동** 부분만 구현한다. 상수·`SkillEnabled`·`SkillCooldown`·`IsSkillReady`·`SkillDamage`·`CalculateSkillDamage`는 053이, 돌파 체력은 052가 이미 했다.
- `Assets/Scripts/Core/Combat/Battle.cs`:
  - 이벤트 `public event System.Action SkillUsed;`를 더한다.
  - **대기 감소** (한 칸의 1번): `SkillEnabled`이면 스킬 대기도 공격 대기와 같이 0.02 줄인다. 0보다 작아지지 않는다 (`Math.Max(0.0, …)`). 이동 중에도, 적이 없어도 줄어든다. 멈춘 동안·패배한 뒤에는 `Step`이 아무것도 하지 않으므로 줄지 않는다. `SkillEnabled`가 `false`면 스킬 대기는 늘 0이다.
  - **제자 공격** (한 칸의 3번): 공격 대기가 `1e-9` 이하이고 거리 `1.0 + 1e-9`(`DiscipleAttackRange`) 이내의 적이 하나라도 있으면 공격 차례다 (지금과 같다). 차례가 아니면 스킬이 준비돼 있어도 쓰지 않는다 (거리 1.0 밖·2.0 안에만 적이 있을 때도). 차례가 되면:
    - `IsSkillReady`가 `false`면 지금의 기본 공격 그대로다.
    - `IsSkillReady`가 `true`면 기본 공격 대신 스킬: `SkillDamage`를 한 번 읽어, 거리 `SkillRange + Epsilon`(2.0 + 1e-9) 이내의 모든 적(`적 좌표 − 제자 좌표`로 잰다)의 체력을 그만큼 줄인다 (0에서 멈춤). 적마다 `EnemyDamaged`를 추가된 순서대로 보낸 뒤 `SkillUsed`를 한 번 보낸다. 그 뒤 스킬 대기를 `SkillCooldownSeconds`(5.0)로, 공격 대기를 `DiscipleBaseAttackInterval`(1.0)로 정한다. 그래서 `EnemyDamaged`·`SkillUsed`를 받는 시점에 `SkillCooldown`은 아직 0이다.
  - 알림 안에서 `Reset`이 불리면 지금처럼 그 칸의 남은 처리를 그만둔다 (`_resetGeneration` 확인). `SkillUsed` 안에서 `Reset`을 부르면 스킬 대기·공격 대기를 정하지 않고, 적의 공격도 하지 않고 그 칸을 끝낸다.
  - 앞 동작 대기(달려와 멈춘 칸), 적의 공격, 쓰러짐 판정과 동시 쓰러짐은 바꾸지 않는다. 스킬 피해로 체력이 0이 된 적도 그 칸에 공격한다 (지금 규칙).
  - 내공·공격력을 강화해도 흐르는 스킬 대기는 바꾸지 않는다 (`SkillDamage`를 피해를 줄 때 읽으므로 따로 할 일은 없다).
  - `Reset()`은 053이 넣은 대로 스킬 대기를 0으로 되돌린다.
- 수치(참고, 지금 코드의 것): 한 칸 0.02초, 제자 이동 초당 6.48(한 칸 0.1296), 제자 공격 범위 1.0, 공격 간격 1.0초, 앞 동작 대기 `1.0 × 7/17`(21칸). 1스테이지 일반 몬스터 체력 30·공격력 5·공격 간격 2.0·범위 0.8, 보스 체력 100·공격력 10·공격 간격 2.0·범위 1.0이고 보스는 움직이지 않는다.
- 테스트에서 지갑은 `new Wallet(1000)`, 강화는 `new StatUpgrades(wallet, 10, 100)`이다. "스킬 전투"는 `new Battle(upgrades, null, true)`, "기본 전투"는 `new Battle(upgrades)`다. "n칸 뒤"는 처음부터 `Step()`을 모두 n번 부른 뒤다. 좌표·시간은 오차 1e-6 안에서 비교한다.
- 새 테스트는 `Assets/Tests/EditMode/Combat/BattleSkillTests.cs`(053이 만든 파일, 네임스페이스 `PlumSword.Core.Tests.Combat`)에 더한다. 053의 테스트는 지우지 않는다. 단 하나 `BattleSkillTests.EnabledSkill_StepAndReset_DoNotActivateSkillYet`만 이름은 그대로 두고 기대값을 바꾼다 (검사 수 4개는 줄이지 않는다): `Step()` 뒤 보스 체력 `enemy.MaxHealth - 10` → `enemy.MaxHealth - 20`, `Step()` 뒤 `SkillCooldown` `0.0` → `5.0`. `Reset()` 뒤의 `SkillCooldown` 0과 `IsSkillReady` `true` 검사는 그대로 둔다. 이름이 바뀐 내용과 맞지 않는 까닭은 테스트에 주석 한 줄로 남긴다 (053 때는 스킬이 아직 발동하지 않았다).
- 기존 테스트는 고치지 않는다. `skillEnabled` 기본값이 `false`라 기존 테스트는 그대로 통과해야 한다.
- 구현하다 연결 지점 모양이 사양과 달라지면 사양 문서 `docs/spec/combat.md`의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/Combat/BattleSkillTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 스킬 전투에서 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 뒤 보스의 `Health`는 80이고 `DiscipleHealth`는 90이다.
- 바로 위 1칸에서 `EnemyDamaged` 1번 다음에 `SkillUsed` 1번이 이 순서로 온다.
- 바로 위 1칸 뒤 `SkillCooldown`은 5.0, `IsSkillReady`는 `false`, `DiscipleAttackCooldown`은 1.0이다.
- 이어서 51칸 뒤 보스의 `Health`는 70이고 `SkillUsed`는 1번 그대로, `SkillCooldown`은 4.0이다.
- 이어서 250칸 뒤 보스의 `Health`는 40이고 `IsSkillReady`는 `false`다.
- 이어서 251칸 뒤 보스의 `Health`는 20이고 `SkillUsed`는 모두 2번이다.
- 스킬 전투에서 `AddEnemy(Normal, 1, 0.8)`, `AddEnemy(Boss, 1, 1.9)`, `AddEnemy(Boss, 1, 2.5)` 뒤 1칸 뒤 세 적의 `Health`는 10, 80, 100이고 `EnemyDamaged`는 2번(`Index` 0, 1 순서), `SkillUsed`는 1번 온다.
- 스킬 전투에서 `AddEnemy(Boss, 1, 1.5)` 뒤 1칸 뒤 보스의 `Health`는 100, `SkillUsed`는 0번, `IsSkillReady`는 `true`다.
- 바로 위에 이어서 4칸 뒤 `DisciplePosition`은 0.5, 24칸 뒤 보스의 `Health`는 100, 25칸 뒤 80이고 `SkillUsed`는 1번이다.
- 스킬 전투에서 `AddEnemy(Normal, 1, 0.8)`과 `AddEnemy(Boss, 1, 8.0)` 뒤 1칸 뒤 일반 몬스터의 `Health`는 10이고, 51칸 뒤 `Enemies.Count`는 1이다.
- 바로 위에 이어서 106칸 뒤 `DisciplePosition`은 7.0, 126칸 뒤 보스의 `Health`는 100, 127칸 뒤 90이다.
- 바로 위에 이어서 251칸 뒤 `IsSkillReady`는 `true`이고 보스의 `Health`는 70, 276칸 뒤 70, 277칸 뒤 50이고 `SkillUsed`는 모두 2번이다 (스킬 준비가 끝나도 다음 공격 차례까지 기다린다).
- 기본 전투에서 `AddEnemy(Boss, 1, 1.0)` 뒤 251칸 뒤 보스의 `Health`는 40, `SkillUsed`는 0번, `SkillCooldown`은 0이다.
- 스킬 전투를 `new StatUpgrades(wallet, 15, 5)`로 만들고 `AddEnemy(Normal, 1, 0.8)` 뒤 1칸 뒤 `EnemyDamaged` → `SkillUsed` → `EnemyDefeated` → `DiscipleDefeated`가 이 순서로 1번씩 온다.
- 바로 위 1칸에서 `EnemyDefeated`를 받는 시점에 읽은 `IsDiscipleDefeated`는 `true`다.
- 스킬 전투에서 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고 `upgrades.TryUpgrade(StatType.InnerPower)`를 부르면 `SkillCooldown`은 5.0 그대로이고, 251칸 뒤 보스의 `Health`는 19다.
- 스킬 전투에서 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고 `Reset()`을 부르면 `SkillCooldown`은 0, `IsSkillReady`는 `true`다.
- 바로 위에 이어서 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 더 진행하면 새 보스의 `Health`는 80이다.
- 스킬 전투에서 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고 `SetPaused(true)` 뒤 `Step()` 10번과 `Tick(0.1)`을 불러도 `SkillCooldown`은 5.0이다.
- 스킬 전투를 `new StatUpgrades(wallet, 10, 5)`로 만들고 `AddEnemy(Normal, 1, 0.8)` 뒤 1칸 뒤 `IsDiscipleDefeated`는 `true`, `SkillCooldown`은 5.0이고, 이어서 `Step()` 10번 뒤에도 5.0이다.
- 스킬 전투에서 적이 없을 때 50칸 뒤 `SkillCooldown`은 0, `IsSkillReady`는 `true`, `DisciplePosition`은 6.48이다.
- 스킬 전투에서 `SkillUsed` 안에서 보스의 `Health`와 `SkillCooldown`을 읽게 하고 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하면 읽은 값은 80과 0이다.
- 스킬 전투에서 `SkillUsed` 안에서 `Reset()`을 부르게 하고 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하면 예외 없이 `SkillCooldown`은 0, `DiscipleAttackCooldown`은 0, `DiscipleHealth`는 100, `Enemies.Count`는 0이다.
- 바로 위 1칸에서 `ResetCompleted`는 1번, `DiscipleDamaged`는 0번 온다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 053이 만든 공개 모양(상수, 생성자, `SkillEnabled`, `SkillCooldown`, `IsSkillReady`, `SkillDamage`, `CalculateSkillDamage`)은 바꾸지 않는다.
- 강화 기능의 코드 `Assets/Scripts/Core/StatUpgrade/`, 경지 기능의 코드 `Assets/Scripts/Core/Realm/`, 스테이지 기능의 코드 `Assets/Scripts/Core/Stage/`는 고치지 않는다.
- 화면 코드 `Assets/Scripts/Game/`과 `GameRoot`는 고치지 않는다. 055가 한다.
- 스킬 대기 저장은 하지 않는다.
- `UnityEngine`을 쓰지 않는다.

## 보고서

### 한 일
- 선행 작업의 스킬 테스트와 이번 작업의 완료 조건을 대조하고 충돌을 확인했다. 구현은 진행하지 못했다.

### 바뀐 파일
- `tasks/doing/054_battle_skill_attack.md`: 보류 보고서 추가.
- `.loop/result.json`: 보류 사유와 질문 기록.

### 가정
- 없음.

### 사람이 확인할 것
- `EnabledSkill_StepAndReset_DoNotActivateSkillYet` 테스트는 스킬 활성화 상태의 첫 공격이 기본 공격이고 스킬 대기가 0이어야 한다고 검사한다. 이번 작업은 같은 상황에 스킬 피해와 5초 대기를 요구하면서 053의 테스트를 고치지 말라고 한다. 테스트 변경 허용 여부를 결정해야 한다.

### 테스트 결과
- Unity 테스트는 직접 실행하지 않았다. 구현이 보류되어 루프 판정을 기다린다.


## 보류 사유 (2026-11-19 18:20)

- 사유: 053의 기존 테스트 EnabledSkill_StepAndReset_DoNotActivateSkillYet가 스킬 활성화 후 첫 공격의 기본 피해와 대기 0을 요구하여 054의 스킬 피해와 5초 대기 조건에 충돌한다. 작업 문서는 053 테스트를 고치지 말라고 명시한다.
- 질문: 기존 테스트를 스킬 발동 동작에 맞게 수정하도록 허용할까요?
- 추천안: 해당 테스트의 첫 공격 기대값을 스킬 피해와 5초 대기로 고치고 Reset 후 준비 상태 검사는 유지한다.
- 기능 브랜치: task/054_battle_skill_attack
- 작업 에이전트: codex(시도 1)


## 답변 (2026-11-19 18:29)

- 보류 종류: 에이전트의 질문 (2026-11-19 18:20 보류 사유에 대한 답)
- 결정: 053의 테스트 `BattleSkillTests.EnabledSkill_StepAndReset_DoNotActivateSkillYet`를 고치는 것을 허용한다. 이름은 그대로 두고 앞의 두 기대값만 스킬 발동 동작에 맞게 바꾼다.
- 이유: 053은 스킬 발동을 만들지 않는 작업이라 그때의 동작(첫 공격이 기본 공격)을 검사한 것이다. 이번 작업(054)이 사양대로 스킬을 발동시키면 그 기대값은 성립하지 않는 것이 맞다. 관문은 테스트 메서드가 사라지거나 검사 수가 줄면 합치지 않으므로, 이름과 검사 수는 유지한다.
- 할 일: `Assets/Tests/EditMode/Combat/BattleSkillTests.cs`의 `EnabledSkill_StepAndReset_DoNotActivateSkillYet`에서 `Assert.AreEqual(enemy.MaxHealth - 10, enemy.Health)`를 `Assert.AreEqual(enemy.MaxHealth - 20, enemy.Health)`로, 첫 번째 `Assert.AreEqual(0.0, battle.SkillCooldown)`을 `Assert.AreEqual(5.0, battle.SkillCooldown)`(오차 1e-6 허용 가능)으로 바꾼다. `Reset()` 뒤의 두 검사(`SkillCooldown` 0, `IsSkillReady` `true`)는 그대로 둔다. 이름이 내용과 맞지 않는 까닭을 주석 한 줄로 남긴다. 그 밖의 053 테스트는 고치지 않는다. 이어서 이 문서의 요구사항과 완료 조건대로 스킬 발동을 구현한다.
- 바꾼 칸: 요구사항의 "053의 테스트는 지우거나 고치지 않는다." → "053의 테스트는 지우지 않는다. 단 하나 `EnabledSkill_StepAndReset_DoNotActivateSkillYet`만 이름은 그대로 두고 기대값을 바꾼다 (보스 체력 `MaxHealth - 10` → `MaxHealth - 20`, `SkillCooldown` 0.0 → 5.0, `Reset` 뒤 검사 유지, 검사 수 유지)".
- 기능 브랜치: task/054_battle_skill_attack 에는 보류 보고서만 있고 구현은 없다. 처음부터 구현한다.

## 보고서

### 한 일
- 공격 차례에 준비된 스킬로 범위 내 적을 공격하고 순서대로 피해와 스킬 알림을 보낸 뒤 대기를 설정했다. 이동 중 대기 감소와 알림 중 초기화도 처리했다.
- 기존 지정 테스트의 기대값을 수정하고 완료 조건에 맞는 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/Combat/Battle.cs`: 스킬 공격, 대기 감소 및 이벤트.
- `Assets/Tests/EditMode/Combat/BattleSkillTests.cs`: 스킬 전투 테스트.
- `tasks/doing/054_battle_skill_attack.md`: 이 보고서 추가.
- `.loop/result.json`: 작업 결과.

### 가정
- 없음. 공개 연결 지점과 동작은 기존 전투 사양과 같다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- `git diff --check` 통과. Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프 판정을 기다린다.


## 루프 기록 (2026-11-19 18:36)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/054_battle_skill_attack
- 루프 브랜치: loop/20261119-183146
