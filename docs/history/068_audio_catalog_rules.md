# 068_audio_catalog_rules — 소리 목록·곡 규칙·음량 계산

## 목표
Core(`PlumSword.Core.Audio`)에 소리 기능이 함께 쓰는 기본 타입을 만든다: 곡·효과음 열거형(`MusicTrack`, `SfxId`), 소리 목록과 상수(`AudioCatalog`), 지금 곡을 정하는 규칙(`MusicRules`), 음량 계산(`AudioMix`).

## 묶음
audio

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/audio.md ("동작"의 "배경음악"·"음량"·"일시정지와 포커스", "수치"와 "소리 목록", "연결 지점"의 `MusicTrack`·`SfxId`·`AudioCatalog`·`MusicRules`·`AudioMix`, "완료 조건 초안" 1~5, 12~14)
- docs/spec/stage.md ("연결 지점" 절의 `StageLayout.FinalDisplayStage`)
- docs/dev/audio.md (파일 목록)

## 요구사항
- 폴더 `Assets/Scripts/Core/Audio/`, 네임스페이스 `PlumSword.Core.Audio`. 파일 하나에 공개 타입 하나: `MusicTrack.cs`, `SfxId.cs`, `AudioCatalog.cs`, `MusicRules.cs`, `AudioMix.cs`. `UnityEngine`을 쓰지 않는다.
- 이 작업이 소리 기능의 연결 지점 일부를 정한다. 아래 모양은 사양 "연결 지점" 절 그대로다. 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/audio.md`)의 연결 지점 절을 고치고 보고서의 가정에 적는다.
- `public enum MusicTrack { None = 0, Title = 1, BattleNormal = 2, BattleBoss = 3, BattleFinalBoss = 4 }`
- `public enum SfxId { SwordSwing = 0, AttackHit = 1, UpgradeSuccess = 2, PlumSwordSkill = 3, RealmBreakthrough = 4, UiConfirm = 5, BossAppear = 6, BossDefeated = 7, PlayerDefeated = 8, OfflineReward = 9, EnemyDefeated = 10, UpgradeUnavailable = 11, WindowOpenClose = 12, StageTransition = 13 }`
- `public static class AudioCatalog`
  - `public const double CrossfadeSeconds = 1.0;` (임시) / `public const double SfxMinIntervalSeconds = 0.05;` (임시) / `public const int MaxSfxVoices = 8;` (임시)
  - `public static string MusicPath(MusicTrack track)`: `None` → `null`, `Title` → `"Audio/Music/title"`, `BattleNormal` → `"Audio/Music/battle_normal"`, `BattleBoss` → `"Audio/Music/battle_boss"`, `BattleFinalBoss` → `"Audio/Music/battle_final_boss"`.
  - `public static string SfxPath(SfxId id)`: `"Audio/SFX/"` + 파일 이름. 파일 이름은 `SwordSwing` `sword_swing`, `AttackHit` `attack_hit`, `UpgradeSuccess` `upgrade_success`, `PlumSwordSkill` `plum_sword_skill`, `RealmBreakthrough` `realm_breakthrough`, `UiConfirm` `ui_confirm`, `BossAppear` `boss_appear`, `BossDefeated` `boss_defeated`, `PlayerDefeated` `player_defeated`, `OfflineReward` `offline_reward`, `EnemyDefeated` `enemy_defeated`, `UpgradeUnavailable` `upgrade_unavailable`, `WindowOpenClose` `window_open_close`, `StageTransition` `stage_transition`.
  - `public static double MusicBaseVolume(MusicTrack track)`: `None` 0.0, 나머지 4곡 0.6 (임시).
  - `public static double SfxBaseVolume(SfxId id)`: `SwordSwing`, `AttackHit`, `EnemyDefeated` 0.6, 나머지 11종 1.0 (임시).
  - `public static bool IsSfxEnabled(SfxId id)`: 14종 모두 `true` (임시).
  - 효과음 값은 나중에 사람이 들어 보고 이 클래스 한 곳만 고쳐 바꿀 수 있게, 값을 표(배열·switch 등) 한 곳에 모은다.
  - `MusicTrack`·`SfxId`를 받는 다섯 메서드 모두, 정의되지 않은 값(예: `(MusicTrack)99`, `(SfxId)99`, `(SfxId)-1`)이면 `ArgumentOutOfRangeException`.
- `public static class MusicRules`
  - `public static MusicTrack TrackFor(bool isStartScreenShowing, int displayStageNumber, bool hasBoss)`
  - 먼저 `displayStageNumber`가 1~5 밖이면 `ArgumentOutOfRangeException` (시작 화면이어도 같다).
  - `isStartScreenShowing`이면 `Title`. 아니고 `hasBoss`면 `displayStageNumber == StageLayout.FinalDisplayStage`(5, `PlumSword.Core.Stage.StageLayout`)일 때 `BattleFinalBoss`, 아니면 `BattleBoss`. 나머지는 `BattleNormal`.
- `public static class AudioMix` (계산은 모두 `double`, 반올림 없음)
  - `clamp01(x)`: NaN은 0, 0보다 작으면 0, 1보다 크면 1.
  - `public static double MusicVolume(MusicTrack track, double bgmScale, double weight)` = `MusicBaseVolume(track) × clamp01(bgmScale) × clamp01(weight)`. `None`은 0.
  - `public static double SfxVolume(SfxId id, double sfxScale)` = `SfxBaseVolume(id) × clamp01(sfxScale)`.
  - `public static bool ShouldPlaySfx(double sfxScale)` = `sfxScale > 0` (NaN은 `false`).
  - `public static bool ShouldPauseAll(bool applicationPaused, bool hasFocus)` = `applicationPaused || !hasFocus`.
  - 정의되지 않은 `MusicTrack`·`SfxId`는 `AudioCatalog`처럼 `ArgumentOutOfRangeException`.
- 테스트는 `Assets/Tests/EditMode/Audio/` (네임스페이스 `PlumSword.Core.Tests.Audio`). 실수 비교는 오차 1e-9.
- 완료 조건 3의 파일 확인: 테스트에서 `System.IO.Directory.GetCurrentDirectory()`(유니티 테스트 실행 중에는 프로젝트 폴더)를 기준으로 `Path.Combine(<프로젝트 폴더>, "Assets", "Resources", <경로> + ".wav")`를 `System.IO.File.Exists`로 본다. 파일은 이미 `Assets/Resources/Audio/Music/`, `Assets/Resources/Audio/SFX/`에 있다.
- 이 작업이 끝나면 `docs/spec/index.md`는 고치지 않아도 된다 (소리 줄이 이미 있다).

## 완료 조건
- `Assets/Tests/EditMode/Audio/AudioCatalogTests.cs`, `Assets/Tests/EditMode/Audio/MusicRulesTests.cs`, `Assets/Tests/EditMode/Audio/AudioMixTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- (사양 1) `AudioCatalog.MusicPath`: `Title` → `"Audio/Music/title"`, `BattleNormal` → `"Audio/Music/battle_normal"`, `BattleBoss` → `"Audio/Music/battle_boss"`, `BattleFinalBoss` → `"Audio/Music/battle_final_boss"`, `None` → `null`이고, `(MusicTrack)99` → `ArgumentOutOfRangeException`.
- (사양 2) `AudioCatalog.SfxPath`가 14종 모두 요구사항 표의 경로를 돌려주고(예: `SfxId.StageTransition` → `"Audio/SFX/stage_transition"`), `(SfxId)99` → `ArgumentOutOfRangeException`.
- (사양 3) 곡 4개와 효과음 14개의 경로마다 프로젝트 폴더 기준 `Assets/Resources/<경로>.wav` 파일이 있다 (`System.IO.File.Exists`가 18개 모두 `true`).
- (사양 4) `MusicBaseVolume`은 4곡 0.6, `None` 0.0이고, `SfxBaseVolume`은 `SwordSwing`·`AttackHit`·`EnemyDefeated` 0.6, 나머지 11종 1.0이고, `IsSfxEnabled`는 14종 모두 `true`이며 `(SfxId)99` → `ArgumentOutOfRangeException`.
- (사양 5) `MusicRules.TrackFor`: `(true, 3, true)` → `Title`, `(false, 1, false)` → `BattleNormal`, `(false, 4, true)` → `BattleBoss`, `(false, 5, true)` → `BattleFinalBoss`, `(false, 5, false)` → `BattleNormal`이고, `(false, 0, false)`와 `(true, 6, false)` → `ArgumentOutOfRangeException`.
- (사양 12) `AudioMix.MusicVolume`: `(BattleNormal, 0.8, 1.0)` → 0.48, `(BattleBoss, 0.8, 0.25)` → 0.12, `(Title, 0.0, 1.0)` → 0, `(None, 1.0, 1.0)` → 0, `(Title, 1.5, 2.0)` → 0.6, `(Title, double.NaN, 1.0)` → 0 (오차 1e-9).
- (사양 13) `AudioMix.SfxVolume`: `(AttackHit, 0.8)` → 0.48, `(BossAppear, 0.8)` → 0.8, `(UiConfirm, 0.0)` → 0이고, `ShouldPlaySfx(0.0)` → `false`, `ShouldPlaySfx(0.01)` → `true`, `ShouldPlaySfx(double.NaN)` → `false`.
- (사양 14) `AudioMix.ShouldPauseAll`: `(false, true)` → `false`이고 `(true, true)`, `(false, false)`, `(true, false)` → `true`.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `MusicFader`, `SfxQueue`, `SfxVoicePool`, `SfxTriggers`, `UiSoundWatch`, Game 쪽 화면 코드는 만들지 않는다 (069~073이 한다).
- `AudioCatalog`의 임시값(배율, 켜짐, 간격, 재생기 수, 교차 페이드 시간)을 사양 표와 다르게 바꾸지 않는다.
- 다른 기능의 Core 코드(`Assets/Scripts/Core/` 아래 `Audio` 밖의 폴더)와 다른 기능의 화면 코드(`StatUpgradeView`, `SettingsView`, `StartScreenView`, `OfflineRewardView`, `SaveErrorView` 등)는 고치지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`), 오디오 파일(`Assets/Resources/Audio/` 아래의 `.wav`), 오디오 `.meta` 가져오기 설정, 프로젝트 오디오 설정(`ProjectSettings/`)은 고치지 않는다.
- Core 에서 `UnityEngine`을 쓰지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)는 읽지도 고치지도 않는다.

## 보고서

### 한 일
- 곡과 효과음 열거형, 소리 목록, 곡 선택 규칙 및 음량 계산을 순수 C#으로 구현했다.
- 경로, 18개 WAV 파일, 기본값, 잘못된 열거형, 곡 선택, 음량과 포커스 규칙의 에딧 모드 테스트를 추가했다.
- WAV 파일 존재 검사를 테스트 본문에 직접 배치해 관문에서 확인할 수 있게 했다.

### 바뀐 파일
- `Assets/Scripts/Core/Audio/`의 다섯 C# 파일과 각 `.meta`, 폴더 `.meta`.
- `Assets/Tests/EditMode/Audio/`의 세 테스트 파일과 각 `.meta`, 폴더 `.meta`.
- `tasks/doing/068_audio_catalog_rules.md`에 보고서를 추가하고 `.loop/result.json`을 작성했다.

### 가정
- 없음. 사양의 공개 연결 지점과 임시 수치를 그대로 사용했다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았다. 추가한 테스트와 기존 테스트의 실행 및 결과 판정은 루프 스크립트를 기다린다.
- 파일 목록에서 WAV 18개가 존재함을 확인했다. Core의 새 코드에 `UnityEngine` 참조가 없음을 확인했다.


## 루프 기록 (2026-11-22 05:10)

- 작업 에이전트: codex(시도 1~2)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/068_audio_catalog_rules
- 루프 브랜치: loop/20261122-050216
