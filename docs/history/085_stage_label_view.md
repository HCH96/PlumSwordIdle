# 085_stage_label_view — 진행 표시와 구간 시작 패를 그림 글자로 표시

## 목표
Game(`PlumSword.Game.Stage`)의 작은 스테이지 진행 표시(`SubstagePreview`)와 구간 시작 번호 패(`SubstageIntroBanner`)를 바꾼다. 스테이지마다 다른 제목 그림, 금빛 번호 그림 글자(`1-1`~`4-6`), 보스 구간의 보스 이름 그림으로 표시한다. 번호가 제목판 안쪽 칸의 세로 가운데에 오게 한다.

## 묶음
stage_hud

## 선행 작업
- 084_stage_label_art

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/dev/assets.md (작은 스테이지 진행 UI 줄)
- docs/spec/stage.md ("화면", "연결 지점". 084가 `StageLabelArt`를 더했다)

## 요구사항
- **쓸지 정하기**: `SubstagePreview.Create`에서 `StageLabelArt.HeaderBlankPath`, `NumberSheetPath`(2132×300이 아니면 없는 것으로 본다), `TitlePath(1~5)`, `BossNamePath(1~5)`를 `Resources.Load`로 모두 불러, 하나라도 없으면 지금 화면을 그대로 쓴다 (`Art/UI/Substage/header` + 글자 번호). 모두 있으면 아래 "그림 글자 모드"로 그린다. `SubstageIntroBanner.Create`도 번호 시트와 보스 이름 다섯 장으로 같은 판단을 한다.
- **번호 글자 스프라이트**: `stage_numbers` 텍스처에서 글자 i를 `Sprite.Create(texture, new Rect(GlyphLeft(i), 300 − GlyphTop − GlyphHeight, GlyphWidth(i), GlyphHeight), 가운데 피벗, 32)`로 자른다 (`Rect`의 y는 아래 기준이라 2). 한 번만 만들어 둔다.
- **그림 글자 모드 — 진행 표시** (`SubstagePreview`, 기존 좌표계: 루트는 위쪽 가운데 기준 300×112, `ImageAt`의 위치는 그림의 가운데 y). 기준 배율 `k = 260 / 1536`:
  - `Header` 이미지의 스프라이트를 `header_blank`로 바꾼다. 위치·크기(가운데 (0, −42), 260×85)는 그대로다.
  - 새로 만드는 `StageTitle`, `SubstageNumberArt`, `BossNameArt`는 모두 앵커 위쪽 가운데(0.5, 1), 피벗 가운데(0.5, 0.5)로 두고 `anchoredPosition`이 그림의 가운데가 되게 한다.
  - 새 `Image` `StageTitle`: 화면상 스테이지 n의 `title_n`. `anchoredPosition` (0, −29.3), 높이 13.2, 너비 = 13.2 × 스프라이트 가로/세로 비율. 입력을 가로채지 않는다.
  - 새 빈 오브젝트 `SubstageNumberArt`(`anchoredPosition` (0, −61)): 일반 구간에서 `StageLabelArt.NumberGlyphs(stage, substage)`의 글자마다 자식 `Image`를 하나씩 둔다. 이름은 그 글자(`"1"`, `"-"`, `"6"`)다. 글자 배율 `s = 0.5 × k`이고, 자식의 크기는 (`GlyphWidth × s`, `GlyphHeight × s`), x는 `StageLabelArt.GlyphCenterX(glyphs, i, s)`, y는 0이다. 번호가 바뀔 때 다시 만든다.
  - 새 `Image` `BossNameArt`(`anchoredPosition` (0, −61)): `StageLabelArt.UsesBossName`이 `true`인 구간(보스 구간, 5스테이지)에서 `boss_name_n`을 높이 20, 너비 = 20 × 비율로 보인다. 이때 `SubstageNumberArt`는 숨기고, 아니면 반대다.
  - 기존 TMP `SubstageNumber`는 지우지 않고 비활성화만 한다. 글자(`StageText.SubstageLabel`)는 지금처럼 계속 갱신한다. 기존 테스트가 이 글자를 읽는다.
  - 가운데 −61은 제목판 원본의 안쪽 칸(위에서 265~461줄)의 세로 가운데(363줄)를 UI로 옮긴 값이다: 0.5 − 363 × k. 지금 글자 번호가 아래로 치우친 문제(가운데보다 약 4.5 아래)를 이것으로 고친다.
- **그림 글자 모드 — 구간 시작 패** (`SubstageIntroBanner`, 가운데 기준 360×120, 패 그림이 가득 참). 기준 배율 `k2 = 360 / 2172`:
  - 새로 만드는 오브젝트는 앵커·피벗 모두 가운데(0.5, 0.5)다.
  - 새 빈 오브젝트 `SubstageNumberArt`(`anchoredPosition` (0, −5.5)): 진행 표시와 같은 방식이고 글자 배율은 `0.78 × k2`다.
  - 새 `Image` `BossNameArt`(`anchoredPosition` (0, −5.5)): 보스 구간·5스테이지에서 `boss_name_n`, 높이 29, 너비 = 29 × 비율.
  - 기존 TMP `SubstageNumber`는 비활성화하고 글자는 계속 갱신한다. 등장·퇴장 연출은 그대로다.
  - −5.5는 패 원본의 안쪽 칸(위에서 272~519줄)의 가운데(395줄)를 옮긴 값이다: (362 − 395) × k2.
- 옛 종남 헤더 경로(`JongnamStageHeader`, `JongnamStageTitle`)와 대체 헤더(`StageHeader`)는 그대로 둔다.
- 끝나면 `docs/spec/stage.md` "화면"의 진행 표시·구간 소개 항목을 그림 글자 기준으로 고치고, `docs/dev/assets.md`에 `Art/UI/Substage/Label/`의 그림(빈 제목판, 번호 시트와 글자 위치, 제목 5장, 보스 이름 5장, 원본 `art/source/stage_label/`)을 적는다.

## 완료 조건
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다 (`StageHudTests`, `StageFinalHudTests`의 `SubstageNumber` 글자 검사 포함).
- [플레이 모드] `Assets/Tests/PlayMode/Stage/StageLabelViewTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다. 기존 `StageHudTests`처럼 캔버스와 `StageView`를 직접 만들고 `new StageProgress(wallet, new StatUpgrades(wallet, 1000, 10000), new Battle(upgrades), new System.Random(1), 시작 번호)`로 시작한다.
- [플레이 모드] 시작 번호 2로 `Begin()` 뒤 `SubstagePreview/Header`의 스프라이트 이름은 `header_blank`다.
- [플레이 모드] 같은 상황에서 `SubstagePreview/StageTitle`이 활성이고 스프라이트 이름은 `title_2`다.
- [플레이 모드] 같은 상황에서 `SubstagePreview/SubstageNumberArt`가 활성이고 자식 이름이 차례로 `"2"`, `"-"`, `"1"`이다.
- [플레이 모드] 같은 상황에서 `SubstagePreview/SubstageNumberArt`의 `anchoredPosition.y`는 −61(오차 0.1)이다.
- [플레이 모드] 같은 상황에서 `SubstagePreview/SubstageNumber`(TMP)는 비활성이다.
- [플레이 모드] 같은 상황에서 `SubstageIntroBanner/SubstageNumberArt`의 자식 이름이 차례로 `"2"`, `"-"`, `"1"`이다.
- [플레이 모드] 시작 번호 5로 `Begin()` 뒤 `SubstagePreview/BossNameArt`가 활성이고 스프라이트 이름은 `boss_name_5`다.
- [플레이 모드] 같은 상황에서 `SubstagePreview/SubstageNumberArt`는 비활성이다.
- [플레이 모드] 같은 상황에서 `SubstageIntroBanner/BossNameArt`의 스프라이트 이름은 `boss_name_5`다.
- [플레이 모드] 같은 상황에서 `SubstagePreview/StageTitle`의 스프라이트 이름은 `title_5`다.
- [플레이 모드] 시작 번호 3에서 여섯 구간을 이겨 보스 구간에 들어가면(`StageHudTests`의 진행 방식) `SubstagePreview/BossNameArt`의 스프라이트 이름은 `boss_name_3`이다.
- [사람 확인] 게임을 실행하고 10초 뒤 화면 위쪽 제목판에 "매화 아래 비무" 그림 제목과 금빛 `1-1`이 보이고, 번호가 안쪽 칸의 세로 가운데에 있다.
- [사람 확인] 구간이 바뀔 때 가운데 번호 패의 금빛 번호가 패 안쪽 칸의 가운데에 있다. 에디터에서 재생하며 확인한다.

## 하지 말 것
- `Assets/Scripts/Core/`를 고치지 않는다. 084가 했다.
- 보스 이름 글자(`BossName`), 보스 체력 틀 위치, 진행 선(Track) 표시를 바꾸지 않는다. 086이 한다.
- 그림 파일과 `.meta`를 고치지 않는다.
- 기존 TMP `SubstageNumber` 오브젝트를 지우거나 이름을 바꾸지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/`를 건드리지 않는다.

## 보고서

### 한 일
- 진행 표시와 구간 시작 패에 스테이지별 제목, 가운데 정렬한 번호 그림과 보스 이름 그림을 연결했다. 필요한 그림이 없으면 기존 표시를 유지하고 TMP 번호도 계속 갱신한다.
- 번호 변경과 5스테이지 및 3스테이지 보스 진입을 확인하는 플레이 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Game/Stage/SubstagePreview.cs`, `SubstageIntroBanner.cs`, `StageLabelImages.cs` 및 새 파일의 `.meta`
- `Assets/Tests/PlayMode/Stage/StageLabelViewTests.cs` 및 `.meta`
- `docs/spec/stage.md`, `docs/dev/assets.md`, 이 작업 보고서, `.loop/result.json`

### 가정
- 번호 시트에서 잘라낸 스프라이트는 화면 인스턴스 사이에 공유하고 번호가 같으면 자식 이미지를 다시 만들지 않는다. 공개 연결 지점은 변경하지 않았다.

### 사람이 확인할 것
- 게임을 실행하고 10초 뒤 화면 위쪽 제목판에 "매화 아래 비무" 그림 제목과 금빛 `1-1`이 보이고, 번호가 안쪽 칸의 세로 가운데에 있는지 확인한다.
- 구간이 바뀔 때 가운데 번호 패의 금빛 번호가 패 안쪽 칸의 가운데에 있는지 에디터 재생으로 확인한다.

### 테스트 결과
- `git diff --check` 통과.
- Unity 컴파일과 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-22 14:08)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/085_stage_label_view
- 루프 브랜치: loop/20261122-135729
