# 021_stage_theme_text — 회차·테마 글자와 테마별 보스 이름

## 목표
`PlumSword.Core` 의 `StageText` 에 회차·스테이지·테마를 한 줄로 쓰는 `RoundStageLabel`, 테마 이름 `ThemeName`, 테마별 보스 이름 `ThemedBossName` 을 더한다. 기존 `StageLabel`, `BossName` 은 그대로 둔다. 에딧 모드 테스트로 검사한다.

## 묶음
테마회차

## 선행 작업
- 013_stage_rewards_text

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stage.md ("수치" 절의 스테이지 글자·테마 이름·보스 이름, "경계 상황" 표의 `StageText` 줄, "연결 지점" 절의 `StageText`, "변경 기록"의 "2026-11-17 — 다섯 테마와 회차 순환")

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-17 — 다섯 테마와 회차 순환` 항목 중 `StageText` 부분만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. `StageProgress` 의 회차·화면상 번호는 020 이, 화면은 022 가 한다. 소개 문구 `IntroLine` 은 이 작업에서 만들지 않는다 (023 이 한다).
- 고칠 파일: `Assets/Scripts/Core/Stage/StageText.cs` (네임스페이스 `PlumSword.Core.Stage`, `public static class StageText`).
- 테스트: 기존 `Assets/Tests/EditMode/Stage/StageTextTests.cs` 의 `StageTextTests` 클래스에 새 테스트 메서드로 더한다. 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다.
- 이미 있는 것: `public static string StageLabel(int stage)`(모든 `int` 를 예외 없이 `"STAGE " + 번호` 로 표시), `public const string BossName = "GATEKEEPER"`. 바꾸지 않는다.
- **더할 것** (사양 "연결 지점" 그대로):

  ```csharp
  // round < 1 또는 stage 가 1~5 밖이면 ArgumentOutOfRangeException.
  // 예: (2, 1) -> "ROUND 2 · STAGE 1 · JONGNAM". 숫자는 InvariantCulture.
  public static string RoundStageLabel(int round, int stage);
  // stage 가 1~5 밖이면 ArgumentOutOfRangeException.
  public static string ThemeName(int stage);
  public static string ThemedBossName(int stage);
  ```

  - `stage` 는 화면상 스테이지 번호(1~5)다. 전체 진행 번호가 아니다.
  - `RoundStageLabel` 의 형식은 `ROUND {round} · STAGE {stage} · {ThemeName(stage)}` 다. 가운뎃점은 `·`(U+00B7)이고 양옆에 빈칸 하나씩이다. 숫자는 `CultureInfo.InvariantCulture` 로 쓴다 (자릿수 구분 기호 없음).
  - 화면상 번호별 이름 (모두 임시 영문. 한글 글꼴이 준비되면 바꾼다):

    | 화면상 번호 | `ThemeName` | `ThemedBossName` |
    | --- | --- | --- |
    | 1 | `JONGNAM` | `JONGNAM MASTER` |
    | 2 | `BEGGAR CLAN` | `BEGGAR CLAN MASTER` |
    | 3 | `NAMGUNG` | `NAMGUNG SWORDSMAN` |
    | 4 | `DEMON CULT` | `DEMON CULT COMMANDER` |
    | 5 | `HEAVENLY DEMON` | `HEAVENLY DEMON` |

  - 세 메서드 모두 `stage` 가 1~5 밖이면, `RoundStageLabel` 은 `round` 가 1보다 작아도 `System.ArgumentOutOfRangeException` 이다.
  - 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/stage.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- **기존 테스트**: 아래 테스트는 이름·본문·기대값을 그대로 두고 통과해야 한다. 기존 테스트를 지우거나 이름을 바꾸거나 검사를 줄이지 않는다.
  - `StageTextTests.StageLabel_Stage_PrefixesStageAndSpace`: 그대로 유지.
  - `StageTextTests.StageLabel_Negative_WritesAsIsWithoutException`: 그대로 유지.
  - `StageTextTests.StageLabel_CurrentCultureIsGerman_HasNoDigitSeparator`: 그대로 유지.
  - `StageTextTests.BossName_Always_IsGatekeeper`: 그대로 유지. 게임 화면은 022 에서 새 메서드를 쓰게 된다.

## 완료 조건
- `Assets/Tests/EditMode/Stage/StageTextTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `StageText.RoundStageLabel(2, 1)` 은 `"ROUND 2 · STAGE 1 · JONGNAM"` 이다.
- `StageText.RoundStageLabel(1, 5)` 는 `"ROUND 1 · STAGE 5 · HEAVENLY DEMON"` 이다.
- `CultureInfo.CurrentCulture` 를 `de-DE` 로 바꾼 동안 `StageText.RoundStageLabel(1234, 3)` 은 `"ROUND 1234 · STAGE 3 · NAMGUNG"` 이다 (테스트 끝에 문화권을 되돌린다).
- `StageText.ThemeName(1)`~`ThemeName(5)` 는 순서대로 `"JONGNAM"`, `"BEGGAR CLAN"`, `"NAMGUNG"`, `"DEMON CULT"`, `"HEAVENLY DEMON"` 이다.
- `StageText.ThemedBossName(1)`~`ThemedBossName(5)` 는 순서대로 `"JONGNAM MASTER"`, `"BEGGAR CLAN MASTER"`, `"NAMGUNG SWORDSMAN"`, `"DEMON CULT COMMANDER"`, `"HEAVENLY DEMON"` 이다.
- `ThemeName(0)`, `ThemeName(6)` 은 `ArgumentOutOfRangeException` 이다.
- `ThemedBossName(0)`, `ThemedBossName(6)` 은 `ArgumentOutOfRangeException` 이다.
- `RoundStageLabel(1, 0)`, `RoundStageLabel(1, 6)`, `RoundStageLabel(0, 1)`, `RoundStageLabel(-1, 1)` 은 `ArgumentOutOfRangeException` 이다.
- 기존 테스트가 모두 통과한다. (위 "기존 테스트"의 네 테스트, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `StageLabel(int)`, `BossName` 을 바꾸거나 지우지 않는다.
- `StageText` 에 `IntroLine` 을 만들지 않는다 (023 이 한다).
- `Assets/Scripts/Core/Stage/StageProgress.cs` 를 고치지 않는다 (020 이 한다).
- `Assets/Scripts/Game/` 를 고치지 않는다 (화면은 022 가 한다).
- 한글 글자와 한글 글꼴을 쓰지 않는다.
- Core 에서 `UnityEngine` 을 쓰지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 고치지 않는다.

## 보고서

### 한 일
- 회차·스테이지·테마 표기와 테마 및 보스 이름 조회를 추가하고 입력 범위를 검사했다.
- 이름, 형식, 문화권, 경계값에 대한 에딧 모드 테스트를 추가했다. 기존 API와 테스트는 유지했다.

### 바뀐 파일
- `Assets/Scripts/Core/Stage/StageText.cs`
- `Assets/Tests/EditMode/Stage/StageTextTests.cs`
- `tasks/doing/021_stage_theme_text.md` (보고서)
- `.loop/result.json` (결과)

### 가정
- 없음. 공개 연결 지점과 값은 기존 사양과 같다.

### 사람이 확인할 것
- Unity 라이선스 클라이언트 연결 문제를 해결한 환경에서 전체 테스트를 다시 실행해 통과를 확인한다.

### 테스트 결과
- `git diff --check`: 통과.
- `python tools/loop/run_unity_tests.py`: 직접 실행했으나 Unity 라이선스 클라이언트 연결 거부 및 재연결 반복으로 테스트가 시작되지 않아 중단했다. 신규 및 기존 테스트의 통과 여부는 확인하지 못했다.


## 루프 기록 (2026-11-17 11:51)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/021_stage_theme_text
- 루프 브랜치: loop/20261117-113918
