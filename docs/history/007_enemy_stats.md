# 007_enemy_stats — 몬스터 능력치와 전투 표시 규칙

## 목표
`PlumSword.Core` 에 몬스터 종류 `EnemyKind`, 몬스터 능력치 계산 `EnemyStats`, 전투 화면의 표시 규칙 `CombatLayout`(겹침 어긋남, 체력 바 비율)을 만들고 에딧 모드 테스트로 검사한다. 셋 다 상태가 없는 순수 계산이다.

## 묶음
전투

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/combat.md ("수치", "연결 지점" 절의 `EnemyKind`, `EnemyStats`, `CombatLayout`, "경계 상황" 표의 마지막 네 줄)

## 요구사항
- 만들 파일 (파일 하나에 공개 타입 하나, 네임스페이스 `PlumSword.Core.Combat`):
  - `Assets/Scripts/Core/Combat/EnemyKind.cs`
  - `Assets/Scripts/Core/Combat/EnemyStats.cs`
  - `Assets/Scripts/Core/Combat/CombatLayout.cs`
- 테스트 파일 (네임스페이스 `PlumSword.Core.Tests.Combat`, 테스트 이름은 `<상황>_<기대 결과>` 모양):
  - `Assets/Tests/EditMode/Combat/EnemyStatsTests.cs` (클래스 `EnemyStatsTests`)
  - `Assets/Tests/EditMode/Combat/CombatLayoutTests.cs` (클래스 `CombatLayoutTests`)
- 공개 모양은 사양의 "연결 지점" 절 그대로다. 뒤 작업(008_battle_movement, 009_battle_attack, 011_combat_view)과 스테이지 기능이 이 이름으로 부른다.

  ```csharp
  namespace PlumSword.Core.Combat
  {
      public enum EnemyKind
      {
          Normal = 0,
          Boss = 1,
      }

      public static class EnemyStats
      {
          public const double NormalReactRange = 3.0;
          public const double NormalMoveSpeed = 1.0;

          public static long MaxHealth(EnemyKind kind, int stage);
          public static long Attack(EnemyKind kind, int stage);
          public static double AttackInterval(EnemyKind kind);
          public static double AttackRange(EnemyKind kind);
      }

      public static class CombatLayout
      {
          public static double EnemyOffsetY(int index);
          public static double HealthRatio(long health, long maxHealth);
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/combat.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 사양에 없는 공개 멤버를 더하지 않는다.
- **`EnemyStats`** (수치는 모두 임시값):
  - `MaxHealth(kind, stage)`: `Normal` 은 `30 + 10 × (stage − 1)`, `Boss` 는 `100 + 30 × (stage − 1)`.
  - `Attack(kind, stage)`: `Normal` 은 `5 + 2 × (stage − 1)`, `Boss` 는 `10 + 4 × (stage − 1)`.
  - 계산은 `long` 으로 한다 (`stage` 를 `long` 으로 바꾼 뒤 곱한다. `int` 넘침이 없어야 한다).
  - `AttackInterval(kind)`: `Normal`, `Boss` 모두 2.0 (초). 스테이지가 올라도 같다.
  - `AttackRange(kind)`: `Normal` 0.8, `Boss` 1.0 (유닛).
  - `NormalReactRange` 는 일반 몬스터가 제자에게 다가오기 시작하는 거리(3.0 유닛), `NormalMoveSpeed` 는 일반 몬스터의 이동 속도(초당 1.0 유닛)다. 보스는 움직이지 않으므로 보스용 값은 없다.
  - `stage` 가 1보다 작으면 `System.ArgumentOutOfRangeException` (`MaxHealth`, `Attack`).
  - `kind` 가 정의되지 않은 값(예: `(EnemyKind)99`)이면 `System.ArgumentOutOfRangeException` (네 메서드 모두).
- **`CombatLayout`**:
  - `EnemyOffsetY(index)`: 겹친 일반 몬스터를 화면에서 위아래로 어긋나게 그릴 양. `((index % 3) − 1) × 0.15` (임시값). 0·1·2·3·4 → −0.15·0·0.15·−0.15·0. `index` 가 음수면 `System.ArgumentOutOfRangeException`.
  - `HealthRatio(health, maxHealth)`: 체력 바가 찬 비율. `(double)health / maxHealth` 를 0.0 ~ 1.0 으로 잘라낸다. `maxHealth` 가 0 이하면 0.0 (예외 없음). `health` 가 음수면 0.0, `maxHealth` 보다 크면 1.0.
- 시간·난수·저장을 쓰지 않는다.

## 완료 조건
`double` 값은 오차 1e-9 안에서 비교한다.

- `Assets/Tests/EditMode/Combat/EnemyStatsTests.cs` 와 `Assets/Tests/EditMode/Combat/CombatLayoutTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `EnemyStats.MaxHealth(Normal, stage)` 는 1·2·3스테이지에서 30·40·50, `EnemyStats.Attack(Normal, stage)` 는 5·7·9다.
- `EnemyStats.MaxHealth(Boss, stage)` 는 1·2·3스테이지에서 100·130·160, `EnemyStats.Attack(Boss, stage)` 는 10·14·18이다.
- `EnemyStats.MaxHealth(Normal, int.MaxValue)` 는 21474836490, `EnemyStats.Attack(Boss, int.MaxValue)` 는 8589934594다 (`long` 으로 계산해 넘치지 않는다).
- `EnemyStats.AttackInterval` 은 `Normal`·`Boss` 모두 2.0, `EnemyStats.AttackRange` 는 `Normal` 0.8, `Boss` 1.0이다.
- `EnemyStats.NormalReactRange` 는 3.0, `EnemyStats.NormalMoveSpeed` 는 1.0이다.
- `EnemyStats.MaxHealth(Normal, 0)`, `EnemyStats.Attack(Boss, 0)`, `EnemyStats.MaxHealth(Normal, -1)`, `EnemyStats.MaxHealth((EnemyKind)99, 1)`, `EnemyStats.Attack((EnemyKind)99, 1)`, `EnemyStats.AttackInterval((EnemyKind)99)`, `EnemyStats.AttackRange((EnemyKind)99)` 는 각각 `ArgumentOutOfRangeException` 을 던진다.
- `CombatLayout.EnemyOffsetY` 는 0 → −0.15, 1 → 0, 2 → 0.15, 3 → −0.15, 4 → 0을 돌려주고, −1은 `ArgumentOutOfRangeException` 을 던진다.
- `CombatLayout.HealthRatio` 는 (50, 100) → 0.5, (0, 100) → 0.0, (100, 100) → 1.0, (150, 100) → 1.0, (−5, 100) → 0.0, (10, 0) → 0.0, (10, −5) → 0.0, (30, 40) → 0.75를 돌려준다.
- 기존 테스트가 모두 통과한다. (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다. `PlumSword.Core` 는 순수 C# 이다.
- `Enemy`, `Battle` 을 만들지 않는다. 008_battle_movement 가 한다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. 011_combat_view 가 한다.
- 다른 기능의 폴더(`Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/`)를 고치지 않는다.
- 몬스터를 몇 마리, 어디에 놓는지와 처치 금화를 만들지 않는다. 스테이지 기능의 일이다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.


## 보류 사유 (2026-11-16 16:41)

- 사유: 재시도 한도(5회) 초과. 마지막 실패:
claude 실행 실패 (코드 1): You've hit your 5-hour limit · resets 8:30pm (Asia/Seoul)
- 질문: 실패 원인을 확인하고 작업 문서나 완료 조건을 고쳐 주세요.
- 추천안: 마지막 실패 내용과 기능 브랜치의 작업 중 상태를 보고 판단
- 기능 브랜치: task/007_enemy_stats
- 작업 에이전트: claude(시도 1~6)


## 답변 (2026-11-16 20:32)

- 작업 문서나 완료 조건의 문제가 아니다. 클로드 사용량 한도("5-hour limit") 문구를 루프가 한도로 인식하지 못해 재시도만 소진한 것이다. 루프 설정(`usage_limit_patterns`)을 고쳤다.
- 작업 문서는 그대로다. 기능 브랜치에 남은 변경이 있으면 확인하고 이어서 완성한다.

## 보고서

### 한 일
- 기능 브랜치에 이전 시도의 남은 변경이 없음을 확인하고 처음부터 구현했다.
- `PlumSword.Core.Combat` 에 `EnemyKind`(Normal=0, Boss=1), `EnemyStats`(체력·공격력·공격 간격·공격 범위, 반응 거리·이동 속도 상수), `CombatLayout`(`EnemyOffsetY`, `HealthRatio`)을 사양 "연결 지점" 그대로 만들었다. 사양에 없는 공개 멤버는 없다.
- 능력치 계산은 `(long)stage - 1` 을 먼저 만든 뒤 곱해 `int` 넘침이 없다. `stage < 1`, 정의되지 않은 `kind` 는 `ArgumentOutOfRangeException`.
- `HealthRatio` 는 `maxHealth <= 0` 또는 `health <= 0` 이면 0.0, `health >= maxHealth` 면 1.0, 그 외 `(double)health / maxHealth`.
- 완료 조건마다 에딧 모드 테스트를 작성했다 (`EnemyStatsTests`, `CombatLayoutTests`).

### 바뀐 파일
- `Assets/Scripts/Core/Combat.meta` (새로 만듦, 유니티 자동 생성)
- `Assets/Scripts/Core/Combat/EnemyKind.cs` (+ `.meta`)
- `Assets/Scripts/Core/Combat/EnemyStats.cs` (+ `.meta`)
- `Assets/Scripts/Core/Combat/CombatLayout.cs` (+ `.meta`)
- `Assets/Tests/EditMode/Combat.meta` (새로 만듦, 유니티 자동 생성)
- `Assets/Tests/EditMode/Combat/EnemyStatsTests.cs` (+ `.meta`)
- `Assets/Tests/EditMode/Combat/CombatLayoutTests.cs` (+ `.meta`)
- `tasks/doing/007_enemy_stats.md` (이 보고서)

### 가정
- 수치는 모두 작업 문서·사양의 임시값 그대로다. 새로 정한 수치는 없다.
- `MaxHealth`/`Attack` 에 정의되지 않은 `kind` 와 1보다 작은 `stage` 가 함께 들어오면 `kind` 쪽 예외(매개변수 이름 `kind`)를 먼저 던진다. 둘 다 `ArgumentOutOfRangeException` 이라 사양과 어긋나지 않는다.
- 예외 메시지는 기존 코드처럼 한국어 문장으로 적었다.
- 사양 문서(`docs/spec/combat.md`)와 색인은 이미 이 모양을 담고 있어 고치지 않았다.

### 사람이 확인할 것
- 없음 (완료 조건에 [사람 확인] 항목이 없고, 모든 조건을 테스트로 판정했다).

### 테스트 결과
- `python tools/loop/run_unity_tests.py` 직접 실행: EditMode 127개 중 127개 통과, 0개 실패 (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함).


## 루프 기록 (2026-11-16 20:37)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/007_enemy_stats
- 루프 브랜치: loop/20261116-164139
