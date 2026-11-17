# 034_offline_claim — 오프라인 보상 지급과 저장

## 목표
Core에서 보상을 한 번 지급하고 저장 실패 시 재시도한다.

## 묶음
오프라인보상

## 선행 작업
- 033_offline_calculator

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/offline_reward.md
- docs/spec/save.md (연결 지점과 완료 조건 초안 절)
- docs/spec/currency.md (연결 지점 절)
- docs/spec/stage.md (연결 지점 절)

## 요구사항
- Assets/Scripts/Core/OfflineReward/에 사양의 OfflineRewardClaimState와 OfflineRewardClaim을 각각 구현한다. 공개 계약과 생성자 검증을 따른다.
- 첫 시도에서만 Wallet.Add 후 AutoSave.Enable, 이어 SaveNow(nowUtcTicks)를 호출한다. 실패 후에는 저장만 재시도한다. Done은 추가 변경 없이 true다.
- 테스트의 T0, 가짜 저장소, 스테이지 객체, 보상 준비는 오프라인 보상 사양의 완료 조건 초안을 따른다. 저장 내용은 SaveCodec.Decode로 확인한다.

## 완료 조건
- Assets/Tests/EditMode/OfflineReward/OfflineRewardClaimTests.cs에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 생성자에서 wallet, autoSave, reward를 각각 null로 주면 ArgumentNullException이다.
- 보상이 없는 결과를 생성자에 주면 ArgumentException이다.
- 보상 준비 직후 Waiting, 미지급, 잔액 100, 쓰기 0, 자동 저장 비활성이고 SaveNow는 false로 쓰지 않는다.
- 보상 준비 후 T0 + 7000000000에서 받으면 성공하고 Done, 지급됨, 잔액 700, 자동 저장 활성, 쓰기 1회다.
- 성공한 저장의 금화는 700이고 저장 시각은 T0 + 7000000000이다.
- 성공 후 저장 시각에서 50000000틱 경과한 계산에는 보상이 없다.
- 성공 후 다시 받으면 true이고 잔액 700, 쓰기 1회를 유지한다.
- 저장 실패 시 false, SaveFailed, 지급됨, 잔액 700, 시도 1회이고 HasFailed와 IsDirty가 true다.
- 저장 실패 후 다시 실패하면 false이고 잔액 700, 시도 2회다.
- 두 실패 후 T0 + 8000000000에 성공하면 Done, 잔액 700, 시도 3회, HasFailed false이며 저장 금화는 700, 저장 시각은 T0 + 8000000000이다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- UnityEngine, 화면, 저장·금화·스테이지 기능 내부를 수정하지 않는다.
- 씬·프리팹·에셋, 어셈블리 정의와 Capture 테스트를 수정하지 않는다.

## 보고서

### 한 일
- 보상을 한 번 지급하고 저장 실패 시 저장만 재시도하는 Core 상태와 처리를 구현했다.
- 생성자 검증, 지급 전 상태, 저장 성공과 중복 호출, 연속 실패와 재시도 성공을 검증하는 에딧 모드 테스트를 작성했다.

### 바뀐 파일
- Assets/Scripts/Core/OfflineReward/OfflineRewardClaim.cs 및 .meta
- Assets/Scripts/Core/OfflineReward/OfflineRewardClaimState.cs 및 .meta
- Assets/Tests/EditMode/OfflineReward/OfflineRewardClaimTests.cs 및 .meta

### 가정
- 없음. 공개 계약과 동작은 기존 오프라인 보상 사양을 따른다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 기존 테스트를 포함한 전체 테스트의 루프 판정을 기다린다.


## 루프 기록 (2026-11-17 15:20)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/034_offline_claim
- 루프 브랜치: loop/20261117-151529
