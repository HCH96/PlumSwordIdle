# 090_balance_game_root — GameRoot 시작 능력치를 수치 표에서 읽기

## 목표
Game(`PlumSword.Game.GameRoot`)이 `StatUpgrades`를 만들 때 시작 공격력·최대 체력을 `Battle`의 상수 대신 `BalanceTable.Current`에서 읽게 한 줄을 바꾼다.

## 묶음
balance

## 선행 작업
- 087_balance_table

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다", "테스트")
- docs/spec/balance.md ("동작"의 표, "연결 지점"의 GameRoot 항목)
- docs/spec/stat_upgrade.md ("연결 지점" 절)

## 요구사항
- `Assets/Scripts/Game/GameRoot.cs`의 `Awake`에서 `new PlumSword.Core.StatUpgrade.StatUpgrades(...)`의 둘째·셋째 인자를 `PlumSword.Core.Combat.Battle.DiscipleBaseAttack`, `DiscipleBaseMaxHealth`에서 `PlumSword.Core.Balance.BalanceTable.Current.DiscipleBaseAttack`, `BalanceTable.Current.DiscipleBaseMaxHealth`로 바꾼다. 그 밖의 줄은 고치지 않는다.
- 게임은 `BalanceTable.Current`를 바꾸지 않는다 (늘 기본 표 = 10, 100). 그래서 게임 동작과 기존 테스트는 바뀌지 않는다.
- 플레이 모드 테스트는 `Assets/Tests/PlayMode/Flow/BalanceStartStatsFlowTests.cs`(네임스페이스 `PlumSword.Game.Tests.Flow`)에 쓴다. 기존 흐름 테스트처럼 `[UnitySetUp]`에서 `SceneManager.LoadSceneAsync(0)`, `GameFlow.Relaunch(new MemorySaveStore(), StartEntry.Launch)`로 새 루트를 만든다. `[UnityTearDown]`에서 먼저 `BalanceTable.Current = BalanceTable.Default`로 되돌린 뒤 `GameFlow.ResetToFreshLaunch()`를 부른다 (`docs/dev/architecture.md` "테스트").
- `docs/spec/stat_upgrade.md` "수치"의 시작 능력치 줄 비고를 "`BalanceTable.Current`의 `DiscipleBaseAttack`·`DiscipleBaseMaxHealth` (기본 10, 100)"로 고친다.

## 완료 조건
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다.
- [플레이 모드] `Assets/Tests/PlayMode/Flow/BalanceStartStatsFlowTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- [플레이 모드] (추가) `BalanceTable.Current`를 `DiscipleBaseAttack` 15, `DiscipleBaseMaxHealth` 150인 표로 바꾼 뒤 `GameFlow.Relaunch(new MemorySaveStore(), StartEntry.Launch)`로 새로 띄우면 `GameRoot.StatUpgrades.Attack`은 15, `MaxHealth`는 150이다.
- [플레이 모드] (추가) 기본 표로 새로 띄우면 `GameRoot.StatUpgrades.Attack`은 10, `MaxHealth`는 100이다.

## 하지 말 것
- `GameRoot`에서 시작 능력치 두 인자 말고 다른 줄을 고치지 않는다.
- `Assets/Scripts/Core/`를 고치지 않는다 (087·088·089 가 했다).
- `Battle`의 공개 상수를 지우지 않는다.
- 게임 안에서 수치 표를 바꾸는 화면이나 파일 읽기를 만들지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/`를 건드리지 않는다.

## 보고서

### 한 일
- GameRoot의 시작 공격력과 최대 체력을 현재 수치 표에서 읽도록 바꿨다.
- 기본 표와 변경된 표로 새 루트를 띄워 시작 능력치를 확인하는 플레이 모드 테스트를 추가했다.
- 능력치 강화 사양의 수치와 연결 지점을 현재 수치 표 기준으로 갱신했다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs`
- `Assets/Tests/PlayMode/Flow/BalanceStartStatsFlowTests.cs` 및 `.meta`
- `docs/spec/stat_upgrade.md`
- `tasks/doing/090_balance_game_root.md`, `.loop/result.json`

### 가정
- 없음. 사양의 GameRoot 연결 예시와 필요 의존성을 작업에서 지정한 수치 표에 맞춰 갱신했으며 공개 타입이나 메서드는 바꾸지 않았다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- `git diff --check` 통과.
- Unity 컴파일 및 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-22 16:29)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/090_balance_game_root
- 루프 브랜치: loop/20261122-160726
