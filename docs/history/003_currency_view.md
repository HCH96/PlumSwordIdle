# 003_currency_view — 공용 UI 뿌리와 보유 금화 표시

## 목표
`PlumSword.Game` 에서 `GameRoot` 가 지갑(`Wallet`) 하나와 공용 UI 캔버스·`EventSystem` 을 만들게 하고, 보유 금화 한 줄(`G 0`)을 보여 주는 `CurrencyView` 를 만든다. 화면 조립과 표시만 하고 계산은 Core 의 `Wallet`, `GoldText` 를 쓴다.

## 묶음
금화

## 선행 작업
- 001_wallet
- 002_gold_text

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다" 절)
- docs/dev/art.md ("기본 숫자" 절의 `CanvasScaler` 기준)
- docs/spec/currency.md ("연결 지점", "화면" 절)

## 요구사항
- 선행 작업이 만든 것을 쓴다. 둘 다 네임스페이스 `PlumSword.Core.Currency` 다. 내부는 고치지 않는다.
  - 001_wallet: `Wallet` — `new Wallet()`, `long Balance { get; }`, `event System.Action<long> BalanceChanged` (잔액이 바뀐 뒤 새 잔액을 담아 온다).
  - 002_gold_text: `GoldText.Format(long amount)` — 보유 금화 글자를 돌려준다 (예: 0 → `G 0`, 1234 → `G 1,234`).
- 고칠 파일: `Assets/Scripts/Game/GameRoot.cs` (이미 있다. `Awake` 가 비어 있다). 만들 파일: `Assets/Scripts/Game/Currency/CurrencyView.cs` (네임스페이스 `PlumSword.Game.Currency`).
- 공개 모양은 사양의 "연결 지점" 절 그대로다. 뒤의 화면 기능(강화, 전투, 스테이지)이 `GameRoot.Wallet` 과 `GameRoot.UiCanvas` 를 쓴다.

  ```csharp
  namespace PlumSword.Game
  {
      public sealed class GameRoot : MonoBehaviour
      {
          public PlumSword.Core.Currency.Wallet Wallet { get; }   // 밖에서는 읽기만 (private set)
          public UnityEngine.Canvas UiCanvas { get; }             // 밖에서는 읽기만 (private set)
      }
  }

  namespace PlumSword.Game.Currency
  {
      public sealed class CurrencyView : MonoBehaviour
      {
          public void Initialize(PlumSword.Core.Currency.Wallet wallet, UnityEngine.Canvas canvas);
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/currency.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- `GameRoot.Awake` 는 아래 순서로 조립한다. `[Game]` 은 `GameRoot` 가 붙어 있는 오브젝트다 (`GameBootstrap` 이 만든다).
  1. `Wallet = new Wallet()` (처음 잔액 0. 1단계에는 저장이 없다). 게임에 `Wallet` 은 이것 하나뿐이다.
  2. `[Game]` 의 자식 오브젝트 `[Canvas]` 를 만들고 아래를 붙인 뒤 `UiCanvas` 에 넣는다.
     - `Canvas`: `renderMode = RenderMode.ScreenSpaceOverlay`
     - `CanvasScaler`: `uiScaleMode = ScaleWithScreenSize`, `referenceResolution = (640, 360)`, `screenMatchMode = MatchWidthOrHeight`, `matchWidthOrHeight = 1`
     - `GraphicRaycaster`
  3. 씬에 `EventSystem` 이 하나도 없을 때만 `[Game]` 의 자식 오브젝트 `[EventSystem]` 을 만들고 `EventSystem` 과 `InputSystemUIInputModule`(`UnityEngine.InputSystem.UI`)을 붙인다. 이미 있으면 만들지 않는다.
  4. `[Game]` 의 자식 오브젝트 `[CurrencyView]` 를 만들어 `CurrencyView` 를 붙이고 `Initialize(Wallet, UiCanvas)` 를 부른다.
- 조립 단계는 `GameRoot` 안의 private 메서드로 나눠도 된다. 뒤 기능이 `Awake` 에 자기 줄을 추가하기 쉽게 금화 관련 줄은 한곳에 모은다.
- `CurrencyView.Initialize(wallet, canvas)`:
  - `canvas` 의 자식으로 글자 오브젝트 `GoldLabel` 을 만들고 `TextMeshProUGUI` 를 붙인다.
  - `RectTransform`: `anchorMin = anchorMax = (0, 0)`(캔버스 왼쪽 아래), `pivot = (0, 1)`, `anchoredPosition = (8, 116)`, `sizeDelta = (200, 16)`. (위치와 크기는 임시값)
  - 글자 모양: 글자 크기 16 (임시값), 흰색, 왼쪽 정렬, 줄바꿈 없음, `raycastTarget = false`. 글꼴은 따로 지정하지 않고 TMP 기본 글꼴을 쓴다.
  - 만들자마자 글자를 `GoldText.Format(wallet.Balance)` 로 채운다.
  - `wallet.BalanceChanged` 를 구독해 알림이 올 때마다 글자를 `GoldText.Format(새 잔액)` 으로 바꾼다. `OnDestroy` 에서 구독을 끊는다.
- 글자는 `GoldText.Format` 이 돌려준 것을 그대로 쓴다. 화면 코드에서 글자를 따로 만들거나 붙이거나 고치지 않는다.
- `Update` 로 매 프레임 다시 그리지 않는다. 숫자가 바뀔 때 연출(튀어오름, 숫자 굴리기)은 없다. 누르는 것도 없다.
- TextMeshPro 기본 리소스(TMP Essentials)가 프로젝트에 없을 수 있다 (지금 `Assets/TextMesh Pro/` 폴더가 없다). 리소스가 없어도 예외로 게임이 멈추지 않아야 한다. `CurrencyView` 에서 글자 오브젝트를 만드는 부분과 글자를 바꾸는 부분을 `try/catch` 로 감싸고, 실패하면 `Debug.LogWarning` 을 한 번만 남기고 넘어간다. 리소스를 대신 가져오거나 만들지 않는다 (사람이 에디터에서 가져온다).
- `Initialize` 에 `wallet` 이나 `canvas` 가 `null` 로 들어오면 `System.ArgumentNullException` 을 던진다.
- `Assets/Scripts/Game/Currency/` 폴더를 새로 만든다. `.meta` 파일은 유니티가 화면 없이 실행될 때(테스트 실행) 자동으로 만든다. 만들어진 `.meta` 는 지우지 않는다.
- 금화를 벌 방법(전투·스테이지)이 아직 없어 실행하면 `G 0` 에서 바뀌지 않는다. 확인용 금화 지급 코드나 치트 키를 넣지 않는다.
- 이 작업에는 에딧 모드 테스트를 새로 쓰지 않는다 (테스트 어셈블리는 Core 만 참조한다). 표시 글자의 판정은 002_gold_text 의 테스트가 이미 한다. 플레이 모드 테스트 어셈블리도 만들지 않는다.

## 완료 조건
- [컴파일] `GameRoot.cs` 와 `CurrencyView.cs` 가 컴파일 오류 없이 컴파일되고 기존 테스트가 모두 통과한다. (`WalletTests`, `GoldTextTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)
- [사람 확인] 게임을 실행하면 화면 왼쪽 아래(아래에서 100~116px 높이, 왼쪽에서 8px)에 흰 글자 `G 0` 이 보인다. (TMP Essentials 를 에디터에서 먼저 가져와야 보인다: Window > TextMeshPro > Import TMP Essential Resources)
- [사람 확인] 실행 중 Hierarchy 의 `[Game]` 아래에 `[Canvas]`, `[EventSystem]`, `[CurrencyView]` 가 하나씩 있고, `[Canvas]` 아래에 `GoldLabel` 이 있다.
- [사람 확인] 실행 중 `[Canvas]` 의 `CanvasScaler` 가 Scale With Screen Size, 기준 해상도 640×360, Match 1 이다.
- [사람 확인] TMP Essentials 를 가져오지 않은 상태로 실행해도 콘솔에 예외(빨간 줄)가 반복해서 쌓이지 않고 게임이 멈추지 않는다.

## 하지 말 것
- `Assets/Scripts/Core/` 를 고치지 않는다. `Wallet`, `GoldText` 는 공개된 모양으로만 쓴다. 표시 글자 규칙을 바꿔야 하면 고치지 말고 보고서의 가정에 적는다.
- `GameBootstrap.cs` 를 고치지 않는다.
- `GameRoot` 에는 금화 기능과 공용 UI 뿌리(`Wallet`, `UiCanvas`, `[Canvas]`, `[EventSystem]`, `[CurrencyView]`)만 추가한다. 강화·전투·스테이지 화면이나 카메라 설정을 만들지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다. 화면은 코드로 조립한다. TMP Essentials 리소스를 직접 추가하지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다 (`PlumSword.Game` 은 이미 `Unity.TextMeshPro`, `Unity.InputSystem`, `UnityEngine.UI` 를 참조한다).
- 강화 버튼, 강화 UI 영역의 배경, 금화 아이콘, 획득 연출, 한글 표기를 만들지 않는다. 사양의 "범위 밖"이다.
- 저장·불러오기를 만들지 않는다. 2단계 저장 기능이 한다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `GameRoot.Awake` 가 작업 문서의 순서대로 조립한다: `Wallet = new Wallet()` → `[Canvas]`(Canvas Screen Space - Overlay, CanvasScaler Scale With Screen Size 640×360 Match 1, GraphicRaycaster)를 만들어 `UiCanvas` 에 넣음 → 씬에 `EventSystem` 이 없을 때만 `[EventSystem]`(EventSystem + InputSystemUIInputModule) → `[CurrencyView]` 를 만들어 `Initialize(Wallet, UiCanvas)`.
- 조립 단계는 private 메서드(`CreateUiCanvas`, `EnsureEventSystem`, `CreateCurrencyView`)로 나눴고, `Awake` 에 "Core 객체 / 공용 UI 뿌리 / 기능별 화면" 주석으로 구역을 나눠 뒤 기능이 한 줄씩 추가하기 쉽게 했다.
- `CurrencyView.Initialize`: null 인자면 `ArgumentNullException`. 캔버스 아래 `GoldLabel`(TextMeshProUGUI, 앵커 (0,0), 피벗 (0,1), 위치 (8,116), 크기 200×16, 글자 16, 흰색, 왼쪽 정렬, 줄바꿈 없음, raycastTarget false)을 만들고 `GoldText.Format(wallet.Balance)` 로 채운 뒤 `BalanceChanged` 를 구독한다. `OnDestroy` 에서 구독을 끊는다.
- 글자 오브젝트 만들기와 글자 바꾸기를 각각 `try/catch` 로 감싸고, 실패하면 `Debug.LogWarning` 을 한 번만 남긴다. 라벨을 못 만들었으면 이후 글자 바꾸기는 조용히 건너뛴다.
- 사양 `docs/spec/currency.md` "연결 지점"의 `GameRoot` 속성 모양을 `{ get; private set; }` 로 적었다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs` (수정)
- `Assets/Scripts/Game/Currency/CurrencyView.cs` (새 파일)
- `Assets/Scripts/Game/Currency.meta`, `Assets/Scripts/Game/Currency/CurrencyView.cs.meta` (유니티가 자동 생성)
- `docs/spec/currency.md` ("연결 지점"의 `GameRoot` 속성에 `private set` 표기)

### 가정
- `EventSystem` 존재 확인은 `FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include)` 로 한다. 꺼져 있는 EventSystem 도 "이미 있음"으로 본다.
- 줄바꿈 없음은 `textWrappingMode = TextWrappingModes.NoWrap` 으로 설정했다 (ugui 2.x 에서 `enableWordWrapping` 은 사용 중지 예정 API).
- `Initialize` 를 두 번 부르면 앞의 지갑 구독을 끊고 다시 구독한다 (라벨은 새로 하나 더 만든다). 현재 호출하는 곳은 한 번뿐이다.
- 경고 문구: "CurrencyView: 보유 금화 글자를 표시하지 못했습니다. TMP Essentials 가 필요합니다 ..." + 예외 메시지.
- 사양의 "연결 지점"에 적힌 `{ get; }` 를 작업 문서대로 `{ get; private set; }` 로 구현하고 사양 표기도 맞췄다 (밖에서 보는 모양은 같다).

### 사람이 확인할 것
- [사람 확인] 게임을 실행하면 화면 왼쪽 아래(아래에서 100~116px 높이, 왼쪽에서 8px)에 흰 글자 `G 0` 이 보인다. (TMP Essentials 를 에디터에서 먼저 가져와야 보인다: Window > TextMeshPro > Import TMP Essential Resources)
- [사람 확인] 실행 중 Hierarchy 의 `[Game]` 아래에 `[Canvas]`, `[EventSystem]`, `[CurrencyView]` 가 하나씩 있고, `[Canvas]` 아래에 `GoldLabel` 이 있다.
- [사람 확인] 실행 중 `[Canvas]` 의 `CanvasScaler` 가 Scale With Screen Size, 기준 해상도 640×360, Match 1 이다.
- [사람 확인] TMP Essentials 를 가져오지 않은 상태로 실행해도 콘솔에 예외(빨간 줄)가 반복해서 쌓이지 않고 게임이 멈추지 않는다.
- TMP Essentials 가 없을 때 TMP 자체가 내부에서 `Debug.LogError` 를 남길 수 있다 (예외가 아니라 TMP 의 로그라 try/catch 로 막히지 않는다). 반복해서 쌓이는지 확인 필요.
- 씬에 이미 EventSystem 이 있는 경우 `[EventSystem]` 이 만들어지지 않는 동작은 실행해 보지 않았다.

### 테스트 결과
- `python tools/loop/run_unity_tests.py`: EditMode 33개 중 33개 통과, 0개 실패 (컴파일 오류·경고 없음). `WalletTests`, `GoldTextTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함.
- 이 작업은 작업 문서대로 새 테스트를 쓰지 않았다. 화면 표시는 사람 확인 항목이다.


## 루프 기록 (2026-11-16 16:03)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/003_currency_view
- 루프 브랜치: loop/20261116-155015
