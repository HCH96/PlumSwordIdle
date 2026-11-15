# 스테이지 사양

- 기능 이름: stage (네임스페이스 `PlumSword.Core.Stage`)
- 작성일: 2026-11-16

## 목표

스테이지는 일반 몬스터 10마리와 보스 하나로 된 한 판이다. 이 기능은 스테이지 번호, 몬스터 10마리의 무리 구성과 좌표, 처치 수와 진행도, 보스 등장, 성공·실패 판정과 자동 재도전, 처치 금화의 계산과 지급, 스테이지 전환(페이드, 전투 멈춤, 강화 잠금), 그리고 스테이지 화면(스테이지 글자, 진행 바와 보스 체력 바, 페이드 막)을 맡는다. 이동·공격·체력과 몬스터 능력치, 캐릭터 표시, 카메라, 배경은 맡지 않는다 (전투 기능의 것).

출처: `docs/design/milestone_1.md` "1단계: 스테이지와 필드", "1단계: 금화", "공격 타이밍과 동시 처치", "UI와 전환", "기능 간 연결과 작업 분할 시 확인할 점". 저장 대비는 `docs/design/milestone_2.md` "저장과 불러오기", "오프라인 보상".

## 동작

- **상태**: 스테이지 번호(`StageNumber`, 처음 1), 단계(`Phase`: `Playing` / `FadingOut` / `FadingIn`), 일반 몬스터 처치 수(`DefeatedNormalCount`, 0~10), 이번 스테이지에서 낸 보스(`Boss`, 없으면 `null`).
- **시작** `Begin()`: `GameRoot`가 화면을 모두 조립한 뒤 한 번 부른다. "스테이지 배치"(아래)를 하고 `HasBegun`을 `true`로 만든 뒤 `StageStarted(StageNumber)`, `Changed`를 순서대로 한 번씩 보낸다. 페이드는 없다. 전투 멈춤과 강화 잠금은 건드리지 않는다. 두 번째부터는 아무 일도 없다.
- **스테이지 배치**: 아래를 순서대로 한다.
  1. `battle.Reset()`.
  2. `StageLayout.RollGroups(random)`으로 무리 구성을 새로 정한다.
  3. `StageLayout.Positions(무리 구성)`의 좌표마다 순서대로 `battle.AddEnemy(EnemyKind.Normal, StageNumber, 좌표)`를 부른다 (모두 10번).
  4. `DefeatedNormalCount = 0`, `Boss = null`.
- **적을 가리는 법**: 스테이지는 쓰러진 적을 `Enemy.Kind`로만 가린다. 그 적을 누가 추가했는지는 가리지 않는다.
- **`HasBegun`이 `false`이거나 `Phase`가 `Playing`이 아닌 동안**에는 전투의 알림(`EnemyDefeated`, `DiscipleDefeated`, `EnemyDamaged`)을 모두 무시한다.
- **일반 몬스터가 쓰러짐** (`battle.EnemyDefeated`, `Kind == Normal`): 아래를 순서대로 한다.
  1. `wallet.Add(StageRewards.NormalGold(StageNumber))`. 제자가 같은 칸에 쓰러졌어도(`battle.IsDiscipleDefeated`가 `true`) 준다.
  2. `DefeatedNormalCount`가 10보다 작으면 1 올린다. 이미 10이면 그대로 둔다.
  3. 2번에서 9가 10이 되었고 `battle.IsDiscipleDefeated`가 `false`면 보스를 낸다: `Boss = battle.AddEnemy(EnemyKind.Boss, StageNumber, battle.DisciplePosition + StageLayout.BossDistance)`.
  4. `Changed`를 한 번 보낸다.
- **보스가 쓰러짐** (`battle.EnemyDefeated`, `Kind == Boss`):
  - `battle.IsDiscipleDefeated`가 `true`면(동시에 쓰러짐) 아무것도 하지 않는다. 금화도 알림도 없다. 실패는 곧 오는 `DiscipleDefeated`에서 처리된다.
  - 아니면 성공이다: `wallet.Add(StageRewards.BossGold(StageNumber))` → "전환 시작"(다음 번호 = `StageNumber + 1`, 단 `StageNumber`가 `int.MaxValue`면 그대로) → `StageCleared(StageNumber)` → `Changed`.
- **제자가 쓰러짐** (`battle.DiscipleDefeated`): 실패다. "전환 시작"(다음 번호 = `StageNumber`) → `StageFailed(StageNumber)` → `Changed`.
- **전환 시작**: 다음 번호를 기억해 두고, `Phase = FadingOut`, 페이드 시간 0, `battle.SetPaused(true)`, `upgrades.SetLocked(true)`. `StageCleared`·`StageFailed`를 받는 시점에 `Phase`는 이미 `FadingOut`이고 `StageNumber`는 아직 끝난 스테이지의 번호다.
- **시간** `Tick(deltaSeconds)`: `HasBegun`이 `false`이거나 `Phase`가 `Playing`이면 아무것도 하지 않는다. 아니면 페이드 시간에 `deltaSeconds`를 더하고 아래를 순서대로 본다.
  1. `Phase`가 `FadingOut`이고 페이드 시간이 `0.5 − 1e-9` 이상이면 **가려진 순간의 교체**를 한다: `StageNumber = 기억해 둔 다음 번호`, "스테이지 배치", 페이드 시간에서 0.5를 뺀다 (0보다 작아지면 0), `Phase = FadingIn`, `StageStarted(StageNumber)`, `Changed`.
  2. `Phase`가 `FadingIn`이고 페이드 시간이 `0.5 − 1e-9` 이상이면 **전환을 끝낸다**: `Phase = Playing`, 페이드 시간 0, `battle.SetPaused(false)`, `upgrades.SetLocked(false)`, `Changed`.
  - 한 번의 `Tick`에서 1번과 2번이 모두 일어날 수 있다 (예: `Tick(1.0)`).
- **페이드 막의 진하기** `FadeAlpha`: `Playing`이면 0. `FadingOut`이면 `Math.Min(1.0, 페이드 시간 / 0.5)`. `FadingIn`이면 `Math.Max(0.0, 1.0 − 페이드 시간 / 0.5)`.
- **진행 바 비율** `BarRatio`: `Boss`가 `null`이면 `DefeatedNormalCount / 10.0`. `null`이 아니면 `CombatLayout.HealthRatio(Boss.Health, Boss.MaxHealth)`. 보스가 쓰러진 뒤에도 가려진 순간의 교체 전까지는 `Boss`가 남아 있어 비율은 0.0이다.
- **보스 체력이 줄어듦** (`battle.EnemyDamaged`): 그 적이 `Boss`와 같은 객체면 `Changed`를 한 번 보낸다.
- **`Changed`가 가는 때** (이것이 전부다): `Begin`의 끝, 일반 몬스터가 쓰러질 때마다, 보스 체력이 줄 때마다, 전환 시작(`StageCleared`·`StageFailed` 뒤), 가려진 순간의 교체(`StageStarted` 뒤), 전환이 끝날 때.
- 성공해도 실패해도 금화와 강화 단계는 그대로다. 재도전 중 다시 처치한 몬스터는 다시 금화를 준다.
- 체력 전부 회복, 제자와 카메라의 시작 위치 이동, 공격 대기 초기화는 `battle.Reset()`이 한다 (`docs/spec/combat.md`).
- 화면(`StageView`)은 매 프레임 `Tick(Time.deltaTime)`을 부르고 `FadeAlpha`를 읽어 막을 그리며, `Changed`를 받을 때마다 글자와 바를 다시 그린다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| 생성자의 `wallet`, `upgrades`, `battle`, `random` 중 하나가 `null` | `ArgumentNullException` | |
| 생성자의 `startStage`가 1보다 작음 | 1로 만든다 (예외 없음) | `StageNumber` 1, 알림 없음 |
| `Begin()`을 두 번째로 부름 | 아무 일도 없다 | 변화 없음, 알림 없음 |
| `Begin()` 전에 전투에서 적이 쓰러짐 | 무시한다 | 금화·처치 수 그대로, 알림 없음 |
| `Begin()` 전의 `Tick` | 아무 일도 없다 | 변화 없음 |
| `Playing` 중의 `Tick` | 아무 일도 없다 | 변화 없음 |
| `Tick(0)` | 아무 일도 없다 | 변화 없음 |
| `Tick(음수)`, `Tick(NaN)`, `Tick(무한대)` | `ArgumentOutOfRangeException` (`Begin` 전이어도, `Playing`이어도) | 변화 없음 |
| `FadingOut` 시작에서 `Tick(1.0 이상)` | 교체와 전환 끝을 한 번에 한다 | `Phase`는 `Playing`, `StageStarted` 1번 |
| `FadingOut` 시작에서 `Tick(0.3)` 두 번 | 남은 0.1초를 페이드인으로 넘긴다 | `Phase`는 `FadingIn`, `FadeAlpha` 0.8 |
| 한 칸에 일반 몬스터 여러 마리가 쓰러짐 | 한 마리씩 차례로 처리한다 | 예: 3마리 → 금화 3번, 처치 수 +3, `Changed` 3번 |
| 제자와 일반 몬스터가 같은 칸에 쓰러짐 | 그 몬스터의 금화를 주고 실패 | 금화 +1마리분, 처치 수 +1, `StageFailed` 1번 |
| 제자와 보스가 같은 칸에 쓰러짐 | 실패. 보스 금화 없음 | 금화 그대로, `StageCleared` 없음, `StageFailed` 1번 |
| 열 번째 일반 몬스터와 제자가 같은 칸에 쓰러짐 | 금화를 주고, 보스를 내지 않고 실패 | 처치 수 10, `Boss`는 `null`, `StageFailed` 1번 |
| 처치 수가 이미 10인데 일반 몬스터가 또 쓰러짐 | 금화는 준다. 처치 수는 10 그대로, 보스는 다시 내지 않는다 | 금화 +1마리분, `Changed` 1번 |
| 처치 수가 10이 되기 전에 `Kind`가 `Boss`인 적이 쓰러짐 | 보스 처치와 똑같이 처리한다 (성공) | |
| 페이드 중(`FadingOut`, `FadingIn`)의 강화 시도 | 잠겨 있어 `TryUpgrade`가 `false` (`docs/spec/stat_upgrade.md`) | 금화·단계 그대로 |
| `StageNumber`가 `int.MaxValue`일 때 성공 | 번호를 올리지 않는다 (예외 없음) | 교체 뒤에도 `int.MaxValue` |
| `StageRewards`의 `stage`가 1보다 작음 | `ArgumentOutOfRangeException` | |
| `StageLayout.RollGroups(null)` | `ArgumentNullException` | |
| `StageLayout.Positions(null)` | `ArgumentNullException` | |
| `StageLayout.Positions`의 원소가 1보다 작거나 3보다 큼 | `ArgumentOutOfRangeException` | |
| `StageLayout.Positions(빈 목록)` | 빈 배열 | |
| `StageText.StageLabel(0 이하)` | 그대로 표기 (예외 없음) | 예: `STAGE 0` |

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 스테이지 번호의 타입 | `int` | 처음 값 1, 최소 1, 최대 `int.MaxValue`. 마지막 스테이지는 없다 |
| 금화의 타입 | `long` | 정수만 쓴다. 반올림 없음. `stage`를 `long`으로 바꾼 뒤 곱한다 |
| 시간·좌표·비율의 타입 | `double` (초, 유닛) | 반올림하지 않는다. 테스트는 오차 1e-6 안에서 비교한다 |
| 일반 몬스터 금화 `StageRewards.NormalGold(stage)` | `10 + 2 × (stage − 1)` (임시) | 1·2·3스테이지 → 10·12·14 |
| 보스 금화 `StageRewards.BossGold(stage)` | `50 + 10 × (stage − 1)` (임시) | 1·2·3스테이지 → 50·60·70 |
| 한 스테이지의 일반 몬스터 수 `StageLayout.NormalCount` | 10 | |
| 무리 한 개의 최대 마리 수 `StageLayout.MaxGroupSize` | 3 | 최소 1 |
| 무리 마리 수 고르기 | 남은 수가 0이 될 때까지 `random.Next(1, Math.Min(3, 남은 수) + 1)`을 고르고 남은 수에서 뺀다 | 합은 정확히 10. 무리 수는 4~10개. 예: `2·1·3·1·3`, `1·2·2·3·2` |
| 첫 무리 중심 `StageLayout.FirstGroupPosition` | 6.0 유닛 (임시) | 제자의 시작 좌표는 0 |
| 무리 중심 간격 `StageLayout.GroupSpacing` | 3.0 유닛 (임시) | `g`번째(0부터) 무리 중심 = `6.0 + 3.0 × g` |
| 무리 안 간격 `StageLayout.InGroupSpacing` | 0.3 유닛 (임시) | `n`마리 무리의 `i`번째(0부터) = `중심 + (i − (n − 1) / 2.0) × 0.3` |
| 좌표 예 | `2·1·3·1·3` → 5.85, 6.15, 9.0, 11.7, 12.0, 12.3, 15.0, 17.7, 18.0, 18.3 | 무리 순서, 무리 안에서는 작은 좌표부터 |
| 보스 등장 거리 `StageLayout.BossDistance` | 18.0 유닛 (임시) | 열 번째 처치 순간의 `battle.DisciplePosition + 18.0`. 가로로 긴 화면(20:9)의 오른쪽 끝(제자 앞 16.5)보다 밖이다 |
| 페이드 시간 `StageProgress.FadeSeconds` | 0.5초 (임시) | 페이드아웃 0.5초, 페이드인 0.5초 |
| 페이드 비교의 허용 오차 | 1e-9 | 페이드 시간 `≥ 0.5 − 1e-9`이면 끝난 것으로 본다 |
| 진행도 | `DefeatedNormalCount / 10.0` | 한 마리당 10%. 3마리 → 0.3 |
| 스테이지 글자 `StageText.StageLabel(stage)` | `"STAGE " + stage` | 1 → `STAGE 1`. `stage.ToString(CultureInfo.InvariantCulture)`. 1단계는 한글 글꼴이 없어 한글을 쓰지 않는다 |
| 보스 이름 `StageText.BossName` | `GATEKEEPER` (임시) | 모든 스테이지에서 같다. 기획의 `수문장` |

몬스터·보스의 체력·공격력·공격 간격은 이 기능의 수치가 아니다 (`docs/spec/combat.md`의 `EnemyStats`).

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.Stage
{
    public enum StagePhase
    {
        Playing = 0,     // 전투 중
        FadingOut = 1,   // 성공·실패 뒤 화면을 가리는 중
        FadingIn = 2,    // 새 배치를 마치고 화면을 여는 중
    }

    public static class StageRewards
    {
        // 일반 몬스터 한 마리의 금화. 10 + 2 * (stage - 1). long 으로 계산.
        // stage 가 1 보다 작으면 ArgumentOutOfRangeException.
        public static long NormalGold(int stage);

        // 보스의 금화. 50 + 10 * (stage - 1). long 으로 계산.
        // stage 가 1 보다 작으면 ArgumentOutOfRangeException.
        public static long BossGold(int stage);
    }

    public static class StageLayout
    {
        public const int NormalCount = 10;
        public const int MaxGroupSize = 3;
        public const double FirstGroupPosition = 6.0;
        public const double GroupSpacing = 3.0;
        public const double InGroupSpacing = 0.3;
        public const double BossDistance = 18.0;

        // 무리별 마리 수를 정한다. 남은 수가 0 이 될 때까지
        // random.Next(1, Math.Min(3, 남은 수) + 1) 을 고른다. 합은 정확히 10.
        // random 이 null 이면 ArgumentNullException.
        public static int[] RollGroups(System.Random random);

        // 무리 구성을 좌표로 바꾼다. g 번째 무리 중심 = 6.0 + 3.0 * g,
        // n 마리 무리의 i 번째 = 중심 + (i - (n - 1) / 2.0) * 0.3. 무리 순서, 무리 안은 작은 좌표부터.
        // groups 가 null 이면 ArgumentNullException. 원소가 1 보다 작거나 3 보다 크면 ArgumentOutOfRangeException.
        // 빈 목록이면 빈 배열. 합이 10 인지는 검사하지 않는다.
        public static double[] Positions(System.Collections.Generic.IReadOnlyList<int> groups);
    }

    public sealed class StageProgress
    {
        public const double FadeSeconds = 0.5;

        // wallet: 처치 금화를 줄 지갑. upgrades: 페이드 동안 잠글 강화. battle: 적을 놓고 알림을 받을 전투.
        // random: 무리 구성에 쓸 난수. 넷 중 하나라도 null 이면 ArgumentNullException.
        // startStage: 저장된 값으로 되살릴 때 넣는다. 1 보다 작으면 1 로 만든다 (예외 없음).
        // battle.EnemyDefeated, battle.DiscipleDefeated, battle.EnemyDamaged 를 구독한다.
        // 생성자는 전투·지갑·강화를 건드리지 않고 알림을 보내지 않는다.
        // 시작 상태: Phase 는 Playing, HasBegun 은 false, 처치 수 0, Boss 는 null.
        public StageProgress(
            PlumSword.Core.Currency.Wallet wallet,
            PlumSword.Core.StatUpgrade.StatUpgrades upgrades,
            PlumSword.Core.Combat.Battle battle,
            System.Random random,
            int startStage = 1);

        public int StageNumber { get; }           // 1 이상
        public StagePhase Phase { get; }
        public bool HasBegun { get; }             // Begin 이 불린 뒤 true
        public int DefeatedNormalCount { get; }   // 0 이상 10 이하
        public PlumSword.Core.Combat.Enemy Boss { get; }   // 이번 스테이지에서 낸 보스. 없으면 null
        public bool IsBossActive { get; }         // Boss != null
        public double BarRatio { get; }           // Boss 가 null 이면 처치 수 / 10.0, 아니면 보스 체력 비율
        public double FadeAlpha { get; }          // 페이드 막의 진하기. 0.0 ~ 1.0

        // 첫 스테이지를 배치한다 (battle.Reset 뒤 일반 몬스터 10 마리).
        // StageStarted, Changed 를 순서대로 보낸다. 페이드·멈춤·잠금은 없다.
        // 두 번째부터는 아무 일도 없다.
        public void Begin();

        // 페이드 시간을 센다. Begin 전이거나 Playing 이면 아무것도 하지 않는다.
        // 0: 아무 일도 없다. 음수·NaN·무한대: ArgumentOutOfRangeException (상태와 상관없이).
        // 남은 시간은 다음 단계로 넘긴다 (FadingOut 시작에서 Tick(1.0) 이면 Playing 까지 간다).
        public void Tick(double deltaSeconds);

        // 스테이지가 배치된 뒤에 그 번호를 담아 보낸다 (Begin, 그리고 가려진 순간의 교체).
        public event System.Action<int> StageStarted;

        // 보스를 처치해 성공했을 때 끝난 스테이지 번호를 담아 보낸다. 이때 Phase 는 이미 FadingOut.
        public event System.Action<int> StageCleared;

        // 제자가 쓰러져 실패했을 때 그 스테이지 번호를 담아 보낸다. 이때 Phase 는 이미 FadingOut.
        public event System.Action<int> StageFailed;

        // 화면이 다시 그려야 할 때 보낸다. 가는 때는 "동작"의 목록이 전부다.
        public event System.Action Changed;
    }

    public static class StageText
    {
        public const string BossName = "GATEKEEPER";

        // "STAGE " + stage. 예: 1 -> "STAGE 1". 0 이하도 그대로 (예외 없음).
        public static string StageLabel(int stage);
    }
}
```

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래 하나를 더한다
    {
        // Awake 에서 Battle 을 만든 뒤
        // new StageProgress(Wallet, StatUpgrades, Battle, new System.Random()) 으로 하나 만든다.
        public PlumSword.Core.Stage.StageProgress StageProgress { get; }
    }
}

namespace PlumSword.Game.Stage
{
    public sealed class StageView : MonoBehaviour
    {
        // canvas 아래에 페이드 막, 스테이지 글자, 진행 바, 보스 이름을 만들고 지금 상태로 바로 표시한다.
        // stage.Changed 를 구독하고 OnDestroy 에서 끊는다.
        // Update 에서 stage.Tick(Time.deltaTime) 을 부른 뒤 페이드 막의 진하기를 맞춘다.
        public void Initialize(PlumSword.Core.Stage.StageProgress stage, UnityEngine.Canvas canvas);
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot.Awake`가 `Battle`을 만든 뒤 `StageProgress`를 하나 만들어 `GameRoot.StageProgress`로 들고 있는다. 게임에 `StageProgress`는 하나뿐이다. 난수는 시드 없는 `new System.Random()`이다 (테스트는 직접 만든 난수를 넣는다).
- `Tick`을 부르는 것은 `StageView` 하나뿐이다. `Begin`을 부르는 것은 `GameRoot` 하나뿐이다.
- 2단계 저장 기능이 쓰는 방법: 저장할 값은 `StageNumber` 하나다. `StageStarted`를 구독해 스테이지가 바뀔 때 저장한다. 되살릴 때는 생성자의 `startStage`에 저장된 번호를 넣고 `Begin()`을 부른다.
- 2단계 오프라인 보상 기능이 쓰는 방법: `StageRewards.NormalGold(StageProgress.StageNumber)`가 일반 몬스터 한 마리분 금화다.

### 필요한 것

- `PlumSword.Core.Currency.Wallet` (`docs/spec/currency.md`): `Add(long)`.
- `PlumSword.Core.StatUpgrade.StatUpgrades` (`docs/spec/stat_upgrade.md`): `SetLocked(bool)`.
- `PlumSword.Core.Combat.Battle` (`docs/spec/combat.md`): `Reset()`, `AddEnemy(EnemyKind, int, double)`, `SetPaused(bool)`, `DisciplePosition`, `IsDiscipleDefeated`, `EnemyDefeated`, `DiscipleDefeated`, `EnemyDamaged`.
- `PlumSword.Core.Combat.Enemy` (`Kind`, `Health`, `MaxHealth`), `PlumSword.Core.Combat.EnemyKind`, `PlumSword.Core.Combat.CombatLayout.HealthRatio(long, long)` (`docs/spec/combat.md`).
- `PlumSword.Game.GameRoot.Wallet`, `UiCanvas` (`docs/spec/currency.md`), `GameRoot.StatUpgrades` (`docs/spec/stat_upgrade.md`), `GameRoot.Battle` (`docs/spec/combat.md`). 금화·강화·전투 기능이 먼저 구현되어 있어야 한다.
- 전투의 임시 적 배치 `PlumSword.Game.Combat.TempEncounter`는 이 기능을 구현할 때 지운다 (`docs/spec/combat.md`가 그렇게 정했다). 클래스 파일과 그 `.meta`, `GameRoot`의 `[TempEncounter]` 조립 줄을 함께 지운다.

## 화면

- **조립**: `GameRoot.Awake`에서 `[CombatView]`를 만든 다음, `[TempEncounter]`를 만드는 줄을 지우고 그 자리에서 아래를 순서대로 한다.
  1. `StageProgress`를 만든다.
  2. `[Game]`의 자식 `[StageView]`를 만들어 `StageView`를 붙이고 `Initialize(StageProgress, UiCanvas)`를 부른다.
  3. `StageProgress.Begin()`을 부른다. 화면들이 전투의 `EnemyAdded`를 이미 구독한 뒤여야 몬스터가 보이기 때문에 맨 마지막이다.
- **페이드 막**: 캔버스를 꽉 채우는 검은색 `Image`(앵커 (0, 0) ~ (1, 1), 여백 0). 캔버스의 **첫 번째 자식**으로 둔다 (`SetAsFirstSibling`). 그래서 전투 화면만 가리고 보유 금화, 강화 버튼, 스테이지 글자, 진행 바는 막 위에 그대로 보인다. `raycastTarget = false`. 색의 알파는 매 프레임 `stage.FadeAlpha`다.
- **스테이지 글자**: 화면 위쪽 가운데. 앵커 (0.5, 1), 피벗 (0.5, 1), 위치 (0, −8), 크기 200×16 (임시). 글자는 `StageText.StageLabel(stage.StageNumber)` 그대로다 (예: `STAGE 1`). `TextMeshProUGUI`, 글자 크기 16 (임시), 흰색, 가운데 정렬, TMP 기본 글꼴, `raycastTarget = false`.
- **보스 이름**: 스테이지 글자 아래, 바 위. 앵커 (0.5, 1), 피벗 (0.5, 1), 위치 (0, −26), 크기 200×16 (임시). 글자는 `StageText.BossName`, 글자 크기 14 (임시), 흰색, 가운데 정렬. `stage.IsBossActive`가 `true`일 때만 보인다.
- **진행 바**: 보스 이름 아래. 앵커 (0.5, 1), 피벗 (0.5, 1), 위치 (0, −44), 크기 300×12 (임시). 바탕은 `Image` RGB (0.15, 0.15, 0.15). 채움은 바탕의 자식 `Image`로 왼쪽 끝이 고정되고 가로 길이가 `300 × stage.BarRatio`다 (앵커 (0, 0) ~ (`BarRatio`, 1), 여백 0). 둘 다 `raycastTarget = false`.
  - `stage.IsBossActive`가 `false`: 일반 몬스터 진행 바. 채움 색 노랑 RGB (0.95, 0.80, 0.25) (임시). 한 마리당 10%씩 찬다.
  - `stage.IsBossActive`가 `true`: 보스 체력 바. 채움 색 빨강 RGB (0.85, 0.20, 0.20) (임시). 보스가 등장한 순간 가득 찬 빨간 바로 바뀐다.
  - 보스를 처치했거나 실패한 뒤에도 가려진 순간의 교체까지는 보스 체력 바 그대로다. 교체 때 0%의 일반 진행 바로 돌아간다.
- **바뀌는 때**: 글자·보스 이름·바는 `Initialize`에서 한 번, 그 뒤로 `stage.Changed`가 올 때마다 다시 그린다. 페이드 막의 진하기만 매 프레임 `Update`에서 `Tick`을 부른 뒤 맞춘다.
- **누르는 것**: 없다. 이 화면은 입력을 받지 않는다. 페이드 중 강화 버튼이 눌리지 않는 것은 강화 잠금(`SetLocked`)이 한다. 버튼은 잠긴 동안 어둡게 보인다 (`docs/spec/stat_upgrade.md`).
- **연출**: 페이드 말고는 없다. 성공·실패 글자, 보스 등장 연출, 소리는 없다.
- **사람이 먼저 해 둘 것**: 금화 사양과 같다. TextMeshPro 기본 리소스(TMP Essentials)가 있어야 글자가 보인다. 없어도 예외로 게임이 멈추지 않아야 한다.
- 표시 글자, 바 비율, 페이드 진하기의 판단은 모두 Core(`StageText`, `StageProgress.BarRatio`, `FadeAlpha`, `IsBossActive`)에 있고 에딧 모드 테스트로 판정한다. `StageView`와 `GameRoot`의 조립은 자동 테스트 대상이 아니며 사람이 실행해 확인한다.

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| 현재 스테이지 번호 (`StageProgress.StageNumber`) | `int` | 1 | 1단계에서는 저장하지 않는다. 게임을 끄면 1로 돌아간다 |

- 저장과 불러오기는 2단계의 저장 기능이 만든다. 이 기능은 그때 필요한 것만 미리 제공한다.
  - 저장할 값: `StageNumber` 하나. 처치 수, 보스, 단계(`Phase`), 무리 구성은 저장하지 않는다. 다시 시작하면 저장된 스테이지의 처음부터 새 무리 구성으로 시작한다.
  - 되살리기: `new StageProgress(wallet, upgrades, battle, random, 저장된 번호)` 뒤 `Begin()`. 1보다 작은 값이 들어오면 1스테이지로 시작한다. 저장 전체의 손상 처리는 저장 기능이 정한다.
  - 저장할 때: 저장 기능이 `StageStarted`를 구독해 스테이지가 바뀔 때 저장한다.
- 오프라인 보상: 꺼져 있는 동안 스테이지는 진행되지 않고 번호도 바뀌지 않는다. 오프라인 보상 기능(2단계)은 `StageRewards.NormalGold(StageNumber)`를 한 마리분 금화로 쓴다.

## 범위 밖

- 제자의 이동·공격·체력, 몬스터의 접근과 공격, 몬스터·보스 능력치, 캐릭터 대체 표시와 머리 위 체력 바, 카메라, 임시 배경: 전투 기능(`docs/spec/combat.md`)이 정한다.
- 보유 금화 표시: 금화 기능(`docs/spec/currency.md`). 강화 버튼과 잠겼을 때의 모양: 강화 기능(`docs/spec/stat_upgrade.md`).
- 저장·불러오기, 오프라인 보상 계산과 보상 창: 2단계에서 한다.
- 다섯 테마 스테이지(종남파 ~ 천마), 스테이지 소개 글, 천마 뒤 반복(회차)과 회차 표시, 테마별 보스 이름: 2단계에서 한다. 그때 이 사양을 고친다. 1단계의 끝없이 올라가는 스테이지 번호는 프로토타입용이다.
- 보스 등장 연출, 성공·실패 글자나 결과 창, 소리: 하지 않는다. 보스 등장 연출은 4단계 후보다.
- 보스전 제한 시간, 스테이지 선택, 이전 스테이지로 돌아가기, 수동 재도전 버튼: 없다.
- 한글 표기(`스테이지 1`, `수문장`)와 한글 글꼴: 1단계에서는 하지 않는다. 글꼴이 준비되면 `StageText`의 글자만 바꾼다.
- UI까지 가리는 화면 전체 페이드: 하지 않는다 (전투 화면만 가린다).

## 완료 조건 초안

아래에서 따로 적지 않으면 지갑은 `new Wallet(0)`, 강화는 항목마다 적힌 `new StatUpgrades(wallet, 공격력, 최대 체력, 1.0)`, 전투는 `new Battle(upgrades)`, 스테이지는 `new StageProgress(wallet, upgrades, battle, 최소 난수)`로 만든 것이다.

- **최소 난수**: `System.Random`을 상속해 `Next(int minValue, int maxValue)`가 항상 `minValue`를 돌려주게 한 테스트용 클래스. 무리 구성은 `1`이 10개, 좌표는 6.0, 9.0, …, 33.0이다.
- **최대 난수**: 같은 방식으로 항상 `maxValue − 1`을 돌려주고, 받은 `maxValue`를 순서대로 기록하는 테스트용 클래스. 무리 구성은 `3·3·3·1`이다.
- "시작한 뒤"는 `Begin()`을 부른 뒤다. "n칸"은 `battle.Step()`을 n번 부르는 것이다. "적을 직접 넣는다"는 테스트가 `battle.AddEnemy`를 부르는 것이다. 좌표·비율은 오차 1e-6 안에서 비교한다.

보상 (`StageRewardsTests`)

1. `StageRewards.NormalGold`는 1·2·3스테이지에서 10·12·14, `StageRewards.BossGold`는 50·60·70이다.
2. `StageRewards.NormalGold(int.MaxValue)`는 4294967302, `StageRewards.BossGold(int.MaxValue)`는 21474836510이다.
3. `StageRewards.NormalGold(0)`과 `StageRewards.BossGold(0)`은 각각 `ArgumentOutOfRangeException`을 던진다.

배치 (`StageLayoutTests`)

4. `StageLayout.RollGroups(최대 난수)`는 `3, 3, 3, 1`이고, 난수가 받은 `maxValue`는 순서대로 4, 4, 4, 2다.
5. `StageLayout.RollGroups(최소 난수)`는 `1`이 10개다.
6. 시드 0부터 999까지의 `new System.Random(시드)`마다 `StageLayout.RollGroups`의 결과는 합이 10이고, 모든 원소가 1 이상 3 이하이며, 길이가 4 이상 10 이하다.
7. `StageLayout.Positions(new[] { 2, 1, 3, 1, 3 })`은 5.85, 6.15, 9.0, 11.7, 12.0, 12.3, 15.0, 17.7, 18.0, 18.3이다. `Positions(new[] { 3, 3, 3, 1 })`은 5.7, 6.0, 6.3, 8.7, 9.0, 9.3, 11.7, 12.0, 12.3, 15.0이다.
8. `StageLayout.Positions(빈 배열)`은 빈 배열이다. `Positions(null)`과 `RollGroups(null)`은 `ArgumentNullException`을, `Positions(new[] { 0 })`과 `Positions(new[] { 4 })`는 `ArgumentOutOfRangeException`을 던진다.

진행 (`StageProgressTests`)

9. (10, 100) 새로 만들면 `StageNumber`는 1, `HasBegun`은 `false`, `Phase`는 `Playing`, `DefeatedNormalCount`는 0, `Boss`는 `null`, `IsBossActive`는 `false`, `BarRatio`는 0.0, `FadeAlpha`는 0.0이고, `battle.Enemies.Count`는 0이다. `startStage`에 5를 넣으면 `StageNumber`는 5, 0이나 −3을 넣으면 1이고 예외가 없다.
10. 생성자의 `wallet`, `upgrades`, `battle`, `random`에 각각 `null`을 넣으면 `ArgumentNullException`을 던진다.
11. (10, 100) `Begin()`을 부르면 `HasBegun`은 `true`, `battle.Enemies.Count`는 10이고, 적의 `Position`은 순서대로 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 24.0, 27.0, 30.0, 33.0, 모두 `Kind`가 `Normal`이고 `MaxHealth`가 30이다. `StageStarted`가 1을 담아 1번, 그 뒤에 `Changed`가 1번 온다. `battle.Paused`와 `upgrades.Locked`는 `false`다. `Begin()`을 한 번 더 부르면 `battle.Enemies.Count`는 10 그대로이고 알림은 더 오지 않는다.
12. (10, 100) `startStage`를 3으로 만들고 시작하면 적 10마리의 `MaxHealth`는 모두 50이다.
13. (10, 100) 최대 난수로 만들고 시작하면 적의 `Position`은 순서대로 5.7, 6.0, 6.3, 8.7, 9.0, 9.3, 11.7, 12.0, 12.3, 15.0이다.
14. (1000, 100) `Begin()`을 부르지 않고 `Normal` 적 하나를 1스테이지, 좌표 0.8에 직접 넣어 1칸 진행하면, 금화는 0, `DefeatedNormalCount`는 0이고 `Changed`는 0번 온다. `Tick(1.0)`을 불러도 `Phase`는 `Playing`이고 `battle.Enemies.Count`는 0이다.
15. (1000, 100) 시작한 뒤 `Normal` 적 3마리를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면, 금화는 30, `Wallet.BalanceChanged`의 값은 순서대로 10, 20, 30, `DefeatedNormalCount`는 3, `BarRatio`는 0.3, `Changed`는 (시작 때의 1번 뒤로) 3번 오고, `Phase`는 `Playing`이다.
16. (1000, 100) 시작한 뒤 `Normal` 적 10마리를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면, 금화는 100, `DefeatedNormalCount`는 10, `IsBossActive`는 `true`, `Boss.Kind`는 `Boss`, `Boss.Position`은 18.0, `Boss.MaxHealth`는 100, `BarRatio`는 1.0, `battle.Enemies.Count`는 11, `battle.DiscipleHealth`는 50, `Phase`는 `Playing`이다.
17. (1000, 100) 시작한 뒤 `Normal` 적 11마리를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면, 금화는 110, `DefeatedNormalCount`는 10, `battle.Enemies.Count`는 11 (보스는 하나뿐), `battle.DiscipleHealth`는 45다.
18. (30, 100000) 시작한 뒤 `Normal` 적 10마리를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행한 다음, `Boss.Health`가 100보다 작아질 때까지 최대 5000칸 진행하면, `Boss.Health`는 70, `BarRatio`는 0.7이고, 마지막 칸에서 `Changed`가 1번 이상 온다.
19. (1000, 100) 시작한 뒤 `Boss` 적 하나를 1스테이지, 좌표 1.0에 직접 넣고 1칸 진행하면, 금화는 50, `StageCleared`가 1을 담아 1번, `StageFailed`는 0번 온다. `Phase`는 `FadingOut`, `StageNumber`는 1, `FadeAlpha`는 0.0, `battle.Paused`는 `true`, `upgrades.Locked`는 `true`, `battle.DiscipleHealth`는 90이다. `StageCleared`를 받는 시점에 읽은 `Phase`는 `FadingOut`이고, `Changed`는 `StageCleared` 뒤에 온다.
20. (10, 5) 시작한 뒤 `Normal` 적 하나를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면, 금화는 0, `DefeatedNormalCount`는 0, `StageFailed`가 1을 담아 1번, `StageCleared`는 0번 온다. `Phase`는 `FadingOut`, `battle.Paused`는 `true`, `upgrades.Locked`는 `true`다.
21. (30, 5) 시작한 뒤 `Normal` 적 하나를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면 (제자와 일반 몬스터가 함께 쓰러짐), 금화는 10, `DefeatedNormalCount`는 1, `StageFailed`가 1번, `StageCleared`는 0번 온다.
22. (100, 10) 시작한 뒤 `Boss` 적 하나를 1스테이지, 좌표 1.0에 직접 넣고 1칸 진행하면 (제자와 보스가 함께 쓰러짐), 금화는 0, `StageCleared`는 0번, `StageFailed`가 1을 담아 1번 오고 `Phase`는 `FadingOut`이다.
23. (1000, 50) 시작한 뒤 `Normal` 적 10마리를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면 (열 번째 몬스터와 제자가 함께 쓰러짐), 금화는 100, `DefeatedNormalCount`는 10, `Boss`는 `null`, `battle.Enemies.Count`는 10, `StageFailed`가 1번 온다.
24. 19번에 이어 `Tick(0.25)`를 부르면 `FadeAlpha`는 0.5, `Phase`는 `FadingOut`, `StageNumber`는 1이다. 한 번 더 `Tick(0.25)`를 부르면 `StageNumber`는 2, `Phase`는 `FadingIn`, `FadeAlpha`는 1.0, `DefeatedNormalCount`는 0, `Boss`는 `null`, `BarRatio`는 0.0, `battle.Enemies.Count`는 10, 적의 `MaxHealth`는 모두 40, `Position`은 6.0부터 3.0씩 33.0까지, `battle.DisciplePosition`은 0, `battle.DiscipleHealth`는 100, `battle.Paused`와 `upgrades.Locked`는 `true`, 금화는 50이고, `StageStarted`가 2를 담아 1번 온다.
25. 24번에 이어 `Tick(0.25)`를 부르면 `FadeAlpha`는 0.5, `Phase`는 `FadingIn`이다. 한 번 더 `Tick(0.25)`를 부르면 `Phase`는 `Playing`, `FadeAlpha`는 0.0, `battle.Paused`와 `upgrades.Locked`는 `false`이고, `StageStarted`는 더 오지 않는다.
26. 20번에 이어 `Tick(0.5)`를 부르면 `StageNumber`는 1, `Phase`는 `FadingIn`, `battle.Enemies.Count`는 10, `battle.DiscipleHealth`는 5, `battle.IsDiscipleDefeated`는 `false`이고, `StageStarted`가 1을 담아 한 번 더 온다. 이어서 `Tick(0.5)`를 부르면 `Phase`는 `Playing`이다.
27. 19번에 이어 `Tick(0.3)`을 2번 부르면 `Phase`는 `FadingIn`, `StageNumber`는 2, `FadeAlpha`는 0.8이다.
28. 19번에 이어 `Tick(2.0)`을 부르면 `Phase`는 `Playing`, `StageNumber`는 2, `battle.Paused`와 `upgrades.Locked`는 `false`이고, `StageStarted`가 2를 담아 1번 온다.
29. 19번에 이어 (금화 50, `FadingOut`) `upgrades.TryUpgrade(StatType.Attack)`은 `false`이고 금화는 50 그대로다. `Tick(1.0)` 뒤에는 `true`이고 금화는 30이다.
30. (10, 100) 시작한 뒤 (`Playing`) `Tick(1.0)`과 `Tick(0)`을 불러도 `Phase`는 `Playing`, `StageNumber`는 1, `battle.Enemies.Count`는 10이고 알림은 더 오지 않는다. `Tick(-0.1)`, `Tick(double.NaN)`, `Tick(double.PositiveInfinity)`는 `Begin()` 전에도 뒤에도 각각 `ArgumentOutOfRangeException`을 던진다.
31. `new StatUpgrades(wallet, 100000000000, 100000000000, 1.0)`과 `startStage`가 `int.MaxValue`인 스테이지로, 시작한 뒤 `Boss` 적 하나를 `int.MaxValue`스테이지, 좌표 1.0에 직접 넣고 1칸 진행하면, 금화는 21474836510, `StageCleared`가 1번 온다. `Tick(0.5)` 뒤 `StageNumber`는 `int.MaxValue` 그대로이고 예외가 없다.
32. (1000, 100000) 시작한 뒤 아무것도 직접 넣지 않고 `Phase`가 `FadingOut`이 될 때까지 최대 20000칸 진행하면 (스테이지 한 판 전체), 금화는 150, `DefeatedNormalCount`는 10, `StageCleared`가 1을 담아 1번, `StageFailed`는 0번 오고, `IsBossActive`는 `true`, `BarRatio`는 0.0이다.

표시 글자 (`StageTextTests`)

33. `StageText.StageLabel`은 1 → `"STAGE 1"`, 12 → `"STAGE 12"`, 0 → `"STAGE 0"`을 돌려준다. 현재 문화권을 `de-DE`로 바꾼 상태에서도 1234 → `"STAGE 1234"`다. `StageText.BossName`은 `"GATEKEEPER"`다.

화면 (자동 테스트 없음. 사람이 실행해 확인)

34. 게임을 실행하면 화면 위쪽 가운데에 `STAGE 1`과 빈 노란 진행 바가 보이고, 제자 앞에 몬스터 무리가 보인다. 몬스터를 처치할 때마다 금화가 10씩 늘고 바가 10%씩 찬다. 다시 실행하면 무리 구성이 달라진다.
35. 10마리를 모두 처치하면 바가 가득 찬 빨간 바로 바뀌고 그 위에 `GATEKEEPER`가 보인다. 제자가 달려가 화면 밖에 있던 보스와 싸우고, 보스가 맞을 때마다 빨간 바가 줄어든다.
36. 보스를 처치하면 금화가 50 늘고 전투 화면이 검게 가려졌다가 열리며 `STAGE 2`가 된다. 가려진 동안에도 보유 금화와 강화 버튼은 보이고, 버튼은 어둡고 눌리지 않는다. 열린 뒤 바는 빈 노란 바이고 제자의 체력은 가득하다.
37. 제자의 체력이 0이 되면 같은 방식으로 가려졌다가 같은 스테이지 번호로 처음부터 다시 시작한다. 금화와 강화 단계는 그대로다.
38. `TempEncounter` 클래스와 `GameRoot`의 `[TempEncounter]` 조립이 없다.
39. 기존 테스트 `CoreAssemblyTests.Core_DoesNotReferenceUnity`가 계속 통과한다.

## 결정 기록

| 질문 | 답 | 방식 |
| --- | --- | --- |
| 스테이지 사양이 맡는 범위 | 배치, 보스 등장, 성공·실패와 재도전, 처치 금화 계산·지급, 스테이지 UI, 페이드. 이동·공격·체력·몬스터 능력치·카메라·배경은 전투 | 이미 정해짐 (`docs/spec/combat.md`) |
| 보스 등장 거리 | 열 번째 처치 순간의 제자 좌표 + 18.0 유닛 | 사용자 선택 (추천안) / 임시값 |
| 페이드가 가리는 범위 | 전투 화면만. 금화, 강화 버튼, 스테이지 글자, 진행 바는 그대로 보인다 | 사용자 선택 (추천안) |
| 페이드 단계와 시간을 누가 세나 | Core의 `StageProgress.Tick`. 남은 시간은 다음 단계로 넘긴다 | 직접 정함 |
| 성공·실패 뒤 전환이 시작되는 때 | 기다림 없이 바로 페이드아웃 | 직접 정함 (기획 그대로) |
| 게임을 처음 켰을 때 | 페이드 없이 `Begin()`으로 바로 1스테이지 시작 | 직접 정함 |
| 열 번째 일반 몬스터와 제자가 함께 쓰러질 때 | 금화는 주고, 보스를 내지 않고 실패 | 직접 정함 |
| 처치 수가 10을 넘거나, 10 전에 보스 종류가 쓰러질 때 | 적을 `Kind`로만 가린다. 금화는 주고 처치 수는 10에서 멈춘다. 보스 종류가 쓰러지면 성공 | 직접 정함 |
| 무리 마리 수를 고르는 난수 | `random.Next(1, Math.Min(3, 남은 수) + 1)`을 남은 수가 0이 될 때까지 | 직접 정함 (기획의 규칙을 식으로 옮김) |
| 무리의 좌표 | 첫 무리 중심 6.0, 중심 간격 3.0, 무리 안은 중심 기준 대칭으로 0.3 간격 | 직접 정함 / 임시값 |
| 숫자 타입 | 스테이지 번호 `int`, 금화 `long`, 시간·좌표·비율 `double` | 직접 정함 |
| 스테이지 번호의 상한 | `int.MaxValue`에서 더 오르지 않는다 | 직접 정함 |
| 잘못된 저장 값(1보다 작은 번호) | 1로 시작한다. 예외 없음 | 직접 정함 (기존 사양과 같은 방식) |
| 잘못된 인자(`null`, 음수 시간, 스테이지 0) | 예외 (`ArgumentNullException`, `ArgumentOutOfRangeException`) | 직접 정함 |
| 한글 글꼴이 없을 때 글자 | `STAGE 1`, 보스 이름 `GATEKEEPER` | 직접 정함 (기존 사양과 같은 방식) / 임시값 |
| 진행 바와 글자의 위치·색 | 위쪽 가운데. 일반 노랑, 보스 빨강. 좌표와 크기는 임시값 | 직접 정함 / 임시값 |
| 보스가 쓰러진 뒤의 바 | 가려진 순간의 교체까지 보스 체력 바(0%) 그대로 | 직접 정함 |
| 타입·메서드 이름과 알림 | `StageProgress`, `StagePhase`, `StageRewards`, `StageLayout`, `StageText`, `StageView`. 알림은 `StageStarted`, `StageCleared`, `StageFailed`, `Changed` | 직접 정함 |
