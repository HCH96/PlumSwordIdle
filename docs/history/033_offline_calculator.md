# 033_offline_calculator — 오프라인 보상 계산

## 목표
Core에서 오프라인 보상과 복귀 판단을 계산한다.

## 묶음
오프라인보상

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/offline_reward.md
- docs/spec/stage.md (연결 지점 절)

## 요구사항
- Assets/Scripts/Core/OfflineReward/에 사양의 OfflineRewardCalculator, OfflineRewardResult를 공개 계약 그대로 구현한다. 시각은 인자로 받고 StageRewards.NormalGold를 사용한다.
- 10초 간격, 8시간 상한, 버림, 음수·역행 시각 및 스테이지 검증과 복귀 판단은 사양의 동작을 따른다. 테스트의 T0은 638000000000000000이다.

## 완료 조건
- Assets/Tests/EditMode/OfflineReward/OfflineRewardCalculatorTests.cs에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- IntervalTicks는 100000000이고 MaxTicks는 288000000000이다.
- T0에서 6000000000틱 후 1스테이지 결과는 경과와 적용 6000000000, 60마리, 금화 600, HasReward true, IsCapped false다.
- 250000000틱 경과는 적용 200000000틱, 2마리, 금화 20이다.
- 99999999틱 경과는 적용 0, 처치 0, 금화 0이며 보상이 없다.
- 100000000틱 경과는 1마리, 금화 10이며 보상이 있다.
- 324000000000틱 경과는 적용 288000000000, 2880마리, 금화 28800이며 상한을 초과한다.
- 정확히 288000000000틱 경과는 금화 28800이며 상한 초과가 아니다.
- 6000000000틱 경과에서 3스테이지는 금화 840, 6스테이지는 금화 1200이다.
- 같은 시각과 역행한 시각은 각각 경과와 적용 및 금화가 0이고 보상과 상한 초과가 없다.
- 저장 시각 또는 현재 시각이 음수인 경우와 long.MinValue, long.MaxValue 조합은 예외 없이 경과와 금화가 0이다.
- 저장 시각 0, 현재 T0은 경과 T0, 적용 288000000000, 금화 28800이며 상한을 초과한다.
- stageNumber 0과 -1은 경과가 0이어도 ArgumentOutOfRangeException이다.
- 9시간 경과와 int.MaxValue 스테이지는 2880마리, 금화 12369505829760이다.
- 저장이 최신이고 정확히 100000000틱 경과한 복귀는 재시작한다.
- 99999999틱 경과, 최신이 아닌 저장, 역행 시각, 음수 시각의 복귀는 각각 재시작하지 않는다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- UnityEngine, 화면, 저장·스테이지 기능 내부를 수정하지 않는다.
- 씬·프리팹·에셋, 어셈블리 정의와 Capture 테스트를 수정하지 않는다.

## 보고서

### 한 일
- 오프라인 경과 시간, 보상 상한과 버림, 스테이지별 금화 및 복귀 재시작 판단을 순수 C#으로 구현했다.
- 완료 조건의 정상값, 경계값, 잘못된 시각과 스테이지 및 복귀 판단을 에딧 모드 테스트로 작성했다.

### 바뀐 파일
- Assets/Scripts/Core/OfflineReward/OfflineRewardCalculator.cs, OfflineRewardResult.cs 및 각 .meta
- Assets/Scripts/Core/OfflineReward.meta
- Assets/Tests/EditMode/OfflineReward/OfflineRewardCalculatorTests.cs 및 .meta
- Assets/Tests/EditMode/OfflineReward.meta
- tasks/doing/033_offline_calculator.md, .loop/result.json

### 가정
- 없음. 공개 계약과 수치는 기존 사양을 따랐다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 새 테스트와 기존 테스트 모두 루프 스크립트의 실행 및 판정을 기다린다.
- git diff --check를 실행했으며 오류가 없었다. 신규 파일은 해당 검사에 포함되지 않는다.


## 루프 기록 (2026-11-17 15:18)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/033_offline_calculator
- 루프 브랜치: loop/20261117-151529
