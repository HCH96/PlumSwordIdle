# 012_temp_encounter — 임시 적 배치와 반복

## 목표
`PlumSword.Game` 에 스테이지 기능이 생기기 전까지만 쓰는 임시 적 배치 `TempEncounter` 를 만들고 `GameRoot` 에 조립한다. 일반 몬스터 3마리와 보스 1마리를 놓고, 모두 처치하거나 제자가 패배하면 1초 뒤 처음 배치로 되돌려 반복한다. 이것으로 전투 화면을 실행해 눈으로 확인할 수 있게 된다.

## 묶음
전투

## 선행 작업
- 008_battle_movement
- 009_battle_attack
- 011_combat_view

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다" 절)
- docs/spec/combat.md ("연결 지점" 절의 `TempEncounter`, "화면" 절의 "조립"과 "임시 적 배치")

## 요구사항
- 선행 작업이 만든 것을 쓴다. 내부는 고치지 않는다.
  - 008_battle_movement, 009_battle_attack: `PlumSword.Core.Combat.Battle` — `Enemy AddEnemy(EnemyKind kind, int stage, double position)`, `void Reset()` (제자를 좌표 0·체력 가득으로 되돌리고 모든 적을 없앤다), `IReadOnlyList<Enemy> Enemies { get; }` (살아 있는 적. `EnemyDefeated` 를 받는 시점에 쓰러진 적은 이미 빠져 있다), `event System.Action<Enemy> EnemyDefeated`, `event System.Action DiscipleDefeated`. `PlumSword.Core.Combat.EnemyKind` (`Normal`, `Boss`).
  - 011_combat_view: `PlumSword.Game.GameRoot` 의 `PlumSword.Core.Combat.Battle Battle { get; }`. `GameRoot.Awake` 가 `Battle` 을 만들고 `[CombatView]` 까지 조립해 둔 상태다. `CombatView` 가 매 프레임 `Battle.Tick` 을 부르고 적 오브젝트를 `EnemyAdded` / `EnemyDefeated` / `ResetCompleted` 에 맞춰 만들고 없앤다.
- 고칠 파일: `Assets/Scripts/Game/GameRoot.cs`. 만들 파일: `Assets/Scripts/Game/Combat/TempEncounter.cs` (네임스페이스 `PlumSword.Game.Combat`).
- 공개 모양은 사양의 "연결 지점" 절 그대로다.

  ```csharp
  namespace PlumSword.Game.Combat
  {
      // 스테이지 기능이 생기기 전까지만 쓰는 임시 적 배치. 스테이지 기능을 구현할 때 이 클래스와 GameRoot 의 조립 줄을 지운다.
      public sealed class TempEncounter : MonoBehaviour
      {
          public void Initialize(PlumSword.Core.Combat.Battle battle);
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/combat.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 클래스 위에 "스테이지 기능이 생기기 전까지만 쓰는 임시 코드다. 스테이지 기능을 구현할 때 이 클래스와 `GameRoot` 의 조립 줄을 지운다"는 주석을 단다.
- 이름이 겹치는 것에 주의한다: `PlumSword.Game` 안에서 `Combat` 이라고만 쓰면 네임스페이스 `PlumSword.Game.Combat` 으로 읽힌다. Core 타입이 헷갈리게 읽혀 컴파일 오류가 나면 전체 이름(`PlumSword.Core.Combat.Battle`)이나 `using` 별칭을 쓴다.
- **`GameRoot.Awake` 에 더하는 것**: `[CombatView]` 를 조립한 줄 뒤에서 `[Game]` 의 자식 `[TempEncounter]` 를 만들어 `TempEncounter` 를 붙이고 `Initialize(Battle)` 를 부른다. 이 조립 줄에 "임시. 스테이지 기능을 구현할 때 지운다"는 주석을 단다. 다른 줄은 고치지 않는다.
- **`TempEncounter.Initialize(battle)`**:
  - `battle` 이 `null` 이면 `System.ArgumentNullException`.
  - `battle.EnemyDefeated`, `battle.DiscipleDefeated` 를 구독하고 `OnDestroy` 에서 끊는다.
  - 바로 적을 한 번 배치한다 (아래 "배치").
- **배치**: `battle.Reset()` 을 부른 뒤 아래를 순서대로 부른다 (모두 임시값).
  1. `battle.AddEnemy(EnemyKind.Normal, 1, 6.0)`
  2. `battle.AddEnemy(EnemyKind.Normal, 1, 9.0)`
  3. `battle.AddEnemy(EnemyKind.Normal, 1, 9.3)`
  4. `battle.AddEnemy(EnemyKind.Boss, 1, 15.0)`
- **다시 놓기**: `DiscipleDefeated` 를 받았거나, `EnemyDefeated` 를 받았을 때 `battle.Enemies.Count` 가 0이면 "다시 놓기 대기"를 시작한다. 대기는 1.0초(임시값)이고 `Update` 에서 `Time.deltaTime` 으로 센다. 1.0초가 지나면 "배치"를 한 번 하고 대기를 끝낸다. 이미 대기 중일 때 알림이 또 와도 시간을 다시 세지 않는다.
  - 알림을 받은 그 자리(알림 처리 안)에서 `Reset` 이나 `AddEnemy` 를 부르지 않는다. 대기가 끝난 `Update` 에서 부른다.
- 금화는 주지 않는다. `Wallet` 을 쓰지 않는다. 성공·실패 문구나 페이드도 없다.
- `battle.Tick`, `battle.Step`, `battle.SetPaused` 를 부르지 않는다. 오브젝트를 그리지 않는다 (그리는 것은 `CombatView` 다).
- `Initialize` 가 불리기 전에는 `Update` 가 아무것도 하지 않는다.
- `.meta` 파일은 유니티가 화면 없이 실행될 때(테스트 실행) 자동으로 만든다. 만들어진 `.meta` 는 지우지 않는다.
- 이 작업에는 에딧 모드 테스트를 새로 쓰지 않는다 (테스트 어셈블리는 Core 만 참조한다). 플레이 모드 테스트 어셈블리도 만들지 않는다.
- 참고로 이 배치에서 강화 없이(공격력 10, 체력 100) 싸우면 약 21초 뒤 제자가 체력 30을 남기고 보스까지 모두 처치한다. 사람이 확인할 때의 기준이다.

## 완료 조건
- [컴파일] `GameRoot.cs` 와 `TempEncounter.cs` 가 컴파일 오류 없이 컴파일되고 기존 테스트가 모두 통과한다. (`BattleTests`, `BattleAttackTests`, `BattleUpgradeTests`, `EnemyStatsTests`, `CombatLayoutTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)
- [사람 확인] 게임을 실행하면 제자가 전진하다 주황색 몬스터를 만나 멈춰 싸우고, 맞은 쪽이 잠깐 하얗게 번쩍이며 머리 위 체력 바가 줄어든다. 쓰러진 몬스터는 바로 사라진다.
- [사람 확인] 두 번째와 세 번째 몬스터가 함께 붙으면 위아래로 조금 어긋나 겹쳐 보이고, 제자의 한 번 공격에 둘 다 번쩍인다.
- [사람 확인] 보라색 큰 보스는 머리 위 체력 바가 없고 제자가 다가올 때까지 움직이지 않는다. 보스까지 처치하면(시작 후 약 21초) 약 1초 뒤 제자가 처음 자리로 돌아가 체력이 가득 찬 채 같은 배치로 다시 싸운다.
- [사람 확인] 실행 중 Hierarchy 의 `[Game]` 아래에 `[TempEncounter]` 가 하나 있고, 여러 번 반복되는 동안 콘솔에 예외(빨간 줄)가 없으며 `[CombatView]` 아래에 적 오브젝트가 쌓이지 않는다.

## 하지 말 것
- `Assets/Scripts/Core/` 를 고치지 않는다. `Battle` 은 공개된 모양으로만 쓴다.
- `CombatView.cs`, `Assets/Scripts/Game/Currency/`, `Assets/Scripts/Game/StatUpgrade/`, `GameBootstrap.cs` 를 고치지 않는다.
- `GameRoot` 에는 `[TempEncounter]` 조립 줄만 추가하고 다른 기능의 줄은 고치지 않는다.
- 스테이지 기능(몬스터 10마리 배치, 보스 등장 시점, 성공·실패와 재도전, 금화 지급, 진행 바, 페이드)을 만들지 않는다. `docs/spec/stage.md` 의 것을 미리 구현하지 않는다.
- 강화 버튼을 눌러 보기 위한 확인용 금화 지급, 치트 키를 넣지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다. 화면은 코드로 조립한다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `PlumSword.Game.Combat.TempEncounter` (MonoBehaviour)를 만들었다. 클래스 위에 "스테이지 기능이 생기기 전까지만 쓰는 임시 코드다…" 주석을 달았다.
  - `Initialize(battle)`: `null` 이면 `ArgumentNullException`, `EnemyDefeated`·`DiscipleDefeated` 구독(OnDestroy 에서 해제), 바로 배치.
  - 배치: `Reset()` 뒤 `AddEnemy(Normal,1,6.0)`, `(Normal,1,9.0)`, `(Normal,1,9.3)`, `(Boss,1,15.0)` 순서.
  - 다시 놓기: `DiscipleDefeated` 또는 `EnemyDefeated` 때 `Enemies.Count == 0` 이면 대기 시작(이미 대기 중이면 무시). `Update` 에서 `Time.deltaTime` 으로 1.0초를 센 뒤 배치. 알림 처리 안에서는 `Reset`/`AddEnemy` 를 부르지 않는다. `Initialize` 전에는 `Update` 가 아무것도 하지 않는다.
  - `Tick`/`Step`/`SetPaused`/`Wallet` 은 쓰지 않는다.
- `GameRoot.Awake` 의 `CreateCombatView();` 뒤에 `CreateTempEncounter();` 한 줄(주석 "임시. 스테이지 기능을 구현할 때 지운다")을 더하고, `[Game]` 의 자식 `[TempEncounter]` 를 만드는 `CreateTempEncounter` 메서드를 추가했다.
- `docs/spec/combat.md` "연결 지점"의 `TempEncounter.Initialize` 주석에 예외 규칙 한 줄을 덧붙였다.

### 바뀐 파일
- `Assets/Scripts/Game/Combat/TempEncounter.cs` (새 파일)
- `Assets/Scripts/Game/Combat/TempEncounter.cs.meta` (유니티가 자동 생성)
- `Assets/Scripts/Game/GameRoot.cs`
- `docs/spec/combat.md`

### 가정
- `Initialize` 를 두 번 부르면 `CombatView` 와 같게 `InvalidOperationException` 을 던진다 (작업 문서에 없어 기존 스타일을 따름). 사양 "연결 지점" 주석에 적었다. 공개 모양은 바뀌지 않았다.
- 조립은 기존 `CreateXxxView` 패턴처럼 `Awake` 에 한 줄 호출 + 별도 private 메서드 `CreateTempEncounter` 로 했다. 지울 때 메서드와 호출 줄 둘 다 지운다(메서드 주석에 적음).
- 임시 수치(대기 1.0초, 스테이지 1, 좌표 6.0/9.0/9.3/15.0)는 작업 문서 값 그대로다.

### 사람이 확인할 것
- [사람 확인] 게임을 실행하면 제자가 전진하다 주황색 몬스터를 만나 멈춰 싸우고, 맞은 쪽이 잠깐 하얗게 번쩍이며 머리 위 체력 바가 줄어든다. 쓰러진 몬스터는 바로 사라진다.
- [사람 확인] 두 번째와 세 번째 몬스터가 함께 붙으면 위아래로 조금 어긋나 겹쳐 보이고, 제자의 한 번 공격에 둘 다 번쩍인다.
- [사람 확인] 보라색 큰 보스는 머리 위 체력 바가 없고 제자가 다가올 때까지 움직이지 않는다. 보스까지 처치하면(시작 후 약 21초) 약 1초 뒤 제자가 처음 자리로 돌아가 체력이 가득 찬 채 같은 배치로 다시 싸운다.
- [사람 확인] 실행 중 Hierarchy 의 `[Game]` 아래에 `[TempEncounter]` 가 하나 있고, 여러 번 반복되는 동안 콘솔에 예외(빨간 줄)가 없으며 `[CombatView]` 아래에 적 오브젝트가 쌓이지 않는다.
- 제자가 패배하는 경우(강화 없이는 일어나지 않음)의 다시 놓기는 실행해 보지 못했다.

### 테스트 결과
- `python tools/loop/run_unity_tests.py --project .` → EditMode 191개 중 191개 통과, 0개 실패. 컴파일 오류 없음 (Game 어셈블리 포함).
- 이 작업은 작업 문서 지시대로 새 테스트를 쓰지 않았다.


## 루프 기록 (2026-11-16 20:53)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/012_temp_encounter
- 루프 브랜치: loop/20261116-164139
