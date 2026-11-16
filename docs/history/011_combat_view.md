# 011_combat_view — 전투 필드 화면과 GameRoot 조립

## 목표
`PlumSword.Game` 에서 `GameRoot` 가 전투 로직 `Battle` 하나를 만들어 들고 있게 하고, 필드 화면 `CombatView` 를 만든다. `CombatView` 는 매 프레임 `Battle.Tick` 을 부르고, 제자·몬스터·보스의 대체 표시(단색 사각형), 머리 위 체력 바, 피격 번쩍임, 카메라, 임시 배경(땅과 나무)을 그린다. 계산은 Core 의 `Battle`, `CombatLayout` 을 쓴다.

## 묶음
전투

## 선행 작업
- 006_stat_upgrade_view
- 007_enemy_stats
- 008_battle_movement
- 009_battle_attack
- 010_battle_upgrade_reentry

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다" 절)
- docs/dev/art.md ("기본 숫자" 절의 카메라 크기)
- docs/spec/combat.md ("연결 지점", "화면" 절)
- docs/spec/stat_upgrade.md ("연결 지점" 절의 `GameRoot.StatUpgrades`)

## 요구사항
- 선행 작업이 만든 것을 쓴다. 내부는 고치지 않는다.
  - 006_stat_upgrade_view: `PlumSword.Game.GameRoot` 의 `PlumSword.Core.StatUpgrade.StatUpgrades StatUpgrades { get; }`. `GameRoot.Awake` 는 `Wallet` → `StatUpgrades` 를 만들고 `[Canvas]` → `[EventSystem]` → `[CurrencyView]` → `[StatUpgradeView]` 를 조립해 둔 상태다. `StatUpgrades` 는 `new StatUpgrades(Wallet, 10, 100, 1.0)` 에 해당하는 줄로 만들어져 있다 (숫자에 이름 있는 상수가 붙어 있을 수 있다).
  - 008~010: `PlumSword.Core.Combat.Battle` — `new Battle(StatUpgrades upgrades)`, `void Tick(double deltaSeconds)`, `double DisciplePosition { get; }`, `long DiscipleHealth { get; }`, `long DiscipleMaxHealth { get; }`, `IReadOnlyList<Enemy> Enemies { get; }`, 상수 `DiscipleBaseAttack`(10), `DiscipleBaseMaxHealth`(100), `DiscipleBaseAttackInterval`(1.0), 알림 `event Action<Enemy> EnemyAdded`, `event Action<Enemy> EnemyDamaged`, `event Action DiscipleDamaged`, `event Action<Enemy> EnemyDefeated`, `event Action ResetCompleted`. `PlumSword.Core.Combat.Enemy` — `EnemyKind Kind`, `int Index`, `double Position`, `long Health`, `long MaxHealth`.
  - 007_enemy_stats: `PlumSword.Core.Combat.EnemyKind` (`Normal`, `Boss`), `PlumSword.Core.Combat.CombatLayout` — `double EnemyOffsetY(int index)` (겹친 일반 몬스터의 위아래 어긋남), `double HealthRatio(long health, long maxHealth)` (체력 바가 찬 비율 0.0 ~ 1.0).
- 고칠 파일: `Assets/Scripts/Game/GameRoot.cs`. 만들 파일: `Assets/Scripts/Game/Combat/CombatView.cs` (네임스페이스 `PlumSword.Game.Combat`). 화면에만 쓰는 도우미 타입이 필요하면 같은 폴더에 `internal` 로 만든다 (파일 하나에 타입 하나).
- 공개 모양은 사양의 "연결 지점" 절 그대로다. 뒤 작업(012_temp_encounter)과 스테이지 기능이 `GameRoot.Battle` 을 쓴다.

  ```csharp
  namespace PlumSword.Game
  {
      public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래 하나를 더한다
      {
          public PlumSword.Core.Combat.Battle Battle { get; }   // 밖에서는 읽기만 (private set)
      }
  }

  namespace PlumSword.Game.Combat
  {
      public sealed class CombatView : MonoBehaviour
      {
          public void Initialize(PlumSword.Core.Combat.Battle battle, UnityEngine.Camera camera);
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/combat.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 이름이 겹치는 것에 주의한다: `PlumSword.Game` 안에서 `Combat` 이라고만 쓰면 네임스페이스 `PlumSword.Game.Combat` 으로 읽힌다. Core 타입이 헷갈리게 읽혀 컴파일 오류가 나면 전체 이름(`PlumSword.Core.Combat.Battle`)이나 `using` 별칭을 쓴다. `Core` 의 `double` 값은 `float` 로 바꿔서 쓴다.
- **`GameRoot.Awake` 에 더하는 것** (다른 기능의 줄은 아래 1번의 숫자 교체 말고는 고치지 않는다):
  1. `StatUpgrades` 를 만드는 줄의 시작 능력치 숫자(10, 100, 1.0 또는 그 값을 담은 `GameRoot` 의 상수)를 `Battle.DiscipleBaseAttack`, `Battle.DiscipleBaseMaxHealth`, `Battle.DiscipleBaseAttackInterval` 로 바꾼다 (값은 같다). 쓰이지 않게 된 `GameRoot` 의 임시 상수와 그 주석은 지운다. 이 교체는 사양(`docs/spec/combat.md` "연결 지점")이 정한 것이다.
  2. `StatUpgrades` 를 만든 줄 바로 뒤에서 `Battle = new Battle(StatUpgrades)` 로 하나 만든다. 게임에 `Battle` 은 이것 하나뿐이다.
  3. 기존 화면 조립(`[StatUpgradeView]`까지) 뒤에서: 카메라를 구한다. `Camera.main` 이 있으면 그것을 쓰고, 없으면 `[Game]` 의 자식 `[Camera]` 를 만들어 `Camera` 를 붙이고 태그를 `MainCamera` 로 단다.
  4. `[Game]` 의 자식 `[CombatView]` 를 만들어 `CombatView` 를 붙이고 `Initialize(Battle, 카메라)` 를 부른다.
- **`CombatView.Initialize(battle, camera)`**:
  - `battle` 이나 `camera` 가 `null` 이면 `System.ArgumentNullException`.
  - 카메라를 설정하고, 제자와 배경 오브젝트를 만든다. `battle` 의 알림(`EnemyAdded`, `EnemyDamaged`, `DiscipleDamaged`, `EnemyDefeated`, `ResetCompleted`)을 구독하고 `OnDestroy` 에서 모두 끊는다.
  - `Initialize` 시점에 이미 `battle.Enemies` 에 적이 있으면 그 적들의 오브젝트도 만든다.
- **`Update`**: `battle.Tick(Time.deltaTime)` 을 부른 뒤, 아래의 위치·체력 바·카메라·배경·번쩍임을 모두 맞춘다. `Tick` 을 부르는 것은 게임 전체에서 `CombatView` 하나뿐이다. `Initialize` 가 불리기 전에는 `Update` 가 아무것도 하지 않는다.
- **그리는 곳**: UI 캔버스가 아니라 월드 공간에 `SpriteRenderer` 로 그린다. 좌표 단위는 유닛이고, 전투의 진행 방향 좌표가 월드의 x다. 땅 높이는 y = 0 (임시값)이고 캐릭터는 모두 y = 0에 선다. 만든 오브젝트는 모두 `[CombatView]` 의 자식(또는 그 아래)으로 둔다.
- **대체 표시 만드는 법**: 스프라이트 리소스를 쓰지 않는다 (`Resources.Load` 없음). 1×1 흰색 `Texture2D` 로 스프라이트를 하나 만들어(픽셀당 유닛 1) 모든 사각형이 함께 쓰고, 크기는 `localScale`, 색은 `SpriteRenderer.color` 로 정한다. 캐릭터의 좌표는 발 아래 가운데가 기준이다 (사각형의 아래 가운데가 그 좌표에 오게 둔다). `OnDestroy` 에서 만든 텍스처와 스프라이트를 없앤다.
- **카메라**: `orthographic = true`, `orthographicSize = 5.625`, `clearFlags = CameraClearFlags.SolidColor`, `backgroundColor` RGB (0.55, 0.75, 0.90) (임시값). 위치는 매 프레임 `(DisciplePosition + 4.0, 0, −10)` (임시값). 제자는 화면 가운데보다 왼쪽에 고정되어 보인다. `Reset` 으로 제자가 좌표 0으로 돌아가면 카메라도 바로 따라간다 (부드럽게 따라가는 처리는 없다).
- **제자**: 1.0×1.0 사각형, RGB (0.25, 0.50, 0.90), 위치 `(DisciplePosition, 0)`, `sortingOrder` 1 (모두 임시값). `docs/dev/art.md` 의 예시(1.5×1.5)가 아니라 이 값을 쓴다.
- **일반 몬스터** (`Kind == Normal`): 1.0×1.0 사각형, RGB (0.80, 0.45, 0.20), 위치 `(Position, CombatLayout.EnemyOffsetY(Index))`, `sortingOrder` 0 (모두 임시값).
- **보스** (`Kind == Boss`): 2.0×3.0 사각형, RGB (0.55, 0.20, 0.60), 위치 `(Position, 0)`, `sortingOrder` 0 (모두 임시값). 어긋남(`EnemyOffsetY`)을 쓰지 않는다.
- **머리 위 체력 바**: 제자와 일반 몬스터에만 둔다. 보스에는 두지 않는다. 캐릭터를 따라다니게 캐릭터 오브젝트의 자식으로 둔다. 바탕은 가로 1.0 × 세로 0.12 사각형, RGB (0.15, 0.15, 0.15), `sortingOrder` 5이고, 세로 가운데가 캐릭터 머리 위 0.2 유닛(발에서 1.2 유닛 높이), 가로 가운데는 캐릭터와 같다. 채움 사각형은 바탕과 같은 높이·세로 위치에, 가로 길이 `1.0 × CombatLayout.HealthRatio(체력, 최대 체력)` 이고 왼쪽 끝이 바탕의 왼쪽 끝에 고정된다 (체력이 줄면 오른쪽부터 줄어든다). 채움 색은 제자 초록 RGB (0.20, 0.80, 0.30), 일반 몬스터 빨강 RGB (0.85, 0.20, 0.20), `sortingOrder` 6 (모두 임시값). 제자는 `DiscipleHealth`, `DiscipleMaxHealth`, 몬스터는 그 `Enemy` 의 `Health`, `MaxHealth` 를 쓴다. 비율은 `CombatLayout.HealthRatio` 가 돌려준 값을 그대로 쓰고 화면 코드에서 따로 계산하지 않는다. 부모의 `localScale` 때문에 자식 크기가 틀어지지 않게 한다 (예: 크기를 주는 사각형과 체력 바를 형제로 두고, 크기 없는 빈 오브젝트가 둘의 부모가 된다).
- **피격 번쩍임**: `EnemyDamaged` 를 받으면 그 적의 몸 사각형을, `DiscipleDamaged` 를 받으면 제자의 몸 사각형을 0.1초(임시값) 동안 흰색으로 바꿨다가 원래 색으로 되돌린다. 번쩍이는 중에 또 맞으면 0.1초를 다시 센다. 시간은 `Update` 에서 `Time.deltaTime` 으로 센다. 체력 바는 번쩍이지 않는다.
- **적 오브젝트의 생성과 제거**: `EnemyAdded` 에서 만들고, `EnemyDefeated` 에서 바로 없앤다 (쓰러지는 연출 없음). `ResetCompleted` 에서 남은 적 오브젝트를 모두 없애고 제자의 번쩍임도 끝낸다. 적과 오브젝트의 짝은 `Enemy` 를 키로 하는 사전으로 들고 있는다. 이미 없앤 적의 알림이 와도 예외가 나지 않아야 한다.
- **제자가 패배했을 때**: 제자 사각형은 그대로 둔다 (체력 바가 비어 있다). 다음 `Reset` 에서 원래대로 보인다. 패배 문구나 연출은 없다.
- **임시 배경** (길이 이어지는 느낌을 주는 표시):
  - 땅: 가로 40 × 세로 6 사각형, RGB (0.35, 0.30, 0.25), 윗변이 y = 0 (가운데 y = −3), 가로 가운데는 매 프레임 카메라의 x. `sortingOrder` −10.
  - 나무: 1.0×2.5 사각형 8개, RGB (0.20, 0.45, 0.25), y = 0에 서 있다 (아래 가운데가 기준). 월드 좌표 5 유닛(임시값)마다 하나씩 있는 것처럼 보이게, 매 프레임 `i` 번째(0~7) 나무를 x = `(floor(카메라 x ÷ 5) − 3 + i) × 5` 에 둔다. `sortingOrder` −5.
- **누르는 것**: 없다. 전투 화면은 입력을 받지 않는다.
- `Assets/Scripts/Game/Combat/` 폴더를 새로 만든다. `.meta` 파일은 유니티가 화면 없이 실행될 때(테스트 실행) 자동으로 만든다. 만들어진 `.meta` 는 지우지 않는다.
- 이 작업이 끝난 시점에는 적을 놓는 코드가 없다 (012_temp_encounter 가 만든다). 실행하면 제자가 혼자 오른쪽으로 계속 전진하는 것이 정상이다. 확인용 적 배치 코드나 치트 키를 넣지 않는다.
- 이 작업에는 에딧 모드 테스트를 새로 쓰지 않는다 (테스트 어셈블리는 Core 만 참조한다). 겹침 어긋남과 체력 바 비율의 판정은 007_enemy_stats 의 `CombatLayoutTests` 가 이미 한다. 플레이 모드 테스트 어셈블리도 만들지 않는다.

## 완료 조건
- [컴파일] `GameRoot.cs` 와 `CombatView.cs` 가 컴파일 오류 없이 컴파일되고 기존 테스트가 모두 통과한다. (`BattleTests`, `BattleAttackTests`, `BattleUpgradeTests`, `EnemyStatsTests`, `CombatLayoutTests`, `StatUpgradesTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)
- [사람 확인] 게임을 실행하면 하늘색 바탕과 갈색 땅, 초록 나무 사각형이 보이고, 파란 제자 사각형이 오른쪽으로 전진하며 나무가 왼쪽으로 일정한 간격으로 끊김 없이 흘러간다. 제자는 화면 가운데보다 왼쪽의 같은 자리에 보인다.
- [사람 확인] 제자 머리 위에 초록색 체력 바가 가득 찬 채로 보이고, 아래쪽의 금화 글자와 강화 버튼 세 개가 그대로 보인다.
- [사람 확인] 실행 중 Hierarchy 의 `[Game]` 아래에 `[CombatView]` 가 하나 있고, 카메라가 Orthographic, Size 5.625 다.
- [사람 확인] 실행 중 콘솔에 예외(빨간 줄)가 없다.

## 하지 말 것
- `Assets/Scripts/Core/` 를 고치지 않는다. `Battle`, `Enemy`, `CombatLayout`, `StatUpgrades` 는 공개된 모양으로만 쓴다. 로직을 바꿔야 하면 고치지 말고 보고서의 가정에 적는다.
- `Assets/Scripts/Game/Currency/`, `Assets/Scripts/Game/StatUpgrade/`, `GameBootstrap.cs` 를 고치지 않는다.
- `GameRoot` 에는 자기 기능의 줄(`Battle` 속성과 생성, 카메라, `[CombatView]` 조립)만 추가하고, 다른 기능의 줄은 요구사항에 적은 시작 능력치 숫자 교체 말고는 고치지 않는다.
- `TempEncounter`(임시 적 배치)를 만들지 않는다. 012_temp_encounter 가 한다. `CombatView` 에서 `AddEnemy`, `Reset`, `SetPaused`, `Step` 을 부르지 않는다.
- 화면 코드에서 피해, 거리, 쓰러짐을 계산하지 않는다. `Battle` 의 상태를 읽어 그리기만 한다.
- 스테이지 화면(진행 바, 보스 체력 바, 스테이지 글자, 페이드)을 만들지 않는다. 스테이지 기능의 일이다.
- 실제 스프라이트, 공격·피격·쓰러짐 애니메이션, 피해 숫자, 효과음, 시차 스크롤, Pixel Perfect Camera 설정을 만들지 않는다. 사양의 "범위 밖"이다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다. 화면은 코드로 조립한다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `GameRoot` 에 `Battle` 속성(private set)을 더하고, `Awake` 에서 `StatUpgrades` 를 만든 바로 뒤에 `new Battle(StatUpgrades)` 로 하나 만든다. `StatUpgrades` 의 시작 능력치 인자를 `Battle.DiscipleBaseAttack`, `DiscipleBaseMaxHealth`, `DiscipleBaseAttackInterval` 로 바꾸고, 쓰이지 않게 된 임시 상수 `BaseAttack`, `BaseMaxHealth`, `BaseAttackInterval` 과 그 주석을 지웠다.
- `[StatUpgradeView]` 조립 뒤에 `CreateCombatView()` 를 더했다: `Camera.main` 을 쓰고, 없으면 `[Game]` 의 자식 `[Camera]` 를 만들어 `Camera` 를 붙이고 `MainCamera` 태그를 단다. 그 뒤 `[CombatView]` 를 만들어 `Initialize(Battle, 카메라)` 를 부른다.
- `CombatView` (월드 공간, `SpriteRenderer`): 1×1 흰 텍스처로 만든 스프라이트 하나를 모든 사각형이 함께 쓴다. 카메라 설정, 제자·일반 몬스터·보스 사각형, 제자·일반 몬스터의 머리 위 체력 바(바탕·채움, 왼쪽 끝 고정), 피격 번쩍임 0.1초(다시 맞으면 다시 셈), 땅과 나무 8그루, 매 프레임 `Tick` 뒤 위치·체력 바·카메라·배경·번쩍임 맞추기. 적 오브젝트는 `Enemy` 를 키로 하는 사전으로 들고 `EnemyAdded` 에서 만들고 `EnemyDefeated` 에서 없애며, `ResetCompleted` 에서 모두 없애고 제자 번쩍임을 끝낸다. 없는 적의 알림은 무시한다. `OnDestroy` 에서 알림을 끊고 텍스처·스프라이트를 없앤다.
- 캐릭터는 크기 없는 빈 Root(발 아래 가운데) 아래에 몸 사각형과 체력 바를 형제로 둬서 몸의 `localScale` 이 체력 바에 영향을 주지 않는다.
- 사양 `docs/spec/combat.md` "연결 지점" 의 `GameRoot.Battle` 에 private set, `CombatView.Initialize` 에 예외 규칙을 주석으로 적었다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs` (수정)
- `Assets/Scripts/Game/Combat/CombatView.cs` (새로)
- `Assets/Scripts/Game/Combat/CombatActor.cs` (새로, `internal` 도우미: 캐릭터 하나의 오브젝트 묶음과 번쩍임 남은 시간)
- `Assets/Scripts/Game/Combat.meta`, `Assets/Scripts/Game/Combat/CombatView.cs.meta`, `Assets/Scripts/Game/Combat/CombatActor.cs.meta` (유니티가 자동 생성)
- `docs/spec/combat.md` ("연결 지점" 절 주석 두 줄)

### 가정
- `CombatView.Initialize` 를 두 번 부르면 `InvalidOperationException` 을 던진다 (작업 문서에 없음. 사양 "연결 지점" 주석에 적었다).
- 체력 바의 세로 가운데는 "몸 높이 + 0.2" 로 계산했다 (제자·일반 몬스터는 1.2. 보스는 체력 바가 없어 쓰지 않는다).
- 일반 몬스터의 체력 바도 몸과 함께 `EnemyOffsetY` 만큼 위아래로 어긋난다 (캐릭터 Root 의 자식이므로).
- 오브젝트 이름: `[Disciple]`, `[Enemy]`, `[Boss]`, `[Ground]`, `[Tree]`, 자식 `Body`, `HealthBack`, `HealthFill` (임시). 텍스처는 Point 필터, Clamp.
- 오브젝트 위치는 `[CombatView]` 기준 `localPosition` 으로 둔다. `[Game]` 과 `[CombatView]` 가 원점에 있다고 가정한다 (`GameBootstrap` 이 원점에 만든다고 봄).
- 번쩍임 남은 시간은 `Tick` 이 같은 프레임에 보낸 피격 알림 직후 `Update` 의 `deltaTime` 만큼 바로 줄어든다 (그 프레임은 흰색으로 그려진다. 사실상 0.1초에서 한 프레임 이하 차이).
- `Initialize` 시점에도 화면을 한 번 맞춘다 (첫 `Update` 전에 카메라·배경이 제자리에 있게).

### 사람이 확인할 것
- [사람 확인] 게임을 실행하면 하늘색 바탕과 갈색 땅, 초록 나무 사각형이 보이고, 파란 제자 사각형이 오른쪽으로 전진하며 나무가 왼쪽으로 일정한 간격으로 끊김 없이 흘러간다. 제자는 화면 가운데보다 왼쪽의 같은 자리에 보인다.
- [사람 확인] 제자 머리 위에 초록색 체력 바가 가득 찬 채로 보이고, 아래쪽의 금화 글자와 강화 버튼 세 개가 그대로 보인다.
- [사람 확인] 실행 중 Hierarchy 의 `[Game]` 아래에 `[CombatView]` 가 하나 있고, 카메라가 Orthographic, Size 5.625 다.
- [사람 확인] 실행 중 콘솔에 예외(빨간 줄)가 없다.
- 적 표시, 체력 바 줄어듦, 피격 번쩍임, 적 제거, Reset 처리는 적을 놓는 코드가 아직 없어(012_temp_encounter) 실행으로도 확인하지 못했다. 012 이후에 확인한다.
- 씬의 기존 카메라가 Perspective 등 다른 설정이어도 `Initialize` 가 덮어쓴다. 씬 카메라에 다른 컴포넌트(예: 2D 렌더러 설정)가 있으면 그대로 둔다.

### 테스트 결과
- `python tools/loop/run_unity_tests.py`: EditMode 191개 중 191개 통과, 0개 실패. 컴파일 오류 없음 (Game 어셈블리 포함).
- 새 테스트는 쓰지 않았다 (작업 문서 지시).


## 루프 기록 (2026-11-16 20:51)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/011_combat_view
- 루프 브랜치: loop/20261116-164139
