# 036_offline_view — 오프라인 보상 창

## 목표
Game에서 오프라인 보상 창과 받기 버튼을 만든다.

## 묶음
오프라인보상

## 선행 작업
- 033_offline_calculator
- 034_offline_claim
- 035_offline_text

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/offline_reward.md
- docs/dev/art.md

## 요구사항
- Assets/Scripts/Game/OfflineReward/OfflineRewardView.cs에 사양의 공개 Initialize 계약과 화면 절의 가림막·패널·글자·버튼을 코드로 조립한다. 좌표, 크기, 색, 입력 차단과 조건부 표시를 사양 그대로 따른다.
- 버튼은 현재 UTC 틱을 전달해 TryClaim을 호출하고 성공 콜백은 한 번만 호출한다. 실패하면 창에 남아 실패 문구와 RETRY를 표시한다. TMP 리소스가 없어도 게임이 예외로 멈추지 않게 한다.
- 화면은 GameRoot에 아직 조립하지 않는다. 다음 작업이 조립한다.

## 완료 조건
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다.
- [사람 확인] 10분 전 저장 파일로 실행했을 때 보상 창의 시간·금화·CLAIM과 가림막 배치가 사양과 맞는지 확인한다.
- [사람 확인] 9시간 전 저장 파일로 실행했을 때 적용 8시간 줄이 보이는지 확인한다.
- [사람 확인] 저장 실패를 유도해 RETRY 표시와 재시도를 확인한다.

## 하지 말 것
- Core와 기존 기능의 내부, GameRoot 조립을 수정하지 않는다.
- 씬·프리팹·에셋, 어셈블리 정의와 Capture 테스트를 수정하지 않는다.

## 보고서

### 한 일
- 사양의 가림막, 보상 창, 조건부 문구와 받기 버튼을 코드로 조립했다.
- 현재 UTC 틱으로 보상을 받고 저장 실패 시 RETRY를 표시하며 성공 콜백의 중복 호출을 막았다.
- TMP 리소스가 없을 때 글자 생성과 변경 예외를 처리하고 화면 소멸 시 가림막도 정리한다.
- GameRoot 조립은 다음 작업에 남겼다.

### 바뀐 파일
- Assets/Scripts/Game/OfflineReward.meta
- Assets/Scripts/Game/OfflineReward/OfflineRewardView.cs 및 .meta
- tasks/doing/036_offline_view.md (보고서)
- .loop/result.json

### 가정
- 화면 소유 오브젝트가 외부에서 파괴되는 경우에도 캔버스의 가림막을 함께 제거한다.
- 공개 계약과 화면 규칙은 기존 사양과 같아 사양 변경은 없다.

### 사람이 확인할 것
- 10분 전 저장 파일로 실행했을 때 보상 창의 시간·금화·CLAIM과 가림막 배치가 사양과 맞는지 확인한다.
- 9시간 전 저장 파일로 실행했을 때 적용 8시간 줄이 보이는지 확인한다.
- 저장 실패를 유도해 RETRY 표시와 재시도를 확인한다.

### 테스트 결과
- Unity 테스트와 컴파일은 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.
- 사양에서 OfflineRewardView를 자동 테스트 대상에서 제외하므로 화면 테스트는 추가하지 않았다.
- git diff --check 통과, 새 메타 GUID 중복 없음. 새 파일은 diff --check 대상에 포함되지 않는다.


## 루프 기록 (2026-11-17 15:25)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/036_offline_view
- 루프 브랜치: loop/20261117-151529
