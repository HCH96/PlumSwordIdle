# 077_flow_offline_reward — 오프라인 보상 1회 지급과 이어지는 진행 흐름 테스트

## 목표
실제 `GameRoot` 를 한 시간 전 저장으로 켰을 때 오프라인 보상 창이 뜨고, 받기를 누르면 금화가 한 번만 들어오며 저장되고, 창이 닫힌 뒤 전투가 이어지고, 다시 켜도 보상이 또 나오지 않는지 검사하는 플레이 모드 흐름 테스트를 만든다. 테스트만 추가한다.

## 묶음
flow_test

## 선행 작업
- 074_flow_test_hooks

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("테스트" 절)
- docs/spec/offline_reward.md ("동작", "수치", "화면")
- docs/spec/save.md ("동작"의 "불러오기", "자동 저장")
- docs/spec/stage.md ("수치"의 금화)

## 요구사항
- 테스트 파일 `Assets/Tests/PlayMode/Flow/OfflineRewardFlowTests.cs` (`PlumSword.Game.Tests.Flow.OfflineRewardFlowTests`). 074 가 만든 `GameFlow` 도우미(`FindRoot`, `Relaunch`, `ResetToFreshLaunch`, `WaitUntil`)와 `GameRoot.BatchSaveStore`·`RecreateAs` 를 쓴다.
- SetUp: `SceneManager.LoadSceneAsync(0)` 후 3프레임 기다린다. TearDown: `yield return GameFlow.ResetToFreshLaunch()`.
- 흐름 (테스트 하나에 이어서 하고, 아래 완료 조건을 단계마다 메시지를 달아 `Assert` 한다). `Time.timeScale` 은 1 그대로 둔다.
  1. `long savedAt = DateTime.UtcNow.Ticks - TimeSpan.FromHours(1).Ticks;` 로 한 시간 전 시각을 만든다.
  2. `MemorySaveStore` 에 `SaveCodec.Encode(new SaveData(100, 0, 0, 0, 0, 2, 2, savedAt, 0))` 를 쓴다 (금화 100, 강화 0, 경지 삼류, 2스테이지, 소개 2까지 봄).
  3. `GameFlow.Relaunch(store, StartEntry.Launch)`.
  4. 창이 뜬 상태를 검사한다.
  5. 받기를 누른다: `root.UiCanvas.transform.Find("OfflineRewardOverlay/OfflineRewardWindow/ClaimButton")` 의 `Button.onClick.Invoke()`. 누르기 직전의 `DateTime.UtcNow.Ticks` 를 기록한다.
  6. 3프레임 기다린 뒤 받은 뒤의 상태를 검사한다.
  7. `root.OfflineRewardClaim.TryClaim(DateTime.UtcNow.Ticks)` 를 한 번 더 부른다 (중복 지급이 없는지).
  8. 다시 켜기: `RecreateAs` 직전에 `SaveCodec.Decode(store.Text).Data.Gold` 를 기록하고 같은 프레임에 `GameFlow.Relaunch(store, StartEntry.Launch)`. 새 루트를 검사한다.
- 기대 금화의 근거 (offline_reward.md "동작"·"수치", stage.md 금화): 경과 1시간 = 36,000,000,000 틱. 8시간 상한보다 작다. 10초(`IntervalTicks` 100,000,000) 단위로 내리면 그대로이므로 `KillCount` = 360. `StageRewards.NormalGold(2)` = 10 + 2×1 = 12. 보상 금화 = 360 × 12 = 4,320. 받은 뒤 금화 = 100 + 4,320 = 4,420.
  - 1단계에서 시각을 만든 뒤 `Awake` 가 지금 시각을 읽기까지 몇 프레임(10초보다 훨씬 짧음)이 흐르므로 경과는 1시간 + 몇 초이고, 10초 단위로 내리면 360 이다. 1단계와 3단계 사이에 `yield` 를 두지 않는다.

## 완료 조건
- [플레이 모드] `Assets/Tests/PlayMode/Flow/OfflineRewardFlowTests.cs` 에 아래 조건마다 검사가 있고 모두 통과한다.
- [플레이 모드] 한 시간 전 저장으로 켜면 `OfflineReward.HasReward` 는 `true`, `OfflineReward.KillCount` 는 360, `OfflineReward.Gold` 는 4320 이다.
- [플레이 모드] 받기 전에는 `Wallet.Balance` 가 100 이다.
- [플레이 모드] 받기 전에는 `Battle.Paused` 가 `true`, `StageProgress.HasBegun` 이 `false`, `AutoSave.Enabled` 가 `false` 다.
- [플레이 모드] 받기 전에는 `UiCanvas` 아래에 `OfflineRewardOverlay/OfflineRewardWindow/ClaimButton` 이 있다.
- [플레이 모드] 받기를 누른 뒤 `Wallet.Balance` 는 4420 이다.
- [플레이 모드] 받기를 누른 뒤 `OfflineRewardClaim.State` 는 `OfflineRewardClaimState.Done`, `HasPaid` 는 `true` 다.
- [플레이 모드] 받기를 누른 뒤 `SaveCodec.Decode(store.Text).Data.Gold` 는 4420 이고 `SavedAtUtcTicks` 는 누르기 직전 기록한 시각 이상이다.
- [플레이 모드] 받기를 누르고 3프레임 뒤 `OfflineRewardOverlay` 는 없고, `StageProgress.HasBegun` 은 `true`, `StageProgress.StageNumber` 는 2, `StageProgress.Phase` 는 `StagePhase.SubstageIntro` 다.
- [플레이 모드] 그 뒤 실제 시간 10초 안에 `StageProgress.Phase` 가 `StagePhase.Playing` 이 되고, 그때 `Battle.Paused` 는 `false` 다.
- [플레이 모드] 받은 뒤 `TryClaim` 을 한 번 더 부르면 `true` 를 돌려주고 `Wallet.Balance` 는 그 직전 값과 같다 (다시 주지 않는다).
- [플레이 모드] 받은 뒤 다시 켜면 새 루트의 `OfflineReward.HasReward` 는 `false` 이고 `[OfflineRewardView]` 자식이 없다.
- [플레이 모드] 받은 뒤 다시 켜면 새 루트의 `Wallet.Balance` 는 다시 켜기 직전 저장된 금화와 같고 4420 이상이다.
- [컴파일] 기존 테스트가 모두 통과한다.

## 하지 말 것
- 게임 코드(`Assets/Scripts/`)를 고치지 않는다. 테스트가 실패하면 테스트를 게임에 맞춰 비틀지 말고, 사양과 다른 점을 보고서에 적는다. 사양과 다르면 blocked 로 보고한다.
- `GameFlow` 도우미와 `GameFlowTests.cs`, 075·076 의 테스트 파일을 고치지 않는다. 필요한 것은 이 테스트 파일 안의 private 메서드로 만든다.
- 백그라운드 복귀(`OfflineResumeWatcher`)는 검사하지 않는다 (배치 모드에서는 만들어지지 않는다).
- 저장 실패 때의 `RETRY` 는 검사하지 않는다 (기존 `OfflineRewardClaimTests` 가 한다).
- 씬·프리팹·에셋 파일, 어셈블리 정의, `Assets/Tests/PlayMode/Capture/` 를 고치지 않는다.

## 보고서

### 한 일
- 한 시간 전 저장으로 시작하여 보상 수령, 저장, 중복 수령 방지 및 재실행을 검사하는 플레이 모드 테스트를 작성했다.
- 수령 후 3프레임의 전투 일시정지 조건이 현재 스테이지 사양 및 구현과 충돌하는 것을 확인하여 보류한다.

### 바뀐 파일
- `Assets/Tests/PlayMode/Flow/OfflineRewardFlowTests.cs` 및 `.meta`: 흐름 테스트 추가.
- `tasks/doing/077_flow_offline_reward.md`: 보고서 추가.
- `.loop/result.json`: 보류 결과 기록.

### 가정
- 없음.

### 사람이 확인할 것
- 작업 문서는 수령 3프레임 뒤 `Battle.Paused`가 거짓이라고 요구하지만 `docs/spec/stage.md`는 시작 시 1.2초간 구간 소개와 전투 정지를 요구한다. `StageProgress.Begin()`은 구간 소개에서 전투를 다시 멈춘다. 어떤 조건을 우선할지 결정해야 한다.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다. 정적 확인상 위 전투 정지 조건은 구간 소개가 3프레임 안에 끝나지 않으면 실패한다.


## 보류 사유 (2026-11-22 09:34)

- 사유: 수령 3프레임 뒤 전투가 재개되어야 한다는 완료 조건이 스테이지 사양의 1.2초 구간 소개 중 전투 정지 및 현재 구현과 충돌한다.
- 질문: 수령 직후 3프레임의 일시정지 해제와 1.2초 구간 소개 중 정지 중 어느 조건을 우선해야 하나요?
- 추천안: 구간 소개 동안 전투 정지를 유지하고 소개가 끝난 뒤 전투 재개를 검사하도록 완료 조건을 수정한다.
- 기능 브랜치: task/077_flow_offline_reward (작업 중 상태 커밋됨)
- 작업 에이전트: codex(시도 1)


## 답변 (2026-11-22 09:42)

- 보류 종류: 에이전트의 질문 (2026-11-22 09:34 보류 사유에 대한 답)
- 결정: 구간 소개 중 전투 정지(스테이지 사양)를 우선한다. 받기를 누르고 3프레임 뒤에 `Battle.Paused` 가 `false` 여야 한다는 원래 완료 조건은 작업 문서를 쓸 때의 실수였다. 3프레임 뒤에는 창이 닫히고 스테이지가 시작되어 구간 소개(`SubstageIntro`) 중인지를 검사하고, 전투 재개는 소개가 끝나 `Playing` 이 된 뒤에 검사한다.
- 이유: `docs/spec/stage.md` 에 따라 구간이 시작되면 `SubstageIntro` 에서 번호를 1.2초 보여 주는 동안 전투를 멈춘다. `StageProgress.Begin()` 이 그렇게 구현되어 있고 맞는 동작이다. 이 저장은 2스테이지, 본 소개 번호 2 이므로 테마 소개(3초)는 없고 1.2초 구간 소개만 있다. 같은 묶음의 076 도 소개가 끝난 `Playing` 상태에서 전투 재개를 검사했다.
- 할 일: `Assets/Tests/PlayMode/Flow/OfflineRewardFlowTests.cs` 에서 받기 3프레임 뒤 검사를 `OfflineRewardOverlay` 없음, `StageProgress.HasBegun == true`, `StageNumber == 2`, `Phase == StagePhase.SubstageIntro` 로 바꾼다. 이어서 `GameFlow.WaitUntil(() => root.StageProgress.Phase == StagePhase.Playing, 10f, ...)` 로 기다린 뒤 `Battle.Paused == false` 를 검사한다. 그다음 요구사항의 7단계(`TryClaim` 한 번 더)와 8단계(다시 켜기)를 그대로 한다. 게임 코드는 고치지 않는다.
- 바꾼 칸: 완료 조건. "받기를 누르고 3프레임 뒤 `OfflineRewardOverlay` 는 없고, `Battle.Paused` 는 `false`, `StageProgress.HasBegun` 은 `true`, `StageProgress.StageNumber` 는 2 다." → 두 항목으로 나눔: "받기를 누르고 3프레임 뒤 `OfflineRewardOverlay` 는 없고, `StageProgress.HasBegun` 은 `true`, `StageProgress.StageNumber` 는 2, `StageProgress.Phase` 는 `StagePhase.SubstageIntro` 다." 와 "그 뒤 실제 시간 10초 안에 `StageProgress.Phase` 가 `StagePhase.Playing` 이 되고, 그때 `Battle.Paused` 는 `false` 다."
- 기능 브랜치: task/077_flow_offline_reward 의 작업 중 커밋(`OfflineRewardFlowTests.cs`)은 살려서 이어간다. 위 검사 부분만 고친다.

## 보고서

### 한 일
- 한 시간 전 저장의 오프라인 보상 계산, 수령과 저장, 중복 지급 방지, 재실행을 하나의 플레이 모드 흐름으로 검사한다.
- 사람의 답변에 따라 수령 3프레임 뒤 구간 소개를 확인하고, 10초 안에 Playing으로 전환된 뒤 전투 재개를 확인한다.

### 바뀐 파일
- `Assets/Tests/PlayMode/Flow/OfflineRewardFlowTests.cs` 및 `.meta`: 흐름 테스트 추가 및 구간 소개 검사 수정.
- `tasks/doing/077_flow_offline_reward.md`: 보고서 추가.
- `.loop/result.json`: 완료 결과 기록.

### 가정
- 없음.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- `git diff --check` 통과. Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-22 09:48)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/077_flow_offline_reward
- 루프 브랜치: loop/20261122-094424
