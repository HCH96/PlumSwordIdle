# 069_music_fader — 곡 교차 페이드 상태

## 목표
Core(`PlumSword.Core.Audio`)에 두 곡의 교차 페이드 비율을 계산하는 `MusicFader`를 만든다. 재생은 하지 않고 상태만 바꾼다.

## 묶음
audio

## 선행 작업
- 068_audio_catalog_rules

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/audio.md ("동작"의 "배경음악"(교차 페이드), "경계 상황" 표, "수치", "연결 지점"의 `MusicFader`, "완료 조건 초안" 6~11)

## 요구사항
- 파일 `Assets/Scripts/Core/Audio/MusicFader.cs`, `public sealed class MusicFader` (네임스페이스 `PlumSword.Core.Audio`). `UnityEngine`을 쓰지 않는다.
- 068이 만든 `MusicTrack`과 `AudioCatalog.CrossfadeSeconds`(1.0)를 쓴다.
- 이 작업이 정하는 연결 지점은 사양 "연결 지점" 절 그대로다. 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/audio.md`)의 연결 지점 절을 고치고 보고서의 가정에 적는다.
  - `public MusicFader(double crossfadeSeconds = AudioCatalog.CrossfadeSeconds)`: 0 이하·NaN·무한대면 `ArgumentOutOfRangeException`. 처음 상태: `Current` `None`, `Previous` `None`, 두 비율 0.
  - `public MusicTrack Current { get; }` 커지고 있거나 다 커진 곡. `public double CurrentWeight { get; }` 0~1, `Current`가 `None`이면 0.
  - `public MusicTrack Previous { get; }` 줄어들고 있는 곡, 없으면 `None`. `public double PreviousWeight { get; }` 0~1, `Previous`가 `None`이면 0.
  - `public bool Request(MusicTrack track)`:
    - 정의되지 않은 값(예: `(MusicTrack)99`)이면 `ArgumentOutOfRangeException` (상태 그대로).
    - `track == Current`면 `false`, 아무 일 없음 (비율 그대로).
    - 다르면: `Current`가 `None`이 아니면 `Previous = Current`, `PreviousWeight = CurrentWeight` (그전 `Previous`는 버린다). `Current`가 `None`이면 `Previous`와 `PreviousWeight`는 그대로. 그다음 `Current = track`, `CurrentWeight = 0`, `true`를 돌려준다.
  - `public void Tick(double deltaSeconds)`:
    - 음수·NaN·무한대면 `ArgumentOutOfRangeException` (상태 그대로). 0이면 아무 일 없음.
    - 변화량 `d = deltaSeconds / crossfadeSeconds`. `CurrentWeight = min(1, CurrentWeight + d)` (`Current`가 `None`이면 0 그대로). `PreviousWeight = max(0, PreviousWeight - d)`, 0이 되면 `Previous = None`.
- 두 곡보다 많이 겹치지 않는다 (상태가 `Current`, `Previous` 둘뿐).
- 테스트는 `Assets/Tests/EditMode/Audio/MusicFaderTests.cs` (네임스페이스 `PlumSword.Core.Tests.Audio`). 실수 비교는 오차 1e-9. 완료 조건 7~10은 앞 조건의 상태에 이어서 하는 시나리오이므로 테스트 안에서 앞 단계를 다시 만들어 이어 간다.

## 완료 조건
- `Assets/Tests/EditMode/Audio/MusicFaderTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- (사양 6) `new MusicFader()`는 `Current`·`Previous`가 `None`이고 두 비율이 0이며, `new MusicFader(0)`, `new MusicFader(-1)`, `new MusicFader(double.NaN)` → `ArgumentOutOfRangeException`.
- (사양 7) 새 `MusicFader`에 `Request(Title)` → `true`, `Current` `Title`, `CurrentWeight` 0이고, 이어서 `Tick(0.5)` → `CurrentWeight` 0.5, `Tick(0.6)` → 1.0이며, 다시 `Request(Title)` → `false`이고 `CurrentWeight` 1.0 그대로다.
- (사양 8) 7에 이어 `Request(BattleNormal)` → `true`, `Previous` `Title`·`PreviousWeight` 1.0, `Current` `BattleNormal`·`CurrentWeight` 0.0이고, `Tick(0.25)` → `CurrentWeight` 0.25, `PreviousWeight` 0.75.
- (사양 9) 8에 이어 `Request(BattleBoss)` → `Previous` `BattleNormal`·`PreviousWeight` 0.25(`Title` 버림), `Current` `BattleBoss`·`CurrentWeight` 0.0이고, `Tick(0.25)` → `CurrentWeight` 0.25, `Previous` `None`, `PreviousWeight` 0.
- (사양 10) `Title`이 `CurrentWeight` 1.0일 때 `Request(None)` → `true`, `Previous` `Title`·1.0, `Current` `None`·0이고, `Tick(1.0)` → `Previous` `None`이며 `CurrentWeight`는 0 그대로.
- (사양 11) `Tick(-0.1)`, `Tick(double.NaN)`, `Tick(double.PositiveInfinity)` → `ArgumentOutOfRangeException`이고 비율은 그대로이며, `Tick(0)` → 상태 그대로이고, `Request((MusicTrack)99)` → `ArgumentOutOfRangeException`.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 068이 만든 `MusicTrack`, `SfxId`, `AudioCatalog`, `MusicRules`, `AudioMix`의 공개 모양과 값을 바꾸지 않는다. `AudioCatalog`의 임시값(배율, 켜짐, 간격, 재생기 수, 교차 페이드 시간)을 사양 표와 다르게 바꾸지 않는다.
- `SfxQueue`, `SfxVoicePool`, `SfxTriggers`, `UiSoundWatch`, Game 쪽 화면 코드는 만들지 않는다 (070~073이 한다).
- 다른 기능의 Core 코드(`Assets/Scripts/Core/` 아래 `Audio` 밖의 폴더)와 다른 기능의 화면 코드(`StatUpgradeView`, `SettingsView`, `StartScreenView`, `OfflineRewardView`, `SaveErrorView` 등)는 고치지 않는다.
- 씬·프리팹·에셋 파일, 오디오 파일, 오디오 `.meta` 가져오기 설정, 프로젝트 오디오 설정은 고치지 않는다.
- Core 에서 `UnityEngine`을 쓰지 않는다.

## 보고서

### 한 일
- 두 곡의 교차 페이드 상태, 중간 전환, 무음 요청과 유효성 검사를 구현했다.
- 완료 조건 6~11과 사용자 지정 시간 및 무음 중 재요청을 검증하는 에딧 모드 테스트를 작성했다.
- 무음 중 재요청 테스트의 상태 검사를 본문에 명시하여 관문에서 검사를 인식하도록 수정했다.
- 무음 중 새 곡 요청 시 이전 곡을 유지하는 동작을 사양에 명확히 적었다.

### 바뀐 파일
- `Assets/Scripts/Core/Audio/MusicFader.cs` 및 `.meta`
- `Assets/Tests/EditMode/Audio/MusicFaderTests.cs` 및 `.meta`
- `docs/spec/audio.md`
- `tasks/doing/069_music_fader.md`, `.loop/result.json`

### 가정
- 없음. 공개 연결 지점은 사양과 같다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 테스트를 직접 실행하지 않았으며 전체 테스트와 새 테스트 및 관문 검사의 루프 판정을 기다린다.


## 루프 기록 (2026-11-22 05:16)

- 작업 에이전트: codex(시도 1~2)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/069_music_fader
- 루프 브랜치: loop/20261122-050216
