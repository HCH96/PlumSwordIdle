# 017_stage_progress_outcome — 스테이지 성공·실패 판정과 전환 시작

## 목표
`PlumSword.Core` 의 `StageProgress` 에 성공·실패 판정을 더한다. 보스를 처치하면 보스 금화를 주고 성공, 제자가 쓰러지면 실패로 판정하고, 둘 다 "전환 시작"(페이드아웃 단계로 바꾸고 전투를 멈추고 강화를 잠금)을 한다. 페이드 시간을 세어 다음 스테이지로 넘어가는 것은 뒤 작업(018)이 한다. 에딧 모드 테스트로 검사한다.

## 묶음
스테이지

## 선행 작업
- 004_stat_upgrades
- 010_battle_upgrade_reentry
- 013_stage_rewards_text
- 016_stage_progress_kills

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stage.md ("동작" 절의 "보스가 쓰러짐", "제자가 쓰러짐", "전환 시작", "`Changed` 가 가는 때", "경계 상황" 표, "연결 지점" 절)
- docs/spec/combat.md ("동작" 절의 "동시에 쓰러짐", "멈춤", "알림 안에서 전투를 다시 부를 때", "연결 지점" 절)
- docs/spec/stat_upgrade.md ("동작" 절의 "잠금", "연결 지점" 절의 `SetLocked`, `Locked`, `TryUpgrade`)

## 요구사항
- 고칠 파일: `Assets/Scripts/Core/Stage/StageProgress.cs` (네임스페이스 `PlumSword.Core.Stage`).
- 새 테스트 파일: `Assets/Tests/EditMode/Stage/StageProgressOutcomeTests.cs` (네임스페이스 `PlumSword.Core.Tests.Stage`, 클래스 `StageProgressOutcomeTests`). 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다. 앞 작업의 `StageProgressTests.cs`, `StageProgressKillTests.cs` 의 테스트는 지우거나 약하게 고치지 않는다.
- 선행 작업이 만든 것을 쓴다.
  - 016_stage_progress_kills 까지: `StageProgress` 는 `Begin()` 으로 첫 배치를 하고, 일반 몬스터가 쓰러지면 금화(`StageRewards.NormalGold`)를 주고 `DefeatedNormalCount` 를 올리며 열 번째에 보스를 낸다 (`Boss`). `HasBegun` 이 `false` 이거나 `Phase` 가 `Playing` 이 아니면 전투의 알림을 모두 무시한다. `battle.EnemyDefeated` 의 `Kind == Boss` 쪽과 `battle.DiscipleDefeated` 의 처리 메서드는 비어 있다. 공개 모양(`StageCleared`, `StageFailed`, `Phase`, `FadeAlpha`, `Tick` 포함)은 이미 모두 선언되어 있다. `Tick` 은 인자 검사만 하고 아무것도 하지 않는다.
  - 013_stage_rewards_text: `StageRewards.BossGold(int stage)` (1스테이지 50, `int.MaxValue` 스테이지 21474836510).
  - 004_stat_upgrades: `StatUpgrades` 의 `void SetLocked(bool locked)`, `bool Locked { get; }`, `bool TryUpgrade(StatType stat)` (잠긴 동안 `false`). `StatType.Attack`.
  - 010_battle_upgrade_reentry 까지: `Battle` 의 `void SetPaused(bool paused)`, `bool Paused { get; }`, `bool IsDiscipleDefeated { get; }`. 알림 안에서 `SetPaused(true)` 를 부르면 진행 중인 칸은 끝까지 처리되고 다음 칸부터 멈춘다. 한 칸의 알림 순서는 쓰러진 적마다 `EnemyDefeated` → 제자가 패배했으면 `DiscipleDefeated` 한 번이고, `EnemyDefeated` 를 받는 시점에 `IsDiscipleDefeated` 는 이미 그 칸의 결과다.
- 공개 모양은 바꾸지 않는다 (사양 `docs/spec/stage.md` 의 "연결 지점" 절 그대로다). 구현하다 모양이 사양과 달라지면 사양 문서의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- **보스가 쓰러짐** (`battle.EnemyDefeated`, `Kind == EnemyKind.Boss`. `HasBegun` 이 `true` 이고 `Phase` 가 `Playing` 일 때만):
  - `battle.IsDiscipleDefeated` 가 `true` 면(동시에 쓰러짐) 아무것도 하지 않는다. 금화도 알림도 없다. 실패는 곧 오는 `DiscipleDefeated` 에서 처리된다.
  - 아니면 성공이다. 순서대로: `wallet.Add(StageRewards.BossGold(StageNumber))` → "전환 시작"(다음 번호 = `StageNumber + 1`. 단 `StageNumber` 가 `int.MaxValue` 면 그대로 `int.MaxValue`) → `StageCleared(StageNumber)` → `Changed`.
  - 쓰러진 적이 `Boss` 속성의 그 객체인지는 가리지 않는다. 처치 수가 10이 되기 전이라도 `Kind` 가 `Boss` 인 적이 쓰러지면 똑같이 성공이다.
- **제자가 쓰러짐** (`battle.DiscipleDefeated`. `HasBegun` 이 `true` 이고 `Phase` 가 `Playing` 일 때만): 실패다. 순서대로: "전환 시작"(다음 번호 = `StageNumber`) → `StageFailed(StageNumber)` → `Changed`.
- **전환 시작** (`private` 메서드): 다음 번호를 `private` 필드에 기억해 두고, `Phase = StagePhase.FadingOut`, 페이드 시간(`private double` 필드) 0, `battle.SetPaused(true)`, `upgrades.SetLocked(true)`. 018_stage_progress_fade 가 이 두 필드를 읽어 교체를 한다.
  - `StageCleared`·`StageFailed` 를 받는 시점에 `Phase` 는 이미 `FadingOut` 이고 `StageNumber` 는 아직 끝난 스테이지의 번호다.
  - 전환 시작 뒤에는 `Phase` 가 `Playing` 이 아니므로 전투의 알림을 모두 무시한다 (같은 칸에 뒤따라 오는 알림 포함. 예: 성공 처리 뒤 같은 칸의 `DiscipleDefeated` 는 올 수 없지만 와도 무시된다).
- **`FadeAlpha`**: 이 작업에서는 `FadingOut` 이 된 직후의 값 0.0 만 맞으면 된다 (페이드 시간이 0이다). 페이드 시간에 따른 값은 018_stage_progress_fade 가 만든다.
- `Tick` 은 이 작업에서 고치지 않는다. 그래서 이 작업이 끝난 시점에는 성공·실패 뒤 `Phase` 가 `FadingOut` 에 머문다. 정상이다.
- 성공해도 실패해도 금화와 강화 단계는 그대로다.
- 시간·난수·저장은 이 작업에서 새로 쓰지 않는다.

## 완료 조건
항목 앞의 `(공격력, 최대 체력)` 은 `upgrades = new StatUpgrades(wallet, 공격력, 최대 체력, 1.0)` 의 값이다. 따로 적지 않으면 `wallet = new Wallet(0)`, `battle = new Battle(upgrades)`, `stage = new StageProgress(wallet, upgrades, battle, new MinRandom())` 로 만든 것이다. "시작한 뒤"는 `stage.Begin()` 을 부른 뒤, "n칸 진행"은 `battle.Step()` 을 n번 부르는 것, "적을 직접 넣는다"는 테스트가 `battle.AddEnemy` 를 부르는 것이다. 금화는 `wallet.Balance` 다. 알림 횟수는 `StageProgress` 를 만든 직후 구독해 센 누적 횟수다.

- `Assets/Tests/EditMode/Stage/StageProgressOutcomeTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- **(성공 상태)** (1000, 100) 시작한 뒤 `Boss` 적 하나를 1스테이지, 좌표 1.0 에 직접 넣고 1칸 진행하면, 금화는 50, `StageCleared` 가 1을 담아 1번, `StageFailed` 는 0번 온다. `Phase` 는 `FadingOut`, `StageNumber` 는 1, `FadeAlpha` 는 0.0, `battle.Paused` 는 `true`, `upgrades.Locked` 는 `true`, `battle.DiscipleHealth` 는 90 이다. `StageCleared` 를 받는 시점에 읽은 `Phase` 는 `FadingOut`, `StageNumber` 는 1 이고, `Changed` 는 `StageCleared` 뒤에 온다.
- **(실패 상태)** (10, 5) 시작한 뒤 `Normal` 적 하나를 1스테이지, 좌표 0.8 에 직접 넣고 1칸 진행하면, 금화는 0, `DefeatedNormalCount` 는 0, `StageFailed` 가 1을 담아 1번, `StageCleared` 는 0번 온다. `Phase` 는 `FadingOut`, `battle.Paused` 는 `true`, `upgrades.Locked` 는 `true` 다. `StageFailed` 를 받는 시점에 읽은 `Phase` 는 `FadingOut` 이고, `Changed` 는 `StageFailed` 뒤에 온다.
- (30, 5) 시작한 뒤 `Normal` 적 하나를 1스테이지, 좌표 0.8 에 직접 넣고 1칸 진행하면 (제자와 일반 몬스터가 함께 쓰러짐), 금화는 10, `DefeatedNormalCount` 는 1, `StageFailed` 가 1번, `StageCleared` 는 0번 온다.
- (100, 10) 시작한 뒤 `Boss` 적 하나를 1스테이지, 좌표 1.0 에 직접 넣고 1칸 진행하면 (제자와 보스가 함께 쓰러짐), 금화는 0, `StageCleared` 는 0번, `StageFailed` 가 1을 담아 1번 오고 `Phase` 는 `FadingOut` 이다.
- (1000, 50) 시작한 뒤 `Normal` 적 10마리를 1스테이지, 좌표 0.8 에 직접 넣고 1칸 진행하면 (열 번째 몬스터와 제자가 함께 쓰러짐), 금화는 100, `DefeatedNormalCount` 는 10, `Boss` 는 `null`, `battle.Enemies.Count` 는 10, `StageFailed` 가 1번 온다.
- 위 "성공 상태"에 이어 (금화 50, `FadingOut`) `upgrades.TryUpgrade(StatType.Attack)` 은 `false` 이고 금화는 50 그대로다.
- 위 "성공 상태"에 이어 테스트가 `battle.SetPaused(false)` 를 부르고 `Normal` 적 하나를 1스테이지, 좌표 0.8 에 직접 넣은 뒤 그 적의 `IsDead` 가 `true` 가 될 때까지 최대 60칸 진행하면 (페이드 중의 처치는 무시), 그 적의 `IsDead` 는 `true`, 금화는 50 그대로, `DefeatedNormalCount` 는 0 이고, `Changed`·`StageCleared`·`StageFailed` 는 더 오지 않는다.
- `wallet = new Wallet(0)`, `upgrades = new StatUpgrades(wallet, 100000000000, 100000000000, 1.0)`, `startStage` 가 `int.MaxValue` 인 스테이지로, 시작한 뒤 `Boss` 적 하나를 `int.MaxValue` 스테이지, 좌표 1.0 에 직접 넣고 1칸 진행하면, 예외 없이 금화는 21474836510, `StageCleared` 가 `int.MaxValue` 를 담아 1번 오고 `Phase` 는 `FadingOut` 이다.
- (1000, 100000) 시작한 뒤 아무것도 직접 넣지 않고 `Phase` 가 `FadingOut` 이 될 때까지 최대 20000칸 진행하면 (스테이지 한 판 전체), 금화는 150, `DefeatedNormalCount` 는 10, `StageCleared` 가 1을 담아 1번, `StageFailed` 는 0번 오고, `IsBossActive` 는 `true`, `BarRatio` 는 0.0 이다.
- 기존 테스트가 모두 통과한다. (`StageProgressTests`, `StageProgressKillTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다. `PlumSword.Core` 는 순수 C# 이다.
- `StageProgress` 의 공개 모양을 바꾸거나 공개 멤버를 더하지 않는다.
- `Tick` 의 페이드 처리(페이드 시간 더하기, 가려진 순간의 교체, 전환 끝, 멈춤·잠금 풀기)와 `FadeAlpha` 의 페이드 중 값을 만들지 않는다. 018_stage_progress_fade 가 한다.
- 016_stage_progress_kills 가 만든 일반 몬스터 처치 처리(금화, 처치 수, 보스 등장, `Changed`)의 동작을 바꾸지 않는다.
- 성공·실패 뒤 기다리는 시간, 결과 글자, 수동 재도전을 만들지 않는다. 사양의 "범위 밖"이다.
- `StageRewards.cs`, `StageText.cs`, `StageLayout.cs`, `StagePhase.cs` 를 고치지 않는다.
- `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/`, `Assets/Scripts/Core/Combat/` 를 고치지 않는다. `Wallet`, `StatUpgrades`, `Battle` 은 공개된 모양으로만 쓴다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. 019_stage_view 가 한다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `StageProgress` 에 보스 처치 성공 처리(`OnBossDefeated`), 제자 쓰러짐 실패 처리(`OnDiscipleDefeated`), `private` 전환 시작(`BeginTransition`)을 채웠다.
  - 보스: `HasBegun && Phase == Playing` 일 때만. `battle.IsDiscipleDefeated` 면 아무것도 하지 않음. 아니면 `wallet.Add(BossGold)` → 전환 시작(다음 번호 `+1`, `int.MaxValue` 면 그대로) → `StageCleared` → `Changed`.
  - 제자: 같은 조건에서 전환 시작(다음 번호 = 같은 번호) → `StageFailed` → `Changed`.
  - 전환 시작: `_nextStageNumber` 기억, `Phase = FadingOut`, `_fadeElapsed = 0`, `battle.SetPaused(true)`, `upgrades.SetLocked(true)`.
- `StageCleared`·`StageFailed` 의 CS0067 경고 억제를 걷어냈다 (이제 보낸다).
- 에딧 모드 테스트 `StageProgressOutcomeTests` 10개를 더했다 (완료 조건 항목마다 하나 이상).

### 바뀐 파일
- `Assets/Scripts/Core/Stage/StageProgress.cs` (수정)
- `Assets/Tests/EditMode/Stage/StageProgressOutcomeTests.cs` (새 파일)
- `Assets/Tests/EditMode/Stage/StageProgressOutcomeTests.cs.meta` (새 파일)

### 가정
- 018 이 읽을 `private` 필드 이름은 `_nextStageNumber`(int), `_fadeElapsed`(double) 로 정했다. 이 작업에서는 쓰기만 하고 읽지 않아 CS0414 경고를 `#pragma` 로 막아 두었다 (018 에서 걷어내면 된다).
- 사양 문서는 이미 이번 동작과 같아서 고치지 않았다. 공개 모양은 바꾸지 않았다.

### 사람이 확인할 것
- 없음 (완료 조건에 [사람 확인] 항목이 없고, 모두 테스트로 판정했다).

### 테스트 결과
- `python tools/loop/run_unity_tests.py --project .` → EditMode 259개 중 259개 통과, 0개 실패 (`StageProgressTests`, `StageProgressKillTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함, 새 `StageProgressOutcomeTests` 10개 포함).


## 루프 기록 (2026-11-16 21:08)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/017_stage_progress_outcome
- 루프 브랜치: loop/20261116-205758
