# 031_save_views — 저장 실패 글자와 손상 오류 창 화면

## 목표
`PlumSword.Game.Save` 에 화면 코드 둘을 만든다. `SaveView` 는 매 프레임 자동 저장의 `Tick`·`Flush` 를 부르고, 끌 때·백그라운드로 갈 때 `SaveNow` 를 부르며, 쓰기 실패 중에 `SAVE FAILED` 글자를 보여 준다. `SaveErrorView` 는 손상 오류 창을 만들고 `NEW GAME` 을 누르면 받은 동작을 한 번 부른 뒤 사라진다. `GameRoot` 에 붙이는 것은 032 가 한다.

## 묶음
저장

## 선행 작업
- 026_save_data
- 029_auto_save_retry

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/dev/art.md
- docs/spec/save.md ("동작" 절의 "화면 코드의 갱신"·"손상", "수치" 절의 "표시 글자" 표, "연결 지점" 절의 `SaveView`·`SaveErrorView`·`AutoSave`·`SaveText`, "화면" 절 전체)

## 요구사항
- 새 파일 (네임스페이스 `PlumSword.Game.Save`): `Assets/Scripts/Game/Save/SaveView.cs`, `Assets/Scripts/Game/Save/SaveErrorView.cs`.
- 새 테스트 파일 (네임스페이스 `PlumSword.Game.Tests.Save`): `Assets/Tests/PlayMode/Save/SaveViewTests.cs`, `Assets/Tests/PlayMode/Save/SaveErrorViewTests.cs`. 테스트는 자기 캔버스(`new GameObject` + `Canvas`)와 자기 Core 객체를 만들어 쓰고 `[TearDown]` 에서 만든 오브젝트를 지운다. `[Game]` 루트와 그 안의 객체는 건드리지 않는다. 쓰기 결과를 바꿀 수 있는 가짜 저장소는 테스트 클래스 안의 `private sealed class` 로 둔다 (`ISaveStore` 구현, 시도 횟수 기록, 성공 여부를 바꿀 수 있음). 플레이 모드 테스트 어셈블리는 TextMeshPro 를 참조하지 않으므로 오브젝트 이름·`activeSelf`·`Image`·`Button`(uGUI) 으로만 검사한다.
- 선행 작업이 만든 것을 쓴다: 029 까지 완성된 `PlumSword.Core.Save.AutoSave`(`HasFailed`, `event Action<bool> FailedChanged`, `Tick(double)`, `Flush(long)`, `SaveNow(long)`, 생성자 `AutoSave(ISaveStore, Wallet, StatUpgrades, StageProgress, bool enabled = true)`), 026 의 `SaveText`(`SaveFailed`, `CorruptTitle`, `CorruptMessage`, `NewGameButton`).
- **공개 모양** (사양 "연결 지점" 그대로):

  ```csharp
  namespace PlumSword.Game.Save
  {
      public sealed class SaveView : MonoBehaviour
      {
          // canvas 아래에 저장 실패 글자를 만들고 autoSave.HasFailed 로 바로 표시한다.
          // autoSave.FailedChanged 를 구독하고 OnDestroy 에서 끊는다.
          // LateUpdate: Tick(Time.unscaledDeltaTime) → Flush(DateTime.UtcNow.Ticks).
          // OnApplicationPause(true), OnApplicationQuit: SaveNow(DateTime.UtcNow.Ticks).
          public void Initialize(PlumSword.Core.Save.AutoSave autoSave, UnityEngine.Canvas canvas);
      }

      public sealed class SaveErrorView : MonoBehaviour
      {
          // canvas 아래에 손상 오류 창을 만든다. NEW GAME 을 누르면 onNewGame 을 한 번 부르고 자기 오브젝트를 없앤다.
          // onNewGame 이 null 이면 ArgumentNullException.
          public void Initialize(UnityEngine.Canvas canvas, System.Action onNewGame);
      }
  }
  ```

  - 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/save.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- **`SaveView`**
  - `Initialize` 전에는 `LateUpdate` 등에서 아무것도 하지 않는다 (`autoSave` 가 없으면 건너뜀). `autoSave`, `canvas` 가 `null` 이면 `ArgumentNullException`.
  - 저장 실패 글자: 캔버스 아래에 이름 `SaveFailedText` 오브젝트. `RectTransform` 앵커 (1, 1), 피벗 (1, 1), 위치 (−8, −62), 크기 160×16 (임시값). `TextMeshProUGUI`, 글자 `SaveText.SaveFailed`, 글자 크기 14 (임시값), 색 RGB (1.0, 0.35, 0.35) (임시값), 오른쪽 정렬, TMP 기본 글꼴, `raycastTarget = false`.
  - 보이기: `autoSave.HasFailed` 가 `true` 일 때만 `SaveFailedText` 가 켜진다 (`SetActive`). `Initialize` 에서 한 번 맞추고, 그 뒤로 `FailedChanged` 가 올 때마다 맞춘다. `OnDestroy` 에서 `FailedChanged` 구독을 끊는다.
  - `LateUpdate`: `autoSave.Tick(Time.unscaledDeltaTime)` 다음 `autoSave.Flush(DateTime.UtcNow.Ticks)`.
  - `OnApplicationPause(bool paused)`: `paused` 가 `true` 일 때만 `autoSave.SaveNow(DateTime.UtcNow.Ticks)`. `OnApplicationQuit`: `autoSave.SaveNow(DateTime.UtcNow.Ticks)`. 끌 때 실패하면 더 하지 않는다.
- **`SaveErrorView`**
  - `onNewGame` 이 `null` 이면 `ArgumentNullException`. `canvas` 가 `null` 이면 `ArgumentNullException`.
  - 캔버스의 **마지막 자식**으로 이름 `SaveErrorPanel` 오브젝트를 만든다 (`SetAsLastSibling`). 이것이 가림막이다: 캔버스를 꽉 채움(앵커 (0, 0)~(1, 1), 오프셋 0), 검은색 알파 0.8 `Image` (임시값), `raycastTarget = true` (뒤의 강화 버튼이 눌리지 않는다).
  - `SaveErrorPanel` 의 자식 `SaveErrorWindow`: 앵커·피벗 (0.5, 0.5), 위치 (0, 0), 크기 360×140 (임시값), 바탕 `Image` RGB (0.12, 0.12, 0.12) (임시값).
  - `SaveErrorWindow` 의 자식 (위치는 창 가운데 기준, 글자는 모두 `TextMeshProUGUI`, TMP 기본 글꼴):
    - `Title`: 위치 (0, 40), 크기 340×24, 글자 `SaveText.CorruptTitle`, 크기 18 (임시값), 흰색, 가운데 정렬.
    - `Message`: 위치 (0, 8), 크기 340×32, 글자 `SaveText.CorruptMessage`, 크기 12 (임시값), 흰색, 가운데 정렬, 줄바꿈 허용.
    - `NewGameButton`: 위치 (0, −40), 크기 160×32 (임시값), `Image` + `Button`, 색 RGB (0.20, 0.45, 0.25), `Button.transition = None`. 자식 글자 `SaveText.NewGameButton`, 크기 16 (임시값), 흰색, 가운데 정렬, `raycastTarget = false`.
  - **누르면**: 처음 한 번만 `onNewGame` 을 부르고 `SaveErrorPanel` 과 `SaveErrorView` 가 붙은 자기 오브젝트를 `Destroy` 한다. 같은 프레임에 두 번 눌려도 `onNewGame` 은 한 번만 부른다 (불렀는지 기록하는 필드로 막는다).
- TMP Essentials 가 없을 때도 예외로 게임이 멈추지 않게, 글자를 만드는 부분은 `PlumSword.Game.Currency.CurrencyView` 의 글자 만드는 방식(예외를 잡아 경고를 남기고 계속)을 따른다.
- 다른 화면의 오브젝트를 찾거나 고치지 않는다.

## 완료 조건
`SaveViewTests` 의 Core 객체는 `wallet = new Wallet(0)`, `upgrades = new StatUpgrades(wallet, 10, 100, 1.0)`, `stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new System.Random(0))`, `autoSave = new AutoSave(가짜 저장소, wallet, upgrades, stage)` 이다. "프레임을 넘긴다"는 `yield return null` 을 두 번 하는 것이다.

- [플레이 모드] `Assets/Tests/PlayMode/Save/SaveViewTests.cs` 와 `Assets/Tests/PlayMode/Save/SaveErrorViewTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- [플레이 모드] `SaveView.Initialize(autoSave, canvas)` 뒤 캔버스 아래에 `SaveFailedText` 오브젝트가 있고 `activeSelf` 는 `false` 다.
- [플레이 모드] `SaveView` 를 붙인 뒤 `wallet.Add(10)` 을 하고 프레임을 넘기면 가짜 저장소의 시도가 1 이고 `autoSave.IsDirty` 는 `false` 다.
- [플레이 모드] 가짜 저장소를 실패하게 하고 `wallet.Add(10)` 뒤 프레임을 넘기면 `autoSave.HasFailed` 가 `true` 이고 `SaveFailedText` 의 `activeSelf` 는 `true` 다.
- [플레이 모드] 이어서 가짜 저장소를 성공하게 하고 `autoSave.SaveNow(1)` 을 부르면 `SaveFailedText` 의 `activeSelf` 는 `false` 다.
- [플레이 모드] `SaveView` 의 오브젝트에 `SendMessage("OnApplicationPause", true)` 를 하면 가짜 저장소의 시도가 1 늘고, `SendMessage("OnApplicationPause", false)` 는 시도를 늘리지 않는다.
- [플레이 모드] `SaveErrorView.Initialize(canvas, null)` 은 `ArgumentNullException` 이다.
- [플레이 모드] 자식 하나(`Other`)가 있는 캔버스에서 `SaveErrorView.Initialize(canvas, 동작)` 을 하면 캔버스의 마지막 자식이 `SaveErrorPanel` 이다.
- [플레이 모드] `SaveErrorPanel` 에는 `raycastTarget` 이 `true` 인 `Image` 가 있고 앵커가 (0, 0)~(1, 1) 이다.
- [플레이 모드] `SaveErrorPanel` 아래에서 찾은 `NewGameButton` 에 `Button` 이 있다.
- [플레이 모드] `NewGameButton` 의 `Button.onClick.Invoke()` 를 같은 프레임에 두 번 하면 동작은 한 번만 불린다.
- [플레이 모드] 위처럼 누른 뒤 프레임을 넘기면 `SaveErrorPanel` 오브젝트와 `SaveErrorView` 가 붙은 오브젝트가 모두 없어져 있다 (`== null`).
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다.
- [사람 확인] 에디터에서 손상 창을 띄웠을 때(032 뒤, `save.dat` 을 아무 글자로 바꾸고 실행) 화면 가운데에 `SAVE DATA IS CORRUPTED`, 그 아래 `START A NEW GAME? THE OLD SAVE WILL BE REPLACED.`, 그 아래 초록 `NEW GAME` 버튼이 겹치지 않고 보이며 뒤 화면은 어둡게 가려진다.
- [사람 확인] 에디터에서 저장 실패 중일 때(032 뒤, `save.dat` 을 읽기 전용으로 바꾸고 금화를 얻음) 화면 오른쪽 위 진행 바 아래에 빨간 `SAVE FAILED` 가 보인다.

## 하지 말 것
- `GameRoot` 를 고치지 않는다 (032 가 한다).
- `Assets/Scripts/Core/` 의 코드를 고치지 않는다 (`AutoSave`, `SaveText` 는 연결 지점으로만 쓴다).
- 다른 화면 코드(`Assets/Scripts/Game/Currency/`, `StatUpgrade/`, `Combat/`, `Stage/`)를 고치지 않는다.
- `FileSaveStore` 를 고치지 않는다 (030 의 것).
- `Assets/Tests/PlayMode/Capture/` 와 `GameBootTests` 를 고치지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 바꾸지 않는다 (테스트 어셈블리에 TextMeshPro 참조를 더하지 않는다).

## 보고서

### 한 일
- 자동 저장 갱신과 실패 표시를 연결하고 손상 오류 창과 새 게임 버튼을 구현했다.
- 화면 구조, 저장 시도, 실패 복구, 일시정지와 중복 클릭을 확인하는 플레이 모드 테스트를 추가했다.
- 저장 사양의 화면 자동 테스트 설명을 갱신했다.

### 바뀐 파일
- `Assets/Scripts/Game/Save/SaveView.cs`, `SaveErrorView.cs` 및 각 `.meta`
- `Assets/Tests/PlayMode/Save/SaveViewTests.cs`, `SaveErrorViewTests.cs` 및 각 `.meta`
- `docs/spec/save.md`
- `tasks/doing/031_save_views.md`, `.loop/result.json`

### 가정
- 재초기화 시 기존 실패 표시와 이벤트 구독을 교체한다. 새 게임 콜백이 예외를 던져도 창을 제거한다.

### 사람이 확인할 것
- 032 뒤 `save.dat`을 임의의 글자로 바꾸고 실행하여 가운데 손상 제목과 문구, 초록 NEW GAME 버튼이 겹치지 않고 보이고 뒤 화면이 어둡게 가려지는지 확인한다.
- 032 뒤 `save.dat`을 읽기 전용으로 바꾸고 금화를 얻어 오른쪽 위 진행 바 아래에 빨간 SAVE FAILED가 보이는지 확인한다.
- Unity 라이선스 연결을 복구한 뒤 전체 에딧 모드와 플레이 모드 테스트 및 컴파일을 확인한다.

### 테스트 결과
- `python tools/loop/run_unity_tests.py`: 직접 실행하지 못함. Unity 로그에 라이선스 클라이언트 연결 끊김과 재연결 실패가 기록되어 테스트 실행에 진입하지 못해 중단했다. 컴파일 및 테스트 통과 여부는 확인하지 못했다.


## 루프 기록 (2026-11-17 14:17)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/031_save_views
- 루프 브랜치: loop/20261117-134243
