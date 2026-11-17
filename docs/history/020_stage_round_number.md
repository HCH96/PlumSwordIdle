# 020_stage_round_number — 회차와 화면상 스테이지 번호

## 목표
`PlumSword.Core` 의 `StageProgress` 에 전체 진행 번호에서 계산하는 회차(`RoundNumber`)와 화면상 스테이지 번호(`DisplayStageNumber`, 1~5)를 더한다. 다섯 스테이지를 돌고 천마(5스테이지) 뒤 다음 회차의 1스테이지로 넘어가는 규칙과, 회차가 올라도 전체 진행 번호로 몬스터 능력치·금화가 계속 오르는 것을 에딧 모드 테스트로 검사한다.

## 묶음
테마회차

## 선행 작업
- 018_stage_progress_fade

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stage.md ("동작" 절의 "상태", "수치" 절의 전체 진행 번호·회차와 스테이지, "경계 상황" 표의 `int.MaxValue` 줄, "연결 지점" 절, "저장" 절, "변경 기록"의 "2026-11-17 — 다섯 테마와 회차 순환")
- docs/spec/combat.md ("연결 지점" 절의 `EnemyStats`, `Battle.AddEnemy`)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-17 — 다섯 테마와 회차 순환` 항목 중 `StageProgress` 부분만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. `StageText` 의 새 메서드는 021 이, 화면은 022 가 한다. 같은 사양의 `2026-11-17 — 스테이지 소개` 항목(`StagePhase.Intro`, `seenIntroStage` 등)은 이 작업에서 만들지 않는다 (023, 024 가 한다).
- 고칠 파일: `Assets/Scripts/Core/Stage/StageProgress.cs` (네임스페이스 `PlumSword.Core.Stage`).
- 새 테스트 파일: `Assets/Tests/EditMode/Stage/StageProgressRoundTests.cs` (네임스페이스 `PlumSword.Core.Tests.Stage`, 클래스 `StageProgressRoundTests`). 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다. 테스트용 난수는 이미 있는 `Assets/Tests/EditMode/Stage/MinRandom.cs` 의 `MinRandom` 을 쓴다 (`Next(min, max)` 가 항상 `min`. 무리 구성은 `1` 이 10개, 좌표는 6.0, 9.0, …, 33.0).
- 이미 있는 것 (공개 모양과 동작은 그대로 둔다):
  - `StageProgress(Wallet wallet, StatUpgrades upgrades, Battle battle, System.Random random, int startStage = 1)`, `int StageNumber`(전체 진행 번호, 1 이상 `int.MaxValue` 이하. 1보다 작은 `startStage` 는 1. `int.MaxValue` 에서 성공해도 그대로), `Begin()`, `Tick(double)`, 알림 `StageStarted`·`StageCleared`·`StageFailed`(인자는 전체 진행 번호), `Changed`. 일반 몬스터와 보스는 `battle.AddEnemy(kind, StageNumber, 좌표)` 로 놓고, 금화는 `StageRewards.NormalGold(StageNumber)`, `BossGold(StageNumber)` 다.
  - `EnemyStats`(전투 기능, `docs/spec/combat.md`): 일반 최대 체력 `30 + 10 × (stage − 1)`, 공격력 `5 + 2 × (stage − 1)`, 보스 최대 체력 `100 + 30 × (stage − 1)`, 공격력 `10 + 4 × (stage − 1)`. `StageRewards`: 일반 `10 + 2 × (stage − 1)`, 보스 `50 + 10 × (stage − 1)`. 고치지 않는다.
- **`StageProgress` 에 더할 것** (사양 "연결 지점" 그대로):

  ```csharp
  public int DisplayStageNumber { get; }    // (StageNumber - 1) % 5 + 1
  public int RoundNumber { get; }           // (StageNumber - 1) / 5 + 1
  ```

  - 정수 나눗셈이다. 따로 저장하지 않고 `StageNumber` 에서 매번 계산한다. 그래서 `Begin()` 전에도, 가려진 순간의 교체로 `StageNumber` 가 바뀐 뒤에도 바로 맞는 값이다. 전체 진행 번호 1~6 → 1회차 1~5, 2회차 1.
  - `StageNumber` 가 `int.MaxValue`(2147483647)면 회차는 429496730, 화면상 번호는 2 이고 넘침이 없다.
  - 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/stage.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 저장할 값은 지금처럼 `StageNumber` 하나다. 회차와 화면상 번호는 저장하지 않는다 (저장 기능은 이 작업에서 만들지 않는다).
- **기존 테스트**: 아래 테스트는 이름·본문·기대값을 그대로 두고 통과해야 한다. 회차·화면상 번호 검사는 새 테스트로 더한다. 기존 테스트를 지우거나 이름을 바꾸거나 검사를 줄이지 않는다.
  - `StageProgressFadeTests.MaxStageCleared_StaysAtMaxWithoutException`: 그대로 유지.
  - `StageProgressOutcomeTests.BossDefeatedOnMaxStage_NoOverflowAndCleared`: 그대로 유지.
- 화면(`Assets/Scripts/Game/`)은 이 작업에서 고치지 않는다. 그래서 이 작업이 끝나도 게임 화면은 아직 `STAGE 1` 을 보여 준다.

## 완료 조건
따로 적지 않으면 `wallet = new Wallet(0)`, `upgrades = new StatUpgrades(wallet, 공격력, 최대 체력, 1.0)`(항목 앞의 `(공격력, 최대 체력)`), `battle = new Battle(upgrades)`, `stage = new StageProgress(wallet, upgrades, battle, new MinRandom(), startStage)` 로 만든 것이다. "1칸 진행"은 `battle.Step()` 을 한 번 부르는 것이다. 알림 횟수는 `StageProgress` 를 만든 직후 구독해 센 누적 횟수다.

- `Assets/Tests/EditMode/Stage/StageProgressRoundTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- (10, 100) 으로 `startStage` 1, 2, 3, 4, 5, 6 을 넣어 각각 새로 만들고 `Begin()` 전에 읽으면, `StageNumber` 는 1, 2, 3, 4, 5, 6, `RoundNumber` 는 1, 1, 1, 1, 1, 2, `DisplayStageNumber` 는 1, 2, 3, 4, 5, 1 이다.
- (10, 100) 으로 `startStage` 0 과 -3 을 넣어 각각 만들면 `StageNumber`, `RoundNumber`, `DisplayStageNumber` 는 모두 1 이다.
- (1000, 100), `startStage: 5` 로 만들어 `Begin()` 을 부르고 `battle.AddEnemy(EnemyKind.Boss, 5, 1.0)` 뒤 1칸 진행하면, 금화는 90, `StageCleared` 가 5를 담아 1번 오고, `Phase` 는 `FadingOut`, `StageNumber` 는 5 다.
- 위에 이어 `Tick(0.5)` 를 부르면 `StageNumber` 는 6, `RoundNumber` 는 2, `DisplayStageNumber` 는 1 이고, `StageStarted` 가 6을 담아 1번 더 오며, 금화는 90 이고 `upgrades.GetLevel(StatType.Attack)`, `GetLevel(StatType.Health)`, `GetLevel(StatType.AttackSpeed)` 는 모두 0 그대로다.
- (10, 30), `startStage: 6` 으로 만들어 `Begin()` 을 부르면 `battle.Enemies` 의 일반 상대 10명의 `MaxHealth` 는 모두 80, `Attack` 은 모두 15 이다.
- (10, 30), `startStage: 6` 으로 `Begin()` 뒤 `battle.AddEnemy(EnemyKind.Boss, 6, 1.0)` 이 돌려준 보스의 `MaxHealth` 는 250, `Attack` 은 30 이다.
- `StageRewards.NormalGold(6)` 은 20, `StageRewards.BossGold(6)` 은 100 이다.
- (10, 30), `startStage: 6` 으로 `Begin()` 뒤 `battle.AddEnemy(EnemyKind.Boss, 6, 1.0)` 을 부르고 1칸 진행하면 보스는 살아 있고(`Health` 240) `StageFailed` 가 6을 담아 1번 온다. 이어서 `Tick(0.5)` 를 부르면 `StageNumber` 6, `RoundNumber` 2, `DisplayStageNumber` 1, `battle.Enemies.Count` 10(모두 `EnemyKind.Normal`), `battle.DiscipleHealth` 30, `DefeatedNormalCount` 0, `Boss` 는 `null` 이다.
- (10, 100), `startStage: 6` 으로 만든 스테이지의 `StageNumber` 를 읽어(6) 새 지갑·강화 (10, 100)·전투·`MinRandom` 과 그 값으로 다른 스테이지를 만들어 `Begin()` 을 부르면, `StageNumber` 6, `RoundNumber` 2, `DisplayStageNumber` 1, `DefeatedNormalCount` 0, `Boss` 는 `null` 이고 일반 상대 10명의 `MaxHealth` 는 모두 80 이다.
- `wallet = new Wallet(0)`, `upgrades = new StatUpgrades(wallet, 100000000000, 100000000000, 1.0)`, `startStage: int.MaxValue` 로 만들어 `Begin()` 뒤 `battle.AddEnemy(EnemyKind.Boss, int.MaxValue, 1.0)` 을 부르고 1칸 진행하면 금화는 21474836510 이고 `StageCleared` 가 `int.MaxValue` 를 담아 1번 온다. 이어서 `Tick(0.5)` 를 부르면 예외 없이 `StageNumber` 는 `int.MaxValue`, `RoundNumber` 는 429496730, `DisplayStageNumber` 는 2 다.
- 기존 테스트가 모두 통과한다. (위 "기존 테스트"의 두 테스트, `StageProgressTests`, `StageProgressKillTests`, `StageProgressOutcomeTests`, `StageProgressFadeTests`, `StageLayoutTests`, `StageRewardsTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `StageNumber` 의 뜻(전체 진행 번호)과 알림 인자, 생성자 모양을 바꾸지 않는다. 몬스터 능력치와 금화 공식에 넘기는 번호도 전체 진행 번호 그대로다.
- `Assets/Scripts/Core/Stage/StageText.cs` 를 고치지 않는다 (021 이 한다).
- `Assets/Scripts/Core/Combat/`, `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/` 를 고치지 않는다. 공개된 모양으로만 쓴다.
- `Assets/Scripts/Game/` 를 고치지 않는다 (화면은 022 가 한다).
- 스테이지 소개, 테마별 배경·몬스터 외형, 저장·불러오기를 만들지 않는다.
- Core 에서 `UnityEngine` 을 쓰지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 고치지 않는다.

## 보고서

### 한 일
- 전체 진행 번호에서 회차와 화면상 스테이지 번호를 계산하는 속성을 추가했다.
- 시작 번호, 5에서 6으로의 전환, 6의 능력치·보상·재도전·복원, 최대 정수 경계를 검사하는 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- Assets/Scripts/Core/Stage/StageProgress.cs
- Assets/Tests/EditMode/Stage/StageProgressRoundTests.cs 및 .meta
- tasks/doing/020_stage_round_number.md (보고서)
- .loop/result.json (결과)

### 가정
- 없음. 공개 연결 지점과 계산식은 기존 사양과 같다.

### 사람이 확인할 것
- Unity 라이선스가 활성화된 환경에서 전체 테스트와 새 에딧 모드 테스트를 실행해 통과 여부를 확인한다.

### 테스트 결과
- python tools/loop/run_unity_tests.py: Unity 유효 라이선스가 없어 EditMode와 PlayMode를 실행하지 못함 (종료 코드 198). 기존 테스트와 신규 테스트 통과 여부는 확인하지 못했다.
- dotnet build PlumSword.Core.Tests.csproj --no-restore --nologo: Windows SDK 경로 접근이 거부되어 빌드하지 못함.
- git diff --check: 통과.

## 보고서

### 한 일
- 이전 작업의 회차·화면상 스테이지 번호 계산과 완료 조건별 에딧 모드 테스트를 확인했다. 사양의 공개 연결 지점 및 계산식과 일치한다.

### 바뀐 파일
- Assets/Scripts/Core/Stage/StageProgress.cs
- Assets/Tests/EditMode/Stage/StageProgressRoundTests.cs 및 .meta
- tasks/doing/020_stage_round_number.md (보고서)
- .loop/result.json (결과)

### 가정
- 없음. 기존 사양을 그대로 구현했다.

### 사람이 확인할 것
- Unity 라이선스가 정상인 환경에서 신규 및 기존 테스트를 실행해 통과 여부를 확인한다.
- 이번 테스트 실행 종료 시도가 접근 거부되어 실행 프로세스가 남아 있을 수 있다.

### 테스트 결과
- python tools/loop/run_unity_tests.py: EditMode 실행 중 라이선스 초기화 실패 및 클라이언트 연결 거부가 로그에 기록되어 테스트 결과를 얻지 못했다. 종료 시도는 접근 거부됐다.
- git diff --check loop/20261117-113918...HEAD: 통과.


## 루프 기록 (2026-11-17 11:46)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/020_stage_round_number
- 루프 브랜치: loop/20261117-113918
