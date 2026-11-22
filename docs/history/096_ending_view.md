# 096_ending_view — 엔딩 화면 EndingView, GameRoot 의 회귀·플레이 시간·엔딩 조립, 흐름 테스트 본문

## 목표
Game 에 엔딩 화면 `EndingView`(`PlumSword.Game.Stage`)를 만들고, `GameRoot`가 회귀 규칙을 켠 `StageProgress`, `PlayTime`, 저장된 세 값, 엔딩(자동 저장 끄기 → 저장 지우기 → 엔딩 화면)을 조립하게 한다. 천마 처치 흐름 테스트의 본문을 엔딩 기준으로 고친다.

## 묶음
regression

## 선행 작업
- 092_auto_upgrade_view
- 093_stage_regression
- 094_play_time_ending_text
- 095_save_regression_fields

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다", "테스트")
- docs/spec/regression.md ("동작"의 총 플레이 시간·엔딩, "연결 지점"의 GameRoot·EndingView, "화면", "저장")
- docs/spec/start_menu.md ("연결 지점" 절, `ReturnToTitle`·`IsStartScreenShowing`)
- docs/spec/save.md ("연결 지점" 절)
- docs/spec/auto_upgrade.md ("연결 지점"의 GameRoot)

## 요구사항
- `Assets/Scripts/Game/Stage/EndingView.cs`: `public sealed class EndingView : MonoBehaviour`, `public void Initialize(UnityEngine.Canvas canvas, double playSeconds, int regressionCount, System.Action onReturn)`.
  - `canvas` 아래에 화면 전체를 덮는 `EndingPanel`(`Image`, 앵커 (0, 0)~(1, 1), 색 RGBA (0.05, 0.02, 0.06, 0.92), `raycastTarget = true`로 뒤 입력을 막음) (임시). 다른 UI 위에 보이도록 캔버스 자식의 마지막에 둔다.
  - `EndingPanel` 아래 글자 셋(TMP, Galmuri11, 가운데 정렬, RGB (1, 0.95, 0.85), 캔버스 가운데 기준 위치): `EndingTitle` = `EndingText.Title`(크기 36, (0, 60)), `EndingPlayTime` = `EndingText.PlayTime(playSeconds)`(크기 18, (0, 0)), `EndingRegressions` = `EndingText.Regressions(regressionCount)`(크기 18, (0, −30)). 크기·위치는 임시.
  - 버튼 `EndingReturnButton`: 가운데 기준 (0, −90), 160×40, 자식 글자 `EndingText.ReturnButton`(Galmuri11, 크기 16). 바탕은 `Resources.Load<Sprite>(PlumSword.Game.StartMenu.StartScreenArt.StartButtonPath)`가 있으면 그 그림, 없으면 단색 RGB (0.35, 0.12, 0.2) (임시). 누르면 `onReturn`을 한 번 부른다.
  - 플레이 시간과 회귀 횟수는 받은 값으로 고정한다 (다시 그리지 않는다). 새 음악·소리 없음.
- `Assets/Scripts/Game/GameRoot.cs`:
  - 공개 속성 `public PlumSword.Core.Stage.PlayTime PlayTime { get; private set; }`. `Awake`에서 `PlayTime = new PlumSword.Core.Stage.PlayTime(data != null ? data.PlayTimeMs / 1000.0 : 0.0)`.
  - 092 가 넣은 `new AutoUpgrade(StatUpgrades, Wallet, true)`의 셋째 인자를 `data != null ? data.AutoUpgrade : true`로 바꾼다.
  - `CreateStage`의 `new StageProgress(...)`를 `new StageProgress(Wallet, StatUpgrades, Battle, new System.Random(), startStage, seenIntroStage, StageProgress.DefaultClearWaitSeconds, true, data != null ? data.RegressionCount : 0)`로 바꾼다 (회귀 횟수를 넘길 수 있게 `CreateStage`에 인자를 더한다).
  - `new AutoSave(...)`에 `playTime: PlayTime, autoUpgrade: AutoUpgrade`를 넘긴다 (`enabled`의 지금 규칙은 그대로).
  - `Update`를 더한다: `!IsStartScreenShowing && StageProgress.HasBegun && StageProgress.Phase != StagePhase.Cleared`일 때만 `PlayTime.Add(Time.deltaTime)`. (시작 화면, 오프라인 보상 창(시작 전), 엔딩 화면, `Time.timeScale` 0 동안은 늘지 않는다.)
  - `StageProgress.GameCleared`를 구독해 엔딩을 띄운다: ① `AutoSave.Disable()` ② 저장소 `TryDelete()` (실패해도 계속) ③ `[Game]`의 자식 `[EndingView]`를 만들어 `EndingView.Initialize(UiCanvas, PlayTime.Seconds, StageProgress.RegressionCount, ReturnToTitle)`. 공개 속성 `public PlumSword.Game.Stage.EndingView Ending { get; private set; }`로 들고 있는다 (테스트가 찾기 쉽게, 이름은 자유롭게 정해도 되며 정하면 보고서에 적는다).
  - 그 밖의 기능 조립 줄은 고치지 않는다.
- **바뀌는 기존 테스트** `FullRoundFlowTests.FreshLaunch_DefeatFinalBoss_ShowsEndingAndClearsSave` (`Assets/Tests/PlayMode/Flow/FullRoundFlowTests.cs`): 이름은 사람이 이미 바꿨다. 이름은 그대로 두고 본문을 엔딩 기준으로 고친다. 지금 본문의 검사는 14개다. **14개 이상**을 유지한다.
  - 그대로 둔다: 시작 확인 3개(`Empty`, `StageNumber` 1, `RoundNumber` 1), `PowerUp(root, 200)`과 그 뒤 2개(`Attack` 410, `MaxHealth` 4100), `balanceAfterUpgrade` 기록, `Time.timeScale = 20f`.
  - 기다림: `root.StageProgress.StageNumber == 6` 대신 `root.StageProgress.Phase == StagePhase.Cleared`를 실제 시간 180초 안에 기다린다 (실패 메시지에 지금처럼 상태를 적는다). 그 뒤 3프레임 기다린다.
  - 그 뒤 검사(지금의 2회차 검사 9개를 바꾼다): `Phase`는 `Cleared`, `StageNumber`는 5, `RegressionCount`는 0, 모은 `StageCleared`는 [1, 2, 3, 4, 5], `StageFailed`는 비어 있음, `Realm.Current`는 `RealmLevel.SecondRate`, `Wallet.Balance - balanceAfterUpgrade`는 974 이상 1286 이하, `[Game]` 아래 `[EndingView]` 오브젝트가 있음, 배치 저장소 `store.Text`는 `null`, `root.AutoSave.Enabled`는 `false` (10개, 모두 합해 15개).
  - 흐름 도우미 `GameFlow.Relaunch`가 자동 강화를 끄므로(092) 금화 검사는 지금 범위 그대로다.
  - 이 밖에 회귀 규칙을 켜서 실패하는 기존 테스트가 있으면, 이름은 두고 기대값을 회귀 규칙에 맞게 바꾼 뒤 무엇을 A → B 로 바꿨는지 보고서의 가정에 적는다 (검사 수는 줄이지 않는다).
- 끝나면 `docs/spec/regression.md` "화면"과 실제가 달라진 곳을 고치고, `docs/spec/save.md`·`docs/spec/start_menu.md`의 `GameRoot` 조립 설명에 `PlayTime`, 회귀 규칙, 엔딩을 한 줄씩 더한다.

## 완료 조건
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다 (`FullRoundFlowTests`는 요구사항에 적은 본문으로).
- [플레이 모드] 31. `Assets/Tests/PlayMode/Stage/EndingViewTests.cs`: `EndingView.Initialize(canvas, 754.9, 3, onReturn)`이면 `EndingPanel`, `EndingTitle`(`"천마 토벌"`), `EndingPlayTime`(`"플레이 시간 12:34"`), `EndingRegressions`(`"회귀 3회"`), `EndingReturnButton`이 있고, 버튼을 누르면 `onReturn`이 1번 불린다.
- [플레이 모드] 32. `Assets/Tests/PlayMode/Stage/GameRootRegressionTests.cs`: 첫 씬을 불러오면 `GameRoot.StageProgress.UseRegression`은 `true`이고 `GameRoot.PlayTime`은 `null`이 아니다.
- [플레이 모드] 33. 같은 파일: 첫 씬에서 게임을 시작해(`GameFlow.ResetToFreshLaunch()` 뒤) 실제 시간 2초가 지나면 `GameRoot.PlayTime.Seconds`는 1.0보다 크다.
- [플레이 모드] 34. `FullRoundFlowTests.FreshLaunch_DefeatFinalBoss_ShowsEndingAndClearsSave`: 지금처럼 강하게 강화해 진행하면 천마 처치 뒤 `Phase`가 `Cleared`가 되고, `[EndingView]`가 있으며, 배치 저장소의 `Text`는 `null`이다 (요구사항의 검사 15개).
- [사람 확인] 35. 에디터에서 약한 상태로 천마에게 지면 1-1로 돌아가고 금화·강화 레벨·경지가 그대로다. 에디터에서 실행해 확인한다.
- [사람 확인] 36. 천마를 이기면 엔딩 화면에 `천마 토벌`, 플레이 시간, 회귀 횟수가 보이고, `처음으로`를 누르면 시작 화면에 이어하기 없이 처음 시작만 있다. 에디터에서 실행해 확인한다.

## 하지 말 것
- `Assets/Scripts/Core/`를 고치지 않는다 (093·094·095 가 했다).
- `GameRoot`에서 이 작업에 적은 줄 말고 다른 기능의 조립 줄을 고치지 않는다.
- `FullRoundFlowTests` 말고 기존 테스트의 이름을 바꾸거나 지우지 않는다. 검사 수를 줄이지 않는다.
- 회귀 연출, 플레이 중 회귀 횟수 표시, 엔딩 뒤 이어서 하기, 최고 기록, 엔딩 음악·그림을 만들지 않는다.
- 수치 표(`BalanceTable`) 값을 바꾸지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/`를 건드리지 않는다.

## 보고서

### 한 일
- 회귀 규칙과 저장된 회귀 횟수·플레이 시간·자동 강화 상태를 게임에 연결하고, 천마 처치 시 자동 저장을 끄고 저장을 지운 뒤 엔딩 화면을 표시했다.
- 엔딩 화면과 플레이 시간 누적 플레이 모드 테스트를 추가하고 천마 처치 흐름 검사를 엔딩 기준으로 바꿨다.
- 관련 사양에 조립과 화면 동작을 반영했다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs`, `Assets/Scripts/Game/Stage/EndingView.cs` 및 그 `.meta`
- `Assets/Tests/PlayMode/Flow/FullRoundFlowTests.cs`, `Assets/Tests/PlayMode/Stage/EndingViewTests.cs`, `GameRootRegressionTests.cs` 및 새 테스트의 `.meta`
- `docs/spec/regression.md`, `docs/spec/save.md`, `docs/spec/start_menu.md`

### 가정
- 엔딩 화면을 찾는 공개 속성 이름은 `Ending`으로 정하고 회귀 사양의 연결 지점에 추가했다.
- 글자 영역의 임시 크기는 제목 600×48, 시간과 회귀 횟수 각각 600×28로 정했다. 버튼 글자 자식 이름은 `Label`이다.
- 기존 시작 버튼 그림에 원래 글자가 포함되어 있어도 지정된 그림을 그대로 사용한다.

### 사람이 확인할 것
- 에디터에서 약한 상태로 천마에게 지면 1-1로 돌아가고 금화·강화 레벨·경지가 그대로인지 확인한다.
- 천마를 이기면 엔딩 화면에 `천마 토벌`, 플레이 시간, 회귀 횟수가 보이고, `처음으로`를 누르면 시작 화면에 이어하기 없이 처음 시작만 있는지 에디터에서 확인한다.
- 시작 버튼 그림에 포함된 기존 글자와 엔딩 버튼 글자가 겹쳐 보이는지 확인한다.

### 테스트 결과
- `git diff --check` 통과. Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 컴파일 및 테스트 판정을 기다린다.


## 루프 기록 (2026-11-22 17:09)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/096_ending_view
- 루프 브랜치: loop/20261122-164616
