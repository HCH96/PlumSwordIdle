# 038_start_menu_rules — 시작 화면 판단 규칙

## 목표
Core(`PlumSword.Core.StartMenu`)에 시작 화면 기능의 열거형 네 개와 판단 규칙 `StartMenuRules`를 만든다.

## 묶음
시작화면

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/start_menu.md ("동작", "연결 지점" 절)
- docs/spec/save.md ("연결 지점" 절)

## 요구사항
- 폴더 `Assets/Scripts/Core/StartMenu/`, 네임스페이스 `PlumSword.Core.StartMenu`. 파일 하나에 공개 타입 하나.
- 열거형을 사양 "연결 지점"의 값 그대로 만든다:
  - `StartEntry { Launch = 0, ResumeInGame = 1, ResumeOnTitle = 2, TitleReturn = 3 }`
  - `StartButtonKind { NewGame = 0, Continue = 1 }`
  - `SettingsMode { Title = 0, InGame = 1 }`
  - `BackKeyAction { None = 0, OpenSettings = 1, CloseSettings = 2 }`
- `public static class StartMenuRules`의 메서드 여섯 개. 모두 예외를 던지지 않는다.
  - `bool ShowsStartScreen(StartEntry entry, bool isBatchMode)`: `Launch`이면 `!isBatchMode`. `ResumeInGame`이면 `false`. `ResumeOnTitle`, `TitleReturn`이면 `true`. 정의되지 않은 값은 `true`.
  - `bool CalculatesOfflineReward(StartEntry entry)`: `TitleReturn`만 `false`, 나머지(정의되지 않은 값 포함)는 `true`.
  - `StartButtonKind StartButton(PlumSword.Core.Save.SaveLoadStatus status)`: `Loaded`이면 `Continue`, 나머지(`Empty`, `UnsupportedVersion`, `Corrupt`, 정의되지 않은 값)는 `NewGame`.
  - `bool AutoSaveStartsEnabled(PlumSword.Core.Save.SaveLoadStatus status, bool hasOfflineReward)`: `status == Loaded && !hasOfflineReward`일 때만 `true`.
  - `StartEntry ResumeEntry(bool isStartScreenShowing)`: `true`면 `ResumeOnTitle`, `false`면 `ResumeInGame`.
  - `BackKeyAction BackKey(bool isSettingsOpen, bool isStartScreenShowing, bool hasBegun)`: `isSettingsOpen`이면 `CloseSettings`. 아니고 `!isStartScreenShowing && hasBegun`이면 `OpenSettings`. 나머지는 `None`.
- 이미 있는 `PlumSword.Core.Save.SaveLoadStatus`를 쓴다. 고치지 않는다.
- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/start_menu.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 테스트 네임스페이스는 `PlumSword.Core.Tests.StartMenu`.

## 완료 조건
- `Assets/Tests/EditMode/StartMenu/StartMenuRulesTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `StartButton(Loaded)`는 `Continue`이다.
- `StartButton(Empty)`, `StartButton(UnsupportedVersion)`, `StartButton(Corrupt)`, `StartButton((SaveLoadStatus)99)`는 모두 `NewGame`이다.
- `ShowsStartScreen(Launch, false)`는 `true`, `ShowsStartScreen(Launch, true)`는 `false`이다.
- `ShowsStartScreen(ResumeInGame, false)`와 `ShowsStartScreen(ResumeInGame, true)`는 모두 `false`이다.
- `ShowsStartScreen(ResumeOnTitle, false)`, `(ResumeOnTitle, true)`, `(TitleReturn, false)`, `(TitleReturn, true)`, `((StartEntry)99, false)`는 모두 `true`이다.
- `CalculatesOfflineReward`는 `Launch`, `ResumeInGame`, `ResumeOnTitle`, `(StartEntry)99`에서 `true`이다.
- `CalculatesOfflineReward(TitleReturn)`은 `false`이다.
- `AutoSaveStartsEnabled(Loaded, false)`는 `true`이다.
- `AutoSaveStartsEnabled`의 `(Loaded, true)`, `(Empty, false)`, `(Empty, true)`, `(UnsupportedVersion, false)`, `(Corrupt, false)`, `((SaveLoadStatus)99, false)`는 모두 `false`이다.
- `ResumeEntry(true)`는 `ResumeOnTitle`, `ResumeEntry(false)`는 `ResumeInGame`이다.
- `BackKey(true, x, y)`는 `x`, `y`의 네 조합 모두 `CloseSettings`이다.
- `BackKey(false, false, true)`는 `OpenSettings`이다.
- `BackKey(false, false, false)`, `BackKey(false, true, false)`, `BackKey(false, true, true)`는 모두 `None`이다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `UnityEngine`을 쓰지 않는다.
- `Assets/Scripts/Core/Save/` 등 다른 기능의 코드를 고치지 않는다.
- `VolumeSettings`, `LeaveGame`, `StartMenuText`와 화면은 만들지 않는다 (039, 040, 041, 042 이후가 한다).
- 씬·프리팹·에셋, 어셈블리 정의, `Assets/Tests/PlayMode/Capture/`를 고치지 않는다.

## 보고서

### 한 일
- 시작 화면 열거형 네 개와 여섯 가지 판단 규칙을 순수 C#으로 구현했다.
- 지정된 입력 조합과 열거형 값을 확인하는 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/StartMenu/`의 열거형 네 파일, `StartMenuRules.cs` 및 각 `.meta`, 폴더 `.meta`.
- `Assets/Tests/EditMode/StartMenu/StartMenuRulesTests.cs` 및 파일과 폴더의 `.meta`.
- `tasks/doing/038_start_menu_rules.md` 보고서, `.loop/result.json`.

### 가정
없음. 공개 연결 지점은 사양과 같다.

### 사람이 확인할 것
없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 신규 및 기존 테스트 결과는 완료 보고 후 루프의 판정을 기다린다.
- 새 `.meta` GUID가 기존 Assets 내에서 중복되지 않음을 확인했다.


## 루프 기록 (2026-11-17 18:26)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/038_start_menu_rules
- 루프 브랜치: loop/20261117-182336
