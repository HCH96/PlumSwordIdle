# 044_start_menu_root — GameRoot 시작 순서와 뒤로 가기 키

## 목표
Game의 `GameRoot`에 시작 화면·설정 창·떠나기·시작 화면 복귀·앱 종료를 조립하고, 뒤로 가기 키 `BackKeyWatcher`를 만든다.

## 묶음
시작화면

## 선행 작업
- 038_start_menu_rules
- 039_volume_settings
- 040_leave_game
- 042_start_screen_view
- 043_settings_view

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다", "테스트" 절)
- docs/spec/start_menu.md (전체. 특히 "동작", "연결 지점"의 `GameRoot`·`BackKeyWatcher`와 "`GameRoot.Awake` 순서")
- docs/spec/save.md ("연결 지점" 절)
- docs/spec/offline_reward.md ("연결 지점" 절)
- docs/spec/stage.md ("연결 지점" 절)
- docs/spec/combat.md ("연결 지점" 절)

## 요구사항
- `Assets/Scripts/Game/GameRoot.cs`를 사양 "연결 지점"의 `GameRoot`대로 바꾼다:
  - 정적 `internal static bool NextIsResumeRestart`를 `internal static PlumSword.Core.StartMenu.StartEntry NextEntry`로 바꾼다 (기본값 `Launch`).
  - 공개 속성 추가: `StartEntry Entry { get; private set; }`, `bool IsStartScreenShowing { get; private set; }`, `VolumeSettings Volume { get; private set; }`, `LeaveGame LeaveGame { get; private set; }`, `PlumSword.Game.StartMenu.SettingsView Settings { get; private set; }`.
  - `IsResumeRestart`는 이름과 공개 모양을 그대로 두고, `Entry`가 `ResumeInGame` 또는 `ResumeOnTitle`일 때 `true`가 되게 한다.
- `Awake` 순서 (사양 "`GameRoot.Awake` 순서" 그대로):
  1. 맨 앞: `Entry = NextEntry`, `NextEntry = StartEntry.Launch`.
  2. 저장소를 고르고 `LoadResult`를 읽는다 (그대로).
  3. `Wallet`, `StatUpgrades`, `Battle` (그대로).
  4. 캔버스, 이벤트 시스템, 금화·강화·전투 화면, 스테이지(`StageProgress`와 `[StageView]`, `Begin()`은 아직) (그대로).
  5. `LoadResult.Status == Loaded && StartMenuRules.CalculatesOfflineReward(Entry)`이면 `OfflineReward = OfflineRewardCalculator.Calculate(LoadResult.Data.SavedAtUtcTicks, DateTime.UtcNow.Ticks, StageProgress.StageNumber)`, 아니면 `null`.
  6. `AutoSave = new AutoSave(저장소, Wallet, StatUpgrades, StageProgress, enabled: StartMenuRules.AutoSaveStartsEnabled(LoadResult.Status, OfflineReward != null && OfflineReward.HasReward))`. `[SaveView]`는 그대로.
  7. `Volume = Application.isBatchMode ? new VolumeSettings() : VolumePrefs.Load()`. `LeaveGame = new LeaveGame(AutoSave)`. `[Game]`의 자식 `[SettingsView]`를 만들어 `Initialize(UiCanvas, Volume, LeaveGame, ReturnToTitle, 앱 종료, 음량 쓰기)`하고 `Settings`에 담는다. 음량 쓰기는 배치 모드면 아무것도 하지 않고, 아니면 `VolumePrefs.Save(Volume)`.
  8. `StartMenuRules.ShowsStartScreen(Entry, Application.isBatchMode)`이면: `IsStartScreenShowing = true`, `Battle.SetPaused(true)`, `[Game]`의 자식 `[StartScreenView]`를 만들어 `Initialize(UiCanvas, StartMenuRules.StartButton(LoadResult.Status), 시작 흐름, () => Settings.Open(SettingsMode.Title))`. 아니면 시작 흐름을 바로 한다.
  9. `Application.isBatchMode`가 `false`이면 `[OfflineResumeWatcher]`는 그대로. `[Game]`의 자식 `[BackKeyWatcher]`를 만들어 `Initialize(this, Settings)` (배치 모드에서도 만든다).
- 시작 흐름 (비공개 메서드. 시작 버튼을 누를 때, 또는 8번에서 바로): `IsStartScreenShowing = false`로 한 뒤
  1. `LoadResult.Status == Corrupt`: `Battle.SetPaused(true)`, `[Game]`의 자식 `[SaveErrorView]`로 손상 창 (지금의 `StartNewGame` 콜백 그대로: `AutoSave.Enable()` → `Battle.SetPaused(false)` → `StageProgress.Begin()`).
  2. 아니고 `OfflineReward != null && OfflineReward.HasReward`: `Battle.SetPaused(true)`, `OfflineRewardClaim = new OfflineRewardClaim(Wallet, AutoSave, OfflineReward)`, `[Game]`의 자식 `[OfflineRewardView]` (지금의 `StartAfterClaim` 그대로).
  3. 아니면: `AutoSave.Enable()` → `Battle.SetPaused(false)` → `StageProgress.Begin()`.
  - 보상은 `Awake`에서 계산한 `OfflineReward`를 그대로 쓰고 다시 계산하지 않는다.
- `Restart()`: 공개 모양 그대로. `NextIsResumeRestart = true` 대신 `NextEntry = StartMenuRules.ResumeEntry(IsStartScreenShowing)`. 나머지(이미 다시 만드는 중이면 아무 일 없음, 끄고 지우고 `[GameRestarter]`)는 그대로.
- `public void ReturnToTitle()` 추가: `NextEntry = StartEntry.TitleReturn`으로 `Restart()`와 같은 방법(`SetActive(false)`, `Destroy`, `[GameRestarter]`)으로 다시 만든다. `Restart`와 같은 `restarting` 표시를 써서, 둘 중 무엇이든 이미 불렸으면 아무 일도 없다.
- 앱 종료 (비공개 메서드): `#if UNITY_EDITOR`에서는 `UnityEditor.EditorApplication.isPlaying = false`, 그 밖에는 `Application.Quit()`.
- `GameRestarter`, `OfflineResumeWatcher`가 `NextIsResumeRestart`를 쓰고 있으면 `NextEntry`에 맞게 고친다 (동작은 그대로).
- `Assets/Scripts/Game/StartMenu/BackKeyWatcher.cs`, `public sealed class BackKeyWatcher : MonoBehaviour`:
  - `public void Initialize(PlumSword.Game.GameRoot root, SettingsView settings)`: 둘 중 `null`이면 `ArgumentNullException`.
  - `Update`: `Initialize` 전이거나 `UnityEngine.InputSystem.Keyboard.current`가 `null`이면 아무것도 하지 않는다. `Keyboard.current.escapeKey.wasPressedThisFrame`이면 `StartMenuRules.BackKey(settings.IsOpen, root.IsStartScreenShowing, root.StageProgress.HasBegun)`의 결과가 `OpenSettings`면 `settings.Open(SettingsMode.InGame)`, `CloseSettings`면 `settings.Close()`, `None`이면 아무것도 하지 않는다.
- 플레이 모드 테스트: `Assets/Tests/PlayMode/StartMenu/GameRootStartMenuTests.cs`, 네임스페이스 `PlumSword.Game.Tests.StartMenu`. 준비는 기존 `Assets/Tests/PlayMode/OfflineReward/GameRootOfflineRewardTests.cs`처럼 `SceneManager.LoadSceneAsync(0)` 뒤 몇 프레임 기다려 `[Game]`의 `GameRoot`를 찾는다. 이 테스트는 루프와 명령줄의 배치 모드에서 판정하며, 건너뛰는 코드(`Assert.Ignore`, `Assume`, `[Ignore]`, 조건부 조기 종료 등)는 넣지 않는다. 설정 창을 연 테스트는 끝에서 `Close()`로 닫는다.
- 기존 플레이 모드 테스트(`GameBootTests`, `GameRootSaveTests`, `GameRootOfflineRewardTests`)는 고치지 않는다. 배치 모드는 시작 화면을 건너뛰고 시작 흐름 3번을 바로 하므로 지금의 기대값(`AutoSave.Enabled` true, `HasBegun` true, `IsResumeRestart` false 등)이 그대로 성립해야 한다.
- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/start_menu.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- [플레이 모드] `Assets/Tests/PlayMode/StartMenu/GameRootStartMenuTests.cs`에 아래 `[플레이 모드]` 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- [플레이 모드] 배치 모드에서 게임이 켜지면 `GameRoot.Entry`는 `Launch`, `IsResumeRestart`는 `false`, `IsStartScreenShowing`은 `false`이다.
- [플레이 모드] 배치 모드에서 `[Game]` 아래에 `[StartScreenView]`가 없고 캔버스 아래에 "StartScreenPanel"이 없다.
- [플레이 모드] 배치 모드에서 `[Game]` 아래 `[SettingsView]`에 `SettingsView`가 붙어 있고, `GameRoot.Settings`가 그것이며 `IsOpen`은 `false`이다.
- [플레이 모드] 배치 모드에서 캔버스 아래에 "GameSettingsButton"이 있다.
- [플레이 모드] 배치 모드에서 `GameRoot.Volume`은 `Bgm` 80, `Sfx` 80이다.
- [플레이 모드] 배치 모드에서 `GameRoot.LeaveGame`은 `null`이 아니고 `HasFailed`는 `false`이다.
- [플레이 모드] 배치 모드에서 `StageProgress.HasBegun`은 `true`, `AutoSave.Enabled`는 `true`이다.
- [플레이 모드] 배치 모드에서 `[Game]` 아래에 `[BackKeyWatcher]`가 있고 `BackKeyWatcher`가 붙어 있다.
- [플레이 모드] 배치 모드에서 켠 뒤 `Battle.Paused`를 읽어 두고 `GameRoot.Settings.Open(SettingsMode.InGame)`을 하면 `IsOpen` `true`, `Mode` `InGame`이고 "SettingsPanel"이 켜져 있으며 캔버스의 마지막 자식이다.
- [플레이 모드] 위 경우 `Battle.Paused`는 읽어 둔 값 그대로다.
- [플레이 모드] 이어서 `Close()`를 하면 `IsOpen` `false`이고 "SettingsPanel"은 꺼져 있다.
- [컴파일] 화면 코드가 컴파일되고 기존 테스트(`GameBootTests`, `GameRootSaveTests`, `GameRootOfflineRewardTests` 포함)가 모두 통과한다.
- [사람 확인] 에디터에서 `save.dat` 없이 플레이하면 시작 화면에 `PLUM BLOSSOM SWORD`, `START`, `SETTINGS`가 보이고, `START`를 누르면 1스테이지가 시작된다. 멈췄다 다시 플레이하면 `CONTINUE`가 보인다. (배치 모드의 화면 갈무리에는 시작 화면이 나오지 않으므로 에디터에서 해 봐야 한다)
- [사람 확인] 에디터에서 시작 화면의 `SETTINGS`로 슬라이더를 30·60으로 바꾸고 `CLOSE` → 플레이를 멈췄다 다시 켜서 열면 30·60이다.
- [사람 확인] 실행하고 10초 뒤 화면 갈무리에서 왼쪽 위 `SETTINGS` 버튼이 금화 표시·저장 실패 글자 자리와 겹치지 않는다.
- [사람 확인] 에디터에서 게임 중 `SETTINGS`(또는 Esc)로 창을 열면 전투가 계속되고, Esc로 닫힌다.
- [사람 확인] 에디터에서 `TO TITLE`을 누르면 시작 화면에 `CONTINUE`가 보이고, 누르면 같은 스테이지 처음부터 보상 창 없이 시작된다.
- [사람 확인] 에디터에서 `QUIT GAME`을 누르면 플레이 모드가 멈추고, 다시 켜면 `CONTINUE`가 보인다.
- [사람 확인] 에디터에서 `save.dat`을 읽기 전용으로 바꾸고 `TO TITLE`·`QUIT GAME`을 누르면 `SAVE FAILED. TRY AGAIN.`이 보이고 떠나지 않는다.
- [사람 확인] 에디터에서 `save.dat`을 아무 글자로 바꾸고 `START`를 누르면 손상 창이 뜬다.
- [사람 확인] 폰 빌드에서 시작 화면에 10초 이상 백그라운드로 둔 뒤 돌아오면 시작 화면이 다시 뜨고, 안드로이드 뒤로 가기 키가 게임 중 설정 창을 여닫는다.

## 하지 말 것
- Core(`Assets/Scripts/Core/`)를 고치지 않는다. 038~041이 만든 타입을 그대로 쓴다.
- `StartScreenView`, `SettingsView`, `VolumePrefs`의 동작을 바꾸지 않는다 (042, 043이 만들었다). 고쳐야 할 결함이 있으면 보고서의 가정에 적는다.
- `GameRoot`에서 금화·강화·전투·스테이지·저장 화면의 조립 줄은 순서와 내용을 바꾸지 않는다. 손상 창·보상 창·`Begin`을 시작 흐름 안으로 옮기는 것만 한다.
- 기존 테스트 파일을 고치지 않는다.
- `OnApplicationFocus` 처리, 소리 재생, 시작 화면과 게임 사이의 페이드를 만들지 않는다.
- 씬·프리팹·에셋, 어셈블리 정의, `ProjectSettings/`, `Assets/Tests/PlayMode/Capture/`를 고치지 않는다.

## 보고서

### 한 일
- GameRoot에 시작 화면, 설정, 떠나기, 시작 화면 복귀와 종료를 연결하고 시작 원인에 따른 보상·자동 저장·시작 흐름을 적용했다.
- 뒤로 가기 키 감시와 배치 모드 시작·설정 화면 플레이 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs`
- `Assets/Scripts/Game/StartMenu/BackKeyWatcher.cs` 및 `.meta`
- `Assets/Tests/PlayMode/StartMenu/GameRootStartMenuTests.cs` 및 `.meta`
- `tasks/doing/044_start_menu_root.md`, `.loop/result.json`

### 가정
- 없음. 공개 연결 지점은 기존 시작 메뉴 사양을 따른다.

### 사람이 확인할 것
- 에디터에서 `save.dat` 없이 플레이하면 시작 화면에 `PLUM BLOSSOM SWORD`, `START`, `SETTINGS`가 보이고, `START`를 누르면 1스테이지가 시작된다. 멈췄다 다시 플레이하면 `CONTINUE`가 보인다. (배치 모드의 화면 갈무리에는 시작 화면이 나오지 않으므로 에디터에서 해 봐야 한다)
- 에디터에서 시작 화면의 `SETTINGS`로 슬라이더를 30·60으로 바꾸고 `CLOSE` → 플레이를 멈췄다 다시 켜서 열면 30·60이다.
- 실행하고 10초 뒤 화면 갈무리에서 왼쪽 위 `SETTINGS` 버튼이 금화 표시·저장 실패 글자 자리와 겹치지 않는다.
- 에디터에서 게임 중 `SETTINGS`(또는 Esc)로 창을 열면 전투가 계속되고, Esc로 닫힌다.
- 에디터에서 `TO TITLE`을 누르면 시작 화면에 `CONTINUE`가 보이고, 누르면 같은 스테이지 처음부터 보상 창 없이 시작된다.
- 에디터에서 `QUIT GAME`을 누르면 플레이 모드가 멈추고, 다시 켜면 `CONTINUE`가 보인다.
- 에디터에서 `save.dat`을 읽기 전용으로 바꾸고 `TO TITLE`·`QUIT GAME`을 누르면 `SAVE FAILED. TRY AGAIN.`이 보이고 떠나지 않는다.
- 에디터에서 `save.dat`을 아무 글자로 바꾸고 `START`를 누르면 손상 창이 뜬다.
- 폰 빌드에서 시작 화면에 10초 이상 백그라운드로 둔 뒤 돌아오면 시작 화면이 다시 뜨고, 안드로이드 뒤로 가기 키가 게임 중 설정 창을 여닫는다.

### 테스트 결과
- `git diff --check` 통과.
- Unity 컴파일과 플레이 모드 테스트는 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-17 18:47)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/044_start_menu_root
- 루프 브랜치: loop/20261117-182336
