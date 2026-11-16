# 013_stage_rewards_text — 처치 금화 계산과 스테이지 표시 글자

## 목표
`PlumSword.Core` 에 스테이지 기능의 계산용 정적 클래스 두 개를 만든다. 처치 금화를 계산하는 `StageRewards` 와 화면에 보여 줄 글자를 정하는 `StageText` 다. 에딧 모드 테스트로 검사한다.

## 묶음
스테이지

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stage.md ("수치" 절, "경계 상황" 표, "연결 지점" 절의 `StageRewards`, `StageText`)

## 요구사항
- 만들 파일 (네임스페이스 `PlumSword.Core.Stage`, 파일 하나에 공개 타입 하나):
  - `Assets/Scripts/Core/Stage/StageRewards.cs`
  - `Assets/Scripts/Core/Stage/StageText.cs`
- 테스트 파일 (네임스페이스 `PlumSword.Core.Tests.Stage`): `Assets/Tests/EditMode/Stage/StageRewardsTests.cs`, `Assets/Tests/EditMode/Stage/StageTextTests.cs`. 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다.
- `Assets/Scripts/Core/Stage/`, `Assets/Tests/EditMode/Stage/` 폴더를 새로 만든다. `.meta` 파일은 유니티가 화면 없이 실행될 때(테스트 실행) 자동으로 만든다. 만들어진 `.meta` 는 지우지 않는다.
- 공개 모양은 사양의 "연결 지점" 절 그대로다.

  ```csharp
  namespace PlumSword.Core.Stage
  {
      public static class StageRewards
      {
          public static long NormalGold(int stage);
          public static long BossGold(int stage);
      }

      public static class StageText
      {
          public const string BossName = "GATEKEEPER";
          public static string StageLabel(int stage);
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/stage.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- **`StageRewards.NormalGold(stage)`**: 일반 몬스터 한 마리의 금화. `10 + 2 × (stage − 1)` (임시값). `stage` 를 `long` 으로 바꾼 뒤 곱한다 (`int` 로 계산하면 넘친다). 1·2·3스테이지 → 10·12·14.
- **`StageRewards.BossGold(stage)`**: 보스의 금화. `50 + 10 × (stage − 1)` (임시값). `long` 으로 계산한다. 1·2·3스테이지 → 50·60·70.
- 두 메서드 모두 `stage` 가 1보다 작으면 `System.ArgumentOutOfRangeException`.
- **`StageText.StageLabel(stage)`**: `"STAGE " + stage.ToString(CultureInfo.InvariantCulture)`. 자릿수 구분 기호를 넣지 않는다. 0 이하도 그대로 표기하고 예외를 던지지 않는다 (예: `STAGE 0`, `STAGE -3`). 1단계에는 한글 글꼴이 없어 한글을 쓰지 않는다.
- **`StageText.BossName`**: `"GATEKEEPER"` (임시값). 모든 스테이지에서 같다.
- 시간, 난수, 저장은 쓰지 않는다. 상태가 없는 계산뿐이다.
- 문화권 테스트는 `CultureInfo.CurrentCulture` 를 `de-DE` 로 바꾸고, 끝나면 `finally` 에서 원래 값으로 되돌린다.

## 완료 조건
- `Assets/Tests/EditMode/Stage/StageRewardsTests.cs` 와 `Assets/Tests/EditMode/Stage/StageTextTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `StageRewards.NormalGold` 는 1·2·3스테이지에서 10·12·14 를, `StageRewards.BossGold` 는 50·60·70 을 돌려준다.
- `StageRewards.NormalGold(int.MaxValue)` 는 4294967302, `StageRewards.BossGold(int.MaxValue)` 는 21474836510 이다.
- `StageRewards.NormalGold(0)`, `StageRewards.NormalGold(-1)`, `StageRewards.BossGold(0)` 은 각각 `ArgumentOutOfRangeException` 을 던진다.
- `StageText.StageLabel` 은 1 → `"STAGE 1"`, 12 → `"STAGE 12"`, 0 → `"STAGE 0"` 을 돌려주고 예외가 없다.
- 현재 문화권을 `de-DE` 로 바꾼 상태에서 `StageText.StageLabel(1234)` 는 `"STAGE 1234"` 다.
- `StageText.BossName` 은 `"GATEKEEPER"` 다.
- 기존 테스트가 모두 통과한다. (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다. `PlumSword.Core` 는 순수 C# 이다.
- `StageLayout`, `StagePhase`, `StageProgress` 를 만들지 않는다. 014_stage_layout, 015_stage_progress_begin 이 한다.
- `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/`, `Assets/Scripts/Core/Combat/` 를 고치지 않는다. 몬스터 능력치(`EnemyStats`)는 전투 기능의 것이다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다.
- 한글 표기(`스테이지 1`, `수문장`), 테마별 보스 이름을 만들지 않는다. 사양의 "범위 밖"이다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `PlumSword.Core.Stage.StageRewards` 를 만들었다. `NormalGold(stage)` = `10 + 2 × (stage − 1)`, `BossGold(stage)` = `50 + 10 × (stage − 1)` 이고, `stage` 를 `long` 으로 바꾼 뒤 계산한다. `stage < 1` 이면 `ArgumentOutOfRangeException` 을 던진다.
- `PlumSword.Core.Stage.StageText` 를 만들었다. `BossName = "GATEKEEPER"` 이고, `StageLabel(stage)` = `"STAGE " + stage.ToString(CultureInfo.InvariantCulture)` 이다. 0 이하도 예외 없이 그대로 쓴다.
- 완료 조건마다 에딧 모드 테스트를 썼다 (`StageRewardsTests`, `StageTextTests`).
- 공개 모양이 사양의 "연결 지점" 절과 같아서 사양 문서와 색인은 고치지 않았다 (`docs/spec/index.md` 에는 이미 두 타입이 적혀 있다).

### 바뀐 파일
- `Assets/Scripts/Core/Stage/StageRewards.cs` (새 파일)
- `Assets/Scripts/Core/Stage/StageText.cs` (새 파일)
- `Assets/Tests/EditMode/Stage/StageRewardsTests.cs` (새 파일)
- `Assets/Tests/EditMode/Stage/StageTextTests.cs` (새 파일)
- `.meta` 파일: `Assets/Scripts/Core/Stage.meta`, `Assets/Tests/EditMode/Stage.meta`, 그리고 위 네 파일의 `.meta` (테스트를 실행할 때 유니티가 만들었다)

### 가정
- 수치(10/2, 50/10, `GATEKEEPER`)는 사양과 작업 문서에 적힌 임시값을 그대로 썼다.
- 예외 메시지는 영어 `"stage must be 1 or greater."` 로 정했고, 매개변수 이름과 실제 값을 함께 담는다.
- 완료 조건에는 없지만 `StageLabel(-3)` → `"STAGE -3"` 테스트를 하나 더 넣었다 (요구사항에 적힌 예).

### 사람이 확인할 것
- 없음 (완료 조건에 [사람 확인] 항목이 없고, 모든 조건을 테스트로 판정했다).

### 테스트 결과
- `python tools/loop/run_unity_tests.py`: EditMode 208개 중 208개 통과, 0개 실패 (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함). 종료 코드 0.


## 루프 기록 (2026-11-16 20:59)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/013_stage_rewards_text
- 루프 브랜치: loop/20261116-205758
