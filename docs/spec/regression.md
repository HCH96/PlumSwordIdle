# 회귀와 엔딩 사양

- 기능 이름: regression (네임스페이스 `PlumSword.Core.Stage`)
- 작성일: 2026-11-22

## 목표

천마를 첫 판에는 거의 이길 수 없게 만들고, 천마에게 지면 1-1로 **회귀**해 다시 강해지며 도전하게 한다. 금화·강화·경지는 회귀해도 그대로다. 천마를 처음 이기면 **엔딩 화면**(천마 토벌, 총 플레이 시간, 회귀 횟수)을 보여 주고 저장을 지운 뒤 시작 화면으로 돌아간다. 목표는 자동 강화 기준 회귀 3번쯤, 약 20분에 천마를 처음 잡는 것이다 (수치는 `docs/spec/balance.md`와 시뮬레이터가 맞춘다).

지금의 "천마에게 지면 천마전 재도전, 이기면 다음 회차" 규칙(`docs/spec/stage.md`)은 `StageProgress`의 회귀 규칙을 켰을 때 이 사양으로 바뀐다. 게임(`GameRoot`)은 켜고, 끄면(기본값) 지금과 같다.

출처: `docs/design/milestone_4.md` "목표와 범위", "마일스톤 달성 기준", 2026-11-22 대화.

## 동작

- **회귀 규칙 켜기**: `StageProgress` 생성자 끝의 선택 인자 `bool useRegression = false`. `false`면 지금과 완전히 같다 (기존 테스트는 바뀌지 않는다). `GameRoot`는 `true`를 넣는다.
- **회귀** (`useRegression`이 `true`이고 화면상 스테이지 5에서 제자가 쓰러짐):
  1. 지금의 실패와 같게 `FadingOut`으로 간다 (대기 없음, 전투 멈춤, 강화 잠금).
  2. 다음 번호는 같은 번호가 아니라 **1**이다. `RegressionCount`를 1 늘린다.
  3. `StageFailed(실패한 번호)` → `Regressed(새 RegressionCount)` → `Changed` 순서로 보낸다.
  4. 가려진 순간의 교체는 지금의 큰 스테이지 교체와 같다 (1스테이지 1구간 배치, 전투 초기화). 테마 소개는 이미 본 번호(`SeenIntroStage`)를 그대로 두므로 다시 나오지 않는다. 구간 번호 소개는 나온다.
  5. 금화·강화 단계·경지·처치 수는 바뀌지 않는다. 별도 회귀 연출은 없다.
- **천마 처치** (`useRegression`이 `true`이고 화면상 스테이지 5의 보스를 혼자 이김):
  1. 보스 금화는 지금처럼 준다. `StageCleared(5의 번호)`도 지금처럼 보낸다.
  2. 결판 뒤 대기(`ClearWaitSeconds`)가 양수면 지금처럼 `ClearWait`로 기다린다. 대기가 끝나는 순간(대기가 0이면 바로) 페이드아웃 대신 `Phase = Cleared`가 되고, `battle.SetPaused(true)`, `upgrades.SetLocked(true)`를 한 뒤 `GameCleared` → `Changed`를 보낸다.
  3. `Cleared`에서는 `Tick`이 아무것도 하지 않고 전투 알림도 무시한다. 다음 번호로 가지 않는다 (`StageNumber`는 그대로).
- **시작 번호**: `useRegression`이 `true`면 생성자의 `startStage`가 5보다 크면 화면상 번호 `(startStage − 1) % 5 + 1`로 바꿔 시작한다 (옛 저장의 2회차 이상). 예: 7 → 2, 10 → 5.
- **회귀 횟수 되살리기**: 생성자 끝의 선택 인자 `int regressionCount = 0`. 음수면 0.
- **총 플레이 시간** `PlayTime`: 초 단위 `double`. `GameRoot`가 매 프레임 `Time.deltaTime`을 더한다. 더하는 때는 시작 화면이 보이지 않고(`!IsStartScreenShowing`), `StageProgress.HasBegun`이며, `Phase`가 `Cleared`가 아닐 때뿐이다. 그래서 시작 화면·오프라인 보상 창(시작 전)·꺼져 있는 시간·엔딩 화면은 세지 않는다. `Time.timeScale`이 0인 동안도 늘지 않는다.
- **엔딩** (`GameCleared`를 받은 `GameRoot`):
  1. 자동 저장을 끈다 (`AutoSave.Disable()`).
  2. 저장을 지운다 (`ISaveStore.TryDelete()`). 실패해도 계속한다.
  3. `[Game]`의 자식 `[EndingView]`로 엔딩 화면을 띄운다.
  4. 엔딩 화면의 `처음으로`를 누르면 `GameRoot.ReturnToTitle()`. 저장이 지워졌으므로 시작 화면은 저장 없는 상태(이어하기 없음)로 뜬다.
  5. 엔딩 화면에서 게임을 끄면, 다음 실행은 저장이 없으므로 처음부터다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| `useRegression` `false`에서 스테이지 5 패배 | 지금처럼 스테이지 5 재도전 | `RegressionCount` 그대로, `Regressed` 없음 |
| `useRegression` `false`에서 천마 처치 | 지금처럼 다음 회차 | `Cleared` 없음, `GameCleared` 없음 |
| `useRegression` `true`에서 스테이지 1~4 패배 | 지금처럼 그 스테이지 1구간부터 재도전 | 회귀 아님 |
| 천마와 제자가 같은 칸에 쓰러짐 | 지금 규칙대로 실패 → 회귀 | 보스 금화 없음 |
| `ClearWait` 중 남은 천마 기술에 제자가 쓰러짐 | 무시 (지금 규칙) | 엔딩으로 간다 |
| `Cleared`에서 `Tick`, 전투 알림 | 아무 일도 없다 | |
| `regressionCount` 음수 | 0으로 시작 | |
| `RegressionCount`가 `int.MaxValue`에서 회귀 | `int.MaxValue` 그대로 | 넘치지 않음 |
| `PlayTime.Add`에 음수·NaN·무한대 | `ArgumentOutOfRangeException` | 시간 그대로 |
| `new PlayTime(음수·NaN·무한대)` | 0으로 시작 | |
| `ISaveStore.TryDelete()`에 지울 파일이 없음 | `true` | |
| 저장 지우기 실패 (`TryDelete` `false`) | 엔딩은 그대로 보인다 | 다음 실행은 마지막 저장(천마 구간)부터 |
| `EndingText.PlayTime`에 음수·NaN·무한대 | `ArgumentOutOfRangeException` | |

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| `StagePhase.Cleared` | 6 | 천마를 처음 이겨 게임이 끝난 상태 |
| 회귀 뒤 번호 | 1 (1스테이지 1구간) | |
| `RegressionCount` | `int`, 처음 0 | 회귀마다 +1, 상한 `int.MaxValue` |
| `PlayTime.Seconds` | `double` (초), 처음 0 | 저장은 밀리초 `long` = 내림(초 × 1000) |
| 엔딩 제목 `EndingText.Title` | `천마 토벌` | |
| 플레이 시간 글자 `EndingText.PlayTime(seconds)` | `"플레이 시간 " + 분 + ":" + 초` | 분 = 내림(초 / 60)을 두 자리 이상(00, 07, 62), 초 = 내림(초) % 60을 두 자리. 754.9 → `플레이 시간 12:34`, 0 → `플레이 시간 00:00`, 3725 → `플레이 시간 62:05` |
| 회귀 글자 `EndingText.Regressions(count)` | `"회귀 " + count + "회"` | 3 → `회귀 3회`, 0 → `회귀 0회` |
| 버튼 글자 `EndingText.ReturnButton` | `처음으로` | |

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.Stage
{
    public enum StagePhase
    {
        // 기존 0~5 그대로
        Cleared = 6,   // 천마를 처음 이겨 게임이 끝났다. Tick·전투 알림을 무시한다.
    }

    public sealed class StageProgress
    {
        // 끝에 두 인자를 더한다. useRegression 이 false 면 지금과 같다. regressionCount 음수는 0.
        public StageProgress(Wallet wallet, StatUpgrades upgrades, Battle battle, Random random,
            int startStage = 1, int seenIntroStage = 5, double clearWaitSeconds = 0.0,
            bool useRegression = false, int regressionCount = 0);

        public bool UseRegression { get; }
        public int RegressionCount { get; }

        // 회귀할 때 StageFailed 다음, Changed 앞에 새 RegressionCount 를 담아 한 번.
        public event System.Action<int> Regressed;

        // 천마를 처음 이겨 Phase 가 Cleared 가 된 뒤, Changed 앞에 한 번.
        public event System.Action GameCleared;
    }

    public sealed class PlayTime
    {
        // initialSeconds 가 음수·NaN·무한대면 0.
        public PlayTime(double initialSeconds = 0.0);
        public double Seconds { get; }
        // 음수·NaN·무한대: ArgumentOutOfRangeException (시간 그대로).
        public void Add(double deltaSeconds);
        // 내림(Seconds * 1000).
        public long Milliseconds { get; }
    }

    public static class EndingText
    {
        public const string Title = "천마 토벌";
        public const string ReturnButton = "처음으로";
        // 음수·NaN·무한대: ArgumentOutOfRangeException.
        public static string PlayTime(double seconds);
        public static string Regressions(int count);
    }
}
```

저장 기능에 더하는 것 (`docs/spec/save.md`):

```csharp
namespace PlumSword.Core.Save
{
    public interface ISaveStore
    {
        bool TryWrite(string text);
        // 저장을 지운다. 지울 것이 없어도 true. 지우지 못하면 false (예외 없음).
        bool TryDelete();
    }

    public sealed class MemorySaveStore : ISaveStore
    {
        // TryDelete: Text 를 null 로 하고 true.
    }

    public sealed class SaveData
    {
        // 생성자 끝에 세 인자를 더한다.
        public SaveData(long gold, int attackLevel, int healthLevel, int innerPowerLevel, int realmLevel,
            int stageNumber, int seenIntroStage, long savedAtUtcTicks, int realmKills = 0,
            int regressionCount = 0, long playTimeMs = 0, bool autoUpgrade = true);
        public int RegressionCount { get; }
        public long PlayTimeMs { get; }
        public bool AutoUpgrade { get; }

        // 끝에 두 인자를 더한다. null 이면 그 칸은 처음 값(0, true).
        // RegressionCount 는 stage.RegressionCount, PlayTimeMs 는 playTime.Milliseconds, AutoUpgrade 는 autoUpgrade.Enabled.
        public static SaveData Capture(Wallet wallet, StatUpgrades upgrades, RealmProgress realm, StageProgress stage,
            long savedAtUtcTicks, PlumSword.Core.Stage.PlayTime playTime = null,
            PlumSword.Core.StatUpgrade.AutoUpgrade autoUpgrade = null);
    }

    public sealed class AutoSave
    {
        // 끝에 두 인자를 더해 Capture 에 넘긴다.
        public AutoSave(ISaveStore store, Wallet wallet, StatUpgrades upgrades, RealmProgress realm, StageProgress stage,
            bool enabled = true, PlumSword.Core.Stage.PlayTime playTime = null,
            PlumSword.Core.StatUpgrade.AutoUpgrade autoUpgrade = null);
        // 자동 저장을 끈다. 이후 Flush 와 Tick 은 쓰지 않는다. Enable() 로 다시 켤 수 있다.
        public void Disable();
    }
}
```

- 파일 형식(판 2 그대로, 판 번호를 올리지 않는다): 기존 열 줄 뒤에 세 줄을 이 순서로 더한다: `regressionCount=<int>`, `playTimeMs=<long>`, `autoUpgrade=<0 또는 1>`. 읽을 때 줄이 없으면 각각 0, 0, `true`. 값이 정수가 아니면 `Corrupt`. `autoUpgrade`는 0이면 `false`, 다른 정수면 `true`. 음수 회귀 횟수·플레이 시간은 그대로 담고, 되살릴 때 각 생성자가 0으로 만든다.
- `FileSaveStore.TryDelete()`: 저장 파일을 지운다. 파일이 없으면 `true`, 지우다 예외가 나면 `false`.

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스
    {
        // StageProgress 를 new StageProgress(..., DefaultClearWaitSeconds, true, 저장된 회귀 횟수) 로 만든다.
        // PlayTime 을 new PlayTime(저장된 playTimeMs / 1000.0) 으로 만든다. AutoUpgrade 는 저장된 autoUpgrade 로 만든다.
        // AutoSave 에 PlayTime, AutoUpgrade 를 넘긴다. Update 에서 "동작"의 조건일 때 PlayTime.Add(Time.deltaTime).
        // StageProgress.GameCleared 에서 "동작"의 엔딩 1~3.
        public PlumSword.Core.Stage.PlayTime PlayTime { get; }
        public PlumSword.Game.Stage.EndingView Ending { get; }
    }
}

namespace PlumSword.Game.Stage
{
    public sealed class EndingView : MonoBehaviour
    {
        // canvas 아래에 화면 전체를 덮는 "EndingPanel" 을 만들고 글자 셋과 버튼 "EndingReturnButton" 을 둔다.
        // 버튼을 누르면 onReturn 을 부른다 (GameRoot 는 ReturnToTitle 을 넘긴다).
        public void Initialize(UnityEngine.Canvas canvas, double playSeconds, int regressionCount, System.Action onReturn);
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot.Awake`가 `PlayTime`을 하나 만들어 들고 있는다. 회귀 횟수는 `StageProgress`가 들고 있다. `EndingView`는 `GameCleared` 때 `GameRoot`가 만들어 `Ending`에 담는다.
- 시뮬레이터는 `useRegression: true`로 `StageProgress`를 만들고, `PlayTime` 대신 흐른 시뮬레이션 시간을 센다.

### 필요한 것

- `StageProgress`, `StagePhase`, `StageFailed`, `StageCleared`, `ClearWaitSeconds` (`docs/spec/stage.md`).
- `SaveData`, `SaveCodec`, `AutoSave`, `ISaveStore`, `FileSaveStore`, `MemorySaveStore` (`docs/spec/save.md`).
- `AutoUpgrade.Enabled` (`docs/spec/auto_upgrade.md`).
- `GameRoot.ReturnToTitle()`, `IsStartScreenShowing` (`docs/spec/start_menu.md`).

## 화면

- **회귀**: 별도 화면 없음. 지금의 실패 전환(페이드, 제자 쓰러짐 대기)과 같다.
- **엔딩 화면** `EndingView` (`[Game]`의 자식 `[EndingView]`):
  - `EndingPanel`: 캔버스의 마지막 자식으로 전체를 덮는 이미지, 색 RGBA (0.05, 0.02, 0.06, 0.92) (임시). 뒤의 입력을 막는다.
  - 글자 셋(TMP, Galmuri11, 가운데 정렬, 밝은 아이보리 RGB (1, 0.95, 0.85)): `EndingTitle` `EndingText.Title`(크기 36, 위치 캔버스 가운데 기준 (0, 60)), `EndingPlayTime` `EndingText.PlayTime(플레이 시간)`(크기 18, (0, 0)), `EndingRegressions` `EndingText.Regressions(회귀 횟수)`(크기 18, (0, −30)). 크기·위치는 임시.
  - 버튼 `EndingReturnButton`: 가운데 기준 (0, −90), 160×40, 자식 `Label`에 `EndingText.ReturnButton`(Galmuri11, 크기 16). 바탕은 시작 화면 버튼 그림이 있으면 그것, 없으면 단색 RGB (0.35, 0.12, 0.2) (임시). 콜백은 한 번만 호출한다.
  - 플레이 시간과 회귀 횟수는 엔딩을 띄운 순간의 값으로 고정한다.
  - 엔딩 동안 소리는 지금 상태 그대로 둔다 (새 음악 없음).
- 글자는 Core(`EndingText`)에서 만들고 에딧 모드 테스트로 판정한다. 화면 오브젝트와 버튼은 플레이 모드 테스트로 확인한다.

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| 회귀 횟수 `regressionCount` | `int` | 0 | `StageProgress.RegressionCount` |
| 총 플레이 시간 `playTimeMs` | `long` (밀리초) | 0 | `PlayTime.Milliseconds` |
| 자동 강화 켜짐 `autoUpgrade` | `bool` (0/1) | `true` (1) | `AutoUpgrade.Enabled` (`docs/spec/auto_upgrade.md`) |

- 저장하는 때: 지금의 자동 저장 규칙 그대로다. 회귀는 금화가 아니라 `Changed`로 알려지지만, 회귀 뒤 첫 처치에서 금화가 바뀌어 저장된다. 플레이 시간과 자동 강화 켜짐도 다음 저장 때 함께 저장된다.
- 판 번호는 2 그대로다. 세 줄이 없는 기존 판 2 저장은 0, 0, `true`로 읽는다.
- 엔딩에서 저장을 지운다. 지운 뒤에는 자동 저장을 끈 상태이므로 다시 쓰지 않는다.
- 오프라인 보상: 꺼져 있는 동안 회귀 횟수와 플레이 시간은 바뀌지 않는다. 보상 금액 규칙은 그대로다.

## 범위 밖

- 회귀 연출(전용 글자·효과), 플레이 중 회귀 횟수 표시: 하지 않는다 (2026-11-22 결정).
- 엔딩 뒤 이어서 하기, 최고 기록 저장, 엔딩 음악·그림: 하지 않는다.
- 회귀 보너스(영구 능력치, 천마 피해 보상): 하지 않는다.
- 천마를 강하게 만드는 수치와 20분 맞추기: `docs/spec/balance.md`와 시뮬레이터.
- 플레이 모드 흐름 테스트 `FullRoundFlowTests.FreshLaunch_ClearFirstRound_EntersSecondRoundAndSaves`: 게임이 회귀 규칙을 켜면 2회차가 없어 성립하지 않는다. 이름은 루프 전에 사람이 대화에서 바꾼다 (아래 완료 조건 34). 본문은 루프가 고친다.

## 완료 조건 초안

회귀 (`Assets/Tests/EditMode/Stage/StageProgressRegressionTests.cs`). 공통 준비: `wallet = new Wallet(0)`, `battle = new Battle(upgrades)`, `new MinRandom()`, `Begin()` 뒤 소개가 끝날 때까지 `Tick`, "결판까지"는 `Phase`가 `Playing`이 아닐 때까지 `battle.Step()`(최대 20000번).

1. `upgrades = new StatUpgrades(wallet, 1, 1)`, `new StageProgress(wallet, upgrades, battle, new MinRandom(), 5, 5, 0.0, true)`로 결판까지 가면 `Phase`는 `FadingOut`, `StageFailed`는 5를 담아 1번, `Regressed`는 1을 담아 1번 온다.
2. 1번에서 알림 순서는 `StageFailed` → `Regressed` → `Changed`다.
3. 1번 뒤 `Tick(0.5)`이면 `StageNumber`는 1, `SubstageNumber`는 1, `RegressionCount`는 1이다.
4. 1번 전후로 `wallet.Balance`와 `upgrades`의 세 단계는 같다.
5. 1번과 같되 `useRegression`을 주지 않으면(`false`) `Tick(0.5)` 뒤 `StageNumber`는 5이고 `Regressed`는 0번, `RegressionCount`는 0이다.
6. `useRegression` `true`, 시작 번호 3에서 제자가 쓰러지면 `Tick(0.5)` 뒤 `StageNumber`는 3이고 `Regressed`는 0번이다.
7. `regressionCount: 2`로 만들고 1번처럼 지면 `Regressed`는 3을 담아 온다.
8. `regressionCount: -4`로 만들면 `RegressionCount`는 0이다.
9. `useRegression` `true`, 시작 번호 7이면 `StageNumber`는 2이고, 시작 번호 10이면 5다.
10. `useRegression` `false`, 시작 번호 7이면 `StageNumber`는 7이다.

천마 처치 (`StageProgressRegressionTests`)

11. `upgrades = new StatUpgrades(wallet, 100000, 100000)`, `new StageProgress(wallet, upgrades, battle, new MinRandom(), 5, 5, 1.5, true)`로 결판까지 가면 `Phase`는 `ClearWait`, `StageCleared`는 5를 담아 1번 왔다.
12. 11번 뒤 `Tick(1.4)`이면 `Phase`는 `ClearWait`이고 `GameCleared`는 0번이다.
13. 11번 뒤 `Tick(1.6)`이면 `Phase`는 `Cleared`, `GameCleared`는 1번, `battle.Paused`는 `true`, `upgrades.Locked`는 `true`다.
14. 13번 뒤 `Tick(10)`을 부르면 `Phase`는 `Cleared`, `StageNumber`는 5 그대로이고 `GameCleared`는 1번 그대로다.
15. 11번과 같되 대기 0.0이면 결판 즉시 `Phase`는 `Cleared`이고 `GameCleared`는 1번이다.
16. 11번에서 `GameCleared` 안에서 읽은 `Phase`는 `Cleared`이고, `GameCleared`는 `Changed`보다 먼저 온다.
17. 11번과 같되 `useRegression` `false`면 `Tick(1.5)`, `Tick(0.5)` 뒤 `StageNumber`는 6이고 `GameCleared`는 0번이다.

플레이 시간·엔딩 글자 (`Assets/Tests/EditMode/Stage/PlayTimeTests.cs`, `EndingTextTests.cs`)

18. `new PlayTime()`의 `Seconds`는 0이다. `Add(1.5)`를 두 번 부르면 3.0이고 `Milliseconds`는 3000이다.
19. `new PlayTime(12.3456)`의 `Milliseconds`는 12345다.
20. `Add(-1)`, `Add(double.NaN)`, `Add(double.PositiveInfinity)`는 각각 `ArgumentOutOfRangeException`을 던지고 `Seconds`는 그대로다.
21. `new PlayTime(-5)`와 `new PlayTime(double.NaN)`의 `Seconds`는 0이다.
22. `EndingText.PlayTime(754.9)`는 `"플레이 시간 12:34"`, `PlayTime(0)`은 `"플레이 시간 00:00"`, `PlayTime(3725)`는 `"플레이 시간 62:05"`다.
23. `EndingText.PlayTime(-1)`은 `ArgumentOutOfRangeException`을 던진다.
24. `EndingText.Regressions(3)`은 `"회귀 3회"`, `Regressions(0)`은 `"회귀 0회"`다. `Title`은 `"천마 토벌"`, `ReturnButton`은 `"처음으로"`다.

저장 (`Assets/Tests/EditMode/Save/SaveRegressionFieldsTests.cs`)

25. `new SaveData(…, regressionCount: 3, playTimeMs: 754900, autoUpgrade: false)`을 `Encode`하면 마지막 세 줄이 `regressionCount=3`, `playTimeMs=754900`, `autoUpgrade=0`이고, `Decode`하면 같은 세 값이 나온다.
26. 세 줄이 없는 판 2 글자를 `Decode`하면 `Loaded`이고 `RegressionCount` 0, `PlayTimeMs` 0, `AutoUpgrade` `true`다.
27. `playTimeMs=abc` 줄이 있으면 `Corrupt`다. `autoUpgrade=5`는 `true`로 읽는다.
28. `Capture`에 `RegressionCount` 2인 `StageProgress`, `Seconds` 12.5인 `PlayTime`, 꺼진 `AutoUpgrade`를 넘기면 `RegressionCount` 2, `PlayTimeMs` 12500, `AutoUpgrade` `false`다. 둘을 `null`로 넘기면 0, `true`다.
29. `MemorySaveStore`에 `TryWrite("a")` 뒤 `TryDelete()`는 `true`이고 `Text`는 `null`이다.
30. `AutoSave.Disable()` 뒤 금화가 바뀌고 `Flush`를 불러도 저장소에 쓰지 않는다.

화면과 조립 (플레이 모드)

31. `Assets/Tests/PlayMode/Stage/EndingViewTests.cs`: `EndingView.Initialize(canvas, 754.9, 3, onReturn)`이면 `EndingPanel`, `EndingTitle`(`"천마 토벌"`), `EndingPlayTime`(`"플레이 시간 12:34"`), `EndingRegressions`(`"회귀 3회"`), `EndingReturnButton`이 있고, 버튼을 누르면 `onReturn`이 1번 불린다.
32. 첫 씬을 불러오면 `GameRoot.StageProgress.UseRegression`은 `true`이고 `GameRoot.PlayTime`은 `null`이 아니다.
33. 첫 씬에서 게임을 시작해 2초 지나면 `GameRoot.PlayTime.Seconds`는 1.0보다 크다.
34. 흐름 테스트 (이름은 사람이 루프 전에 `FreshLaunch_DefeatFinalBoss_ShowsEndingAndClearsSave`로 바꾼다): 지금처럼 강하게 강화해 진행하면 천마 처치 뒤 `Phase`가 `Cleared`가 되고, `[EndingView]`가 있으며, 배치 저장소의 `Text`는 `null`이다.

사람 확인

35. 에디터에서 약한 상태로 천마에게 지면 1-1로 돌아가고 금화·강화 레벨·경지가 그대로다.
36. 천마를 이기면 엔딩 화면에 `천마 토벌`, 플레이 시간, 회귀 횟수가 보이고, `처음으로`를 누르면 시작 화면에 이어하기 없이 처음 시작만 있다.

## 결정 기록

결정 기록은 [records/regression.md](records/regression.md)의 "결정 기록"에 있다. 구현에 필요한 규칙은 위 절들에 모두 있다.
