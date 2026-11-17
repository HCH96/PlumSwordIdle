# 042_start_screen_view — 시작 화면

## 목표
Game(`PlumSword.Game.StartMenu`)에 제목·시작 버튼·설정 버튼이 있는 시작 화면 `StartScreenView`를 코드로 만든다. `GameRoot`에는 아직 조립하지 않는다.

## 묶음
시작화면

## 선행 작업
- 038_start_menu_rules
- 041_start_menu_text

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다", "테스트" 절)
- docs/dev/art.md
- docs/spec/start_menu.md ("화면"의 "시작 화면"과 "공통", "연결 지점"의 `StartScreenView`)

## 요구사항
- `Assets/Scripts/Game/StartMenu/StartScreenView.cs`, `public sealed class StartScreenView : MonoBehaviour` (네임스페이스 `PlumSword.Game.StartMenu`).
- `public void Initialize(UnityEngine.Canvas canvas, PlumSword.Core.StartMenu.StartButtonKind button, System.Action onStart, System.Action onOpenSettings)`:
  - `canvas`, `onStart`, `onOpenSettings` 중 하나라도 `null`이면 `ArgumentNullException`.
  - `canvas` 아래에 "StartScreenPanel"을 만들고 `SetAsLastSibling()`으로 캔버스의 마지막 자식으로 둔다.
- 화면 구조 (사양 "화면 → 시작 화면" 그대로. 값은 모두 임시값):
  - "StartScreenPanel": 앵커 (0, 0)~(1, 1)로 캔버스를 꽉 채우는 불투명 `Image`, RGB (0.10, 0.07, 0.12), `raycastTarget = true`.
  - 제목 "Title": `StartMenuText.Title`, 위치 (0, 70), 크기 600×48, 글자 크기 36, RGB (1.0, 0.78, 0.86).
  - 시작 버튼 "StartButton": 위치 (0, −20), 크기 200×40, 색 RGB (0.20, 0.45, 0.25), 글자 `StartMenuText.StartButtonLabel(button)`, 글자 크기 18.
  - 설정 버튼 "SettingsButton": 위치 (0, −72), 크기 200×32, 색 RGB (0.25, 0.25, 0.30), 글자 `StartMenuText.SettingsButton`, 글자 크기 14.
  - 위치는 모두 캔버스 가운데(앵커·피벗 (0.5, 0.5)) 기준.
  - 공통: 버튼은 `Image` + `Button`, `Button.transition = None`. 글자는 `TextMeshProUGUI`(TMP 기본 글꼴), 흰색(제목은 위 색), 가운데 정렬, `raycastTarget = false`.
- 입력:
  - "StartButton"을 누르면 `onStart`를 한 번 부르고 자기 오브젝트와 "StartScreenPanel"을 `Destroy`한다. 없어지기 전에(같은 프레임에) 또 눌러도 `onStart`는 한 번만 부른다.
  - "SettingsButton"을 누를 때마다 `onOpenSettings`를 부른다.
- 자기 오브젝트가 밖에서 지워질 때도 "StartScreenPanel"을 함께 지운다.
- TMP 리소스가 없어도 예외로 게임이 멈추지 않게 한다 (기존 `Assets/Scripts/Game/OfflineReward/OfflineRewardView.cs`, `Assets/Scripts/Game/Save/SaveErrorView.cs`의 글자 만드는 방식을 따른다).
- 플레이 모드 테스트: `Assets/Tests/PlayMode/StartMenu/StartScreenViewTests.cs`, 네임스페이스 `PlumSword.Game.Tests.StartMenu`. 테스트가 따로 만든 캔버스(`Canvas` + `GraphicRaycaster`)를 쓰고 테스트가 끝나면 만든 오브젝트를 지운다. 버튼 누르기는 `Button.onClick.Invoke()`로 한다. 글자는 패널 아래 `TextMeshProUGUI`들의 `text`로 확인한다.

## 완료 조건
- [플레이 모드] `Assets/Tests/PlayMode/StartMenu/StartScreenViewTests.cs`에 아래 `[플레이 모드]` 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- [플레이 모드] `Initialize`에 `canvas`를 `null`로 넣으면 `ArgumentNullException`이다.
- [플레이 모드] `Initialize`에 `onStart`를 `null`로 넣으면 `ArgumentNullException`이다.
- [플레이 모드] `Initialize`에 `onOpenSettings`를 `null`로 넣으면 `ArgumentNullException`이다.
- [플레이 모드] 캔버스에 자식 하나를 먼저 둔 뒤 `Initialize(canvas, Continue, …)`를 하면 "StartScreenPanel"은 캔버스의 마지막 자식이다.
- [플레이 모드] 위 경우 "StartScreenPanel"의 앵커는 (0, 0)~(1, 1)이고 그 `Image`의 `raycastTarget`은 `true`이다.
- [플레이 모드] 위 경우 패널 아래 글자에 `"PLUM BLOSSOM SWORD"`, `"CONTINUE"`, `"SETTINGS"`가 있다.
- [플레이 모드] 새 캔버스에서 `Initialize(canvas, NewGame, …)`를 하면 "StartButton"의 글자는 `"START"`이다.
- [플레이 모드] "SettingsButton"의 `onClick`을 두 번 부르면 `onOpenSettings`가 2번 불린다.
- [플레이 모드] "StartButton"의 `onClick`을 같은 프레임에 두 번 부르면 `onStart`가 1번 불린다.
- [플레이 모드] 위 경우 한 프레임 뒤 "StartScreenPanel"과 `StartScreenView`가 붙었던 오브젝트가 없다.
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다.

## 하지 말 것
- `GameRoot`와 `GameBootstrap`을 고치지 않는다. 시작 화면 조립은 044가 한다.
- Core(`Assets/Scripts/Core/`)와 다른 기능의 화면 코드를 고치지 않는다.
- 설정 창(`SettingsView`)은 만들지 않는다 (043이 한다).
- 씬·프리팹·에셋, 어셈블리 정의, `Assets/Tests/PlayMode/Capture/`를 고치지 않는다.

## 보고서

### 한 일
- 시작 화면 패널과 제목, 시작·설정 버튼을 코드로 조립하고 입력 및 패널 정리를 구현했다.
- 입력 검증, 화면 구조와 문구, 버튼 동작, 오브젝트 삭제를 확인하는 플레이 모드 테스트를 추가했다.
- 새 게임 시작 버튼의 `START` 문구 검사를 `Assert.That`으로 명시해 관문이 검사로 인식하도록 수정했다.
- 기존 사양과 공개 연결 지점에 맞춰 구현하여 사양 변경은 없다.

### 바뀐 파일
- `Assets/Scripts/Game/StartMenu.meta`, `Assets/Scripts/Game/StartMenu/StartScreenView.cs` 및 그 `.meta`
- `Assets/Tests/PlayMode/StartMenu.meta`, `Assets/Tests/PlayMode/StartMenu/StartScreenViewTests.cs` 및 그 `.meta`
- `tasks/doing/042_start_screen_view.md`, `.loop/result.json`

### 가정
- 테스트 어셈블리 정의를 변경하지 않기 위해 TMP 컴포넌트의 `text` 속성은 리플렉션으로 읽는다. 화면 크기와 색상은 사양에 적힌 임시값을 사용한다.

### 사람이 확인할 것
- 실제 화면의 배치와 글꼴 표시를 확인한다. 시작 화면의 게임 루트 조립은 후속 작업 범위다.

### 테스트 결과
- Unity 컴파일과 플레이 모드 테스트는 직접 실행하지 않았으며 루프 스크립트의 실행과 판정을 기다린다.
- 관문 및 교차 리뷰 결과는 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-17 18:39)

- 작업 에이전트: codex(시도 1~2)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/042_start_screen_view
- 루프 브랜치: loop/20261117-182336
