# 094_play_time_ending_text — 총 플레이 시간 PlayTime 과 엔딩 글자 EndingText

## 목표
Core(`PlumSword.Core.Stage`)에 총 플레이 시간을 세는 `PlayTime`과 엔딩 화면 글자를 만드는 `EndingText`를 만든다. 시간을 더하는 쪽(`GameRoot`)과 엔딩 화면은 096 이 한다.

## 묶음
regression

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("순수 C# 규칙")
- docs/spec/regression.md ("경계 상황", "수치", "연결 지점"의 `PlayTime`·`EndingText`)

## 요구사항
- `Assets/Scripts/Core/Stage/PlayTime.cs`: `public sealed class PlayTime`.
  - `public PlayTime(double initialSeconds = 0.0)`: 음수·NaN·무한대면 0으로 시작한다 (예외 없음).
  - `public double Seconds { get; }`.
  - `public void Add(double deltaSeconds)`: 음수·NaN·무한대면 `ArgumentOutOfRangeException`이고 시간은 그대로. 0은 허용한다.
  - `public long Milliseconds { get; }` = 내림(`Seconds` × 1000).
  - 시간은 인자로만 받는다 (`DateTime.Now`, `UnityEngine.Time`을 쓰지 않는다).
- `Assets/Scripts/Core/Stage/EndingText.cs`: `public static class EndingText`.
  - `public const string Title = "천마 토벌";`, `public const string ReturnButton = "처음으로";`
  - `public static string PlayTime(double seconds)` = `"플레이 시간 " + 분 + ":" + 초`. 분 = 내림(seconds / 60)을 두 자리 이상(0 → `00`, 7 → `07`, 62 → `62`), 초 = 내림(seconds) % 60을 두 자리(`00`~`59`). 음수·NaN·무한대면 `ArgumentOutOfRangeException`. 숫자는 문화권과 무관하게(`CultureInfo.InvariantCulture`) 만든다.
  - `public static string Regressions(int count)` = `"회귀 " + count + "회"`.
- 구현하다 모양이 사양과 달라지면 `docs/spec/regression.md`의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/Stage/PlayTimeTests.cs`와 `Assets/Tests/EditMode/Stage/EndingTextTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 18. `new PlayTime()`의 `Seconds`는 0이다. `Add(1.5)`를 두 번 부르면 3.0이고 `Milliseconds`는 3000이다.
- 19. `new PlayTime(12.3456)`의 `Milliseconds`는 12345다.
- 20. `Add(-1)`, `Add(double.NaN)`, `Add(double.PositiveInfinity)`는 각각 `ArgumentOutOfRangeException`을 던지고 `Seconds`는 그대로다.
- 21. `new PlayTime(-5)`와 `new PlayTime(double.NaN)`의 `Seconds`는 0이다.
- 22. `EndingText.PlayTime(754.9)`는 `"플레이 시간 12:34"`, `PlayTime(0)`은 `"플레이 시간 00:00"`, `PlayTime(3725)`는 `"플레이 시간 62:05"`다.
- 23. `EndingText.PlayTime(-1)`은 `ArgumentOutOfRangeException`을 던진다.
- 24. `EndingText.Regressions(3)`은 `"회귀 3회"`, `Regressions(0)`은 `"회귀 0회"`다. `Title`은 `"천마 토벌"`, `ReturnButton`은 `"처음으로"`다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `StageProgress`, 저장(`PlumSword.Core.Save`), `GameRoot`, 화면을 고치지 않는다 (093·095·096).
- 플레이 시간을 저장하거나 화면에 보여 주지 않는다 (095·096).
- `UnityEngine`을 쓰지 않는다. 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 총 플레이 시간 누적과 밀리초 변환, 엔딩 글자 형식을 순수 C#으로 구현했다.
- 완료 조건과 유효하지 않은 입력, 문화권 독립성을 검사하는 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/Stage/PlayTime.cs`, `EndingText.cs` 및 각 `.meta`
- `Assets/Tests/EditMode/Stage/PlayTimeTests.cs`, `EndingTextTests.cs` 및 각 `.meta`
- `tasks/doing/094_play_time_ending_text.md`, `.loop/result.json`

### 가정
- 사양에서 정의하지 않은 극단적인 누적 시간의 부동소수점 및 long 범위 초과는 별도 상한을 두지 않고 기본 숫자 연산에 따른다.
- 공개 연결 지점과 기존 사양은 변경하지 않았다.

### 사람이 확인할 것
- 없음

### 테스트 결과
- Unity 테스트는 직접 실행하지 않았으며, 신규 및 기존 테스트의 실행과 결과 판정은 루프를 기다린다.


## 루프 기록 (2026-11-22 16:56)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/094_play_time_ending_text
- 루프 브랜치: loop/20261122-164616
