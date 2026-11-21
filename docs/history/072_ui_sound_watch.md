# 072_ui_sound_watch — 창·버튼 소리 판정

## 목표
Core(`PlumSword.Core.Audio`)에 화면이 매 프레임 넘기는 창 상태와 버튼 누름을 받아 `SfxQueue`에 창·버튼 효과음을 요청하는 `UiSoundWatch`를 만든다.

## 묶음
audio

## 선행 작업
- 070_sfx_queue_voice_pool

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/audio.md ("동작"의 "효과음"(붙는 곳 표, 버튼 판정, 창 판정, `Drain(now)` 순서 3번), "경계 상황" 표, "연결 지점"의 `UiSoundWatch`, "완료 조건 초안" 32~37)

## 요구사항
- 파일 `Assets/Scripts/Core/Audio/UiSoundWatch.cs`, `public sealed class UiSoundWatch` (네임스페이스 `PlumSword.Core.Audio`). `UnityEngine`을 쓰지 않는다. 화면 입력(포인터, `EventSystem`, `Button`)은 읽지 않고 bool 값만 받는다 (그건 073의 `AudioView`가 한다).
- 070이 만든 `SfxQueue`(`Request(SfxId)`, `Drain(double)`)와 068이 만든 `SfxId`를 쓴다.
- 이 작업이 정하는 연결 지점은 사양 "연결 지점" 절 그대로다. 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/audio.md`)의 연결 지점 절을 고치고 보고서의 가정에 적는다.
  - `public UiSoundWatch(SfxQueue queue)`: `queue`가 `null`이면 `ArgumentNullException`. 처음 상태: 네 값(`settingsOpen`, `rewardWindowShowing`, `errorWindowShowing`, `rewardPaid`) 모두 `false`, 기억한 누름 없음.
  - `public void UpdateWindows(bool settingsOpen, bool rewardWindowShowing, bool errorWindowShowing, bool rewardPaid)`:
    - `settingsOpen`이 지난번 값과 다르면(열림·닫힘 모두) `WindowOpenClose`.
    - `rewardWindowShowing`이 `false` → `true`면 `WindowOpenClose`. `true` → `false`는 소리 없음.
    - `errorWindowShowing`이 `false` → `true`면 `WindowOpenClose`. `true` → `false`는 소리 없음.
    - `rewardPaid`가 `false` → `true`면 `OfflineReward`.
    - 그다음 네 값을 기억한다. 처음 상태가 모두 `false`이므로 첫 호출에 이미 떠 있는 창도 소리를 낸다.
  - `public void PressStarted(bool onButton, bool interactable)`: `onButton`이 `true`면 `interactable`이 `true`일 때 `UiConfirm`, `false`일 때 `UpgradeUnavailable`을 기억한다 (요청은 아직 하지 않음). `onButton`이 `false`면 기억을 지운다. 이전 기억은 덮어쓴다.
  - `public void PressEnded()`: 기억한 소리를 `queue.Request`하고 기억을 지운다. 기억이 없으면 아무 일 없음.
- 같은 프레임에 버튼음과 `WindowOpenClose`·`OfflineReward`·`UpgradeSuccess`가 함께 있을 때 버튼음이 빠지는 것은 `SfxQueue.Drain`의 3번 규칙이 한다. 여기서는 그대로 요청한다.
- 테스트는 `Assets/Tests/EditMode/Audio/UiSoundWatchTests.cs` (네임스페이스 `PlumSword.Core.Tests.Audio`). 큐는 `new SfxQueue(0)`(간격 없음)을 쓰고, 한 프레임마다(호출 묶음마다) `Drain`을 불러 그 결과를 검사한다. 목록 비교는 순서까지 본다.

## 완료 조건
- `Assets/Tests/EditMode/Audio/UiSoundWatchTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- (사양 32) `new UiSoundWatch(null)` → `ArgumentNullException`이다.
- (사양 33) 새 감시에서 `UpdateWindows(true, false, false, false)` → `Drain` → `[WindowOpenClose]`이고, 같은 값으로 다시 → 빈 목록이고, `UpdateWindows(false, false, false, false)` → `[WindowOpenClose]`이다.
- (사양 34) `rewardWindowShowing`만 `false` → `true`로 바꾸면 `[WindowOpenClose]`이고, 이어서 `true` → `false`로 바꾸면 빈 목록이다.
- (사양 34) `errorWindowShowing`만 `false` → `true`로 바꾸면 `[WindowOpenClose]`이다.
- (사양 34) 새 감시의 첫 호출이 `UpdateWindows(false, true, false, false)`여도 `[WindowOpenClose]`이다.
- (사양 35) `rewardPaid`가 `false` → `true`면 `[OfflineReward]`이고, 이어서 계속 `true`면 빈 목록이다.
- (사양 35) 보상 창이 떠 있던(`(false, true, false, false)`) 다음 프레임에 `UpdateWindows(false, false, false, true)`(창이 사라지고 보상 받음) → `[OfflineReward]`이다.
- (사양 36) `PressStarted(true, true)` → `Drain` → 빈 목록이고, 이어서 `PressEnded()` → `Drain` → `[UiConfirm]`이다.
- (사양 36) `PressStarted(true, false)`, `PressEnded()` → `Drain` → `[UpgradeUnavailable]`이다.
- (사양 36) `PressStarted(false, true)`, `PressEnded()` → `Drain` → 빈 목록이다.
- (사양 36) 누르기 시작 없이 `PressEnded()` → `Drain` → 빈 목록이다.
- (사양 37) 같은 프레임에 `PressStarted(true, true)`, `PressEnded()`, `UpdateWindows(true, false, false, false)` → `Drain` → `[WindowOpenClose]`이다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 068·070이 만든 `SfxId`, `AudioCatalog`, `SfxQueue` 등의 공개 모양과 값을 바꾸지 않는다. `AudioCatalog`의 임시값(배율, 켜짐, 간격, 재생기 수, 교차 페이드 시간)을 사양 표와 다르게 바꾸지 않는다.
- `SfxTriggers`(071)는 만들거나 고치지 않는다. Game 쪽 화면 코드(073)는 만들지 않는다.
- 다른 기능의 Core 코드(`Assets/Scripts/Core/` 아래 `Audio` 밖의 폴더)와 다른 기능의 화면 코드(`StatUpgradeView`, `SettingsView`, `StartScreenView`, `OfflineRewardView`, `SaveErrorView` 등)는 고치지 않는다.
- 씬·프리팹·에셋 파일, 오디오 파일, 오디오 `.meta` 가져오기 설정, 프로젝트 오디오 설정은 고치지 않는다.
- Core 에서 `UnityEngine`을 쓰지 않는다.

## 보고서

### 한 일
- 창 상태 변화와 보상 지급, 버튼 누름과 놓음을 감시하여 효과음 큐에 요청한다.
- 완료 조건과 누름 기억의 덮어쓰기, 해제 및 상태 재전환을 검사하는 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/Audio/UiSoundWatch.cs` 및 `.meta`
- `Assets/Tests/EditMode/Audio/UiSoundWatchTests.cs` 및 `.meta`
- `tasks/doing/072_ui_sound_watch.md` 보고서, `.loop/result.json`

### 가정
없음. 공개 연결 지점과 동작은 기존 사양을 따른다.

### 사람이 확인할 것
없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 신규 및 기존 테스트의 결과는 루프 판정을 기다린다.
- `git diff --check` 통과. 새 파일은 별도로 확인했다.


## 루프 기록 (2026-11-22 05:34)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/072_ui_sound_watch
- 루프 브랜치: loop/20261122-050216
