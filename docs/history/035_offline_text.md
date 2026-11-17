# 035_offline_text — 오프라인 보상 표시 문구

## 목표
Core에서 보상 창에 표시할 문구를 만든다.

## 묶음
오프라인보상

## 선행 작업
- 034_offline_claim

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/offline_reward.md
- docs/spec/currency.md (연결 지점 절)

## 요구사항
- Assets/Scripts/Core/OfflineReward/OfflineRewardText.cs에 사양의 상수와 공개 메서드를 구현한다.
- 시간은 초 단위로 내리고 불변 문화권의 시:분:초로 표시한다. 금화는 GoldText.Format을 사용한다.

## 완료 조건
- Assets/Tests/EditMode/OfflineReward/OfflineRewardTextTests.cs에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- FormatDuration은 324000000000, 6000000000, 250000000, 99999999, 0, 3600000000000을 각각 9:00:00, 0:10:00, 0:00:25, 0:00:09, 0:00:00, 100:00:00으로 표시한다.
- FormatDuration(-1)은 ArgumentOutOfRangeException이다.
- ElapsedLine(324000000000)은 AWAY 9:00:00이다.
- AppliedLine(288000000000)은 REWARD FOR 8:00:00 (MAX)이다.
- GoldLine(28800)은 G 28,800이다.
- Title, SaveFailedMessage, ClaimButton, RetryButton은 각각 OFFLINE REWARD, SAVE FAILED. TAP RETRY., CLAIM, RETRY다.
- ButtonLabel은 SaveFailed에서 RETRY, Waiting·Done·정의되지 않은 99에서 CLAIM이다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- UnityEngine, 화면과 금화 기능 내부를 수정하지 않는다.
- 씬·프리팹·에셋, 어셈블리 정의와 Capture 테스트를 수정하지 않는다.

## 보고서

### 한 일
- 오프라인 보상의 시간, 금화, 버튼과 고정 문구를 사양에 맞게 구현했다.
- 완료 조건과 불변 문화권 표시를 검사하는 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- Assets/Scripts/Core/OfflineReward/OfflineRewardText.cs 및 .meta
- Assets/Tests/EditMode/OfflineReward/OfflineRewardTextTests.cs 및 .meta
- tasks/doing/035_offline_text.md (보고서)
- .loop/result.json

### 가정
없음. 공개 연결 지점은 사양과 같다.

### 사람이 확인할 것
없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 추가 및 기존 테스트의 루프 판정을 기다린다.
- git diff --check 통과 (새 파일은 추적 전 상태).


## 루프 기록 (2026-11-17 15:23)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/035_offline_text
- 루프 브랜치: loop/20261117-151529
