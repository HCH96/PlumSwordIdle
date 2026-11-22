# 086_boss_hud_cleanup — 보스 구간 화면 정리: 이름 글자·진행 선 숨김, 체력 틀 올리기

## 목표
Game(`PlumSword.Game.Stage`)의 보스 구간 화면을 정리한다. 보스 체력 틀 옆의 영문 보스 이름 글자와, 보스 구간에서 꽉 찬 분홍 줄로 남던 진행 선을 숨긴다. 보스 체력 틀은 위쪽 진행 표시 바로 아래로 올린다. 보스 이름은 085의 그림 글자가 진행 표시 안에 보여 준다.

## 묶음
stage_hud

## 선행 작업
- 085_stage_label_view

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stage.md ("화면"의 보스 체력 틀과 진행 표시 항목)

## 요구사항
- **보스 이름 글자 숨김**: `StageView`의 TMP `BossName`은 보스 구간에서도 늘 비활성이다. 지우지 않고, 글자(`StageText.ThemedBossName`)는 지금처럼 계속 갱신한다. 기존 테스트가 이 글자를 읽는다. 옛 종남 헤더 경로에서도 비활성이다.
- **진행 선 숨김**: `SubstagePreview.SetMarkersVisible(bool)`이 표식 6개와 `BossMarker`와 함께 `Track`, `TrackHighlight`, `Progress`도 같은 값으로 켜고 끈다. 그래서 보스 구간(`IsBossActive`)에서는 진행 선이 보이지 않고, 일반 구간에서는 다시 보인다.
- **보스 체력 틀 위치**: 틀의 가운데를 (0, −126)에서 **(0, −112)**로 올린다. 크기 300×100, 위쪽 가운데 앵커·가운데 피벗은 그대로다. 다섯 틀 모두 같은 높이다. 진행 표시 제목판의 아래 끝(−84.5)에서 가장 높은 틀(5번, 그림 윗변이 가운데보다 25.8 위)의 윗변이 −86.2에 오는 값이다.
  - 게이지(`StageBar`) 위치 공식의 −126도 −112로 바꾼다: 중심 y `−112 − ((위 + 아래 + 1)/2 − 362)·k`. x·너비·높이 공식과 게이지 칸 표는 그대로다.
  - 틀이 없을 때 쓰는 대체 바(180×7)의 가운데도 (0, −126)에서 (0, −112)로 옮긴다.
- 끝나면 `docs/spec/stage.md` "화면"을 고친다: 보스 이름 글자를 표시하지 않음, 보스 구간의 진행 선 숨김, 틀 중심 (0, −112). "수치" 표의 "보스 체력 틀 크기 / 중심"도 고친다.
- **기존 테스트**: 이름은 그대로 두고 값만 바꾼다. 어서트 수는 줄이지 않는다.
  - `Assets/Tests/PlayMode/Stage/StageHudTests.cs` `ProgressHud_ChangesFromProgressToBossHealth`: 보스 구간 확인 부분의 `Assert.That(bossName.activeSelf, Is.True)` → `Is.False`. 바로 다음 줄의 `BossName` 글자 검사는 그대로다.
  - `Assets/Tests/PlayMode/Stage/StageFinalHudTests.cs` `FinalStage_BeginsWithFinalBossHud`: `Assert.That(bossName.gameObject.activeSelf, Is.True)` → `Is.False`. `"HEAVENLY DEMON"` 글자 검사는 그대로다.
  - 같은 파일 `NormalAndBossStages_UpdateMarkersAndBossHud`: `canvas.transform.Find("BossName").gameObject.activeSelf`의 `Is.True` → `Is.False`.
  - `Assets/Tests/PlayMode/Stage/BossHealthFrameTests.cs` `BossStage_ShowsMatchingFrame`: `CheckRect(frame, 0, -126, 300, 100)` → `CheckRect(frame, 0, -112, 300, 100)`. 1번 틀의 `CheckRect(Rect("StageBar"), 13.7f, -127.4f, 215.6f, 8.3f)` → `CheckRect(Rect("StageBar"), 13.7f, -113.4f, 215.6f, 8.3f)`. 형제 순서와 5번 틀 크기 검사는 그대로다.
  - 같은 파일 `MissingFrame_UsesFallbackBar`: `CheckRect(Rect("StageBar"), 0, -126, 180, 7)` → `CheckRect(Rect("StageBar"), 0, -112, 180, 7)`.

## 완료 조건
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다 (위 기존 테스트는 요구사항에 적은 값으로).
- [플레이 모드] `Assets/Tests/PlayMode/Stage/BossHudCleanupTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다. 기존 `BossHealthFrameTests`처럼 `StageView`를 직접 만들어 보스 구간까지 진행한다.
- [플레이 모드] 시작 번호 1로 보스 구간에 들어가면 `SubstagePreview/Track`, `SubstagePreview/TrackHighlight`, `SubstagePreview/Progress`가 모두 비활성이다.
- [플레이 모드] 시작 번호 1로 `Begin()` 직후(일반 1구간) `SubstagePreview/Track`, `TrackHighlight`, `Progress`는 모두 활성이다.
- [플레이 모드] 시작 번호 1로 보스 구간에 들어가면 `BossName`은 비활성이다.
- [플레이 모드] 시작 번호 3으로 보스 구간에 들어가면 `BossHealthFrame`의 `anchoredPosition`은 (0, −112)(오차 0.1)이다.
- [사람 확인] 보스 구간에서 보스 체력 틀이 위쪽 진행 표시 바로 아래에 붙어 있고, 틀 위에 영문 이름이나 분홍 줄이 보이지 않는다. 에디터에서 1스테이지 보스까지 진행해 확인한다.

## 하지 말 것
- `Assets/Scripts/Core/`를 고치지 않는다.
- 085가 만든 그림 글자(`StageTitle`, `SubstageNumberArt`, `BossNameArt`)의 위치·크기를 바꾸지 않는다.
- 보스 체력 틀의 크기, 게이지 칸 표, 채움 색, `BarRatio` 동작을 바꾸지 않는다.
- `BossName` TMP 오브젝트를 지우거나 이름을 바꾸지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/`를 건드리지 않는다.

## 보고서

### 한 일
- 보스 이름 TMP는 숨긴 채 텍스트 갱신을 유지하고, 보스 구간에서 진행 선과 표식을 함께 숨기도록 했다.
- 보스 체력 틀과 게이지 및 대체 바를 위로 옮기고 스테이지 사양을 갱신했다.
- 기존 테스트의 기대값을 바꾸고 진행 선, 이름, 틀 위치와 일반 구간 복귀를 확인하는 플레이 모드 테스트를 추가했다.
- 관문에서 지적한 일반·보스 구간 테스트에 진행 선 세 요소의 활성 상태 검사를 각각 직접 넣었다.

### 바뀐 파일
- `Assets/Scripts/Game/Stage/StageView.cs`, `SubstagePreview.cs`
- `Assets/Tests/PlayMode/Stage/StageHudTests.cs`, `StageFinalHudTests.cs`, `BossHealthFrameTests.cs`, `BossHudCleanupTests.cs` 및 그 `.meta`
- `docs/spec/stage.md`
- `tasks/doing/086_boss_hud_cleanup.md`, `.loop/result.json`

### 가정
없음.

### 사람이 확인할 것
- 에디터에서 1스테이지 보스까지 진행해 보스 체력 틀이 위쪽 진행 표시 바로 아래에 붙어 있고, 틀 위에 영문 이름이나 분홍 줄이 보이지 않는지 확인한다.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며 컴파일과 플레이 모드 테스트, 관문 결과는 루프 판정을 기다린다.
- `git diff --check`로 변경된 추적 파일의 공백 오류가 없는지 확인했다.


## 루프 기록 (2026-11-22 14:16)

- 작업 에이전트: codex(시도 1~2)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/086_boss_hud_cleanup
- 루프 브랜치: loop/20261122-135729
