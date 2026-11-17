# 023_stage_intro_begin — 스테이지 소개의 상태와 시작 판단, 소개 문구

## 목표
`PlumSword.Core` 의 스테이지에 소개 단계(`StagePhase.Intro`)와 본 소개 번호(`SeenIntroStage`)를 더한다. `Begin()` 이 1회차에서 아직 보지 않은 스테이지면 소개를 시작하고 전투를 멈춘다. 소개 문구 `StageText.IntroLine` 도 만든다. 소개 시간을 세어 끝내는 것과 스테이지 전환 때의 소개는 024 가 한다. 에딧 모드 테스트로 검사한다.

## 묶음
스테이지소개

## 선행 작업
- 020_stage_round_number
- 021_stage_theme_text

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stage.md ("동작" 절의 "상태", "시작", "소개 판단", 전투 알림 무시, "페이드 막의 진하기", "경계 상황" 표의 소개 관련 줄, "수치" 절의 소개 항목과 `IntroLine` 표, "연결 지점" 절, "변경 기록"의 "2026-11-17 — 스테이지 소개")
- docs/spec/combat.md ("연결 지점" 절의 `Battle.SetPaused`, `Paused`, `Step`)
- docs/spec/stat_upgrade.md ("연결 지점" 절의 `Locked`, `TryUpgrade`)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-17 — 스테이지 소개` 항목 중 아래에 적은 부분(상태, 생성자, `Begin` 의 소개 판단, `IntroLine`)만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. `Tick` 의 소개 처리와 가려진 순간의 교체 때의 소개 판단은 024 가, 화면과 `GameRoot` 는 025 가 한다.
- 고칠 파일: `Assets/Scripts/Core/Stage/StagePhase.cs`, `Assets/Scripts/Core/Stage/StageProgress.cs`, `Assets/Scripts/Core/Stage/StageText.cs` (네임스페이스 `PlumSword.Core.Stage`).
- 새 테스트 파일: `Assets/Tests/EditMode/Stage/StageProgressIntroTests.cs` (네임스페이스 `PlumSword.Core.Tests.Stage`, 클래스 `StageProgressIntroTests`). `IntroLine` 테스트는 기존 `StageTextTests` 클래스에 새 테스트 메서드로 더한다. 테스트 이름은 `<상황>_<기대 결과>` 모양. 테스트용 난수는 이미 있는 `MinRandom` 을 쓴다.
- 선행 작업이 만든 것을 쓴다.
  - 020_stage_round_number: `StageProgress.RoundNumber`(`(StageNumber − 1) / 5 + 1`), `DisplayStageNumber`(`(StageNumber − 1) % 5 + 1`).
  - 021_stage_theme_text: `StageText.ThemeName(int)` 과 그 테스트가 있는 `StageTextTests`.
  - 이미 있는 `StageProgress`: `Begin()` 은 "스테이지 배치"(`battle.Reset()` → 무리 구성 → 일반 몬스터 10마리 → 처치 수 0, `Boss = null`) 뒤 `HasBegun = true`, `StageStarted(StageNumber)`, `Changed` 를 보낸다. `Phase` 가 `Playing` 이 아니면 전투 알림(`EnemyDefeated`, `DiscipleDefeated`, `EnemyDamaged`)을 무시한다. `FadeAlpha` 는 `FadingOut`·`FadingIn` 이 아니면 0 이다.
- **공개 모양** (사양 "연결 지점" 그대로):

  ```csharp
  public enum StagePhase
  {
      Playing = 0,
      FadingOut = 1,
      FadingIn = 2,
      Intro = 3,       // 1회차 첫 진입의 소개를 보여 주는 중. 전투는 멈추고 강화는 열려 있다
  }

  // StageProgress
  public const double IntroSeconds = 3.0;   // (임시값)
  public StageProgress(Wallet wallet, StatUpgrades upgrades, Battle battle, System.Random random,
                       int startStage = 1, int seenIntroStage = 5);
  public int SeenIntroStage { get; }        // 0 이상 5 이하
  public bool IsIntroShowing { get; }       // Phase == StagePhase.Intro

  // StageText
  public static string IntroLine(int stage);   // stage 가 1~5 밖이면 ArgumentOutOfRangeException
  ```

  - `StagePhase.Intro` 는 끝에 더하고 기존 값의 번호는 바꾸지 않는다.
  - 생성자의 `seenIntroStage` 는 0보다 작으면 0, 5보다 크면 5로 잘라 `SeenIntroStage` 에 둔다 (예외 없음, 알림 없음). 기본값 5 는 "다섯 개 모두 봄"이라 소개를 보여 주지 않으므로 기존 호출과 기존 테스트는 그대로 동작한다.
  - 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/stage.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- **소개 판단** (`private` 메서드로 둔다. 024 가 가려진 순간의 교체에서도 쓴다): `RoundNumber` 가 1이고 `DisplayStageNumber` 가 `SeenIntroStage` 보다 크면 소개를 보여 주기로 하고 `SeenIntroStage = DisplayStageNumber` 로 올린다. 아니면 보여 주지 않고 `SeenIntroStage` 는 그대로다.
- **`Begin()`**: 순서대로 "스테이지 배치" → "소개 판단" → 소개를 보여 주기로 했으면 `Phase = Intro`, 소개 시간(`private double` 필드) 0, `battle.SetPaused(true)` → `HasBegun = true` → `StageStarted(StageNumber)` → `Changed`. 그래서 `StageStarted` 를 받는 시점에 `SeenIntroStage` 는 이미 올라가 있고 `Phase` 는 `Intro` 다. 소개를 보여 주지 않으면 지금과 똑같다 (전투 멈춤을 건드리지 않는다). 강화 잠금은 어느 경우든 건드리지 않는다. 두 번째 `Begin()` 은 지금처럼 아무 일도 없다.
- **소개 중(`Intro`)**: 전투 알림은 지금 규칙(`Playing` 이 아니면 무시) 그대로 무시된다. `FadeAlpha` 는 0 이다. 강화는 잠기지 않는다.
- **`Tick` 은 이 작업에서 소개를 처리하지 않는다** (024 가 한다). 다만 `Intro` 중에 `Tick` 이 불려도 예외가 나거나 `Phase` 가 바뀌지 않게 둔다.
- **`StageText.IntroLine(stage)`** (임시 영문, 그대로 옮긴다):

  | stage | `IntroLine` |
  | --- | --- |
  | 1 | `A Mount Hua disciple faces a first sparring match at Jongnam.` |
  | 2 | `Pass the Beggar Clan's trial and hear of the Demon Cult's movements.` |
  | 3 | `Cross swords with Namgung to prepare for the battle to come.` |
  | 4 | `The battle to hold back the Demon Cult's army begins.` |
  | 5 | `At the end of the war, the Heavenly Demon awaits.` |

- **기존 테스트**: 바뀌는 것이 없다. 생성자의 기본값이 소개를 보여 주지 않으므로 기존 테스트(`Begin` 직후 `battle.Paused` 가 `false` 등)는 이름·본문·기대값을 그대로 두고 통과해야 한다. 기존 테스트를 지우거나 이름을 바꾸거나 검사를 줄이지 않는다.
- 저장은 하지 않는다. `SeenIntroStage` 를 읽을 수 있게만 한다.

## 완료 조건
따로 적지 않으면 `wallet = new Wallet(0)`, `upgrades = new StatUpgrades(wallet, 공격력, 최대 체력, 1.0)`(항목 앞의 `(공격력, 최대 체력)`), `battle = new Battle(upgrades)`, `stage = new StageProgress(wallet, upgrades, battle, new MinRandom(), startStage, seenIntroStage)` 로 만든 것이다 (`startStage` 를 적지 않으면 1). "1칸 진행"은 `battle.Step()` 을 한 번 부르는 것이다. 알림 횟수와 순서는 `StageProgress` 를 만든 직후 구독해 기록한 것이다.

- `Assets/Tests/EditMode/Stage/StageProgressIntroTests.cs` 와 `Assets/Tests/EditMode/Stage/StageTextTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- (10, 100) 으로 `seenIntroStage` 를 생략해 만들면 `SeenIntroStage` 는 5 이다.
- (10, 100) 으로 `seenIntroStage` 에 0, 3, −2, 9 를 넣어 각각 만들면 예외 없이 `SeenIntroStage` 는 0, 3, 0, 5 이다.
- 위 다섯 경우(생략, 0, 3, −2, 9) 모두 `Begin()` 전의 `IsIntroShowing` 은 `false`, `Phase` 는 `Playing` 이다.
- (10, 100), `seenIntroStage: 0` 으로 만들어 `Begin()` 을 부르면 `SeenIntroStage` 1, `Phase` `Intro`, `IsIntroShowing` `true`, `battle.Paused` `true`, `upgrades.Locked` `false`, `FadeAlpha` 0.0, `battle.Enemies.Count` 10 이다.
- (10, 100), `seenIntroStage: 0` 으로 `Begin()` 을 부르면 `StageStarted` 가 1을 담아 1번, 그 뒤에 `Changed` 가 1번 오고, `StageStarted` 를 받는 시점에 읽은 `SeenIntroStage` 는 1, `Phase` 는 `Intro` 다.
- (10, 100), `seenIntroStage: 0` 으로 `Begin()` 뒤 `battle.AddEnemy(EnemyKind.Normal, 1, 0.8)` 을 부르고 1칸 진행하면 금화 0, `DefeatedNormalCount` 0, `battle.DisciplePosition` 0 이고 `Changed` 는 더 오지 않는다 (모두 1번).
- `wallet = new Wallet(20)`, (10, 100), `seenIntroStage: 0` 으로 `Begin()` 뒤 `upgrades.TryUpgrade(StatType.Attack)` 은 `true` 이고, 금화는 0, `Phase` 는 `Intro` 그대로다.
- (10, 100), `startStage: 3, seenIntroStage: 3` 으로 `Begin()` 을 부르면 `Phase` `Playing`, `battle.Paused` `false`, `SeenIntroStage` 3 이다.
- (10, 100), `startStage: 3, seenIntroStage: 2` 로 `Begin()` 을 부르면 `Phase` `Intro`, `SeenIntroStage` 3 이다.
- (10, 100), `startStage: 7, seenIntroStage: 0` 으로 `Begin()` 을 부르면 (2회차) `Phase` `Playing`, `battle.Paused` `false`, `SeenIntroStage` 0 이다.
- (10, 100), `seenIntroStage` 생략으로 `Begin()` 을 부르면 `Phase` `Playing`, `battle.Paused` `false`, `SeenIntroStage` 5 이다.
- `StageText.IntroLine(1)`~`IntroLine(5)` 는 위 표의 영문 문구와 글자 하나까지 같다.
- `StageText.IntroLine(0)` 과 `IntroLine(6)` 은 `ArgumentOutOfRangeException` 이다.
- `(int)StagePhase.Playing`, `FadingOut`, `FadingIn`, `Intro` 는 0, 1, 2, 3 이다.
- 기존 테스트가 모두 통과한다. (`StageProgressTests`, `StageProgressKillTests`, `StageProgressOutcomeTests`, `StageProgressFadeTests`, `StageProgressRoundTests`, `StageTextTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `Tick` 에 소개 시간 세기나 소개 끝내기를 넣지 않는다. 가려진 순간의 교체에 소개 판단을 넣지 않는다 (024 가 한다).
- 기존 `StagePhase` 값의 번호, `StageNumber`, 알림 인자, 기존 생성자 인자의 뜻을 바꾸지 않는다.
- `Assets/Scripts/Core/Combat/`, `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/` 를 고치지 않는다.
- `Assets/Scripts/Game/` 를 고치지 않는다 (025 가 한다).
- 소개 넘기기, 2회차 이후의 소개, 한글 문구, 저장·불러오기를 만들지 않는다.
- Core 에서 `UnityEngine` 을 쓰지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 고치지 않는다.

## 보고서

> 사후 복원 (2026-11-17, 루프 리뷰): 구현 에이전트가 쓴 보고서가 줄바꿈이 `\n` 글자로 들어가 "하지 말 것" 줄에 붙는 바람에, 루프가 양식 칸 변경으로 보고 되돌리면서 함께 지워졌다. 아래는 `.loop/logs/023_stage_intro_begin_attempt1_codex.out.txt` 에 남은 원문을 그대로 옮긴 것이다. 이 작업이 남긴 Core 주석의 한글 깨짐은 main 의 후속 커밋에서 고쳤다.

### 한 일
- 첫 회차의 미확인 스테이지에서 Begin 시 소개 상태로 들어가고 전투를 멈추며 본 소개 번호를 갱신한다.
- 소개 문구와 공개 상태를 추가하고 생성자 범위 보정, 이벤트 순서, 전투·강화 동작을 테스트했다. 소개 시간 진행과 전환 중 소개 판단은 024의 범위로 남겼다.

### 바뀐 파일
- Assets/Scripts/Core/Stage/StagePhase.cs
- Assets/Scripts/Core/Stage/StageProgress.cs
- Assets/Scripts/Core/Stage/StageText.cs
- Assets/Tests/EditMode/Stage/StageProgressIntroTests.cs 및 .meta
- Assets/Tests/EditMode/Stage/StageTextTests.cs
- tasks/doing/023_stage_intro_begin.md, .loop/result.json

### 가정
없음. 공개 연결 지점은 사양과 일치한다.

### 사람이 확인할 것
없음.

### 테스트 결과
- python tools/loop/run_unity_tests.py: 통과 (EditMode 323/323, PlayMode 2/2).
- git diff --check: 통과.

## 루프 기록 (2026-11-17 12:03)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/023_stage_intro_begin
- 루프 브랜치: loop/20261117-115917
