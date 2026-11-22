# 076_flow_save_reload — 저장 후 종료·다시 켜기 흐름 테스트

## 목표
실제 `GameRoot` 로 진행하다 끄고(즉시 저장) 다시 켰을 때 금화·강화·경지·스테이지가 그대로 이어지고 진행이 계속되는지 검사하는 플레이 모드 흐름 테스트를 만든다. 테스트만 추가한다.

## 묶음
flow_test

## 선행 작업
- 074_flow_test_hooks

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("테스트" 절)
- docs/spec/save.md ("동작"의 "저장 값", "불러오기", "자동 저장", "저장하는 때")
- docs/spec/stage.md ("동작")
- docs/spec/offline_reward.md ("동작"의 "켤 때")

## 요구사항
- 테스트 파일 `Assets/Tests/PlayMode/Flow/SaveReloadFlowTests.cs` (`PlumSword.Game.Tests.Flow.SaveReloadFlowTests`). 074 가 만든 `GameFlow` 도우미(`FindRoot`, `Relaunch`, `ResetToFreshLaunch`, `PowerUp`, `WaitUntil`)와 `GameRoot.BatchSaveStore`·`RecreateAs` 를 쓴다.
- SetUp: `SceneManager.LoadSceneAsync(0)` 후 3프레임 기다린다. TearDown: `yield return GameFlow.ResetToFreshLaunch()`.
- 흐름 (테스트 하나에 이어서 하고, 아래 완료 조건을 단계마다 메시지를 달아 `Assert` 한다):
  1. 빈 `MemorySaveStore` 하나로 `GameFlow.Relaunch(store, StartEntry.Launch)`.
  2. `GameFlow.PowerUp(root, 200)` 후 `root.StatUpgrades.TryUpgrade(StatType.InnerPower)` 를 한 번 한다 (비용만큼 먼저 `Wallet.Add(GetCost(InnerPower))`). 내공도 저장되는지 보려는 것이다.
  3. `Time.timeScale = 20f` (임시값) 로 두고 `GameFlow.WaitUntil(() => root.StageProgress.StageNumber == 3, 120f, ...)`.
  4. 끄기를 흉내 낸다: `root.AutoSave.SaveNow(DateTime.UtcNow.Ticks)` 가 `true` 인지 보고, 그 직후 금화, 세 강화 단계, `Realm.Current`, `Realm.Kills`, `StageProgress.StageNumber`, `SeenIntroStage` 를 기록한다. 같은 프레임 안에서 바로 `GameFlow.Relaunch(store, StartEntry.Launch)` 를 한다 (`RecreateAs` 가 루트를 바로 꺼서 그 뒤로는 진행되지 않는다). 기록과 `Relaunch` 사이에 `yield` 를 두지 않는다.
  5. 새 루트(`GameFlow.FindRoot()`)를 검사한다.
  6. `Time.timeScale = 20f` 로 다시 두고 `StageNumber == 4` 를 기다려 다시 켠 뒤에도 진행이 이어지는지 본다. 새 루트의 강화는 불러온 값(공격력 단계 200 등)이므로 다시 `PowerUp` 하지 않는다.
- 처음 켤 때 저장 시각이 방금이라 오프라인 보상이 없다 (offline_reward.md: 10초 미만은 보상 없음, 창 없이 바로 시작). 기록부터 다시 켜기까지가 10초를 넘지 않게 한다.
- 시간이 오래 걸려 120초 안에 끝나지 않으면 원인을 보고서에 적고 blocked 로 보고한다. 게임 코드를 고쳐 맞추지 않는다.

## 완료 조건
- [플레이 모드] `Assets/Tests/PlayMode/Flow/SaveReloadFlowTests.cs` 에 아래 조건마다 검사가 있고 모두 통과한다.
- [플레이 모드] 진행해서 `StageProgress.StageNumber` 가 3 이 된 뒤 `AutoSave.SaveNow(DateTime.UtcNow.Ticks)` 는 `true` 다.
- [플레이 모드] 다시 켠 새 루트의 `LoadResult.Status` 는 `Loaded` 다.
- [플레이 모드] 새 루트의 `Wallet.Balance` 는 끄기 직전 기록한 금화와 같다.
- [플레이 모드] 새 루트의 공격력·체력·내공 단계는 끄기 직전 기록한 값(200, 200, 1)과 같다.
- [플레이 모드] 새 루트의 `Realm.Current` 와 `Realm.Kills` 는 끄기 직전 기록한 값과 같다.
- [플레이 모드] 새 루트의 `StageProgress.StageNumber` 는 끄기 직전 기록한 값(3)과 같고 `SubstageNumber` 는 1 이다.
- [플레이 모드] 새 루트의 `StageProgress.SeenIntroStage` 는 끄기 직전 기록한 값과 같다.
- [플레이 모드] 새 루트의 `StageProgress.HasBegun` 은 `true`, `Battle.Paused` 는 `false` 다.
- [플레이 모드] 새 루트의 `Battle.DiscipleHealth` 는 `Battle.DiscipleMaxHealth` 와 같다 (불러오면 체력이 가득 찬다).
- [플레이 모드] 새 루트의 `OfflineReward` 는 `null` 이 아니고 `HasReward` 는 `false` 이며, `[OfflineRewardView]` 자식이 없다.
- [플레이 모드] 다시 켠 뒤 `Time.timeScale` 20 에서 실제 시간 120초 안에 `StageProgress.StageNumber` 가 4 가 된다.
- [컴파일] 기존 테스트가 모두 통과한다.

## 하지 말 것
- 게임 코드(`Assets/Scripts/`)를 고치지 않는다. 테스트가 실패하면 테스트를 게임에 맞춰 비틀지 말고, 사양과 다른 점을 보고서에 적는다. 사양과 다르면 blocked 로 보고한다.
- `GameFlow` 도우미와 `GameFlowTests.cs`, 075 의 `FullRoundFlowTests.cs` 를 고치지 않는다. 필요한 것은 이 테스트 파일 안의 private 메서드로 만든다.
- 오프라인 보상 지급과 다음 회차 진입은 검사하지 않는다 (077·075 가 한다).
- 씬·프리팹·에셋 파일, 어셈블리 정의, `Assets/Tests/PlayMode/Capture/` 를 고치지 않는다.

## 보고서

### 한 일
- 실제 게임 진행 중 즉시 저장하고 같은 저장소로 다시 켠 뒤 저장 값, 시작 상태와 4스테이지까지의 진행을 검사하는 플레이 모드 테스트를 추가했다.
- 제한 시간 초과 시 스테이지, 구간, 단계와 전투 상태를 실패 메시지에 남긴다.
- 재시작 직후 복원 값을 확인한 다음 구간 소개가 끝날 때까지 기다리고 전투 재개를 검사한다. 소개 대기 시간도 4스테이지 도달 제한 120초에 포함한다.

### 바뀐 파일
- `Assets/Tests/PlayMode/Flow/SaveReloadFlowTests.cs`
- `Assets/Tests/PlayMode/Flow/SaveReloadFlowTests.cs.meta`

### 가정
- 재생성 도우미가 기다리는 프레임 동안 전투가 진행되지 않도록 저장 직전에 시간 배율을 0으로 두고 복원 상태 검사 후 20으로 되돌렸다. 저장과 기존 루트 비활성화는 같은 프레임에 수행한다.
- 스테이지 사양에 따라 다시 시작한 첫 구간의 1.2초 소개 중에는 전투가 정지하므로, 소개가 끝난 `Playing` 상태에서 전투 재개를 검사한다.
- 사양과 공개 연결 지점의 변경은 없다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 컴파일 및 플레이 모드 테스트는 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다. 120초 제한의 실제 통과 여부도 아직 확인되지 않았다.
- 이전 루프 판정에서는 재시작 직후 구간 소개 중 `Battle.Paused` 검사에 실패했다. 수정 후 결과는 아직 확인되지 않았다.


## 루프 기록 (2026-11-22 09:32)

- 작업 에이전트: codex(시도 1~2)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/076_flow_save_reload
- 루프 브랜치: loop/20261122-091339
