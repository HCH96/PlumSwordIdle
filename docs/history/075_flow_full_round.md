# 075_flow_full_round — 새 게임부터 천마 처치, 다음 회차 진입까지 흐름 테스트

## 목표
실제 `GameRoot` 로 새 게임을 시작해 1~5스테이지(천마 최종 보스 포함)를 이기고 2회차 1스테이지에 들어가는지 검사하는 플레이 모드 흐름 테스트를 만든다. 테스트만 추가한다.

## 묶음
flow_test

## 선행 작업
- 074_flow_test_hooks

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("테스트" 절)
- docs/spec/stage.md ("동작", "수치")
- docs/spec/realm.md ("수치")
- docs/spec/save.md ("동작"의 "자동 저장", "저장하는 때")

## 요구사항
- 테스트 파일 `Assets/Tests/PlayMode/Flow/FullRoundFlowTests.cs` (`PlumSword.Game.Tests.Flow.FullRoundFlowTests`). 074 가 만든 `GameFlow` 도우미(`FindRoot`, `Relaunch`, `ResetToFreshLaunch`, `PowerUp`, `WaitUntil`)와 `GameRoot.BatchSaveStore`·`RecreateAs` 를 쓴다.
- SetUp: `SceneManager.LoadSceneAsync(0)` 후 3프레임 기다린다. TearDown: `yield return GameFlow.ResetToFreshLaunch()` (실패해도 다음 테스트가 처음 켠 게임을 보게).
- 흐름 (테스트 하나에 이어서 하고, 아래 완료 조건을 단계마다 `Assert` 한다. 조건마다 메시지를 달아 어느 조건이 실패했는지 알 수 있게 한다):
  1. 빈 `MemorySaveStore` 하나를 만들어 `GameFlow.Relaunch(store, StartEntry.Launch)`. 새 게임과 같다.
  2. `root.StageProgress.StageCleared`, `StageFailed` 를 구독해 번호를 목록에 모은다.
  3. `GameFlow.PowerUp(root, 200)`. 공격력 10 + 2×200 = 410, 최대 체력 100 + 20×200 = 4100 이 되어 1회차의 모든 상대(최종 보스 체력 220, 공격력 26)를 쉽게 이긴다. 그 직후 `Wallet.Balance` 를 기록한다.
  4. `Time.timeScale = 20f` (임시값. 너무 느리거나 불안정하면 조절하고 보고서의 가정에 적는다).
  5. `GameFlow.WaitUntil(() => root.StageProgress.StageNumber == 6, 180f, ...)`. 실패 메시지에는 `StageNumber`, `SubstageNumber`, `Phase`, `Battle.IsDiscipleDefeated`, 모은 `StageCleared`·`StageFailed` 목록을 넣는다.
  6. 3프레임 더 기다린 뒤(자동 저장의 `Flush`) 나머지를 검사한다. `Time.timeScale` 은 TearDown 의 `ResetToFreshLaunch` 가 1 로 되돌린다.
- 금화 범위의 근거 (stage.md "수치"와 `StageRewards`): 일반 상대 금화 `10 + 2×(번호−1)`, 보스 금화 `50 + 10×(번호−1)`. 1~4스테이지는 일반 구간 6개 × 2~3명과 보스 1명, 5스테이지는 최종 보스 1명뿐이다. 그래서 1회차에서 받는 금화는 최소 12×(10+12+14+16) + (50+60+70+80+90) = 624 + 350 = 974, 최대 18×(10+12+14+16) + 350 = 936 + 350 = 1286 이다. 강화는 3단계 뒤로 하지 않으므로 금화는 이것만큼만 는다.
- 경지의 근거 (realm.md "수치"): 처치 수는 최소 4×(12+1)+1 = 53, 최대 4×(18+1)+1 = 77 이다. 삼류 → 이류에 30이 필요하고 이류 → 일류에 50이 더 필요하므로 (77 − 30 = 47 < 50) 1회차가 끝나면 경지는 반드시 이류(`SecondRate`)다.
- 시간이 오래 걸리면(루프의 테스트 전체 시간을 크게 늘리면) `Time.timeScale` 을 올려 보고, 그래도 180초 안에 끝나지 않으면 진행을 막는 원인(예: 어떤 화면 대기가 실제 시간을 씀)을 보고서에 적고 blocked 로 보고한다. 게임 코드를 고쳐 맞추지 않는다.

## 완료 조건
- [플레이 모드] `Assets/Tests/PlayMode/Flow/FullRoundFlowTests.cs` 에 아래 조건마다 검사가 있고 모두 통과한다.
- [플레이 모드] 빈 저장소로 `Launch` 하면 `LoadResult.Status` 는 `Empty`, `StageProgress.StageNumber` 는 1, `RoundNumber` 는 1 이다.
- [플레이 모드] `PowerUp(root, 200)` 뒤 `StatUpgrades.Attack` 은 410, `StatUpgrades.MaxHealth` 는 4100 이다.
- [플레이 모드] `Time.timeScale` 20 에서 실제 시간 180초 안에 `StageProgress.StageNumber` 가 6 이 된다.
- [플레이 모드] 그때 `RoundNumber` 는 2, `DisplayStageNumber` 는 1, `SubstageNumber` 는 1 이다.
- [플레이 모드] 그때까지 모은 `StageCleared` 번호는 차례대로 [1, 2, 3, 4, 5] 다.
- [플레이 모드] 그때까지 `StageFailed` 는 한 번도 오지 않았다.
- [플레이 모드] 그때 `Realm.Current` 는 `RealmLevel.SecondRate` 다.
- [플레이 모드] 그때 `Wallet.Balance` 는 3단계 직후 기록한 값보다 974 이상 1286 이하만큼 많다.
- [플레이 모드] 3프레임 뒤 `SaveCodec.Decode(store.Text).Data.StageNumber` 는 6 이다 (자동 저장이 다음 회차 진입을 저장했다).
- [플레이 모드] 테스트가 끝난 뒤 TearDown 의 `ResetToFreshLaunch()` 로 `StageProgress.StageNumber` 는 1, `Time.timeScale` 은 1 이다 (다음 테스트를 위한 정리. 같은 파일의 두 번째 테스트나 TearDown 안의 `Assert` 로 확인한다).
- [컴파일] 기존 테스트가 모두 통과한다.

## 하지 말 것
- 게임 코드(`Assets/Scripts/`)를 고치지 않는다. 테스트가 실패하면 테스트를 게임에 맞춰 비틀지 말고, 사양과 다른 점을 보고서에 적는다. 사양과 다르면 blocked 로 보고한다.
- `GameFlow` 도우미(`Assets/Tests/PlayMode/Flow/GameFlow.cs`)와 `GameFlowTests.cs` 를 고치지 않는다. 필요한 것이 없으면 이 테스트 파일 안의 private 메서드로 만든다.
- 저장 후 다시 켜기와 오프라인 보상은 검사하지 않는다 (076·077 이 한다).
- 씬·프리팹·에셋 파일, 어셈블리 정의, `Assets/Tests/PlayMode/Capture/` 를 고치지 않는다.

## 보고서

### 한 일
- 빈 저장소에서 새 게임을 시작해 1~5스테이지 완료, 2회차 진입, 보상·경지·자동 저장을 확인하는 플레이 모드 테스트를 추가했다.
- TearDown에서 새 게임과 시간 배율 복원을 검사한다. 제한 시간 초과 시 진행 상태와 이벤트 목록을 출력한다.

### 바뀐 파일
- `Assets/Tests/PlayMode/Flow/FullRoundFlowTests.cs` 및 짝 `.meta`
- `tasks/doing/075_flow_full_round.md` 보고서, `.loop/result.json`

### 가정
- 없음. 시간 배율은 작업 문서의 임시값 20을 사용했다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 새 테스트와 기존 테스트의 컴파일·실행 결과는 루프 스크립트의 판정을 기다린다.
- `git diff --check` 통과. 새 파일은 추적 전이므로 해당 검사에 포함되지 않는다.


## 루프 기록 (2026-11-22 09:25)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/075_flow_full_round
- 루프 브랜치: loop/20261122-091339
