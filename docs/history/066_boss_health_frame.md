# 066_boss_health_frame — 스테이지별 보스 체력 틀

## 목표
`PlumSword.Game`의 `StageView`가 보스 구간에서 화면상 스테이지 번호에 맞는 보스 체력 틀 그림(`Art/UI/boss_health_frame_1~5`)을 표시하고, 붉은 체력 바(`StageBar`)를 틀의 게이지 칸에 맞춘다. 틀이 없으면 기존 180×7 바를 쓴다.

## 묶음
stage45_final

## 선행 작업
- 065_stage_view_final

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("테스트" 절 포함)
- docs/spec/stage.md ("화면" 절의 "보스 체력 틀" 줄과 게이지 칸 표, "수치" 절, "검증" 절의 완료 조건 51~53, "변경 기록" 절)
- docs/dev/assets.md (boss_health_frame_1~5 줄)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-22 — 4·5스테이지 진행 구체화` 항목 중 보스 체력 틀(완료 조건 51~53)만 구현한다. 그 밖의 규칙은 이미 구현되어 있거나 063~065·067이 한다.
- 틀 그림 `Assets/Resources/Art/UI/boss_health_frame_1.png`~`_5.png`와 `.meta`는 이미 커밋되어 있다 (Sprite 형식, 2172×724px 원본). 새로 만들거나 고치지 말고 `Resources.Load<Sprite>("Art/UI/boss_health_frame_" + n)`으로 불러오기만 한다. 경로 앞부분은 `StageView` 안의 `private static string BossHealthFramePath = "Art/UI/boss_health_frame_";` 필드로 둔다 (readonly 아님). 테스트는 리플렉션(`BindingFlags.NonPublic | BindingFlags.Static`)으로 없는 경로를 넣고 `finally`에서 되돌린다. 경로를 바꾼 뒤 만든 `StageView`에서 틀이 없게 동작하면 된다 (불러오는 시점은 직접 정하고 가정에 적는다).
- `StageView`가 캔버스 아래에 `BossHealthFrame` 이름의 `Image` 오브젝트를 만든다. 위쪽 가운데 기준(기존 `PlaceTopCenter` 사용)으로 크기 300×100, 중심 (0, −126)(임시값)에 두고 `raycastTarget = false`, `preserveAspect`는 쓰지 않는다. 그리는 순서: `StageBar`(어두운 바탕과 `Fill`)보다 위, `BossName`보다 아래 (보스 이름이 틀 위에 그려진다). 형제 순서로 맞춘다.
- `IsBossActive`인 동안, 화면상 스테이지 번호 n(`DisplayStageNumber`, 1~5)의 틀 그림이 있으면 `BossHealthFrame`을 활성화하고 그 그림을 쓴다. 보스가 아니면(일반 구간, 다음 큰 스테이지 배치 뒤) 비활성화한다.
- 틀을 표시하는 동안 `StageBar`를 틀의 게이지 칸에 맞춘다. 게이지 칸은 원본 픽셀(왼쪽 위 기준, 끝 포함)이고 `k = 300 / 2172`이다.
  - 바 너비 `(오른쪽 − 왼쪽 + 1) × k`, 높이 `(아래 − 위 + 1) × k`
  - 중심 x `((왼쪽 + 오른쪽 + 1) / 2 − 1086) × k`, 중심 y `−126 − ((위 + 아래 + 1) / 2 − 362) × k`
  - 칸 표 (n: 왼쪽~오른쪽 / 위~아래): 1: 405~1965 / 342~401, 2: 457~1933 / 324~385, 3: 448~1947 / 334~397, 4: 518~1964 / 351~425, 5: 533~1930 / 322~401.
  - 이 표와 계산은 `StageView` 안의 private 정적 배열·메서드로 둔다.
- 채움 색(`BossFillColor`)과 `BarRatio`에 따른 `Fill`의 `anchorMax.x`는 그대로다. 보스 이름 위치 (0, −111)도 그대로다.
- 틀 그림이 없으면 `BossHealthFrame`을 비활성으로 두고 보스 구간 `StageBar`는 기존대로 180×7, 중심 (0, −126)이다.
- 일반 구간의 `StageBar` 위치·크기·활성 여부는 바꾸지 않는다.
- 새 플레이 모드 테스트는 `Assets/Tests/PlayMode/Stage/BossHealthFrameTests.cs`(클래스 `BossHealthFrameTests`, 네임스페이스 `PlumSword.Game.Tests.Stage`)에 쓴다. 만드는 법은 기존 `StageHudTests`와 같다 (캔버스, `StageView.Initialize`, `stage.Begin()`, 강화 공격력 1000·최대 체력 10000, `new System.Random(1)`, 여섯 구간을 `Tick`과 `Battle.Step()`으로 진행). 크기는 `RectTransform.sizeDelta`, 중심은 `anchoredPosition`으로 비교한다 (기존 `PlaceTopCenter`가 위쪽 가운데 앵커·가운데 피벗이면 그대로, 다르면 사양의 중심과 같은 값이 나오도록 비교 방법을 가정에 적는다).

## 완료 조건
- [플레이 모드] `Assets/Tests/PlayMode/Stage/BossHealthFrameTests.cs`에 아래 [플레이 모드] 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- [플레이 모드] 시작 번호 1·2·3·4·5 각각에서 보스 구간이 되면 `BossHealthFrame`은 활성이다.
- [플레이 모드] 위 상황에서 `BossHealthFrame`의 `Image.sprite.name`은 `boss_health_frame_{n}`(n = 시작 번호)이다.
- [플레이 모드] 위 상황에서 `BossHealthFrame`의 `raycastTarget`은 거짓이다.
- [플레이 모드] 위 상황에서 `BossHealthFrame`의 크기는 300×100, 중심은 (0, −126)이다 (오차 0.1).
- [플레이 모드] 시작 번호 1의 보스 구간에서 `StageBar`의 크기는 약 215.6×8.3, 중심은 약 (13.7, −127.4)이다 (오차 0.1).
- [플레이 모드] 시작 번호 5의 보스 구간에서 `StageBar`의 크기는 약 193.1×11.0이다 (오차 0.1).
- [플레이 모드] 시작 번호 1의 보스 구간에서 형제 순서는 `StageBar` < `BossHealthFrame` < `BossName`이다.
- [플레이 모드] 시작 번호 1의 일반 구간(`Begin()` 직후)에서 `BossHealthFrame`은 비활성이다.
- [플레이 모드] 위 일반 구간에서 `StageBar`는 기존처럼 비활성이다.
- [플레이 모드] 시작 번호 1에서 보스를 이겨 다음 큰 스테이지(2스테이지 1구간)가 배치되면 `BossHealthFrame`은 비활성이다.
- [플레이 모드] `BossHealthFramePath`를 없는 경로로 바꾸고 시작 번호 1의 보스 구간이 되면 `BossHealthFrame`은 없거나 비활성이다.
- [플레이 모드] 위 상황에서 `StageBar`의 크기는 180×7, 중심은 (0, −126)이다.
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다.
- [사람 확인] 에디터에서 1스테이지 보스 구간(종남 장로)까지 진행하면 화면 위쪽에 종남 보스 체력 틀이 보이고 붉은 체력 바가 틀의 게이지 칸 안에 어긋남 없이 들어가며, 보스 이름이 틀 위에 가려지지 않고 보인다.

## 하지 말 것
- `Assets/Resources/Art/UI/boss_health_frame_*.png`와 `.meta`를 새로 만들거나 고치지 않는다.
- `Assets/Scripts/Core/`는 고치지 않는다.
- 065가 만든 번호 글자·표식 숨김·쓰러짐 대기 동작을 바꾸지 않는다.
- 일반 구간의 진행 표시·`StageBar`를 바꾸지 않는다.
- `CombatView`·`JongnamSprites`·`SubstagePreview`·`SubstageIntroBanner`는 고치지 않는다.
- 기존 테스트를 지우거나 이름을 바꾸거나 고치지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 바꾸지 않는다. 화면은 코드로 조립한다.
- `Assets/Tests/PlayMode/Capture/`는 고치지 않는다.

## 보고서

### 한 일
- 화면상 스테이지에 맞는 보스 체력 틀과 게이지 칸에 맞춘 체력 바를 표시하고, 틀이 없을 때 기존 크기의 대체 바를 표시한다.
- 일반 구간, 스테이지 전환, 틀 누락과 스테이지별 표시를 검사하는 플레이 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Game/Stage/StageView.cs`
- `Assets/Tests/PlayMode/Stage/BossHealthFrameTests.cs` 및 `.meta`
- `docs/spec/stage.md`
- `tasks/doing/066_boss_health_frame.md`, `.loop/result.json`

### 가정
- 틀 다섯 장은 `StageView.Initialize`에서 불러온다. 이후 같은 뷰에서는 다시 불러오지 않는다.
- 기존 `PlaceTopCenter`의 피벗은 위쪽이므로 보스 틀과 보스 체력 바는 가운데 피벗으로 바꿔 지정된 anchoredPosition이 실제 중심이 되게 한다. 일반 구간은 기존 피벗을 유지한다.
- 공개 연결 지점 변경 없음.

### 사람이 확인할 것
- 에디터에서 1스테이지 보스 구간(종남 장로)까지 진행하면 화면 위쪽에 종남 보스 체력 틀이 보이고 붉은 체력 바가 틀의 게이지 칸 안에 어긋남 없이 들어가며, 보스 이름이 틀 위에 가려지지 않고 보인다.

### 테스트 결과
- `git diff --check` 통과.
- Unity 컴파일과 플레이 모드 테스트는 지시대로 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-22 04:10)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/066_boss_health_frame
- 루프 브랜치: loop/20261122-035241
