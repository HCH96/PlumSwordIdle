# 074_flow_test_hooks — 흐름 테스트용 저장소 연결과 다시 켜기 도우미

## 목표
플레이 모드 테스트가 실제 `GameRoot` 를 "저장 → 종료 → 다시 켜기"로 돌려 볼 수 있게, 배치 모드 저장소를 테스트가 넘길 수 있는 연결(Game)과 흐름 테스트들이 함께 쓸 도우미(플레이 모드 테스트)를 만든다. 뒤 작업(075~077)의 흐름 테스트가 이것을 쓴다.

## 묶음
flow_test

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다", "테스트" 절)
- docs/spec/save.md ("동작"의 "불러오기"·"저장소", "연결 지점"의 `GameRoot` 부분)
- docs/spec/start_menu.md ("연결 지점"의 `StartEntry`, `GameRoot` 시작 순서)

## 요구사항
- 왜 필요한가: 지금 배치 모드(루프와 명령줄 테스트)의 `GameRoot.Awake` 는 매번 새 `MemorySaveStore` 를 만들고 `SaveCodec.Decode(null)`(`Empty`)로 시작한다. 그래서 테스트가 게임을 다시 만들면 저장한 내용이 사라져 "저장 → 다시 켜기"를 검사할 수 없다. 또 `GameRoot.Restart()` 는 다음 시작을 `ResumeInGame` 등으로 정해 버려, 테스트가 처음 켤 때(`Launch`)와 같은 상태로 되돌릴 방법이 없다.
- `Assets/Scripts/Game/GameRoot.cs` 에 아래 둘을 더한다. 다른 줄의 동작은 바꾸지 않는다.
  - `internal static PlumSword.Core.Save.MemorySaveStore BatchSaveStore;` — 기본값 `null`. 배치 모드일 때만 쓴다.
    - `Awake` 의 배치 모드 분기를 이렇게 바꾼다: `BatchSaveStore` 가 `null` 이면 지금과 똑같이 새 `MemorySaveStore` 와 `SaveCodec.Decode(null)`. `null` 이 아니면 `saveStore = BatchSaveStore`, `LoadResult = SaveCodec.Decode(BatchSaveStore.Text)` (글자가 없으면 `Decode(null)` 과 같아 `Empty`).
    - 배치 모드가 아니면(에디터 플레이, 빌드) `BatchSaveStore` 를 보지 않는다. 지금처럼 `FileSaveStore` 를 쓴다.
  - `internal void RecreateAs(PlumSword.Core.StartMenu.StartEntry entry)` — 이미 다시 만드는 중(`restarting`)이면 아무것도 하지 않는다. 아니면 `restarting = true`, `NextEntry = entry` 로 두고 `Restart()`·`ReturnToTitle()` 과 같은 `Recreate()` 를 부른다. 테스트 전용이다. 게임 코드에서 부르지 않는다.
- `Assets/Scripts/Game/AssemblyInfo.cs` 를 새로 만든다: `[assembly: InternalsVisibleTo("PlumSword.Game.Tests")]` 한 줄과, Core 의 `Assets/Scripts/Core/AssemblyInfo.cs` 처럼 이유를 적은 주석. 어셈블리 정의(`.asmdef`)는 고치지 않는다.
- 플레이 모드 테스트 도우미 `Assets/Tests/PlayMode/Flow/GameFlow.cs` (네임스페이스 `PlumSword.Game.Tests.Flow`, `public static class GameFlow`). 메서드 모양은 아래와 같고, 뒤 작업이 이 이름 그대로 쓴다.
  - `public static GameRoot FindRoot()` — 켜져 있는 `GameBootstrap.RootName` 오브젝트의 `GameRoot`. 없으면 `null`.
  - `public static IEnumerator Relaunch(MemorySaveStore store, StartEntry entry)` — `GameRoot.BatchSaveStore = store` 로 두고, 지금 루트에서 `RecreateAs(entry)` 를 부른 뒤, 지금 루트와 다른 새 `GameRoot` 가 켜질 때까지 프레임마다 기다린다. 새 루트가 생긴 뒤 3프레임 더 기다린다 (첫 `Flush` 가 끝나도록). 300프레임 안에 새 루트가 안 생기면 `Assert.Fail`.
  - `public static IEnumerator ResetToFreshLaunch()` — `Time.timeScale = 1f` 로 되돌리고, `Relaunch(null, StartEntry.Launch)` 를 하고, 끝나면 `GameRoot.BatchSaveStore = null` 로 둔다. 결과는 배치 모드에서 처음 켠 게임과 같다 (`Entry` `Launch`, `LoadResult` `Empty`, 1스테이지 시작).
  - `public static IEnumerator PowerUp(GameRoot root, int levels)` — 공격력과 체력을 각각 `levels` 단계 올린다. 한 단계마다 `root.StatUpgrades.Locked` 가 `false` 가 될 때까지 프레임을 기다린 뒤(최대 600프레임, 넘으면 `Assert.Fail`), `root.Wallet.Add(root.StatUpgrades.GetCost(stat))` 로 그 비용만큼 주고 `TryUpgrade(stat)` 가 `true` 인지 `Assert` 한다. 그래서 끝나면 금화는 시작 때와 같다. 내공은 올리지 않는다.
  - `public static IEnumerator WaitUntil(Func<bool> condition, float maxRealSeconds, Func<string> describe)` — 조건이 참이 될 때까지 프레임마다 기다린다. `Time.realtimeSinceStartup` 기준으로 `maxRealSeconds` 를 넘기면 `Assert.Fail(describe())` 로 그때 상태(스테이지 번호, 구간, 단계 등)를 메시지에 남긴다.
- 흐름 테스트 규칙 (도우미 파일 맨 위 주석에도 적는다): `[Game]` 루트는 테스트 사이에 남으므로, 흐름 테스트는 `[UnityTearDown]` 에서 반드시 `GameFlow.ResetToFreshLaunch()` 를 돌려 다음 테스트가 처음 켠 게임을 보게 한다. 테스트가 실패해도 TearDown 은 돈다.
- 도우미 자체의 테스트 `Assets/Tests/PlayMode/Flow/GameFlowTests.cs` (`PlumSword.Game.Tests.Flow.GameFlowTests`). SetUp 은 기존 `GameRootSaveTests` 처럼 `SceneManager.LoadSceneAsync(0)` 후 3프레임 기다린다. TearDown 은 위 규칙대로 `ResetToFreshLaunch()`.
- 문서:
  - `docs/spec/save.md` "동작"의 "저장소" 항목에 배치 모드의 `BatchSaveStore` 규칙을 한 줄 더하고, "연결 지점"의 `GameRoot` 부분에 `BatchSaveStore` 와 `RecreateAs` 를 "테스트 전용(internal)"이라고 적는다. 다른 절과 `## 변경 기록` 은 고치지 않는다.
  - `docs/dev/architecture.md` "테스트" 절에 한 줄: 흐름 테스트는 `Assets/Tests/PlayMode/Flow/` 에 두고 `GameFlow` 도우미를 쓰며, TearDown 에서 `ResetToFreshLaunch()` 를 부른다.
  - 구현하다 위 메서드 모양이 달라지면 사양 문서의 연결 지점 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- [플레이 모드] `Assets/Tests/PlayMode/Flow/GameFlowTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- [플레이 모드] 저장 글자 `SaveCodec.Encode(new SaveData(500, 3, 2, 1, 1, 3, 3, DateTime.UtcNow.Ticks, 5))` 를 쓴 `MemorySaveStore` 로 `Relaunch(store, StartEntry.Launch)` 하면 새 루트의 `LoadResult.Status` 는 `Loaded` 다.
- [플레이 모드] 같은 상황에서 새 루트의 `Wallet.Balance` 는 500 이다.
- [플레이 모드] 같은 상황에서 새 루트의 공격력·체력·내공 단계는 3, 2, 1 이다.
- [플레이 모드] 같은 상황에서 새 루트의 `Realm.Current` 는 `RealmLevel.SecondRate`, `Realm.Kills` 는 5 다.
- [플레이 모드] 같은 상황에서 새 루트의 `StageProgress.StageNumber` 는 3 이고 `HasBegun` 은 `true` 다.
- [플레이 모드] 같은 상황에서 새 루트의 `OfflineReward.HasReward` 는 `false` 다 (저장 시각이 방금이므로).
- [플레이 모드] 같은 상황에서 `Relaunch` 가 끝난 뒤 `SaveCodec.Decode(store.Text).Data.StageNumber` 는 3 이다 (새 루트의 자동 저장이 같은 저장소에 썼다).
- [플레이 모드] `Relaunch` 뒤 켜져 있는 `GameRoot` 는 하나뿐이고, 이전 루트 오브젝트는 지워졌다.
- [플레이 모드] 3스테이지 저장으로 다시 켠 뒤 `ResetToFreshLaunch()` 를 하면 `Entry` 는 `Launch`, `LoadResult.Status` 는 `Empty`, `StageProgress.StageNumber` 는 1, `GameRoot.BatchSaveStore` 는 `null` 이다.
- [플레이 모드] 처음 켠 게임에서 `PowerUp(root, 10)` 을 하면 공격력과 체력 단계는 각각 10 이고, `Wallet.Balance` 는 시작 때와 같다.
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다 (`BatchSaveStore` 가 `null` 일 때 배치 모드 시작이 지금과 같으므로 `GameRootSaveTests`·`GameRootOfflineRewardTests`·`GameRootStartMenuTests` 가 그대로 통과한다).

## 하지 말 것
- `GameRoot` 의 다른 조립 순서와 다른 기능의 줄을 바꾸지 않는다. 위 두 가지(`BatchSaveStore`, `RecreateAs`)만 더한다.
- 배치 모드가 아닐 때의 동작(`FileSaveStore`, 시작 화면)을 바꾸지 않는다.
- `PlumSword.Core` 의 코드를 고치지 않는다.
- 어셈블리 정의(`.asmdef`)를 만들거나 고치지 않는다. 씬·프리팹·에셋 파일을 고치지 않는다.
- 진행·저장·오프라인 보상의 흐름 테스트 자체는 만들지 않는다 (075·076·077 이 한다).
- `Assets/Tests/PlayMode/Capture/` 를 고치지 않는다.

## 보고서

### 한 일
- 배치 모드 저장소 주입과 지정한 시작 방식의 테스트 전용 루트 재생성을 추가했다.
- 흐름 테스트용 재실행·초기화·강화·조건 대기 도우미와 플레이 모드 검증을 추가하고 사양과 테스트 규칙을 갱신했다.
- 재생성 중 씬 메인 카메라의 태그를 잠시 감춰 새 루트가 자체 카메라를 소유하도록 하고, 씬 재로드 후 카메라가 유지되는 회귀 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs`, `Assets/Scripts/Game/AssemblyInfo.cs` 및 메타 파일
- `Assets/Tests/PlayMode/Flow/` 도우미·테스트와 폴더 및 파일 메타 파일
- `docs/spec/save.md`, `docs/dev/architecture.md`
- `tasks/doing/074_flow_test_hooks.md`, `.loop/result.json`

### 가정
- 흐름 테스트의 재생성 루트는 다음 테스트의 씬 교체를 견디도록 자체 카메라를 사용한다. 외부 메인 카메라 태그는 재생성 후 복구한다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 컴파일과 플레이 모드 테스트는 지시에 따라 직접 실행하지 않았으며 루프의 판정을 기다린다.


## 루프 기록 (2026-11-22 09:21)

- 작업 에이전트: codex(시도 1~2)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/074_flow_test_hooks
- 루프 브랜치: loop/20261122-091339
