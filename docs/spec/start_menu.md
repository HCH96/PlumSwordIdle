# 시작 화면과 기본 메뉴 사양

- 기능 이름: start_menu (네임스페이스 `PlumSword.Core.StartMenu`)
- 작성일: 2026-11-17

## 목표

게임을 켜면 제목과 시작 버튼이 있는 시작 화면을 먼저 보여 준다. 저장이 있으면 `CONTINUE`(이어하기), 없으면 `START`(게임 시작)로 들어간다. 시작 화면과 게임 중 모두 설정 창을 열어 배경음악·효과음 음량(0~100)을 따로 맞추고 기기(PlayerPrefs)에 저장한다. 게임 중 설정 창에서는 저장한 뒤 시작 화면으로 돌아가거나 게임을 끌 수 있고, 저장에 실패하면 오류를 보이고 그 자리에 머문다. 무엇을 보일지, 자동 저장을 켤지, 뒤로 가기 키를 어떻게 처리할지의 판단과 음량 값은 Core가, 화면·PlayerPrefs·앱 종료·루트 다시 만들기는 Game이 맡는다. 소리는 아직 없으므로 음량은 값과 알림만 둔다.

출처: `docs/design/milestone_2.md` "추가하거나 변경할 기능 → 시작 화면과 기본 메뉴", 같은 문서의 "저장과 불러오기"·"오프라인 보상"(시작 화면과 맞물리는 부분), "마일스톤 달성 기준"(처음 시작·이어하기, 음량 설정, 시작 화면 복귀와 게임 종료). 연결은 `docs/spec/save.md`, `docs/spec/offline_reward.md`, `docs/spec/stage.md`의 "연결 지점" 절.

## 동작

- **루트가 만들어진 까닭** `StartEntry`: `[Game]` 루트는 넷 중 하나로 만들어진다. `Launch`(처음 켬), `ResumeInGame`(게임 중 백그라운드 복귀로 다시 만듦), `ResumeOnTitle`(시작 화면에서 백그라운드 복귀로 다시 만듦), `TitleReturn`(게임 중 설정의 `TO TITLE`로 다시 만듦). `GameRoot`가 다음 루트의 까닭을 정적 값 `NextEntry`에 적어 두고, 새 `Awake`가 맨 앞에서 읽어 `Entry`에 담은 뒤 `NextEntry`를 `Launch`로 되돌린다. 지금의 `NextIsResumeRestart`는 `NextEntry`로 바뀐다. `IsResumeRestart`는 `Entry`가 `ResumeInGame` 또는 `ResumeOnTitle`일 때 `true`다.
- **시작 화면을 보이는지** `StartMenuRules.ShowsStartScreen(entry, isBatchMode)`: `Launch`는 배치 모드가 아닐 때만 `true`. `ResumeInGame`은 `false`. `ResumeOnTitle`, `TitleReturn`은 `true`. 정의되지 않은 값은 `true`.
  - 그래서 루프의 테스트와 화면 갈무리(배치 모드)는 처음 켤 때 시작 화면 없이 바로 시작 흐름을 한다 (`START`를 누른 것과 같다). 지금까지의 배치 모드 시작 상태가 그대로 유지된다.
- **오프라인 보상을 계산하는지** `StartMenuRules.CalculatesOfflineReward(entry)`: `TitleReturn`만 `false`, 나머지(정의되지 않은 값 포함)는 `true`. `false`이면 `LoadResult`가 `Loaded`여도 `OfflineReward`는 `null`이다. 계산하는 때와 방법은 오프라인 보상 사양 그대로다 (`Awake`에서 한 번).
- **시작 버튼** `StartMenuRules.StartButton(status)`: `Loaded`이면 `Continue`, 나머지(`Empty`, `UnsupportedVersion`, `Corrupt`, 정의되지 않은 값)는 `NewGame`. 글자는 `CONTINUE` / `START`.
- **자동 저장을 켠 채 만드는지** `StartMenuRules.AutoSaveStartsEnabled(status, hasOfflineReward)`: `status == Loaded`이고 `hasOfflineReward == false`일 때만 `true`. 나머지는 꺼진 채로 만들고, 시작 흐름에서 켠다 (아래).
  - 저장이 없거나(`Empty`), 판이 다르거나(`UnsupportedVersion`), 손상이면(`Corrupt`) 시작 화면에서 끄거나 백그라운드로 가도 파일을 쓰지 않는다. 처음 켜고 시작 화면에서 바로 끄면 빈 진행이 저장되어 다음에 `CONTINUE`가 뜨는 일, 판이 다른 파일이나 손상 파일을 누르기 전에 덮어쓰는 일을 막는다.
  - 저장이 있고 보상이 없으면 켜 둔다. 그래서 시작 화면에서 끄거나 백그라운드로 갈 때 `SaveView`의 `SaveNow`가 지금 시각을 써서, 시작 화면에 머문 시간이 다음 보상에 들어가지 않는다.
  - 보상이 있으면 오프라인 보상 사양대로 받을 때까지 꺼 둔다.
- **시작 흐름** (`GameRoot`의 비공개 메서드. 시작 버튼을 누를 때, 또는 시작 화면을 건너뛸 때 `Awake` 끝에서 한 번): `IsStartScreenShowing = false`로 한 뒤 아래 중 하나를 한다.
  1. `LoadResult.Status == Corrupt`: `[Game]`의 자식 `[SaveErrorView]`를 만들어 손상 오류 창을 띄운다. 전투는 멈춘 채, `AutoSave`는 꺼진 채다. `NEW GAME`은 저장 사양대로 `AutoSave.Enable()` → `Battle.SetPaused(false)` → `StageProgress.Begin()`.
  2. 아니고 `OfflineReward != null && OfflineReward.HasReward`: 오프라인 보상 사양대로 `Battle.SetPaused(true)`, `OfflineRewardClaim = new OfflineRewardClaim(Wallet, AutoSave, OfflineReward)`, `[Game]`의 자식 `[OfflineRewardView]`로 보상 창을 띄운다. 받으면 `Battle.SetPaused(false)` → `StageProgress.Begin()`.
  3. 아니면: `AutoSave.Enable()` → `Battle.SetPaused(false)` → `StageProgress.Begin()`. `Begin`의 `StageStarted`로 그 프레임 끝에 저장된다.
  - 보상은 `Awake`에서 계산해 둔 `OfflineReward`를 그대로 쓰고, 누를 때 다시 계산하지 않는다 (오프라인 보상 사양. 시작 화면에 머문 시간 제외).
- **시작 화면** (`ShowsStartScreen`이 `true`일 때): `IsStartScreenShowing = true`, `Battle.SetPaused(true)`. `Begin`을 부르지 않고 `[Game]`의 자식 `[StartScreenView]`로 시작 화면을 띄운다. 뒤의 게임 화면(금화, 강화 버튼, 전투, 설정 버튼)은 이미 조립되어 있지만 시작 화면이 모두 덮는다. 시작 버튼을 누르면 시작 화면을 없애고 시작 흐름을 한다. 시작 화면의 `SETTINGS`를 누르면 설정 창을 시작 화면용(`SettingsMode.Title`)으로 연다.
- **설정 창** (`SettingsView`, 루트마다 하나. `Awake`에서 닫힌 채로 만든다):
  - 열기: 시작 화면의 `SETTINGS`는 `Title`, 게임 화면 왼쪽 위의 `SETTINGS` 버튼과 뒤로 가기 키는 `InGame`으로 연다. 열 때마다 창을 캔버스의 마지막 자식으로 올리고, 슬라이더를 지금 음량으로 맞춘다 (알림 없이). 이미 열려 있을 때 `Open`을 부르면 모드만 바꾸고 열린 채 둔다.
  - 두 모드 모두 음량 슬라이더 둘과 `CLOSE`가 있다. `InGame`에만 `TO TITLE`, `QUIT GAME`과 그 실패 문구가 보인다.
  - 창이 열려 있어도 전투·스테이지·자동 저장은 멈추지 않는다 (`Battle.SetPaused`를 부르지 않는다). 가림막이 입력을 막아 뒤의 강화 버튼은 눌리지 않는다.
  - `CLOSE`: 창을 닫고 음량을 기기에 쓴다 (`saveVolume`). 닫혀 있을 때 `Close`를 부르면 아무 일도 없다 (`saveVolume`도 부르지 않는다).
- **음량** `VolumeSettings`: 배경음악 `Bgm`, 효과음 `Sfx`, 각각 `int` 0~100. 만들 때와 바꿀 때 범위 밖 값은 잘라낸다. 바꿀 때 잘라낸 값이 지금 값과 다를 때만 `Changed`를 한 번 보낸다. 소리가 없으므로 지금은 아무 데도 적용하지 않는다. 나중에 소리를 넣는 기능이 `BgmScale`·`SfxScale`(0.0~1.0)과 `Changed`를 쓴다.
  - 슬라이더를 움직이면 `SetBgm`/`SetSfx`를 부르고 옆 글자(`BGM 80`)를 맞춘다.
- **음량 저장** `VolumePrefs` (Game): PlayerPrefs의 정수 두 개.
  - 읽기 `VolumePrefs.Load()`: `new VolumeSettings(PlayerPrefs.GetInt(BgmKey, 80), PlayerPrefs.GetInt(SfxKey, 80))`. 값이 없으면 80, 범위 밖이면 생성자가 잘라낸다.
  - 쓰기 `VolumePrefs.Save(volume)`: `SetInt` 둘 다음 `PlayerPrefs.Save()`.
  - 쓰는 때 (이것이 전부다): `CLOSE`, `TO TITLE`·`QUIT GAME`에서 저장이 성공해 떠나기 직전, 설정 창의 `OnApplicationPause(true)`와 `OnApplicationQuit`.
  - 배치 모드에서는 PlayerPrefs를 읽지도 쓰지도 않는다: 음량은 `new VolumeSettings()`이고 `saveVolume`은 아무것도 하지 않는다.
- **떠나기** `LeaveGame.TryLeave(nowUtcTicks)`: `autoSave.SaveNow(nowUtcTicks)`를 부른다. 성공하면 `HasFailed = false`, `true`. 실패하면(자동 저장이 꺼져 있어 `false`인 경우 포함) `HasFailed = true`, `false`. `TO TITLE`과 `QUIT GAME`이 같은 규칙을 쓴다.
  - `TO TITLE`을 누르면: `TryLeave(DateTime.UtcNow.Ticks)`. `true`이면 `saveVolume` → `onReturnToTitle`(= `GameRoot.ReturnToTitle()`). `false`이면 창에 실패 문구를 보이고 아무 데도 가지 않는다. 다시 누르면 다시 시도한다. 그사이 `AutoSave`의 3초 자동 재시도도 돈다 (저장 사양).
  - `QUIT GAME`을 누르면: 같은 판정. `true`이면 `saveVolume` → `onQuit`(= 앱 종료). `false`이면 실패 문구를 보이고 끄지 않는다.
  - 실패 문구는 `InGame` 모드에서 `LeaveGame.HasFailed`가 `true`인 동안 보인다. 창을 닫았다 다시 열어도 그대로다. 다음 시도가 성공하면 떠나므로 다시 볼 일이 없다.
- **시작 화면으로 돌아가기** `GameRoot.ReturnToTitle()`: `Restart()`와 같은 방법으로 루트를 다시 만들되 `NextEntry = TitleReturn`이다. 지금 루트를 끄고(`SetActive(false)`) 지우므로 전투가 그 자리에서 멈춘다. `[GameRestarter]`가 다음 프레임에 새 루트를 만든다. 새 루트는 방금 쓴 파일을 읽고(`Loaded`), 보상을 계산하지 않고, 자동 저장을 켠 채 만들고, 시작 화면에 `CONTINUE`를 보인다. 누르면 시작 흐름 3번으로 저장된 스테이지의 처음부터 체력을 전부 채우고 새 무리 구성으로 시작한다 (스테이지 사양). 이미 본 소개는 다시 나오지 않는다.
  - 이미 다시 만드는 중이면(`Restart` 또는 `ReturnToTitle`을 이미 부름) 아무 일도 없다. 둘은 같은 표시(`restarting`)를 쓴다.
- **앱 종료** (`GameRoot`의 비공개 메서드): 에디터에서는 `UnityEditor.EditorApplication.isPlaying = false` (`#if UNITY_EDITOR`), 빌드에서는 `Application.Quit()`. 끌 때 `SaveView`의 `OnApplicationQuit`이 한 번 더 `SaveNow`를 한다 (저장 사양 그대로).
- **백그라운드 복귀**: 오프라인 보상 사양의 `OfflineResumeWatcher` 그대로다. 다만 `GameRoot.Restart()`가 다음 까닭을 `NextEntry = StartMenuRules.ResumeEntry(IsStartScreenShowing)`으로 정한다.
  - 시작 화면이 떠 있으면 `ResumeOnTitle`: 다시 시작 화면이 뜨고 보상을 다시 계산한다 (백그라운드에 있던 시간이 들어간다).
  - 아니면 `ResumeInGame`: 시작 화면 없이 시작 흐름을 바로 한다 (지금과 같다). 보상 창이나 손상 창이 떠 있을 때는 시작 화면이 이미 없으므로 이쪽이다.
- **뒤로 가기 키** (`BackKeyWatcher`): 매 프레임 `UnityEngine.InputSystem.Keyboard.current`의 `escapeKey.wasPressedThisFrame`을 본다 (PC의 Esc. 안드로이드의 뒤로 가기 키도 이 키로 들어온다). `Keyboard.current`가 `null`이면 아무것도 하지 않는다. 눌렸으면 `StartMenuRules.BackKey(settings.IsOpen, root.IsStartScreenShowing, root.StageProgress.HasBegun)`의 결과대로 한다.
  - 설정 창이 열려 있으면 `CloseSettings` (`CLOSE`와 같다).
  - 닫혀 있고, 시작 화면이 없고, 스테이지가 시작됐으면 `OpenSettings` (`InGame`으로 연다).
  - 그 밖(시작 화면, 보상 창·손상 창이 떠 있어 아직 시작 전)은 `None`. 앱을 끄지 않는다.
- **시작 화면에 머문 시간** (정리): 보상은 켤 때 한 번 계산하므로 시작 화면에 머문 시간은 이번 보상에 들어가지 않는다. 보상이 없는 저장이면 자동 저장이 켜져 있어, 시작 화면에서 끄거나 백그라운드로 갈 때 그 시각이 저장되어 다음 보상에도 들어가지 않는다. 보상을 받기 전에 시작 화면에서 끄면 파일을 쓰지 않으므로 다음에 켤 때 옛 저장 시각부터 다시 계산한다 (머문 시간도 들어간다. 오프라인 보상 사양이 보상 창에 대해 허용한 것과 같다). 시작 화면으로 돌아간 뒤에는 보상을 계산하지 않는다.
- **매화 꽃잎 흩날림** `PetalField` (2026-11-17 추가. 시작 화면 그림과 꽃잎 그림이 있을 때만): 꽃잎 `DefaultCount`(50)장 중 약 22~40장이 늘 화면에 떠 있고, 새 꽃잎이 약 0.7초마다 한 장씩 꾸준히 들어와 시작 화면 전체를 오른쪽 아래로 흩날린다. 좌표는 꽃잎 영역 가운데가 원점, 오른쪽·위쪽이 +인 UI 단위다.
  - 꽃잎마다 깊이 `Depth`(0 = 멂 ~ 1 = 가까움, 고르게 무작위)를 정하고, 깊이에 비례해 크기(긴 변 7~14), 떨어지는 속도(초당 12~26에 ×0.85~1.15 무작위), 불투명도(0.55~1.0), 바람을 받는 정도(×0.5~1.0)가 커진다. 그래서 가까운 꽃잎일수록 크고 진하고 빠르다.
  - 바람 `Wind` = 8 + 6·sin(0.35t) + 3·sin(0.9t + 1) (t = 지난 초. 초당 −1 ~ 17, 대개 오른쪽): 천천히 세졌다 약해지는 돌풍.
  - 흔들림: 꽃잎마다 폭 6~16, 빠르기 0.8~1.8 rad/s의 좌우 흔들림(가로 속도 = 폭 × 빠르기 × cos 위상). 옆으로 크게 움직일 때는 떨어지는 속도가 0.6배까지 줄어 나뭇잎처럼 미끄러진다 (× (0.6 + 0.4·sin²)).
  - 돌기·뒤집기: 화면 평면 회전 초당 20~70도(방향 무작위), 뒤집힘 위상 1.0~3.0 rad/s로 가로 배율 `FlipScale` = 부호(cos) × (0.25 + 0.75·|cos|). 부호가 바뀌면 그림이 좌우로 뒤집혀 앞뒷면처럼 보이고, 0.25보다 얇아지지 않아 사라지지 않는다.
  - 나가면 기다리기: 아래로 `Margin`(16)보다 더, 오른쪽으로 16보다 더, 왼쪽으로 64보다 더 나가면 깊이·종류·움직임을 새로 정하고 화면 밖 기다리는 자리에 멈춘다 (`IsWaiting = true`, 움직이지 않고 그리지 않음). 기다리는 자리는 `LeftShare` 몫으로 왼쪽 바깥 띠(왼쪽 끝 − 16 ~ − 40, 세로는 아래에서 높이의 1/4 위 ~ 위 끝), 나머지는 위쪽 바깥 띠(위 끝 + 16 ~ + 40, 가로는 화면 폭). `LeftShare` = 들어오는 양의 비 (왼쪽: 바람 8 × 0.75 × 높이 × 0.75, 위: 평균 떨어지는 속도 19 × 0.8 × 폭). 640×360에서 약 14%.
  - 내보내기 (꾸준히 들어오게 하는 부분): `EmitInterval`(0.7초) × 0.5~1.5 무작위 간격마다 기다리는 꽃잎을 먼저 들어온 순서로 한 장 내보낸다. 내보낼 때 화면 가장자리 바로 바깥(위쪽이면 위 끝 + 크기×0.6 + 0~4, 왼쪽이면 왼쪽 끝 − 같은 값)으로 옮겨서 곧바로 화면에 들어온다. 간격이 됐는데 기다리는 꽃잎이 없으면 다음에 나가는 꽃잎을 바로 내보낸다. 꽃잎 50장은 이 속도로 내보내도 늘 몇 장(5분 시험에서 8장 이상)이 남도록 정한 수다. 그래서 몰려오거나 한동안 끊기지 않는다 (2분 동안 새 꽃잎이 들어오는 간격 3.5초 이하, 초당 평균 1.2장 이상을 에딧 모드 테스트로 확인).
  - 처음: 모두 영역 안 전체에 고르게 흩어 둔 뒤, 만들 때 `PrewarmSeconds`(40초)만큼 미리 움직이고 `Elapsed`를 0으로 되돌린다. 그래서 첫 화면부터 꽃잎이 흩어져 있고, 나가고 들어오는 때가 서로 어긋난 흐름 중간에서 시작한다.
  - `Tick(deltaSeconds)`: 0 이하·NaN·무한이면 아무것도 하지 않고, 0.1초(`MaxStep`)보다 길면 0.1초만 움직인다 (멈췄다 돌아올 때 한꺼번에 튀지 않게). 같은 시드의 `System.Random`이면 결과가 같다.
  - `Resize(width, height)`: 영역이 바뀌면 모든 꽃잎 위치(기다리는 꽃잎 포함)를 같은 비율로 늘이거나 줄인다 (X × 새 폭/옛 폭, Y × 새 높이/옛 높이). 크기·속도는 그대로다. `GameRoot`는 캔버스를 만든 프레임에 시작 화면을 만드는데 그때 캔버스는 아직 화면 픽셀 크기(예: 1920×1080)이고 다음 프레임에 640×360 기준으로 줄어든다. 위치를 그대로 두면 대부분이 화면 밖으로 밀려 한꺼번에 위에서 다시 떨어지므로(2026-11-17 고친 문제), 비율로 옮겨 흩어진 모양을 지킨다. 0 이하·NaN·무한은 `ArgumentOutOfRangeException`.
  - 시작 화면에서만 움직이고, 시작 버튼을 눌러 화면이 없어지면 함께 없어진다. 게임 속도와 상관없이 실제 시간(`Time.unscaledDeltaTime`)으로 움직인다.
- **버튼 반응** `ButtonHoverState` (2026-11-17 추가. 시작 화면의 그림 버튼 둘에만. 그림이 없을 때의 대체 버튼에는 없다): 마우스를 올리면(호버) 버튼 그림이 살짝 커지고(×1.06) 밝아지며(색 곱 0.88 → 1.0) 2만큼 위로 뜬다. 누르는 동안은 조금 작아지고(×0.97) 덜 밝아지며(0.95) 원래 높이로 내려앉는다 (누름이 올림보다 우선). 마우스를 내리면 평소로 돌아간다. 터치처럼 올림 없이 바로 누르면 누름 모양만 보인다.
  - 값 `Hover`·`Press`(0~1)는 목표(올림·누름이면 1, 아니면 0)를 지수 감쇠로 따라간다: 한 번에 `1 − e^(−14 × dt)`만큼. 0.1초에 약 75%, 0.2초에 약 94%.
  - 모양: `Scale`·`Brightness`·`Lift`는 평소 값에서 올림 값으로 `Hover`만큼, 그 결과에서 누름 값으로 `Press`만큼 섞는다.
  - `SetHovered(bool)`, `SetPressed(bool)`, `Exit()`(둘 다 풀기), `Tick(deltaSeconds)`(0 이하·NaN·무한은 무시, 0.1초보다 길면 0.1초만), `Snap()`(바로 목표 값으로).
  - 버튼(눌리는 영역)은 움직이지 않고 그림 자식만 움직인다. 그림이 커지거나 뜨면서 마우스가 버튼 가장자리에서 들어갔다 나갔다를 되풀이하지 않게 하려는 것이다. 클릭 판정과 동작은 그대로다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| 저장 없음 (`Empty`) | 시작 화면에 `START`. 누르면 1스테이지부터 | 시작 화면 동안 자동 저장 꺼짐. 누르면 켜고 `Begin`, 그 프레임 끝에 처음 저장 |
| 판이 다름 (`UnsupportedVersion`) | `START`. 손상 창 없음 | 위와 같다. 누르기 전에는 파일을 덮어쓰지 않음 |
| 손상 (`Corrupt`) | `START`. 누르면 손상 창, `NEW GAME`으로 시작 | 누르기 전과 창이 떠 있는 동안 아무것도 쓰지 않음 |
| 저장 있음, 보상 없음 | `CONTINUE`. 누르면 저장된 스테이지 처음부터 | 자동 저장 켜짐. 시작 화면에서 끄면 지금 시각 저장 |
| 저장 있음, 보상 있음 | `CONTINUE`. 누르면 보상 창, `CLAIM` 뒤 시작 | 받기 전에는 아무것도 쓰지 않음 |
| 시작 버튼을 두 번 (없어지기 전) | 시작 흐름은 한 번만 | |
| 시작 화면에서 설정 창이 열린 채 | 가림막에 막혀 시작 버튼이 눌리지 않음 | |
| 배치 모드로 처음 켬 | 시작 화면 없이 시작 흐름 바로 | 지금까지와 같은 시작 상태 |
| 배치 모드에서 `TO TITLE` | 시작 화면이 뜨지만 저장이 메모리라 `START` | 자동 테스트 대상 아님 |
| 시작 화면에서 백그라운드 10초 이상 | `ResumeOnTitle`로 다시 만듦, 시작 화면 | 보상 다시 계산 |
| 게임 중(보상 창·손상 창 포함) 백그라운드 10초 이상 | `ResumeInGame`, 시작 화면 없이 시작 흐름 | 오프라인 보상 사양 그대로 |
| 시작 화면으로 돌아간 뒤 `CONTINUE` | 보상 창 없이 시작 | `OfflineReward` null |
| `TO TITLE` 저장 실패 | 머묾, 실패 문구, 다시 누르면 다시 시도 | 전투 계속. `HasFailed` true |
| 실패 뒤 다시 눌러 성공 | 시작 화면으로 | `HasFailed` false |
| `QUIT GAME` 저장 실패 | 끄지 않음, 실패 문구 | 위와 같다 |
| 자동 저장이 꺼진 채 `TryLeave` | 실패와 같다 (`false`, `HasFailed` true) | 쓰지 않음 |
| `TO TITLE`을 두 번, 또는 복귀 다시 만들기와 겹침 | 다시 만들기는 한 번만 | |
| 게임 중 설정 창이 열려 있음 | 전투·스테이지·자동 저장 계속 | 강화 버튼은 가림막에 막힘 |
| 음량 범위 밖 (−5, 150, `int.MinValue`, `int.MaxValue`) | 잘라냄 (0 또는 100) | 예외 없음 |
| 같은 음량으로 다시 설정 (잘라낸 뒤 같은 경우 포함) | 알림 없음 | |
| PlayerPrefs에 음량 없음 / 범위 밖 | 80 / 잘라냄 | |
| 닫힌 설정 창을 `Close` | 아무 일 없음 | `saveVolume` 부르지 않음 |
| 뒤로 가기: 설정 창 열림 | 닫음 | 음량을 기기에 씀 |
| 뒤로 가기: 게임 중, 창 닫힘 | `InGame`으로 엶 | |
| 뒤로 가기: 시작 화면, 보상 창·손상 창 | 아무 일 없음 | |
| 키보드 없음 (`Keyboard.current` null) | 아무 일 없음 | |
| 에디터에서 `QUIT GAME` | 플레이 모드가 멈춤 | |
| `LeaveGame` 생성자에 `null` | `ArgumentNullException` | |
| 화면 `Initialize`의 참조 인자에 `null` | `ArgumentNullException` | |

`GameRoot.ReturnToTitle()`, `Restart()`의 까닭 정하기, 시작 화면이 뜬 실제 흐름, `BackKeyWatcher`, 앱 종료, `VolumePrefs`는 자동 테스트 대상이 아니다 (배치 모드 시작 상태와 화면 단위 동작만 플레이 모드 테스트로 확인한다). 판단(`StartMenuRules`, `VolumeSettings`, `LeaveGame`, 표시 글자)은 Core에 있고 에딧 모드 테스트로 판정한다.

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 음량 범위 `VolumeSettings.MinVolume` / `MaxVolume` | 0 / 100 | `int` |
| 처음 음량 `VolumeSettings.DefaultVolume` | 80 (임시) | 배경음악·효과음 둘 다 |
| 비율 `BgmScale`, `SfxScale` | `Bgm / 100.0`, `Sfx / 100.0` | `double`, 반올림 없음. 80 → 0.8 |
| PlayerPrefs 키 `VolumePrefs.BgmKey` / `SfxKey` | `PlumSword.BgmVolume` / `PlumSword.SfxVolume` | `int` 값 |
| 백그라운드 복귀 때 다시 만드는 기준 | 오프라인 보상 사양 그대로 (10초) | 이 기능의 수치 아님 |
| 꽃잎 수 `PetalField.DefaultCount` | 50 (임시) | 화면에 약 22~40장, 나머지는 화면 밖에서 차례를 기다림 |
| 내보내는 간격 `EmitInterval` × `MinEmitJitter`~`MaxEmitJitter` | 0.7초 × 0.5~1.5 (임시) | |
| 내보낼 때 가장자리 바깥 거리 | 크기 × 0.6 + 0~`EdgeJitter`(4) | |
| 미리 움직이기 `PrewarmSeconds` | 40초 | |
| 왼쪽에서 나타나는 세로 범위 `LeftSpawnBottom` | 아래에서 높이 × 0.25 위 ~ 위 끝 | |
| 꽃잎 크기 `MinSize` / `MaxSize` | 7 / 14 (임시) | 긴 변, UI 단위. 깊이에 비례 |
| 떨어지는 속도 `MinFallSpeed` / `MaxFallSpeed` | 12 / 26 (임시) | 초당 UI 단위. 깊이에 비례, ×0.85~1.15 |
| 불투명도 `MinAlpha` / `MaxAlpha` | 0.55 / 1.0 (임시) | 깊이에 비례 |
| 흔들림 폭 / 빠르기 | 6~16 / 0.8~1.8 rad/s (임시) | |
| 회전 `MinSpinSpeed` / `MaxSpinSpeed` | 20 / 70 (임시) | 초당 도, 방향 무작위 |
| 뒤집힘 빠르기 / 가장 얇은 배율 | 1.0~3.0 rad/s / 0.25 (임시) | |
| 바람 `BaseWind` / `GustWind` / `FlutterWind` | 8 / 6 / 3 (임시) | 초당 UI 단위 |
| 다시 나타나는 기준 `Margin` / 띠 `SpawnBand` | 16 / 24 (임시) | |
| 한 번에 움직이는 가장 긴 시간 `MaxStep` | 0.1초 | |
| 버튼 호버 배율 / 누름 배율 `ButtonHoverState.HoverScale` / `PressScale` | 1.06 / 0.97 (임시) | |
| 버튼 밝기 평소 / 호버 / 누름 `NormalBrightness` / `HoverBrightness` / `PressBrightness` | 0.88 / 1.0 / 0.95 (임시) | 그림 색에 곱함 |
| 버튼 뜨는 높이 호버 / 누름 `HoverLift` / `PressLift` | 2 / 0 (임시) | UI 단위 |
| 버튼 반응 빠르기 `Response` / 한 번에 가장 긴 시간 `MaxStep` | 14 /초 / 0.1초 (임시) | |

**표시 글자** (`StartMenuText`, 한글 글꼴 전이라 영문):

| 이름 | 값 | 예 |
| --- | --- | --- |
| `Title` | `PLUM BLOSSOM SWORD` (임시 제목. 한글 글꼴이 준비되면 `매화검 키우기`로 바꾼다) | |
| `NewGameButton` | `START` | |
| `ContinueButton` | `CONTINUE` | |
| `SettingsButton` | `SETTINGS` | 시작 화면과 게임 화면의 버튼 |
| `SettingsTitle` | `SETTINGS` | 설정 창 제목 |
| `ToTitleButton` | `TO TITLE` | |
| `QuitButton` | `QUIT GAME` | |
| `CloseButton` | `CLOSE` | |
| `LeaveFailedMessage` | `SAVE FAILED. TRY AGAIN.` | |
| `StartButtonLabel(kind)` | `Continue`이면 `CONTINUE`, 나머지(정의되지 않은 값 포함)는 `START` | |
| `BgmLine(volume)` | `BGM ` + 십진수 (불변 문화권, 자릿수 구분 없음, 잘라내지 않음) | 80 → `BGM 80`, −5 → `BGM -5` |
| `SfxLine(volume)` | `SFX ` + 십진수 (같은 규칙) | 0 → `SFX 0` |

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.StartMenu
{
    public enum StartEntry
    {
        Launch = 0,          // 처음 켬
        ResumeInGame = 1,    // 게임 중(시작 화면이 없을 때) 백그라운드 복귀로 다시 만듦
        ResumeOnTitle = 2,   // 시작 화면에서 백그라운드 복귀로 다시 만듦
        TitleReturn = 3,     // 게임 중 설정의 TO TITLE 로 다시 만듦
    }

    public enum StartButtonKind
    {
        NewGame = 0,    // START
        Continue = 1,   // CONTINUE
    }

    public enum SettingsMode
    {
        Title = 0,      // 시작 화면에서 엶. 음량과 CLOSE 만
        InGame = 1,     // 게임 중에 엶. TO TITLE, QUIT GAME 도 있음
    }

    public enum BackKeyAction
    {
        None = 0,
        OpenSettings = 1,
        CloseSettings = 2,
    }

    public static class StartMenuRules
    {
        // Launch: !isBatchMode. ResumeInGame: false. ResumeOnTitle, TitleReturn: true. 정의되지 않은 값: true. 예외 없음.
        public static bool ShowsStartScreen(StartEntry entry, bool isBatchMode);

        // TitleReturn 만 false. 나머지(정의되지 않은 값 포함) true. 예외 없음.
        public static bool CalculatesOfflineReward(StartEntry entry);

        // Loaded 면 Continue, 나머지(정의되지 않은 값 포함) NewGame. 예외 없음.
        public static StartButtonKind StartButton(PlumSword.Core.Save.SaveLoadStatus status);

        // status == Loaded && !hasOfflineReward 일 때만 true. 예외 없음.
        public static bool AutoSaveStartsEnabled(PlumSword.Core.Save.SaveLoadStatus status, bool hasOfflineReward);

        // 백그라운드 복귀로 다시 만들 때의 까닭. true 면 ResumeOnTitle, false 면 ResumeInGame.
        public static StartEntry ResumeEntry(bool isStartScreenShowing);

        // isSettingsOpen 이면 CloseSettings. 아니고 !isStartScreenShowing && hasBegun 이면 OpenSettings. 나머지 None.
        public static BackKeyAction BackKey(bool isSettingsOpen, bool isStartScreenShowing, bool hasBegun);
    }

    public sealed class VolumeSettings
    {
        public const int MinVolume = 0;
        public const int MaxVolume = 100;
        public const int DefaultVolume = 80;   // (임시)

        // 두 값을 0~100 으로 잘라내 담는다. 예외 없음. 알림 없음.
        public VolumeSettings(int bgm = DefaultVolume, int sfx = DefaultVolume);

        public int Bgm { get; }
        public int Sfx { get; }
        public double BgmScale { get; }   // Bgm / 100.0
        public double SfxScale { get; }   // Sfx / 100.0

        // Bgm 또는 Sfx 가 실제로 바뀐 뒤에 한 번.
        public event System.Action Changed;

        // 0~100 으로 잘라낸 값이 지금과 다를 때만 바꾸고 Changed 한 번. 같으면 아무 일도 없다. 예외 없음.
        public void SetBgm(int volume);
        public void SetSfx(int volume);
    }

    public sealed class LeaveGame
    {
        // autoSave 가 null 이면 ArgumentNullException. 아무것도 바꾸지 않는다. 시작 상태: HasFailed false.
        public LeaveGame(PlumSword.Core.Save.AutoSave autoSave);

        // 마지막 TryLeave 가 실패했으면 true. 성공하면 false.
        public bool HasFailed { get; }

        // autoSave.SaveNow(nowUtcTicks). true: HasFailed = false, true. false(꺼져 있음 포함): HasFailed = true, false.
        public bool TryLeave(long nowUtcTicks);
    }

    // 꽃잎 한 장의 지금 상태. PetalField 만 바꾼다. 좌표는 영역 가운데 원점, 오른쪽·위쪽 +.
    public sealed class Petal
    {
        public double X { get; }
        public double Y { get; }
        public double Size { get; }        // 긴 변 (UI 단위)
        public int Variant { get; }        // 0 ~ variantCount-1 (그림 종류)
        public double Rotation { get; }    // 0 이상 360 미만 (도, 반시계 +)
        public double FlipScale { get; }   // ±(0.25 ~ 1). 음수면 좌우 뒤집힘
        public double Alpha { get; }       // 0.55 ~ 1.0
        public double Depth { get; }       // 0 = 멂 ~ 1 = 가까움
        public bool IsWaiting { get; }     // 화면 밖에서 내보낼 차례를 기다리는 중 (움직이지 않음, 그리지 않음)
    }

    public sealed class PetalField
    {
        public const int DefaultCount = 50;
        public const double MinSize = 7, MaxSize = 14;
        public const double MinFallSpeed = 12, MaxFallSpeed = 26;
        public const double MinAlpha = 0.55, MaxAlpha = 1.0;
        public const double MinSwayAmplitude = 6, MaxSwayAmplitude = 16;
        public const double MinSwayRate = 0.8, MaxSwayRate = 1.8;
        public const double MinSpinSpeed = 20, MaxSpinSpeed = 70;
        public const double MinFlipRate = 1.0, MaxFlipRate = 3.0;
        public const double MinFlipScale = 0.25;
        public const double BaseWind = 8, GustWind = 6, FlutterWind = 3;
        public const double Margin = 16;
        public const double SpawnBand = 24;
        public const double MaxStep = 0.1;
        public const double PrewarmSeconds = 40;
        public const double LeftSpawnBottom = 0.25;
        public const double EmitInterval = 0.7;
        public const double MinEmitJitter = 0.5, MaxEmitJitter = 1.5;
        public const double EdgeJitter = 4;

        // 만들면서 PrewarmSeconds 만큼 미리 움직이고 Elapsed 를 0 으로 되돌린다.
        // width·height 가 0 이하·NaN·무한, count < 0, variantCount < 1 이면 ArgumentOutOfRangeException. random 이 null 이면 ArgumentNullException.
        public PetalField(double width, double height, int count, int variantCount, System.Random random);

        public double Width { get; }
        public double Height { get; }
        public double Elapsed { get; }     // Tick 으로 지난 초 (MaxStep 으로 자른 값의 합)
        public double Wind { get; }        // 지금 바람 (초당, 오른쪽 +)
        public System.Collections.Generic.IReadOnlyList<Petal> Petals { get; }
        public int WaitingCount { get; }   // 기다리는 꽃잎 수
        public double LeftShare { get; }   // 다시 나타날 때 왼쪽 바깥을 고를 몫 (0~1)

        // 모든 꽃잎 위치를 새 크기에 비례해 옮긴다.
        public void Resize(double width, double height);
        public void Tick(double deltaSeconds);
    }

    // 시작 화면 그림 버튼의 호버·누름 반응 값.
    public sealed class ButtonHoverState
    {
        public const double HoverScale = 1.06, PressScale = 0.97;
        public const double NormalBrightness = 0.88, HoverBrightness = 1.0, PressBrightness = 0.95;
        public const double HoverLift = 2, PressLift = 0;
        public const double Response = 14;
        public const double MaxStep = 0.1;

        public bool IsHovered { get; }
        public bool IsPressed { get; }
        public double Hover { get; }        // 0~1
        public double Press { get; }        // 0~1
        public double Scale { get; }        // PressScale ~ HoverScale
        public double Brightness { get; }   // NormalBrightness ~ HoverBrightness
        public double Lift { get; }         // 0 ~ HoverLift

        public void SetHovered(bool hovered);
        public void SetPressed(bool pressed);
        public void Exit();                 // 올림·누름 모두 풂
        public void Tick(double deltaSeconds);
        public void Snap();
    }

    public static class StartMenuText
    {
        public const string Title = "PLUM BLOSSOM SWORD";
        public const string NewGameButton = "START";
        public const string ContinueButton = "CONTINUE";
        public const string SettingsButton = "SETTINGS";
        public const string SettingsTitle = "SETTINGS";
        public const string ToTitleButton = "TO TITLE";
        public const string QuitButton = "QUIT GAME";
        public const string CloseButton = "CLOSE";
        public const string LeaveFailedMessage = "SAVE FAILED. TRY AGAIN.";

        // Continue → CONTINUE, 나머지(정의되지 않은 값 포함) → START.
        public static string StartButtonLabel(StartButtonKind kind);

        // "BGM " / "SFX " + 불변 문화권 십진수. 잘라내지 않는다. 예외 없음.
        public static string BgmLine(int volume);
        public static string SfxLine(int volume);
    }
}
```

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래를 더하고 바꾼다
    {
        // 이 루트가 만들어진 까닭. Awake 맨 앞에서 NextEntry 를 읽어 담는다.
        public PlumSword.Core.StartMenu.StartEntry Entry { get; private set; }

        // 시작 화면이 떠 있으면 true. 시작 흐름을 시작하면 false.
        public bool IsStartScreenShowing { get; private set; }

        // 루트에 하나뿐인 음량. 배치 모드면 new VolumeSettings(), 아니면 VolumePrefs.Load().
        public PlumSword.Core.StartMenu.VolumeSettings Volume { get; private set; }

        // 루트에 하나뿐인 떠나기 판정. new LeaveGame(AutoSave).
        public PlumSword.Core.StartMenu.LeaveGame LeaveGame { get; private set; }

        // 루트에 하나뿐인 설정 창.
        public PlumSword.Game.StartMenu.SettingsView Settings { get; private set; }

        // (이미 있음, 뜻이 바뀜) Entry 가 ResumeInGame 또는 ResumeOnTitle 이면 true.
        public bool IsResumeRestart { get; }

        // (이미 있음, 바뀜) NextEntry = StartMenuRules.ResumeEntry(IsStartScreenShowing). 나머지는 그대로.
        public void Restart();

        // NextEntry = TitleReturn 으로 Restart 와 같은 방법(끄고 지우고 [GameRestarter])으로 다시 만든다.
        // 이미 다시 만드는 중이면 아무 일도 없다.
        public void ReturnToTitle();

        // 다음 루트의 까닭. 지금의 NextIsResumeRestart 를 대신한다. 새 Awake 가 읽고 Launch 로 되돌린다.
        internal static PlumSword.Core.StartMenu.StartEntry NextEntry;
    }
}

namespace PlumSword.Game.StartMenu
{
    public sealed class StartScreenView : MonoBehaviour
    {
        // canvas 의 마지막 자식으로 시작 화면 "StartScreenPanel" 을 만든다. canvas, onStart, onOpenSettings 중 null 이면 ArgumentNullException.
        // 시작 버튼: onStart 를 한 번 부르고 자기 오브젝트와 패널을 없앤다. 없어지기 전에 또 눌러도 한 번만.
        // SETTINGS 버튼: 누를 때마다 onOpenSettings.
        // 그림은 StartScreenArt.Load() 로 읽는다 (아래 오버로드에 그대로 넘김).
        public void Initialize(
            UnityEngine.Canvas canvas,
            PlumSword.Core.StartMenu.StartButtonKind button,
            System.Action onStart,
            System.Action onOpenSettings);

        // art 가 null 이면 그림 없이 대체 표시(단색 바탕, 영문 글자)로 만든다. 테스트가 대체 표시를 확인할 때 쓴다.
        public void Initialize(
            UnityEngine.Canvas canvas,
            PlumSword.Core.StartMenu.StartButtonKind button,
            System.Action onStart,
            System.Action onOpenSettings,
            StartScreenArt art);
    }

    // 시작 화면 그림 네 장 (Resources 경로 Art/StartMenu/...). 하나라도 없으면 Load 가 null.
    // 꽃잎 그림 세 장은 따로: 찾은 것만 Petals 에 담는다 (없으면 빈 목록 → 꽃잎 없이 그림).
    public sealed class StartScreenArt
    {
        public const string BackgroundPath = "Art/StartMenu/title_background";
        public const string StartButtonPath = "Art/StartMenu/button_start";
        public const string ContinueButtonPath = "Art/StartMenu/button_continue";
        public const string SettingsButtonPath = "Art/StartMenu/button_settings";
        public static readonly string[] PetalPaths;   // petal_open, petal_curled, petal_folded (이 순서가 Variant 0, 1, 2)

        // petals 가 null 이면 꽃잎 없음. null 항목은 뺀다.
        public StartScreenArt(UnityEngine.Sprite background, UnityEngine.Sprite startButton, UnityEngine.Sprite continueButton, UnityEngine.Sprite settingsButton,
            System.Collections.Generic.IReadOnlyList<UnityEngine.Sprite> petals = null);
        public UnityEngine.Sprite Background { get; }
        public UnityEngine.Sprite StartButton { get; }
        public UnityEngine.Sprite ContinueButton { get; }
        public UnityEngine.Sprite SettingsButton { get; }
        public System.Collections.Generic.IReadOnlyList<UnityEngine.Sprite> Petals { get; }

        public static StartScreenArt Load();

        // Continue → ContinueButton, 나머지(정의되지 않은 값 포함) → StartButton.
        public UnityEngine.Sprite StartButtonSprite(PlumSword.Core.StartMenu.StartButtonKind kind);
    }

    // 그림 버튼에 붙어 포인터 들어옴·나감·누름·뗌을 ButtonHoverState 에 전하고, 매 프레임(unscaledDeltaTime) 그림 자식에
    // 배율(localScale x·y), 높이(anchoredPosition.y = Lift), 색(Brightness, Brightness, Brightness, 1)을 적용한다.
    // 누름·뗌은 왼쪽 버튼(eventData 가 null 이면 그대로)만. 꺼지면(OnDisable) 평소 모양으로 바로 돌아간다.
    public sealed class StartButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public PlumSword.Core.StartMenu.ButtonHoverState State { get; }

        // visual 이 null 이면 ArgumentNullException. Snap 한 평소 모양으로 바로 맞춘다.
        public void Initialize(UnityEngine.RectTransform visual);
    }

    // 꽃잎 Image 를 PetalField 에 맞춰 그린다. 자기 RectTransform 이 꽃잎 영역이다.
    public sealed class StartScreenPetals : MonoBehaviour
    {
        public PlumSword.Core.StartMenu.PetalField Field { get; }

        // sprites 가 null 이거나 비었으면 ArgumentException. 자식으로 "Petal" Image count 개를 만든다.
        // Update: 영역 크기가 Field 와 다르면 Resize → Tick(Time.unscaledDeltaTime) → 위치·회전·배율·크기·불투명도 적용.
        public void Initialize(System.Collections.Generic.IReadOnlyList<UnityEngine.Sprite> sprites, int count, int seed);
    }

    public sealed class SettingsView : MonoBehaviour
    {
        // canvas 아래에 게임 화면 버튼 "GameSettingsButton" 과 닫힌 설정 창 "SettingsPanel" 을 만든다.
        // 참조 인자 중 null 이 있으면 ArgumentNullException.
        // GameSettingsButton 을 누르면 Open(InGame).
        // TO TITLE / QUIT GAME: leaveGame.TryLeave(DateTime.UtcNow.Ticks). true 면 saveVolume → onReturnToTitle / onQuit.
        //   false 면 실패 문구를 보이고 머문다.
        // OnApplicationPause(true), OnApplicationQuit: saveVolume.
        public void Initialize(
            UnityEngine.Canvas canvas,
            PlumSword.Core.StartMenu.VolumeSettings volume,
            PlumSword.Core.StartMenu.LeaveGame leaveGame,
            System.Action onReturnToTitle,
            System.Action onQuit,
            System.Action saveVolume);

        public bool IsOpen { get; }
        public PlumSword.Core.StartMenu.SettingsMode Mode { get; }   // 마지막으로 연 모드. 처음 Title

        // 창을 보이고 캔버스의 마지막 자식으로 올린다. 슬라이더를 지금 음량으로(알림 없이), 모드에 맞게 버튼을 보인다.
        // 이미 열려 있으면 모드만 바꾼다.
        public void Open(PlumSword.Core.StartMenu.SettingsMode mode);

        // 열려 있으면 닫고 saveVolume. 닫혀 있으면 아무 일도 없다.
        public void Close();
    }

    public sealed class BackKeyWatcher : MonoBehaviour
    {
        // root, settings 중 null 이면 ArgumentNullException.
        // Update: Keyboard.current?.escapeKey.wasPressedThisFrame 이면
        //   StartMenuRules.BackKey(settings.IsOpen, root.IsStartScreenShowing, root.StageProgress.HasBegun) 대로 Open(InGame) / Close().
        public void Initialize(PlumSword.Game.GameRoot root, SettingsView settings);
    }

    public static class VolumePrefs
    {
        public const string BgmKey = "PlumSword.BgmVolume";
        public const string SfxKey = "PlumSword.SfxVolume";

        // new VolumeSettings(PlayerPrefs.GetInt(BgmKey, 80), PlayerPrefs.GetInt(SfxKey, 80)).
        public static PlumSword.Core.StartMenu.VolumeSettings Load();

        // SetInt 둘 → PlayerPrefs.Save(). volume 이 null 이면 ArgumentNullException.
        public static void Save(PlumSword.Core.StartMenu.VolumeSettings volume);
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot`가 `Volume`, `LeaveGame`, `SettingsView` 하나씩을 만들어 들고 있는다. `TryLeave`를 부르는 것은 `SettingsView` 하나뿐이다. `ReturnToTitle`은 `SettingsView`와 엔딩 화면에서 부른다. 시작 흐름과 `Begin()`을 부르는 것은 `GameRoot` 하나뿐이다 (스테이지 사양과 같다). 소리를 넣는 기능은 나중에 `GameRoot.Volume`의 `BgmScale`·`SfxScale`·`Changed`를 쓴다.
- **`GameRoot.Awake` 순서** (저장·오프라인 보상 사양의 순서를 이것으로 대신한다):
  - 회귀 사양에 따라 `PlayTime`을 복원하고 회귀 규칙을 켠다. 천마 처치 후 저장을 지우고 엔딩을 띄우며 엔딩에서 시작 화면으로 돌아간다.
  1. 맨 앞: `Entry = NextEntry`, `NextEntry = Launch`. (오프라인 보상 사양의 "`IsResumeRestart`를 정적 표시에서 읽고 지운다"가 여기로 온다.)
  2. 저장소를 고르고 `LoadResult`를 읽는다 (저장 사양 그대로).
  3. `Wallet`, `StatUpgrades`, `Battle`을 만든다 (그대로).
  4. 캔버스, 이벤트 시스템, 화면 조립(금화, 강화, 전투), 스테이지(`StageProgress`와 `[StageView]`. `Begin()`은 아직)은 그대로다.
  5. 보상: `LoadResult.Status == Loaded && StartMenuRules.CalculatesOfflineReward(Entry)`이면 `OfflineReward = OfflineRewardCalculator.Calculate(LoadResult.Data.SavedAtUtcTicks, DateTime.UtcNow.Ticks, StageProgress.StageNumber)`, 아니면 `null`.
  6. `AutoSave = new AutoSave(저장소, Wallet, StatUpgrades, StageProgress, enabled: StartMenuRules.AutoSaveStartsEnabled(LoadResult.Status, OfflineReward != null && OfflineReward.HasReward))`. `[SaveView]`는 그대로다.
  7. (새로) `Volume = Application.isBatchMode ? new VolumeSettings() : VolumePrefs.Load()`. `LeaveGame = new LeaveGame(AutoSave)`. `[Game]`의 자식 `[SettingsView]`를 만들어 `Initialize(UiCanvas, Volume, LeaveGame, ReturnToTitle, 앱 종료, 음량 쓰기)`하고 `Settings`에 담는다. 음량 쓰기는 배치 모드면 아무것도 하지 않고, 아니면 `VolumePrefs.Save(Volume)`.
  8. (바뀜) `StartMenuRules.ShowsStartScreen(Entry, Application.isBatchMode)`이면 시작 화면: `IsStartScreenShowing = true`, `Battle.SetPaused(true)`, `[Game]`의 자식 `[StartScreenView]`를 만들어 `Initialize(UiCanvas, StartMenuRules.StartButton(LoadResult.Status), 시작 흐름, () => Settings.Open(SettingsMode.Title))`. 아니면 시작 흐름을 바로 한다. (저장·오프라인 보상 사양의 6번 "손상 창 / 보상 창 / `Begin`"은 시작 흐름 안으로 옮겨졌다.)
  9. `Application.isBatchMode`가 `false`이면 `[OfflineResumeWatcher]`는 그대로. (새로) `[Game]`의 자식 `[BackKeyWatcher]`를 만들어 `Initialize(this, Settings)` (배치 모드에서도 만든다).
- 캔버스 위 순서: 7번의 게임 설정 버튼과 닫힌 설정 창이 먼저, 8번의 시작 화면이 그 위다. 손상 창·보상 창은 시작 흐름에서 만들어지므로 그 위다. 설정 창은 열 때마다 맨 위로 올라간다.

### 필요한 것

- `PlumSword.Core.Save` (`docs/spec/save.md`): `AutoSave`(생성자의 `enabled`, `Enable()`, `SaveNow(long)`), `SaveLoadStatus`, `SaveLoadResult.Status`, `GameRoot.LoadResult`, `GameRoot.AutoSave`, `SaveErrorView.Initialize(Canvas, Action)`, `SaveView`(`OnApplicationPause(true)`·`OnApplicationQuit`의 `SaveNow`). 이미 구현되어 있다.
- `PlumSword.Core.OfflineReward` (`docs/spec/offline_reward.md`): `OfflineRewardCalculator.Calculate`, `OfflineRewardResult.HasReward`, `OfflineRewardClaim`, `OfflineRewardView.Initialize`, `OfflineResumeWatcher`, `GameRoot.OfflineReward`, `OfflineRewardClaim`, `IsResumeRestart`, `Restart()`, `GameRestarter`. 이미 구현되어 있다.
- `PlumSword.Core.Stage.StageProgress` (`docs/spec/stage.md`): `Begin()`, `HasBegun`.
- `PlumSword.Core.Combat.Battle.SetPaused(bool)` (`docs/spec/combat.md`).
- `PlumSword.Game.GameBootstrap.RootName`, `GameRoot.UiCanvas`, `Wallet`, `StatUpgrades`, `Battle`, `StageProgress`.
- Input System의 `UnityEngine.InputSystem.Keyboard` (`PlumSword.Game`이 이미 참조한다).

## 화면

### 설정 UI 그림 적용 (2026-11-18)

설정 창과 게임 중 설정 버튼에 [설정 UI 에셋](../dev/assets.md)을 적용한다. 창은 350×233.33이며 게임 중 설정 버튼은 112×32다. 게임 중 설정 버튼은 SPD 강화 버튼 위에 두며 캔버스 왼쪽 아래 기준 오른쪽 아래 피벗 위치는 (632, 60)이다. 독립된 한글 라벨·버튼·슬라이더 바탕과 손잡이를 사용한다. 볼륨 숫자와 채움은 현재 값에 따라 바뀌며 최대 음량에서 바 안쪽을 채운다. 설정 창 그림이 없으면 아래의 기존 영문·단색 대체 UI를 사용한다. 시작 화면과 게임 중의 버튼 표시 조건, 저장 및 조작 동작은 아래 규칙과 같다. 아래 설정 창과 게임 설정 버튼의 크기·색·영문 표시는 그림이 없을 때의 대체값이다.

게임 중 설정 버튼과 창 안의 세 버튼은 포인터를 올리면 그림이 부드럽게 커지고 따뜻한 빛을 띠며, 누르면 잠깐 작아진다. 입력 영역은 움직이지 않는다. 두 슬라이더는 손잡이에 같은 반응을 주고 값이 바뀌는 동안 짧게 밝아진다. 창이 닫히면 효과를 초기화한다.

- **조립**: 위 "`GameRoot.Awake` 순서"의 7~9번. 씬은 하나 그대로이고 씬·프리팹·설정 파일은 고치지 않는다.
- **시작 화면** (`StartScreenView`): 캔버스의 **마지막 자식** "StartScreenPanel"로 만든다 (`SetAsLastSibling`).
  - 바탕: 캔버스를 꽉 채우는(앵커 (0, 0)~(1, 1)) 불투명 `Image`, RGB (0.10, 0.07, 0.12) (임시). `raycastTarget = true`라 뒤의 버튼이 눌리지 않는다.
  - **그림이 있을 때** (2026-11-17 적용. `StartScreenArt.Load()`가 그림 네 장을 모두 찾음. 그림은 [에셋 목록](../dev/assets.md) "시작 화면"):
    - 배경 "Background": 바탕의 자식. 제목 `매화검 키우기`가 그려진 `title_background`(1672×941) `Image`, `raycastTarget = false`. `AspectRatioFitter`(`EnvelopeParent`, 비율 = 그림 폭 ÷ 높이)로 비율을 지키며 화면을 빈틈없이 덮고, 넘치는 쪽은 가운데 기준으로 잘린다. 16:9(640×360)는 꼭 맞고, 더 긴 가로 화면(780·800·840)은 위아래가, 4:3(480)은 양옆이 조금 잘린다. 제목 글자와 두 버튼은 이 모든 비율에서 화면 안에 남고 버튼은 제목·제자와 겹치지 않는다 (플레이 모드 테스트 `Initialize_WithArt_KeepsTitleAndButtonsVisible`). 21:9에서는 제목 위 매화 가지 끝과 제자 발끝이 조금 잘린다.
    - 꽃잎 "Petals": 배경 바로 다음 자식(배경 위, 버튼 아래). 바탕을 꽉 채우는 영역(앵커 (0, 0)~(1, 1))에 `StartScreenPetals`를 붙여 `Initialize(art.Petals, PetalField.DefaultCount, Environment.TickCount)`. 자식 "Petal" `Image` 50개, `raycastTarget = false`라 버튼 누르기를 막지 않는다. 꽃잎 하나는 그림 비율대로 긴 변을 `Size`로 맞추고, 위치 (`X`, `Y`), z 회전 `Rotation`, 가로 배율 `FlipScale`(회전 전 꽃잎 자신의 가로), 색 흰색에 알파 `Alpha`. `IsWaiting`이면 `Image.enabled = false`로 숨긴다. 그림 종류는 `Variant`(0 펼쳐진, 1 휘어진, 2 접힌). `art.Petals`가 비었으면 만들지 않는다.
    - 제목 글자 "Title"은 **만들지 않는다** (배경 그림에 이미 있다).
    - 시작 버튼 "StartButton": 위치 (44, −28), 크기 230×42. 그림 `StartScreenArt.StartButtonSprite(button)`: `Continue`면 `button_continue`(이어하기), 나머지는 `button_start`(게임 시작). 설정 버튼 "SettingsButton": 위치 (44, −80), 크기 230×42, 그림 `button_settings`(설정). 두 버튼 모두 버튼 자신은 투명한 눌리는 영역(`Image` 색 (1, 1, 1, 0), `raycastTarget = true`, 그림 없음)이고, 그림은 같은 크기의 자식 "Art" `Image`(`preserveAspect = true`, `raycastTarget = false`, 색 = 평소 밝기 (0.88, 0.88, 0.88, 1))가 그린다. 버튼에 `StartButtonHover`를 붙여 "Art"만 키우고 밝히고 띄운다("동작"의 "버튼 반응"). 글자 오브젝트 없음(글자가 그림에 있다). x = 44는 배경 그림의 제목 가운데에 맞춘 값이고, 버튼은 제목 아래·제자 오른쪽의 빈 곳에 있다.
  - **그림이 없을 때** (대체 표시. 아래 세 줄은 그림 적용 전과 같다):
  - 제목 "Title" `StartMenuText.Title`: 가운데 기준 위치 (0, 70), 크기 600×48, 글자 크기 36 (임시), 매화색 RGB (1.0, 0.78, 0.86) (임시).
  - 시작 버튼 "StartButton": 위치 (0, −20), 크기 200×40 (임시), 색 RGB (0.20, 0.45, 0.25) (강화 버튼의 누를 수 있는 색과 같다). 글자 `StartMenuText.StartButtonLabel(button)`, 글자 크기 18 (임시).
  - 설정 버튼 "SettingsButton": 위치 (0, −72), 크기 200×32 (임시), 색 RGB (0.25, 0.25, 0.30) (임시). 글자 `StartMenuText.SettingsButton`, 글자 크기 14 (임시).
  - 위치는 모두 캔버스 가운데(앵커·피벗 (0.5, 0.5)) 기준이다.
- **게임 설정 버튼** (`SettingsView`가 만든다): 캔버스의 자식 "GameSettingsButton". SPD 버튼 위에 있고 앵커 (0, 0), 피벗 (1, 0), 위치 (632, 60)이다. 그림이 있으면 크기 112×32이며 없으면 72×20 단색·영문 대체 표시를 쓴다. 시작 화면이 떠 있는 동안에는 시작 화면에 덮여 보이지 않는다.
- **설정 창** (`SettingsView`): 캔버스의 자식 "SettingsPanel". 처음에는 꺼져 있다 (`SetActive(false)`).
  - 가림막: "SettingsPanel" 자체. 캔버스를 꽉 채우는 검은색 알파 0.5 `Image` (임시), `raycastTarget = true`. 뒤의 전투는 보이고 계속 움직이지만 강화 버튼과 시작 버튼은 눌리지 않는다.
  - 창 "Window": 가운데, 크기 360×230 (임시), 바탕 RGB (0.12, 0.12, 0.12) (임시).
  - 제목 "Title" `StartMenuText.SettingsTitle`: 위치 (0, 92), 크기 340×24, 글자 크기 18 (임시).
  - 배경음악 줄: 글자 "BgmLabel" `BgmLine(volume.Bgm)` 위치 (−120, 56), 크기 100×18, 글자 크기 14 (임시), 왼쪽 정렬. 슬라이더 "BgmSlider" 위치 (50, 56), 크기 200×16 (임시).
  - 효과음 줄: "SfxLabel" `SfxLine(volume.Sfx)` 위치 (−120, 24), "SfxSlider" 위치 (50, 24). 크기와 글자는 배경음악 줄과 같다.
  - 슬라이더: uGUI `Slider`를 코드로 만든다. `minValue` 0, `maxValue` 100, `wholeNumbers = true`, 왼쪽에서 오른쪽. 바탕 RGB (0.30, 0.30, 0.30), 채움 RGB (0.20, 0.45, 0.25), 손잡이 흰색 12×20 (모두 임시). 값이 바뀌면 `SetBgm((int)value)` / `SetSfx((int)value)` 후 옆 글자를 맞춘다.
  - `TO TITLE` "ToTitleButton": 위치 (−85, −18), 크기 150×32 (임시), 색 RGB (0.25, 0.25, 0.30). `QUIT GAME` "QuitButton": 위치 (85, −18), 크기 150×32 (임시), 색 RGB (0.45, 0.20, 0.20) (임시). 글자 크기 14 (임시). `InGame`일 때만 보인다.
  - 실패 문구 "LeaveFailedMessage" `StartMenuText.LeaveFailedMessage`: 위치 (0, −48), 크기 340×16, 글자 크기 12 (임시), 빨강 RGB (1.0, 0.35, 0.35) (임시). `InGame`이고 `leaveGame.HasFailed`일 때만 보인다 (처음에는 숨김).
  - `CLOSE` "CloseButton": 위치 (0, −86), 크기 120×28 (임시), 색 RGB (0.25, 0.25, 0.30), 글자 크기 14 (임시).
  - 창 안의 위치는 창 가운데 기준이다.
- **공통**: 버튼은 `Image` + `Button`, `Button.transition = None`, 글자는 흰색 가운데 정렬(따로 적은 것 말고), `raycastTarget = false`. 글자는 모두 `TextMeshProUGUI`, TMP 기본 글꼴.
- **연출**: 시작 화면의 매화 꽃잎 흩날림과 시작 화면·게임 중 설정 버튼 및 설정 창 조작부의 호버·누름 반응을 쓴다. 시작 화면과 게임 사이의 페이드와 버튼 소리는 없다.
- **사람이 먼저 해 둘 것**: 금화 사양과 같다 (TMP Essentials). 없어도 예외로 게임이 멈추지 않아야 한다.
- **사람이 확인하는 법** (에디터 플레이):
  1. `save.dat`이 없는 채로 플레이 → 시작 화면에 `게임 시작` 그림 버튼(그림이 없으면 `START`) → 누르면 1스테이지. 바로 멈췄다가 다시 플레이하면 `이어하기`(`CONTINUE`). 아래 단계의 `START`·`CONTINUE`·`SETTINGS`도 시작 화면에서는 그림의 `게임 시작`·`이어하기`·`설정`이다. 게임 창 비율을 16:9와 더 긴 가로(예: 2400×1080)로 바꿔 제목과 버튼이 잘리지 않는지 본다.
  2. 시작 화면에서 `SETTINGS` → 슬라이더를 30·60으로 → `CLOSE` → 플레이를 멈췄다 다시 켜고 열면 30·60.
  3. 게임 중 `SETTINGS`(또는 Esc) → 창이 열린 동안 전투가 계속되는지. Esc로 닫히는지.
  4. `TO TITLE` → 시작 화면에 `CONTINUE` → 누르면 같은 스테이지 처음부터, 보상 창 없음.
  5. `QUIT GAME` → 플레이 모드가 멈춤 → 다시 켜면 `CONTINUE`.
  6. 저장 실패는 `save.dat`을 읽기 전용으로 바꾸고 `TO TITLE`·`QUIT GAME`을 눌러 실패 문구와 머묾을 확인한다.
  7. 손상은 `save.dat`을 아무 글자로 바꿔 `START` → 손상 창을 확인한다. 보상은 오프라인 보상 사양의 확인법대로 `savedAtUtcTicks`를 바꿔 `CONTINUE` → 보상 창을 확인한다.
  8. 백그라운드 복귀와 안드로이드 뒤로 가기 키는 폰 빌드에서 확인한다.
- 표시 글자와 무엇을 보일지(`StartMenuRules`, `StartButtonLabel`)의 판단은 Core에 있고 에딧 모드 테스트로 판정한다. `StartScreenView`와 `SettingsView`의 구조·입력은 플레이 모드 테스트로 확인하고, 실제 배치와 흐름은 사람이 실행해 확인한다.

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| `PlumSword.BgmVolume` (PlayerPrefs) | `int` | 80 (임시) | 0~100. 범위 밖은 읽을 때 잘라냄 |
| `PlumSword.SfxVolume` (PlayerPrefs) | `int` | 80 (임시) | 0~100. 범위 밖은 읽을 때 잘라냄 |

- 진행 저장 `save.dat`에는 아무것도 더하지 않는다 (판 1 그대로). 음량은 `NEW GAME`이나 손상 처리와 상관없이 남는다.
- `Entry`, `IsStartScreenShowing`, 설정 창이 열렸는지, `LeaveGame.HasFailed`는 저장하지 않는다.
- 배치 모드에서는 PlayerPrefs를 읽지도 쓰지도 않는다.
- 오프라인 보상: 음량은 상관없다. 꺼져 있는 동안 바뀌지 않는다. 시작 화면에 머문 시간의 처리는 "동작"의 "시작 화면에 머문 시간"이다.

## 범위 밖

- 실제 소리 에셋과 재생, `AudioSource`·`AudioListener`에 음량 적용: 하지 않는다. 소리를 넣는 기능(4단계 에셋 적용 등)이 `GameRoot.Volume`을 쓴다.
- 한글 글꼴(TMP 글꼴 에셋)은 아직 없다. 시작 화면과 설정 창·게임 화면 설정 버튼의 한글은 그림에 들어 있고, 그림이 없을 때의 대체 표시는 영문이다.
- 다른 설정 항목(언어, 진동, 화질, 음소거 단추 등), 계정·클라우드: 하지 않는다.
- 저장이 있는데 처음부터 새로 시작하는 버튼(저장 지우기), 시작 화면의 종료 버튼, 떠나기 확인 창("정말 나갈까요?"), 저장 실패 때 `그냥 종료`: 하지 않는다.
- 시작 화면에서 뒤로 가기 키로 앱 끄기: 하지 않는다.
- 시작 화면과 게임 사이의 페이드·연출, 키보드·게임패드로 버튼 고르기 표시는 아직 없다. 시작 화면과 게임 중 설정 버튼·설정 창 조작부에는 호버·누름 반응을 적용했다 ("화면").
- 씬 나누기: 하지 않는다 (한 씬에서 코드로 조립).
- 같은 실행 안에서 시작 화면으로 돌아갔다가 `CONTINUE`할 때의 오프라인 보상: 하지 않는다 (오프라인 보상 사양).
- PC 창 포커스 잃음(`OnApplicationFocus`) 처리: 하지 않는다 (오프라인 보상 사양과 같다).
- `GameRoot.ReturnToTitle()`·백그라운드 복귀·앱 종료의 실제 흐름 자동 테스트: 하지 않는다 (사람이 확인).

## 완료 조건 초안

테스트에서 "가짜 저장소"와 "스테이지 객체"는 `docs/spec/save.md` 완료 조건 초안의 것과 같다. 가짜 저장소는 쓴 횟수와 마지막 글자를 세고, 실패하게 바꿀 수 있다. 스테이지 객체는 새 `Battle(upgrades)`와 `new System.Random(0)`, 시작 능력치 `(10, 100, 1.0)`으로 만든다. "떠나기 준비"는 다음과 같다: `new Wallet(0)`, 그 지갑의 `StatUpgrades`, 스테이지 객체(`startStage: 1`, `seenIntroStage: 1`), 가짜 저장소로 켜진(`enabled: true`) `AutoSave`를 만들고 `new LeaveGame(autoSave)`를 만든다. 플레이 모드의 화면 단위 테스트는 테스트가 따로 만든 캔버스(`Canvas` + `GraphicRaycaster`)를 쓴다.

1. `StartMenuRules.StartButton(Loaded)`는 `Continue`이고, `StartButton(Empty)`, `StartButton(UnsupportedVersion)`, `StartButton(Corrupt)`, `StartButton((SaveLoadStatus)99)`는 모두 `NewGame`이다.
2. `ShowsStartScreen`은 `(Launch, false)` → `true`, `(Launch, true)` → `false`, `(ResumeInGame, false)`·`(ResumeInGame, true)` → `false`, `(ResumeOnTitle, false)`·`(ResumeOnTitle, true)` → `true`, `(TitleReturn, false)`·`(TitleReturn, true)` → `true`, `((StartEntry)99, false)` → `true`이다.
3. `CalculatesOfflineReward`는 `Launch`, `ResumeInGame`, `ResumeOnTitle`, `(StartEntry)99`에서 `true`, `TitleReturn`에서 `false`이다.
4. `AutoSaveStartsEnabled(Loaded, false)`는 `true`이다. `(Loaded, true)`, `(Empty, false)`, `(Empty, true)`, `(UnsupportedVersion, false)`, `(Corrupt, false)`, `((SaveLoadStatus)99, false)`는 모두 `false`이다.
5. `ResumeEntry(true)`는 `ResumeOnTitle`, `ResumeEntry(false)`는 `ResumeInGame`이다.
6. `BackKey`는 `isSettingsOpen`이 `true`이면 나머지 두 값의 네 조합 모두 `CloseSettings`이다. `(false, false, true)`는 `OpenSettings`, `(false, false, false)`, `(false, true, false)`, `(false, true, true)`는 모두 `None`이다.
7. `VolumeSettings.MinVolume`은 0, `MaxVolume`은 100, `DefaultVolume`은 80이다. `new VolumeSettings()`는 `Bgm` 80, `Sfx` 80, `BgmScale` 0.8, `SfxScale` 0.8이다 (오차 1e-9).
8. `new VolumeSettings(-5, 150)`은 `Bgm` 0, `Sfx` 100이다. `new VolumeSettings(int.MinValue, int.MaxValue)`는 0, 100이다. `new VolumeSettings(30, 55)`는 30, 55이고 `BgmScale` 0.3, `SfxScale` 0.55이다 (오차 1e-9). 모두 예외가 없다.
9. `new VolumeSettings()`에 `Changed`를 기록하고 차례로: `SetBgm(30)` → `Bgm` 30, `Sfx` 80, 알림 1번. `SetBgm(30)` → 알림 1번 그대로. `SetSfx(150)` → `Sfx` 100, 알림 2번. `SetSfx(int.MaxValue)` → `Sfx` 100, 알림 2번 그대로. `SetBgm(-1)` → `Bgm` 0, 알림 3번. `SetBgm(int.MinValue)` → `Bgm` 0, 알림 3번 그대로.
10. `new LeaveGame(null)`은 `ArgumentNullException`이다. 떠나기 준비 직후 `HasFailed`는 `false`, 쓴 횟수 0이다.
11. 떠나기 준비 뒤 `wallet.Add(10)` → `TryLeave(100)`은 `true`이고, `HasFailed` `false`, 쓴 횟수 1, 마지막 글자를 `Decode`하면 `Gold` 10, `SavedAtUtcTicks` 100이다.
12. 떠나기 준비에서 가짜 저장소를 실패하게 하고 `TryLeave(100)`을 하면 `false`, `HasFailed` `true`, 시도 1, `autoSave.HasFailed` `true`이다. 이어서 `TryLeave(200)`도 `false`, 시도 2이다. 저장소를 성공하게 바꾸고 `TryLeave(300)`을 하면 `true`, `HasFailed` `false`, 시도 3, 마지막 글자의 `SavedAtUtcTicks` 300이다.
13. 떠나기 준비를 `AutoSave`를 `enabled: false`로 바꿔 만들고 `TryLeave(100)`을 하면 `false`, `HasFailed` `true`, 쓴 횟수 0이다.
14. `StartMenuText`의 `Title`, `NewGameButton`, `ContinueButton`, `SettingsButton`, `SettingsTitle`, `ToTitleButton`, `QuitButton`, `CloseButton`, `LeaveFailedMessage`는 각각 `"PLUM BLOSSOM SWORD"`, `"START"`, `"CONTINUE"`, `"SETTINGS"`, `"SETTINGS"`, `"TO TITLE"`, `"QUIT GAME"`, `"CLOSE"`, `"SAVE FAILED. TRY AGAIN."`이다.
15. `StartButtonLabel(Continue)`는 `"CONTINUE"`, `StartButtonLabel(NewGame)`은 `"START"`, `StartButtonLabel((StartButtonKind)99)`는 `"START"`이다. `BgmLine(80)`은 `"BGM 80"`, `BgmLine(100)`은 `"BGM 100"`, `SfxLine(0)`은 `"SFX 0"`, `SfxLine(-5)`는 `"SFX -5"`이다.
16. (플레이 모드) 배치 모드에서 게임이 켜지면 `GameRoot.Entry`는 `Launch`, `IsResumeRestart` `false`, `IsStartScreenShowing` `false`이고, `[Game]` 아래에 `[StartScreenView]`가 없고 캔버스 아래에 "StartScreenPanel"이 없다. `[Game]` 아래 `[SettingsView]`에 `SettingsView`가 붙어 있고 `GameRoot.Settings`가 그것이며 `IsOpen`은 `false`이다. 캔버스 아래에 "GameSettingsButton"이 있다. `GameRoot.Volume`은 `Bgm` 80, `Sfx` 80, `GameRoot.LeaveGame`은 `null`이 아니고 `HasFailed` `false`, `StageProgress.HasBegun` `true`, `AutoSave.Enabled` `true`, `[Game]` 아래에 `[BackKeyWatcher]`가 있다. 이 테스트는 루프와 명령줄의 배치 모드에서 판정하며, 건너뛰는 코드(`Assert.Ignore`, `Assume`, 조건부 조기 종료 등)는 넣지 않는다 (오프라인 보상 사양의 플레이 모드 테스트와 같다).
17. (플레이 모드) 배치 모드에서 켠 뒤 `Battle.Paused`를 읽어 두고 `GameRoot.Settings.Open(SettingsMode.InGame)`을 하면 `IsOpen` `true`, `Mode` `InGame`, "SettingsPanel"이 켜져 있고 캔버스의 마지막 자식이며 `Battle.Paused`는 읽어 둔 값 그대로다. `Close()` 뒤 `IsOpen` `false`, "SettingsPanel"은 꺼져 있다. 건너뛰는 코드는 넣지 않는다.
18. (플레이 모드) `StartScreenView.Initialize`에 `canvas`, `onStart`, `onOpenSettings`를 하나씩 `null`로 넣으면 각각 `ArgumentNullException`이다. `Initialize(canvas, Continue, …)` 뒤 "StartScreenPanel"은 캔버스의 마지막 자식이고, 앵커가 (0, 0)~(1, 1)이며, 그 `Image`의 `raycastTarget`은 `true`이다. 그 아래 글자는 `"PLUM BLOSSOM SWORD"`, `"CONTINUE"`, `"SETTINGS"`를 담는다. 새 캔버스에서 `Initialize(canvas, NewGame, …)`의 시작 버튼 글자는 `"START"`이다.
19. (플레이 모드) 18번처럼 만든 뒤 "SettingsButton"의 `onClick`을 두 번 부르면 `onOpenSettings`가 2번 불린다. "StartButton"의 `onClick`을 같은 프레임에 두 번 부르면 `onStart`가 1번 불리고, 한 프레임 뒤 "StartScreenPanel"과 `StartScreenView`의 오브젝트가 없다.
20. (플레이 모드) `SettingsView.Initialize`에 `canvas`, `volume`, `leaveGame`, `onReturnToTitle`, `onQuit`, `saveVolume`을 하나씩 `null`로 넣으면 각각 `ArgumentNullException`이다. 떠나기 준비의 `LeaveGame`과 `new VolumeSettings()`로 `Initialize`한 뒤 `IsOpen`은 `false`, "SettingsPanel"은 있고 꺼져 있으며, "GameSettingsButton"은 켜져 있다. "GameSettingsButton"의 `onClick`을 부르면 `IsOpen` `true`, `Mode` `InGame`이다.
21. (플레이 모드) 20번처럼 만든 뒤 `Open(Title)`을 하면 "SettingsPanel"이 켜져 있고 캔버스의 마지막 자식이며, "ToTitleButton", "QuitButton", "LeaveFailedMessage"는 꺼져 있고 "BgmLabel"의 글자는 `"BGM 80"`이다. 이어서 `Open(InGame)`을 하면 `IsOpen` `true` 그대로이고 "ToTitleButton", "QuitButton"이 켜지며 "LeaveFailedMessage"는 꺼져 있다.
22. (플레이 모드) 20번처럼 만들어 `Open(InGame)`한 뒤 "BgmSlider"의 `value`를 30으로 하면 `volume.Bgm` 30, "BgmLabel" `"BGM 30"`이다. "SfxSlider"의 `value`를 0으로 하면 `volume.Sfx` 0, "SfxLabel" `"SFX 0"`이다. "CloseButton"의 `onClick`을 부르면 `IsOpen` `false`, "SettingsPanel"은 꺼지고 `saveVolume`은 1번 불린다. 그 뒤 `Close()`를 한 번 더 불러도 `saveVolume`은 1번 그대로다.
23. (플레이 모드) 20번처럼 만들되 가짜 저장소를 실패하게 하고 `Open(InGame)` 뒤 "ToTitleButton"의 `onClick`을 부르면 `onReturnToTitle` 0번, `saveVolume` 0번, `IsOpen` `true`, "LeaveFailedMessage"가 켜져 있다. "QuitButton"의 `onClick`을 불러도 `onQuit` 0번이다. `Close()` 뒤 `Open(InGame)`을 하면 "LeaveFailedMessage"는 켜져 있다. 저장소를 성공하게 바꾸고 "ToTitleButton"의 `onClick`을 부르면 `onReturnToTitle` 1번, `saveVolume`은 2번(앞의 `Close` 1번 + 떠나기 1번)이다. 따로 새로 만든 성공하는 저장소의 창에서 `Open(InGame)` 뒤 "QuitButton"의 `onClick`을 부르면 `onQuit` 1번, `saveVolume` 1번, `onReturnToTitle` 0번이다.

## 결정 기록

결정 기록은 [records/start_menu.md](records/start_menu.md)의 "결정 기록"에 있다. 구현에 필요한 규칙은 위 절들에 모두 있다.
