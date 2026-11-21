# 070_sfx_queue_voice_pool — 효과음 큐와 재생기 고르기

## 목표
Core(`PlumSword.Core.Audio`)에 한 프레임의 효과음 요청을 모아 규칙대로 꺼내는 `SfxQueue`와, 효과음 재생기 번호를 고르는 `SfxVoicePool`을 만든다.

## 묶음
audio

## 선행 작업
- 068_audio_catalog_rules

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/audio.md ("동작"의 "효과음"(`Drain(now)` 순서, 동시 재생), "경계 상황" 표, "수치", "연결 지점"의 `SfxQueue`·`SfxVoicePool`, "완료 조건 초안" 15~22, 38~41)

## 요구사항
- 파일 `Assets/Scripts/Core/Audio/SfxQueue.cs`, `Assets/Scripts/Core/Audio/SfxVoicePool.cs` (네임스페이스 `PlumSword.Core.Audio`). `UnityEngine`을 쓰지 않는다. 시간은 모두 인자(초, `double`)로 받는다.
- 068이 만든 `SfxId`, `AudioCatalog.SfxMinIntervalSeconds`(0.05), `AudioCatalog.MaxSfxVoices`(8), `AudioCatalog.IsSfxEnabled`를 쓴다.
- 이 작업이 정하는 연결 지점은 사양 "연결 지점" 절 그대로다. 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/audio.md`)의 연결 지점 절을 고치고 보고서의 가정에 적는다.
- `public sealed class SfxQueue`
  - `public SfxQueue(double minIntervalSeconds = AudioCatalog.SfxMinIntervalSeconds, System.Func<SfxId, bool> isEnabled = null)`: `minIntervalSeconds`가 음수·NaN·무한대면 `ArgumentOutOfRangeException`. 0은 간격 없음. `isEnabled`가 `null`이면 `AudioCatalog.IsSfxEnabled`를 쓴다.
  - `public bool HasPending { get; }`: 꺼내지 않은 요청이 있으면 `true`.
  - `public void Request(SfxId id)`: 이번 프레임 요청에 더한다. 같은 id는 하나로 본다. 정의되지 않은 값(예: `(SfxId)99`)이면 `ArgumentOutOfRangeException`.
  - `public System.Collections.Generic.IReadOnlyList<SfxId> Drain(double nowSeconds)`: `nowSeconds`가 NaN·무한대면 `ArgumentOutOfRangeException`이고 모인 요청은 그대로 남는다. 그 밖에는 모인 요청에 차례로:
    1. 합치기: 같은 `SfxId`는 한 번.
    2. 스킬이 대신: `PlumSwordSkill`이 있으면 `SwordSwing`, `AttackHit`을 뺀다.
    3. 구체적인 소리가 버튼음을 대신: `UpgradeSuccess`, `OfflineReward`, `WindowOpenClose` 중 하나라도 있으면 `UiConfirm`, `UpgradeUnavailable`을 뺀다.
    4. 꺼진 소리: `isEnabled(id)`가 `false`인 것을 뺀다.
    5. 간격: 그 id를 마지막으로 꺼낸 시각이 있고 `0 <= now - 마지막 < minIntervalSeconds`이면 뺀다. 뺀 것은 마지막 시각을 바꾸지 않는다. `now`가 마지막보다 작으면(시간이 거꾸로) 빼지 않는다.
    6. 남은 것의 마지막 시각을 `now`로 적고, `SfxId` 번호(정수 값) 순으로 돌려준다. 모인 요청은 (뺀 것까지) 모두 비운다.
  - 요청이 없으면 빈 목록(`null` 아님).
- `public sealed class SfxVoicePool`
  - `public SfxVoicePool(int count = AudioCatalog.MaxSfxVoices)`: `count < 1`이면 `ArgumentOutOfRangeException`. 처음엔 모두 비어 있다.
  - `public int Count { get; }`
  - `public int Acquire(double nowSeconds, double lengthSeconds)`: `nowSeconds`가 NaN·무한대이거나 `lengthSeconds`가 음수·NaN·무한대면 `ArgumentOutOfRangeException` (상태 그대로). 끝난 재생기(끝 시각 `<= nowSeconds`, 처음의 빈 재생기 포함) 중 번호가 가장 작은 것, 없으면 시작 시각이 가장 이른 것(같으면 번호가 작은 것)을 골라 시작 = `nowSeconds`, 끝 = `nowSeconds + lengthSeconds`로 적고 번호(0 ~ Count-1)를 돌려준다.
- 테스트는 `Assets/Tests/EditMode/Audio/SfxQueueTests.cs`, `Assets/Tests/EditMode/Audio/SfxVoicePoolTests.cs` (네임스페이스 `PlumSword.Core.Tests.Audio`). 목록 비교는 순서까지 같은지 본다.

## 완료 조건
- `Assets/Tests/EditMode/Audio/SfxQueueTests.cs`, `Assets/Tests/EditMode/Audio/SfxVoicePoolTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- (사양 15) 새 `SfxQueue()`에 `Request(EnemyDefeated)` 세 번 → `Drain(1.0)` → `[EnemyDefeated]`이고, 이어서 요청 없이 `Drain(1.01)` → 빈 목록, `HasPending` `false`.
- (사양 16) 15에 이어 `Request(EnemyDefeated)`, `Request(AttackHit)` → `Drain(1.04)` → `[AttackHit]`(`EnemyDefeated`는 간격으로 빠짐)이고, 다시 `Request(EnemyDefeated)` → `Drain(1.06)` → `[EnemyDefeated]`.
- (사양 17) `SwordSwing`, `AttackHit`, `PlumSwordSkill`을 요청하고 `Drain` → `[PlumSwordSkill]`이고, `SwordSwing`, `AttackHit`만 요청하고 `Drain` → `[SwordSwing, AttackHit]`.
- (사양 18) `UiConfirm`+`UpgradeSuccess` → `[UpgradeSuccess]`, `UpgradeUnavailable`+`UpgradeSuccess` → `[UpgradeSuccess]`, `UiConfirm`+`WindowOpenClose` → `[WindowOpenClose]`, `UiConfirm`+`OfflineReward` → `[OfflineReward]`, `UiConfirm`만 → `[UiConfirm]` (각각 새 큐에서).
- (사양 19) `BossDefeated`, `AttackHit` 순으로 요청하고 `Drain` → `[AttackHit, BossDefeated]`.
- (사양 20) `new SfxQueue(0.05, id => id != SfxId.AttackHit)`에 `AttackHit`, `SwordSwing`을 요청하고 `Drain` → `[SwordSwing]`.
- (사양 21) `Request((SfxId)99)`, `Drain(double.NaN)`, `new SfxQueue(-0.01)` → 각각 `ArgumentOutOfRangeException`이다.
- (사양 21) 요청이 있는 큐에서 `Drain(double.NaN)`이 예외를 던진 뒤에도 `HasPending`은 `true`다.
- (사양 21) `new SfxQueue(0)`에서 `UiConfirm`을 요청해 `Drain(1.0)`, 다시 요청해 `Drain(1.0)` → 두 번 다 `[UiConfirm]`.
- (사양 22) `Drain(5.0)`으로 `UiConfirm`을 꺼낸 뒤 `Request(UiConfirm)` → `Drain(4.0)` → `[UiConfirm]`.
- (사양 38) `new SfxVoicePool(0)` → `ArgumentOutOfRangeException`이고, `new SfxVoicePool().Count` → 8.
- (사양 39) `new SfxVoicePool(2)`에서 `Acquire(0, 1)` → 0, `Acquire(0.1, 1)` → 1, `Acquire(0.2, 1)` → 0, `Acquire(0.3, 1)` → 1, `Acquire(2.0, 1)` → 0.
- (사양 40) `new SfxVoicePool()`(8개)에서 `Acquire(0, 1)` 여덟 번 → 0~7 차례로, 아홉 번째 `Acquire(0.5, 1)` → 0.
- (사양 41) `Acquire(double.NaN, 1)`, `Acquire(0, -1)`, `Acquire(0, double.PositiveInfinity)` → 각각 `ArgumentOutOfRangeException`.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 068이 만든 `MusicTrack`, `SfxId`, `AudioCatalog`, `MusicRules`, `AudioMix`의 공개 모양과 값을 바꾸지 않는다. `AudioCatalog`의 임시값(배율, 켜짐, 간격, 재생기 수, 교차 페이드 시간)을 사양 표와 다르게 바꾸지 않는다.
- `MusicFader`(069), `SfxTriggers`(071), `UiSoundWatch`(072), Game 쪽 화면 코드(073)는 만들거나 고치지 않는다.
- 다른 기능의 Core 코드(`Assets/Scripts/Core/` 아래 `Audio` 밖의 폴더)와 다른 기능의 화면 코드(`StatUpgradeView`, `SettingsView`, `StartScreenView`, `OfflineRewardView`, `SaveErrorView` 등)는 고치지 않는다.
- 씬·프리팹·에셋 파일, 오디오 파일, 오디오 `.meta` 가져오기 설정, 프로젝트 오디오 설정은 고치지 않는다.
- Core 에서 `UnityEngine`을 쓰지 않는다.

## 보고서

### 한 일
- 효과음 요청의 중복 제거, 대체, 활성화 및 간격 필터와 번호 순 반환을 구현했다.
- 비어 있거나 끝난 재생기를 우선하고 모두 사용 중이면 가장 먼저 시작한 재생기를 고르는 풀을 구현했다.
- 완료 조건과 잘못된 입력의 상태 보존을 검사하는 에딧 모드 테스트를 추가했다.
- 관문이 검사를 인식하도록 목록 검사를 `Assert.That`으로 바꾸고 순서와 빈 목록 검증을 유지했다.

### 바뀐 파일
- `Assets/Scripts/Core/Audio/SfxQueue.cs`, `SfxVoicePool.cs` 및 각 `.meta`
- `Assets/Tests/EditMode/Audio/SfxQueueTests.cs`, `SfxVoicePoolTests.cs` 및 각 `.meta`
- `tasks/doing/070_sfx_queue_voice_pool.md`, `.loop/result.json`

### 가정
- 없음. 공개 연결 지점과 동작은 기존 사양을 따르므로 사양 변경은 없다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 추가한 테스트와 기존 테스트의 루프 판정을 기다린다.
- `git diff --check`를 실행했으며 공백 오류는 없었다. Git이 줄바꿈 변환 경고를 출력했다.


## 루프 기록 (2026-11-22 05:24)

- 작업 에이전트: codex(시도 1~2)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/070_sfx_queue_voice_pool
- 루프 브랜치: loop/20261122-050216
