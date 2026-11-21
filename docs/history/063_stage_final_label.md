# 063_stage_final_label — 최종 스테이지 상수와 구간 번호 글자

## 목표
`PlumSword.Core`의 스테이지 기능에 최종 스테이지 번호 상수 `StageLayout.FinalDisplayStage`와 구간 번호 글자 `StageText.SubstageLabel(int, int)`를 추가한다. 기존 동작은 바꾸지 않는다.

## 묶음
stage45_final

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stage.md ("연결 지점" 절, "검증" 절의 완료 조건 40, "변경 기록" 절)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-22 — 4·5스테이지 진행 구체화` 항목 중 연결 지점 두 개만 구현한다. 그 밖의 규칙은 이미 구현되어 있거나 뒤 작업(064~067)이 한다.
- `Assets/Scripts/Core/Stage/StageLayout.cs`에 `public const int FinalDisplayStage = 5;`를 추가한다. 설명 주석: 일반 구간 없이 최종 보스만 나오는 화면상 큰 스테이지 번호. 기존 상수와 메서드는 그대로 둔다.
- `Assets/Scripts/Core/Stage/StageText.cs`에 `public static string SubstageLabel(int stage, int substage)`를 추가한다.
  - `stage`가 `StageLayout.FinalDisplayStage`(5)이면 `substage`와 상관없이 `"FINAL"`을 돌려준다.
  - 그 밖에는 `"{stage}-{substage}"`를 문화권과 무관하게 만든다 (`int.ToString(CultureInfo.InvariantCulture)` 사용).
  - `stage`가 1~5 밖이거나 `substage`가 1~7 밖이면 `ArgumentOutOfRangeException`을 던진다 (범위 검사를 FINAL 판단보다 먼저 한다).
- 구현하다 모양이 사양과 달라지면 사양 문서의 연결 지점 절을 고치고 보고서의 가정에 적는다.
- 테스트는 기존 `Assets/Tests/EditMode/Stage/StageTextTests.cs`와 `StageLayoutTests.cs`에 메서드를 추가한다 (기존 메서드는 고치지 않는다).

## 완료 조건
- `Assets/Tests/EditMode/Stage/StageTextTests.cs`와 `StageLayoutTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `StageLayout.FinalDisplayStage`는 5다.
- `StageText.SubstageLabel(5, 7)`은 `"FINAL"`이다.
- `StageText.SubstageLabel(5, 1)`은 `"FINAL"`이다.
- `StageText.SubstageLabel(4, 7)`은 `"4-7"`이다.
- `StageText.SubstageLabel(1, 1)`은 `"1-1"`이다.
- 현재 문화권을 `tr-TR`로 바꾼 상태에서도 `StageText.SubstageLabel(3, 6)`은 `"3-6"`이다 (테스트 끝에 문화권을 되돌린다).
- `StageText.SubstageLabel(0, 1)`과 `(6, 1)`은 각각 `ArgumentOutOfRangeException`을 던진다.
- `StageText.SubstageLabel(1, 0)`과 `(1, 8)`은 각각 `ArgumentOutOfRangeException`을 던진다.
- `StageText.SubstageLabel(5, 0)`과 `(5, 8)`도 각각 `ArgumentOutOfRangeException`을 던진다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `StageProgress`의 동작은 바꾸지 않는다 (064가 한다).
- 화면 코드(`Assets/Scripts/Game/`)는 고치지 않는다 (065가 `SubstageLabel`을 쓴다).
- 기존 `StageText`·`StageLayout` 멤버의 모양과 값을 바꾸지 않는다.
- Core 에서 `UnityEngine`을 쓰지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 최종 화면상 스테이지 상수와 범위 검사 및 문화권 독립 서식을 적용한 구간 번호 글자를 추가했다.
- 상수, 일반·최종 스테이지 표시, 문화권, 입력 경계에 대한 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/Stage/StageLayout.cs`
- `Assets/Scripts/Core/Stage/StageText.cs`
- `Assets/Tests/EditMode/Stage/StageLayoutTests.cs`
- `Assets/Tests/EditMode/Stage/StageTextTests.cs`
- `tasks/doing/063_stage_final_label.md`, `.loop/result.json` (루프 보고)

### 가정
- 없음. 공개 연결 지점과 동작은 기존 사양과 같다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- `git diff --check` 통과.
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며 신규 및 기존 테스트의 실행과 결과 판정은 루프 스크립트를 기다린다.


## 루프 기록 (2026-11-22 03:56)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/063_stage_final_label
- 루프 브랜치: loop/20261122-035241
