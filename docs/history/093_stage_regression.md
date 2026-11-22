# 093_stage_regression — StageProgress 회귀 규칙과 천마 처치 상태 Cleared

## 목표
Core(`PlumSword.Core.Stage.StageProgress`)에 선택 인자 회귀 규칙(`useRegression`)을 더한다. 켜면 천마(화면상 스테이지 5)에게 졌을 때 1스테이지 1구간으로 회귀하고 회귀 횟수를 센다. 천마를 이기면 다음 회차 대신 `StagePhase.Cleared`가 되고 `GameCleared`를 보낸다. 끄면(기본값) 지금과 완전히 같다.

## 묶음
regression

## 선행 작업
- 089_balance_substage

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/regression.md ("동작"의 회귀 규칙 켜기·회귀·천마 처치·시작 번호·회귀 횟수 되살리기, "경계 상황", "수치", "연결 지점"의 `StagePhase`·`StageProgress`)
- docs/spec/stage.md ("동작", "연결 지점" 절)

## 요구사항
- `Assets/Scripts/Core/Stage/StagePhase.cs`에 `Cleared = 6`을 더한다 (주석: 천마를 처음 이겨 게임이 끝났다. Tick·전투 알림을 무시한다). 기존 값 0~5는 그대로다.
- `Assets/Scripts/Core/Stage/StageProgress.cs`:
  - 생성자 끝에 두 선택 인자를 더한다: `public StageProgress(Wallet wallet, StatUpgrades upgrades, Battle battle, Random random, int startStage = 1, int seenIntroStage = 5, double clearWaitSeconds = 0.0, bool useRegression = false, int regressionCount = 0)`.
  - `public bool UseRegression { get; }`, `public int RegressionCount { get; private set; }` (음수로 받으면 0).
  - `public event System.Action<int> Regressed;`, `public event System.Action GameCleared;`
  - **시작 번호**: `useRegression`이 `true`이고 `startStage`가 5보다 크면 `(startStage − 1) % 5 + 1`로 시작한다 (예: 7 → 2, 10 → 5). `false`면 지금 규칙 그대로다. 1보다 작으면 1인 규칙은 그대로다.
  - **회귀** (`UseRegression`이 `true`이고 `DisplayStageNumber == 5`에서 제자가 쓰러짐, 지금 `OnDiscipleDefeated`의 실패 처리 자리):
    1. 지금 실패와 같게 전환을 시작한다 (`FadingOut`, 대기 없음, 전투 멈춤, 강화 잠금). 다음 번호는 **1**이다.
    2. `RegressionCount`를 1 늘린다. `int.MaxValue`면 그대로 둔다 (넘치지 않는다).
    3. `StageFailed(실패한 번호)` → `Regressed(새 RegressionCount)` → `Changed` 순서로 보낸다.
    4. 가려진 순간의 교체는 지금의 큰 스테이지 교체와 같다 (1스테이지 1구간 배치, 전투 초기화). `SeenIntroStage`는 바꾸지 않으므로 테마 소개는 다시 나오지 않고, 구간 번호 소개는 나온다.
    5. 금화·강화 단계·경지·처치 수는 건드리지 않는다.
  - 스테이지 1~4에서 지면 회귀 규칙과 상관없이 지금처럼 그 스테이지를 다시 한다.
  - **천마 처치** (`UseRegression`이 `true`이고 `DisplayStageNumber == 5`의 보스를 혼자 이김):
    1. 보스 금화와 `StageCleared(번호)`는 지금처럼 준다·보낸다.
    2. `ClearWaitSeconds`가 양수면 지금처럼 `ClearWait`로 기다린다. 대기가 끝나는 순간(지금 `ClearWait` → `FadingOut`으로 넘어가는 자리), 대기가 0이면 결판 즉시 페이드아웃 대신 `Phase = StagePhase.Cleared`로 두고 `battle.SetPaused(true)`, `upgrades.SetLocked(true)`를 한 뒤 `GameCleared` → `Changed`를 보낸다.
    3. `Cleared`에서는 `Tick`이 (인자 검사 말고는) 아무것도 하지 않고 전투 알림도 무시한다. 다음 번호로 가지 않는다 (`StageNumber` 그대로).
  - 천마와 제자가 같은 칸에 쓰러지면 지금 규칙대로 실패이므로 회귀한다 (보스 금화 없음). `ClearWait` 중에 제자가 쓰러지는 것은 지금처럼 무시한다.
  - `useRegression`이 `false`면 위 어느 것도 일어나지 않는다: 스테이지 5 패배는 지금처럼 재도전, 천마 처치는 지금처럼 다음 회차, `Regressed`·`GameCleared` 없음, `RegressionCount`는 생성자 값 그대로.
- 기존 테스트: 기본값이 `false`라 기대값이 바뀌는 기존 테스트는 없다 (`StageProgressFinalTests`의 다음 회차·재도전 테스트 포함). 기존 테스트는 고치지 않는다.
- 구현하다 모양이 사양과 달라지면 `docs/spec/regression.md`의 "연결 지점" 절을 고치고 보고서의 가정에 적는다. 끝나면 `docs/spec/stage.md` "연결 지점"에 `useRegression`·`regressionCount` 인자, `UseRegression`, `RegressionCount`, `Regressed`, `GameCleared`, `StagePhase.Cleared`를 한 줄씩 더한다.
- 테스트 공통 준비: `wallet = new Wallet(0)`, `battle = new Battle(upgrades)`, 기존 `Assets/Tests/EditMode/Stage/MinRandom.cs`의 `new MinRandom()`. `Begin()` 뒤 소개가 끝날 때까지 `Tick`, "결판까지"는 `Phase`가 `Playing`이 아닐 때까지 `battle.Step()`(최대 20000번).

## 완료 조건
- `Assets/Tests/EditMode/Stage/StageProgressRegressionTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 1. `upgrades = new StatUpgrades(wallet, 1, 1)`, `new StageProgress(wallet, upgrades, battle, new MinRandom(), 5, 5, 0.0, true)`로 결판까지 가면 `Phase`는 `FadingOut`, `StageFailed`는 5를 담아 1번, `Regressed`는 1을 담아 1번 온다.
- 2. 1번에서 알림 순서는 `StageFailed` → `Regressed` → `Changed`다.
- 3. 1번 뒤 `Tick(0.5)`이면 `StageNumber`는 1, `SubstageNumber`는 1, `RegressionCount`는 1이다.
- 4. 1번 전후로 `wallet.Balance`와 `upgrades`의 세 단계는 같다.
- 5. 1번과 같되 `useRegression`을 주지 않으면(`false`) `Tick(0.5)` 뒤 `StageNumber`는 5이고 `Regressed`는 0번, `RegressionCount`는 0이다.
- 6. `useRegression` `true`, 시작 번호 3에서 제자가 쓰러지면 `Tick(0.5)` 뒤 `StageNumber`는 3이고 `Regressed`는 0번이다.
- 7. `regressionCount: 2`로 만들고 1번처럼 지면 `Regressed`는 3을 담아 온다.
- 8. `regressionCount: -4`로 만들면 `RegressionCount`는 0이다.
- 9. `useRegression` `true`, 시작 번호 7이면 `StageNumber`는 2이고, 시작 번호 10이면 5다.
- 10. `useRegression` `false`, 시작 번호 7이면 `StageNumber`는 7이다.
- 11. `upgrades = new StatUpgrades(wallet, 100000, 100000)`, `new StageProgress(wallet, upgrades, battle, new MinRandom(), 5, 5, 1.5, true)`로 결판까지 가면 `Phase`는 `ClearWait`, `StageCleared`는 5를 담아 1번 왔다.
- 12. 11번 뒤 `Tick(1.4)`이면 `Phase`는 `ClearWait`이고 `GameCleared`는 0번이다.
- 13. 11번 뒤 `Tick(1.6)`이면 `Phase`는 `Cleared`, `GameCleared`는 1번, `battle.Paused`는 `true`, `upgrades.Locked`는 `true`다.
- 14. 13번 뒤 `Tick(10)`을 부르면 `Phase`는 `Cleared`, `StageNumber`는 5 그대로이고 `GameCleared`는 1번 그대로다.
- 15. 11번과 같되 대기 0.0이면 결판 즉시 `Phase`는 `Cleared`이고 `GameCleared`는 1번이다.
- 16. 11번에서 (13번처럼 `Tick(1.6)` 할 때) `GameCleared` 안에서 읽은 `Phase`는 `Cleared`이고, `GameCleared`는 `Changed`보다 먼저 온다.
- 17. 11번과 같되 `useRegression` `false`면 `Tick(1.5)`, `Tick(0.5)` 뒤 `StageNumber`는 6이고 `GameCleared`는 0번이다.
- (추가) `RegressionCount`가 `int.MaxValue`인 진행(`regressionCount: int.MaxValue`)에서 1번처럼 지면 `Regressed`는 `int.MaxValue`를 담아 오고 `RegressionCount`는 `int.MaxValue`다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `useRegression`이 `false`일 때의 동작을 바꾸지 않는다.
- 플레이 시간(`PlayTime`), 엔딩 글자(`EndingText`), 저장, 화면, `GameRoot`는 만들거나 고치지 않는다 (094·095·096).
- 회귀 연출, 플레이 중 회귀 횟수 표시, 회귀 보너스를 더하지 않는다.
- `Battle`, `StatUpgrades`, `Wallet`의 내부 코드를 고치지 않는다. 공개 메서드(`SetPaused`, `SetLocked` 등)만 쓴다.
- 기존 테스트를 고치지 않는다.
- `UnityEngine`을 쓰지 않는다. 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 선택 가능한 천마 패배 회귀와 천마 처치 후 종료 상태, 알림 순서 및 회귀 횟수 상한을 구현했다.
- 기본 설정의 재도전·다음 회차와 회귀 설정의 시작 번호, 자원 보존, 대기 및 종료를 검사하는 에딧 모드 테스트를 추가했다.
- 스테이지 사양의 연결 지점과 종료 동작을 갱신했다.

### 바뀐 파일
- `Assets/Scripts/Core/Stage/StagePhase.cs`
- `Assets/Scripts/Core/Stage/StageProgress.cs`
- `Assets/Tests/EditMode/Stage/StageProgressRegressionTests.cs` 및 짝 `.meta`
- `docs/spec/stage.md`
- `tasks/doing/093_stage_regression.md`, `.loop/result.json`

### 가정
- 없음.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- `git diff --check` 통과.
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 실행과 판정을 기다린다.


## 루프 기록 (2026-11-22 16:52)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/093_stage_regression
- 루프 브랜치: loop/20261122-164616
