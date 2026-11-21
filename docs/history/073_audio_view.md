# 073_audio_view — 소리 재생 화면 연결

## 목표
Game(`PlumSword.Game.Audio`)에 소리 파일을 읽는 `AudioClipCache`와, Core 소리 객체를 만들어 매 프레임 판단을 넘기고 결과대로 `AudioSource`를 재생하는 `AudioView`를 만들고, `GameRoot`에 `Audio` 속성과 조립 한 줄을 더한다.

## 묶음
audio

## 선행 작업
- 068_audio_catalog_rules
- 069_music_fader
- 070_sfx_queue_voice_pool
- 071_sfx_triggers
- 072_ui_sound_watch

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다", "테스트" 절: 플레이 모드 테스트는 빈 씬에서 시작하고 첫 씬을 불러오며, 테스트 중 오류 로그가 나면 실패한다)
- docs/spec/audio.md (전체. 특히 "동작"의 "판단과 재생을 나눈다"·"배경음악"·"효과음"(버튼 판정, 창 판정, 동시 재생)·"음량"·"일시정지와 포커스"·"파일이 없을 때", "연결 지점"의 `PlumSword.Game.Audio`·`GameRoot`, "완료 조건 초안" 42~46)
- docs/spec/start_menu.md ("연결 지점" 절: `VolumeSettings`, `SettingsView.IsOpen`, `GameRoot` 시작 순서. 배치 모드에서는 시작 화면 없이 바로 시작하고 음량은 기본값 80)
- docs/spec/stage.md ("연결 지점" 절: `StageProgress.DisplayStageNumber`, `Boss`)
- docs/spec/offline_reward.md ("연결 지점" 절: `OfflineRewardClaim.HasPaid`)
- docs/dev/audio.md

## 요구사항
- 폴더 `Assets/Scripts/Game/Audio/`, 네임스페이스 `PlumSword.Game.Audio`. 파일 `AudioClipCache.cs`, `AudioView.cs`. 판단은 모두 앞 작업의 Core 클래스(`PlumSword.Core.Audio`의 `MusicRules`, `MusicFader`, `AudioMix`, `SfxQueue`, `SfxTriggers`, `UiSoundWatch`, `SfxVoicePool`, `AudioCatalog`)를 호출하고, 이 화면 코드는 `AudioSource` 재생, `Resources.Load`, 화면 입력 읽기, 포커스 처리만 한다.
- 이 작업이 정하는 연결 지점은 사양 "연결 지점" 절 그대로다. 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/audio.md`)의 연결 지점 절을 고치고 보고서의 가정에 적는다.
- `public sealed class AudioClipCache`
  - `public UnityEngine.AudioClip Load(string path)`: `path`가 `null`이면 `null` (로그 없음). 경로마다 `Resources.Load<AudioClip>(path)`를 한 번만 부르고 결과(`null` 포함)를 기억한다. 결과가 `null`이면 그 경로로 처음 한 번만 `Debug.LogWarning("[Audio] 소리 파일을 찾지 못했습니다: " + path)`를 남긴다. 다시 부르면 기억한 값을 돌려주고 로그를 남기지 않는다. `Debug.LogError`는 쓰지 않는다.
- `public sealed class AudioView : MonoBehaviour`
  - `public void Initialize(PlumSword.Game.GameRoot root)`: `root`가 `null`이면 `ArgumentNullException`. 두 번 부르면 `InvalidOperationException`. 이 오브젝트에 곡 재생기 `AudioSource` 2개(`loop = true`, `playOnAwake = false`, `spatialBlend = 0`)와 효과음 재생기 `AudioCatalog.MaxSfxVoices`(8)개(`loop = false`, `playOnAwake = false`, `spatialBlend = 0`)를 만든다. `new SfxQueue()`, `new SfxTriggers(queue, root.Battle, root.StageProgress, root.StatUpgrades, root.Realm)`, `new UiSoundWatch(queue)`, `new MusicFader()`, `new SfxVoicePool()`, `new AudioClipCache()`를 만든다. `root.Volume.Changed`를 구독한다. `AudioListener.pause = false`. 일시정지 상태는 처음 `paused = false`, `focused = true`.
  - `public PlumSword.Core.Audio.MusicTrack CurrentTrack { get; }` = `MusicFader.Current`.
  - `public UnityEngine.AudioSource CurrentMusicSource { get; }` = `Current`를 트는 곡 재생기.
  - `public System.Collections.Generic.IReadOnlyList<UnityEngine.AudioSource> SfxSources { get; }` 번호 = `SfxVoicePool` 번호.
  - `LateUpdate` (초기화 전이면 아무것도 하지 않음):
    1. 곡: `MusicRules.TrackFor(root.IsStartScreenShowing, root.StageProgress.DisplayStageNumber, root.StageProgress.Boss != null)` → `MusicFader.Request`. `true`면 버려지는 곡(그전 `Previous`)의 재생기를 `Stop`하고, 쓰지 않던 재생기에 새 곡 clip(`AudioClipCache.Load(AudioCatalog.MusicPath(곡))`)을 넣어 처음부터 `Play` (clip 이 `null`이면 재생하지 않고 소리 없이 둔다. `MusicFader`는 그대로 진행). → `MusicFader.Tick(Time.unscaledDeltaTime)` → 두 재생기 음량을 `(float)AudioMix.MusicVolume(그 곡, root.Volume.BgmScale, 그 곡의 비율)`로. `Previous`가 `None`이 된 재생기는 `Stop`.
    2. 버튼: `UnityEngine.InputSystem.Pointer.current`의 `press.wasPressedThisFrame`이면 그 위치로 `EventSystem.current.RaycastAll`을 해서 맨 앞 결과의 오브젝트에서 `GetComponentInParent<UnityEngine.UI.Button>()`을 찾는다. 있으면 `UiSoundWatch.PressStarted(true, button.IsInteractable())`, 결과가 없거나 버튼이 아니면 `PressStarted(false, false)`. `press.wasReleasedThisFrame`이면 `PressEnded()`. `Pointer.current` 또는 `EventSystem.current`가 `null`이면 이 단계는 아무것도 하지 않는다.
    3. 창: `UiSoundWatch.UpdateWindows(root.Settings.IsOpen, [Game] 아래에 OfflineRewardView 가 있는지, [Game] 아래에 SaveErrorView 가 있는지, root.OfflineRewardClaim != null && root.OfflineRewardClaim.HasPaid)`. 창 유무는 `root.GetComponentInChildren<PlumSword.Game.OfflineReward.OfflineRewardView>(true) != null`, `root.GetComponentInChildren<PlumSword.Game.Save.SaveErrorView>(true) != null`처럼 본다 (정확한 방식은 기존 코드에 맞춘다).
    4. 효과음: `now = Time.unscaledTimeAsDouble`, `SfxQueue.Drain(now)`. `AudioMix.ShouldPlaySfx(root.Volume.SfxScale)`가 `false`면 재생하지 않는다 (Drain 은 이미 했으므로 간격 기록은 남는다). `true`면 효과음마다 `AudioClipCache.Load(AudioCatalog.SfxPath(id))` → `null`이면 건너뜀(재생기를 차지하지 않음) → `SfxVoicePool.Acquire(now, clip.length)` → 그 재생기 `Stop`, `clip` 넣기, `volume = (float)AudioMix.SfxVolume(id, root.Volume.SfxScale)`, `Play`. 각 효과음 재생기가 지금 어떤 `SfxId`를 트는지 기억한다.
  - `Volume.Changed`를 받으면 울리고 있는(`isPlaying`) 효과음 재생기의 음량을 그 효과음의 `AudioMix.SfxVolume(id, SfxScale)`로 바로 고친다. (곡 음량은 매 프레임 계산하므로 따로 하지 않는다)
  - `OnApplicationPause(bool pause)`, `OnApplicationFocus(bool focus)`: 상태를 적고 `AudioListener.pause = AudioMix.ShouldPauseAll(paused, focused)`.
  - `OnDestroy`: `SfxTriggers.Dispose()`, `Volume.Changed` 구독 해제.
- `GameRoot` (`Assets/Scripts/Game/GameRoot.cs`, 이미 있는 클래스): `public PlumSword.Game.Audio.AudioView Audio { get; private set; }` 속성 하나를 더하고, `Awake`에서 설정 창 `Settings.Initialize(...)` 바로 다음, 시작 화면을 띄우는 `if (StartMenuRules.ShowsStartScreen(...))` 앞에 `[Game]`의 자식 `[AudioView]` 오브젝트를 만들어 `AudioView`를 붙이고 `Initialize(this)` 한다 (기존 `[SettingsView]` 만드는 줄과 같은 모양). 이것 말고 `GameRoot`의 다른 줄은 고치지 않는다.
- 씬 카메라에 `AudioListener`가 이미 있다. 새로 만들지 않는다.
- 플레이 모드 테스트: `Assets/Tests/PlayMode/Audio/AudioViewTests.cs` (네임스페이스 `PlumSword.Game.Tests.Audio`).
  - **배치 모드에서 돈다.** 테스트 러너는 화면 없는 배치 모드라 `GameRoot`가 시작 화면 없이(`IsStartScreenShowing == false`) 저장 없이 새 판 1스테이지로 바로 시작하고, 음량은 기본값(`Bgm` 80, `Sfx` 80)이다. 그래서 첫 곡은 `BattleNormal`이다.
  - **오류 로그가 나면 실패한다.** 테스트 중 `Debug.LogError`나 처리되지 않은 예외가 나면 그 테스트는 실패한다 (`docs/dev/architecture.md` "테스트" 절). 파일이 없을 때도 경고(`LogWarning`)만 남겨야 한다.
  - 시작은 `GameBootTests`처럼 `yield return SceneManager.LoadSceneAsync(0);` 뒤 몇 프레임 기다리고, 루트는 `GameObject.Find(GameBootstrap.RootName)`의 `GameRoot`로 찾는다. `[Game]` 루트는 테스트 사이에 남아 있을 수 있으므로 매 테스트에서 다시 찾는다.
  - 완료 조건 43에서 넣은 보스는 공유된 전투에 남으므로, 테스트 끝에 보스가 다른 테스트에 영향을 주지 않게 정리한다 (예: 보스를 제자에서 멀리(+10) 둔다. 필요하면 `[TearDown]`에서 기존 플레이 모드 테스트가 쓰는 방식으로 루트를 다시 만든다). 다른 플레이 모드 테스트가 깨지면 안 된다.
  - 시간 기다리기는 실제 시간(`WaitForSecondsRealtime`)으로 한다. 음량 비교는 오차 1e-3.
  - `LogAssert.Expect(LogType.Warning, "[Audio] 소리 파일을 찾지 못했습니다: Audio/SFX/__missing__")`로 경고 한 번을 확인하고, 그 뒤 `LogAssert.NoUnexpectedReceived()`로 더 없음을 확인한다.
  - 배치 모드에서 오디오 출력 장치가 없어 `AudioSource`의 재생 상태를 믿을 수 없을 수 있다. 그래서 완료 조건은 `clip`, `loop`, `volume`만 본다 (`isPlaying`은 검사하지 않는다). `Resources.Load<AudioClip>`가 배치 모드에서 `null`을 돌려주는 등 완료 조건을 확인할 수 없으면, 조건을 약하게 바꾸지 말고 이유를 적어 blocked 로 보고한다.
- 문서: `docs/dev/audio.md` 첫 문단의 "아직 게임 재생에 연결하지 않았고" 부분을 "소리 사양대로 `AudioView`가 재생에 연결했다(배치 모드 플레이 모드 테스트로 확인, 실제 청취는 사람이 확인)"는 뜻으로 고친다. 그 밖의 표는 고치지 않는다.

## 완료 조건
- [컴파일] `PlumSword.Game`의 새 화면 코드와 `GameRoot` 변경이 컴파일되고 기존 테스트가 모두 통과한다.
- [플레이 모드] `Assets/Tests/PlayMode/Audio/AudioViewTests.cs`에 아래 [플레이 모드] 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- [플레이 모드] (사양 42) 부팅 뒤 `[Game]`의 자식 중 `[AudioView]`라는 이름의 오브젝트가 정확히 하나 있고 `GameRoot.Audio`가 그 오브젝트의 `AudioView`다.
- [플레이 모드] (사양 42) 부팅 뒤 몇 프레임 뒤 `GameRoot.Audio.CurrentTrack`이 `BattleNormal`, `CurrentMusicSource.clip.name`이 `"battle_normal"`, `CurrentMusicSource.loop`가 `true`다.
- [플레이 모드] (사양 43) `GameRoot.Battle.AddEnemy(EnemyKind.Boss, 1, GameRoot.Battle.DisciplePosition + 10)` 뒤 다음 프레임에 `GameRoot.Audio.SfxSources` 중 하나의 `clip.name`이 `"boss_appear"`다.
- [플레이 모드] (사양 44) 부팅 뒤 실제 시간 1.5초 이상 기다린 뒤 `GameRoot.Volume.SetBgm(0)` → 다음 프레임 `CurrentMusicSource.volume`이 0(오차 1e-3)이고 `clip`은 그 전과 같은 객체다.
- [플레이 모드] (사양 44) 이어서 `SetBgm(100)` → 다음 프레임 `CurrentMusicSource.volume`이 0.6(오차 1e-3)이다.
- [플레이 모드] (사양 45) `new AudioClipCache().Load("Audio/SFX/__missing__")`를 같은 캐시로 두 번 부르면 두 번 다 `null`이고 경고 로그가 정확히 한 번이다 (`LogAssert`).
- [플레이 모드] (사양 45) `new AudioClipCache().Load(null)` → `null`이고 로그가 없다.
- [플레이 모드] (사양 46) 기존 테스트 `GameBootTests`가 오류 로그 없이 통과한다 (`[AudioView]` 조립 중 예외나 오류 로그가 없다).
- [사람 확인] 에디터에서 게임을 실행해 들어 본다: 시작 화면에서 제목곡이 1초에 걸쳐 커지고, 시작 버튼을 누르면 일반 전투곡으로 교차 페이드되며, 공격·적 처치·강화 버튼·설정 창 열고 닫기 때 효과음이 난다. 설정 창 음량 슬라이더를 0으로 내리면 곡·효과음이 각각 들리지 않는다. (화면 갈무리로는 알 수 없다)

## 하지 말 것
- 다른 기능의 화면 코드(`StatUpgradeView`, `SettingsView`, `StartScreenView`, `OfflineRewardView`, `SaveErrorView`, `CombatView`, `StageView` 등)는 고치지 않는다. 읽기만 한다.
- `GameRoot`에는 `Audio` 속성과 `[AudioView]` 조립 줄만 더한다. 다른 기능의 조립 줄과 시작 순서는 고치지 않는다.
- 다른 기능의 Core 코드(`Assets/Scripts/Core/` 아래 `Audio` 밖의 폴더)는 고치지 않는다.
- 068~072가 만든 `PlumSword.Core.Audio` 타입의 공개 모양과 값을 바꾸지 않는다. `AudioCatalog`의 임시값(배율, 켜짐, 간격, 재생기 수, 교차 페이드 시간)을 사양 표와 다르게 바꾸지 않는다. 판단 로직을 화면 코드로 옮겨 다시 쓰지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`), 오디오 파일(`Assets/Resources/Audio/` 아래의 `.wav`), 오디오 `.meta` 가져오기 설정, 프로젝트 오디오 설정(`ProjectSettings/AudioManager.asset` 등), AudioMixer 에셋은 만들거나 고치지 않는다.
- 새 어셈블리 정의를 만들거나 기존 어셈블리 정의를 고치지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/`와 기존 플레이 모드 테스트 파일은 고치지 않는다.
- `docs/dev/audio.md`는 위 요구사항의 한 문장만 고친다.

## 보고서

### 한 일
- Core 소리 판단을 AudioSource, 입력, 창 상태 및 음량에 연결하고 GameRoot에서 조립했다.
- 클립 캐시와 부팅, 보스 효과음, 음량 및 누락 파일에 대한 플레이 모드 테스트를 추가했다.
- 각 오디오 테스트에서 공유 루트를 비활성화하고 별도의 테스트 루트를 사용한 뒤 제거하여 보스와 스테이지 진행이 후속 테스트에 남지 않도록 했다. 공유 루트를 복원할 때 스테이지가 그대로인지 검사한다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs`
- `Assets/Scripts/Game/Audio.meta`, `Assets/Scripts/Game/Audio/AudioClipCache.cs`, `AudioClipCache.cs.meta`, `AudioView.cs`, `AudioView.cs.meta`
- `Assets/Tests/PlayMode/Audio.meta`, `Assets/Tests/PlayMode/Audio/AudioViewTests.cs`, `AudioViewTests.cs.meta`
- `docs/dev/audio.md`
- `tasks/doing/073_audio_view.md`, `.loop/result.json`

### 가정
- 보스는 작업 문서의 예시대로 제자보다 10만큼 멀리 배치한다. 테스트 동안 기존 공유 루트는 비활성화해 원래 카메라 참조와 전투 상태를 보존하고, 테스트 루트는 현재 씬의 카메라를 사용한다.

### 사람이 확인할 것
- 에디터에서 시작 화면 제목곡의 1초 페이드인과 시작 버튼 이후 일반 전투곡 교차 페이드, 공격·적 처치·강화 버튼·설정 창 열고 닫기 효과음을 듣는다. 설정 창의 각 음량 슬라이더를 0으로 내리면 해당 곡과 효과음이 들리지 않는지도 확인한다.

### 테스트 결과
- `git diff --check` 통과.
- 이전 루프 검사에서 EditMode 884개는 통과했고 PlayMode 148개 중 4개는 스테이지 2 상태가 남은 것으로 보이는 값 때문에 실패했다. 테스트 루트를 분리해 원인을 수정했으나 효과는 아직 확인되지 않았다.
- Unity 컴파일과 테스트는 지시에 따라 직접 실행하지 않았으며 루프의 재판정을 기다린다.


## 보류 사유 (2026-11-22 05:46)

- 사유: 교차 리뷰를 실행하지 못함: claude 의 판정 JSON 이 불완전하거나 완료 조건 근거·판정이 모순됨
- 질문: 리뷰 에이전트가 왜 실패했는지 확인해 주세요 (.loop/logs 의 *_review*_ 로그).
- 추천안: 원인을 해결한 뒤 todo 로 옮긴다. 리뷰 없이 진행하려면 loop_config.local.json 에 review_enabled: false
- 기능 브랜치: task/073_audio_view (작업 중 상태 커밋됨)
- 작업 에이전트: codex(시도 1~3)


## 답변 (2026-11-22 06:48)

- 보류 종류: 교차 리뷰 실행 실패 (2026-11-22 05:46 보류 사유에 대한 답)
- 원인: 리뷰어(claude)는 두 번 다 pass 판정을 냈고 지적은 note 3건뿐이었다. 그런데 완료 조건 10번(사양 46, `GameBootTests` 통과)을 "루프가 판정"으로 적은 것이 루프 스크립트 검사에 걸렸다. 루프는 조건 문장에 "기존 테스트"나 "전체 테스트"라는 글자가 그대로 있을 때만 "루프가 판정"을 허용하는데, 원래 문장은 "기존 `GameBootTests`가"여서 그 글자가 없었다. 구현이나 리뷰 판단에는 잘못이 없다. `GameBootTests`는 시도 3의 테스트 실행(05:43)에서 통과했다.
- 결정: 완료 조건 10번 문장에 "기존 테스트"라는 말을 넣는다. 뜻은 바뀌지 않는다. 구현은 그대로 두고 다시 검증한다.
- 이유: 이 조건은 이번 작업 전부터 있던 테스트(`GameBootTests`)가 계속 통과하는지를 묻는다. 루프가 매번 모든 테스트를 돌리므로 새 테스트 없이 루프의 테스트 결과로 판정하는 것이 맞다.
- 리뷰 지적에 대한 판단: 앞선 리뷰의 note 3건(테스트가 공유 루트를 끄고 새 GameRoot 를 만드는 것, `[AudioView]` 수를 컴포넌트 수로 확인하는 것, 보고서의 이전 PlayMode 실패 기록)은 모두 받아들일 만하다. 고칠 것으로 보지 않는다. 10번 조건은 "루프가 판정"으로 적는다.
- 할 일: 코드를 새로 고칠 것은 없다. 기능 브랜치의 작업이 테스트를 통과하는지 확인하고, 보고서의 "테스트 결과"를 이번 실행 결과로 갱신한다.
- 바꾼 칸: 완료 조건 10번의 "기존 `GameBootTests`가 오류 로그 없이 통과한다"를 "기존 테스트 `GameBootTests`가 오류 로그 없이 통과한다"로 바꿨다 (뒤의 괄호 설명은 그대로).
- 기능 브랜치: task/073_audio_view 의 작업 중 커밋은 살려서 이어간다.

## 보고서

### 한 일
- 이전 작업의 AudioView, AudioClipCache, GameRoot 조립과 플레이 모드 테스트를 유지하고 변경 사항을 확인했다.
- 최신 답변에 따라 기존 테스트 GameBootTests의 통과 여부는 루프의 전체 테스트 결과로 판정한다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs`
- `Assets/Scripts/Game/Audio.meta`, `Assets/Scripts/Game/Audio/AudioClipCache.cs`, `AudioClipCache.cs.meta`, `AudioView.cs`, `AudioView.cs.meta`
- `Assets/Tests/PlayMode/Audio.meta`, `Assets/Tests/PlayMode/Audio/AudioViewTests.cs`, `AudioViewTests.cs.meta`
- `docs/dev/audio.md`, `tasks/doing/073_audio_view.md`, `.loop/result.json`

### 가정
- 보스는 제자보다 10만큼 멀리 배치한다. 테스트 중 기존 공유 루트를 비활성화하고 테스트 루트를 따로 만들어 전투 상태를 보존한다.

### 사람이 확인할 것
- 에디터에서 시작 화면 제목곡의 1초 페이드인과 시작 버튼 이후 일반 전투곡 교차 페이드, 공격·적 처치·강화 버튼·설정 창 열고 닫기 효과음을 듣는다. 설정 창의 각 음량 슬라이더를 0으로 내리면 해당 곡과 효과음이 들리지 않는지도 확인한다.

### 테스트 결과
- `git diff --check loop/20261122-065407...HEAD` 통과.
- Unity 컴파일과 테스트는 이번 실행에서 직접 실행하지 않았으며 루프의 판정을 기다린다. 기존 테스트 `GameBootTests`도 루프가 판정한다.


## 루프 기록 (2026-11-22 06:57)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/073_audio_view
- 루프 브랜치: loop/20261122-065407
