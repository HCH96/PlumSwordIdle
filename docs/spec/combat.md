# 전투 사양

- 기능 이름: combat (네임스페이스 `PlumSword.Core.Combat`)
- 작성일: 2026-11-16
- 고친 날: 2026-11-19

## 목표

제자와 몬스터가 외길 위에서 자동으로 싸우는 기능이다. 이 기능은 제자의 전진, 몬스터의 접근, 서로의 공격과 피해, 자동 발동 스킬 이십사수매화검법(발동 시점·피해·재사용 대기), 쓰러짐 판정과 알림, 체력 강화·경지 돌파 때 현재 체력 증가, 그리고 필드 화면(제자·몬스터·보스의 대체 표시, 머리 위 체력 바, 카메라, 임시 배경, 스킬의 임시 검기 띠)을 맡는다. 어떤 적을 어디에 놓을지, 성공·실패와 재도전, 금화 보상은 맡지 않는다 (스테이지 기능의 것). 경지 보너스가 들어간 공격력·최대 체력과 내공 단계는 강화 기능이, 경지 단계는 경지 기능이 정하고 전투는 읽기만 한다.

출처: `docs/design/milestone_1.md` "1단계: 전투", "1단계: 화면과 연출"의 "필드와 카메라", "기능 간 연결과 작업 분할 시 확인할 점". `docs/design/milestone_3.md` "목표와 현재 방향", "경지 돌파", "자동 발동 스킬", "3단계 화면".

## 동작

- `GameRoot`는 `Battle.EnemyDefeated` 알림마다 경지의 처치 수를 한 번 올린다. 일반 몬스터와 보스를 모두 세며 실제 돌파와 저장은 경지·저장 기능이 맡는다.

- 전투는 한 줄(진행 방향 좌표 하나) 위에서 일어난다. 제자는 좌표가 커지는 쪽으로 전진한다. 적은 항상 제자와 같거나 더 큰 좌표에 있다.
- **시간**: 전투는 0.02초(임시) 길이의 칸 단위로 진행한다. `Tick(deltaSeconds)`는 받은 시간을 모아 두었다가 모은 시간이 `0.02 - 1e-9` 이상인 동안 한 칸씩 진행(`Step`)하고 0.02를 뺀다 (0보다 작아지면 0). 남은 시간은 다음 `Tick`으로 넘긴다. `deltaSeconds`가 0.25(임시)보다 크면 0.25로 잘라낸다.
- **한 칸(`Step`)의 순서**. 멈춤 상태(`Paused`)이거나 제자가 패배한 상태(`IsDiscipleDefeated`)면 아무것도 하지 않는다.
  1. **대기 감소**: 제자와 모든 적의 공격 대기 시간을 0.02 줄인다. 스킬이 켜져 있으면(`SkillEnabled`) 스킬 대기(`SkillCooldown`)도 0.02 줄인다. 0보다 작아지지 않는다. 이동 중에도, 적이 없어도 줄어든다.
  2. **이동**:
     - 제자: 가장 가까운 적까지의 거리 `d`(적 좌표 − 제자 좌표의 최솟값)를 본다. 적이 없으면 `6.48 × 0.02`만큼 전진한다. `d ≤ 1.0 + 1e-9`이면 멈춘다. 아니면 `min(6.48 × 0.02, d − 1.0)`만큼 전진한다.
     - 일반 몬스터(추가된 순서대로, 제자가 움직인 뒤의 좌표로 계산): 제자까지의 거리 `d`가 `d ≤ 3.0 + 1e-9`이고 `d > 0.8 + 1e-9`이면 `min(1.0 × 0.02, d − 0.8)`만큼 제자 쪽으로 다가온다. 그 밖에는 움직이지 않는다.
     - 종남 장로(전체 스테이지 번호를 5로 나눈 나머지가 1인 보스)는 제자까지 3.0유닛 안에 들어오면 초당 1.0유닛으로 접근하고 1.0유닛에서 멈춘다. 다른 보스는 현재 제자리에 있는다. 보스별 이동 방식은 독립적으로 정한다 (수치 임시).
  2-1. **앞 동작 시작** (2026-11-17 추가): 이번 칸에 제자가 전진했고, 직전 칸이 끝났을 때는 제자의 공격 범위(`1.0 + 1e-9`) 안에 적이 없었는데 이동이 끝난 지금은 있으면(달려와 멈춘 칸), 제자의 공격 대기를 `max(지금 대기, Battle.DiscipleBaseAttackInterval × 7/17)`(1.0초면 0.4118초, 21칸)로 올린다 (`Battle.DiscipleAttackWindUpRatio`). 그래서 달려와 멈추자마자 피해를 주지 않고, 공격 애니메이션의 앞 동작(17프레임 중 0~6번)이 끝나는 순간 타격 프레임(7번)에 맞춰 피해를 준다. 제자가 전진하지 않은 채 범위 안에 적이 생긴 경우(처음부터 범위 안에 놓인 적, 적을 쓰러뜨린 뒤 다음 적이 이미 범위 안에 있는 경우)는 대기를 바꾸지 않는다. 범위 안 적 여부는 `Reset` 때 "없음"으로 되돌린다.
  2-2. **종남 투사체 이동**: 기존 눈꽃 검기는 한 칸에 0.12유닛 왼쪽으로 움직인다. 제자 좌표에 닿거나 지나면 투사체를 없애고 기록된 피해를 준 뒤 `DiscipleDamaged`를 보낸다. 발사된 칸에는 이동하지 않으며, 발사한 보스가 쓰러져도 검기는 남는다. `Reset`은 모두 없앤다.
  3. **공격** (이동이 끝난 좌표로 판정한다):
     - 제자: 대기 시간이 `1e-9` 이하이고 거리 `1.0 + 1e-9` 이내의 적이 하나라도 있으면 **공격 차례**다. 차례가 되면 스킬이 준비됐는지(`IsSkillReady`: 스킬이 켜져 있고 스킬 대기가 `1e-9` 이하) 보고 아래 둘 중 하나만 한다. 차례가 아니면 스킬이 준비돼 있어도 쓰지 않는다 (거리 1.0 밖·2.0 안에만 적이 있을 때도 그렇다).
       - **기본 공격** (스킬이 준비되지 않았을 때): `StatUpgrades.Attack`을 한 번 읽어, 거리 `1.0 + 1e-9` 이내의 모든 적의 체력을 그만큼 줄인다 (0에서 멈춤). 적마다 `EnemyDamaged`를 추가된 순서대로 보낸다. 그 뒤 공격 대기를 `Battle.DiscipleBaseAttackInterval`(1.0초)로 정한다.
       - **스킬 이십사수매화검법** (스킬이 준비됐을 때, 기본 공격 대신): `SkillDamage`를 한 번 읽어, 거리 `2.0 + 1e-9`(`Battle.SkillRange`) 이내의 모든 적의 체력을 그만큼 줄인다 (0에서 멈춤). 적마다 `EnemyDamaged`를 추가된 순서대로 보낸 뒤 `SkillUsed`를 한 번 보낸다. 그 뒤 스킬 대기를 `Battle.SkillCooldownSeconds`(5.0초)로, 공격 대기를 `Battle.DiscipleBaseAttackInterval`(1.0초)로 정한다. 그래서 `EnemyDamaged`와 `SkillUsed`를 받는 시점에 `SkillCooldown`은 아직 0이고 `DiscipleAttackCooldown`도 정해지기 전의 값이다.
     - 적(추가된 순서대로): 대기 시간이 `1e-9` 이하이고 제자까지의 거리가 자기 공격 범위(일반 0.8, 보스 1.0)에 `1e-9`를 더한 값 이하이면 공격한다. 종남 장로는 공격 횟수를 세어 세 번째마다 기본 공격 대신 눈꽃 검기를 자신의 좌표에서 발사한다. 검기는 공격 당시의 공격력만큼 적중 시 피해를 준다. 나머지 공격은 즉시 체력을 자기 공격력만큼 줄이고 `DiscipleDamaged`를 보낸다. 공격 대기는 모두 자기 공격 간격으로 정한다. **이번 칸에 제자의 공격으로 체력이 0이 된 적도 공격한다.**
  4. **쓰러짐 판정**: 체력이 0인 적을 모두 `Enemies`에서 빼고 `IsDead`를 `true`로 만든다. 제자의 체력이 0이면 `IsDiscipleDefeated`를 `true`로 만든다. 이 값들을 모두 바꾼 뒤에 알림을 보낸다: 쓰러진 적마다 `EnemyDefeated`(추가된 순서대로), 그다음 제자가 패배했으면 `DiscipleDefeated` 한 번.
- **동시에 쓰러짐**: 같은 칸에 제자와 적이 함께 쓰러지면 둘 다 처리된다. `EnemyDefeated`를 받는 시점에 `IsDiscipleDefeated`는 이미 `true`다. 이것으로 스테이지 기능이 동시 처치를 가려낸다. 전투는 성공·실패와 보상을 판정하지 않는다.
- **패배 뒤**: `Reset`이 불릴 때까지 `Step`과 `Tick`은 아무것도 하지 않는다. 남아 있는 적은 그대로 `Enemies`에 있다.
- **제자 쓰러짐 표시**: HP가 0이 되어 패배하면 선택된 남동쪽 쓰러짐 시트 17프레임을 0.075초 간격으로 한 번 재생하고 마지막 프레임을 유지한다. 리셋하면 대기 프레임으로 돌아간다. 일반 피격에는 별도 동작을 쓰지 않는다. 패배 전환 표시 시간은 [스테이지 사양](stage.md)을 따른다.
- **종남 상대 쓰러짐 표시**: 1스테이지의 남녀 제자와 장로가 패배하면 체력 바를 숨기고 해당 쓰러짐 17프레임을 0.075초 간격으로 한 번 재생한다. 전투가 멈춰도 화면 동작은 진행하며 마지막 프레임을 0.5초 유지한 뒤 없앤다. 전투 판정과 보상은 즉시 처리한다. 시트가 없으면 기존처럼 바로 없앤다.
- **적 추가** `AddEnemy(kind, stage, position)`: 능력치를 `EnemyStats`로 계산해 적을 만들고 `Enemies` 끝에 넣은 뒤 `EnemyAdded`를 보낸다. 새 적은 체력이 가득하고 공격 대기 시간이 0(바로 공격할 수 있음)이다. 순번 `Index`는 `Reset` 뒤 0부터, 추가할 때마다 1씩 는다.
- **초기화** `Reset()`: 제자 좌표 0, 제자 체력 = `StatUpgrades.MaxHealth`, 제자 공격 대기 0, 스킬 대기 0(준비됨), `IsDiscipleDefeated = false`, 모든 적과 종남 검기 제거(`EnemyDefeated`는 보내지 않는다), 다음 `Index` 0, 모아 둔 시간 0으로 만든 뒤 `ResetCompleted`를 한 번 보낸다. `Paused`는 바꾸지 않는다.
- **멈춤** `SetPaused(paused)`: 멈춘 동안 `Step`과 `Tick`은 아무것도 하지 않고, `Tick`이 받은 시간은 모으지 않고 버린다. 알림은 없다. 처음에는 멈춰 있지 않다. 멈춤을 거는 쪽은 스테이지 기능이다 (페이드 동안).
- **스킬 켜기**: 생성자의 `skillEnabled`가 `true`일 때만 스킬이 있다. 게임에서는 `GameRoot`가 `true`로 만든다. `false`(기본값)이면 스킬 대기가 줄지 않고(0 그대로), 공격 차례는 늘 기본 공격이며, `SkillUsed`가 오지 않는다 (1·2단계와 같은 전투). 처음 상태의 스킬 대기는 0(준비됨)이다. 별도 습득 과정은 없다.
- **강화·경지 반영** (`docs/spec/stat_upgrade.md`, `docs/spec/realm.md`의 약속):
  - 공격력: 기본 공격·스킬 피해를 줄 때마다 `StatUpgrades.Attack`(경지 보너스 포함)을 읽는다.
  - 공격 간격: 늘 `Battle.DiscipleBaseAttackInterval`(1.0초)이다. 강화·경지로 바뀌지 않는다.
  - 내공: 스킬 피해를 줄 때마다 `StatUpgrades.GetLevel(StatType.InnerPower)`를 읽는다 (`SkillDamage`). 내공·공격력을 강화해도 흐르는 스킬 대기는 바꾸지 않는다.
  - 체력: `StatUpgrades.Upgraded`에서 `StatType.Health`를 받으면 `늘어난 양 = StatUpgrades.MaxHealth − 기억해 둔 직전 최대 체력`을 구해 기억을 새 값으로 바꾼다. 제자가 패배한 상태가 아니면 현재 체력을 늘어난 양만큼 늘린다 (예: `40/100 → 60/120`). 다른 능력치(공격력, 내공)의 `Upgraded`에는 아무것도 하지 않는다.
  - 경지 돌파: 생성자에 `realm`을 받았으면 `realm.BrokeThrough`를 받을 때 체력 강화와 같은 처리를 한다: `늘어난 양 = StatUpgrades.MaxHealth − 기억해 둔 직전 최대 체력`, 기억을 새 값으로 바꾸고, 패배한 상태가 아니면 현재 체력을 그만큼 늘린다 (예: `90/100 → 100/110`). `BrokeThrough`를 받을 때 `realm.Current`는 이미 새 경지라 `MaxHealth`는 새 값이다. 멈춘 동안에도 반영한다. 알림은 없다. `realm`이 `null`이면 구독하지 않는다 (돌파해도 현재 체력은 그대로다).
- 몬스터를 처치해도 제자의 체력은 회복하지 않는다. 체력이 가득 차는 것은 `Reset`뿐이다.
- **알림 안에서 전투를 다시 부를 때**: 알림을 받는 쪽이 그 안에서 `AddEnemy`, `SetPaused`, `Reset`을 불러도 예외 없이 동작해야 한다 (보낼 대상 목록을 미리 따로 떠 둔다).
  - `AddEnemy`: 새 적은 다음 칸부터 움직이고 공격한다.
  - `SetPaused(true)`: 진행 중인 칸은 끝까지 처리하고, 다음 칸부터 멈춘다.
  - `Reset`: 그 칸의 남은 처리와 남은 알림을 모두 그만둔다. 예: `SkillUsed` 안에서 `Reset`을 부르면 스킬 대기·공격 대기를 정하지 않고 그 칸을 끝낸다 (적의 공격도 하지 않는다).
- 화면(`CombatView`)은 매 프레임 `Tick(Time.deltaTime)`을 부르고 전투 상태를 읽어 그린다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| 생성자의 `upgrades`가 `null` | `ArgumentNullException` | |
| `Tick(0)` | 아무 일도 없다 | 변화 없음 |
| `Tick(음수)`, `Tick(NaN)`, `Tick(무한대)` | `ArgumentOutOfRangeException` | 변화 없음 |
| `Tick(0.25보다 큰 값)` | 0.25로 잘라낸다 (예외 없음) | 예: `Tick(1.0)` → 12칸 진행, 0.01초 남김 |
| `Tick(0.02보다 작은 값)` | 모아 둔다 | 예: `Tick(0.01)` 두 번 → 1칸 |
| 멈춘 동안 `Step`, `Tick` | 아무 일도 없다 | 변화 없음. `Tick`의 시간은 버린다 |
| 제자가 패배한 뒤 `Step`, `Tick` | 아무 일도 없다 | 변화 없음, 알림 없음 |
| 적이 하나도 없음 | 제자가 계속 전진한다 | 칸마다 좌표 +0.1296 |
| 피해가 남은 체력보다 큼 | 체력 0에서 멈춘다 | 음수가 되지 않는다 |
| 같은 칸에 제자와 적이 함께 체력 0 | 둘 다 쓰러짐 처리 | `EnemyDefeated` 먼저, 그때 `IsDiscipleDefeated`는 `true`. 그 뒤 `DiscipleDefeated` 1번 |
| 체력이 0이 된 제자를 같은 칸에 다른 적이 또 공격 | 체력 0 그대로 | `DiscipleDamaged`는 공격한 적 수만큼 간다. `DiscipleDefeated`는 1번 |
| 범위 밖의 적 | 제자의 공격에 맞지 않는다 | 체력 그대로, `EnemyDamaged` 없음 |
| 패배한 상태에서 체력 강화 | 현재 체력을 늘리지 않는다 | 체력 0 그대로. 직전 최대 체력 기억만 바꾼다 |
| `AddEnemy`의 `stage`가 1보다 작음 | `ArgumentOutOfRangeException` | 변화 없음, 알림 없음 |
| `AddEnemy`의 `kind`가 정의되지 않은 값 (예: `(EnemyKind)99`) | `ArgumentOutOfRangeException` | 변화 없음, 알림 없음 |
| `AddEnemy`의 `position`이 `NaN`, 무한대, 또는 `DisciplePosition`보다 작음 (`DisciplePosition − 1e-9` 미만) | `ArgumentOutOfRangeException` | 변화 없음, 알림 없음 |
| `AddEnemy`의 `position`이 `DisciplePosition`과 같음 | 정상 추가 | |
| 멈춘 동안 또는 패배한 뒤 `AddEnemy` | 정상 추가 | `EnemyAdded` 1번 |
| `SetPaused`를 지금과 같은 값으로 부름 | 아무 일도 없다 | |
| `Reset`을 연달아 부름 | 부를 때마다 같은 결과 | `ResetCompleted`가 부른 횟수만큼 |
| 멈춘 동안 `Reset` | 정상 초기화 | `Paused`는 `true` 그대로 |
| `EnemyDefeated` 안에서 `AddEnemy` | 정상 추가, 다음 칸부터 행동 | |
| 알림 안에서 `Reset` | 그 칸의 남은 처리와 알림을 그만둔다 | 예: 동시 쓰러짐의 `EnemyDefeated` 안에서 `Reset` → `DiscipleDefeated` 0번 |
| `EnemyStats`의 `stage`가 1보다 작거나 `kind`가 정의되지 않음 | `ArgumentOutOfRangeException` | |
| `CombatLayout.EnemyOffsetY(음수)` | `ArgumentOutOfRangeException` | |
| `CombatLayout.HealthRatio`의 `maxHealth`가 0 이하 | 0.0 | |
| `CombatLayout.HealthRatio`의 `health`가 음수 / `maxHealth`보다 큼 | 0.0 / 1.0 으로 잘라낸다 | |
| 공격 차례에 스킬이 준비됨 | 기본 공격 대신 스킬 | 2.0 안 모든 적 −`SkillDamage`, `EnemyDamaged` 적 수만큼 → `SkillUsed` 1번, 스킬 대기 5.0, 공격 대기 1.0 |
| 스킬 대기가 끝났지만 공격 대기가 남음 | 다음 공격 차례까지 기다린다 | `IsSkillReady`는 `true` 그대로 |
| 거리 1.0 밖·2.0 안에만 적이 있음 | 공격 차례가 아니다. 스킬도 쓰지 않는다 | 제자는 계속 다가간다 |
| 거리 2.0 밖의 적 | 스킬에 맞지 않는다 | 체력 그대로, `EnemyDamaged` 없음 |
| 적이 없음 | 스킬 대기는 계속 줄고, 0이면 준비된 채 기다린다 | |
| 멈춘 동안, 패배한 뒤 | 스킬 대기도 줄지 않는다 | |
| `skillEnabled`가 `false` | 스킬이 없다 | `SkillCooldown` 0, `IsSkillReady` `false`, `SkillUsed` 없음 |
| 스킬 피해가 남은 체력보다 큼 | 체력 0에서 멈춘다 | |
| 패배한 상태에서 경지 돌파 | 현재 체력을 늘리지 않는다 | 체력 0 그대로. 직전 최대 체력 기억만 바꾼다 |
| 생성자의 `realm`이 `null` | 돌파를 구독하지 않는다 | 돌파해도 현재 체력 그대로 |
| `CalculateSkillDamage`의 `attack` 또는 `innerPowerLevel`이 음수 | `ArgumentOutOfRangeException` | |
| `CombatLayout.SkillSlashAlpha`의 시간이 음수 / `NaN` | 0.6 / 0.0 | |

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 체력·공격력의 타입 | `long` | 정수만 쓴다. 반올림 없음 |
| 좌표·거리·시간의 타입 | `double` (유닛, 초) | 반올림하지 않는다. 비교에는 허용 오차를 쓴다. 테스트는 오차 1e-6 안에서 비교한다 |
| 허용 오차 `Battle.Epsilon` | 1e-9 | 거리 `≤ 범위 + 1e-9`, 대기 `≤ 1e-9`이면 준비됨 |
| 한 칸의 길이 `Battle.StepSeconds` | 0.02초 (임시) | |
| 한 번에 받는 시간의 상한 `Battle.MaxTickSeconds` | 0.25초 (임시) | |
| 제자 시작 능력치 | 공격력 10, 최대 체력 100, 공격 간격 1.0초 (임시) | `Battle.DiscipleBaseAttack`, `DiscipleBaseMaxHealth`, `DiscipleBaseAttackInterval`. `GameRoot`가 `StatUpgrades`를 만들 때 공격력·최대 체력을 넣는다. 공격 간격은 전투가 직접 쓰고 강화로 바뀌지 않는다 |
| 제자 이동 속도 `Battle.DiscipleMoveSpeed` | 초당 6.48 유닛 (임시) | 한 칸에 0.1296. 사용자의 요청으로 기존 3.6에서 1.8배로 올렸다. 달리기 애니메이션 속도는 그대로다. |
| 제자 공격 범위 `Battle.DiscipleAttackRange` | 앞쪽 1.0 유닛 (임시) | 범위 안 모든 적에게 같은 피해 |
| 앞 동작 비율 `Battle.DiscipleAttackWindUpRatio` | 7/17 (임시) | 달려와 멈춘 칸에 대기를 `공격 간격 × 7/17` 이상으로 올린다. 간격 1.0초 → 0.4118초(21칸). 공격 애니메이션 17프레임 중 타격 프레임 7번에 맞춘 값이라 `DiscipleAnimator.AttackHitFrame / AttackFrameCount`와 같아야 한다 |
| 기본 공격 피해 | `StatUpgrades.Attack` | 경지 보너스 포함 |
| 스킬 범위 `Battle.SkillRange` | 앞쪽 2.0 유닛 (임시) | 범위 안 모든 적에게 같은 피해 |
| 스킬 재사용 대기 `Battle.SkillCooldownSeconds` | 5.0초 (임시) | 스킬을 쓴 칸부터 센다(250칸). 기본 공격 간격과 따로 센다 |
| 스킬 배율 `Battle.SkillBasePercent`, `Battle.SkillPercentPerInnerLevel` | 200%, 내공 한 단계당 +10%p (임시) | 정수 % (`int`) |
| 스킬 피해 `Battle.CalculateSkillDamage(attack, innerPowerLevel)` | `attack × (200 + 10 × innerPowerLevel) / 100` | `long`으로 계산하고 정수 나눗셈(내림). (10, 0) → 20, (10, 1) → 21, (13, 2) → 28, (11, 0) → 22. `SkillDamage`는 `StatUpgrades.Attack`과 `GetLevel(StatType.InnerPower)`를 넣은 값 |
| 검기 띠 | 가로 2.0 × 세로 0.4, RGB (1.0, 0.55, 0.75), 불투명도 0.6에서 0.25초 동안 0으로 (임시) | `CombatLayout.SkillSlashSeconds` = 0.25, `SkillSlashAlpha(t)` = `0.6 × (1 − t / 0.25)`를 0.0 ~ 0.6으로 잘라냄 |
| 일반 몬스터 반응 거리 `EnemyStats.NormalReactRange` | 3.0 유닛 (임시) | |
| 일반 몬스터 이동 속도 `EnemyStats.NormalMoveSpeed` | 초당 1.0 유닛 (임시) | 한 칸에 0.02 |
| 일반 몬스터 공격 범위 | 0.8 유닛 (임시) | `EnemyStats.AttackRange(Normal)` |
| 보스 공격 범위 | 1.0 유닛 (임시) | `EnemyStats.AttackRange(Boss)` |
| 종남 장로 이동 / 눈꽃 검기 | 감지 3.0, 이동 1.0유닛/초, 정지 거리 1.0 / 매 세 번째 공격, 속도 6.0유닛/초, 피해는 공격력과 같음 | 모두 임시. 다른 보스의 이동·기술과 별도 |
| 일반 몬스터 체력 | `30 + 10 × (stage − 1)` (임시) | 1·2·3스테이지 → 30·40·50 |
| 일반 몬스터 공격력 | `5 + 2 × (stage − 1)` (임시) | 1·2·3스테이지 → 5·7·9 |
| 보스 체력 | `100 + 30 × (stage − 1)` (임시) | 1·2·3스테이지 → 100·130·160 |
| 보스 공격력 | `10 + 4 × (stage − 1)` (임시) | 1·2·3스테이지 → 10·14·18 |
| 일반 몬스터·보스 공격 간격 | 2.0초 (임시) | 스테이지가 올라도 같다 |
| 능력치 계산 | `long`으로 한다 (`stage`를 `long`으로 바꾼 뒤 곱한다) | |
| 겹침 표시 `CombatLayout.EnemyOffsetY(index)` | `((index % 3) − 1) × 0.15` (임시) | 0·1·2·3·4 → −0.15·0·0.15·−0.15·0. 일반 몬스터에만 쓴다. 거리 계산에는 넣지 않는다 |
| 체력 바 비율 `CombatLayout.HealthRatio(health, maxHealth)` | `health / maxHealth` (`double`로 나눔), 0.0 ~ 1.0으로 잘라냄 | 50/100 → 0.5, 0/100 → 0.0 |
| 번쩍임 시간 | 0.1초 (임시) | 화면 코드의 값 |

몬스터를 몇 마리, 어디에 놓는지와 처치 금화는 이 기능의 수치가 아니다 ("범위 밖").

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.Combat
{
    public enum EnemyKind
    {
        Normal = 0,
        Boss = 1,
    }

    public static class EnemyStats
    {
        public const double NormalReactRange = 3.0;
        public const double NormalMoveSpeed = 1.0;

        // Normal: 30 + 10 * (stage - 1), Boss: 100 + 30 * (stage - 1). long 으로 계산.
        // stage 가 1 보다 작거나 kind 가 정의되지 않은 값이면 ArgumentOutOfRangeException (아래 stage, kind 를 받는 메서드 모두 같다).
        public static long MaxHealth(EnemyKind kind, int stage);

        // Normal: 5 + 2 * (stage - 1), Boss: 10 + 4 * (stage - 1).
        public static long Attack(EnemyKind kind, int stage);

        // Normal, Boss 모두 2.0 (초).
        public static double AttackInterval(EnemyKind kind);

        // Normal: 0.8, Boss: 1.0 (유닛).
        public static double AttackRange(EnemyKind kind);
    }

    // 적 하나. Battle.AddEnemy 만 만든다 (생성자는 internal). 값은 Battle 만 바꾼다.
    public sealed class Enemy
    {
        public EnemyKind Kind { get; }
        public int Index { get; }                 // Reset 뒤 0 부터, AddEnemy 마다 1 씩 는다
        public double Position { get; }           // 진행 방향 좌표
        public long MaxHealth { get; }
        public long Health { get; }               // 0 이상 MaxHealth 이하
        public long Attack { get; }
        public double AttackInterval { get; }
        public double AttackCooldown { get; }     // 다음 공격까지 남은 시간(초). 0 이면 바로 공격할 수 있다
        public bool IsDead { get; }               // 쓰러짐 판정에서 Enemies 에서 빠질 때 true
        public bool IsJongnamBoss { get; }        // 종남 테마의 보스
        public int AttackCount { get; }           // 이 적의 공격 횟수. 종남은 세 번째마다 검기
    }

    public sealed class BossProjectile
    {
        public double Position { get; }
        public long Damage { get; }
    }

    public sealed class Battle
    {
        public const double StepSeconds = 0.02;
        public const double MaxTickSeconds = 0.25;
        public const double Epsilon = 1e-9;
        public const double BossProjectileSpeed = 6.0;
        public const long DiscipleBaseAttack = 10;
        public const long DiscipleBaseMaxHealth = 100;
        public const double DiscipleBaseAttackInterval = 1.0;
        public const double DiscipleMoveSpeed = 6.48;
        public const double DiscipleAttackRange = 1.0;
        public const double DiscipleAttackWindUpRatio = 7.0 / 17.0;   // 달려와 멈춘 칸의 앞 동작 대기 = 공격 간격 × 이 값
        public const double SkillRange = 2.0;                 // 스킬 범위 (유닛)
        public const double SkillCooldownSeconds = 5.0;       // 스킬 재사용 대기 (초)
        public const int SkillBasePercent = 200;              // 내공 0단계의 스킬 배율 (%)
        public const int SkillPercentPerInnerLevel = 10;      // 내공 한 단계당 더하는 배율 (%p)

        // upgrades: 제자의 공격력·최대 체력·내공 단계를 읽을 곳. null 이면 ArgumentNullException.
        // realm: null 이 아니면 realm.BrokeThrough 를 구독해 돌파 때 현재 체력을 늘린다. null 이면 구독하지 않는다.
        // skillEnabled: true 면 이십사수매화검법을 쓴다. false(기본값)면 기본 공격만 한다.
        // upgrades.Upgraded 를 구독한다. 시작 상태: 제자 좌표 0, 체력 = upgrades.MaxHealth,
        // 공격 대기 0, 스킬 대기 0, 적 없음, 멈추지 않음, 패배 아님. 알림을 보내지 않는다.
        public Battle(PlumSword.Core.StatUpgrade.StatUpgrades upgrades,
            PlumSword.Core.Realm.RealmProgress realm = null, bool skillEnabled = false);

        public double DisciplePosition { get; }
        public long DiscipleHealth { get; }             // 0 이상
        public long DiscipleMaxHealth { get; }          // upgrades.MaxHealth 그대로
        public double DiscipleAttackCooldown { get; }   // 다음 공격까지 남은 시간(초)
        public double DiscipleAttackInterval { get; }   // 늘 DiscipleBaseAttackInterval (1.0). 화면의 공격 애니메이션 속도에 쓴다
        public bool IsDiscipleDefeated { get; }
        public bool Paused { get; }

        public bool SkillEnabled { get; }               // 생성자의 skillEnabled
        public double SkillCooldown { get; }            // 스킬을 다시 쓸 수 있을 때까지 남은 시간(초). 0 이상. 스킬이 꺼져 있으면 늘 0
        public bool IsSkillReady { get; }               // SkillEnabled 이고 SkillCooldown <= Epsilon
        public long SkillDamage { get; }                // CalculateSkillDamage(upgrades.Attack, upgrades.GetLevel(StatType.InnerPower)). 읽을 때마다 계산

        // attack × (SkillBasePercent + SkillPercentPerInnerLevel × innerPowerLevel) / 100 을 long 정수 나눗셈(내림)으로.
        // attack 또는 innerPowerLevel 이 음수면 ArgumentOutOfRangeException.
        public static long CalculateSkillDamage(long attack, int innerPowerLevel);

        // 살아 있는 적. 추가된 순서. 쓰러진 적은 쓰러짐 판정에서 빠진다.
        public System.Collections.Generic.IReadOnlyList<Enemy> Enemies { get; }
        public System.Collections.Generic.IReadOnlyList<BossProjectile> BossProjectiles { get; }

        // 시간을 받아 모으고, 0.02 초마다 Step 을 한 번씩 부른다. 남은 시간은 다음으로 넘긴다.
        // 0: 아무 일도 없다. 음수·NaN·무한대: ArgumentOutOfRangeException. 0.25 보다 크면 0.25 로 잘라낸다.
        // 멈췄거나 패배한 상태면 아무것도 하지 않고 시간도 모으지 않는다.
        public void Tick(double deltaSeconds);

        // 정확히 한 칸(0.02 초) 진행한다: 대기 감소(스킬 대기 포함) -> 이동 -> 앞 동작 시작 -> 기존 보스 투사체 이동·적중 -> 공격 -> 쓰러짐 판정.
        // 멈췄거나 패배한 상태면 아무것도 하지 않는다.
        public void Step();

        // 적을 추가하고 EnemyAdded 를 보낸 뒤 그 적을 돌려준다. 능력치는 EnemyStats 로 계산한다.
        // stage < 1, 정의되지 않은 kind, position 이 NaN·무한대이거나 DisciplePosition - Epsilon 보다 작으면
        // ArgumentOutOfRangeException (아무것도 바꾸지 않는다). 멈췄거나 패배한 상태에서도 추가된다.
        // (Epsilon 허용: 칸마다 0.1296 을 더한 좌표의 소수 오차 때문에 "제자와 같은 좌표" 가 거부되지 않게 한다.)
        public Enemy AddEnemy(EnemyKind kind, int stage, double position);

        // 제자를 좌표 0, 체력 가득, 공격 대기 0, 스킬 대기 0, 패배 아님으로 되돌리고 모든 적을 없앤 뒤
        // ResetCompleted 를 보낸다. EnemyDefeated 는 보내지 않는다. Paused 는 바꾸지 않는다.
        public void Reset();

        // 멈추거나 푼다. 같은 값이면 아무 일도 없다. 알림은 없다.
        public void SetPaused(bool paused);

        public event System.Action<Enemy> EnemyAdded;      // AddEnemy 가 적을 넣은 뒤
        public event System.Action<Enemy> EnemyDamaged;    // 제자의 공격으로 그 적의 체력이 준 뒤
        public event System.Action DiscipleDamaged;        // 적의 공격 한 번마다, 제자의 체력을 줄인 뒤
        public event System.Action<Enemy> EnemyDefeated;   // 쓰러짐 판정에서. 이때 IsDiscipleDefeated 는 이미 이번 칸의 결과다
        public event System.Action DiscipleDefeated;       // 쓰러짐 판정에서, EnemyDefeated 들 다음에 한 번
        public event System.Action ResetCompleted;         // Reset 이 끝난 뒤
        public event System.Action SkillUsed;              // 스킬 피해의 EnemyDamaged 들 다음에 한 번. 이때 SkillCooldown 은 아직 0
    }

    public static class CombatLayout
    {
        // 겹친 일반 몬스터의 화면상 위아래 어긋남. ((index % 3) - 1) * 0.15.
        // 예: 0 -> -0.15, 1 -> 0, 2 -> 0.15, 3 -> -0.15. 음수: ArgumentOutOfRangeException.
        public static double EnemyOffsetY(int index);

        // 체력 바가 찬 비율. (double)health / maxHealth 를 0.0 ~ 1.0 으로 잘라낸다. maxHealth <= 0 이면 0.0.
        public static double HealthRatio(long health, long maxHealth);

        public const double SkillSlashSeconds = 0.25;   // 검기 띠가 보이는 시간(초)

        // 검기 띠의 불투명도. 0.6 * (1 - elapsedSeconds / 0.25) 를 0.0 ~ 0.6 으로 잘라낸다.
        // 예: 0 -> 0.6, 0.125 -> 0.3, 0.25 이상 -> 0.0. 음수 -> 0.6, NaN -> 0.0.
        public static double SkillSlashAlpha(double elapsedSeconds);
    }
}
```

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래 하나를 더한다
    {
        // Awake 에서 Realm, StatUpgrades 를 만든 뒤 new Battle(StatUpgrades, Realm, true) 로 하나 만든다 (스킬 켜짐).
        public PlumSword.Core.Combat.Battle Battle { get; }   // private set
    }
}

namespace PlumSword.Core.Combat
{
    // 제자 스프라이트가 재생하는 동작.
    public enum DiscipleMotion
    {
        Idle = 0,
        Run = 1,
        Attack = 2,
    }

    // 제자 스프라이트의 동작과 프레임 번호를 고르는 표시 규칙. 거리·피해 계산에는 쓰지 않는다. 규칙은 "화면"의 "제자 애니메이션".
    public sealed class DiscipleAnimator
    {
        public const int IdleFrameCount = 24;
        public const int RunFrameCount = 24;
        public const int AttackFrameCount = 17;
        public const int AttackHitFrame = 7;          // 검기가 처음 보이는 프레임. 피해를 주는 순간과 겹친다
        public const int AttackSlashFrameCount = 3;   // 검기가 보이는 프레임 수 (7~9)
        public const double IdleFramesPerSecond = 10.0;
        public const double RunFramesPerSecond = 20.0;
        public const double MoveThreshold = 1e-6;     // 한 프레임에 이보다 많이 움직이면 "움직임"
        public const double RunGraceSeconds = 0.1;    // 이만큼 움직이지 않아야 달리기를 멈춘다

        public DiscipleMotion Motion { get; }   // 처음 Idle
        public int Frame { get; }               // 처음 0. 0 이상 그 동작의 프레임 수 미만

        // AttackFrameCount / attackInterval. 0 이하·NaN·무한대면 ArgumentOutOfRangeException.
        public static double AttackFramesPerSecond(double attackInterval);

        // 한 프레임 진행한다. deltaSeconds 가 음수·NaN 이면 0 으로 본다. attackInterval 이 잘못되면 위와 같은 예외.
        public void Update(double deltaSeconds, bool moved, bool attacked, bool engaged, double cooldown, double attackInterval);

        // Idle 0번 프레임으로 되돌린다.
        public void Reset();
    }
}

namespace PlumSword.Game.Combat
{
    public sealed class CombatView : MonoBehaviour
    {
        // 제자·배경 오브젝트를 만들고 카메라를 설정한다. battle 의 알림을 구독하고 OnDestroy 에서 끊는다.
        // Update 에서 battle.Tick(Time.deltaTime) 을 부른 뒤 화면을 맞춘다.
        // battle, camera 가 null 이면 ArgumentNullException. 두 번 부르면 InvalidOperationException.
        public void Initialize(PlumSword.Core.Combat.Battle battle, UnityEngine.Camera camera);
    }

    // 스테이지 기능이 생기기 전까지만 쓰는 임시 적 배치. 스테이지 기능을 구현할 때 이 클래스와 GameRoot 의 조립 줄을 지운다.
    public sealed class TempEncounter : MonoBehaviour
    {
        // 바로 적을 한 번 배치한다. battle.EnemyDefeated, battle.DiscipleDefeated 를 구독하고 OnDestroy 에서 끊는다.
        // battle 이 null 이면 ArgumentNullException. 두 번 부르면 InvalidOperationException.
        public void Initialize(PlumSword.Core.Combat.Battle battle);
    }
}
```

- `TempEncounter`는 스테이지 기능 구현(019_stage_view)으로 지워졌다. 적 배치는 `StageProgress`가 한다 (`docs/spec/stage.md`).

- 누가 만들고 누가 들고 있는지: `GameRoot.Awake`가 `StatUpgrades`를 만든 뒤 `Battle`을 하나 만들어 `GameRoot.Battle`로 들고 있는다. 게임에 `Battle`은 하나뿐이다. `GameRoot`는 `StatUpgrades`를 만들 때 시작 능력치로 `Battle.DiscipleBaseAttack, Battle.DiscipleBaseMaxHealth`를 넣고, `Battle`은 `new Battle(StatUpgrades, Realm, true)`로 만든다 (`Realm`을 먼저 만든다).
- `Tick`을 부르는 것은 `CombatView` 하나뿐이다. 다른 기능은 `Tick`과 `Step`을 부르지 않는다.
- 스테이지 기능이 쓰는 방법:
  - 스테이지 시작·재도전: 화면이 가려진 동안 `Reset()` 뒤 `AddEnemy(EnemyKind.Normal, 스테이지 번호, 좌표)`를 10번.
  - 처치 집계와 보상: `EnemyDefeated`를 받아 `Kind`로 일반·보스를 가린다. 보스의 `EnemyDefeated`에서 `IsDiscipleDefeated`가 `true`면 동시 처치(실패, 보스 금화 없음)다.
  - 보스 등장: 열 번째 `EnemyDefeated` 안에서 `AddEnemy(EnemyKind.Boss, 스테이지 번호, DisciplePosition + 거리)`를 부를 수 있다.
  - 실패: `DiscipleDefeated`.
  - 페이드: 시작할 때 `SetPaused(true)`, 끝나면 `SetPaused(false)`.
  - 보스 체력 바: 보스 `Enemy`의 `Health`, `MaxHealth`와 `EnemyDamaged`.
- 2단계 저장 기능이 쓰는 방법: 전투에는 저장할 값이 없다. 불러온 뒤 스테이지 기능이 `Reset()`으로 시작한다.

### 필요한 것

- `PlumSword.Core.StatUpgrade.StatUpgrades` (`docs/spec/stat_upgrade.md`): `Attack`, `MaxHealth`, `GetLevel(StatType.InnerPower)`, `Upgraded`. 구현되어 있다.
- `PlumSword.Core.StatUpgrade.StatType.Health`, `StatType.InnerPower` (`docs/spec/stat_upgrade.md`).
- `PlumSword.Core.Realm.RealmProgress.BrokeThrough` (`docs/spec/realm.md`). 구현되어 있다.
- `PlumSword.Game.GameRoot.StatUpgrades`, `GameRoot.Realm`.
- 적 배치는 스테이지 기능(`docs/spec/stage.md`)이 한다.

## 화면

화면 위쪽의 자동 전투 영역이다. UI 캔버스가 아니라 월드 공간에 `SpriteRenderer`로 그린다. 좌표 단위는 유닛이다.

- **조립**: `GameRoot.Awake`에서 `Battle`을 만든 다음 `[Game]`의 자식 `[CombatView]`를 만들어 `CombatView`를 붙이고 `Initialize(Battle, 카메라)`를 부른다. 카메라는 `Camera.main`을 쓰고, 없으면 `[Game]`의 자식 `[Camera]`를 만들어 `Camera`를 붙이고 `MainCamera` 태그를 단다. 그 뒤 `[Game]`의 자식 `[TempEncounter]`를 만들어 `TempEncounter`를 붙이고 `Initialize(Battle)`를 부른다.
  - `[TempEncounter]`는 스테이지 기능 구현(019_stage_view)으로 지워졌다. 적 배치는 `StageProgress`가 한다 (`docs/spec/stage.md`).
- **대체 표시 만드는 법**: 제자와 1스테이지 일반 상대는 아래 스프라이트를 쓰고, 에셋이 없거나 아직 적용하지 않은 대상은 대체 표시를 쓴다. 1×1 흰색 `Texture2D` 스프라이트를 사각형들이 공유하고, 크기는 `localScale`, 색은 `SpriteRenderer.color`로 정한다. 캐릭터의 좌표는 발 아래 가운데가 기준이다.
- **카메라**: Orthographic, 크기 5.625 (`docs/dev/art.md`). URP 2D의 Pixel Perfect Camera를 붙인다 (기준 해상도 640×360, PPU 32, Grid Snapping = Pixel Snapping, Crop Frame = None, 2026-11-17). 카메라 크기는 이 컴포넌트가 화면 높이의 정수배가 되게 정하고(1280×720·1920×1080에서는 5.625 그대로), 카메라와 스프라이트를 픽셀 격자에 맞춰 그려 소수 좌표 때문에 떨려 보이지 않게 한다. 남는 폭·높이는 잘라내지 않고 더 보여 준다. 배경색 RGB (0.55, 0.75, 0.90) (임시), 단색으로 지움. 위치는 매 프레임 `(DisciplePosition + 4.0, 0, −10)` (임시). 제자는 화면 가운데보다 왼쪽에 고정되어 보인다. `Reset`으로 제자가 좌표 0으로 돌아가면 카메라도 바로 따라간다.
- **땅 높이**: 전투 좌표는 y = 0 (임시). 표시할 때는 캐릭터 공통 오프셋을 적용한다.
- **제자**: 전투 위치는 `(DisciplePosition, 0)`, 표시 위치는 임시 연무장 배치에 맞춰 여기서 왼쪽 2.25·아래 1.5 유닛 이동한다. 그리는 순서(`sortingOrder`)는 10000. 아래 "제자 스프라이트"를 불러오면 스프라이트로, 하나라도 불러오지 못하면 1.5×1.5 사각형 RGB (0.25, 0.50, 0.90) 대체 표시로 그린다 (모두 임시).
- **제자 스프라이트** (`PlumSword.Game.Combat.DiscipleSprites`, 2026-11-17 적용):
  - `Resources.Load<Texture2D>`로 `Art/Characters/disciple_idle`, `disciple_run`, `disciple_attack` 세 시트를 읽고, 가로 한 줄 시트를 칸마다 `Sprite.Create`로 자른다 (PPU 32, 화면에서 임시 1.5배). 칸 크기와 피벗(칸 왼쪽 아래에서 잰 px)은 대기 76×76·(38, 12), 달리기 76×80·(38, 14), 공격 76×76·(38, 12)이다. 원본이 64×64 기본 그림에 여백을 더한 칸이라 이 피벗이 모두 발 아래 가운데다. 그래서 동작이 바뀌어도 발 위치와 화면상 크기가 그대로다.
  - 시트가 없거나 크기가 위와 다르면 경고 로그(`Debug.LogWarning`)를 남기고 세 동작 모두 대체 표시를 쓴다. 게임은 그대로 진행된다.
  - 몸 스프라이트는 발 피벗을 유지하며 Root 자리에서 1.5배로 표시한다. 제자 체력 바의 세로 가운데는 발에서 `53/32 × 1.5 + 0.2` 유닛(대기 그림의 머리 끝 + 간격)에 고정한다. 동작마다 움직이지 않는다.
  - 피격 번쩍임: 스프라이트는 색을 곱하므로 흰색 대신 RGB (1.0, 0.45, 0.45)로 0.1초 물들인다 (임시). 대체 표시는 지금처럼 흰색.
- **제자 애니메이션** (`DiscipleAnimator`, Core): `CombatView.Update`가 `Tick` 앞뒤의 값을 비교해 매 프레임 부르고, `Motion`·`Frame`의 스프라이트를 보여 준다. `Battle.Reset`(`ResetCompleted`) 때 `Reset()`.
  - 입력: `moved` = 이번 프레임에 `DisciplePosition`이 `MoveThreshold`보다 많이 커졌다. `attacked` = 이번 프레임의 `Tick` 안에서 `EnemyDamaged`를 받았다 (`EnemyDamaged`는 제자의 공격(기본 공격과 스킬)만 보낸다. 간격은 1.0초라 한 `Tick`에 공격은 많아야 한 번이다. 대기가 늘었는지로는 가리지 않는다: 달려와 멈춘 칸에도 앞 동작 대기로 는다). `engaged` = 멈춤이 아니고 패배가 아니고, 적 중 하나라도 `적 좌표 − 제자 좌표 ≤ 1.0 + 1e-9`.
  - 우선순위: ① 공격 마무리 중이면 공격 ② `engaged`이고 움직이지 않았고 남은 대기가 타격 앞 동작 시간 이내면 공격 앞 동작 ③ 달리는 중이면 달리기 ④ 대기.
  - 공격 재생 속도는 `17 / 공격 간격` fps (간격 1.0초 → 17 fps). 계속 공격하면 마무리가 끝나는 순간 다음 앞 동작이 시작되어 끊김 없이 이어진다.
  - 앞 동작: 남은 대기를 프레임으로 바꾼 값 `cooldown × fps`가 7 이하이면 `7 − ceil(cooldown × fps)` 프레임(0~6)을 보여 준다. 그래서 **대기가 0이 되어 피해가 들어가는 칸에 타격 프레임(7, 검기가 처음 보이는 프레임)이 보인다.**
  - 공격한 프레임: `공격 간격 − 남은 대기`(공격 뒤 흐른 시간)부터 세어 7번에서 16번까지 재생한다. 마무리 중에 다시 움직이면(적을 쓰러뜨려 달리기 시작) 검기 프레임 7~9까지만 보여 주고 달리기로 바꾼다.
  - **달려와 멈춘 칸**: `Battle`이 대기를 `공격 간격 × 7/17`로 올리므로("동작"의 2-1), 그 칸부터 앞 동작 0번이 시작되고 피해는 7번 프레임에 들어간다. 앞 동작은 `engaged`이면 그 칸에 움직였어도 보여 준다. 대기 없이 공격이 일어나는 경우(제자가 서 있는 채 범위 안에 적이 생겼을 때)에는 타격 프레임 7부터 재생한다.
  - 달리기 20 fps, 대기 10 fps로 반복한다 (임시). 제자 이동 속도는 초당 6.48 유닛이다 ("수치"). `Battle`은 0.02초 칸으로 움직여 화면 프레임(예: 60 fps)에 움직임이 없는 프레임이 끼므로, 움직임이 `RunGraceSeconds`(0.1초) 동안 없어야 달리기를 멈춘다. 다른 동작에서 달리기·대기로 들어가면 처음부터 센다.
  - 패배·멈춤 상태는 `engaged`가 아니므로 대기 동작을 보여 준다 (진행 중이던 공격 마무리는 끝까지 재생한다).
- **스킬 (이십사수매화검법)**: 제자는 기본 공격과 같은 공격 애니메이션을 보여 준다 (스킬도 `EnemyDamaged`를 보내므로 `attacked`가 된다. `DiscipleAnimator`는 바꾸지 않는다). 그에 더해 `SkillUsed`를 받으면 **검기 띠**를 하나 보인다 (모두 임시).
  - 모양: 1×1 흰색 스프라이트를 가로 2.0 × 세로 0.4로 늘린 사각형, 색 RGB (1.0, 0.55, 0.75).
  - 위치: 쏜 순간의 제자 표시 위치 기준으로 왼쪽 끝이 발의 x, 세로 가운데가 발 위 0.75 유닛이다. 곧 가운데 `(DisciplePosition − 2.25 + 1.0, −1.5 + 0.75)`에 고정되고 제자를 따라가지 않는다. 그리는 순서는 20000 (캐릭터 위, 체력 바 아래).
  - 불투명도: 쏜 뒤 흐른 시간 `t`(`Time.deltaTime`으로 센다)로 `CombatLayout.SkillSlashAlpha(t)`이고, `CombatLayout.SkillSlashSeconds`(0.25초)가 지나면 숨긴다. 보이는 중에 또 `SkillUsed`가 오면 새 위치에서 처음부터 센다. `ResetCompleted`에서 숨긴다.
  - 스킬 버튼과 재사용 대기 표시는 없다.
- **일반 몬스터**: 1스테이지에는 종남 남녀 제자 스프라이트를 인덱스 짝수·홀수 순서로 번갈아 표시한다. 대기와 걷기는 각각 10·12fps, 공격은 24프레임을 공격 간격에 맞춰 재생하며 대략 10번 프레임을 피해 시점에 맞춘다. 첫 공격은 기존 전투가 즉시 피해를 주므로 앞 동작이 생략될 수 있다. 이미지가 없거나 다른 스테이지면 1.5×1.5 주황색 사각형을 쓴다. 전투 위치 `(Position, CombatLayout.EnemyOffsetY(Index))`, 몸 그리기 순서는 `10 + min(Index, 9989)`로 고정해 겹친 적들의 앞뒤가 프레임마다 바뀌지 않는다. 원본 크기와 피벗은 `docs/dev/assets.md`의 종남 표를 따른다.
- **보스**: 종남 장로는 확정된 스프라이트를 사용하고 대기·걷기를 각 10·12fps로 반복한다. 공격은 24프레임을 공격 간격에 맞춰 재생하고 일반 공격 13번, 스킬 발사 14번 프레임을 공격 시점에 맞춘다 (0부터 셈). 첫 공격은 앞 동작이 생략될 수 있다. 발사된 검기는 작고 푸른 눈꽃 표시로 제자를 향해 이동하며 적중하면 없어진다. 전투가 멈추면 애니메이션과 검기도 멈춘다. 시트·피벗은 `docs/dev/assets.md`를 따른다. 리소스가 없거나 다른 테마면 3.0×4.5 보라색 사각형을 사용한다. 전투 위치 `(Position, 0)`, 그리는 순서는 일반 몬스터와 같은 인덱스 기준이다. 일반 몬스터와 보스는 공통 화면 오프셋에 더해 오른쪽 0.4유닛을 추가로 옮긴다.
- **머리 위 체력 바**: 제자와 일반 몬스터에만 둔다. 보스 체력은 스테이지 기능의 위쪽 바가 보여 준다. 머리 위 0.2 유닛에 명암이 있는 픽셀 금속 프레임을 두고 제자는 금색 프레임·매화 장식·초록 채움, 일반 몬스터는 은색 프레임·빨강 채움으로 구분한다. 채워지지 않은 영역과 프레임 밖은 투명하여 필드 배경이 보인다. 채움은 최대 가로 1.0 × 세로 0.125 유닛이다. 채움의 왼쪽 끝은 고정되고 실제 체력 비율을 약 0.3초 동안 부드럽게 따라간다. 전투 수치는 즉시 적용하며 표시만 보간한다. 도중에 체력이 다시 바뀌면 현재 표시 위치에서 새 비율로 이동하고 초기화할 때는 즉시 실제 비율로 맞춘다. 장식을 포함한 바 전체는 캐릭터를 따라가고, 종남 상대의 쓰러짐 때 함께 숨긴다.
- **피격 번쩍임**: `EnemyDamaged`를 받으면 적을, `DiscipleDamaged`를 받으면 제자를 0.1초(임시) 동안 색을 바꿨다가 원래 색으로 되돌린다. 스프라이트는 붉게 물들이고 대체 사각형은 흰색으로 바꾼다. 일반 적과 보스 모두 적용하며 별도 피격 애니메이션이나 밀림은 없다. 번쩍이는 중에 또 맞으면 0.1초를 다시 센다.
- **피해 숫자**: 제자와 적이 피해를 받으면 실제로 줄어든 체력을 아이보리 숫자 이미지로 표시한다 (남은 체력을 넘긴 피해는 실제 감소량만 표시). 숫자는 쉼표로 자릿수를 구분하며 약 0.8초 동안 위로 떠오르고 사라진다. 동시에 생기는 숫자는 가로로 조금씩 어긋나게 한다. 초기화하면 지우고 전투가 멈추면 움직임도 멈춘다. 이미지가 없으면 숫자를 표시하지 않는다. 스킬도 당분간 같은 색이며 매화색은 추후 적용한다.
- **적 오브젝트의 생성과 제거**: `EnemyAdded`에서 만들고, `EnemyDefeated`에서 바로 없앤다 (쓰러지는 연출 없음). `ResetCompleted`에서 남은 적 오브젝트를 모두 없앤다.
- **제자가 패배했을 때**: 제자 사각형(또는 스프라이트의 대기 동작)은 그대로 둔다 (체력 바가 비어 있다). 다음 `Reset`에서 원래대로 보인다.
- **배경**: 1스테이지는 사용자가 확정한 화산파 연무장 배경(`Art/Backgrounds/stage1_preview`)이 있으면 한 장을 좌우로 번갈아 반전해 가로로 반복하고 버튼 뒤까지 채운다. 배경이 없거나 다른 스테이지면 기존 대체 표시를 쓴다. 별도 시차 레이어는 없다.
- **1스테이지 꽃잎**: 시작 화면의 꽃잎 그림과 `PetalField`를 재사용해 화면에 드물게 흩날린다. 꽃잎 20장 중 활성 꽃잎을 표시하고, 그림의 긴 변은 시작 화면 크기(7~14)의 60%인 4.2~8.4 기준 픽셀, 불투명도는 기존 값의 80%다. 캔버스의 UI보다 뒤에 그리며 입력을 가로채지 않는다. 다른 스테이지에서는 숨기고, 이미지가 없으면 꽃잎 없이 동작한다.
  - 땅: 가로 40 × 세로 6 사각형, RGB (0.35, 0.30, 0.25), 윗변이 y = 0, 가로 가운데는 매 프레임 카메라의 x. 그리는 순서 −10.
  - 나무: 1.0×2.5 사각형 8개, RGB (0.20, 0.45, 0.25), y = 0에 서 있다. 월드 좌표 5 유닛(임시)마다 하나씩 있는 것처럼 보이게, 매 프레임 `i`번째(0~7) 나무를 x = `(floor(카메라 x ÷ 5) − 3 + i) × 5`에 둔다. 그리는 순서 −5.
- **바뀌는 때**: 위치·체력 바·카메라·배경은 매 프레임 `Update`에서 `Tick`을 부른 뒤 맞춘다.
- **누르는 것**: 없다. 전투 화면은 입력을 받지 않는다.
- **임시 적 배치 (`TempEncounter`)**: `Initialize`에서 바로, 그리고 다시 놓을 때마다 `Reset()`을 부른 뒤 `AddEnemy(Normal, 1, 6.0)`, `AddEnemy(Normal, 1, 9.0)`, `AddEnemy(Normal, 1, 9.3)`, `AddEnemy(Boss, 1, 15.0)`을 순서대로 부른다 (임시). `DiscipleDefeated`를 받았거나, `EnemyDefeated`를 받았을 때 `battle.Enemies.Count`가 0이면 1.0초(임시, `Time.deltaTime`으로 센다) 기다린 뒤 다시 놓는다. 금화는 주지 않는다.
  - 스테이지 기능 구현(019_stage_view)으로 지워졌다. 적 배치는 `StageProgress`가 한다 (`docs/spec/stage.md`).
- 겹침 어긋남, 체력 바 비율, 검기 띠 불투명도의 판단은 Core의 `CombatLayout`에, 제자 동작·프레임 선택은 `DiscipleAnimator`에 있고 에딧 모드 테스트로 판정한다. 제자 스프라이트는 플레이 모드 테스트 `DiscipleSpriteTests`가, 스킬 활성화와 검기 띠의 초기 상태·표시·소멸·초기화는 `CombatSkillTests`가 확인한다. 실제 게임에서 보이는 연출과 돌파 시 체력 바는 사람이 실행해 확인한다.

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| (없음) | | | 전투는 저장할 값이 없다 |

- 제자의 현재 체력, 좌표, 공격 대기 시간, 적의 상태는 저장하지 않는다. 2단계 기획도 "남은 체력과 처치 진행도는 복원하지 않고, 저장된 스테이지의 시작 지점에서 체력을 전부 회복하고 시작한다"로 정했다.
- 스킬 재사용 대기도 저장하지 않는다. 게임을 다시 켜면(`new Battle`) 준비된 상태로 시작한다. 경지와 내공 단계의 저장은 저장 기능이 한다.
- 되살리기: 따로 없다. `new Battle(upgrades)`의 시작 상태가 곧 스테이지 시작 상태다. 불러온 뒤에는 스테이지 기능이 `Reset()`과 `AddEnemy`로 시작한다.
- 잘못된 저장 값: 해당 없음 (저장하는 값이 없다).
- 오프라인 보상: 꺼져 있는 동안 전투는 진행되지 않는다. 오프라인 보상 금액 계산에도 전투 상태는 쓰이지 않는다.

## 범위 밖

- 각 작은 스테이지의 일반 몬스터 무리 구성과 좌표, 보스 등장 시점·거리, 스테이지 번호는 스테이지 기능이 정한다. `AddEnemy`를 쓴다.
- 스테이지 성공·실패 판정, 자동 재도전, 다음 스테이지 진행: 스테이지 기능이 정한다. `EnemyDefeated`, `DiscipleDefeated`, `IsDiscipleDefeated`, `Reset`을 쓴다.
- 처치 금화의 금액과 지급, 동시 처치 때 보상을 주는지: 스테이지 기능이 정한다. 전투는 `Wallet`을 쓰지 않는다.
- 진행 바, 보스 체력 바와 보스 이름, 스테이지 번호 표시, 페이드, 페이드 중 전투 멈춤과 강화 잠금: 스테이지 기능이 정한다. `SetPaused`를 쓴다.
- 강화 단계와 비용, 최종 능력치 계산: 강화 기능(`docs/spec/stat_upgrade.md`)이 정한다.
- 저장·불러오기, 오프라인 보상: 2단계에서 한다.
- 스킬 애니메이션: 사람이 새로 만들어 넣을 예정이다. 들어오면 따로 기획한다 (그때 `DiscipleAnimator`에 스킬 동작을 더할지 정한다). 지금은 기본 공격 애니메이션과 임시 검기 띠다.
- 스킬 버튼, 재사용 대기 표시, 스킬 습득·여러 스킬, 스킬 소리: 하지 않는다.
- 경지·내공 단계의 저장과 저장 판 번호: 저장 기능이 한다.
- 내공 강화 단계·비용과 경지 보너스 계산은 강화 기능(`docs/spec/stat_upgrade.md`), 경지 단계·처치 수 조건·보너스 %는 경지 기능(`docs/spec/realm.md`)이 정한다.
- 스킬·내공·경지 수치의 밸런스: 4단계에서 한다.
- 일반 원거리 몬스터와 제한 시간은 없다. 종남 외 보스의 이동·특수 기술은 아직 정하지 않았다.
- 종남 일반 상대·종남 장로 보스와 확정된 1스테이지 배경은 적용했다 ("화면"). 다른 테마의 실제 상대·보스와 배경, 제자의 피격 애니메이션, 효과음, 시차 스크롤은 아직 적용하지 않았다.
- 체력·공격력이 `long` 범위를 넘칠 만큼 큰 경우: 다루지 않는다.

## 완료 조건 초안

아래에서 따로 적지 않으면 지갑은 `new Wallet(1000)`, 강화는 `new StatUpgrades(wallet, 10, 100)`이다. "스킬 전투"는 `new Battle(upgrades, null, true)`, "기본 전투"는 `new Battle(upgrades)`다. "경지 전투"는 `realm = new RealmProgress(wallet)`, `upgrades = new StatUpgrades(wallet, 10, 100, 0, 0, 0, realm)`, `new Battle(upgrades, realm)`이다. "n칸 뒤"는 처음부터 `Step()`을 모두 n번 부른 뒤다. 좌표·시간은 오차 1e-6 안에서 비교한다.

이미 구현되고 바뀌지 않은 완료 조건(1~45)은 기록 파일로 옮겼다. 번호는 그대로 두고, 새 조건은 46번부터 붙인다. 옮긴 조건은 지금도 기본 전투(`new Battle(upgrades)`)로 그대로 성립한다.

스킬 피해 계산 (`BattleSkillTests`)

46. `Battle.CalculateSkillDamage`는 (10, 0) → 20, (10, 1) → 21, (13, 2) → 28, (11, 0) → 22, (0, 5) → 0, (10, `int.MaxValue`) → 2147483667을 돌려주고, (−1, 0)과 (10, −1)은 각각 `ArgumentOutOfRangeException`을 던진다.
47. 스킬 전투를 새로 만들면 `SkillEnabled`는 `true`, `SkillCooldown`은 0, `IsSkillReady`는 `true`, `SkillDamage`는 20이다. 기본 전투는 `SkillEnabled` `false`, `SkillCooldown` 0, `IsSkillReady` `false`다.
48. 스킬 전투에서 `upgrades.TryUpgrade(StatType.InnerPower)` 뒤 `SkillDamage`는 21이고, 이어서 `upgrades.TryUpgrade(StatType.Attack)` 뒤 25다 (공격력 12 × 210% = 25.2 → 25).

스킬 발동 (`BattleSkillTests`)

49. 스킬 전투에서 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 뒤: 보스의 `Health`는 80이고, `EnemyDamaged` 1번 다음에 `SkillUsed` 1번이 이 순서로 온다. `SkillCooldown`은 5.0, `IsSkillReady`는 `false`, `DiscipleAttackCooldown`은 1.0, `DiscipleHealth`는 90이다.
50. 49번에 이어: 51칸 뒤 보스의 `Health`는 70(기본 공격)이고 `SkillUsed`는 1번 그대로, `SkillCooldown`은 4.0이다. 250칸 뒤 보스의 `Health`는 40이고 `IsSkillReady`는 `false`, 251칸 뒤 20이고 `SkillUsed`는 2번이다.
51. 스킬 전투에서 `AddEnemy(Normal, 1, 0.8)`, `AddEnemy(Boss, 1, 1.9)`, `AddEnemy(Boss, 1, 2.5)` 뒤 1칸 뒤: 세 적의 `Health`는 10, 80, 100이고 `EnemyDamaged`는 2번(`Index` 0, 1 순서), `SkillUsed`는 1번 온다 (2.0 안의 적만 맞는다).
52. 스킬 전투에서 고정형 보스 `AddEnemy(Boss, 2, 1.5)` 뒤: 1칸 뒤 보스의 `Health`는 130, `SkillUsed`는 0번, `IsSkillReady`는 `true`다 (거리 1.0 밖·2.0 안이면 공격 차례가 아니다). 4칸 뒤 `DisciplePosition`은 0.5, 24칸 뒤 보스의 `Health`는 130, 25칸 뒤 110이고 `SkillUsed`는 1번이다 (달려와 멈춘 앞 동작 뒤 스킬).
53. 스킬 전투에서 `AddEnemy(Normal, 1, 0.8)`과 고정형 `AddEnemy(Boss, 2, 8.0)` 뒤: 1칸 뒤 일반 몬스터의 `Health`는 10, 51칸 뒤 `Enemies.Count`는 1이다. 106칸 뒤 `DisciplePosition`은 7.0, 126칸 뒤 보스의 `Health`는 130, 127칸 뒤 120이다. 251칸 뒤 `IsSkillReady`는 `true`이고 보스의 `Health`는 100, 276칸 뒤 100, 277칸 뒤 80이고 `SkillUsed`는 2번이다 (스킬 준비가 끝나도 다음 공격 차례까지 기다린다).
54. 기본 전투에서 `AddEnemy(Boss, 1, 1.0)` 뒤 251칸 뒤: 보스의 `Health`는 40, `SkillUsed`는 0번, `SkillCooldown`은 0이다.
55. 스킬 전투를 `new StatUpgrades(wallet, 15, 5)`로 만들고 `AddEnemy(Normal, 1, 0.8)` 뒤 1칸 뒤: `EnemyDamaged` → `SkillUsed` → `EnemyDefeated` → `DiscipleDefeated`가 이 순서로 1번씩 오고, `EnemyDefeated`를 받는 시점에 읽은 `IsDiscipleDefeated`는 `true`다 (스킬 피해 30으로 쓰러진 몬스터도 그 칸에 공격한다).
56. 스킬 전투에서 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고 `upgrades.TryUpgrade(StatType.InnerPower)`를 부르면 `SkillCooldown`은 5.0 그대로이고, 251칸 뒤 보스의 `Health`는 19다 (40 − 21).
57. 스킬 전투에서 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고 `Reset()`을 부르면 `SkillCooldown`은 0, `IsSkillReady`는 `true`다. 이어서 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 더 진행하면 새 보스의 `Health`는 80이다.
58. 스킬 전투에서 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고 `SetPaused(true)` 뒤 `Step()` 10번과 `Tick(0.1)`을 불러도 `SkillCooldown`은 5.0이다.
59. 스킬 전투를 `new StatUpgrades(wallet, 10, 5)`로 만들고 `AddEnemy(Normal, 1, 0.8)` 뒤 1칸 뒤: `IsDiscipleDefeated`는 `true`, `SkillCooldown`은 5.0이고, 이어서 `Step()` 10번 뒤에도 5.0이다.
60. 스킬 전투에서 적이 없을 때 50칸 뒤 `SkillCooldown`은 0, `IsSkillReady`는 `true`, `DisciplePosition`은 6.48이다.
61. 스킬 전투에서 `SkillUsed` 안에서 보스의 `Health`와 `SkillCooldown`을 읽게 하고 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하면, 읽은 값은 80과 0이다.
62. 스킬 전투에서 `SkillUsed` 안에서 `Reset()`을 부르게 하고 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하면, 예외 없이 `SkillCooldown`은 0, `DiscipleAttackCooldown`은 0, `DiscipleHealth`는 100, `Enemies.Count`는 0이고, `ResetCompleted`는 1번, `DiscipleDamaged`는 0번 온다.

경지 돌파 (`BattleRealmTests`)

63. 경지 전투에서 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하면(`DiscipleHealth` 90) `realm.RecordKill()`을 30번 부른 뒤 `DiscipleMaxHealth`는 110, `DiscipleHealth`는 100이다. 이어서 `upgrades.TryUpgrade(StatType.Health)` 뒤 `DiscipleMaxHealth`는 132, `DiscipleHealth`는 122다.
64. 지갑을 `new Wallet(10000)`으로 한 경지 전투에서 `RecordKill()`을 30·50·70·100번 차례로 부르면 `DiscipleMaxHealth`는 140, `DiscipleHealth`는 140이다.
65. 경지 전투를 `new StatUpgrades(wallet, 10, 10, 0, 0, 0, realm)`로 만들고 `AddEnemy(Normal, 1, 0.8)`을 2번 부른 뒤 1칸 뒤(`IsDiscipleDefeated` `true`, `DiscipleHealth` 0) `realm.RecordKill()`을 30번 부르면 `DiscipleHealth`는 0 그대로, `DiscipleMaxHealth`는 11이다. 이어서 `Reset()` 뒤 `DiscipleHealth`는 11이다.
66. 경지 전투의 `upgrades`로 `new Battle(upgrades)`(realm 없이)를 만들고 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행한 뒤 `realm.RecordKill()`을 30번 부르면 `DiscipleHealth`는 90 그대로, `DiscipleMaxHealth`는 110이다 (돌파를 구독하지 않는다).
67. 경지 전투에서 `SetPaused(true)` 뒤 `realm.RecordKill()`을 30번 부르면 `DiscipleHealth`는 110이다 (멈춘 동안에도 반영한다).

표시 규칙 (`CombatLayoutTests`)

68. `CombatLayout.SkillSlashAlpha`는 0 → 0.6, 0.125 → 0.3, 0.25 → 0.0, 0.3 → 0.0, −0.1 → 0.6, `double.NaN` → 0.0을 돌려주고, `CombatLayout.SkillSlashSeconds`는 0.25다.

화면 (자동 테스트 없음. 사람이 실행해 확인)

69. 게임을 실행하면 제자가 처음 몬스터를 만나 공격할 때 제자 앞쪽에 연분홍 띠가 잠깐 보였다 사라지고, 그 공격은 기본 공격의 두 배 피해를 준다. 그 뒤 약 5초마다 같은 띠가 보이고, 그 사이에는 띠 없이 기본 공격을 한다.
70. 금화를 모아 이류로 돌파하면 그 자리에서 제자 체력 바가 늘어난 최대 체력에 맞게 차오른다 (체력이 가득일 때 돌파하면 계속 가득이다).
71. 기존 테스트 `CoreAssemblyTests.Core_DoesNotReferenceUnity`와 기존 전투 테스트(1~45의 테스트)가 고치지 않은 채 계속 통과한다.

## 결정 기록

결정 기록은 [records/combat.md](records/combat.md)의 "결정 기록"에 있다. 구현에 필요한 규칙은 위 절들에 모두 있다.

## 변경 기록

### 2026-11-19 — 3단계: 이십사수매화검법, 돌파 때 현재 체력 증가, 공격 속도 제거 반영 (기준 커밋: c02cb2a)

| 절 | 바뀌기 전 | 바뀐 뒤 |
| --- | --- | --- |
| 목표 | 전진·접근·공격·쓰러짐·필드 화면 | 자동 발동 스킬, 체력 강화·돌파 때 현재 체력, 스킬 검기 띠 추가. 출처에 milestone_3 |
| 동작 | 대기 감소는 제자·적의 공격 대기 | 스킬이 켜져 있으면 스킬 대기도 줄인다 |
| 동작 | 앞 동작 대기 `StatUpgrades.AttackInterval × 7/17`, 공격 뒤 대기 `StatUpgrades.AttackInterval` | `DiscipleBaseAttackInterval`(1.0) 기준 (공격 속도 제거, 사람이 이미 구현) |
| 동작 | 공격 차례에 늘 기본 공격 | 스킬이 준비됐으면 기본 공격 대신 스킬: 2.0 안 모든 적에 `SkillDamage`, `EnemyDamaged`들 → `SkillUsed`, 스킬 대기 5.0, 공격 대기 1.0 |
| 동작 | 강화 반영: 공격력·공격 속도·체력 | 공격력(경지 포함)·공격 간격 고정·내공(스킬 피해)·체력·경지 돌파(`BrokeThrough` → 현재 체력 증가) |
| 동작 | (없음) | 스킬 켜기 `skillEnabled`, `Reset` 때 스킬 대기 0, `SkillUsed` 안의 `Reset` |
| 경계 상황 | 스킬·돌파 줄 없음 | 스킬 준비·기다림·범위·멈춤·패배·꺼짐, 패배 중 돌파, `realm` `null`, `CalculateSkillDamage` 음수, `SkillSlashAlpha` |
| 수치 | 시작 공격 간격을 `StatUpgrades`에 넣음, 앞 동작 0.5초 예, 제자 피해량 | 공격 간격은 전투가 직접 쓴다. 스킬 범위 2.0, 대기 5.0, 배율 200% + 10%p, 피해 공식, 검기 띠 (모두 임시) |
| 연결 지점 | `Battle(StatUpgrades upgrades)`, `DiscipleAttackInterval` = `upgrades.AttackInterval` | `Battle(upgrades, RealmProgress realm = null, bool skillEnabled = false)`, 상수 `SkillRange`·`SkillCooldownSeconds`·`SkillBasePercent`·`SkillPercentPerInnerLevel`, `SkillEnabled`, `SkillCooldown`, `IsSkillReady`, `SkillDamage`, `CalculateSkillDamage`, `SkillUsed`, `CombatLayout.SkillSlashSeconds`·`SkillSlashAlpha`. `DiscipleAttackInterval`은 늘 1.0. `GameRoot`는 `new Battle(StatUpgrades, Realm, true)` |
| 연결 지점 (필요한 것) | `Attack`, `MaxHealth`, `AttackInterval`, `Upgraded` | `Attack`, `MaxHealth`, `GetLevel(StatType.InnerPower)`, `Upgraded`, `RealmProgress.BrokeThrough`, `GameRoot.Realm` |
| 화면 | 공격 간격 0.5초 예 | 1.0초 고정. 스킬 때 기본 공격 애니메이션 + 검기 띠 |
| 저장 | 전투는 저장 값 없음 | 같음. 스킬 대기도 저장하지 않는다 |
| 범위 밖 | 경지·스킬·내공·공격 속도 제거는 3단계에서 | 스킬 애니메이션(사람이 나중에), 스킬 버튼·표시, 경지·내공 저장(저장 기능), 밸런스(4단계) |
| 완료 조건 | 1~45 | 1~45는 기록 파일로 옮김. 새로 46~71 |

- 완료 조건 초안: 새로 46~71 / 바뀜 없음 / 1~45는 기록 파일로 옮김 (이미 구현됨. 21-1·23은 공격 속도 제거로 없어짐).
- 바뀌는 연결 지점: `Battle` 생성자 끝에 `RealmProgress realm = null, bool skillEnabled = false` 추가 (기존 호출은 그대로). 위 표의 상수·속성·메서드·이벤트 추가. `CombatLayout`에 `SkillSlashSeconds`, `SkillSlashAlpha` 추가. `GameRoot`의 `Battle` 생성이 `new Battle(StatUpgrades, Realm, true)`로.
- 고쳐야 할 기존 코드: `Assets/Scripts/Core/Combat/Battle.cs` — 스킬과 돌파 구독. `Assets/Scripts/Core/Combat/CombatLayout.cs` — 검기 띠 불투명도. `Assets/Scripts/Game/Combat/CombatView.cs` — 검기 띠 표시. `Assets/Scripts/Game/GameRoot.cs` — `Battle` 생성 인자.
- 기존 테스트:

| 테스트 | 지금 | 바뀐 뒤 | 처리 |
| --- | --- | --- | --- |
| 없음 | | | `skillEnabled` 기본값 `false`, `realm` 기본값 `null`이라 기존 전투·스테이지·저장 테스트와 `GameBootTests`는 그대로 통과한다 |
