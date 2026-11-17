# 043_settings_view — 설정 창과 음량 저장

## 목표
Game(`PlumSword.Game.StartMenu`)에 게임 화면의 설정 버튼과 설정 창 `SettingsView`, PlayerPrefs 음량 저장 `VolumePrefs`를 코드로 만든다. `GameRoot`에는 아직 조립하지 않는다.

## 묶음
시작화면

## 선행 작업
- 038_start_menu_rules
- 039_volume_settings
- 040_leave_game
- 041_start_menu_text

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다", "테스트" 절)
- docs/dev/art.md
- docs/spec/start_menu.md ("동작"의 "설정 창"·"음량"·"음량 저장"·"떠나기", "화면"의 "게임 설정 버튼"·"설정 창"·"공통", "연결 지점"의 `SettingsView`·`VolumePrefs`)
- docs/spec/save.md ("연결 지점" 절)

## 요구사항
- `Assets/Scripts/Game/StartMenu/SettingsView.cs`, `public sealed class SettingsView : MonoBehaviour` (네임스페이스 `PlumSword.Game.StartMenu`).
  - `public void Initialize(UnityEngine.Canvas canvas, PlumSword.Core.StartMenu.VolumeSettings volume, PlumSword.Core.StartMenu.LeaveGame leaveGame, System.Action onReturnToTitle, System.Action onQuit, System.Action saveVolume)`: 여섯 인자 중 하나라도 `null`이면 `ArgumentNullException`. `canvas` 아래에 게임 화면 버튼 "GameSettingsButton"(켜짐)과 설정 창 "SettingsPanel"(꺼짐, `SetActive(false)`)을 이 순서로 만든다.
  - `public bool IsOpen { get; }`, `public SettingsMode Mode { get; }` (마지막으로 연 모드. 처음은 `Title`).
  - `public void Open(SettingsMode mode)`: "SettingsPanel"을 켜고 `SetAsLastSibling()`으로 캔버스의 마지막 자식으로 올린다. 슬라이더 둘을 지금 `volume.Bgm`/`Sfx`로 맞추되 `SetBgm`/`SetSfx`를 부르지 않는다 (`SetValueWithoutNotify`). 옆 글자도 맞춘다. 모드에 맞게 버튼을 보인다. 이미 열려 있으면 모드만 바꾸고 열린 채 둔다.
  - `public void Close()`: 열려 있으면 닫고 `saveVolume`을 한 번 부른다. 닫혀 있으면 아무 일도 없다 (`saveVolume`도 부르지 않는다).
  - 표시: "ToTitleButton", "QuitButton"은 `Mode == InGame`일 때만 켠다. "LeaveFailedMessage"는 `Mode == InGame && leaveGame.HasFailed`일 때만 켠다 (처음에는 꺼짐). 창을 닫았다 다시 열어도 `HasFailed`이면 보인다.
  - "GameSettingsButton"을 누르면 `Open(SettingsMode.InGame)`.
  - 슬라이더 값이 바뀌면 `volume.SetBgm((int)value)` / `volume.SetSfx((int)value)` 뒤 옆 글자를 `StartMenuText.BgmLine(volume.Bgm)` / `SfxLine(volume.Sfx)`로 맞춘다.
  - "ToTitleButton": `leaveGame.TryLeave(DateTime.UtcNow.Ticks)`. `true`이면 `saveVolume` → `onReturnToTitle`. `false`이면 실패 문구를 켜고 창을 연 채 머문다.
  - "QuitButton": 같은 판정. `true`이면 `saveVolume` → `onQuit`. `false`이면 실패 문구를 켜고 머문다.
  - `OnApplicationPause(bool paused)`에서 `paused`가 `true`이면, `OnApplicationQuit()`에서 `saveVolume`을 부른다 (`Initialize` 전이면 아무것도 하지 않는다).
  - 창이 열려 있어도 `Battle.SetPaused`를 부르지 않는다 (이 클래스는 전투를 모른다).
- 화면 구조 (사양 "화면" 그대로. 값은 모두 임시값):
  - "GameSettingsButton": 캔버스의 자식, 앵커 (0, 1), 피벗 (0, 1), 위치 (8, −62), 크기 72×20, 색 RGB (0.25, 0.25, 0.30), 글자 `StartMenuText.SettingsButton`, 글자 크기 12.
  - "SettingsPanel": 캔버스를 꽉 채우는(앵커 (0, 0)~(1, 1)) 검은색 알파 0.5 `Image`, `raycastTarget = true` (가림막).
  - "Window": "SettingsPanel"의 자식, 가운데, 크기 360×230, 바탕 RGB (0.12, 0.12, 0.12). 아래 위치는 모두 창 가운데 기준이고 아래 오브젝트는 모두 "Window"의 자식이다.
  - "Title" `StartMenuText.SettingsTitle`: 위치 (0, 92), 크기 340×24, 글자 크기 18.
  - "BgmLabel" `BgmLine(volume.Bgm)`: 위치 (−120, 56), 크기 100×18, 글자 크기 14, 왼쪽 정렬. "BgmSlider": 위치 (50, 56), 크기 200×16.
  - "SfxLabel" `SfxLine(volume.Sfx)`: 위치 (−120, 24). "SfxSlider": 위치 (50, 24). 크기와 글자는 배경음악 줄과 같다.
  - 슬라이더: uGUI `Slider`를 코드로 만든다. `minValue` 0, `maxValue` 100, `wholeNumbers = true`, `Direction.LeftToRight`. 바탕 RGB (0.30, 0.30, 0.30), 채움 RGB (0.20, 0.45, 0.25), 손잡이 흰색 12×20.
  - "ToTitleButton": 위치 (−85, −18), 크기 150×32, 색 RGB (0.25, 0.25, 0.30), 글자 `ToTitleButton`. "QuitButton": 위치 (85, −18), 크기 150×32, 색 RGB (0.45, 0.20, 0.20), 글자 `QuitButton`. 글자 크기 14.
  - "LeaveFailedMessage" `StartMenuText.LeaveFailedMessage`: 위치 (0, −48), 크기 340×16, 글자 크기 12, RGB (1.0, 0.35, 0.35).
  - "CloseButton": 위치 (0, −86), 크기 120×28, 색 RGB (0.25, 0.25, 0.30), 글자 `CloseButton`, 글자 크기 14. 누르면 `Close()`.
  - 공통: 버튼은 `Image` + `Button`, `Button.transition = None`. 글자는 `TextMeshProUGUI`(TMP 기본 글꼴), 흰색(따로 적은 것 말고), 가운데 정렬(따로 적은 것 말고), `raycastTarget = false`. TMP 리소스가 없어도 예외로 게임이 멈추지 않게 한다 (기존 `OfflineRewardView`, `SaveErrorView` 방식을 따른다).
- `Assets/Scripts/Game/StartMenu/VolumePrefs.cs`, `public static class VolumePrefs`:
  - `public const string BgmKey = "PlumSword.BgmVolume";`, `public const string SfxKey = "PlumSword.SfxVolume";`
  - `public static VolumeSettings Load()`: `new VolumeSettings(PlayerPrefs.GetInt(BgmKey, 80), PlayerPrefs.GetInt(SfxKey, 80))`.
  - `public static void Save(VolumeSettings volume)`: `null`이면 `ArgumentNullException`. `SetInt` 둘 → `PlayerPrefs.Save()`.
  - `VolumePrefs`는 자동 테스트 대상이 아니다 (테스트에서 PlayerPrefs를 건드리지 않는다).
- 플레이 모드 테스트: `Assets/Tests/PlayMode/StartMenu/SettingsViewTests.cs`, 네임스페이스 `PlumSword.Game.Tests.StartMenu`.
  - 테스트가 따로 만든 캔버스(`Canvas` + `GraphicRaycaster`)를 쓰고 끝나면 만든 오브젝트를 지운다.
  - "떠나기 준비": `new Wallet(0)`, `new StatUpgrades(wallet, 10, 100, 1.0)`, `new StageProgress(wallet, upgrades, new Battle(upgrades), new System.Random(0), 1, 1)`, 테스트 안의 가짜 저장소로 켜진 `new AutoSave(store, wallet, upgrades, stage, enabled: true)`, `new LeaveGame(autoSave)`. 가짜 저장소는 `ISaveStore`를 구현하고 `Succeeds`로 실패하게 바꿀 수 있는 테스트 안의 비공개 클래스로 만든다 (기존 `Assets/Tests/PlayMode/Save/SaveViewTests.cs`의 `FakeStore`와 같은 방식. 그 파일은 고치지 않는다).
  - 콜백 `onReturnToTitle`, `onQuit`, `saveVolume`은 불린 횟수를 세는 람다로 넣는다. 버튼 누르기는 `Button.onClick.Invoke()`로 한다.
- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/start_menu.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- [플레이 모드] `Assets/Tests/PlayMode/StartMenu/SettingsViewTests.cs`에 아래 `[플레이 모드]` 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- [플레이 모드] `Initialize`에 `canvas`, `volume`, `leaveGame`, `onReturnToTitle`, `onQuit`, `saveVolume`을 하나씩 `null`로 넣으면 각각 `ArgumentNullException`이다.
- [플레이 모드] 떠나기 준비의 `LeaveGame`과 `new VolumeSettings()`로 `Initialize`한 뒤 `IsOpen`은 `false`, "SettingsPanel"은 있고 꺼져 있으며, "GameSettingsButton"은 켜져 있다.
- [플레이 모드] 위처럼 만든 뒤 "GameSettingsButton"의 `onClick`을 부르면 `IsOpen` `true`, `Mode` `InGame`이다.
- [플레이 모드] 위처럼 만든 뒤 `Open(Title)`을 하면 "SettingsPanel"이 켜져 있고 캔버스의 마지막 자식이다.
- [플레이 모드] 위 `Open(Title)` 뒤 "ToTitleButton", "QuitButton", "LeaveFailedMessage"는 꺼져 있고 "BgmLabel"의 글자는 `"BGM 80"`이다.
- [플레이 모드] 이어서 `Open(InGame)`을 하면 `IsOpen`은 `true` 그대로이고 "ToTitleButton", "QuitButton"이 켜지며 "LeaveFailedMessage"는 꺼져 있다.
- [플레이 모드] 위처럼 만들어 `Open(InGame)`한 뒤 "BgmSlider"의 `value`를 30으로 하면 `volume.Bgm` 30, "BgmLabel" `"BGM 30"`이다.
- [플레이 모드] 이어서 "SfxSlider"의 `value`를 0으로 하면 `volume.Sfx` 0, "SfxLabel" `"SFX 0"`이다.
- [플레이 모드] 이어서 "CloseButton"의 `onClick`을 부르면 `IsOpen` `false`, "SettingsPanel"은 꺼지고 `saveVolume`은 1번 불린다.
- [플레이 모드] 이어서 `Close()`를 한 번 더 불러도 `saveVolume`은 1번 그대로다.
- [플레이 모드] 가짜 저장소를 실패하게 하고 `Open(InGame)` 뒤 "ToTitleButton"의 `onClick`을 부르면 `onReturnToTitle` 0번, `saveVolume` 0번, `IsOpen` `true`, "LeaveFailedMessage"가 켜져 있다.
- [플레이 모드] 이어서 "QuitButton"의 `onClick`을 불러도 `onQuit`은 0번이다.
- [플레이 모드] 이어서 `Close()` 뒤 `Open(InGame)`을 하면 "LeaveFailedMessage"는 켜져 있다.
- [플레이 모드] 이어서 저장소를 성공하게 바꾸고 "ToTitleButton"의 `onClick`을 부르면 `onReturnToTitle` 1번, `saveVolume` 2번(앞의 `Close` 1번 + 떠나기 1번)이다.
- [플레이 모드] 새로 만든 성공하는 저장소의 창에서 `Open(InGame)` 뒤 "QuitButton"의 `onClick`을 부르면 `onQuit` 1번, `saveVolume` 1번, `onReturnToTitle` 0번이다.
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다.

## 하지 말 것
- `GameRoot`와 `GameBootstrap`을 고치지 않는다. 설정 창 조립, 앱 종료, 시작 화면 복귀, 뒤로 가기 키는 044가 한다.
- Core(`Assets/Scripts/Core/`)와 다른 기능의 화면 코드, 기존 테스트 파일(`Assets/Tests/PlayMode/Save/` 등)을 고치지 않는다.
- 소리 재생, `AudioSource`·`AudioListener` 적용을 하지 않는다.
- 씬·프리팹·에셋, 어셈블리 정의, `Assets/Tests/PlayMode/Capture/`를 고치지 않는다.

## 보고서

### 한 일
- 설정 버튼과 모드별 설정 창, 음량 슬라이더, 저장 실패 및 떠나기 콜백을 구현했다.
- PlayerPrefs 음량 읽기·쓰기와 플레이 모드 검증 코드를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Game/StartMenu/SettingsView.cs` 및 `.meta`
- `Assets/Scripts/Game/StartMenu/VolumePrefs.cs` 및 `.meta`
- `Assets/Tests/PlayMode/StartMenu/SettingsViewTests.cs` 및 `.meta`

### 가정
- 슬라이더 손잡이는 양 끝에서 잘리지 않도록 이동 영역의 좌우에 각각 6픽셀 여백을 두었다. 공개 연결 지점은 사양과 같다.

### 사람이 확인할 것
- 후속 조립 작업 이후 설정 창의 실제 배치와 입력 흐름을 에디터에서 확인한다.

### 테스트 결과
- Unity 컴파일 및 플레이 모드 테스트는 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.
- `git diff --check`를 실행했고 출력된 오류는 없다. 새 파일은 해당 검사 대상에 포함되지 않는다.


## 루프 기록 (2026-11-17 18:43)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/043_settings_view
- 루프 브랜치: loop/20261117-182336
