# 018_stage_progress_fade — 페이드 시간과 스테이지 교체

## 목표
`PlumSword.Core` 의 `StageProgress` 에 페이드 전환을 더해 스테이지 로직을 완성한다. `Tick` 이 페이드 시간을 세어, 화면이 다 가려진 순간에 다음 스테이지(성공) 또는 같은 스테이지(실패)를 새로 배치하고, 페이드인이 끝나면 전투 멈춤과 강화 잠금을 푼다. 페이드 막의 진하기 `FadeAlpha` 도 만든다. 에딧 모드 테스트로 검사한다.

## 묶음
스테이지

## 선행 작업
- 004_stat_upgrades
- 010_battle_upgrade_reentry
- 017_stage_progress_outcome

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("순수 C# 규칙이 지켜지는 방식" 절의 경과 시간)
- docs/spec/stage.md ("동작" 절의 "전환 시작", "시간", "페이드 막의 진하기", "`Changed` 가 가는 때", "경계 상황" 표, "수치" 절의 페이드 항목, "연결 지점" 절)
- docs/spec/combat.md ("동작" 절의 "초기화", "멈춤", "연결 지점" 절)
- docs/spec/stat_upgrade.md ("연결 지점" 절의 `SetLocked`, `TryUpgrade`)

## 요구사항
- 고칠 파일: `Assets/Scripts/Core/Stage/StageProgress.cs` (네임스페이스 `PlumSword.Core.Stage`).
- 새 테스트 파일: `Assets/Tests/EditMode/Stage/StageProgressFadeTests.cs` (네임스페이스 `PlumSword.Core.Tests.Stage`, 클래스 `StageProgressFadeTests`). 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다. 앞 작업의 `StageProgressTests.cs`, `StageProgressKillTests.cs`, `StageProgressOutcomeTests.cs` 의 테스트는 지우거나 약하게 고치지 않는다.
- 선행 작업이 만든 것을 쓴다.
  - 017_stage_progress_outcome 까지: `StageProgress` 는 보스 처치(성공)나 제자 패배(실패) 때 "전환 시작"을 한다 — 다음 번호(성공이면 `StageNumber + 1`, `int.MaxValue` 면 그대로. 실패면 `StageNumber`)를 `private` 필드에 기억하고, `Phase = FadingOut`, 페이드 시간(`private double` 필드) 0, `battle.SetPaused(true)`, `upgrades.SetLocked(true)`. `Phase` 가 `Playing` 이 아닌 동안 전투의 알림은 무시된다. "스테이지 배치"(`battle.Reset()` → `StageLayout.RollGroups(random)` → 좌표마다 `battle.AddEnemy(EnemyKind.Normal, StageNumber, 좌표)` 10번 → `DefeatedNormalCount = 0`, `Boss = null`)는 `Begin()` 이 쓰는 `private` 메서드로 있다. `Tick` 은 인자 검사(음수·`NaN`·무한대 → `ArgumentOutOfRangeException`)만 하고 있다.
  - 010_battle_upgrade_reentry 까지: `Battle.Reset()` 은 제자를 좌표 0, 체력 가득(`upgrades.MaxHealth`), 패배 아님으로 되돌리고 모든 적을 없앤다. `Paused` 는 바꾸지 않는다. `Battle.SetPaused(false)` 로 멈춤을 푼다.
  - 004_stat_upgrades: `StatUpgrades.SetLocked(false)` 로 잠금을 푼다. `TryUpgrade(StatType.Attack)` 의 첫 비용은 20 이다.
- 공개 모양은 바꾸지 않는다 (사양 `docs/spec/stage.md` 의 "연결 지점" 절 그대로다). 구현하다 모양이 사양과 달라지면 사양 문서의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- **`Tick(deltaSeconds)`**:
  - 음수, `NaN`, 무한대면 `System.ArgumentOutOfRangeException` (상태와 상관없이. 이미 있다).
  - `HasBegun` 이 `false` 이거나 `Phase` 가 `Playing` 이면 아무것도 하지 않는다.
  - 아니면 페이드 시간에 `deltaSeconds` 를 더하고 아래를 순서대로 본다. `Tick(0)` 은 더해도 값이 같아 아무 일도 없다.
    1. `Phase` 가 `FadingOut` 이고 페이드 시간이 `0.5 − 1e-9` 이상이면 **가려진 순간의 교체**를 한다. 순서대로: `StageNumber = 기억해 둔 다음 번호`, "스테이지 배치", 페이드 시간에서 0.5 를 뺀다 (0보다 작아지면 0), `Phase = FadingIn`, `StageStarted(StageNumber)`, `Changed`.
    2. `Phase` 가 `FadingIn` 이고 페이드 시간이 `0.5 − 1e-9` 이상이면 **전환을 끝낸다**. 순서대로: `Phase = Playing`, 페이드 시간 0, `battle.SetPaused(false)`, `upgrades.SetLocked(false)`, `Changed`.
  - 한 번의 `Tick` 에서 1번과 2번이 모두 일어날 수 있다 (예: `FadingOut` 시작에서 `Tick(1.0)` → `Playing` 까지 간다). 남은 시간은 다음 단계로 넘긴다 (예: `Tick(0.3)` 두 번 → 남은 0.1초가 페이드인으로 넘어간다).
  - 0.5 는 `StageProgress.FadeSeconds` (임시값), 1e-9 는 페이드 비교의 허용 오차다. 페이드아웃 0.5초, 페이드인 0.5초다.
- **`FadeAlpha`** (페이드 막의 진하기, 0.0 ~ 1.0): `Playing` 이면 0. `FadingOut` 이면 `Math.Min(1.0, 페이드 시간 / 0.5)`. `FadingIn` 이면 `Math.Max(0.0, 1.0 − 페이드 시간 / 0.5)`.
- 교체 때 `battle.Reset()` 이 제자의 체력을 전부 회복하고 좌표를 0으로 되돌린다. 금화와 강화 단계는 그대로다. 실패 뒤 재도전에서 다시 처치한 몬스터는 다시 금화를 준다.
- 교체 뒤에도 `FadingIn` 동안은 전투가 멈춰 있고 강화가 잠겨 있다. 전환이 끝나 `Playing` 이 되면 전투의 알림을 다시 처리한다.
- `Changed` 가 가는 때에 이 작업이 더하는 것: 가려진 순간의 교체(`StageStarted` 뒤 1번), 전환이 끝날 때 1번. `Tick` 이 아무것도 바꾸지 않을 때는 보내지 않는다.
- 경과 시간은 `Tick` 의 인자로만 받는다 (`DateTime`, `Stopwatch` 를 쓰지 않는다). 난수는 생성자로 받은 것만 쓴다. 저장은 하지 않는다.

## 완료 조건
항목 앞의 `(공격력, 최대 체력)` 은 `upgrades = new StatUpgrades(wallet, 공격력, 최대 체력, 1.0)` 의 값이다. 따로 적지 않으면 `wallet = new Wallet(0)`, `battle = new Battle(upgrades)`, `stage = new StageProgress(wallet, upgrades, battle, new MinRandom())` 로 만든 것이다. "n칸 진행"은 `battle.Step()` 을 n번 부르는 것이다. 금화는 `wallet.Balance` 다. 알림 횟수는 `StageProgress` 를 만든 직후 구독해 센 누적 횟수다. 좌표·비율은 오차 1e-6 안에서 비교한다.

"성공 상태"는 (1000, 100) 으로 `stage.Begin()` 뒤 `battle.AddEnemy(EnemyKind.Boss, 1, 1.0)` 을 부르고 1칸 진행한 상태다. 이때 금화 50, `Phase` 는 `FadingOut`, `StageNumber` 는 1, `battle.DiscipleHealth` 는 90, `StageStarted` 는 1번(값 1) 온 상태다. "실패 상태"는 (10, 5) 로 `stage.Begin()` 뒤 `battle.AddEnemy(EnemyKind.Normal, 1, 0.8)` 을 부르고 1칸 진행한 상태다. 이때 금화 0, `Phase` 는 `FadingOut`, `battle.IsDiscipleDefeated` 는 `true` 다.

- `Assets/Tests/EditMode/Stage/StageProgressFadeTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 성공 상태에서 `Tick(0.25)` 를 부르면 `FadeAlpha` 는 0.5, `Phase` 는 `FadingOut`, `StageNumber` 는 1 이다. 한 번 더 `Tick(0.25)` 를 부르면 `StageNumber` 는 2, `Phase` 는 `FadingIn`, `FadeAlpha` 는 1.0, `DefeatedNormalCount` 는 0, `Boss` 는 `null`, `BarRatio` 는 0.0, `battle.Enemies.Count` 는 10, 적의 `MaxHealth` 는 모두 40, `Position` 은 6.0 부터 3.0 씩 33.0 까지, `battle.DisciplePosition` 은 0, `battle.DiscipleHealth` 는 100, `battle.Paused` 와 `upgrades.Locked` 는 `true`, 금화는 50 이고, `StageStarted` 가 2를 담아 1번 더 온다.
- 위에 이어 (`Tick(0.25)` 를 2번 부른 뒤) `Tick(0.25)` 를 부르면 `FadeAlpha` 는 0.5, `Phase` 는 `FadingIn` 이다. 한 번 더 `Tick(0.25)` 를 부르면 `Phase` 는 `Playing`, `FadeAlpha` 는 0.0, `battle.Paused` 와 `upgrades.Locked` 는 `false` 이고, `StageStarted` 는 모두 2번(값 1, 2)뿐이다.
- 실패 상태에서 `Tick(0.5)` 를 부르면 `StageNumber` 는 1, `Phase` 는 `FadingIn`, `battle.Enemies.Count` 는 10, `battle.DiscipleHealth` 는 5, `battle.IsDiscipleDefeated` 는 `false` 이고, `StageStarted` 가 1을 담아 한 번 더 온다 (모두 2번). 이어서 `Tick(0.5)` 를 부르면 `Phase` 는 `Playing` 이고 `battle.Paused` 와 `upgrades.Locked` 는 `false` 다.
- 성공 상태에서 `Tick(0.3)` 을 2번 부르면 `Phase` 는 `FadingIn`, `StageNumber` 는 2, `FadeAlpha` 는 0.8 이다.
- 성공 상태에서 `Tick(2.0)` 을 부르면 `Phase` 는 `Playing`, `StageNumber` 는 2, `FadeAlpha` 는 0.0, `battle.Paused` 와 `upgrades.Locked` 는 `false` 이고, 이 `Tick` 동안 `StageStarted` 가 2를 담아 1번, `Changed` 가 2번 오며 첫 `Changed` 는 `StageStarted` 뒤에 온다.
- 성공 상태에서 `Tick(0)` 을 부르면 `Phase` 는 `FadingOut`, `FadeAlpha` 는 0.0, `StageNumber` 는 1 이고 알림은 더 오지 않는다.
- 성공 상태에서 (금화 50) `upgrades.TryUpgrade(StatType.Attack)` 은 `false` 이고 금화는 50 그대로다. `Tick(1.0)` 뒤에는 `true` 이고 금화는 30 이다.
- 성공 상태에서 `Tick(1.0)` 을 부른 뒤 (`StageNumber` 2, `Playing`) `battle.AddEnemy(EnemyKind.Normal, 2, 0.8)` 을 부르고 1칸 진행하면 금화는 62, `DefeatedNormalCount` 는 1 이다 (전환이 끝나면 처치를 다시 센다).
- `wallet = new Wallet(0)`, `upgrades = new StatUpgrades(wallet, 100000000000, 100000000000, 1.0)`, `startStage` 가 `int.MaxValue` 인 스테이지로, `Begin()` 뒤 `battle.AddEnemy(EnemyKind.Boss, int.MaxValue, 1.0)` 을 부르고 1칸 진행한 다음 `Tick(0.5)` 를 부르면, 예외 없이 `StageNumber` 는 `int.MaxValue` 그대로이고 `Phase` 는 `FadingIn`, `battle.Enemies.Count` 는 10 이다.
- 기존 테스트가 모두 통과한다. (`StageProgressTests`, `StageProgressKillTests`, `StageProgressOutcomeTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다 (`Time.deltaTime` 포함). `PlumSword.Core` 는 순수 C# 이다.
- `StageProgress` 의 공개 모양을 바꾸거나 공개 멤버를 더하지 않는다.
- 016_stage_progress_kills, 017_stage_progress_outcome 이 만든 처치·성공·실패 처리의 동작을 바꾸지 않는다.
- `Begin()` 에 페이드를 넣지 않는다. 게임을 처음 켰을 때는 페이드 없이 바로 시작한다.
- `Battle.Tick`, `Battle.Step` 을 `StageProgress` 안에서 부르지 않는다.
- 저장·불러오기, 오프라인 보상, 스테이지 선택, 보스전 제한 시간을 만들지 않는다. 사양의 "범위 밖"이다.
- `StageRewards.cs`, `StageText.cs`, `StageLayout.cs`, `StagePhase.cs` 를 고치지 않는다.
- `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/`, `Assets/Scripts/Core/Combat/` 를 고치지 않는다. `StatUpgrades`, `Battle` 은 공개된 모양으로만 쓴다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. 019_stage_view 가 한다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `StageProgress.Tick` 에 페이드 처리를 넣었다. 페이드 시간을 더한 뒤 (1) `FadingOut` 이고 `≥ 0.5 − 1e-9` 이면 가려진 순간의 교체(`StageNumber = 다음 번호` → 스테이지 배치 → 페이드 시간 −0.5(최소 0) → `FadingIn` → `StageStarted` → `Changed`), (2) `FadingIn` 이고 `≥ 0.5 − 1e-9` 이면 전환 끝(`Playing` → 페이드 시간 0 → 전투 멈춤 풀기 → 강화 잠금 풀기 → `Changed`)을 차례로 본다. 한 `Tick` 에서 둘 다 일어날 수 있고 남은 시간은 페이드인으로 넘어간다.
- `FadeAlpha` 를 사양대로 계산하게 했다 (`Playing` 0, `FadingOut` `min(1, t/0.5)`, `FadingIn` `max(0, 1 − t/0.5)`).
- 017 이 남겨 둔 `#pragma warning disable CS0414` 와 "018 이 만든다" 주석을 정리했다. 허용 오차 1e-9 를 `private const FadeTolerance` 로 두었다 (공개 모양 변화 없음).
- 완료 조건마다 에딧 모드 테스트를 `StageProgressFadeTests` 에 썼다.

### 바뀐 파일
- `Assets/Scripts/Core/Stage/StageProgress.cs` (수정)
- `Assets/Tests/EditMode/Stage/StageProgressFadeTests.cs` (새 파일. `.meta` 는 유니티 테스트 실행 때 만들어짐)

### 가정
- 허용 오차 1e-9 를 `private const double FadeTolerance` 로 두었다. 공개 멤버는 더하지 않았다.
- 사양 문서는 이미 이 동작을 담고 있어 고치지 않았다 (연결 지점 변화 없음).

### 사람이 확인할 것
- 없음 (완료 조건에 [사람 확인] 항목 없음. 화면에서 페이드가 보이는 것은 019_stage_view 에서 확인)

### 테스트 결과
- `python tools/loop/run_unity_tests.py --project .` → EditMode 269개 중 269개 통과, 0개 실패 (기존 `StageProgressTests`, `StageProgressKillTests`, `StageProgressOutcomeTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함).


## 루프 기록 (2026-11-16 21:10)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/018_stage_progress_fade
- 루프 브랜치: loop/20261116-205758
