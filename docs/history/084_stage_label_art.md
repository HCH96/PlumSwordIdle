# 084_stage_label_art — 스테이지 제목·번호·보스 이름 그림 글자 배치 계산

## 목표
Core(`PlumSword.Core.Stage`)에 표시용 클래스 `StageLabelArt`를 만든다. 진행 표시와 구간 시작 패에 올릴 그림 글자를 계산한다: 스테이지 제목 그림, 금빛 번호 글자, 보스 이름 그림 가운데 무엇을 쓸지, 번호 글자의 시트 위치와 가로 배치. 화면(085)은 이 계산대로 그리기만 한다.

## 묶음
stage_hud

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stage.md ("화면", "연결 지점"의 `StageText.SubstageLabel`)

## 요구사항
- 파일 `Assets/Scripts/Core/Stage/StageLabelArt.cs`, `public static class StageLabelArt` (네임스페이스 `PlumSword.Core.Stage`).
- 그림 에셋은 이미 커밋되어 있다 (`Assets/Resources/Art/UI/Substage/Label/`): `header_blank`(1536×505, 글자를 지운 제목판), `stage_numbers`(2132×300, 금빛 0~9와 `-`), `title_1`~`title_5`(스테이지 제목: 매화 아래 비무, 개방 분타, 남궁세가 연무장, 천마신교 외성, 천마 교주전), `boss_name_1`~`boss_name_5`(보스 이름: 장로 진무현, 방주 홍무진, 가주 남궁혁, 호법 사도진, 교주 이천상). 제목·이름 그림은 투명 여백을 잘라 둔 것이라 크기가 서로 다르다.
- 경로 상수와 메서드:
  - `public const string HeaderBlankPath = "Art/UI/Substage/Label/header_blank";`
  - `public const string NumberSheetPath = "Art/UI/Substage/Label/stage_numbers";`
  - `public static string TitlePath(int stage)` → `"Art/UI/Substage/Label/title_{stage}"`
  - `public static string BossNamePath(int stage)` → `"Art/UI/Substage/Label/boss_name_{stage}"`
  - `stage`는 화면상 스테이지 번호(1~5)이며, 밖이면 `ArgumentOutOfRangeException`. 문자열은 문화권과 무관하게 만든다.
- `public static bool UsesBossName(int stage, int substage)`: `stage`가 5이거나 `substage`가 7이면 `true`, 아니면 `false`. `stage` 1~5, `substage` 1~7 밖이면 `ArgumentOutOfRangeException`. 보스 구간과 5스테이지에서는 번호(`4-7`, `FINAL`) 대신 보스 이름 그림을 쓴다는 뜻이다.
- `public static int[] NumberGlyphs(int stage, int substage)`: `stage` 1~4, `substage` 1~6일 때 `{stage, 10, substage}`를 새 배열로 돌려준다 (10은 `-`). 그 밖(5스테이지, 7구간, 범위 밖)은 `ArgumentOutOfRangeException`.
- 번호 시트의 글자 위치(시트 왼쪽 위 기준 px). 글자 번호 0~9는 숫자, 10은 `-`.
  - `public const int HyphenGlyph = 10;`
  - `public static int GlyphLeft(int glyph)`: 20, 225, 397, 594, 784, 985, 1190, 1382, 1574, 1766, 1962
  - `public static int GlyphWidth(int glyph)`: 185, 152, 177, 170, 181, 185, 172, 172, 172, 176, 150
  - `public const int GlyphTop = 8;`, `public const int GlyphHeight = 290;` (모든 글자 공통. 시트 위에서 8~297줄)
  - `public const int GlyphOverlap = 18;` (이웃 글자를 이만큼 겹쳐 붙인다)
  - `glyph`가 0~10 밖이면 `ArgumentOutOfRangeException`.
- `public static double LabelWidth(int[] glyphs, double scale)`: `(Σ GlyphWidth − GlyphOverlap × (개수 − 1)) × scale`. 빈 배열이면 0. `glyphs`가 `null`이면 `ArgumentNullException`, `scale`이 0 이하·NaN·무한대이면 `ArgumentOutOfRangeException`.
- `public static double GlyphCenterX(int[] glyphs, int index, double scale)`: 전체를 가운데(0) 정렬했을 때 `index`번째 글자의 가운데 x. `−LabelWidth/2 + (Σ_{j<index} (GlyphWidth(j) − GlyphOverlap) + GlyphWidth(index)/2) × scale`. `index`가 범위 밖이면 `ArgumentOutOfRangeException`. 인자 검사는 `LabelWidth`와 같다.
- `UnityEngine`을 쓰지 않는다. 끝나면 `docs/spec/stage.md`의 "연결 지점"에 `StageLabelArt`를 더하고, "화면"에 "제목·번호·보스 이름은 그림 글자(`StageLabelArt`)"를 적는다. 085가 위 이름을 그대로 쓴다.

## 완료 조건
- `Assets/Tests/EditMode/Stage/StageLabelArtTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `StageLabelArt.TitlePath(1)`은 `"Art/UI/Substage/Label/title_1"`이고 `BossNamePath(5)`는 `"Art/UI/Substage/Label/boss_name_5"`다.
- `TitlePath(0)`, `TitlePath(6)`, `BossNamePath(0)`은 각각 `ArgumentOutOfRangeException`을 던진다.
- `UsesBossName(1, 1)`과 `UsesBossName(4, 6)`은 `false`다.
- `UsesBossName(1, 7)`, `UsesBossName(4, 7)`, `UsesBossName(5, 7)`은 `true`다.
- `UsesBossName(0, 1)`, `UsesBossName(1, 0)`, `UsesBossName(1, 8)`은 각각 `ArgumentOutOfRangeException`을 던진다.
- `NumberGlyphs(1, 1)`은 {1, 10, 1}이고 `NumberGlyphs(4, 6)`은 {4, 10, 6}이다.
- `NumberGlyphs(5, 7)`, `NumberGlyphs(1, 7)`, `NumberGlyphs(0, 1)`은 각각 `ArgumentOutOfRangeException`을 던진다.
- `GlyphLeft(0)`은 20, `GlyphWidth(0)`은 185, `GlyphLeft(10)`은 1962, `GlyphWidth(10)`은 150이다.
- `GlyphLeft(11)`과 `GlyphWidth(-1)`은 각각 `ArgumentOutOfRangeException`을 던진다.
- `LabelWidth({1, 10, 1}, 1.0)`은 418이고 `LabelWidth({4, 10, 6}, 0.5)`는 233.5다.
- `LabelWidth(new int[0], 1.0)`은 0이다.
- `LabelWidth(null, 1.0)`은 `ArgumentNullException`을, `LabelWidth({1}, 0)`은 `ArgumentOutOfRangeException`을 던진다.
- `GlyphCenterX({1, 10, 1}, i, 1.0)`은 i = 0, 1, 2에서 −133, 0, 133이다 (오차 1e-9).
- `GlyphCenterX({4, 10, 6}, i, 1.0)`은 i = 0, 1, 2에서 −143, 4.5, 147.5다 (오차 1e-9).
- `GlyphCenterX({1, 10, 1}, 3, 1.0)`은 `ArgumentOutOfRangeException`을 던진다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. 085가 한다.
- `StageText`의 기존 메서드와 결과를 바꾸지 않는다.
- 그림 파일과 `.meta`를 고치지 않는다.
- `UnityEngine`을 쓰지 않는다.

## 보고서

### 한 일
- 스테이지 제목과 보스 이름의 경로, 그림 선택, 번호 글자 시트 좌표와 가운데 정렬 배치를 순수 C#으로 구현했다.
- 경계값, 문화권, 글자 좌표와 배치 계산을 검사하는 에딧 모드 테스트를 추가하고 스테이지 사양과 색인을 갱신했다.

### 바뀐 파일
- `Assets/Scripts/Core/Stage/StageLabelArt.cs` 및 `.meta`
- `Assets/Tests/EditMode/Stage/StageLabelArtTests.cs` 및 `.meta`
- `docs/spec/stage.md`, `docs/spec/index.md`
- `tasks/doing/084_stage_label_art.md`, `.loop/result.json`

### 가정
- 여러 인자가 동시에 유효하지 않으면 먼저 검사한 인자의 예외를 반환한다. 글자 배열과 배율은 인덱스보다 먼저 검사한다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- `git diff --check` 통과.
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 새 테스트와 기존 테스트의 결과는 루프의 판정을 기다린다.


## 루프 기록 (2026-11-22 14:02)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/084_stage_label_art
- 루프 브랜치: loop/20261122-135729
