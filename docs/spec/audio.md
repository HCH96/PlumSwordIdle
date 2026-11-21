# 소리 사양

- 기능 이름: audio (네임스페이스 `PlumSword.Core.Audio`, 화면 `PlumSword.Game.Audio`)
- 작성일: 2026-11-22

## 목표

`Assets/Resources/Audio/`에 있는 배경음악 4곡과 효과음 14종(`docs/dev/audio.md`)을 게임에 연결한다. 지금 상태(시작 화면, 일반 전투, 보스, 최종 보스)에 맞는 곡을 교차 페이드로 틀고, 전투·스테이지·강화·경지·오프라인 보상·창·버튼의 알림에 효과음을 낸다. 음량은 설정 창의 `VolumeSettings`(`docs/spec/start_menu.md`)를 따른다.

기획 원본에는 소리 규칙이 없고 2단계 "시작 화면과 기본 메뉴"의 "배경음악·효과음 음량을 각각 조절하고 기기에 저장한다"뿐이다. 나머지는 이 사양에서 정했다. 효과음은 아직 하나씩 들어 보고 확정하지 않았으므로, 효과음의 연결·기본 배율·켜짐은 모두 **(임시)**이고 `AudioCatalog` 표 한 곳만 고쳐 바꿀 수 있게 한다.

## 동작

### 판단과 재생을 나눈다

- "지금 어떤 곡이어야 하는가", "이 알림에 어떤 효과음을 내는가", 합치기·대신하기·간격 제한, 음량 계산, 재생기 고르기는 모두 Core(`PlumSword.Core.Audio`)의 순수 C# 클래스가 정한다. 소리 없이 에딧 모드 테스트로 판정한다.
- Game의 `AudioView` 하나가 `AudioSource` 재생, `Resources.Load`, 화면 입력 읽기, 포커스 처리만 한다. 매 프레임 `LateUpdate`에서 상태를 Core에 넘기고 Core가 돌려준 대로 재생한다. `LateUpdate`이므로 그 프레임의 전투 `Tick`(`CombatView.Update`), 스테이지 `Tick`, 버튼 `onClick`(EventSystem)의 알림이 모두 모인 뒤에 판단한다.
- 시간은 실제 시간이다: 페이드에는 `Time.unscaledDeltaTime`, 효과음 간격과 재생기 고르기에는 `Time.unscaledTimeAsDouble`을 쓴다.

### 배경음악

- **지금 곡** `MusicRules.TrackFor(isStartScreenShowing, displayStageNumber, hasBoss)`. `AudioView`는 매 프레임 `GameRoot.IsStartScreenShowing`, `StageProgress.DisplayStageNumber`, `StageProgress.Boss != null`을 넘긴다.
  1. 시작 화면이 떠 있으면 `Title`.
  2. 아니고 보스가 있으면, 화면상 큰 스테이지가 5(`StageLayout.FinalDisplayStage`)면 `BattleFinalBoss`, 1~4면 `BattleBoss`.
  3. 나머지는 `BattleNormal`. 스테이지 시작 전(오프라인 보상 창·손상 창이 떠 있어 `Begin` 전)도 보스가 없으므로 `BattleNormal`이다.
- 이 규칙 하나로 아래 전환이 모두 나온다 (스테이지 사양의 배치 시점 그대로):
  - 6구간 뒤 페이드의 가려진 순간에 보스가 배치되면 그 프레임부터 보스곡.
  - 보스가 쓰러져도 `StageProgress.Boss`는 다음 가려진 순간까지 남으므로 페이드아웃 0.5초 동안 보스곡이 이어지고, 가려진 순간 새 스테이지 상태의 곡으로 바뀐다 (1~4스테이지의 다음 스테이지는 일반곡).
  - 제자가 쓰러진 경우도 같다: 1~4스테이지는 가려진 순간 1구간부터 다시 배치되어 일반곡. 5스테이지는 새 최종 보스가 바로 배치되어 최종 보스곡 그대로.
  - 최종 보스를 이기면 가려진 순간 다음 회차 1스테이지 1구간이 배치되어 일반곡.
  - 저장된 5스테이지로 이어하기면 `Begin`이 최종 보스를 바로 배치하므로 시작 버튼을 누른 프레임에 `Title` → `BattleFinalBoss`.
  - `TO TITLE` 뒤 새 루트는 시작 화면이 떠 있으므로 `Title`.
- **교차 페이드** `MusicFader`: 곡이 바뀌면 새 곡을 처음부터 틀어 1초에 걸쳐 비율 0 → 1로 키우고, 동시에 옛 곡을 그때의 비율에서 1초에 1씩 줄여 0이 되면 멈춘다. 게임을 켠 뒤 처음 곡도 0에서 1초에 걸쳐 커진다.
  - 바꿀 곡이 지금 곡과 같으면 아무것도 하지 않는다 (이어 틀기. 처음부터 다시 틀지 않는다).
  - 교차 페이드 중에 또 다른 곡으로 바뀌면, 줄어들던 옛 곡은 바로 멈추고(버림), 커지던 곡이 그때 비율에서 줄어드는 쪽이 되며, 새 곡이 0부터 커진다. 두 곡보다 많이 겹치지 않는다.
  - `None`을 요청하면 현재 곡을 줄인다. 현재 곡이 `None`인 동안 새 곡을 요청하면 아직 줄어드는 이전 곡은 그대로 두고 새 곡을 키운다.
  - 화면 페이드(`StageProgress.FadeAlpha`)와 따로 계산한다. 곡이 바뀌는 때가 가려진 순간이라 새 곡은 화면 페이드인(0.5초)과 함께 커지기 시작해 0.5초 더 커진다.
- 곡은 반복 재생한다 (`AudioSource.loop = true`).

### 효과음

- 효과음은 `SfxQueue`에 요청해 모았다가 프레임마다 `Drain(now)`으로 한 번에 꺼내 재생한다. 요청하는 곳은 Core의 `SfxTriggers`(전투·스테이지·강화·경지 알림)와 `UiSoundWatch`(창·버튼)다.
- **붙는 곳** (모두 (임시)):

| `SfxId` | 파일 | 요청하는 때 |
| --- | --- | --- |
| `SwordSwing` | `sword_swing` | `Battle.EnemyDamaged` (기본 공격과 스킬 모두 알림이 오지만 스킬 프레임은 아래 "대신하기"로 빠진다) |
| `AttackHit` | `attack_hit` | `Battle.EnemyDamaged` (`SwordSwing`과 함께) |
| `UpgradeSuccess` | `upgrade_success` | `StatUpgrades.Upgraded` |
| `PlumSwordSkill` | `plum_sword_skill` | `Battle.SkillUsed` |
| `RealmBreakthrough` | `realm_breakthrough` | `RealmProgress.BrokeThrough` |
| `UiConfirm` | `ui_confirm` | 켜진 버튼을 누르기 시작해서 뗀 프레임 (`UiSoundWatch`) |
| `BossAppear` | `boss_appear` | `Battle.EnemyAdded`에서 `Kind == Boss` |
| `BossDefeated` | `boss_defeated` | `StageProgress.StageCleared` (보스를 혼자 이김. 보스와 제자가 함께 쓰러지면 오지 않는다) |
| `PlayerDefeated` | `player_defeated` | `StageProgress.StageFailed` |
| `OfflineReward` | `offline_reward` | `OfflineRewardClaim.HasPaid`가 `false`에서 `true`로 바뀐 프레임 (`UiSoundWatch`) |
| `EnemyDefeated` | `enemy_defeated` | `Battle.EnemyDefeated`에서 `Kind == Normal` |
| `UpgradeUnavailable` | `upgrade_unavailable` | 꺼진(누를 수 없는) 버튼을 누르기 시작해서 뗀 프레임 (`UiSoundWatch`). 살 수 없는 강화 버튼, 전환 중 잠긴 강화 버튼 등 |
| `WindowOpenClose` | `window_open_close` | 설정 창이 열리거나 닫힌 프레임, 오프라인 보상 창 또는 손상 오류 창이 나타난 프레임 (`UiSoundWatch`) |
| `StageTransition` | `stage_transition` | 일반 무리를 모두 이겨 다음 구간(또는 보스 구간)으로 넘어가는 페이드아웃이 시작된 프레임 |

- **구간 사이 페이드 판정** (`SfxTriggers`): `StageProgress.Changed`를 받을 때마다 `Phase`가 `FadingOut`이 아니었다가 `FadingOut`이 되었고, 그때 `StageProgress.Boss == null`이고 `Battle.IsDiscipleDefeated == false`이면 `StageTransition`. 보스를 이긴 뒤(보스가 있음)와 제자가 쓰러진 뒤(패배)의 페이드에는 내지 않는다 (각각 `BossDefeated`, `PlayerDefeated`가 난다). 직전 `Phase`는 만들 때의 `StageProgress.Phase`에서 시작한다.
- 보스는 처치 효과음으로 `EnemyDefeated`를 내지 않고 `BossDefeated`만 낸다. 5스테이지는 `Begin`과 재배치 때마다 최종 보스가 추가되므로 그때마다 `BossAppear`가 난다.
- **버튼 판정** (`AudioView` → `UiSoundWatch`): `UnityEngine.InputSystem.Pointer.current`의 `press.wasPressedThisFrame`이면 그 위치로 `EventSystem.current.RaycastAll`을 해서 맨 앞 결과의 오브젝트에서 `GetComponentInParent<UnityEngine.UI.Button>()`을 찾는다. 있으면 `PressStarted(true, button.IsInteractable())`, 결과가 없거나 버튼이 아니면 `PressStarted(false, false)`. `press.wasReleasedThisFrame`이면 `PressEnded()`. 누르기 시작한 때의 버튼과 켜짐을 기억했다가 뗀 프레임에 소리를 요청하므로, 같은 프레임에 버튼의 `onClick`이 낸 더 구체적인 소리(강화 성공, 창 열고 닫기, 보상 받기)가 버튼 소리를 대신한다. 버튼 밖에서 떼도 소리는 난다. `Pointer.current` 또는 `EventSystem.current`가 `null`이면 아무것도 하지 않는다. 가림막(버튼이 아닌 그림)을 누르면 소리가 없다.
- **창 판정** (`AudioView` → `UiSoundWatch.UpdateWindows`): 설정 창은 `GameRoot.Settings.IsOpen`, 보상 창은 `[Game]` 아래에 `OfflineRewardView`가 있는지, 손상 창은 `[Game]` 아래에 `SaveErrorView`가 있는지, 보상 받음은 `GameRoot.OfflineRewardClaim != null && GameRoot.OfflineRewardClaim.HasPaid`. 보상 창과 손상 창은 나타날 때만 소리를 내고 사라질 때는 내지 않는다 (사라지는 프레임에는 `OfflineReward` 또는 `UiConfirm`이 난다). `UiSoundWatch`의 처음 상태는 모두 `false`이므로 첫 프레임에 이미 떠 있는 창도 소리를 낸다.
- **`Drain(now)` 순서** (모인 요청에 차례로 적용):
  1. **합치기**: 같은 `SfxId`는 한 프레임에 한 번. 요청을 몇 번 해도 하나로 본다 (적 3명 동시 처치도 `EnemyDefeated` 한 번).
  2. **스킬이 대신**: `PlumSwordSkill`이 있으면 `SwordSwing`, `AttackHit`을 뺀다.
  3. **구체적인 소리가 버튼음을 대신**: `UpgradeSuccess`, `OfflineReward`, `WindowOpenClose` 중 하나라도 있으면 `UiConfirm`, `UpgradeUnavailable`을 뺀다.
  4. **꺼진 소리**: `AudioCatalog.IsSfxEnabled`가 `false`인 것을 뺀다.
  5. **간격**: 같은 `SfxId`를 마지막으로 꺼낸 시각이 있고 `0 <= now - 마지막 < 0.05`이면 뺀다. 뺀 것은 마지막 시각을 바꾸지 않는다. `now`가 마지막보다 작으면(시간이 거꾸로) 빼지 않는다.
  6. 남은 것의 마지막 시각을 `now`로 적고, `SfxId` 번호 순으로 돌려준다. 모인 요청은 (뺀 것까지) 모두 비운다.
- **동시 재생** `SfxVoicePool`: 효과음 재생기는 8개. 꺼낸 효과음마다 비어 있는(끝난) 재생기 중 번호가 가장 작은 것을 쓰고, 모두 울리고 있으면 가장 먼저 시작한 것을 끊고 쓴다 (같으면 번호가 작은 것). 효과음 하나는 재생기 하나에서 한 번 재생한다 (`AudioSource.Play`, 반복 없음).

### 음량

- 곡 재생기의 음량은 매 프레임 `AudioMix.MusicVolume(곡, VolumeSettings.BgmScale, 그 곡의 페이드 비율)`. 그래서 슬라이더를 움직이면 그 프레임에 바로 반영된다. `Bgm`이 0이면 음량 0으로 계속 흐르고(멈추지 않음), 올리면 흐르던 자리에서 들린다.
- 효과음은 재생을 시작할 때 `AudioMix.SfxVolume(id, VolumeSettings.SfxScale)`. `VolumeSettings.Changed`를 받으면 울리고 있는 효과음 재생기의 음량도 그 효과음의 값으로 바로 고친다.
- `AudioMix.ShouldPlaySfx(SfxScale)`가 `false`(`Sfx`가 0)이면 꺼낸 효과음을 재생하지 않는다 (재생기를 차지하지 않는다). `Drain`은 그대로 해서 간격 기록은 남는다.
- 음량 저장은 이 기능이 하지 않는다 (`VolumePrefs` 그대로).

### 일시정지와 포커스

- 설정 창, 오프라인 보상 창, 손상 오류 창이 떠 있어도 곡은 그대로 흐른다. 효과음은 알림이 있을 때만 난다 (보상 창·손상 창·소개 동안은 전투가 멈춰 있어 전투 효과음이 나지 않는다).
- 앱이 멈추거나(`OnApplicationPause(true)`) 포커스를 잃으면(`OnApplicationFocus(false)`) 곡과 효과음을 모두 멈춘다: `AudioListener.pause = AudioMix.ShouldPauseAll(paused, focused)`. 돌아오면 멈춘 자리에서 이어진다. `AudioView`는 처음 `paused = false`, `focused = true`로 두고, `Initialize`에서 `AudioListener.pause = false`로 맞춘다.
- 재생기는 `[Game]` 루트마다 하나다. 루트를 다시 만들면(백그라운드 10초 이상 뒤 복귀, `TO TITLE`) 옛 루트와 함께 소리가 끊기고 새 루트의 곡이 처음부터 1초에 걸쳐 커진다.

### 파일이 없을 때

- `AudioClipCache.Load(path)`가 `Resources.Load<AudioClip>(path)`를 경로마다 한 번만 부르고 결과를 기억한다. `null`이면 그 경로로 처음 한 번만 `Debug.LogWarning("[Audio] 소리 파일을 찾지 못했습니다: " + path)`를 남기고 `null`을 돌려준다. 다시 부르면 다시 불러오지 않고 로그도 남기지 않는다.
- 효과음 파일이 없으면 그 효과음만 건너뛴다 (재생기를 차지하지 않는다). 곡 파일이 없으면 `MusicFader`는 그대로 진행하고 그 곡 재생기만 소리 없이 둔다 (그동안 조용하다).
- 오류 로그(`Debug.LogError`)는 남기지 않는다. 플레이 모드 테스트는 오류 로그만 실패로 본다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| 바꿀 곡이 지금 곡과 같음 | `Request`가 `false`, 아무 일 없음 | 비율·재생 위치 그대로 |
| 교차 페이드 중 또 다른 곡 | 줄던 곡 버림, 커지던 곡이 줄어드는 쪽, 새 곡 0부터 | 두 곡까지만 |
| `MusicFader.Tick` 음수·NaN·무한대 | `ArgumentOutOfRangeException` | 바뀌지 않음 |
| `MusicFader.Tick(0)` | 아무 일 없음 | |
| 정의되지 않은 `MusicTrack`·`SfxId` | `ArgumentOutOfRangeException` | 바뀌지 않음 |
| `TrackFor`의 큰 스테이지 번호가 1~5 밖 | `ArgumentOutOfRangeException` (시작 화면이어도) | |
| 같은 효과음을 한 프레임에 여러 번 | 한 번 | |
| 같은 효과음을 0.05초 안에 다시 | 뺌 | 마지막 시각 그대로 |
| 스킬과 기본 공격음이 같은 프레임 | `PlumSwordSkill`만 | |
| 버튼음과 강화 성공·창·보상이 같은 프레임 | 버튼음(`UiConfirm`, `UpgradeUnavailable`) 뺌 | |
| `Drain`의 `now`가 NaN·무한대 | `ArgumentOutOfRangeException` | 모인 요청 그대로 |
| `Drain`의 `now`가 마지막 시각보다 작음 | 간격으로 빼지 않음 | |
| 요청 없이 `Drain` | 빈 목록 | |
| 효과음 재생기 8개가 모두 울림 | 가장 먼저 시작한 것을 끊고 씀 | |
| `Sfx` 0 | 효과음 재생 안 함 | 간격 기록은 남음 |
| `Bgm` 0 | 곡은 음량 0으로 계속 | |
| 음량 Scale 이 0~1 밖·NaN (`AudioMix`) | 0~1로 잘라냄, NaN은 0 | |
| 파일 없음 | 그 소리만 건너뜀, 경로마다 경고 한 번 | |
| 앱 멈춤·포커스 잃음 | 모두 일시정지, 돌아오면 이어서 | |
| 생성자에 `null` | `ArgumentNullException` (구독하지 않음) | |
| `SfxTriggers.Dispose` 두 번 | 두 번째는 아무 일 없음 | |
| 누르기 시작 없이 `PressEnded` | 아무 일 없음 | |
| 버튼이 아닌 곳을 누름 | 소리 없음 | |

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 교차 페이드 `AudioCatalog.CrossfadeSeconds` | 1.0초 | (임시). `double` |
| 같은 효과음 최소 간격 `AudioCatalog.SfxMinIntervalSeconds` | 0.05초 | (임시). `double` |
| 효과음 재생기 수 `AudioCatalog.MaxSfxVoices` | 8 | (임시). `int` |
| 곡 기본 배율 `MusicBaseVolume` | 4곡 모두 0.6, `None`은 0.0 | (임시) |
| 효과음 기본 배율 `SfxBaseVolume` | `SwordSwing`, `AttackHit`, `EnemyDefeated` 0.6, 나머지 11종 1.0 | (임시) |
| 효과음 켜짐 `IsSfxEnabled` | 14종 모두 `true` | (임시). 들어 보고 뺄 소리는 여기서 `false` |
| 곡 음량 | `MusicBaseVolume(곡) × clamp01(BgmScale) × clamp01(비율)` | `double`. NaN은 0으로 본다 |
| 효과음 음량 | `SfxBaseVolume(id) × clamp01(SfxScale)` | `double`. `AudioSource.volume`에는 `(float)`로 넣는다 |
| 페이드 비율 변화 | 초당 `1 / CrossfadeSeconds` | 커지는 곡은 1에서, 줄어드는 곡은 0에서 멈춘다 |

- 예: `Bgm` 80이면 일반곡 음량 0.6 × 0.8 × 1.0 = 0.48. 교차 페이드 0.25초 지점의 새 보스곡은 0.6 × 0.8 × 0.25 = 0.12. `Sfx` 80이면 `AttackHit` 0.48, `BossAppear` 0.8.
- 반올림은 하지 않는다. 테스트는 오차 1e-9로 비교한다 (`AudioSource.volume`을 읽는 플레이 모드 테스트는 1e-3).

### 소리 목록 (`AudioCatalog`)

리소스 경로는 `Assets/Resources/` 다음부터 확장자를 뺀 것이다.

| 값 | 번호 | 리소스 경로 | 기본 배율 |
| --- | --- | --- | --- |
| `MusicTrack.None` | 0 | 없음 (`null`) | 0.0 |
| `MusicTrack.Title` | 1 | `Audio/Music/title` | 0.6 |
| `MusicTrack.BattleNormal` | 2 | `Audio/Music/battle_normal` | 0.6 |
| `MusicTrack.BattleBoss` | 3 | `Audio/Music/battle_boss` | 0.6 |
| `MusicTrack.BattleFinalBoss` | 4 | `Audio/Music/battle_final_boss` | 0.6 |
| `SfxId.SwordSwing` | 0 | `Audio/SFX/sword_swing` | 0.6 |
| `SfxId.AttackHit` | 1 | `Audio/SFX/attack_hit` | 0.6 |
| `SfxId.UpgradeSuccess` | 2 | `Audio/SFX/upgrade_success` | 1.0 |
| `SfxId.PlumSwordSkill` | 3 | `Audio/SFX/plum_sword_skill` | 1.0 |
| `SfxId.RealmBreakthrough` | 4 | `Audio/SFX/realm_breakthrough` | 1.0 |
| `SfxId.UiConfirm` | 5 | `Audio/SFX/ui_confirm` | 1.0 |
| `SfxId.BossAppear` | 6 | `Audio/SFX/boss_appear` | 1.0 |
| `SfxId.BossDefeated` | 7 | `Audio/SFX/boss_defeated` | 1.0 |
| `SfxId.PlayerDefeated` | 8 | `Audio/SFX/player_defeated` | 1.0 |
| `SfxId.OfflineReward` | 9 | `Audio/SFX/offline_reward` | 1.0 |
| `SfxId.EnemyDefeated` | 10 | `Audio/SFX/enemy_defeated` | 0.6 |
| `SfxId.UpgradeUnavailable` | 11 | `Audio/SFX/upgrade_unavailable` | 1.0 |
| `SfxId.WindowOpenClose` | 12 | `Audio/SFX/window_open_close` | 1.0 |
| `SfxId.StageTransition` | 13 | `Audio/SFX/stage_transition` | 1.0 |

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.Audio
{
    public enum MusicTrack
    {
        None = 0,
        Title = 1,
        BattleNormal = 2,
        BattleBoss = 3,
        BattleFinalBoss = 4,
    }

    public enum SfxId
    {
        SwordSwing = 0,
        AttackHit = 1,
        UpgradeSuccess = 2,
        PlumSwordSkill = 3,
        RealmBreakthrough = 4,
        UiConfirm = 5,
        BossAppear = 6,
        BossDefeated = 7,
        PlayerDefeated = 8,
        OfflineReward = 9,
        EnemyDefeated = 10,
        UpgradeUnavailable = 11,
        WindowOpenClose = 12,
        StageTransition = 13,
    }

    // 소리 목록과 상수. 값은 "수치"의 표. 효과음을 들어 보고 바꿀 때는 이 클래스만 고친다.
    public static class AudioCatalog
    {
        public const double CrossfadeSeconds = 1.0;        // (임시)
        public const double SfxMinIntervalSeconds = 0.05;  // (임시)
        public const int MaxSfxVoices = 8;                 // (임시)

        // None 은 null. 정의되지 않은 값은 ArgumentOutOfRangeException (아래 MusicTrack, SfxId 를 받는 메서드 모두 같다).
        public static string MusicPath(MusicTrack track);
        public static string SfxPath(SfxId id);

        // None 은 0.0, 4곡 0.6 (임시).
        public static double MusicBaseVolume(MusicTrack track);
        // SwordSwing, AttackHit, EnemyDefeated 0.6, 나머지 1.0 (임시).
        public static double SfxBaseVolume(SfxId id);
        // 14종 모두 true (임시).
        public static bool IsSfxEnabled(SfxId id);
    }

    public static class MusicRules
    {
        // isStartScreenShowing 이면 Title. 아니고 hasBoss 면 displayStageNumber == StageLayout.FinalDisplayStage(5) 일 때 BattleFinalBoss,
        // 아니면 BattleBoss. 나머지 BattleNormal.
        // displayStageNumber 가 1~5 밖이면 ArgumentOutOfRangeException (isStartScreenShowing 이어도).
        public static MusicTrack TrackFor(bool isStartScreenShowing, int displayStageNumber, bool hasBoss);
    }

    // 두 곡의 교차 페이드 상태. 재생은 하지 않는다.
    public sealed class MusicFader
    {
        // crossfadeSeconds 가 0 이하·NaN·무한대면 ArgumentOutOfRangeException.
        // 처음: Current None, Previous None, 두 비율 0.
        public MusicFader(double crossfadeSeconds = AudioCatalog.CrossfadeSeconds);

        public MusicTrack Current { get; }     // 커지고 있거나 다 커진 곡
        public double CurrentWeight { get; }   // 0~1. Current 가 None 이면 0
        public MusicTrack Previous { get; }    // 줄어들고 있는 곡. 없으면 None
        public double PreviousWeight { get; }  // 0~1. Previous 가 None 이면 0

        // track == Current 면 false, 아무 일 없음.
        // 다르면: Current 가 None 이 아닐 때 Previous = Current, PreviousWeight = CurrentWeight (그전 Previous 는 버린다).
        // Current 가 None 이면 Previous 는 그대로. 그다음 Current = track, CurrentWeight = 0, true.
        // true 를 받으면 화면은 새 곡을 처음부터 튼다. 정의되지 않은 값은 ArgumentOutOfRangeException.
        public bool Request(MusicTrack track);

        // CurrentWeight 를 deltaSeconds / crossfadeSeconds 만큼 올려 1 에서 멈추고(Current 가 None 이면 0 그대로),
        // PreviousWeight 를 같은 만큼 내려 0 이 되면 Previous = None.
        // 음수·NaN·무한대: ArgumentOutOfRangeException (바뀌지 않음). 0: 아무 일 없음.
        public void Tick(double deltaSeconds);
    }

    public static class AudioMix
    {
        // MusicBaseVolume(track) × clamp01(bgmScale) × clamp01(weight). NaN 은 0 으로 본다. None 은 0.
        public static double MusicVolume(MusicTrack track, double bgmScale, double weight);
        // SfxBaseVolume(id) × clamp01(sfxScale). NaN 은 0.
        public static double SfxVolume(SfxId id, double sfxScale);
        // sfxScale > 0. NaN 은 false.
        public static bool ShouldPlaySfx(double sfxScale);
        // applicationPaused || !hasFocus.
        public static bool ShouldPauseAll(bool applicationPaused, bool hasFocus);
    }

    // 한 프레임의 효과음 요청을 모아 규칙("동작"의 Drain 순서)대로 꺼낸다.
    public sealed class SfxQueue
    {
        // minIntervalSeconds 가 음수·NaN·무한대면 ArgumentOutOfRangeException. 0 은 간격 없음.
        // isEnabled 가 null 이면 AudioCatalog.IsSfxEnabled 를 쓴다 (테스트에서 꺼진 소리를 확인할 때 넘긴다).
        public SfxQueue(double minIntervalSeconds = AudioCatalog.SfxMinIntervalSeconds,
            System.Func<SfxId, bool> isEnabled = null);

        public bool HasPending { get; }   // 꺼내지 않은 요청이 있으면 true

        // 이번 프레임 요청에 더한다. 같은 id 는 하나로 본다. 정의되지 않은 값은 ArgumentOutOfRangeException.
        public void Request(SfxId id);

        // 합치기 → 스킬이 대신 → 버튼음 빼기 → 꺼진 소리 빼기 → 간격 → 번호 순으로 돌려주고 요청을 비운다.
        // 요청이 없으면 빈 목록 (null 아님). nowSeconds 가 NaN·무한대면 ArgumentOutOfRangeException (요청 그대로).
        public System.Collections.Generic.IReadOnlyList<SfxId> Drain(double nowSeconds);
    }

    // 게임 알림을 구독해 SfxQueue 에 요청한다. 무엇에 무엇을 요청하는지는 "동작"의 표.
    public sealed class SfxTriggers : System.IDisposable
    {
        // 인자 중 null 이 있으면 ArgumentNullException (아무것도 구독하지 않는다).
        // battle 의 EnemyDamaged, SkillUsed, EnemyDefeated, EnemyAdded, stage 의 StageCleared, StageFailed, Changed,
        // upgrades 의 Upgraded, realm 의 BrokeThrough 를 구독한다. 직전 Phase 는 stage.Phase 에서 시작한다.
        public SfxTriggers(SfxQueue queue,
            PlumSword.Core.Combat.Battle battle,
            PlumSword.Core.Stage.StageProgress stage,
            PlumSword.Core.StatUpgrade.StatUpgrades upgrades,
            PlumSword.Core.Realm.RealmProgress realm);

        // 구독을 모두 끊는다. 두 번째부터는 아무 일 없음.
        public void Dispose();
    }

    // 창과 버튼의 상태 변화를 SfxQueue 에 요청한다. 화면이 매 프레임 상태를 넘긴다.
    public sealed class UiSoundWatch
    {
        // queue 가 null 이면 ArgumentNullException. 처음 상태: 네 값 모두 false, 기억한 누름 없음.
        public UiSoundWatch(SfxQueue queue);

        // settingsOpen 이 지난번과 다르면 WindowOpenClose. rewardWindowShowing, errorWindowShowing 이 false → true 면 WindowOpenClose
        // (true → false 는 없음). rewardPaid 가 false → true 면 OfflineReward. 그다음 네 값을 기억한다.
        public void UpdateWindows(bool settingsOpen, bool rewardWindowShowing, bool errorWindowShowing, bool rewardPaid);

        // 누르기 시작. onButton 이 true 면 interactable 에 따라 UiConfirm / UpgradeUnavailable 을 기억한다(요청은 아직 안 함).
        // false 면 기억을 지운다. 이전 기억은 덮어쓴다.
        public void PressStarted(bool onButton, bool interactable);

        // 기억한 소리를 요청하고 기억을 지운다. 기억이 없으면 아무 일 없음.
        public void PressEnded();
    }

    // 효과음 재생기 고르기.
    public sealed class SfxVoicePool
    {
        // count < 1 이면 ArgumentOutOfRangeException. 처음엔 모두 비어 있다.
        public SfxVoicePool(int count = AudioCatalog.MaxSfxVoices);

        public int Count { get; }

        // 끝난 재생기(끝 시각 <= nowSeconds) 중 번호가 가장 작은 것, 없으면 시작 시각이 가장 이른 것(같으면 번호가 작은 것)을
        // 골라 시작 = nowSeconds, 끝 = nowSeconds + lengthSeconds 로 적고 번호(0 ~ Count-1)를 돌려준다.
        // nowSeconds 가 NaN·무한대이거나 lengthSeconds 가 음수·NaN·무한대면 ArgumentOutOfRangeException (바뀌지 않음).
        public int Acquire(double nowSeconds, double lengthSeconds);
    }
}
```

```csharp
namespace PlumSword.Game.Audio
{
    // Resources 에서 소리 파일을 읽고 기억한다.
    public sealed class AudioClipCache
    {
        // 경로마다 Resources.Load<AudioClip> 를 한 번만 부른다. null 이면 처음 한 번만
        // Debug.LogWarning("[Audio] 소리 파일을 찾지 못했습니다: " + path) 를 남기고 null. 다시 부르면 기억한 값(로그 없음).
        // path 가 null 이면 null (로그 없음).
        public UnityEngine.AudioClip Load(string path);
    }

    public sealed class AudioView : UnityEngine.MonoBehaviour
    {
        // 곡 재생기 2개(loop, playOnAwake false)와 효과음 재생기 MaxSfxVoices 개(playOnAwake false)를 이 오브젝트에 만들고,
        // SfxQueue, SfxTriggers(root.Battle, root.StageProgress, root.StatUpgrades, root.Realm), UiSoundWatch, MusicFader,
        // SfxVoicePool, AudioClipCache 를 만든다. root.Volume.Changed 를 구독한다. AudioListener.pause = false.
        // root 가 null 이면 ArgumentNullException. 두 번 부르면 InvalidOperationException.
        public void Initialize(PlumSword.Game.GameRoot root);

        public PlumSword.Core.Audio.MusicTrack CurrentTrack { get; }            // MusicFader.Current
        public UnityEngine.AudioSource CurrentMusicSource { get; }              // Current 를 트는 곡 재생기
        public System.Collections.Generic.IReadOnlyList<UnityEngine.AudioSource> SfxSources { get; }   // 번호 = SfxVoicePool 번호

        // LateUpdate: (1) 곡: TrackFor → Request(true 면 쓰지 않던 재생기에 새 곡 clip 을 넣어 처음부터 Play. 버린 곡은 Stop)
        //   → Tick(Time.unscaledDeltaTime) → 두 재생기 음량을 MusicVolume 으로. Previous 가 None 이 된 재생기는 Stop.
        // (2) 포인터 누름·뗌 → UiSoundWatch. (3) UpdateWindows. (4) Drain(Time.unscaledTimeAsDouble) →
        //   ShouldPlaySfx 이면 효과음마다 Load → null 이면 건너뜀 → Acquire(now, clip.length) → 그 재생기 Stop, clip, 음량, Play.
        // OnApplicationPause / OnApplicationFocus: AudioListener.pause = ShouldPauseAll(paused, focused).
        // OnDestroy: SfxTriggers.Dispose, Volume.Changed 구독 해제.
    }
}

namespace PlumSword.Game
{
    public sealed class GameRoot : UnityEngine.MonoBehaviour   // 이미 있는 클래스에 아래 하나를 더한다
    {
        // Awake 에서 설정 창(Settings.Initialize) 바로 다음, 시작 화면을 띄우기 전에 [Game] 의 자식 [AudioView] 를 만들어
        // Initialize(this) 한다.
        public PlumSword.Game.Audio.AudioView Audio { get; }   // private set
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot.Awake`가 `[AudioView]`를 하나 만들어 `GameRoot.Audio`로 들고 있는다. `AudioView`가 Core 소리 객체를 모두 만들고 들고 있는다. 다른 기능은 소리 기능을 부르지 않는다 (소리 기능이 다른 기능의 알림과 상태를 읽기만 한다).
- 다른 기능의 화면 코드(`StatUpgradeView`, `SettingsView`, `StartScreenView`, `OfflineRewardView`, `SaveErrorView`)는 고치지 않는다.

### 필요한 것

모두 구현되어 있다 (고정 integration `d1a4cea`).

- `PlumSword.Core.Combat.Battle` (`docs/spec/combat.md`): `EnemyDamaged`, `SkillUsed`, `EnemyDefeated`, `EnemyAdded`, `IsDiscipleDefeated`, `Enemy.Kind`, `EnemyKind`.
- `PlumSword.Core.Stage.StageProgress` (`docs/spec/stage.md`): `StageCleared`, `StageFailed`, `Changed`, `Phase`, `Boss`, `DisplayStageNumber`, `StagePhase.FadingOut`, `StageLayout.FinalDisplayStage`.
- `PlumSword.Core.StatUpgrade.StatUpgrades.Upgraded` (`docs/spec/stat_upgrade.md`).
- `PlumSword.Core.Realm.RealmProgress.BrokeThrough` (`docs/spec/realm.md`).
- `PlumSword.Core.StartMenu.VolumeSettings` (`docs/spec/start_menu.md`): `BgmScale`, `SfxScale`, `Changed`.
- `PlumSword.Core.OfflineReward.OfflineRewardClaim.HasPaid` (`docs/spec/offline_reward.md`).
- `PlumSword.Game.GameRoot`: `Battle`, `StageProgress`, `StatUpgrades`, `Realm`, `Volume`, `Settings`(`SettingsView.IsOpen`), `IsStartScreenShowing`, `OfflineRewardClaim`. `[Game]` 자식의 `PlumSword.Game.OfflineReward.OfflineRewardView`, `PlumSword.Game.Save.SaveErrorView`.
- 씬 카메라의 `AudioListener` (이미 있다). Input System의 `Pointer`, uGUI의 `EventSystem`·`Button`.

## 화면

해당 없음. 새로 보이는 것이 없다. 음량 슬라이더는 설정 창에 이미 있다 (`docs/spec/start_menu.md`).

## 저장

해당 없음. 저장할 값이 없다. 음량은 `VolumePrefs`가 이미 저장하고, 곡의 재생 위치는 저장하지 않는다 (켤 때마다 곡이 처음부터).

- 오프라인 보상: 영향 없음.

## 범위 밖

- 효과음 교체·재생성. 사람이 하나씩 들어 본 뒤 빼거나 배율을 바꾸는 것은 `AudioCatalog` 표를 고치는 별도 작업이다 (파일을 바꾸면 `docs/dev/audio.md`도).
- 진동, 3D·위치 소리(모두 2D, `spatialBlend` 0), 곡 고르기 메뉴, 음소거 버튼.
- 파일이 없는 소리: 제자 피격, 몬스터·보스 공격, 보스 투사체, 금화 획득, 시작 화면 꽃잎.
- 테마(스테이지)마다 다른 곡, 회차마다 다른 곡.
- 곡의 반복 이음새 다듬기, 파일 편집, 오디오 가져오기 설정(`.meta`)과 프로젝트 오디오 설정 변경, AudioMixer 에셋.
- 미리 불러오기·메모리 최적화. 처음 필요할 때 불러온다.
- 보스 등장 카메라·애니메이션 연출 (4단계 후보).

## 완료 조건 초안

에딧 모드 (`PlumSword.Core.Tests.Audio`):

1. `AudioCatalog.MusicPath`: `Title` → `"Audio/Music/title"`, `BattleNormal` → `"Audio/Music/battle_normal"`, `BattleBoss` → `"Audio/Music/battle_boss"`, `BattleFinalBoss` → `"Audio/Music/battle_final_boss"`, `None` → `null`. `(MusicTrack)99` → `ArgumentOutOfRangeException`.
2. `AudioCatalog.SfxPath`가 14종 모두 "소리 목록" 표의 경로를 돌려준다 (예: `SfxId.StageTransition` → `"Audio/SFX/stage_transition"`). `(SfxId)99` → `ArgumentOutOfRangeException`.
3. 곡 4개와 효과음 14개의 경로마다 프로젝트 폴더 기준 `Assets/Resources/<경로>.wav` 파일이 있다 (`System.IO.File.Exists`).
4. `MusicBaseVolume`: 4곡 0.6, `None` 0.0. `SfxBaseVolume`: `SwordSwing`·`AttackHit`·`EnemyDefeated` 0.6, 나머지 11종 1.0. `IsSfxEnabled`: 14종 모두 `true`, `(SfxId)99` → `ArgumentOutOfRangeException`.
5. `MusicRules.TrackFor`: `(true, 3, true)` → `Title`, `(false, 1, false)` → `BattleNormal`, `(false, 4, true)` → `BattleBoss`, `(false, 5, true)` → `BattleFinalBoss`, `(false, 5, false)` → `BattleNormal`. `(false, 0, false)`, `(true, 6, false)` → `ArgumentOutOfRangeException`.
6. `new MusicFader()`: `Current`·`Previous` `None`, 두 비율 0. `new MusicFader(0)`, `(-1)`, `(double.NaN)` → `ArgumentOutOfRangeException`.
7. `Request(Title)` → `true`, `Current` `Title`, `CurrentWeight` 0. `Tick(0.5)` → 0.5. `Tick(0.6)` → 1.0. 다시 `Request(Title)` → `false`, `CurrentWeight` 1.0 그대로.
8. 이어서 `Request(BattleNormal)` → `true`, `Previous` `Title` 1.0, `Current` `BattleNormal` 0.0. `Tick(0.25)` → `CurrentWeight` 0.25, `PreviousWeight` 0.75.
9. 이어서 `Request(BattleBoss)` → `Previous` `BattleNormal` 0.25 (`Title` 버림), `Current` `BattleBoss` 0.0. `Tick(0.25)` → `CurrentWeight` 0.25, `Previous` `None`, `PreviousWeight` 0.
10. `Title`이 1.0일 때 `Request(None)` → `true`, `Previous` `Title` 1.0, `Current` `None` 0. `Tick(1.0)` → `Previous` `None`, `CurrentWeight` 0 그대로.
11. `MusicFader.Tick(-0.1)`, `Tick(double.NaN)`, `Tick(double.PositiveInfinity)` → `ArgumentOutOfRangeException`, 비율 그대로. `Tick(0)` → 그대로. `Request((MusicTrack)99)` → `ArgumentOutOfRangeException`.
12. `AudioMix.MusicVolume`: `(BattleNormal, 0.8, 1.0)` → 0.48, `(BattleBoss, 0.8, 0.25)` → 0.12, `(Title, 0.0, 1.0)` → 0, `(None, 1.0, 1.0)` → 0, `(Title, 1.5, 2.0)` → 0.6, `(Title, double.NaN, 1.0)` → 0 (오차 1e-9).
13. `AudioMix.SfxVolume`: `(AttackHit, 0.8)` → 0.48, `(BossAppear, 0.8)` → 0.8, `(UiConfirm, 0.0)` → 0. `ShouldPlaySfx(0.0)` → `false`, `(0.01)` → `true`, `(double.NaN)` → `false`.
14. `AudioMix.ShouldPauseAll`: `(false, true)` → `false`, `(true, true)`, `(false, false)`, `(true, false)` → `true`.
15. `SfxQueue`: `Request(EnemyDefeated)` 세 번 → `Drain(1.0)` → `[EnemyDefeated]`. 요청 없이 `Drain(1.01)` → 빈 목록, `HasPending` `false`.
16. 간격: 15에 이어 `Request(EnemyDefeated)` → `Drain(1.04)` → 빈 목록. 다시 `Request(EnemyDefeated)` → `Drain(1.06)` → `[EnemyDefeated]`. `Drain(1.04)` 때 함께 요청한 `AttackHit`은 나온다.
17. 스킬: `SwordSwing`, `AttackHit`, `PlumSwordSkill` 요청 → `[PlumSwordSkill]`. `SwordSwing`, `AttackHit`만 → `[SwordSwing, AttackHit]`.
18. 버튼음: `UiConfirm`+`UpgradeSuccess` → `[UpgradeSuccess]`, `UpgradeUnavailable`+`UpgradeSuccess` → `[UpgradeSuccess]`, `UiConfirm`+`WindowOpenClose` → `[WindowOpenClose]`, `UiConfirm`+`OfflineReward` → `[OfflineReward]`, `UiConfirm`만 → `[UiConfirm]`.
19. 순서: `BossDefeated`, `AttackHit` 순으로 요청 → `[AttackHit, BossDefeated]`.
20. 꺼진 소리: `new SfxQueue(0.05, id => id != SfxId.AttackHit)`에 `AttackHit`, `SwordSwing` 요청 → `[SwordSwing]`.
21. `Request((SfxId)99)`, `Drain(double.NaN)`, `new SfxQueue(-0.01)` → `ArgumentOutOfRangeException`. `Drain(double.NaN)` 뒤에도 요청은 남아 `HasPending` `true`. `new SfxQueue(0)`이면 같은 `now`로 두 번 꺼내도 두 번 다 나온다.
22. 시간이 거꾸로: `Drain(5.0)`으로 `UiConfirm`을 꺼낸 뒤 `Request(UiConfirm)` → `Drain(4.0)` → `[UiConfirm]`.
23. `SfxTriggers` 생성자의 다섯 인자 중 하나라도 `null` → `ArgumentNullException`.
24. `Battle.AddEnemy(EnemyKind.Boss, 1, 좌표)` → `Drain` → `[BossAppear]`. `AddEnemy(EnemyKind.Normal, 1, 좌표)` → 빈 목록.
25. 스킬이 꺼진 `Battle`(`skillEnabled: false`)에 일반 적 하나를 제자 앞 0.5에 두고 첫 `EnemyDamaged`까지 `Step` → `Drain` → `[SwordSwing, AttackHit]`.
26. 스킬이 켜진 `Battle`에 일반 적 하나를 제자 앞 0.5에 두고 첫 `SkillUsed`까지 `Step` → `Drain` 결과에 `PlumSwordSkill`이 있고 `SwordSwing`, `AttackHit`이 없다.
27. 일반 적이 쓰러진 칸 → `Drain`에 `EnemyDefeated`. 보스가 쓰러진 칸 → `EnemyDefeated` 없음.
28. `StageProgress`로 1스테이지 1구간의 무리를 모두 쓰러뜨려 `Phase`가 `FadingOut`이 된 프레임 → `Drain`에 `StageTransition`과 `EnemyDefeated`.
29. 보스를 혼자 이긴 프레임 → `Drain`에 `BossDefeated`, `StageTransition` 없음. 제자가 쓰러진 프레임(일반 구간과 보스 구간 각각) → `PlayerDefeated`, `StageTransition` 없음. 보스와 제자가 같은 칸에 쓰러지면 `BossDefeated` 없음, `PlayerDefeated` 있음. (보스를 이기게 하려면 `StatUpgrades`를 충분히 강하게 만든다.)
30. `StatUpgrades.TryUpgrade` 성공 → `[UpgradeSuccess]`. 실패(금화 부족) → 빈 목록. `RealmProgress`가 돌파하면 `RealmBreakthrough`.
31. `Dispose` 뒤에는 24, 30의 알림이 와도 요청이 없다. `Dispose` 두 번 → 예외 없음.
32. `new UiSoundWatch(null)` → `ArgumentNullException`.
33. `UpdateWindows(true, false, false, false)` → `[WindowOpenClose]`. 같은 값으로 다시 → 빈 목록. `(false, false, false, false)` → `[WindowOpenClose]`.
34. `rewardWindowShowing` `false` → `true` → `[WindowOpenClose]`, `true` → `false` → 빈 목록. `errorWindowShowing` `false` → `true` → `[WindowOpenClose]`. 처음 호출이 `(false, true, false, false)`여도 `[WindowOpenClose]`.
35. `rewardPaid` `false` → `true` → `[OfflineReward]`, 계속 `true` → 빈 목록. 같은 프레임에 보상 창이 사라져도(`(false, false, false, true)`) `[OfflineReward]`.
36. `PressStarted(true, true)` → `Drain` → 빈 목록. `PressEnded()` → `Drain` → `[UiConfirm]`. `PressStarted(true, false)`, `PressEnded()` → `[UpgradeUnavailable]`. `PressStarted(false, true)`, `PressEnded()` → 빈 목록. 기억 없이 `PressEnded()` → 빈 목록.
37. 같은 프레임에 `PressStarted(true, true)`, `PressEnded()`, `UpdateWindows(true, false, false, false)` → `[WindowOpenClose]`.
38. `new SfxVoicePool(0)` → `ArgumentOutOfRangeException`. `new SfxVoicePool().Count` → 8.
39. `new SfxVoicePool(2)`: `Acquire(0, 1)` → 0, `Acquire(0.1, 1)` → 1, `Acquire(0.2, 1)` → 0, `Acquire(0.3, 1)` → 1, `Acquire(2.0, 1)` → 0.
40. 8개 풀에서 `Acquire(0, 1)` 여덟 번 → 0~7, 아홉 번째 `Acquire(0.5, 1)` → 0.
41. `Acquire(double.NaN, 1)`, `Acquire(0, -1)`, `Acquire(0, double.PositiveInfinity)` → `ArgumentOutOfRangeException`.

플레이 모드 (`PlumSword.Game.Tests.Audio`, 배치 모드라 시작 화면 없이 새 판 1스테이지로 시작):

42. 부팅 뒤 `[Game]`의 자식 `[AudioView]`가 하나 있고 `GameRoot.Audio`가 그것이다. 몇 프레임 뒤 `CurrentTrack`이 `BattleNormal`, `CurrentMusicSource.clip.name`이 `"battle_normal"`, `loop`가 `true`.
43. `GameRoot.Battle.AddEnemy(EnemyKind.Boss, 1, 제자 좌표 + 10)` → 다음 프레임에 `SfxSources` 중 하나의 `clip.name`이 `"boss_appear"`.
44. 부팅 뒤 1.5초 이상(실제 시간) 기다린 뒤 `Volume.SetBgm(0)` → 다음 프레임 `CurrentMusicSource.volume` 0 (오차 1e-3)이고 `clip`은 그대로. `SetBgm(100)` → 다음 프레임 0.6.
45. `new AudioClipCache().Load("Audio/SFX/__missing__")`를 두 번 → 두 번 다 `null`, 경고 로그가 정확히 한 번 (`LogAssert`). `Load(null)` → `null`, 로그 없음.
46. 기존 `GameBootTests`가 오류 로그 없이 통과한다.

## 결정 기록

결정 기록은 [records/audio.md](records/audio.md)의 "결정 기록"에 있다. 구현에 필요한 규칙은 위 절들에 모두 있다.
