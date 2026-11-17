# 오프라인 보상 사양

- 기능 이름: offline_reward (네임스페이스 `PlumSword.Core.OfflineReward`)
- 작성일: 2026-11-17

## 목표

게임이 꺼져 있던 시간에 비례해 금화를 준다. 켤 때 마지막 저장 시각과 지금 시각으로 보상을 계산하고, 보상이 있으면 전투를 시작하기 전에 보상 창을 띄워 `CLAIM`을 누르면 지급한다. 지급과 새 저장 시각은 파일 하나에 함께 저장해 같은 시간분을 두 번 받지 않게 한다. 앱이 백그라운드에 10초 이상 있다가 돌아오면 게임을 다시 켠 것처럼 저장 파일로 새로 시작해서 같은 흐름을 탄다. 계산과 지급 순서는 Core가, 지금 시각을 읽는 일, 창, 다시 만드는 일은 Game이 맡는다.

출처: `docs/design/milestone_2.md` "추가하거나 변경할 기능 → 오프라인 보상", "시작 화면과 기본 메뉴"(시작 화면에 머문 시간 제외, 이어하기 뒤 보상 창), "마일스톤 달성 기준"(오프라인 금화 표시·지급, 중복 지급 없음), "미결 사항과 결정 기록"(오프라인 보상 저장의 원자적 처리). 연결은 `docs/spec/save.md`, `docs/spec/stage.md`, `docs/spec/currency.md`의 "연결 지점" 절.

## 동작

- **보상 계산** `OfflineRewardCalculator.Calculate(savedAtUtcTicks, nowUtcTicks, stageNumber)`: 아래 순서로 계산해 `OfflineRewardResult`를 돌려준다. 아무것도 바꾸지 않는다.
  1. `stageNumber`가 1보다 작으면 `ArgumentOutOfRangeException` (시각과 상관없이 먼저 검사).
  2. 경과 시간 `ElapsedTicks`: 두 시각 중 하나라도 음수이거나 `nowUtcTicks < savedAtUtcTicks`(시계를 되돌림)이면 0. 아니면 `nowUtcTicks − savedAtUtcTicks`.
  3. 적용 시간 `AppliedTicks`: `Math.Min(ElapsedTicks, MaxTicks)`를 10초 단위로 내린 값.
  4. 처치 수 `KillCount` = `AppliedTicks / IntervalTicks`. 금화 `Gold` = `KillCount × StageRewards.NormalGold(stageNumber)`.
  5. `HasReward` = `Gold > 0`. `IsCapped` = `ElapsedTicks > MaxTicks`.
- **켤 때** (`GameRoot.Awake`): 저장 사양의 순서에 아래를 끼운다 ("연결 지점"의 "`GameRoot.Awake` 순서").
  - 저장을 불러온 결과가 `Loaded`이면, `StageProgress`를 만든 뒤 `DateTime.UtcNow.Ticks`를 한 번 읽어 `OfflineReward = Calculate(LoadResult.Data.SavedAtUtcTicks, 지금, StageProgress.StageNumber)`로 계산한다. `Loaded`가 아니면(`Empty`, `UnsupportedVersion`, `Corrupt`) 계산하지 않고 `OfflineReward`는 `null`이다.
  - 보상이 있으면(`OfflineReward.HasReward`) `AutoSave`를 **꺼진 채로** 만들고, `Battle.SetPaused(true)`를 하고, `Begin()`을 부르지 않고, 보상 창을 띄운다. 창이 떠 있는 동안 파일은 쓰지 않는다. 그래서 `CLAIM`을 누르기 전에 꺼지면 파일은 그대로이고, 다음에 켤 때 옛 저장 시각부터 다시 계산한다 (창을 띄워 둔 시간도 들어가고, 8시간 상한은 그대로).
  - 보상이 없으면(10초 미만, 시계를 되돌림, 저장 없음 등) 창 없이 저장 사양대로 `Begin()`을 부른다. `Begin`의 `StageStarted`로 그 프레임 끝에 지금 시각이 저장되어 보상 기준 시각이 갱신된다.
- **받기** `OfflineRewardClaim.TryClaim(nowUtcTicks)`:
  1. 이미 끝났으면(`Done`) 아무것도 하지 않고 `true`.
  2. 아직 주지 않았으면(`HasPaid == false`) `wallet.Add(Reward.Gold)`, `HasPaid = true`, `autoSave.Enable()`. 한 번 준 뒤로는 다시 주지 않는다.
  3. `autoSave.SaveNow(nowUtcTicks)`. 금화와 새 저장 시각이 파일 하나에 함께 쓰인다.
  4. 성공하면 `State = Done`, `true`. 실패하면 `State = SaveFailed`, `false`.
  - 그래서 같은 시간분을 두 번 받지 않는다. 저장이 성공하면 파일의 금화와 시각이 함께 바뀐다. 실패한 채 끄면 파일은 옛 금화와 옛 시각 그대로이고, 메모리에서 준 금화는 사라진다.
- **창을 닫고 시작**: `TryClaim`이 `true`를 돌려주면 창이 `onClaimed`를 한 번 부르고 스스로 없어진다. `onClaimed`는 `Battle.SetPaused(false)` → `StageProgress.Begin()`이다.
- **저장 실패**: 창에 남는다. 실패 문구를 보이고 버튼 글자를 `RETRY`로 바꾼다. 다시 누르면 `TryClaim`이 `SaveNow`만 다시 부른다. 그사이 `AutoSave`의 3초 자동 재시도도 돈다 (켜졌고 `IsDirty`이므로). 성공할 때까지 전투는 시작하지 않는다.
- **백그라운드 복귀** (`OfflineResumeWatcher`, 배치 모드가 아닐 때만 있다):
  - `OnApplicationPause(true)`: 그때의 `DateTime.UtcNow.Ticks`를 떠난 시각으로 기억한다 (저장은 `SaveView`의 `SaveNow`가 한다).
  - `OnApplicationPause(false)`: 떠난 시각이 기억되어 있으면, 저장이 최신인지(`!autoSave.HasFailed && !autoSave.IsDirty`)를 보고 `OfflineRewardCalculator.ShouldRestartOnResume(떠난 시각, 지금, 최신)`이 `true`이면 `GameRoot.Restart()`를 부른다. 어느 쪽이든 떠난 시각을 지운다. 떠난 시각이 없으면 아무것도 하지 않는다.
  - `ShouldRestartOnResume`: 저장이 최신이고, 경과 시간(`Calculate`의 2번과 같은 규칙)이 `IntervalTicks` 이상이면 `true`.
  - 10초 미만이면 다시 만들지 않고 그대로 계속한다. 시계를 되돌렸을 때도 같다.
  - 백그라운드로 갈 때 저장이 실패했으면(파일이 최신이 아님) 다시 만들지 않고 그대로 계속한다. 보상은 없다.
- **다시 만들기** `GameRoot.Restart()`: 다음 `[Game]`이 백그라운드 복귀로 만들어졌다고 표시해 두고(`IsResumeRestart`), 지금 루트를 끈 뒤(`SetActive(false)`) 지운다(`Destroy`). 그리고 `[GameRestarter]` 오브젝트(`DontDestroyOnLoad`)를 만든다. `GameRestarter`는 다음 프레임의 `Update`에서 `GameBootstrap`과 같은 방법으로 새 `[Game]`을 만든 뒤 자기를 지운다 (이름 `GameBootstrap.RootName`, `DontDestroyOnLoad`, `AddComponent<GameRoot>()`). 새 `GameRoot.Awake`가 파일을 다시 읽어 켤 때와 똑같이 한다: 저장된 스테이지의 처음, 체력 전부 회복, 보상이 있으면 보상 창.
  - 보상 창이나 손상 창이 떠 있는 채로 백그라운드에 갔다 오면: `AutoSave`가 꺼져 있어 `IsDirty`가 `false`이므로 최신으로 본다. 10초 이상이면 다시 만들어 같은 창이 다시 뜬다 (보상은 그만큼 늘어난다).
  - `CLAIM` 뒤 저장 실패로 창이 남은 채로 갔다 오면: `HasFailed`가 `true`라 다시 만들지 않는다.
  - 페이드 중에 백그라운드로 가면: 저장 사양과 같다. 저장된 번호(끝난 스테이지)부터 다시 한다.
- **시작 화면에 머문 시간**: 보상은 켤 때(`Awake`) 한 번 계산한 `OfflineReward`를 쓴다. `CLAIM`을 누를 때 다시 계산하지 않는다. 그래서 켠 뒤 창이나 시작 화면에 머문 시간은 이번 보상에 들어가지 않는다. 받은 뒤의 저장 시각은 누른 시각이다. 같은 실행 안에서 시작 화면에 갔다가 돌아와도 다시 계산하지 않는다 (시작 화면 기능이 따를 규칙. "연결 지점").

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| 경과 10초 미만 (0 ~ 9.9999999초) | 보상 없음. 창 없이 `Begin` | `HasReward` false. 그 프레임 끝 저장으로 시각 갱신 |
| 10초로 나누어떨어지지 않음 (예: 25초) | 나머지는 버림 | 20초분, 2마리 |
| 정확히 10초 | 1마리분 | 1스테이지면 금화 10 |
| 정확히 8시간 | 상한 아님 | `IsCapped` false, 2,880마리 |
| 8시간 초과 (예: 9시간) | 8시간분만 | `IsCapped` true, 창에 실제·적용 시간 둘 다 |
| 지금 < 저장 시각 (시계를 되돌림) | 경과 0, 보상 없음, 창 없음 | 첫 저장에서 저장 시각이 지금으로 바뀜 |
| 저장 시각이나 지금 시각이 음수 | 경과 0, 보상 없음 | 예외 없음 |
| 저장 시각이 아주 오래전 (예: 0) | 8시간 상한 | `IsCapped` true |
| `stageNumber`가 1보다 작음 | `ArgumentOutOfRangeException` (경과가 0이어도) | |
| `stageNumber`가 `int.MaxValue` | 넘침 없음 | 9시간이면 금화 12,369,505,829,760 |
| 저장 없음, 판이 다름 (`Empty`, `UnsupportedVersion`) | 계산하지 않음 | `OfflineReward` null, 창 없음 |
| 손상 (`Corrupt`) | 계산하지 않음. 저장 사양의 손상 창 | `OfflineReward` null |
| `CLAIM`을 누르기 전에 끔 | 아무것도 쓰지 않음 | 다음에 켤 때 옛 저장 시각부터 다시 계산 |
| `CLAIM`을 두 번 (성공 뒤) | 두 번째는 아무 일 없음, `true` | 금화 한 번만, 쓰기 한 번만 |
| `CLAIM` 뒤 저장 실패 | `false`, `SaveFailed`. 창에 남아 `RETRY` | 금화는 이미 더해짐. 다시 누르면 `SaveNow`만, 금화 그대로 |
| 저장 실패 뒤 다시 실패 | `false`, `SaveFailed` 그대로 | 금화 그대로 |
| 저장 실패 뒤 성공 | `true`, `Done`, 창을 닫고 전투 시작 | 파일에 보상이 더해진 금화와 그때 시각 |
| 저장 실패 상태에서 끔 | `SaveView`의 `SaveNow`가 한 번 더 시도 | 성공하면 보상 반영. 실패하면 파일 그대로라 다음에 다시 받음 (메모리 금화는 사라져 중복 없음) |
| 저장 성공 직후 끄고 바로 켬 | 경과 10초 미만이면 보상 없음 | 같은 시간분을 다시 받지 않음 |
| `OfflineRewardClaim` 생성자 인자 중 `null` | `ArgumentNullException` | |
| `OfflineRewardClaim`에 `HasReward`가 `false`인 결과 | `ArgumentException` | |
| 백그라운드 10초 미만 뒤 복귀 | 다시 만들지 않고 계속 | 전투·처치 수 그대로 |
| 백그라운드 10초 이상, 저장 최신 | `GameRoot.Restart()` | 저장된 스테이지 처음부터, 보상 창 |
| 백그라운드로 갈 때 저장 실패 (`HasFailed` 또는 `IsDirty`) | 다시 만들지 않고 계속 | 보상 없음 |
| 백그라운드 중 시계를 되돌림 | 다시 만들지 않고 계속 | |
| 보상 창·손상 창이 뜬 채 백그라운드 10초 이상 | 다시 만들어 같은 창 | 보상 다시 계산 (늘어남) |
| `CLAIM` 저장 실패 창이 뜬 채 백그라운드 | 다시 만들지 않음 (`HasFailed`) | 창 그대로 |
| 떠난 알림 없이 돌아온 알림만 옴 | 무시 | |
| 배치 모드 | 복귀 감시를 만들지 않음 | |
| `OfflineRewardText.FormatDuration`에 음수 | `ArgumentOutOfRangeException` | |

`GameRoot.Restart()`, `OfflineResumeWatcher`, 창의 실제 동작은 자동 테스트 대상이 아니다 (배치 모드 시작 상태만 플레이 모드 테스트로 확인한다). 판단(`ShouldRestartOnResume`, `TryClaim`, 표시 글자)은 Core에 있고 에딧 모드 테스트로 판정한다.

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 시각의 단위 | UTC `DateTime.Ticks` (`long`), 1초 = 10,000,000틱 | Game이 `DateTime.UtcNow.Ticks`를 넘긴다. Core는 시각을 읽지 않는다 |
| 보상 간격 `OfflineRewardCalculator.IntervalTicks` | 100,000,000 (10초) (임시) | `long`. 일반 몬스터 한 마리분 |
| 최대 인정 시간 `OfflineRewardCalculator.MaxTicks` | 288,000,000,000 (8시간) (임시) | `long`. 간격의 배수 |
| 적용 시간 | `min(경과, MaxTicks) − min(경과, MaxTicks) % IntervalTicks` | 버림 |
| 처치 수 | `적용 시간 / IntervalTicks` | `long`. 최대 2,880 |
| 금화 | `처치 수 × StageRewards.NormalGold(stageNumber)` | `long`. 반올림 없음. 최대 2,880 × 4,294,967,302 = 12,369,505,829,760 이라 넘치지 않는다 |
| 한 마리분 금화 | `10 + 2 × (N − 1)` (임시, 스테이지 기능의 수치) | `N`은 전체 진행 번호. 1·3·6 → 10·14·20 |
| 예 | 1스테이지 10분 → 600, 25초 → 20, 9시간 → 28,800, 3스테이지 10분 → 840, 6번(2회차 1스테이지) 10분 → 1,200 | |
| 복귀 시 다시 만드는 기준 | 경과 `IntervalTicks` 이상 | 보상 간격과 같다 |

**표시 글자** (`OfflineRewardText`, 한글 글꼴 전이라 영문):

| 이름 | 값 | 예 |
| --- | --- | --- |
| `Title` | `OFFLINE REWARD` | |
| `FormatDuration(ticks)` | 초로 내린 뒤 `{시}:{분 두 자리}:{초 두 자리}`. 시는 자릿수 제한·구분 없음. 불변 문화권 | 324,000,000,000 → `9:00:00`, 250,000,000 → `0:00:25`, 3,600,000,000,000 → `100:00:00` |
| `ElapsedLine(ticks)` | `AWAY ` + `FormatDuration(ticks)` | `AWAY 9:00:00` |
| `AppliedLine(ticks)` | `REWARD FOR ` + `FormatDuration(ticks)` + ` (MAX)` | `REWARD FOR 8:00:00 (MAX)` |
| `GoldLine(gold)` | `GoldText.Format(gold)` | `G 28,800` |
| `SaveFailedMessage` | `SAVE FAILED. TAP RETRY.` | |
| `ClaimButton` / `RetryButton` | `CLAIM` / `RETRY` | |
| `ButtonLabel(state)` | `SaveFailed`이면 `RETRY`, 나머지(정의되지 않은 값 포함)는 `CLAIM` | |

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.OfflineReward
{
    public static class OfflineRewardCalculator
    {
        public const long IntervalTicks = 100_000_000L;      // 10초 (임시)
        public const long MaxTicks = 288_000_000_000L;       // 8시간 (임시)

        // "동작"의 보상 계산. stageNumber 가 1 보다 작으면 ArgumentOutOfRangeException (먼저 검사).
        // 시각이 음수이거나 now < savedAt 이면 경과 0. 그 밖에는 예외 없음.
        public static OfflineRewardResult Calculate(long savedAtUtcTicks, long nowUtcTicks, int stageNumber);

        // 백그라운드 복귀 때 게임을 다시 만들지. isSaveUpToDate 가 true 이고
        // 경과(Calculate 와 같은 규칙: 음수·되돌림은 0)가 IntervalTicks 이상이면 true. 예외 없음.
        public static bool ShouldRestartOnResume(long pausedAtUtcTicks, long resumedAtUtcTicks, bool isSaveUpToDate);
    }

    public sealed class OfflineRewardResult
    {
        // Calculate 만 만든다 (생성자는 internal).
        public long ElapsedTicks { get; }   // 실제 경과. 0 이상
        public long AppliedTicks { get; }   // 상한과 10초 버림을 적용한 시간. 0 이상 MaxTicks 이하
        public long KillCount { get; }      // AppliedTicks / IntervalTicks
        public long Gold { get; }           // KillCount * StageRewards.NormalGold(stageNumber)
        public bool HasReward { get; }      // Gold > 0
        public bool IsCapped { get; }       // ElapsedTicks > MaxTicks
    }

    public enum OfflineRewardClaimState
    {
        Waiting = 0,      // 아직 누르지 않음
        SaveFailed = 1,   // 금화는 줬고 저장이 실패함. 다시 누르면 저장만 다시
        Done = 2,         // 저장까지 성공
    }

    public sealed class OfflineRewardClaim
    {
        // wallet, autoSave, reward 중 null 이면 ArgumentNullException. reward.HasReward 가 false 면 ArgumentException.
        // 아무것도 바꾸지 않는다. 시작 상태: State Waiting, HasPaid false.
        // autoSave 는 꺼진 채(enabled: false)로 만든 것을 넘긴다 (GameRoot 가 그렇게 만든다. 검사하지 않는다).
        public OfflineRewardClaim(
            PlumSword.Core.Currency.Wallet wallet,
            PlumSword.Core.Save.AutoSave autoSave,
            OfflineRewardResult reward);

        public OfflineRewardResult Reward { get; }
        public OfflineRewardClaimState State { get; }
        public bool HasPaid { get; }        // wallet.Add 를 했으면 true

        // Done: 아무것도 하지 않고 true.
        // 아니면: HasPaid 가 false 일 때만 wallet.Add(Reward.Gold), HasPaid = true, autoSave.Enable().
        // 그다음 autoSave.SaveNow(nowUtcTicks). 성공: State = Done, true. 실패: State = SaveFailed, false.
        public bool TryClaim(long nowUtcTicks);
    }

    public static class OfflineRewardText
    {
        public const string Title = "OFFLINE REWARD";
        public const string SaveFailedMessage = "SAVE FAILED. TAP RETRY.";
        public const string ClaimButton = "CLAIM";
        public const string RetryButton = "RETRY";

        // ticks 를 초로 내려 "{h}:{mm}:{ss}". 음수면 ArgumentOutOfRangeException. 불변 문화권.
        public static string FormatDuration(long ticks);
        public static string ElapsedLine(long elapsedTicks);   // "AWAY " + FormatDuration
        public static string AppliedLine(long appliedTicks);   // "REWARD FOR " + FormatDuration + " (MAX)"
        public static string GoldLine(long gold);              // GoldText.Format(gold)
        public static string ButtonLabel(OfflineRewardClaimState state);   // SaveFailed → RETRY, 나머지 CLAIM
    }
}
```

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래를 더한다
    {
        // Awake 에서 켤 때 계산한 보상. LoadResult 가 Loaded 가 아니면 null.
        public PlumSword.Core.OfflineReward.OfflineRewardResult OfflineReward { get; private set; }

        // 보상이 있을 때만 만든다. 없으면 null.
        public PlumSword.Core.OfflineReward.OfflineRewardClaim OfflineRewardClaim { get; private set; }

        // 이 루트가 Restart() 로 다시 만들어졌으면 true (백그라운드 복귀). 처음 켤 때는 false.
        public bool IsResumeRestart { get; private set; }

        // 다음 루트에 IsResumeRestart 를 표시하고, 지금 루트를 끄고 지운 뒤
        // [GameRestarter] 가 다음 프레임에 새 [Game] 을 만든다.
        public void Restart();
    }

    // 다음 프레임 Update 에서 GameBootstrap 과 같은 방법으로 [Game] 을 만들고 자기를 지운다. (internal)
    internal sealed class GameRestarter : MonoBehaviour { }
}

namespace PlumSword.Game.OfflineReward
{
    public sealed class OfflineRewardView : MonoBehaviour
    {
        // canvas 의 마지막 자식으로 보상 창을 만든다. claim, canvas, onClaimed 중 null 이면 ArgumentNullException.
        // 버튼을 누르면 claim.TryClaim(DateTime.UtcNow.Ticks). true 면 onClaimed 를 한 번 부르고 자기 오브젝트를 없앤다.
        // false 면 실패 문구를 보이고 버튼 글자를 OfflineRewardText.ButtonLabel(claim.State) 로 바꾼다.
        public void Initialize(
            PlumSword.Core.OfflineReward.OfflineRewardClaim claim,
            UnityEngine.Canvas canvas,
            System.Action onClaimed);
    }

    public sealed class OfflineResumeWatcher : MonoBehaviour
    {
        // root, autoSave 중 null 이면 ArgumentNullException.
        // OnApplicationPause(true): 떠난 시각 = DateTime.UtcNow.Ticks.
        // OnApplicationPause(false): 떠난 시각이 있으면 ShouldRestartOnResume(떠난 시각, 지금,
        //   !autoSave.HasFailed && !autoSave.IsDirty) 가 true 일 때 root.Restart(). 떠난 시각을 지운다.
        public void Initialize(PlumSword.Game.GameRoot root, PlumSword.Core.Save.AutoSave autoSave);
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot`가 `OfflineReward`(계산 결과)와, 보상이 있을 때 `OfflineRewardClaim` 하나를 만들어 들고 있는다. `TryClaim`을 부르는 것은 `OfflineRewardView` 하나뿐이다. `Restart`를 부르는 것은 `OfflineResumeWatcher` 하나뿐이다.
- **`GameRoot.Awake` 순서** (`docs/spec/save.md` "`GameRoot.Awake` 순서"에서 바뀌는 곳만):
  - 4번(`StageProgress`를 만든 뒤) 다음: `IsResumeRestart`를 정적 표시에서 읽고 표시를 지운다. `LoadResult.Status == Loaded`이면 `OfflineReward = OfflineRewardCalculator.Calculate(LoadResult.Data.SavedAtUtcTicks, DateTime.UtcNow.Ticks, StageProgress.StageNumber)`, 아니면 `null`.
  - 5번: `AutoSave`의 `enabled`를 `LoadResult.Status != SaveLoadStatus.Corrupt && !(OfflineReward != null && OfflineReward.HasReward)`로 한다. `[SaveView]`는 그대로다.
  - 6번: `Corrupt`이면 그대로(손상 창). 아니고 보상이 있으면 `Battle.SetPaused(true)`, `OfflineRewardClaim = new OfflineRewardClaim(Wallet, AutoSave, OfflineReward)`, `[Game]`의 자식 `[OfflineRewardView]`를 만들어 `Initialize(OfflineRewardClaim, UiCanvas, onClaimed)`. `onClaimed`는 `Battle.SetPaused(false)` → `StageProgress.Begin()`. 보상이 없으면 `StageProgress.Begin()`.
  - 7번 (새로): `Application.isBatchMode`가 `false`이면 `[Game]`의 자식 `[OfflineResumeWatcher]`를 만들어 `Initialize(this, AutoSave)`.
- **시작 화면 기능(2단계, 아직 사양 없음)이 따를 것**:
  - 6번 전체(손상 창 / 보상 창 / `Begin`)를 `이어하기`를 누른 뒤로 옮긴다. 보상은 `Awake`에서 계산된 `GameRoot.OfflineReward`·`OfflineRewardClaim`을 그대로 쓰고, 누른 시각으로 다시 계산하지 않는다 (시작 화면에 머문 시간 제외).
  - `이어하기`를 누르기 전에는 `AutoSave`를 켜지 않는다 (보상이 있으면 꺼진 채로 만들어져 있다). 그래서 시작 화면에서 끄면 아무것도 쓰지 않는다.
  - `게임 시작`(저장 없음)은 보상이 없다 (`OfflineReward` null).
  - 게임 중 시작 화면으로 돌아갔다가 `이어하기`로 돌아오면 보상이 없다. 같은 실행 안에서는 다시 계산하지 않는다. 다시 계산하려면 이 사양을 고친다.
  - `GameRoot.IsResumeRestart`가 `true`이면(백그라운드 복귀로 다시 만들어진 루트) 시작 화면을 건너뛰고 6번을 바로 한다.

### 필요한 것

- `PlumSword.Core.Save` (`docs/spec/save.md`): `AutoSave`(생성자의 `enabled`, `Enable()`, `SaveNow(long)`, `HasFailed`, `IsDirty`), `SaveLoadResult`, `SaveLoadStatus`, `SaveData.SavedAtUtcTicks`, `GameRoot.LoadResult`, `GameRoot.AutoSave`, `SaveView`(백그라운드로 갈 때의 `SaveNow`). **저장 기능이 먼저 구현되어 있어야 한다** (고정 integration `30464f4`에는 아직 없다).
- `PlumSword.Core.Stage` (`docs/spec/stage.md`): `StageRewards.NormalGold(int)`, `StageProgress.StageNumber`, `StageProgress.Begin()`.
- `PlumSword.Core.Currency` (`docs/spec/currency.md`): `Wallet.Add(long)`, `GoldText.Format(long)`.
- `PlumSword.Core.Combat.Battle.SetPaused(bool)` (`docs/spec/combat.md`).
- `PlumSword.Game.GameBootstrap.RootName`, `GameRoot.Wallet`, `UiCanvas`, `Battle`, `StageProgress`.

## 화면

- **조립**: 위 "`GameRoot.Awake` 순서"의 6·7번.
- **보상 창** (`OfflineRewardView`): 캔버스의 **마지막 자식**으로 만든다 (`SetAsLastSibling`, 모든 UI 위).
  - 가림막: 캔버스를 꽉 채우는 검은색 알파 0.8 `Image` (임시). `raycastTarget = true`라 뒤의 강화 버튼이 눌리지 않는다 (창이 열린 동안 강화를 멈추는 방법).
  - 창: 가운데, 앵커·피벗 (0.5, 0.5), 위치 (0, 0), 크기 360×170 (임시), 바탕 RGB (0.12, 0.12, 0.12) (임시).
  - 제목 `OfflineRewardText.Title`: 위치 (0, 60), 크기 340×24, 글자 크기 18 (임시), 흰색.
  - 지난 시간 `ElapsedLine(Reward.ElapsedTicks)`: 위치 (0, 34), 크기 340×18, 글자 크기 14 (임시), 흰색.
  - 적용 시간 `AppliedLine(Reward.AppliedTicks)`: 위치 (0, 14), 크기 340×16, 글자 크기 12 (임시), 흰색. `Reward.IsCapped`가 `true`일 때만 보인다.
  - 금화 `GoldLine(Reward.Gold)`: 위치 (0, −10), 크기 340×26, 글자 크기 20 (임시), 노랑 RGB (0.95, 0.80, 0.25) (임시).
  - 실패 문구 `SaveFailedMessage`: 위치 (0, −34), 크기 340×16, 글자 크기 12 (임시), 빨강 RGB (1.0, 0.35, 0.35) (임시). `claim.State == SaveFailed`일 때만 보인다 (처음에는 숨김).
  - 버튼: 위치 (0, −62), 크기 160×32 (임시). `Image` + `Button`, 색 RGB (0.20, 0.45, 0.25), `Button.transition = None`. 글자 `ButtonLabel(claim.State)`, 글자 크기 16 (임시), 흰색, `raycastTarget = false`.
  - 글자는 모두 `TextMeshProUGUI`, TMP 기본 글꼴, 가운데 정렬. 위치는 창 가운데 기준이다.
  - **누르면**: `TryClaim(DateTime.UtcNow.Ticks)`. `true`이면 `onClaimed`를 한 번 부르고 창 오브젝트를 없앤다. 없어지기 전에 또 눌러도 `onClaimed`는 한 번만이다. `false`이면 실패 문구를 보이고 버튼 글자를 바꾼다.
  - 창이 떠 있는 동안 뒤의 전투는 멈춰 있고 몬스터는 없다. 금화 표시는 받기 전 금화이고, 받으면 곧바로 늘어난다.
- **연출**: 보상 창 자체의 별도 연출과 소리는 없다. 보상을 받으면 공통 금화 표시가 올라가는 연출을 한다 ([금화 사양](currency.md)).
- **사람이 먼저 해 둘 것**: 금화 사양과 같다 (TMP Essentials). 없어도 예외로 게임이 멈추지 않아야 한다.
- **사람이 확인하는 법**: 에디터에서 한 번 플레이해 `save.dat`을 만든다. 그다음 `savedAtUtcTicks`를 10분 전 값(지금 틱 − 6,000,000,000)으로 바꾸고 다시 플레이하면 창이 뜬다. 9시간 전으로 바꾸면 적용 시간 줄도 보인다. 백그라운드 복귀는 폰 빌드에서 확인한다.
- 표시 글자와 언제 보일지(`IsCapped`, `State`, `ButtonLabel`)의 판단은 Core에 있고 에딧 모드 테스트로 판정한다. 배치 모드의 `GameRoot` 시작 상태는 플레이 모드 테스트로 확인한다. 보상 창과 백그라운드 복귀의 실제 동작은 사람이 확인한다.

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| (이 기능 자신의 값 없음) | | | 저장 사양의 `savedAtUtcTicks`를 보상 기준 시각으로 쓴다. 판 번호는 1 그대로 |

- `OfflineReward`, `OfflineRewardClaim`의 상태, 떠난 시각, `IsResumeRestart`는 저장하지 않는다.
- 보상 기준 시각을 따로 두지 않는다. 받은 뒤 `SaveNow`가 쓴 시각, 보상이 없을 때 첫 자동 저장이 쓴 시각이 다음 기준이 된다.
- 오프라인 보상: 이 기능 자신이 오프라인 보상이다. 꺼져 있는 동안 금화 말고는 아무것도 바뀌지 않는다.

## 범위 밖

- 시작 화면, `이어하기`, 시작 화면에서 보상 창을 띄우는 시점의 조립: 시작 화면 기능이 정한다 ("연결 지점"의 따를 것).
- 같은 실행 안에서 시작 화면 → `이어하기` 때의 보상: 하지 않는다.
- 보상 기준 시각을 저장 시각과 따로 두기 (저장 판 2): 하지 않는다.
- 시계 조작 막기(서버 시각, 단조 시계), 오프라인 중 스테이지 진행·강화, 금화 말고 다른 보상, 광고 보고 두 배 받기: 하지 않는다.
- 보스 금화, 처치 속도(실제 전투력)를 보상에 반영하기: 하지 않는다 (기획: 일반 몬스터 한 마리분 고정).
- 회차별 보상 공식: 따로 두지 않는다. 전체 진행 번호를 그대로 `NormalGold`에 넣는다 (스테이지 기능의 공식).
- 저장 실패 때 창의 `CONTINUE`(저장 없이 진행): 하지 않는다.
- PC의 창 포커스 잃음(`OnApplicationFocus`) 처리: 하지 않는다. 백그라운드는 `OnApplicationPause`만 본다.
- 한글 문구와 글꼴: 글꼴이 준비되면 `OfflineRewardText`의 글자만 바꾼다.

## 완료 조건 초안

테스트에서 `T0`은 `638000000000000000`이다. 1초는 10,000,000틱이다. "가짜 저장소"와 "스테이지 객체"는 `docs/spec/save.md` 완료 조건 초안의 것과 같다. 가짜 저장소는 쓴 횟수와 마지막 글자를 세고 실패하게 바꿀 수 있다. 스테이지 객체는 새 `Battle(upgrades)`와 `new System.Random(0)`, 시작 능력치 `(10, 100, 1.0)`으로 만든다. "보상 준비"는 다음과 같다: `new Wallet(100)`, 그 지갑의 `StatUpgrades`, 스테이지 객체(`startStage: 1`, `seenIntroStage: 1`), 가짜 저장소로 `enabled: false`인 `AutoSave`를 만들고, `reward = Calculate(T0, T0 + 6000000000, 1)`로 `new OfflineRewardClaim(wallet, autoSave, reward)`를 만든다.

1. `OfflineRewardCalculator.IntervalTicks`는 100000000, `MaxTicks`는 288000000000이다.
2. `Calculate(T0, T0 + 6000000000, 1)`은 `ElapsedTicks` 6000000000, `AppliedTicks` 6000000000, `KillCount` 60, `Gold` 600, `HasReward` `true`, `IsCapped` `false`이다.
3. `Calculate(T0, T0 + 250000000, 1)`은 `ElapsedTicks` 250000000, `AppliedTicks` 200000000, `KillCount` 2, `Gold` 20이다.
4. `Calculate(T0, T0 + 99999999, 1)`은 `AppliedTicks` 0, `KillCount` 0, `Gold` 0, `HasReward` `false`이다. `Calculate(T0, T0 + 100000000, 1)`은 `KillCount` 1, `Gold` 10, `HasReward` `true`이다.
5. `Calculate(T0, T0 + 324000000000, 1)`은 `ElapsedTicks` 324000000000, `AppliedTicks` 288000000000, `KillCount` 2880, `Gold` 28800, `IsCapped` `true`이다. `Calculate(T0, T0 + 288000000000, 1)`은 `AppliedTicks` 288000000000, `Gold` 28800, `IsCapped` `false`이다.
6. `Calculate(T0, T0 + 6000000000, 3)`의 `Gold`는 840, `Calculate(T0, T0 + 6000000000, 6)`의 `Gold`는 1200이다.
7. `Calculate(T0, T0 - 1, 1)`과 `Calculate(T0, T0, 1)`은 둘 다 `ElapsedTicks` 0, `AppliedTicks` 0, `Gold` 0, `HasReward` `false`, `IsCapped` `false`이다.
8. `Calculate(-1, T0, 1)`, `Calculate(T0, -1, 1)`, `Calculate(long.MinValue, long.MaxValue, 1)`은 모두 예외 없이 `ElapsedTicks` 0, `Gold` 0이다. `Calculate(0, T0, 1)`은 `ElapsedTicks` 638000000000000000, `AppliedTicks` 288000000000, `Gold` 28800, `IsCapped` `true`이다.
9. `Calculate(T0, T0 + 6000000000, 0)`, `Calculate(T0, T0, -1)`은 둘 다 `ArgumentOutOfRangeException`이다.
10. `Calculate(T0, T0 + 324000000000, int.MaxValue)`는 예외 없이 `KillCount` 2880, `Gold` 12369505829760이다.
11. `ShouldRestartOnResume(T0, T0 + 100000000, true)`는 `true`이다. `(T0, T0 + 99999999, true)`, `(T0, T0 + 36000000000, false)`, `(T0, T0 - 1, true)`, `(-1, T0, true)`는 모두 `false`이다.
12. `OfflineRewardClaim` 생성자에 `wallet`, `autoSave`, `reward`를 하나씩 `null`로 넣으면 각각 `ArgumentNullException`이다. `Calculate(T0, T0, 1)`의 결과를 넣으면 `ArgumentException`이다.
13. 보상 준비 직후 `State`는 `Waiting`, `HasPaid`는 `false`, 잔액 100, 쓴 횟수 0, `autoSave.Enabled`는 `false`이다. 이때 `autoSave.SaveNow(T0 + 6500000000)`(받기 전에 끔)은 `false`이고 쓴 횟수 0, 잔액 100이다.
14. 보상 준비 뒤 `TryClaim(T0 + 7000000000)`은 `true`이다. 그 뒤 `State` `Done`, `HasPaid` `true`, 잔액 700, `autoSave.Enabled` `true`, 쓴 횟수 1이고, 마지막 글자를 `Decode`하면 `Gold` 700, `SavedAtUtcTicks` T0 + 7000000000이다. 그 `SavedAtUtcTicks`로 `Calculate(SavedAtUtcTicks, SavedAtUtcTicks + 50000000, 1)`을 하면 `HasReward`는 `false`이다.
15. 14번에 이어 `TryClaim(T0 + 8000000000)`은 `true`이고, 잔액 700, 쓴 횟수 1 그대로다.
16. 보상 준비에서 가짜 저장소를 실패하게 하고 `TryClaim(T0 + 7000000000)`을 하면 `false`, `State` `SaveFailed`, `HasPaid` `true`, 잔액 700, 쓴 횟수(시도) 1, `autoSave.HasFailed` `true`, `autoSave.IsDirty` `true`이다. 이어서 `TryClaim(T0 + 7500000000)`도 `false`이고 잔액 700, 시도 2이다.
17. 16번에 이어 저장소를 성공하게 바꾸고 `TryClaim(T0 + 8000000000)`을 하면 `true`, `State` `Done`, 잔액 700, 시도 3, `autoSave.HasFailed` `false`이고, 마지막 글자를 `Decode`하면 `Gold` 700, `SavedAtUtcTicks` T0 + 8000000000이다.
18. `OfflineRewardText.FormatDuration`은 324000000000 → `"9:00:00"`, 6000000000 → `"0:10:00"`, 250000000 → `"0:00:25"`, 99999999 → `"0:00:09"`, 0 → `"0:00:00"`, 3600000000000 → `"100:00:00"`이다. -1은 `ArgumentOutOfRangeException`이다.
19. `ElapsedLine(324000000000)`은 `"AWAY 9:00:00"`, `AppliedLine(288000000000)`은 `"REWARD FOR 8:00:00 (MAX)"`, `GoldLine(28800)`은 `"G 28,800"`이다. `Title`, `SaveFailedMessage`, `ClaimButton`, `RetryButton`은 `"OFFLINE REWARD"`, `"SAVE FAILED. TAP RETRY."`, `"CLAIM"`, `"RETRY"`이다.
20. `ButtonLabel(Waiting)`은 `"CLAIM"`, `ButtonLabel(SaveFailed)`는 `"RETRY"`, `ButtonLabel(Done)`은 `"CLAIM"`, `ButtonLabel((OfflineRewardClaimState)99)`는 `"CLAIM"`이다.
21. (플레이 모드) 배치 모드에서 게임이 켜지면 `GameRoot.OfflineReward`는 `null`, `GameRoot.OfflineRewardClaim`은 `null`, `GameRoot.IsResumeRestart`는 `false`, `GameRoot.AutoSave.Enabled`는 `true`, `GameRoot.StageProgress.HasBegun`은 `true`이다. `[Game]` 아래에 `[OfflineRewardView]`와 `[OfflineResumeWatcher]`가 없다. 이 테스트는 루프와 명령줄의 배치 모드에서 판정하며, 건너뛰는 코드(`Assert.Ignore`, `Assume`, 조건부 조기 종료 등)는 넣지 않는다 (저장 사양의 플레이 모드 테스트와 같다).

## 결정 기록

결정 기록은 [records/offline_reward.md](records/offline_reward.md)의 "결정 기록"에 있다. 구현에 필요한 규칙은 위 절들에 모두 있다.
