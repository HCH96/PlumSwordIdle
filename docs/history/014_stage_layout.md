# 014_stage_layout — 몬스터 무리 구성과 좌표 계산

## 목표
`PlumSword.Core` 에 한 스테이지의 일반 몬스터 10마리를 무리로 나누고 좌표로 바꾸는 정적 클래스 `StageLayout` 을 만든다. 뒤 작업의 테스트가 함께 쓸 테스트용 난수 클래스 두 개도 만든다. 에딧 모드 테스트로 검사한다.

## 묶음
스테이지

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("순수 C# 규칙이 지켜지는 방식" 절의 난수 주입)
- docs/spec/stage.md ("수치" 절, "경계 상황" 표, "연결 지점" 절의 `StageLayout`)

## 요구사항
- 만들 파일: `Assets/Scripts/Core/Stage/StageLayout.cs` (네임스페이스 `PlumSword.Core.Stage`). `Assets/Scripts/Core/Stage/` 폴더가 없으면 만든다.
- 테스트 파일 (네임스페이스 `PlumSword.Core.Tests.Stage`, 폴더 `Assets/Tests/EditMode/Stage/`. 없으면 만든다):
  - `StageLayoutTests.cs` — 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다.
  - `MinRandom.cs` — `internal sealed class MinRandom : System.Random`. `Next(int minValue, int maxValue)` 를 재정의해 항상 `minValue` 를 돌려준다.
  - `MaxRandom.cs` — `internal sealed class MaxRandom : System.Random`. `Next(int minValue, int maxValue)` 를 재정의해 항상 `maxValue − 1` 을 돌려주고, 받은 `maxValue` 를 순서대로 기록해 `IReadOnlyList<int> MaxValues { get; }` 로 내준다.
  - 뒤 작업(015_stage_progress_begin 부터)의 테스트가 `MinRandom`, `MaxRandom` 을 이 이름 그대로 쓴다.
- `.meta` 파일은 유니티가 화면 없이 실행될 때(테스트 실행) 자동으로 만든다. 만들어진 `.meta` 는 지우지 않는다.
- 공개 모양은 사양의 "연결 지점" 절 그대로다.

  ```csharp
  namespace PlumSword.Core.Stage
  {
      public static class StageLayout
      {
          public const int NormalCount = 10;
          public const int MaxGroupSize = 3;
          public const double FirstGroupPosition = 6.0;
          public const double GroupSpacing = 3.0;
          public const double InGroupSpacing = 0.3;
          public const double BossDistance = 18.0;

          public static int[] RollGroups(System.Random random);
          public static double[] Positions(System.Collections.Generic.IReadOnlyList<int> groups);
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/stage.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 상수 값은 모두 임시값이다 (`NormalCount` 10 은 확정).
- **`RollGroups(random)`**: 무리별 마리 수를 정한다. 남은 수를 10(`NormalCount`)에서 시작해, 0이 될 때까지 `random.Next(1, Math.Min(3, 남은 수) + 1)` 을 골라 결과에 더하고 남은 수에서 뺀다. 난수는 반드시 이 두 인자짜리 `Next(int, int)` 로만, 무리 하나에 한 번씩만 부른다 (테스트용 난수가 이것을 재정의한다). 합은 정확히 10이고 무리 수는 4~10개다. 예: `2·1·3·1·3`, `1·2·2·3·2`.
  - `random` 이 `null` 이면 `System.ArgumentNullException`.
- **`Positions(groups)`**: 무리 구성을 좌표로 바꾼다. `g` 번째(0부터) 무리의 중심 = `6.0 + 3.0 × g`. `n` 마리 무리의 `i` 번째(0부터) 좌표 = `중심 + (i − (n − 1) / 2.0) × 0.3`. 결과는 무리 순서대로, 무리 안에서는 작은 좌표부터다. 길이는 원소의 합이다.
  - `groups` 가 `null` 이면 `System.ArgumentNullException`.
  - 원소가 1보다 작거나 3보다 크면 `System.ArgumentOutOfRangeException`.
  - 빈 목록이면 빈 배열을 돌려준다.
  - 합이 10인지는 검사하지 않는다.
- 좌표는 `double` 이고 반올림하지 않는다. 테스트는 오차 1e-6 안에서 비교한다.
- `BossDistance` 는 이 작업에서 상수로만 둔다 (보스를 내는 것은 016_stage_progress_kills 가 한다).
- 시간과 저장은 쓰지 않는다. 난수는 인자로 받은 것만 쓴다 (`new System.Random()` 을 안에서 만들지 않는다).

## 완료 조건
- `Assets/Tests/EditMode/Stage/StageLayoutTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `StageLayout.NormalCount` 는 10, `MaxGroupSize` 는 3, `FirstGroupPosition` 은 6.0, `GroupSpacing` 은 3.0, `InGroupSpacing` 은 0.3, `BossDistance` 는 18.0 이다.
- `StageLayout.RollGroups(new MaxRandom())` 은 `3, 3, 3, 1` 이고, 그 난수의 `MaxValues` 는 순서대로 4, 4, 4, 2 다.
- `StageLayout.RollGroups(new MinRandom())` 은 `1` 이 10개다.
- 시드 0부터 999까지의 `new System.Random(시드)` 마다 `StageLayout.RollGroups` 의 결과는 합이 10이고, 모든 원소가 1 이상 3 이하이며, 길이가 4 이상 10 이하다.
- `StageLayout.Positions(new[] { 2, 1, 3, 1, 3 })` 은 5.85, 6.15, 9.0, 11.7, 12.0, 12.3, 15.0, 17.7, 18.0, 18.3 이다.
- `StageLayout.Positions(new[] { 3, 3, 3, 1 })` 은 5.7, 6.0, 6.3, 8.7, 9.0, 9.3, 11.7, 12.0, 12.3, 15.0 이다. `Positions(1 이 10개)` 는 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 24.0, 27.0, 30.0, 33.0 이다.
- `StageLayout.Positions(빈 배열)` 은 길이 0인 배열이다. `Positions(null)` 과 `RollGroups(null)` 은 `ArgumentNullException` 을, `Positions(new[] { 0 })` 과 `Positions(new[] { 4 })` 는 `ArgumentOutOfRangeException` 을 던진다.
- 기존 테스트가 모두 통과한다. (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다 (`UnityEngine.Random` 포함). `PlumSword.Core` 는 순수 C# 이다.
- `StagePhase`, `StageProgress` 를 만들지 않는다. 015_stage_progress_begin 이 한다. `StageRewards`, `StageText` 는 013_stage_rewards_text 의 것이니 만들거나 고치지 않는다.
- `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/`, `Assets/Scripts/Core/Combat/` 를 고치지 않는다. 이 작업은 `Battle.AddEnemy` 를 부르지 않는다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `PlumSword.Core.Stage.StageLayout` 정적 클래스를 사양 "연결 지점" 모양 그대로 만들었다. 상수 6개, `RollGroups(random)`(무리당 `random.Next(1, Math.Min(3, 남은 수) + 1)` 한 번), `Positions(groups)`(중심 `6.0 + 3.0 × g`, 무리 안 `(i − (n − 1) / 2.0) × 0.3`).
- 테스트용 난수 `MinRandom`(항상 `minValue`), `MaxRandom`(항상 `maxValue − 1`, 받은 `maxValue` 를 `MaxValues` 에 기록)을 `PlumSword.Core.Tests.Stage` 에 `internal sealed` 로 만들었다.
- `StageLayoutTests` 에 완료 조건마다 테스트를 작성했다 (상수, 최대/최소 난수, 시드 0~999, 좌표 예 3종, 빈 배열, null, 범위 밖 0·4).

### 바뀐 파일
- `Assets/Scripts/Core/Stage/StageLayout.cs` (+ `.meta`, 유니티 자동 생성)
- `Assets/Tests/EditMode/Stage/StageLayoutTests.cs` (+ `.meta`)
- `Assets/Tests/EditMode/Stage/MinRandom.cs` (+ `.meta`)
- `Assets/Tests/EditMode/Stage/MaxRandom.cs` (+ `.meta`)
- `tasks/doing/014_stage_layout.md` (이 보고서)

### 가정
- `Positions` 는 원소 검사를 먼저 모두 끝낸 뒤 좌표를 계산한다 (범위 밖 원소가 있으면 결과를 만들지 않고 바로 예외).
- `ArgumentOutOfRangeException` 의 `paramName` 은 `groups`, 실제 값은 잘못된 원소 값으로 넣었다.
- 연결 지점 모양은 사양과 같아 사양 문서는 고치지 않았다. 상수 값(`NormalCount` 제외)은 작업 문서대로 임시값이다.

### 사람이 확인할 것
- 없음 (완료 조건에 [사람 확인] 항목 없음)

### 테스트 결과
- `python tools/loop/run_unity_tests.py --project .` — EditMode 220개 중 220개 통과, 0개 실패 (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함).


## 루프 기록 (2026-11-16 21:01)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/014_stage_layout
- 루프 브랜치: loop/20261116-205758
