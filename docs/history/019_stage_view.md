# 019_stage_view — 스테이지 화면과 GameRoot 조립, 임시 적 배치 제거

## 목표
`PlumSword.Game` 에서 `GameRoot` 가 스테이지 로직 `StageProgress` 하나를 만들어 들고 있게 하고, 스테이지 화면 `StageView`(페이드 막, 스테이지 글자, 보스 이름, 진행 바 겸 보스 체력 바)를 만든다. 전투의 임시 적 배치 `TempEncounter` 는 지운다. 이것으로 스테이지 한 판이 실제 게임에서 돌아간다. 계산과 표시 판단은 Core 의 `StageProgress`, `StageText` 를 쓴다.

## 묶음
스테이지

## 선행 작업
- 003_currency_view
- 006_stat_upgrade_view
- 011_combat_view
- 012_temp_encounter
- 013_stage_rewards_text
- 018_stage_progress_fade

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다" 절, "`.meta` 파일" 절)
- docs/dev/art.md (UI 기준 해상도)
- docs/spec/stage.md ("연결 지점" 절의 `GameRoot.StageProgress`, `StageView`, "화면" 절)
- docs/spec/combat.md ("연결 지점" 절의 `TempEncounter`, "화면" 절의 "조립")
- docs/spec/currency.md ("연결 지점" 절의 `GameRoot.UiCanvas`, "화면" 절의 공용 UI 뿌리와 TextMeshPro)

## 요구사항
- 선행 작업이 만든 것을 쓴다. 내부는 고치지 않는다.
  - 003_currency_view, 006_stat_upgrade_view, 011_combat_view, 012_temp_encounter: `PlumSword.Game.GameRoot` 의 `Wallet`, `UiCanvas`(공용 UI 캔버스), `StatUpgrades`, `Battle` 속성. `GameRoot.Awake` 는 `Wallet` → `StatUpgrades` → `Battle` 을 만들고 `[Canvas]` → `[EventSystem]` → `[CurrencyView]` → `[StatUpgradeView]` → 카메라 → `[CombatView]` → `[TempEncounter]` 를 조립해 둔 상태다. `CombatView` 가 매 프레임 `Battle.Tick` 을 부르고 `Battle.EnemyAdded` 에 맞춰 적 오브젝트를 만든다. 글자를 만드는 방식(TextMeshPro, 기본 리소스가 없을 때의 처리)은 `Assets/Scripts/Game/Currency/CurrencyView.cs` 를 읽고 같은 방식으로 한다.
  - 018_stage_progress_fade 까지: `PlumSword.Core.Stage.StageProgress` — `new StageProgress(Wallet wallet, StatUpgrades upgrades, Battle battle, System.Random random, int startStage = 1)`, `void Begin()`, `void Tick(double deltaSeconds)`, `int StageNumber { get; }`, `bool IsBossActive { get; }`, `double BarRatio { get; }`(0.0 ~ 1.0), `double FadeAlpha { get; }`(0.0 ~ 1.0), `event System.Action Changed`.
  - 013_stage_rewards_text: `PlumSword.Core.Stage.StageText` — `string StageLabel(int stage)`, `const string BossName`.
- 고칠 파일: `Assets/Scripts/Game/GameRoot.cs`. 만들 파일: `Assets/Scripts/Game/Stage/StageView.cs` (네임스페이스 `PlumSword.Game.Stage`). 지울 파일: `Assets/Scripts/Game/Combat/TempEncounter.cs` 와 `Assets/Scripts/Game/Combat/TempEncounter.cs.meta`.
- `Assets/Scripts/Game/Stage/` 폴더를 새로 만든다. `.meta` 파일은 유니티가 화면 없이 실행될 때(테스트 실행) 자동으로 만든다. 만들어진 `.meta` 는 지우지 않는다.
- 공개 모양은 사양의 "연결 지점" 절 그대로다.

  ```csharp
  namespace PlumSword.Game
  {
      public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래 하나를 더한다
      {
          public PlumSword.Core.Stage.StageProgress StageProgress { get; }   // 밖에서는 읽기만 (private set)
      }
  }

  namespace PlumSword.Game.Stage
  {
      public sealed class StageView : MonoBehaviour
      {
          public void Initialize(PlumSword.Core.Stage.StageProgress stage, UnityEngine.Canvas canvas);
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/stage.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 이름이 겹치는 것에 주의한다: `PlumSword.Game` 안에서 `Stage` 라고만 쓰면 네임스페이스 `PlumSword.Game.Stage` 로 읽히고, `GameRoot` 안에서 `StageProgress` 는 속성 이름이기도 하다. 컴파일 오류가 나면 전체 이름(`PlumSword.Core.Stage.StageProgress`)이나 `using` 별칭을 쓴다. Core 의 `double` 값은 `float` 로 바꿔서 쓴다.
- **`TempEncounter` 제거**: 클래스 파일과 그 `.meta` 를 지우고, `GameRoot.Awake` 에서 `[TempEncounter]` 를 만드는 줄(주석 포함)을 지운다. 사양 `docs/spec/combat.md` 가 "스테이지 기능을 구현할 때 지운다"고 정한 것이다. `docs/spec/combat.md` 에서 `TempEncounter` 를 설명하는 곳("연결 지점" 절의 클래스, "화면" 절의 "조립"과 "임시 적 배치")에는 내용을 지우지 말고 "스테이지 기능 구현(019_stage_view)으로 지워졌다. 적 배치는 `StageProgress` 가 한다 (`docs/spec/stage.md`)"는 한 줄을 덧붙인다.
- **`GameRoot.Awake` 에 더하는 것**: `[CombatView]` 를 조립한 줄 뒤, `[TempEncounter]` 줄이 있던 자리에서 아래를 순서대로 한다.
  1. `StageProgress = new StageProgress(Wallet, StatUpgrades, Battle, new System.Random())` 로 하나 만든다. 게임에 `StageProgress` 는 이것 하나뿐이다. 난수는 시드 없는 `new System.Random()` 이다.
  2. `[Game]` 의 자식 `[StageView]` 를 만들어 `StageView` 를 붙이고 `Initialize(StageProgress, UiCanvas)` 를 부른다.
  3. `StageProgress.Begin()` 을 부른다. 화면들(`CombatView`, `StageView`)이 알림을 이미 구독한 뒤여야 몬스터가 보이기 때문에 반드시 맨 마지막이다. `Begin` 을 부르는 것은 게임 전체에서 `GameRoot` 하나뿐이다.
- **`StageView.Initialize(stage, canvas)`**:
  - `stage` 나 `canvas` 가 `null` 이면 `System.ArgumentNullException`.
  - `canvas` 아래에 페이드 막, 스테이지 글자, 보스 이름, 진행 바를 만들고 지금 상태로 바로 한 번 그린다.
  - `stage.Changed` 를 구독하고 `OnDestroy` 에서 끊는다.
- **`Update`**: `stage.Tick(Time.deltaTime)` 을 부른 뒤 페이드 막 색의 알파를 `stage.FadeAlpha` 로 맞춘다. `Tick` 을 부르는 것은 게임 전체에서 `StageView` 하나뿐이다. `Initialize` 가 불리기 전에는 `Update` 가 아무것도 하지 않는다.
- **페이드 막**: 캔버스를 꽉 채우는 검은색 `Image` (앵커 (0, 0) ~ (1, 1), 여백 0). 캔버스의 **첫 번째 자식**으로 둔다 (`SetAsFirstSibling`). 그래서 전투 화면만 가리고 보유 금화, 강화 버튼, 스테이지 글자, 진행 바는 막 위에 그대로 보인다. `raycastTarget = false`. 색은 (0, 0, 0, `FadeAlpha`). 처음 알파는 0 이다.
- **스테이지 글자**: 화면 위쪽 가운데. 앵커 (0.5, 1), 피벗 (0.5, 1), 위치 (0, −8), 크기 200×16 (임시값). 글자는 `StageText.StageLabel(stage.StageNumber)` 그대로다 (예: `STAGE 1`). `TextMeshProUGUI`, 글자 크기 16 (임시값), 흰색, 가운데 정렬, TMP 기본 글꼴, `raycastTarget = false`.
- **보스 이름**: 스테이지 글자 아래, 바 위. 앵커 (0.5, 1), 피벗 (0.5, 1), 위치 (0, −26), 크기 200×16 (임시값). 글자는 `StageText.BossName`, 글자 크기 14 (임시값), 흰색, 가운데 정렬, `raycastTarget = false`. `stage.IsBossActive` 가 `true` 일 때만 보인다 (오브젝트를 켜고 끈다).
- **진행 바**: 보스 이름 아래. 앵커 (0.5, 1), 피벗 (0.5, 1), 위치 (0, −44), 크기 300×12 (임시값). 바탕은 `Image` RGB (0.15, 0.15, 0.15). 채움은 바탕의 자식 `Image` 로 왼쪽 끝이 고정되고 가로 길이가 `300 × stage.BarRatio` 다 (앵커 (0, 0) ~ (`BarRatio`, 1), 여백 0). 둘 다 `raycastTarget = false`. 비율은 `stage.BarRatio` 를 그대로 쓰고 화면 코드에서 따로 계산하지 않는다.
  - `stage.IsBossActive` 가 `false`: 일반 몬스터 진행 바. 채움 색 노랑 RGB (0.95, 0.80, 0.25) (임시값). 한 마리당 10% 씩 찬다.
  - `stage.IsBossActive` 가 `true`: 보스 체력 바. 채움 색 빨강 RGB (0.85, 0.20, 0.20) (임시값). 보스가 등장한 순간 가득 찬 빨간 바로 바뀐다.
  - 보스를 처치했거나 실패한 뒤에도 가려진 순간의 교체까지는 `IsBossActive` 와 `BarRatio` 가 그대로이므로 화면도 그대로다. 교체 때 0% 의 일반 진행 바로 돌아간다. 화면 코드는 이것을 따로 판단하지 않고 `IsBossActive`, `BarRatio` 만 읽는다.
- **바뀌는 때**: 글자·보스 이름·바는 `Initialize` 에서 한 번, 그 뒤로 `stage.Changed` 가 올 때마다 다시 그린다. 매 프레임 다시 그리지 않는다. 페이드 막의 진하기만 매 프레임 `Update` 에서 `Tick` 을 부른 뒤 맞춘다.
- **누르는 것**: 없다. 이 화면은 입력을 받지 않는다. 페이드 중 강화 버튼이 눌리지 않고 어둡게 보이는 것은 `StageProgress` 가 거는 강화 잠금과 강화 화면이 이미 한다. `StageView` 는 `SetLocked`, `SetPaused` 를 부르지 않는다.
- **연출**: 페이드 말고는 없다. 성공·실패 글자, 보스 등장 연출, 소리는 없다.
- TextMeshPro 기본 리소스(TMP Essentials)가 없어도 예외로 게임이 멈추지 않아야 한다 (`CurrencyView` 와 같은 방식).
- 이 작업에는 에딧 모드 테스트를 새로 쓰지 않는다 (테스트 어셈블리는 Core 만 참조한다). 표시 글자, 바 비율, 페이드 진하기의 판정은 앞 작업의 `StageTextTests`, `StageProgressKillTests`, `StageProgressFadeTests` 가 이미 한다. 플레이 모드 테스트 어셈블리도 만들지 않는다.
- 참고로 강화 없이(공격력 10, 체력 100) 1스테이지의 몬스터 10마리와 보스를 모두 이기기는 어렵다. 처치 금화로 강화하며 재도전하는 것이 정상 흐름이다. 확인용 금화 지급이나 치트 키를 넣지 않는다.

## 완료 조건
- [컴파일] `Assets/Scripts/Game/Combat/TempEncounter.cs` 가 없고 `GameRoot.cs` 에 `TempEncounter` 라는 글자가 없으며, `GameRoot.cs` 와 `StageView.cs` 가 컴파일 오류 없이 컴파일되고 기존 테스트가 모두 통과한다. (`StageRewardsTests`, `StageTextTests`, `StageLayoutTests`, `StageProgressTests`, `StageProgressKillTests`, `StageProgressOutcomeTests`, `StageProgressFadeTests`, `BattleTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)
- [사람 확인] 게임을 실행하면 화면 위쪽 가운데에 `STAGE 1` 과 빈 노란 진행 바가 보이고, 제자 앞에 몬스터 무리가 보인다. 몬스터를 처치할 때마다 금화가 10씩 늘고 바가 10% 씩 찬다. 다시 실행하면 무리 구성이 달라진다.
- [사람 확인] 10마리를 모두 처치하면 바가 가득 찬 빨간 바로 바뀌고 그 위에 `GATEKEEPER` 가 보인다. 제자가 달려가 화면 밖에 있던 보라색 보스와 싸우고, 보스가 맞을 때마다 빨간 바가 줄어든다.
- [사람 확인] 보스를 처치하면 금화가 50 늘고 전투 화면이 약 0.5초 동안 검게 가려졌다가 약 0.5초 동안 열리며 `STAGE 2` 가 된다. 가려진 동안에도 보유 금화, 강화 버튼, 스테이지 글자, 바는 보이고, 강화 버튼은 어둡고 눌리지 않는다. 열린 뒤 바는 빈 노란 바이고 제자의 체력은 가득하다.
- [사람 확인] 제자의 체력이 0이 되면 같은 방식으로 가려졌다가 같은 스테이지 번호로 처음부터 다시 시작한다. 금화와 강화 단계는 그대로다.
- [사람 확인] 실행 중 Hierarchy 의 `[Game]` 아래에 `[StageView]` 가 하나 있고 `[TempEncounter]` 는 없으며, 스테이지가 여러 번 바뀌는 동안 콘솔에 예외(빨간 줄)가 없다.

## 하지 말 것
- `Assets/Scripts/Core/` 를 고치지 않는다. `StageProgress`, `StageText`, `Battle`, `Wallet`, `StatUpgrades` 는 공개된 모양으로만 쓴다. 로직을 바꿔야 하면 고치지 말고 보고서의 가정에 적는다.
- `CombatView.cs`, `Assets/Scripts/Game/Currency/`, `Assets/Scripts/Game/StatUpgrade/`, `GameBootstrap.cs` 를 고치지 않는다.
- `GameRoot` 에는 자기 기능의 줄(`StageProgress` 속성과 생성, `[StageView]` 조립, `Begin()` 호출)만 추가하고, 다른 기능의 줄은 `[TempEncounter]` 조립 줄을 지우는 것 말고는 고치지 않는다.
- 화면 코드에서 처치 수, 금화, 성공·실패, 페이드 시간을 계산하지 않는다. `StageProgress` 의 상태를 읽어 그리기만 한다. `StageView` 에서 `Begin`, `battle.Reset`, `AddEnemy`, `SetPaused`, `SetLocked` 를 부르지 않는다.
- UI 까지 가리는 화면 전체 페이드, 성공·실패 글자나 결과 창, 보스 등장 연출, 소리, 한글 표기와 한글 글꼴, 수동 재도전 버튼, 스테이지 선택을 만들지 않는다. 사양의 "범위 밖"이다.
- 저장·불러오기, 오프라인 보상을 만들지 않는다. 2단계에서 한다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다. 화면은 코드로 조립한다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `GameRoot` 에 `StageProgress` 속성(`private set`)을 더하고, `Awake` 의 `[CombatView]` 조립 다음(`[TempEncounter]` 줄이 있던 자리)에서 `CreateStage()` 로 `StageProgress` 생성 → `[StageView]` 조립·`Initialize(StageProgress, UiCanvas)` → `StageProgress.Begin()` 을 순서대로 한다.
- `PlumSword.Game.Stage.StageView` 를 만들었다. 캔버스 첫 번째 자식으로 검은 페이드 막(`SetAsFirstSibling`, 알파 0 에서 시작), 위쪽 가운데에 스테이지 글자·보스 이름(TMP)·진행 바(바탕 + 왼쪽 고정 채움)를 만든다. `Initialize` 에서 한 번, `stage.Changed` 마다 글자·보스 이름 표시 여부·바 비율·바 색을 다시 그리고, `Update` 에서 `stage.Tick(Time.deltaTime)` 뒤 막 알파만 `FadeAlpha` 로 맞춘다. `null` 인자는 `ArgumentNullException`, `OnDestroy` 에서 구독을 끊는다. TMP 생성·글자 설정은 `CurrencyView` 처럼 try/catch 로 감싸 경고 한 번만 남긴다.
- `TempEncounter.cs` 와 `.meta` 를 지우고 `GameRoot` 의 조립 메서드·호출 줄을 지웠다.
- `docs/spec/combat.md` 의 `TempEncounter` 설명 세 곳("연결 지점" 코드 블록 뒤, "화면"의 "조립", "임시 적 배치")에 "019_stage_view 로 지워졌다. 적 배치는 `StageProgress` 가 한다" 한 줄을 덧붙였다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs` (수정)
- `Assets/Scripts/Game/Stage/StageView.cs` (새 파일), `Assets/Scripts/Game/Stage/StageView.cs.meta`, `Assets/Scripts/Game/Stage.meta` (유니티가 자동 생성)
- `Assets/Scripts/Game/Combat/TempEncounter.cs`, `Assets/Scripts/Game/Combat/TempEncounter.cs.meta` (삭제)
- `docs/spec/combat.md` (TempEncounter 제거 안내 한 줄씩)

### 가정
- `BarRatio`, `FadeAlpha` 를 `float` 로 바꿀 때 `Mathf.Clamp01` 로 0~1 을 한 번 더 막았다. Core 가 이미 0~1 을 보장하므로 값은 같고, 비율 계산을 따로 하는 것은 아니다.
- 채움 `Image` 의 피벗은 (0, 0.5) 로 두었다 (앵커로 길이를 정하므로 표시에는 영향 없음).
- 보스 이름 글자는 만들 때 한 번만 `StageText.BossName` 으로 정하고, 이후에는 오브젝트를 켜고 끄기만 한다. 처음 상태(`IsBossActive == false`)이면 `Initialize` 의 첫 그리기에서 꺼진다.
- TMP 오브젝트 이름은 `StageLabel`, `BossName`, 막은 `StageFade`, 바는 `StageBar`/`Fill` 로 정했다.
- 생성과 조립은 `GameRoot` 의 private 메서드 `CreateStage()` 하나로 묶었다 (다른 기능의 `CreateXxxView` 와 같은 스타일). 공개 모양은 사양 그대로라 "연결 지점" 절은 고치지 않았다.

### 사람이 확인할 것
- [사람 확인] 게임을 실행하면 화면 위쪽 가운데에 `STAGE 1` 과 빈 노란 진행 바가 보이고, 제자 앞에 몬스터 무리가 보인다. 몬스터를 처치할 때마다 금화가 10씩 늘고 바가 10% 씩 찬다. 다시 실행하면 무리 구성이 달라진다.
- [사람 확인] 10마리를 모두 처치하면 바가 가득 찬 빨간 바로 바뀌고 그 위에 `GATEKEEPER` 가 보인다. 제자가 달려가 화면 밖에 있던 보라색 보스와 싸우고, 보스가 맞을 때마다 빨간 바가 줄어든다.
- [사람 확인] 보스를 처치하면 금화가 50 늘고 전투 화면이 약 0.5초 동안 검게 가려졌다가 약 0.5초 동안 열리며 `STAGE 2` 가 된다. 가려진 동안에도 보유 금화, 강화 버튼, 스테이지 글자, 바는 보이고, 강화 버튼은 어둡고 눌리지 않는다. 열린 뒤 바는 빈 노란 바이고 제자의 체력은 가득하다.
- [사람 확인] 제자의 체력이 0이 되면 같은 방식으로 가려졌다가 같은 스테이지 번호로 처음부터 다시 시작한다. 금화와 강화 단계는 그대로다.
- [사람 확인] 실행 중 Hierarchy 의 `[Game]` 아래에 `[StageView]` 가 하나 있고 `[TempEncounter]` 는 없으며, 스테이지가 여러 번 바뀌는 동안 콘솔에 예외(빨간 줄)가 없다.
- 페이드 막이 캔버스 첫 자식이라 다른 UI 위를 덮지 않는지(강화 버튼·금화 글자가 막 위에 보이는지) 눈으로 확인. 다른 화면이 나중에 `SetAsFirstSibling` 을 쓰면 순서가 바뀔 수 있다.

### 테스트 결과
- `python tools/loop/run_unity_tests.py`: EditMode 269개 중 269개 통과, 0개 실패. 컴파일 오류 없음 (`GameRoot.cs`, `StageView.cs` 포함).
- `Assets/` 에서 `TempEncounter` 글자 검색 결과 없음.
- 새 테스트는 작업 문서 지시대로 쓰지 않았다.


## 루프 기록 (2026-11-16 21:12)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/019_stage_view
- 루프 브랜치: loop/20261116-205758
