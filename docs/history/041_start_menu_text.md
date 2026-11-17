# 041_start_menu_text — 시작 화면·설정 창 표시 글자

## 목표
Core(`PlumSword.Core.StartMenu`)에 시작 화면과 설정 창의 표시 글자 `StartMenuText`를 만든다.

## 묶음
시작화면

## 선행 작업
- 038_start_menu_rules

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/start_menu.md ("수치"의 "표시 글자", "연결 지점" 절)

## 요구사항
- `Assets/Scripts/Core/StartMenu/StartMenuText.cs`, `public static class StartMenuText` (네임스페이스 `PlumSword.Core.StartMenu`).
- 상수 (사양 그대로):
  - `Title = "PLUM BLOSSOM SWORD"` (임시 제목)
  - `NewGameButton = "START"`, `ContinueButton = "CONTINUE"`
  - `SettingsButton = "SETTINGS"`, `SettingsTitle = "SETTINGS"`
  - `ToTitleButton = "TO TITLE"`, `QuitButton = "QUIT GAME"`, `CloseButton = "CLOSE"`
  - `LeaveFailedMessage = "SAVE FAILED. TRY AGAIN."`
- `string StartButtonLabel(StartButtonKind kind)`: `Continue`이면 `ContinueButton`, 나머지(정의되지 않은 값 포함)는 `NewGameButton`. `StartButtonKind`는 038이 만든 `PlumSword.Core.StartMenu.StartButtonKind`다.
- `string BgmLine(int volume)`: `"BGM "` + `volume`의 십진수. `string SfxLine(int volume)`: `"SFX "` + 같은 규칙. 십진수는 `CultureInfo.InvariantCulture`, 자릿수 구분 없음, 잘라내지 않음 (−5 → `-5`). 예외 없음.
- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/start_menu.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 테스트 네임스페이스는 `PlumSword.Core.Tests.StartMenu`.

## 완료 조건
- `Assets/Tests/EditMode/StartMenu/StartMenuTextTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `Title`, `NewGameButton`, `ContinueButton`은 각각 `"PLUM BLOSSOM SWORD"`, `"START"`, `"CONTINUE"`이다.
- `SettingsButton`, `SettingsTitle`은 둘 다 `"SETTINGS"`이다.
- `ToTitleButton`, `QuitButton`, `CloseButton`은 각각 `"TO TITLE"`, `"QUIT GAME"`, `"CLOSE"`이다.
- `LeaveFailedMessage`는 `"SAVE FAILED. TRY AGAIN."`이다.
- `StartButtonLabel(Continue)`는 `"CONTINUE"`, `StartButtonLabel(NewGame)`은 `"START"`이다.
- `StartButtonLabel((StartButtonKind)99)`는 `"START"`이다.
- `BgmLine(80)`은 `"BGM 80"`, `BgmLine(100)`은 `"BGM 100"`이다.
- `SfxLine(0)`은 `"SFX 0"`, `SfxLine(-5)`는 `"SFX -5"`이다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `UnityEngine`을 쓰지 않는다.
- 038이 만든 열거형과 `StartMenuRules`를 고치지 않는다.
- 다른 기능의 코드와 표시 글자(`SaveText`, `OfflineRewardText` 등)를 고치지 않는다.
- 씬·프리팹·에셋, 어셈블리 정의, `Assets/Tests/PlayMode/Capture/`를 고치지 않는다.

## 보고서

### 한 일
- 시작 화면과 설정 창의 표시 상수, 시작 버튼 선택 및 문화권에 독립적인 음량 문자열을 구현했다.
- 완료 조건과 정의되지 않은 열거형 값, 정수 범위 및 문화권을 확인하는 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/StartMenu/StartMenuText.cs`와 `.meta`
- `Assets/Tests/EditMode/StartMenu/StartMenuTextTests.cs`와 `.meta`
- `tasks/doing/041_start_menu_text.md`, `.loop/result.json`

### 가정
없음. 공개 연결 지점은 사양과 같다.

### 사람이 확인할 것
없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 신규 및 기존 테스트의 루프 판정을 기다린다.
- `git diff --check` 통과 (추적되지 않은 신규 파일 제외).


## 루프 기록 (2026-11-17 18:34)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/041_start_menu_text
- 루프 브랜치: loop/20261117-182336
