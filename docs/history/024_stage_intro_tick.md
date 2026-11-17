# 024_stage_intro_tick — 소개 시간과 스테이지 전환 뒤의 소개

## 목표
`PlumSword.Core` 의 `StageProgress.Tick` 이 소개 시간(3초)을 세어 소개를 끝내고 전투를 다시 움직이게 한다. 가려진 순간의 교체에서도 소개 판단을 해, 1회차에서 처음 들어가는 스테이지면 페이드인이 끝난 뒤 소개를 보여 준다. 에딧 모드 테스트로 검사한다.

## 묶음
스테이지소개

## 선행 작업
- 023_stage_intro_begin

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("순수 C# 규칙이 지켜지는 방식" 절의 경과 시간)
- docs/spec/stage.md ("동작" 절의 "소개 판단", "시간", "`Changed` 가 가는 때", "경계 상황" 표의 소개 관련 줄, "수치" 절의 소개 항목, "저장" 절, "연결 지점" 절, "변경 기록"의 "2026-11-17 — 스테이지 소개")
- docs/spec/combat.md ("연결 지점" 절의 `Battle.SetPaused`, `Step`)
- docs/spec/stat_upgrade.md ("연결 지점" 절의 `SetLocked`, `Locked`)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-17 — 스테이지 소개` 항목 중 `Tick` 과 가려진 순간의 교체 부분만 구현한다. 상태·생성자·`Begin` 의 소개·`IntroLine` 은 023 이 이미 만들었다. 화면과 `GameRoot` 는 025 가 한다.
- 고칠 파일: `Assets/Scripts/Core/Stage/StageProgress.cs` (네임스페이스 `PlumSword.Core.Stage`).
- 새 테스트는 023 이 만든 `Assets/Tests/EditMode/Stage/StageProgressIntroTests.cs` 의 `StageProgressIntroTests` 클래스에 새 테스트 메서드로 더한다 (파일이 너무 길면 같은 폴더에 `StageProgressIntroTickTests.cs`, 클래스 `StageProgressIntroTickTests` 를 새로 만들어도 된다). 023 의 테스트는 지우거나 약하게 고치지 않는다. 단 하나의 예외: `StageProgressIntroTests.Intro_BattleStepAndTick_LeaveBattleAndPhaseUnchanged` 의 `stage.Tick(10.0)` 은 `stage.Tick(2.9)` 로 바꾼다 (이름과 다른 검사는 그대로 둔다. 아래 `## 답변` 참고). 테스트용 난수는 `MinRandom` 을 쓴다.
- 선행 작업이 만든 것을 쓴다.
  - 023_stage_intro_begin: `StagePhase.Intro`(= 3), `StageProgress.IntroSeconds`(3.0, 임시값), 생성자의 `int seenIntroStage = 5`, `int SeenIntroStage`, `bool IsIntroShowing`, 소개 판단(`private` 메서드: `RoundNumber` 가 1이고 `DisplayStageNumber > SeenIntroStage` 면 보여 주기로 하고 `SeenIntroStage = DisplayStageNumber`), 소개 시간(`private double` 필드). `Begin()` 은 소개를 보여 주기로 했으면 `Phase = Intro`, 소개 시간 0, `battle.SetPaused(true)` 를 이미 한다.
  - 이미 있는 `Tick`: 인자 검사(음수·`NaN`·무한대 → `ArgumentOutOfRangeException`, 상태와 상관없이), `HasBegun` 이 `false` 이거나 `Playing` 이면 아무것도 하지 않음, 페이드아웃 → 가려진 순간의 교체 → 페이드인 → 전환 끝. 페이드아웃의 남은 시간은 페이드인으로 넘긴다.
- **`Tick(deltaSeconds)`** 를 사양 "동작"의 "시간" 그대로 고친다. `HasBegun` 이 `false` 이거나 `Phase` 가 `Playing` 이면 아무것도 하지 않는다. 아니면 지금 단계의 시간(`FadingOut`·`FadingIn` 이면 페이드 시간, `Intro` 면 소개 시간)에 `deltaSeconds` 를 더하고 아래를 순서대로 본다.
  1. `Phase` 가 `FadingOut` 이고 페이드 시간이 `0.5 − 1e-9` 이상이면 **가려진 순간의 교체**: `StageNumber = 기억해 둔 다음 전체 번호` → "스테이지 배치" → **"소개 판단"**(새로 더함. 결과를 `private` 필드에 기억해 둔다) → 페이드 시간에서 0.5를 뺀다 (0보다 작아지면 0) → `Phase = FadingIn` → `StageStarted(StageNumber)` → `Changed`. 그래서 `StageStarted` 를 받는 시점에 `SeenIntroStage` 는 이미 올라가 있다.
  2. `Phase` 가 `FadingIn` 이고 페이드 시간이 `0.5 − 1e-9` 이상이면 **페이드인을 끝낸다**: 페이드 시간 0 → `upgrades.SetLocked(false)` → 1번에서 소개를 보여 주기로 했으면 `Phase = Intro`, 소개 시간 0 (전투는 멈춘 채 둔다). 아니면 `Phase = Playing`, `battle.SetPaused(false)` (지금과 같다). 어느 쪽이든 `Changed` 를 한 번 보낸다. **페이드인 뒤 남은 시간은 소개로 넘기지 않고 버린다.** 그래서 소개는 언제나 0초부터 3초를 센다.
  3. `Phase` 가 `Intro` 이고 소개 시간이 `3.0 − 1e-9` 이상이면 **소개를 끝낸다**: `Phase = Playing` → 소개 시간 0 → `battle.SetPaused(false)` → `Changed`. 남은 시간은 버린다.
  - 한 번의 `Tick` 에서 1번과 2번이 함께 일어날 수 있다. 3번은 소개로 들어간 뒤의 `Tick` 에서만 일어난다 (2번에서 소개로 들어간 같은 `Tick` 에서는 소개 시간이 0 이므로 끝나지 않는다). 예: 소개가 없는 전환의 `FadingOut` 시작에서 `Tick(1.0)` → `Playing`. 소개가 있는 전환에서 `Tick(4.0)` → 소개 시간 0 의 `Intro`.
  - `Begin()` 으로 들어간 `Intro` 도 같은 3번 규칙으로 끝난다.
  - 3.0 은 `IntroSeconds`, 1e-9 는 소개 비교의 허용 오차다.
- `Changed` 가 가는 때에 이 작업이 더하는 것: 소개가 끝날 때 1번. 페이드인이 끝날 때는 소개로 들어가든 `Playing` 이 되든 1번이다. `Tick` 이 단계를 바꾸지 않을 때는 보내지 않는다.
- 실패 후 재도전(같은 번호)과 2회차부터는 소개 판단이 "보여 주지 않음"이 되어 지금처럼 페이드인이 끝나면 바로 `Playing` 이다.
- **사양 초안과 다른 점**: 사양 "완료 조건 초안" 46번의 마지막 문장 "이어서 1칸 진행하면 금화는 10이다"는 맞지 않는다 (공격력 10 으로 체력 30 인 1스테이지 일반 상대를 한 번에 쓰러뜨릴 수 없다). 이 작업은 아래 완료 조건(적의 체력이 20 이 된다)을 따른다. 사양 문서의 그 문장은 고치지 않는다.
- **기존 테스트**: 바뀌는 것이 없다. 생성자의 기본값 `seenIntroStage = 5` 에서는 소개 판단이 늘 "보여 주지 않음"이라 기존 페이드 테스트(`StageProgressFadeTests` 등)는 이름·본문·기대값을 그대로 두고 통과해야 한다. 기존 테스트를 지우거나 이름을 바꾸거나 검사를 줄이지 않는다. (예외는 위의 `Tick(10.0)` → `Tick(2.9)` 하나뿐이다.)
- 경과 시간은 `Tick` 의 인자로만 받는다. 저장은 하지 않는다.

## 완료 조건
따로 적지 않으면 `wallet = new Wallet(0)`, `upgrades = new StatUpgrades(wallet, 공격력, 최대 체력, 1.0)`(항목 앞의 `(공격력, 최대 체력)`), `battle = new Battle(upgrades)`, `stage = new StageProgress(wallet, upgrades, battle, new MinRandom(), startStage, seenIntroStage)` 로 만든 것이다 (`startStage` 를 적지 않으면 1). "1칸 진행"은 `battle.Step()` 을 한 번 부르는 것이다. 알림 횟수는 `StageProgress` 를 만든 직후 구독해 센 누적 횟수다.

"소개 뒤 성공 상태"는 (1000, 100), `seenIntroStage: 0` 으로 `Begin()` → `Tick(3.0)` → `battle.AddEnemy(EnemyKind.Boss, 1, 1.0)` → 1칸 진행한 상태다. 이때 `StageCleared` 가 1을 담아 1번 왔고 `Phase` 는 `FadingOut` 이다.

- `Assets/Tests/EditMode/Stage/` 의 `StageProgressIntroTests.cs`(또는 `StageProgressIntroTickTests.cs`)에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- (10, 100), `seenIntroStage: 0` 으로 `Begin()` 뒤 `Tick(2.9)` 를 부르면 `Phase` 는 `Intro`, `battle.Paused` 는 `true` 다.
- 위에 이어 `Tick(0.1)` 을 부르면 `Phase` 는 `Playing`, `IsIntroShowing` 은 `false`, `battle.Paused` 는 `false` 이고 이 `Tick` 동안 `Changed` 가 1번 온다.
- (10, 100), `seenIntroStage: 0` 으로 `Begin()` 뒤 `battle.AddEnemy(EnemyKind.Normal, 1, 0.8)` 이 돌려준 적은, 1칸 진행한 뒤 `Health` 30 이고, `Tick(3.0)` 뒤 다시 1칸 진행하면 `Health` 20 이다.
- (1000, 100), `seenIntroStage: 0` 으로 `Begin()` 뒤 `Tick(3.0)` 을 부르면 `Phase` 는 `Playing` 이다.
- 소개 뒤 성공 상태에서 `Tick(0.5)` 를 부르면 `StageNumber` 2, `Phase` `FadingIn`, `SeenIntroStage` 2 이고 `StageStarted` 가 2를 담아 1번 더 오며, `StageStarted` 를 받는 시점에 읽은 `SeenIntroStage` 는 2 다.
- 위에 이어 `Tick(0.5)` 를 부르면 `Phase` `Intro`, `battle.Paused` `true`, `upgrades.Locked` `false`, `FadeAlpha` 0.0 이다.
- 위에 이어 `Tick(3.0)` 을 부르면 `Phase` `Playing`, `battle.Paused` `false` 다.
- 소개 뒤 성공 상태에서 `Tick(1.5)` 를 부르면 `Phase` 는 `Intro`, `StageNumber` 는 2 다.
- 위에 이어 `Tick(2.9)` 를 부르면 `Phase` 는 `Intro` 그대로이고, 다시 `Tick(0.1)` 을 부르면 `Phase` 는 `Playing` 이다.
- 소개 뒤 성공 상태에서 `Tick(4.0)` 한 번을 부르면 `Phase` `Intro`, `IsIntroShowing` `true`, `battle.Paused` `true` 이고 `StageStarted` 가 2를 담아 1번 더 온다. 이어서 `Tick(3.0)` 을 부르면 `Phase` `Playing`, `battle.Paused` `false` 다.
- (10, 5), `seenIntroStage: 0` 으로 `Begin()` → `Tick(3.0)` → `battle.AddEnemy(EnemyKind.Normal, 1, 0.8)` → 1칸 진행하면 `StageFailed` 가 1을 담아 1번 온다. 이어서 `Tick(1.0)` 을 부르면 `StageNumber` 1, `Phase` `Playing`, `battle.Paused` `false`, `upgrades.Locked` `false`, `SeenIntroStage` 1 이고 `StageStarted` 가 1을 담아 한 번 더 온다 (모두 2번).
- (1000, 100), `startStage: 5, seenIntroStage: 4` 로 `Begin()` 을 부르면 `Phase` `Intro`, `SeenIntroStage` 5 다. 이어서 `Tick(3.0)` → `battle.AddEnemy(EnemyKind.Boss, 5, 1.0)` → 1칸 진행하면 `StageCleared` 가 5를 담아 1번 오고, 이어서 `Tick(1.0)` 을 부르면 `StageNumber` 6, `RoundNumber` 2, `Phase` `Playing`, `battle.Paused` `false`, `SeenIntroStage` 5 다.
- 소개 뒤 성공 상태에서 `Tick(0.5)` 직후(페이드인 중) 읽은 `StageNumber`(2)와 `SeenIntroStage`(2)로 새 지갑·강화 (1000, 100)·전투·`MinRandom` 과 새 스테이지를 만들어 `Begin()` 을 부르면 `Phase` 는 `Playing`, `SeenIntroStage` 는 2 다.
- 소개 뒤 성공 상태에서 `Tick(0.5)`, `Tick(0.5)`, `Tick(1.0)` 을 부른 때(소개 도중) 읽은 값은 `StageNumber` 2, `SeenIntroStage` 2 이고, 그 값으로 위와 같은 방법으로 새 스테이지를 만들어 `Begin()` 을 부르면 `Phase` 는 `Playing` 이다.
- 기존 테스트가 모두 통과한다. (`StageProgressTests`, `StageProgressKillTests`, `StageProgressOutcomeTests`, `StageProgressFadeTests`, `StageProgressRoundTests`, 023 의 `StageProgressIntroTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `StagePhase` 값, 생성자 모양, `Begin()` 의 소개 판단 규칙을 바꾸지 않는다 (023 이 정했다).
- 페이드 시간(0.5초)·페이드아웃의 남은 시간 넘기기·`FadeAlpha` 규칙을 바꾸지 않는다.
- `Assets/Scripts/Core/Combat/`, `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/` 를 고치지 않는다.
- `Assets/Scripts/Game/` 를 고치지 않는다 (025 가 한다).
- 사양 문서의 완료 조건 초안을 고치지 않는다.
- 소개 넘기기, 2회차 이후의 소개, 저장·불러오기를 만들지 않는다.
- Core 에서 `UnityEngine` 을 쓰지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 고치지 않는다.



## 보류 사유 (2026-11-17 12:06)

- 사유: ?? 023 ???? Intro?? Tick(10.0) ??? Intro? Changed ?? ??? ?????, ?? ??? 3? ? Playing ??? Changed ??? ???? ?? ??? ??? ????. ?? EditMode 332? ? ?? ??? 1? ??.
- 질문: ?? Intro_BattleStepAndTick_LeaveBattleAndPhaseUnchanged ???? ?? 3? ?? ??? ?? ???? ????
- 추천안: ?? ????? Tick? 3? ???? ??? ?? ? ?? ??? ????, ? ???? 3? ??? ????.
- 기능 브랜치: task/024_stage_intro_tick (작업 중 상태 커밋됨)
- 작업 에이전트: codex(시도 1)


## 답변 (2026-11-17 12:29)

- 보류 종류: 에이전트의 질문 (2026-11-17 12:06 보류 사유에 대한 답. 보류 사유의 한글은 인코딩 문제로 '?'로 깨져 있다. 내용은 "023 의 테스트 `Intro_BattleStepAndTick_LeaveBattleAndPhaseUnchanged` 가 소개 중 `Tick(10.0)` 뒤에도 `Phase` 가 `Intro` 이고 `Changed` 가 1번이라고 검사해서, 3초 뒤 소개를 끝내는 이 작업과 충돌한다. EditMode 332개 중 그 1개만 실패한다"였다)
- 결정: 그 023 테스트에서 `stage.Tick(10.0)` 한 줄만 `stage.Tick(2.9)` 로 바꾼다. 테스트 이름과 나머지 검사(Assert)는 하나도 바꾸지 않는다.
- 이유: 023 의 `Tick(10.0)` 은 "소개 시간 세기가 아직 없는 023 단계에서 Intro 중 Tick 이 단계를 바꾸지 않는다"를 확인하려던 것이라, 3초 뒤 소개를 끝내는 이 작업의 사양과 처음부터 맞을 수 없다. 3초 미만(2.9초)으로 바꾸면 "소개 도중에는 전투·금화·처치 수·단계가 그대로이고 `Changed` 도 더 오지 않는다"는 원래 뜻이 그대로 남고, 검사 수도 줄지 않는다. 3초 뒤 끝나는 것은 이 작업의 새 테스트가 확인한다. 사람(사용자)이 이 변경을 승인했다. 검사를 약하게 만든 것으로 보지 않는다.
- 할 일:
  - `Assets/Tests/EditMode/Stage/StageProgressIntroTests.cs` 의 `Intro_BattleStepAndTick_LeaveBattleAndPhaseUnchanged` 에서 `Assert.DoesNotThrow(() => stage.Tick(10.0));` 를 `Assert.DoesNotThrow(() => stage.Tick(2.9));` 로 바꾼다. 다른 줄은 건드리지 않는다.
  - 기능 브랜치의 `StageProgress.cs` 구현(`_showIntroAfterFade`, 소개 시간 세기, 소개 끝내기)과 새 테스트는 그대로 살려서 이어 간다. 전체 테스트를 다시 돌려 모두 통과하는지 확인한다.
  - 보고서는 작업 문서 끝에 **실제 줄바꿈으로** 덧붙인다. 한글을 PowerShell 을 거쳐 python 등으로 넘기면 '?'로 깨지고, `\n` 글자를 그대로 쓰면 루프가 양식 칸을 고친 것으로 보고 보고서까지 지운다 (023 과 이번 보류에서 실제로 일어났다). 파일을 직접 편집하는 도구로 UTF-8 그대로 쓴다. `.loop/result.json` 도 UTF-8 로 쓴다.
  - 보고서의 `### 가정` 에 "023 테스트의 `Tick(10.0)` 을 사용자 승인에 따라 `Tick(2.9)` 로 바꿨다"를 적는다.
- 바꾼 칸: 요구사항 — "023 의 테스트는 지우거나 약하게 고치지 않는다" 뒤에 위 테스트의 `Tick(10.0)` → `Tick(2.9)` 예외 한 가지를 더함. 요구사항의 "기존 테스트" 항목 끝에 "(예외는 위의 `Tick(10.0)` → `Tick(2.9)` 하나뿐이다.)"를 더함.
- 기능 브랜치: task/024_stage_intro_tick 의 작업 중 커밋(b7219d4)은 살려서 이어 간다. main 에 들어간 `StageProgress.cs` 주석 복원 커밋(e5f85ae)과 시험 병합해 보니 충돌하지 않는다.

## 보고서

### 한 일
- `StageProgress.Tick` 이 `Intro` 에서 소개 시간을 세고, 3초(`IntroSeconds − 1e-9`) 이상이면 `Phase = Playing` → 소개 시간 0 → `battle.SetPaused(false)` → `Changed` 로 소개를 끝낸다. `Begin()` 으로 들어간 소개도 같은 규칙으로 끝난다.
- 가려진 순간의 교체에서 "소개 판단"을 하고 결과를 `_showIntroAfterFade` 에 기억한다 (`StageStarted` 보다 먼저라 그 시점에 `SeenIntroStage` 는 이미 올라가 있다). 페이드인이 끝날 때 그 결과가 참이면 `Intro`(소개 시간 0, 전투는 멈춘 채), 아니면 지금처럼 `Playing` 이 된다. 어느 쪽이든 `Changed` 1번, 페이드인 뒤 남은 시간은 버린다.
- 이전 시도(codex)의 작업 중 커밋 구현을 살리고 주석만 다듬었다.
- 답변대로 023 테스트 `Intro_BattleStepAndTick_LeaveBattleAndPhaseUnchanged` 의 `stage.Tick(10.0)` 한 줄만 `stage.Tick(2.9)` 로 바꿨다.
- 이전 시도가 023 의 `StageProgressIntroTests.cs` 안에 둘째 클래스로 더했던 새 테스트를 요구사항의 허용안대로 새 파일 `StageProgressIntroTickTests.cs`(클래스 `StageProgressIntroTickTests`)로 옮기고, 완료 조건마다 테스트가 맞게 나눴다 (48번 `Tick(3.0)` → `Playing`, 54번 `Tick(4.0)` 뒤 `Tick(3.0)` 을 따로 검사).

### 바뀐 파일
- `Assets/Scripts/Core/Stage/StageProgress.cs` — 소개 시간 세기, 교체 때 소개 판단, 페이드인 끝의 소개 진입, 소개 끝.
- `Assets/Tests/EditMode/Stage/StageProgressIntroTests.cs` — `Tick(10.0)` → `Tick(2.9)` 한 줄 (사용자 승인).
- `Assets/Tests/EditMode/Stage/StageProgressIntroTickTests.cs` (+ `.meta`) — 새 테스트 10개 (TestCase 포함).

### 가정
- 023 테스트의 `Tick(10.0)` 을 사용자 승인에 따라 `Tick(2.9)` 로 바꿨다.
- 새 테스트는 023 파일에 더하지 않고 요구사항이 허용한 새 파일 `StageProgressIntroTickTests.cs` 에 두었다 (023 파일을 한 줄 외에는 그대로 두려고).
- 소개 비교의 허용 오차는 페이드와 같은 상수 `FadeTolerance`(1e-9)를 함께 쓴다. 사양의 값과 같다.
- 사양(`docs/spec/stage.md`)의 "시간"·"연결 지점"이 이미 이 동작대로 적혀 있어 사양 문서는 고치지 않았다. 공개 연결 지점 모양은 바뀌지 않았다.

### 사람이 확인할 것
- 완료 조건에 [사람 확인] 항목은 없다.
- 화면에서 소개 패널이 3초 보이고 닫히는 것은 025(화면·`GameRoot`)가 끝난 뒤 확인한다.

### 테스트 결과
- `python tools/loop/run_unity_tests.py --project .`: EditMode 333개 중 333개 통과, PlayMode 2개 중 2개 통과.


## 루프 기록 (2026-11-17 12:34)

- 작업 에이전트: claude(시도 1)
- 리뷰: codex (교차) 통과
- 기능 브랜치: task/024_stage_intro_tick
- 루프 브랜치: loop/20261117-123053
