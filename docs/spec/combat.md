# 전투 사양

- 기능 이름: combat (네임스페이스 `PlumSword.Core.Combat`)
- 작성일: 2026-11-16

## 목표

제자와 몬스터가 외길 위에서 자동으로 싸우는 기능이다. 이 기능은 제자의 전진, 몬스터의 접근, 서로의 공격과 피해, 쓰러짐 판정과 알림, 그리고 필드 화면(제자·몬스터·보스의 대체 표시, 머리 위 체력 바, 카메라, 임시 배경)을 맡는다. 어떤 적을 어디에 놓을지, 성공·실패와 재도전, 금화 보상은 맡지 않는다 (스테이지 기능의 것).

출처: `docs/design/milestone_1.md` "1단계: 전투", "1단계: 화면과 연출"의 "필드와 카메라", "기능 간 연결과 작업 분할 시 확인할 점".

## 동작

- 전투는 한 줄(진행 방향 좌표 하나) 위에서 일어난다. 제자는 좌표가 커지는 쪽으로 전진한다. 적은 항상 제자와 같거나 더 큰 좌표에 있다.
- **시간**: 전투는 0.02초(임시) 길이의 칸 단위로 진행한다. `Tick(deltaSeconds)`는 받은 시간을 모아 두었다가 모은 시간이 `0.02 - 1e-9` 이상인 동안 한 칸씩 진행(`Step`)하고 0.02를 뺀다 (0보다 작아지면 0). 남은 시간은 다음 `Tick`으로 넘긴다. `deltaSeconds`가 0.25(임시)보다 크면 0.25로 잘라낸다.
- **한 칸(`Step`)의 순서**. 멈춤 상태(`Paused`)이거나 제자가 패배한 상태(`IsDiscipleDefeated`)면 아무것도 하지 않는다.
  1. **대기 감소**: 제자와 모든 적의 공격 대기 시간을 0.02 줄인다. 0보다 작아지지 않는다. 이동 중에도 줄어든다.
  2. **이동**:
     - 제자: 가장 가까운 적까지의 거리 `d`(적 좌표 − 제자 좌표의 최솟값)를 본다. 적이 없으면 `2.0 × 0.02`만큼 전진한다. `d ≤ 1.0 + 1e-9`이면 멈춘다. 아니면 `min(2.0 × 0.02, d − 1.0)`만큼 전진한다.
     - 일반 몬스터(추가된 순서대로, 제자가 움직인 뒤의 좌표로 계산): 제자까지의 거리 `d`가 `d ≤ 3.0 + 1e-9`이고 `d > 0.8 + 1e-9`이면 `min(1.0 × 0.02, d − 0.8)`만큼 제자 쪽으로 다가온다. 그 밖에는 움직이지 않는다.
     - 보스는 움직이지 않는다.
  3. **공격** (이동이 끝난 좌표로 판정한다):
     - 제자: 대기 시간이 `1e-9` 이하이고 거리 `1.0 + 1e-9` 이내의 적이 하나라도 있으면 공격한다. 이때 `StatUpgrades.Attack`을 한 번 읽어, 범위 안의 모든 적의 체력을 그만큼 줄인다 (0에서 멈춤). 적마다 `EnemyDamaged`를 추가된 순서대로 보낸다. 그 뒤 대기 시간을 그 순간의 `StatUpgrades.AttackInterval`로 정한다.
     - 적(추가된 순서대로): 대기 시간이 `1e-9` 이하이고 제자까지의 거리가 자기 공격 범위(일반 0.8, 보스 1.0)에 `1e-9`를 더한 값 이하이면 공격한다. 제자의 체력을 자기 공격력만큼 줄이고 (0에서 멈춤) `DiscipleDamaged`를 보낸 뒤, 대기 시간을 자기 공격 간격으로 정한다. **이번 칸에 제자의 공격으로 체력이 0이 된 적도 공격한다.**
  4. **쓰러짐 판정**: 체력이 0인 적을 모두 `Enemies`에서 빼고 `IsDead`를 `true`로 만든다. 제자의 체력이 0이면 `IsDiscipleDefeated`를 `true`로 만든다. 이 값들을 모두 바꾼 뒤에 알림을 보낸다: 쓰러진 적마다 `EnemyDefeated`(추가된 순서대로), 그다음 제자가 패배했으면 `DiscipleDefeated` 한 번.
- **동시에 쓰러짐**: 같은 칸에 제자와 적이 함께 쓰러지면 둘 다 처리된다. `EnemyDefeated`를 받는 시점에 `IsDiscipleDefeated`는 이미 `true`다. 이것으로 스테이지 기능이 동시 처치를 가려낸다. 전투는 성공·실패와 보상을 판정하지 않는다.
- **패배 뒤**: `Reset`이 불릴 때까지 `Step`과 `Tick`은 아무것도 하지 않는다. 남아 있는 적은 그대로 `Enemies`에 있다.
- **적 추가** `AddEnemy(kind, stage, position)`: 능력치를 `EnemyStats`로 계산해 적을 만들고 `Enemies` 끝에 넣은 뒤 `EnemyAdded`를 보낸다. 새 적은 체력이 가득하고 공격 대기 시간이 0(바로 공격할 수 있음)이다. 순번 `Index`는 `Reset` 뒤 0부터, 추가할 때마다 1씩 는다.
- **초기화** `Reset()`: 제자 좌표 0, 제자 체력 = `StatUpgrades.MaxHealth`, 제자 공격 대기 0, `IsDiscipleDefeated = false`, 모든 적 제거(`EnemyDefeated`는 보내지 않는다), 다음 `Index` 0, 모아 둔 시간 0으로 만든 뒤 `ResetCompleted`를 한 번 보낸다. `Paused`는 바꾸지 않는다.
- **멈춤** `SetPaused(paused)`: 멈춘 동안 `Step`과 `Tick`은 아무것도 하지 않고, `Tick`이 받은 시간은 모으지 않고 버린다. 알림은 없다. 처음에는 멈춰 있지 않다. 멈춤을 거는 쪽은 스테이지 기능이다 (페이드 동안).
- **강화 반영** (`docs/spec/stat_upgrade.md`의 약속):
  - 공격력: 피해를 줄 때마다 `StatUpgrades.Attack`을 읽는다.
  - 공격 속도: 공격한 직후 다음 대기를 정할 때만 `StatUpgrades.AttackInterval`을 읽는다. 이미 흐르는 대기의 남은 시간은 바꾸지 않는다.
  - 체력: `StatUpgrades.Upgraded`에서 `StatType.Health`를 받으면 `늘어난 양 = StatUpgrades.MaxHealth − 기억해 둔 직전 최대 체력`을 구해 기억을 새 값으로 바꾼다. 제자가 패배한 상태가 아니면 현재 체력을 늘어난 양만큼 늘린다 (예: `40/100 → 60/120`). 다른 능력치의 `Upgraded`에는 아무것도 하지 않는다.
- 몬스터를 처치해도 제자의 체력은 회복하지 않는다. 체력이 가득 차는 것은 `Reset`뿐이다.
- **알림 안에서 전투를 다시 부를 때**: 알림을 받는 쪽이 그 안에서 `AddEnemy`, `SetPaused`, `Reset`을 불러도 예외 없이 동작해야 한다 (보낼 대상 목록을 미리 따로 떠 둔다).
  - `AddEnemy`: 새 적은 다음 칸부터 움직이고 공격한다.
  - `SetPaused(true)`: 진행 중인 칸은 끝까지 처리하고, 다음 칸부터 멈춘다.
  - `Reset`: 그 칸의 남은 처리와 남은 알림을 모두 그만둔다.
- 화면(`CombatView`)은 매 프레임 `Tick(Time.deltaTime)`을 부르고 전투 상태를 읽어 그린다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| 생성자의 `upgrades`가 `null` | `ArgumentNullException` | |
| `Tick(0)` | 아무 일도 없다 | 변화 없음 |
| `Tick(음수)`, `Tick(NaN)`, `Tick(무한대)` | `ArgumentOutOfRangeException` | 변화 없음 |
| `Tick(0.25보다 큰 값)` | 0.25로 잘라낸다 (예외 없음) | 예: `Tick(1.0)` → 12칸 진행, 0.01초 남김 |
| `Tick(0.02보다 작은 값)` | 모아 둔다 | 예: `Tick(0.01)` 두 번 → 1칸 |
| 멈춘 동안 `Step`, `Tick` | 아무 일도 없다 | 변화 없음. `Tick`의 시간은 버린다 |
| 제자가 패배한 뒤 `Step`, `Tick` | 아무 일도 없다 | 변화 없음, 알림 없음 |
| 적이 하나도 없음 | 제자가 계속 전진한다 | 칸마다 좌표 +0.04 |
| 피해가 남은 체력보다 큼 | 체력 0에서 멈춘다 | 음수가 되지 않는다 |
| 같은 칸에 제자와 적이 함께 체력 0 | 둘 다 쓰러짐 처리 | `EnemyDefeated` 먼저, 그때 `IsDiscipleDefeated`는 `true`. 그 뒤 `DiscipleDefeated` 1번 |
| 체력이 0이 된 제자를 같은 칸에 다른 적이 또 공격 | 체력 0 그대로 | `DiscipleDamaged`는 공격한 적 수만큼 간다. `DiscipleDefeated`는 1번 |
| 범위 밖의 적 | 제자의 공격에 맞지 않는다 | 체력 그대로, `EnemyDamaged` 없음 |
| 패배한 상태에서 체력 강화 | 현재 체력을 늘리지 않는다 | 체력 0 그대로. 직전 최대 체력 기억만 바꾼다 |
| `AddEnemy`의 `stage`가 1보다 작음 | `ArgumentOutOfRangeException` | 변화 없음, 알림 없음 |
| `AddEnemy`의 `kind`가 정의되지 않은 값 (예: `(EnemyKind)99`) | `ArgumentOutOfRangeException` | 변화 없음, 알림 없음 |
| `AddEnemy`의 `position`이 `NaN`, 무한대, 또는 `DisciplePosition`보다 작음 (`DisciplePosition − 1e-9` 미만) | `ArgumentOutOfRangeException` | 변화 없음, 알림 없음 |
| `AddEnemy`의 `position`이 `DisciplePosition`과 같음 | 정상 추가 | |
| 멈춘 동안 또는 패배한 뒤 `AddEnemy` | 정상 추가 | `EnemyAdded` 1번 |
| `SetPaused`를 지금과 같은 값으로 부름 | 아무 일도 없다 | |
| `Reset`을 연달아 부름 | 부를 때마다 같은 결과 | `ResetCompleted`가 부른 횟수만큼 |
| 멈춘 동안 `Reset` | 정상 초기화 | `Paused`는 `true` 그대로 |
| `EnemyDefeated` 안에서 `AddEnemy` | 정상 추가, 다음 칸부터 행동 | |
| 알림 안에서 `Reset` | 그 칸의 남은 처리와 알림을 그만둔다 | 예: 동시 쓰러짐의 `EnemyDefeated` 안에서 `Reset` → `DiscipleDefeated` 0번 |
| `EnemyStats`의 `stage`가 1보다 작거나 `kind`가 정의되지 않음 | `ArgumentOutOfRangeException` | |
| `CombatLayout.EnemyOffsetY(음수)` | `ArgumentOutOfRangeException` | |
| `CombatLayout.HealthRatio`의 `maxHealth`가 0 이하 | 0.0 | |
| `CombatLayout.HealthRatio`의 `health`가 음수 / `maxHealth`보다 큼 | 0.0 / 1.0 으로 잘라낸다 | |

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 체력·공격력의 타입 | `long` | 정수만 쓴다. 반올림 없음 |
| 좌표·거리·시간의 타입 | `double` (유닛, 초) | 반올림하지 않는다. 비교에는 허용 오차를 쓴다. 테스트는 오차 1e-6 안에서 비교한다 |
| 허용 오차 `Battle.Epsilon` | 1e-9 | 거리 `≤ 범위 + 1e-9`, 대기 `≤ 1e-9`이면 준비됨 |
| 한 칸의 길이 `Battle.StepSeconds` | 0.02초 (임시) | |
| 한 번에 받는 시간의 상한 `Battle.MaxTickSeconds` | 0.25초 (임시) | |
| 제자 시작 능력치 | 공격력 10, 최대 체력 100, 공격 간격 1.0초 (임시) | `Battle.DiscipleBaseAttack`, `DiscipleBaseMaxHealth`, `DiscipleBaseAttackInterval`. `GameRoot`가 `StatUpgrades`를 만들 때 넣는다 |
| 제자 이동 속도 `Battle.DiscipleMoveSpeed` | 초당 2.0 유닛 (임시) | 한 칸에 0.04 |
| 제자 공격 범위 `Battle.DiscipleAttackRange` | 앞쪽 1.0 유닛 (임시) | 범위 안 모든 적에게 같은 피해 |
| 제자 피해량 | `StatUpgrades.Attack` | |
| 일반 몬스터 반응 거리 `EnemyStats.NormalReactRange` | 3.0 유닛 (임시) | |
| 일반 몬스터 이동 속도 `EnemyStats.NormalMoveSpeed` | 초당 1.0 유닛 (임시) | 한 칸에 0.02 |
| 일반 몬스터 공격 범위 | 0.8 유닛 (임시) | `EnemyStats.AttackRange(Normal)` |
| 보스 공격 범위 | 1.0 유닛 (임시) | `EnemyStats.AttackRange(Boss)`. 보스는 움직이지 않는다 |
| 일반 몬스터 체력 | `30 + 10 × (stage − 1)` (임시) | 1·2·3스테이지 → 30·40·50 |
| 일반 몬스터 공격력 | `5 + 2 × (stage − 1)` (임시) | 1·2·3스테이지 → 5·7·9 |
| 보스 체력 | `100 + 30 × (stage − 1)` (임시) | 1·2·3스테이지 → 100·130·160 |
| 보스 공격력 | `10 + 4 × (stage − 1)` (임시) | 1·2·3스테이지 → 10·14·18 |
| 일반 몬스터·보스 공격 간격 | 2.0초 (임시) | 스테이지가 올라도 같다 |
| 능력치 계산 | `long`으로 한다 (`stage`를 `long`으로 바꾼 뒤 곱한다) | |
| 겹침 표시 `CombatLayout.EnemyOffsetY(index)` | `((index % 3) − 1) × 0.15` (임시) | 0·1·2·3·4 → −0.15·0·0.15·−0.15·0. 일반 몬스터에만 쓴다. 거리 계산에는 넣지 않는다 |
| 체력 바 비율 `CombatLayout.HealthRatio(health, maxHealth)` | `health / maxHealth` (`double`로 나눔), 0.0 ~ 1.0으로 잘라냄 | 50/100 → 0.5, 0/100 → 0.0 |
| 번쩍임 시간 | 0.1초 (임시) | 화면 코드의 값 |

몬스터를 몇 마리, 어디에 놓는지와 처치 금화는 이 기능의 수치가 아니다 ("범위 밖").

## 연결 지점

### 제공하는 것

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

        // Normal: 30 + 10 * (stage - 1), Boss: 100 + 30 * (stage - 1). long 으로 계산.
        // stage 가 1 보다 작거나 kind 가 정의되지 않은 값이면 ArgumentOutOfRangeException (아래 stage, kind 를 받는 메서드 모두 같다).
        public static long MaxHealth(EnemyKind kind, int stage);

        // Normal: 5 + 2 * (stage - 1), Boss: 10 + 4 * (stage - 1).
        public static long Attack(EnemyKind kind, int stage);

        // Normal, Boss 모두 2.0 (초).
        public static double AttackInterval(EnemyKind kind);

        // Normal: 0.8, Boss: 1.0 (유닛).
        public static double AttackRange(EnemyKind kind);
    }

    // 적 하나. Battle.AddEnemy 만 만든다 (생성자는 internal). 값은 Battle 만 바꾼다.
    public sealed class Enemy
    {
        public EnemyKind Kind { get; }
        public int Index { get; }                 // Reset 뒤 0 부터, AddEnemy 마다 1 씩 는다
        public double Position { get; }           // 진행 방향 좌표
        public long MaxHealth { get; }
        public long Health { get; }               // 0 이상 MaxHealth 이하
        public long Attack { get; }
        public double AttackInterval { get; }
        public double AttackCooldown { get; }     // 다음 공격까지 남은 시간(초). 0 이면 바로 공격할 수 있다
        public bool IsDead { get; }               // 쓰러짐 판정에서 Enemies 에서 빠질 때 true
    }

    public sealed class Battle
    {
        public const double StepSeconds = 0.02;
        public const double MaxTickSeconds = 0.25;
        public const double Epsilon = 1e-9;
        public const long DiscipleBaseAttack = 10;
        public const long DiscipleBaseMaxHealth = 100;
        public const double DiscipleBaseAttackInterval = 1.0;
        public const double DiscipleMoveSpeed = 2.0;
        public const double DiscipleAttackRange = 1.0;

        // upgrades: 제자의 공격력·최대 체력·공격 간격을 읽을 곳. null 이면 ArgumentNullException.
        // upgrades.Upgraded 를 구독한다. 시작 상태: 제자 좌표 0, 체력 = upgrades.MaxHealth,
        // 공격 대기 0, 적 없음, 멈추지 않음, 패배 아님. 알림을 보내지 않는다.
        public Battle(PlumSword.Core.StatUpgrade.StatUpgrades upgrades);

        public double DisciplePosition { get; }
        public long DiscipleHealth { get; }             // 0 이상
        public long DiscipleMaxHealth { get; }          // upgrades.MaxHealth 그대로
        public double DiscipleAttackCooldown { get; }   // 다음 공격까지 남은 시간(초)
        public bool IsDiscipleDefeated { get; }
        public bool Paused { get; }

        // 살아 있는 적. 추가된 순서. 쓰러진 적은 쓰러짐 판정에서 빠진다.
        public System.Collections.Generic.IReadOnlyList<Enemy> Enemies { get; }

        // 시간을 받아 모으고, 0.02 초마다 Step 을 한 번씩 부른다. 남은 시간은 다음으로 넘긴다.
        // 0: 아무 일도 없다. 음수·NaN·무한대: ArgumentOutOfRangeException. 0.25 보다 크면 0.25 로 잘라낸다.
        // 멈췄거나 패배한 상태면 아무것도 하지 않고 시간도 모으지 않는다.
        public void Tick(double deltaSeconds);

        // 정확히 한 칸(0.02 초) 진행한다: 대기 감소 -> 이동 -> 공격 -> 쓰러짐 판정.
        // 멈췄거나 패배한 상태면 아무것도 하지 않는다.
        public void Step();

        // 적을 추가하고 EnemyAdded 를 보낸 뒤 그 적을 돌려준다. 능력치는 EnemyStats 로 계산한다.
        // stage < 1, 정의되지 않은 kind, position 이 NaN·무한대이거나 DisciplePosition - Epsilon 보다 작으면
        // ArgumentOutOfRangeException (아무것도 바꾸지 않는다). 멈췄거나 패배한 상태에서도 추가된다.
        // (Epsilon 허용: 칸마다 0.04 를 더한 좌표의 소수 오차 때문에 "제자와 같은 좌표" 가 거부되지 않게 한다.)
        public Enemy AddEnemy(EnemyKind kind, int stage, double position);

        // 제자를 좌표 0, 체력 가득, 공격 대기 0, 패배 아님으로 되돌리고 모든 적을 없앤 뒤
        // ResetCompleted 를 보낸다. EnemyDefeated 는 보내지 않는다. Paused 는 바꾸지 않는다.
        public void Reset();

        // 멈추거나 푼다. 같은 값이면 아무 일도 없다. 알림은 없다.
        public void SetPaused(bool paused);

        public event System.Action<Enemy> EnemyAdded;      // AddEnemy 가 적을 넣은 뒤
        public event System.Action<Enemy> EnemyDamaged;    // 제자의 공격으로 그 적의 체력이 준 뒤
        public event System.Action DiscipleDamaged;        // 적의 공격 한 번마다, 제자의 체력을 줄인 뒤
        public event System.Action<Enemy> EnemyDefeated;   // 쓰러짐 판정에서. 이때 IsDiscipleDefeated 는 이미 이번 칸의 결과다
        public event System.Action DiscipleDefeated;       // 쓰러짐 판정에서, EnemyDefeated 들 다음에 한 번
        public event System.Action ResetCompleted;         // Reset 이 끝난 뒤
    }

    public static class CombatLayout
    {
        // 겹친 일반 몬스터의 화면상 위아래 어긋남. ((index % 3) - 1) * 0.15.
        // 예: 0 -> -0.15, 1 -> 0, 2 -> 0.15, 3 -> -0.15. 음수: ArgumentOutOfRangeException.
        public static double EnemyOffsetY(int index);

        // 체력 바가 찬 비율. (double)health / maxHealth 를 0.0 ~ 1.0 으로 잘라낸다. maxHealth <= 0 이면 0.0.
        public static double HealthRatio(long health, long maxHealth);
    }
}
```

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래 하나를 더한다
    {
        // Awake 에서 StatUpgrades 를 만든 뒤 new Battle(StatUpgrades) 로 하나 만든다.
        public PlumSword.Core.Combat.Battle Battle { get; }
    }
}

namespace PlumSword.Game.Combat
{
    public sealed class CombatView : MonoBehaviour
    {
        // 제자·배경 오브젝트를 만들고 카메라를 설정한다. battle 의 알림을 구독하고 OnDestroy 에서 끊는다.
        // Update 에서 battle.Tick(Time.deltaTime) 을 부른 뒤 화면을 맞춘다.
        public void Initialize(PlumSword.Core.Combat.Battle battle, UnityEngine.Camera camera);
    }

    // 스테이지 기능이 생기기 전까지만 쓰는 임시 적 배치. 스테이지 기능을 구현할 때 이 클래스와 GameRoot 의 조립 줄을 지운다.
    public sealed class TempEncounter : MonoBehaviour
    {
        // 바로 적을 한 번 배치한다. battle.EnemyDefeated, battle.DiscipleDefeated 를 구독하고 OnDestroy 에서 끊는다.
        public void Initialize(PlumSword.Core.Combat.Battle battle);
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot.Awake`가 `StatUpgrades`를 만든 뒤 `Battle`을 하나 만들어 `GameRoot.Battle`로 들고 있는다. 게임에 `Battle`은 하나뿐이다. `GameRoot`가 `StatUpgrades`를 만드는 줄의 숫자 `10, 100, 1.0`은 `Battle.DiscipleBaseAttack, Battle.DiscipleBaseMaxHealth, Battle.DiscipleBaseAttackInterval`로 바꾼다 (값은 같다).
- `Tick`을 부르는 것은 `CombatView` 하나뿐이다. 다른 기능은 `Tick`과 `Step`을 부르지 않는다.
- 스테이지 기능이 쓰는 방법:
  - 스테이지 시작·재도전: 화면이 가려진 동안 `Reset()` 뒤 `AddEnemy(EnemyKind.Normal, 스테이지 번호, 좌표)`를 10번.
  - 처치 집계와 보상: `EnemyDefeated`를 받아 `Kind`로 일반·보스를 가린다. 보스의 `EnemyDefeated`에서 `IsDiscipleDefeated`가 `true`면 동시 처치(실패, 보스 금화 없음)다.
  - 보스 등장: 열 번째 `EnemyDefeated` 안에서 `AddEnemy(EnemyKind.Boss, 스테이지 번호, DisciplePosition + 거리)`를 부를 수 있다.
  - 실패: `DiscipleDefeated`.
  - 페이드: 시작할 때 `SetPaused(true)`, 끝나면 `SetPaused(false)`.
  - 보스 체력 바: 보스 `Enemy`의 `Health`, `MaxHealth`와 `EnemyDamaged`.
- 2단계 저장 기능이 쓰는 방법: 전투에는 저장할 값이 없다. 불러온 뒤 스테이지 기능이 `Reset()`으로 시작한다.

### 필요한 것

- `PlumSword.Core.StatUpgrade.StatUpgrades` (`docs/spec/stat_upgrade.md`): `Attack`, `MaxHealth`, `AttackInterval`, `Upgraded`.
- `PlumSword.Core.StatUpgrade.StatType.Health` (`docs/spec/stat_upgrade.md`).
- `PlumSword.Game.GameRoot.StatUpgrades` (`docs/spec/stat_upgrade.md`). 강화 기능이 먼저 구현되어 있어야 한다.
- 적을 배치하는 스테이지 기능은 아직 사양이 없다. 그때까지 `TempEncounter`가 임시로 대신한다.

## 화면

화면 위쪽의 자동 전투 영역이다. UI 캔버스가 아니라 월드 공간에 `SpriteRenderer`로 그린다. 좌표 단위는 유닛이다.

- **조립**: `GameRoot.Awake`에서 `Battle`을 만든 다음 `[Game]`의 자식 `[CombatView]`를 만들어 `CombatView`를 붙이고 `Initialize(Battle, 카메라)`를 부른다. 카메라는 `Camera.main`을 쓰고, 없으면 `[Game]`의 자식 `[Camera]`를 만들어 `Camera`를 붙이고 `MainCamera` 태그를 단다. 그 뒤 `[Game]`의 자식 `[TempEncounter]`를 만들어 `TempEncounter`를 붙이고 `Initialize(Battle)`를 부른다.
- **대체 표시 만드는 법**: 스프라이트 리소스는 쓰지 않는다. 1×1 흰색 `Texture2D`로 스프라이트를 하나 만들어(픽셀당 유닛 1) 모든 사각형이 함께 쓰고, 크기는 `localScale`, 색은 `SpriteRenderer.color`로 정한다. 캐릭터의 좌표는 발 아래 가운데가 기준이다 (사각형의 아래 가운데가 그 좌표에 오게 둔다).
- **카메라**: Orthographic, 크기 5.625 (`docs/dev/art.md`), 배경색 RGB (0.55, 0.75, 0.90) (임시), 단색으로 지움. 위치는 매 프레임 `(DisciplePosition + 4.0, 0, −10)` (임시). 제자는 화면 가운데보다 왼쪽에 고정되어 보인다. `Reset`으로 제자가 좌표 0으로 돌아가면 카메라도 바로 따라간다.
- **땅 높이**: y = 0 (임시). 캐릭터는 모두 y = 0에 선다.
- **제자**: 1.0×1.0 사각형, RGB (0.25, 0.50, 0.90), 위치 `(DisciplePosition, 0)`, 그리는 순서(`sortingOrder`) 1 (모두 임시).
- **일반 몬스터**: 1.0×1.0 사각형, RGB (0.80, 0.45, 0.20), 위치 `(Position, CombatLayout.EnemyOffsetY(Index))`, 그리는 순서 0 (모두 임시).
- **보스**: 2.0×3.0 사각형, RGB (0.55, 0.20, 0.60), 위치 `(Position, 0)`, 그리는 순서 0 (모두 임시).
- **머리 위 체력 바**: 제자와 일반 몬스터에만 둔다. 보스에는 두지 않는다 (보스 체력은 스테이지 기능의 위쪽 바가 보여 준다). 캐릭터의 자식으로, 머리 위 0.2 유닛에 가로 1.0 × 세로 0.12의 바탕 사각형(RGB (0.15, 0.15, 0.15))과 그 위의 채움 사각형을 둔다. 채움의 가로 길이는 `1.0 × CombatLayout.HealthRatio(체력, 최대 체력)`이고 왼쪽 끝이 고정된다. 채움 색은 제자 초록 RGB (0.20, 0.80, 0.30), 일반 몬스터 빨강 RGB (0.85, 0.20, 0.20). 그리는 순서는 바탕 5, 채움 6 (모두 임시).
- **피격 번쩍임**: `EnemyDamaged`를 받으면 그 적의 사각형을, `DiscipleDamaged`를 받으면 제자의 사각형을 0.1초(임시) 동안 흰색으로 바꿨다가 원래 색으로 되돌린다. 번쩍이는 중에 또 맞으면 0.1초를 다시 센다.
- **적 오브젝트의 생성과 제거**: `EnemyAdded`에서 만들고, `EnemyDefeated`에서 바로 없앤다 (쓰러지는 연출 없음). `ResetCompleted`에서 남은 적 오브젝트를 모두 없앤다.
- **제자가 패배했을 때**: 제자 사각형은 그대로 둔다 (체력 바가 비어 있다). 다음 `Reset`에서 원래대로 보인다.
- **임시 배경**: 길이 이어지는 느낌을 주는 표시다.
  - 땅: 가로 40 × 세로 6 사각형, RGB (0.35, 0.30, 0.25), 윗변이 y = 0, 가로 가운데는 매 프레임 카메라의 x. 그리는 순서 −10.
  - 나무: 1.0×2.5 사각형 8개, RGB (0.20, 0.45, 0.25), y = 0에 서 있다. 월드 좌표 5 유닛(임시)마다 하나씩 있는 것처럼 보이게, 매 프레임 `i`번째(0~7) 나무를 x = `(floor(카메라 x ÷ 5) − 3 + i) × 5`에 둔다. 그리는 순서 −5.
- **바뀌는 때**: 위치·체력 바·카메라·배경은 매 프레임 `Update`에서 `Tick`을 부른 뒤 맞춘다.
- **누르는 것**: 없다. 전투 화면은 입력을 받지 않는다.
- **임시 적 배치 (`TempEncounter`)**: `Initialize`에서 바로, 그리고 다시 놓을 때마다 `Reset()`을 부른 뒤 `AddEnemy(Normal, 1, 6.0)`, `AddEnemy(Normal, 1, 9.0)`, `AddEnemy(Normal, 1, 9.3)`, `AddEnemy(Boss, 1, 15.0)`을 순서대로 부른다 (임시). `DiscipleDefeated`를 받았거나, `EnemyDefeated`를 받았을 때 `battle.Enemies.Count`가 0이면 1.0초(임시, `Time.deltaTime`으로 센다) 기다린 뒤 다시 놓는다. 금화는 주지 않는다.
- 겹침 어긋남과 체력 바 비율의 판단은 Core의 `CombatLayout`에 있고 에딧 모드 테스트로 판정한다. `CombatView`, `TempEncounter`, `GameRoot`의 조립은 자동 테스트 대상이 아니며 사람이 실행해 확인한다.

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| (없음) | | | 전투는 저장할 값이 없다 |

- 제자의 현재 체력, 좌표, 공격 대기 시간, 적의 상태는 저장하지 않는다. 2단계 기획도 "남은 체력과 처치 진행도는 복원하지 않고, 저장된 스테이지의 시작 지점에서 체력을 전부 회복하고 시작한다"로 정했다.
- 되살리기: 따로 없다. `new Battle(upgrades)`의 시작 상태가 곧 스테이지 시작 상태다. 불러온 뒤에는 스테이지 기능이 `Reset()`과 `AddEnemy`로 시작한다.
- 잘못된 저장 값: 해당 없음 (저장하는 값이 없다).
- 오프라인 보상: 꺼져 있는 동안 전투는 진행되지 않는다. 오프라인 보상 금액 계산에도 전투 상태는 쓰이지 않는다.

## 범위 밖

- 일반 몬스터 10마리의 무리 구성과 좌표, 보스가 등장하는 시점과 거리, 스테이지 번호: 스테이지 기능이 정한다. `AddEnemy`를 쓴다.
- 스테이지 성공·실패 판정, 자동 재도전, 다음 스테이지 진행: 스테이지 기능이 정한다. `EnemyDefeated`, `DiscipleDefeated`, `IsDiscipleDefeated`, `Reset`을 쓴다.
- 처치 금화의 금액과 지급, 동시 처치 때 보상을 주는지: 스테이지 기능이 정한다. 전투는 `Wallet`을 쓰지 않는다.
- 진행 바, 보스 체력 바와 보스 이름, 스테이지 번호 표시, 페이드, 페이드 중 전투 멈춤과 강화 잠금: 스테이지 기능이 정한다. `SetPaused`를 쓴다.
- 강화 단계와 비용, 최종 능력치 계산: 강화 기능(`docs/spec/stat_upgrade.md`)이 정한다.
- 저장·불러오기, 오프라인 보상: 2단계에서 한다.
- 경지 돌파 보너스, 자동 발동 스킬과 내공 강화, 공격 속도 강화 제거: 3단계에서 한다. 그때 이 사양을 고친다.
- 원거리 몬스터, 움직이는 보스, 제한 시간: 없다.
- 실제 스프라이트와 공격·피격·쓰러짐 애니메이션, 피해 숫자, 효과음, 실제 배경 이미지와 시차 스크롤: 하지 않는다. 에셋이 준비되면 4단계에서 다룬다.
- Pixel Perfect Camera 설정(`docs/dev/art.md`): 이번에 하지 않는다. 스프라이트를 넣는 작업에서 한다.
- 체력·공격력이 `long` 범위를 넘칠 만큼 큰 경우: 다루지 않는다.

## 완료 조건 초안

아래에서 따로 적지 않으면 지갑은 `new Wallet(1000)`, 강화는 `new StatUpgrades(wallet, 10, 100, 1.0)`, 전투는 `new Battle(upgrades)`로 만든 것이다. "n칸 뒤"는 `Step()`을 n번 부른 뒤다. 좌표·시간은 오차 1e-6 안에서 비교한다.

몬스터 능력치 (`EnemyStatsTests`)

1. `EnemyStats.MaxHealth(Normal, stage)`는 1·2·3스테이지에서 30·40·50, `EnemyStats.Attack(Normal, stage)`는 5·7·9다.
2. `EnemyStats.MaxHealth(Boss, stage)`는 1·2·3스테이지에서 100·130·160, `EnemyStats.Attack(Boss, stage)`는 10·14·18이다.
3. `EnemyStats.AttackInterval`은 `Normal`·`Boss` 모두 2.0, `EnemyStats.AttackRange`는 `Normal` 0.8, `Boss` 1.0이다.
4. `EnemyStats.MaxHealth(Normal, 0)`, `EnemyStats.Attack(Boss, 0)`, `EnemyStats.MaxHealth((EnemyKind)99, 1)`, `EnemyStats.AttackRange((EnemyKind)99)`는 각각 `ArgumentOutOfRangeException`을 던진다.

전투 (`BattleTests`)

5. 새로 만들면 `DisciplePosition`은 0, `DiscipleHealth`는 100, `DiscipleMaxHealth`는 100, `DiscipleAttackCooldown`은 0, `IsDiscipleDefeated`는 `false`, `Paused`는 `false`, `Enemies.Count`는 0이다.
6. `new Battle(null)`은 `ArgumentNullException`을 던진다.
7. 적이 없을 때 50칸 뒤 `DisciplePosition`은 2.0이다.
8. `AddEnemy(Normal, 2, 5.0)`이 돌려준 적은 `Kind`가 `Normal`, `Index` 0, `Position` 5.0, `MaxHealth`와 `Health` 40, `Attack` 7, `AttackInterval` 2.0, `AttackCooldown` 0, `IsDead` `false`이고, `EnemyAdded`가 그 적을 담아 1번 오며 `Enemies.Count`는 1이다. 이어서 `AddEnemy(Boss, 1, 8.0)`의 `Index`는 1이다.
9. `AddEnemy(Normal, 0, 5.0)`, `AddEnemy((EnemyKind)99, 1, 5.0)`, `AddEnemy(Normal, 1, double.NaN)`, `AddEnemy(Normal, 1, double.PositiveInfinity)`, `AddEnemy(Normal, 1, -0.1)`은 각각 `ArgumentOutOfRangeException`을 던지고 `Enemies.Count`는 0, `EnemyAdded`는 0번 온다. `AddEnemy(Normal, 1, 0.0)`은 예외 없이 추가된다.
10. `AddEnemy(Normal, 1, 10.0)` 뒤 1칸 뒤에 그 적의 `Position`은 10.0이고 `DisciplePosition`은 0.04다 (반응 거리 밖의 몬스터는 기다린다).
11. `AddEnemy(Normal, 1, 5.0)` 뒤: 49칸 뒤 적의 `Position`은 5.0, 50칸 뒤 4.98이다 (제자가 3 유닛 안으로 오면 다가온다).
12. 11번에 이어: 82칸 뒤 적의 `Health`는 30, 83칸 뒤 20이고 `DisciplePosition`은 3.32, 적의 `Position`은 4.32다 (제자는 거리 1.0에서 멈춰 바로 공격한다). 100칸 뒤에도 `DisciplePosition`은 3.32다.
13. 12번에 이어: 92칸 뒤 `DiscipleHealth`는 100, 93칸 뒤 95이고 적의 `Position`은 4.12다 (몬스터는 거리 0.8에서 멈춰 바로 공격한다).
14. 13번에 이어: 133칸 뒤 적의 `Health`는 10, 182칸 뒤 10, 183칸 뒤 0이고 `EnemyDefeated`가 그 적을 담아 1번 오며, 적의 `IsDead`는 `true`, `Enemies.Count`는 0, `DiscipleHealth`는 95다 (처치해도 체력은 회복하지 않는다). 184칸 뒤 `DisciplePosition`은 3.36이다.
15. `AddEnemy(Normal, 1, 0.8)`을 3번 부른 뒤 1칸 뒤: 세 적의 `Health`는 모두 20, `DiscipleHealth`는 85, `EnemyDamaged`는 3번(`Index` 0, 1, 2 순서), `DiscipleDamaged`는 3번 온다.
16. 15번에 이어: 51칸 뒤 세 적의 `Health`는 모두 10이다. 101칸 뒤 `Enemies.Count`는 0, `EnemyDefeated`는 3번(`Index` 0, 1, 2 순서) 오고, `DiscipleHealth`는 70, `IsDiscipleDefeated`는 `false`다 (같은 칸에 쓰러진 몬스터도 그 칸의 공격을 한다).
17. `AddEnemy(Normal, 1, 0.8)`과 `AddEnemy(Boss, 1, 1.5)` 뒤 1칸 뒤: 일반 몬스터의 `Health`는 20, 보스의 `Health`는 100이고 `EnemyDamaged`는 1번 온다 (범위 밖의 적은 맞지 않는다).
18. `AddEnemy(Boss, 1, 1.0)` 뒤: 1칸 뒤 보스의 `Health`는 90, `DiscipleHealth`는 90이다. 450칸 뒤 보스의 `Health`는 10, 451칸 뒤 0이고 `EnemyDefeated`가 1번 오며 `DiscipleHealth`는 50이다.
19. `AddEnemy(Boss, 1, 5.0)` 뒤: 99칸 뒤 보스의 `Health`는 100, 100칸 뒤 90이고 `DiscipleHealth`는 90, `DisciplePosition`은 4.0, 보스의 `Position`은 5.0이다 (보스는 움직이지 않는다).
20. `new StatUpgrades(wallet, 30, 100, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)`과 `AddEnemy(Boss, 1, 2.0)` 뒤: 1칸 뒤 `Enemies.Count`는 1, `DiscipleHealth`는 95다. 26칸 뒤 `DisciplePosition`은 1.0, 보스의 `Health`는 100, `DiscipleAttackCooldown`은 0.5, `DiscipleHealth`는 85다. 50칸 뒤 보스의 `Health`는 100, 51칸 뒤 70이다 (이동 중에도 대기가 흐르고, 남은 시간만큼 기다린다).
21. `new StatUpgrades(wallet, 30, 100, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)`과 `AddEnemy(Boss, 1, 4.0)` 뒤: 75칸 뒤 보스의 `Health`는 100, 76칸 뒤 70이다 (도착했을 때 대기가 끝나 있으면 바로 공격한다).
22. `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고 `upgrades.TryUpgrade(StatType.Attack)`을 부르면, 51칸 뒤 보스의 `Health`는 78이다 (90 − 12).
23. `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고 `upgrades.TryUpgrade(StatType.AttackSpeed)`를 부르면, 50칸 뒤 보스의 `Health`는 90, 51칸 뒤 80, 98칸 뒤 80, 99칸 뒤 70이다 (기다리는 중인 공격은 그대로이고 다음 대기부터 0.95초다).
24. `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고(`DiscipleHealth` 90) `upgrades.TryUpgrade(StatType.Health)`를 부르면 `DiscipleHealth`는 110, `DiscipleMaxHealth`는 120이다. `TryUpgrade(StatType.Attack)`을 불러도 `DiscipleHealth`는 110 그대로다.
25. `new StatUpgrades(wallet, 30, 5, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)` 뒤 1칸 뒤: `EnemyDefeated` 1번과 `DiscipleDefeated` 1번이 이 순서로 오고, `EnemyDefeated`를 받는 시점에 읽은 `IsDiscipleDefeated`는 `true`다. `DiscipleHealth`는 0, `Enemies.Count`는 0이다.
26. `new StatUpgrades(wallet, 100, 10, 1.0)`으로, `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 뒤: 보스의 `EnemyDefeated`를 받는 시점에 읽은 `IsDiscipleDefeated`는 `true`이고 `DiscipleDefeated`가 1번 온다.
27. `new StatUpgrades(wallet, 10, 5, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)` 뒤 1칸 뒤: `DiscipleHealth`는 0, `IsDiscipleDefeated`는 `true`, `DiscipleDefeated`는 1번, 적의 `Health`는 20이고 `Enemies.Count`는 1이다. 이어서 `Step()` 100번과 `Tick(0.1)`을 불러도 적의 `Health`는 20, `DisciplePosition`은 0이고 알림은 더 오지 않는다.
28. `new StatUpgrades(wallet, 10, 12, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)`을 3번 부른 뒤 1칸 뒤: `DiscipleHealth`는 0 (음수가 아니다), `DiscipleDamaged`는 3번, `DiscipleDefeated`는 1번 온다.
29. 27번 상태(패배)에서 `upgrades.TryUpgrade(StatType.Health)`를 부르면 `DiscipleHealth`는 0 그대로다. 이어서 `Reset()`을 부르면 `DiscipleHealth`는 25다.
30. 18번처럼 1칸 진행한 뒤 `Reset()`을 부르면 `DisciplePosition`은 0, `DiscipleHealth`는 100, `DiscipleAttackCooldown`은 0, `IsDiscipleDefeated`는 `false`, `Enemies.Count`는 0이고, `ResetCompleted`는 1번, `EnemyDefeated`는 0번 온다. 이어서 `AddEnemy(Normal, 1, 5.0)`의 `Index`는 0이다. `Reset()`을 한 번 더 부르면 `ResetCompleted`가 한 번 더 온다.
31. `SetPaused(true)` 뒤 `Paused`는 `true`이고, `Step()` 10번과 `Tick(0.1)`을 불러도 `DisciplePosition`은 0이다. `SetPaused(false)` 뒤 1칸 뒤 `DisciplePosition`은 0.04다 (멈춘 동안의 `Tick` 시간은 버린다).
32. `SetPaused(true)` 상태에서 `AddEnemy(Normal, 1, 5.0)`은 추가되고 `EnemyAdded`가 1번 온다. `Reset()`을 부르면 `Enemies.Count`는 0이고 `Paused`는 `true` 그대로다. `SetPaused(true)`를 한 번 더 불러도 예외가 없다.
33. 적이 없을 때 `Tick(0.1)` 뒤 `DisciplePosition`은 0.2다 (5칸).
34. 적이 없을 때 `Tick(0.01)` 1번 뒤 `DisciplePosition`은 0, 2번 뒤 0.04다.
35. 적이 없을 때 `Tick(0.05)` 뒤 `DisciplePosition`은 0.08이고, 이어서 `Tick(0.01)` 뒤 0.12다 (남은 시간을 넘긴다).
36. 적이 없을 때 `Tick(1.0)` 뒤 `DisciplePosition`은 0.48이다 (0.25초로 잘라내 12칸).
37. `Tick(0)` 뒤 `DisciplePosition`은 0이다. `Tick(-0.1)`, `Tick(double.NaN)`, `Tick(double.PositiveInfinity)`는 각각 `ArgumentOutOfRangeException`을 던지고 `DisciplePosition`은 0 그대로다.
38. `new StatUpgrades(wallet, 30, 100, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)` 뒤 `EnemyDefeated` 안에서 `AddEnemy(Boss, 1, DisciplePosition + 5.0)`을 부르게 하면, 1칸 뒤 예외 없이 `Enemies.Count`는 1이고 그 적의 `Kind`는 `Boss`, `Health`는 100이다.
39. `new StatUpgrades(wallet, 30, 5, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)` 뒤 `EnemyDefeated` 안에서 `Reset()`을 부르게 하면, 1칸 뒤 `DiscipleDefeated`는 0번 오고 `IsDiscipleDefeated`는 `false`, `DiscipleHealth`는 5다.

표시 규칙 (`CombatLayoutTests`)

40. `CombatLayout.EnemyOffsetY`는 0 → −0.15, 1 → 0, 2 → 0.15, 3 → −0.15, 4 → 0을 돌려주고, −1은 `ArgumentOutOfRangeException`을 던진다.
41. `CombatLayout.HealthRatio`는 (50, 100) → 0.5, (0, 100) → 0.0, (100, 100) → 1.0, (150, 100) → 1.0, (−5, 100) → 0.0, (10, 0) → 0.0, (30, 40) → 0.75를 돌려준다.

화면 (자동 테스트 없음. 사람이 실행해 확인)

42. 게임을 실행하면 하늘색 바탕과 땅, 나무 사각형이 보이고, 파란 제자 사각형이 오른쪽으로 전진하며 나무가 왼쪽으로 흘러간다. 제자는 화면에서 같은 자리에 보인다.
43. 주황색 몬스터를 만나면 제자가 멈춰 싸우고, 맞은 쪽이 잠깐 하얗게 번쩍이며 머리 위 체력 바가 줄어든다. 두 마리가 붙으면 위아래로 어긋나 보인다. 쓰러진 몬스터는 사라진다.
44. 보라색 보스까지 처치하거나 제자의 체력이 0이 되면 약 1초 뒤 처음 배치로 돌아가 다시 싸운다.
45. 기존 테스트 `CoreAssemblyTests.Core_DoesNotReferenceUnity`가 계속 통과한다.

## 결정 기록

| 질문 | 답 | 방식 |
| --- | --- | --- |
| 전투 사양에 스테이지(배치, 보스 등장, 성공·실패, 재도전, 페이드)를 함께 담나 | 전투만 담고 스테이지는 따로 기획한다 | 추천 수락 |
| 처치 금화의 계산과 지급은 어느 기능이 맡나 | 스테이지 기능. 전투는 처치만 알린다 | 추천 수락 |
| 전투 화면에 무엇까지 넣나 | 캐릭터 대체 표시, 머리 위 체력 바, 카메라, 임시 배경 | 추천 수락 |
| 전투 시간을 어떻게 흐르게 하나 | 0.02초 고정 간격. 실시간 자동 전투이고 턴제가 아니다. 같은 칸에서 일어난 일이 "동시"다 | 추천 수락 (턴제가 아니라는 설명 뒤) / 임시값 |
| 같은 순간에 제자의 공격으로 체력이 0이 된 몬스터도 공격하나 | 한다. 모두 공격한 뒤 쓰러짐을 판정한다 | 추천 수락 |
| 스테이지 기능이 생기기 전에 적은 누가 놓나 | `TempEncounter`가 임시로 놓고 반복한다. 스테이지 구현 때 지운다 | 추천 수락 |
| 공격이 일어난 것을 화면에서 어떻게 알리나 | 맞은 쪽이 0.1초 하얗게 번쩍인다 | 추천 수락 / 임시값 |
| 몬스터 능력치 공식은 어느 기능이 가지나 | 전투 (`EnemyStats`). 스테이지는 종류·스테이지 번호·좌표만 넘긴다 | 직접 정함 |
| 한 칸의 처리 순서와 알림 순서 | 대기 감소 → 이동 → 공격 → 쓰러짐 판정. `EnemyDefeated` 다음에 `DiscipleDefeated` | 직접 정함 |
| 이동이 사거리를 넘는 경우 | 넘지 않게 딱 맞춰 멈춘다 (제자 1.0, 몬스터 0.8) | 직접 정함 |
| 적이 하나도 없을 때 제자 | 계속 전진한다 | 직접 정함 |
| 소수 계산 오차 | 거리·대기 비교에 1e-9 허용 | 직접 정함 |
| 한 번에 너무 긴 시간이 들어올 때 | 0.25초로 잘라낸다 | 직접 정함 / 임시값 |
| 잘못된 인자(없는 종류, 스테이지 0, 제자 뒤 좌표, 음수 시간) | `ArgumentOutOfRangeException` | 직접 정함 |
| 패배한 뒤와 페이드 중의 전투 | 패배 뒤에는 `Reset`까지 진행하지 않는다. 멈춤 스위치 `SetPaused`를 둔다 | 직접 정함 |
| 패배한 상태에서 체력 강화 | 현재 체력은 0 그대로 | 직접 정함 |
| 저장할 값 | 없다 (2단계 기획이 체력·진행도를 복원하지 않기로 함) | 직접 정함 |
| 대체 표시의 크기 | 제자·일반 몬스터 1.0×1.0, 보스 2.0×3.0. 몸 크기(32px)에 맞춤 | 직접 정함 / 임시값 |
| 보스 머리 위 체력 바 | 두지 않는다 (스테이지의 위쪽 바가 맡는다) | 직접 정함 |
| 겹침 표시 규칙, 체력 바, 카메라 위치, 임시 배경, 임시 배치 | `Index % 3`으로 −0.15 / 0 / +0.15, 카메라는 제자 앞 4 유닛, 땅과 나무 사각형, 일반 3마리 + 보스 | 직접 정함 / 임시값 |
| 타입·메서드 이름 | `Battle`, `Enemy`, `EnemyKind`, `EnemyStats`, `CombatLayout`, `CombatView`, `TempEncounter` | 직접 정함 |
