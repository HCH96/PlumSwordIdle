# 경지 돌파 사양

- 기능 이름: realm (네임스페이스 `PlumSword.Core.Realm`)
- 작성일: 2026-11-18

## 목표

금화를 모아 돌파 버튼을 눌러 제자의 경지를 `삼류 → 이류 → 일류 → 절정 → 초절정` 다섯 단계로 올리는 기능이다. 이 기능은 경지 단계, 돌파 비용과 구매(금화 소비), 경지별 공격력·최대 체력 보너스 비율(정수 %), 돌파 버튼, 전투 화면 왼쪽 위의 현재 경지 표시, 제자 주변의 경지별 오오라까지 맡는다.

보너스를 실제 공격력·최대 체력에 적용하는 계산(내림 포함)은 능력치 강화 기능이, 돌파 때 현재 체력을 늘리는 것은 전투 기능이 한다. 이 기능은 그쪽이 읽을 단계·비율·알림만 제공한다. 저장 데이터 형식에 경지를 넣는 것은 저장 기능이 한다.

출처: `docs/design/milestone_3.md` "목표와 현재 방향", "경지 돌파", "저장과 반복 진행", "3단계 화면", "3단계 마일스톤 달성 기준".

## 동작

- 경지는 다섯 단계다: `RealmLevel.ThirdRate`(삼류) → `SecondRate`(이류) → `FirstRate`(일류) → `Peak`(절정) → `Transcendent`(초절정). 처음에는 삼류다. 경지는 내려가지 않는다.
- 돌파 조건은 금화뿐이다. 스테이지 번호, 강화 단계, 전투 상태(패배, 구간 소개 중 등)는 보지 않는다.
- **돌파** `TryBreakthrough()`: 아래 순서로 처리한다.
  1. 잠겨 있으면(`Locked`) `false`.
  2. 초절정(`IsMax`)이면 `false`.
  3. `wallet.TrySpend(NextCost)`가 `false`면(금화 부족) `false`.
  4. `Current`를 한 단계 올린다.
  5. `BrokeThrough`를 새 경지를 담아 한 번 보내고 `true`를 돌려준다.
- `false`를 돌려준 경우에는 금화도 경지도 바뀌지 않고 알림도 없다.
- 알림 순서: 돌파 한 번에 `Wallet.BalanceChanged`(3번에서) → 경지 증가 → `BrokeThrough` 순서다. `BalanceChanged`를 받는 시점에는 `Current`가 아직 이전 경지다. `BrokeThrough`를 받는 쪽에서 `Current`, `BonusPercent`, `NextCost`, `IsMax`를 읽으면 새 값이다.
- **확인** `CanBreakthrough()`: 잠겨 있지 않고, 초절정이 아니고, `wallet.CanSpend(NextCost)`가 `true`일 때만 `true`. 아무것도 바꾸지 않는다. 돌파 버튼을 누를 수 있는지는 이 값 그대로다.
- **보너스 비율** `BonusPercent`: 지금 경지의 누적 보너스(정수 %). `RealmRules.BonusPercent(Current)`와 같다. 부를 때마다 지금 경지로 계산한다. 공격력과 최대 체력에 같은 비율이 쓰인다.
- **잠금** `SetLocked(locked)`: 잠긴 동안에는 `TryBreakthrough`와 `CanBreakthrough`가 모두 `false`다. 값이 실제로 바뀐 때에만 `LockedChanged`를 새 값을 담아 한 번 보낸다. 처음에는 잠겨 있지 않다. 잠금은 `GameRoot`가 `StatUpgrades.LockedChanged`를 구독해 같은 값으로 `SetLocked`를 불러 맞춘다. 그래서 스테이지 전환 페이드 동안 강화와 함께 잠긴다. 스테이지 기능은 이 기능을 모른다.
- 스테이지 성공·실패, 천마 처치 뒤 1스테이지로 돌아가는 것, 게임 재시작은 경지에 아무 영향을 주지 않는다.
- 같은 시점에 여러 번 부르면 부른 순서대로 한 번씩, 그때의 금화와 경지로 따로 판정한다.
- 화면은 버튼을 누르면 `TryBreakthrough`를 부르고, `Wallet.BalanceChanged`, `BrokeThrough`, `LockedChanged`를 받을 때마다 버튼과 경지 표시를 다시 그린다. 오오라는 `BrokeThrough`를 받으면 새 경지의 모양으로 바꾸고 돌파 연출을 한다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| 금화가 비용보다 적을 때 `TryBreakthrough` | `false` | 금화·경지 그대로, 알림 없음 |
| 금화가 비용과 같을 때 `TryBreakthrough` | `true` | 금화 0, 경지 +1, `BalanceChanged` 1번, `BrokeThrough` 1번 |
| 초절정에서 `TryBreakthrough` | `false` (금화가 충분해도) | 금화·경지 그대로, 알림 없음 |
| 초절정에서 `NextCost` | 0 (예외 없음) | 변화 없음. 화면은 `MAX`를 보여 준다 |
| 초절정에서 `CanBreakthrough` | `false` | 변화 없음 |
| 잠긴 동안 `TryBreakthrough` | `false` | 금화·경지 그대로, 알림 없음 |
| 잠긴 동안 `CanBreakthrough` | `false` | 변화 없음 |
| `SetLocked`를 지금과 같은 값으로 부름 | 아무 일도 없다 | `LockedChanged` 없음 |
| `TryBreakthrough`를 연달아 부름 | 부를 때마다 그때의 금화·경지로 따로 판정 | 예: 금화 1000에서 세 번 → `true`(200), `true`(500), `false`(1000 필요), 금화 300, 경지 일류 |
| 생성자의 `wallet`이 `null` | `ArgumentNullException` | |
| 생성자의 `startLevel`이 음수 | 0(삼류)으로 만든다 (예외 없음) | 알림 없음, 금화 그대로 |
| 생성자의 `startLevel`이 4보다 큼 | 4(초절정)로 만든다 (예외 없음) | 알림 없음, 금화 그대로 |
| 정의되지 않은 `RealmLevel` 값 (예: `(RealmLevel)99`) | `ArgumentOutOfRangeException` (`RealmRules.BonusPercent`, `RealmRules.BreakthroughCost`, `RealmText.Name`, `RealmText.Label`, `RealmText.AssetId`, `RealmText.ButtonLabel`, `RealmAura.HasAura`, `RealmAura.Color`, `RealmAura.PulseAlpha`) | 변화 없음 |
| `RealmAura.PulseAlpha`의 `seconds`가 `NaN`이거나 무한대 | `ArgumentOutOfRangeException` | |
| `RealmAura.BurstScale`의 `secondsSinceBreakthrough`가 `NaN` | `ArgumentOutOfRangeException` | |
| `RealmAura.BurstScale`에 음수 | 1.5 (예외 없음) | |
| `RealmAura.BurstScale`에 양의 무한대 | 1.0 (예외 없음) | |
| `RealmText.Cost(음수, false)` | 그대로 표기 (예외 없음) | 예: `G -5` |

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 경지의 타입 | `enum RealmLevel` (값 0~4) | 처음 값 `ThirdRate`(0). 단계 수 `RealmRules.LevelCount = 5` |
| 비용의 타입 | `long` | 정수. 반올림 없음 |
| 보너스 비율의 타입 | `int` (%) | 정수. 배율 `double`은 쓰지 않는다 |
| 돌파 비용 `RealmRules.BreakthroughCost(from)` | 삼류 → 이류 200, 이류 → 일류 500, 일류 → 절정 1,000, 절정 → 초절정 2,000, 초절정 0 (모두 임시) | `from`은 돌파하기 전 경지. 삼류에서 초절정까지 합계 3,700 |
| 누적 보너스 `RealmRules.BonusPercent(realm)` | 삼류 0, 이류 10, 일류 20, 절정 30, 초절정 40 (모두 임시) | 공격력·최대 체력 공통 |
| 보너스 적용 공식 (참고, 계산은 능력치 강화 기능) | `(기본값 + 일반 강화 증가량) × (100 + BonusPercent) / 100`을 `long` 정수 나눗셈으로 (= 내림) | 예: 이류에서 체력 120 → 132, 공격력 12 → 13. 100 → 110 |
| 이름 표기 `RealmText.Name` | `THIRD RATE`, `SECOND RATE`, `FIRST RATE`, `PEAK`, `TRANSCENDENT` | 한글 글꼴이 없어 영문 |
| 경지 표시 `RealmText.Label` | `"REALM: " + Name` | 예: `REALM: SECOND RATE` |
| 버튼 첫 줄 `RealmText.ButtonTitle` | `BREAKTHROUGH` | 상수 |
| 비용 표기 `RealmText.Cost` | 최대면 `MAX`, 아니면 `"G " + GoldText.FormatNumber(cost)` | 500 → `G 500`, 1000 → `G 1,000` |
| 버튼 글자 `RealmText.ButtonLabel` | `ButtonTitle + "\n" + Name(current) + "\n" + Cost(cost, isMax)` | `BREAKTHROUGH\nSECOND RATE\nG 500` |
| 에셋 ID `RealmText.AssetId` | `third_rate`, `second_rate`, `first_rate`, `peak`, `transcendent` | 리소스 경로에 쓴다 ("화면") |
| 오오라가 있는 경지 `RealmAura.HasAura` | 이류·일류·절정·초절정. 삼류는 없음 | |
| 오오라 대체 색 `RealmAura.Color` (R, G, B, A) | 이류 (0.55, 0.85, 1.00, 0.35), 일류 (0.35, 0.55, 1.00, 0.40), 절정 (0.70, 0.40, 1.00, 0.45), 초절정 (1.00, 0.55, 0.75, 0.50), 삼류 (0, 0, 0, 0) (모두 임시) | `double` 0~1 |
| 오오라 깜빡임 `RealmAura.PulseAlpha(realm, seconds)` | `Color(realm).A × (0.75 + 0.25 × Math.Sin(2π × seconds / 1.6))` (임시) | 주기 1.6초. 삼류는 0. 예: 이류 0초 0.2625, 0.4초 0.35, 1.2초 0.175 |
| 돌파 연출 크기 `RealmAura.BurstScale(t)` | `t ≤ 0` → 1.5, `0 < t < 0.4` → `1.5 − 0.5 × t / 0.4`, `t ≥ 0.4` → 1.0 (임시) | 예: 0 → 1.5, 0.2 → 1.25, 0.4 → 1.0 |
| 실수 비교 | 오차 1e-9 | 테스트 기준 |

능력치 강화 단계·비용, 몬스터 능력치, 현재 체력은 이 기능의 수치가 아니다 ("범위 밖").

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.Realm
{
    public enum RealmLevel
    {
        ThirdRate = 0,     // 삼류
        SecondRate = 1,    // 이류
        FirstRate = 2,     // 일류
        Peak = 3,          // 절정
        Transcendent = 4,  // 초절정
    }

    public static class RealmRules
    {
        public const int LevelCount = 5;

        // 0, 10, 20, 30, 40 (%). 정의되지 않은 값: ArgumentOutOfRangeException.
        public static int BonusPercent(RealmLevel realm);

        // from 에서 다음 경지로 가는 비용. 200, 500, 1000, 2000, Transcendent 는 0.
        // 정의되지 않은 값: ArgumentOutOfRangeException.
        public static long BreakthroughCost(RealmLevel from);
    }

    public sealed class RealmProgress
    {
        // wallet: 돌파 비용을 쓸 지갑. null 이면 ArgumentNullException.
        // startLevel: 저장된 값으로 되살릴 때 넣는다 ((int)RealmLevel). 음수면 0, 4 보다 크면 4 (예외 없음).
        // 생성자는 금화를 쓰지 않고 알림을 보내지 않는다. 잠기지 않은 상태로 시작한다.
        public RealmProgress(PlumSword.Core.Currency.Wallet wallet, int startLevel = 0);

        public RealmLevel Current { get; }
        public bool IsMax { get; }            // Current == Transcendent
        public int BonusPercent { get; }      // RealmRules.BonusPercent(Current)
        public long NextCost { get; }         // RealmRules.BreakthroughCost(Current). 초절정이면 0

        // 지금 돌파할 수 있으면 true: 잠기지 않았고, 초절정이 아니고, 금화가 NextCost 이상. 아무것도 바꾸지 않는다.
        public bool CanBreakthrough();

        // 돌파한다. 성공하면 금화를 쓰고 경지를 한 단계 올린 뒤 BrokeThrough 를 보내고 true.
        // 잠김, 초절정, 금화 부족이면 아무것도 바꾸지 않고 false (알림 없음).
        public bool TryBreakthrough();

        // 돌파가 성공해 경지가 오른 뒤에 한 번, 새 경지를 담아 보낸다. 실패하면 보내지 않는다.
        public event System.Action<RealmLevel> BrokeThrough;

        public bool Locked { get; }           // 처음 값 false

        // 잠그거나 푼다. 값이 실제로 바뀐 때에만 LockedChanged 를 보낸다.
        public void SetLocked(bool locked);

        public event System.Action<bool> LockedChanged;
    }

    public static class RealmText
    {
        public const string ButtonTitle = "BREAKTHROUGH";

        // "THIRD RATE", "SECOND RATE", "FIRST RATE", "PEAK", "TRANSCENDENT". 정의되지 않은 값: ArgumentOutOfRangeException.
        public static string Name(RealmLevel realm);

        // "REALM: " + Name(realm).
        public static string Label(RealmLevel realm);

        // "third_rate", "second_rate", "first_rate", "peak", "transcendent". 리소스 경로용.
        public static string AssetId(RealmLevel realm);

        // isMax 가 true 면 "MAX" (cost 는 보지 않는다). 아니면 "G " + GoldText.FormatNumber(cost). 음수도 그대로.
        public static string Cost(long cost, bool isMax);

        // ButtonTitle + "\n" + Name(current) + "\n" + Cost(cost, isMax).
        public static string ButtonLabel(RealmLevel current, long cost, bool isMax);
    }

    // 오오라 대체 표시의 색. 0~1 의 double. 생성자는 값을 검사하지 않는다.
    public readonly struct AuraColor
    {
        public AuraColor(double r, double g, double b, double a);
        public double R { get; }
        public double G { get; }
        public double B { get; }
        public double A { get; }
    }

    public static class RealmAura
    {
        public const double PulsePeriodSeconds = 1.6;
        public const double BurstSeconds = 0.4;
        public const double BurstPeakScale = 1.5;

        // ThirdRate 만 false. 정의되지 않은 값: ArgumentOutOfRangeException.
        public static bool HasAura(RealmLevel realm);

        // "수치"의 색. ThirdRate 는 (0, 0, 0, 0).
        public static AuraColor Color(RealmLevel realm);

        // Color(realm).A * (0.75 + 0.25 * Math.Sin(2 * Math.PI * seconds / 1.6)). ThirdRate 는 0.
        // seconds 가 NaN·무한대면 ArgumentOutOfRangeException. 음수는 그대로 계산한다.
        public static double PulseAlpha(RealmLevel realm, double seconds);

        // 돌파 뒤 흐른 시간에 따른 크기 배율. t <= 0 -> 1.5, 0 < t < 0.4 -> 1.5 - 0.5 * t / 0.4, t >= 0.4 -> 1.0.
        // NaN 이면 ArgumentOutOfRangeException. 양의 무한대는 1.0.
        public static double BurstScale(double secondsSinceBreakthrough);
    }
}
```

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래 하나를 더한다
    {
        // Awake 에서 Wallet 을 만든 직후, StatUpgrades 보다 먼저 new RealmProgress(Wallet) 으로 하나 만든다.
        // StatUpgrades 를 만든 뒤 StatUpgrades.LockedChanged 를 구독해 Realm.SetLocked(같은 값) 을 부른다.
        public PlumSword.Core.Realm.RealmProgress Realm { get; }   // private set
    }
}

namespace PlumSword.Game.Realm
{
    public sealed class RealmView : MonoBehaviour
    {
        // canvas 아래에 경지 표시와 돌파 버튼을 만들고 지금 상태로 바로 표시한다.
        // wallet.BalanceChanged, realm.BrokeThrough, realm.LockedChanged 를 구독하고 OnDestroy 에서 끊는다.
        // 인자 중 null 이 있으면 ArgumentNullException. 두 번 부르면 InvalidOperationException.
        public void Initialize(
            PlumSword.Core.Realm.RealmProgress realm,
            PlumSword.Core.Currency.Wallet wallet,
            UnityEngine.Canvas canvas);
    }

    public sealed class RealmAuraView : MonoBehaviour
    {
        // 제자 발밑을 따라다니는 오오라를 만든다. realm.BrokeThrough 를 구독하고 OnDestroy 에서 끊는다.
        // 인자 중 null 이 있으면 ArgumentNullException. 두 번 부르면 InvalidOperationException.
        public void Initialize(
            PlumSword.Core.Realm.RealmProgress realm,
            PlumSword.Core.Combat.Battle battle);
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot.Awake`가 `Wallet`을 만든 직후 `RealmProgress`를 하나 만들어 `GameRoot.Realm`으로 들고 있는다. 게임에 `RealmProgress`는 하나뿐이다. 저장 기능이 경지를 저장하게 되기 전에는 `new RealmProgress(Wallet)`(삼류)로 만든다. 그 뒤에는 `new RealmProgress(Wallet, 저장된 realmLevel)`로 만든다.
- 능력치 강화 기능이 쓰는 방법 (능력치 강화 사양을 고칠 때 반영): `RealmProgress`를 넘겨받아 `Attack`, `MaxHealth`를 계산할 때마다 `BonusPercent`를 읽어 `(기본값 + 증가량) × (100 + BonusPercent) / 100`(`long` 정수 나눗셈)을 돌려준다.
- 전투 기능이 쓰는 방법 (전투 사양을 고칠 때 반영): `BrokeThrough`를 받으면 체력 강화 때와 같이 `늘어난 양 = StatUpgrades.MaxHealth − 기억해 둔 직전 최대 체력`을 구해 기억을 바꾸고, 패배 상태가 아니면 현재 체력을 그만큼 늘린다. `BrokeThrough`를 받는 시점에 `Current`는 이미 새 경지라 `StatUpgrades.MaxHealth`는 새 값이다.
- 저장 기능이 쓰는 방법 (저장 사양을 고칠 때 반영): 저장할 때 `(int)Current`를 읽고, 불러올 때 생성자의 `startLevel`에 넣는다. 돌파는 언제나 금화를 써서 `BalanceChanged`가 가므로, 지금의 자동 저장 규칙(그 프레임 끝에 저장)만으로 돌파 직후의 경지가 저장된다. `BrokeThrough`를 따로 구독하지 않아도 된다.

### 필요한 것

- `PlumSword.Core.Currency.Wallet` (`docs/spec/currency.md`): `CanSpend(long)`, `TrySpend(long)`, `BalanceChanged`.
- `PlumSword.Core.Currency.GoldText.FormatNumber(long)` (`docs/spec/currency.md`).
- `PlumSword.Core.StatUpgrade.StatUpgrades.LockedChanged` (`docs/spec/stat_upgrade.md`): `GameRoot`가 잠금을 맞출 때 쓴다.
- `PlumSword.Core.Combat.Battle.DisciplePosition` (`docs/spec/combat.md`): 오오라 위치.
- `PlumSword.Game.GameRoot.Wallet`, `UiCanvas`, `StatUpgrades`, `Battle`.
- `PlumSword.Game.StatUpgrade.StatUpgradeButtonEffect` (이미 있는 화면 코드, `docs/spec/stat_upgrade.md` "화면"의 강화 연출): `Initialize(Button, RectTransform, Image, Image, TextMeshProUGUI)`, `SetEnabledAppearance(bool)`, `PlaySuccess()`를 그대로 쓴다. 이 클래스는 고치지 않는다.

## 화면

- **조립**: `GameRoot.Awake`에서 `[StatUpgradeView]`를 만든 다음에 `[Game]`의 자식 `[RealmView]`를 만들어 `RealmView`를 붙이고 `Initialize(Realm, Wallet, UiCanvas)`를 부른다. `[CombatView]`를 만든 다음에 `[Game]`의 자식 `[RealmAuraView]`를 만들어 `RealmAuraView`를 붙이고 `Initialize(Realm, Battle)`를 부른다.
- 모든 그림은 `Resources.Load`로 읽는다. 없으면 아래 대체 표시를 쓰고 게임은 그대로 진행된다 (오류 로그를 남기지 않는다. 그림 오오라 시트의 크기가 틀린 경우만 `Debug.LogWarning`).

### 현재 경지 표시 (화면 왼쪽 위)

- 캔버스 왼쪽 위에 앵커와 피벗 (0, 1), 위치 (8, −8).
- 그림 `Art/UI/Realm/name_<AssetId(Current)>`(`Sprite`)가 있으면 `Image`로 보여 준다. 높이 24, 가로는 그림 비율대로(최대 160), `preserveAspect = true`, `raycastTarget = false`.
- 없으면 `TextMeshProUGUI` 한 줄: `RealmText.Label(Current)` (예: `REALM: SECOND RATE`), 크기 200×16, 글자 크기 14, 왼쪽 정렬, 색 RGB (1.0, 0.95, 0.80), TMP 기본 글꼴, `raycastTarget = false` (모두 임시).
- 그림은 경지마다 따로 확인한다. 이류 그림만 있으면 이류일 때만 그림이고 나머지는 글자다.

### 돌파 버튼 (화면 아래쪽 가운데, HP 버튼 위)

- 캔버스 왼쪽 아래에 앵커와 피벗 (0, 0), 위치 (220, 62), 크기 200×48 (임시).
- **그림 모드**: 바탕 `Art/UI/Realm/button`이 있으면 그것을, 없으면 강화 버튼 바탕 `Art/UI/StatUpgrade/button`을 쓴다. 바탕과 금화 아이콘 `Art/UI/StatUpgrade/coin`이 모두 있을 때만 그림 모드다. 배치는 강화 버튼과 같다:
  - 아이콘 `Art/UI/Realm/icon` (9, 8) 32×32. 없으면 아이콘 자리는 비운다 (`Image` 끔).
  - 첫 줄 `RealmText.ButtonTitle` (48, 23) 82×18, 글자 크기 15. 자동 크기 9~15.
  - 둘째 줄 `RealmText.Name(Current)` (48, 8) 82×15, 글자 크기 11. 자동 크기 8~11.
  - 금화 아이콘 (145, 15) 17×17과 비용 숫자 `GoldText.FormatNumber(NextCost)` (164, 11) 31×25, 글자 크기 12, 자동 크기 8~12. 초절정이면 금화 아이콘을 숨기고 `MAX`를 쓴다.
  - 호버·누름·성공 연출은 `StatUpgradeButtonEffect`를 붙여 강화 버튼과 똑같이 한다 (`SetEnabledAppearance(CanBreakthrough())`, 성공 때 `PlaySuccess()`). 둘째 줄 글자가 성공 때 금빛으로 변하는 글자다.
- **대체 모드** (그림이 없을 때): `Image`(단색) + `Button`, `transition = None`. 자식 `TextMeshProUGUI` 하나에 `RealmText.ButtonLabel(Current, NextCost, IsMax)` (예: `BREAKTHROUGH` / `SECOND RATE` / `G 500`), 버튼을 꽉 채우고 가운데 정렬, 글자 크기 14, `raycastTarget = false`. 누를 수 있을 때 RGB (0.55, 0.25, 0.35)에 흰 글자, 누를 수 없을 때 RGB (0.25, 0.25, 0.25)에 RGB (0.6, 0.6, 0.6) 글자 (모두 임시). 대체 모드에는 호버·성공 연출이 없다.
- **누를 수 있을 때**: `CanBreakthrough()`가 `true`일 때만 `Button.interactable = true`. 금화 부족·초절정·잠김을 모양으로 구분하지 않는다. 초절정은 `MAX` 글자로 알 수 있다.
- **누르면**: `TryBreakthrough()`를 부른다. 성공하면 위의 성공 연출을 한다. 한 번 누르면 한 단계다.
- **바뀌는 때**: `Initialize`에서 한 번, 그 뒤로 `wallet.BalanceChanged`, `realm.BrokeThrough`, `realm.LockedChanged`가 올 때마다 버튼과 경지 표시를 갱신한다.

### 오오라 (제자 주변, 월드 공간)

- 위치: 매 프레임 `LateUpdate`에서 발 위치 `(battle.DisciplePosition − 2.25, −1.5)`를 따라간다. 이 값은 전투 사양 "화면"의 제자 표시 위치와 같다. 전투 쪽 값이 바뀌면 함께 고친다. 오오라의 가운데는 발 위 1.25 유닛이다.
- 삼류(`HasAura`가 `false`)에서는 아무것도 보이지 않는다.
- **그림 모드**: `Resources.Load<Texture2D>("Art/Effects/aura_<AssetId(Current)>")`가 있으면, 가로 한 줄 시트를 정사각형 칸(칸 한 변 = 시트 높이, 칸 수 = 가로 ÷ 높이)으로 잘라 10 fps(임시)로 반복 재생한다. 피벗은 칸 가운데, PPU 32, 화면에서 1.5배 (제자와 같다). 색은 흰색(그림 그대로)이다. 가로가 높이로 나누어떨어지지 않으면 경고 로그를 남기고 대체 모드를 쓴다.
- **대체 모드**: 실행 중에 만든 64×64 원형 그러데이션 텍스처(가운데 불투명, 바깥으로 갈수록 투명. 알파 = `(1 − r)²`, r은 가운데에서 잰 거리 ÷ 32, 1 넘으면 0)를 가로 2.4 × 세로 3.0 유닛으로 늘려 그린다 (임시). 색은 `RealmAura.Color(Current)`의 RGB, 알파는 매 프레임 `RealmAura.PulseAlpha(Current, Time.time)`이다.
- 그리는 순서(`sortingOrder`) 9990. 제자(10000) 뒤에 그린다.
- **돌파 연출**: `BrokeThrough`를 받으면 새 경지의 오오라로 바꾸고, 그때부터 흐른 시간 `t`로 크기에 `RealmAura.BurstScale(t)`를 곱한다 (0.4초 동안 1.5배 → 1배). 그림·대체 모드 모두 같다. 연출 중에 또 돌파하면 처음부터 센다.
- 스테이지 전환 페이드, 패배, 구간 소개 중에도 오오라는 그대로 보인다.

- 표시 글자, 누를 수 있는지, 오오라 색·깜빡임·연출 크기 판단은 모두 Core(`RealmText`, `RealmProgress.CanBreakthrough`, `RealmAura`)에 있고 에딧 모드 테스트로 판정한다. `RealmView`, `RealmAuraView`, `GameRoot` 조립은 사람이 실행해 확인한다.

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| 현재 경지 `realmLevel` (`(int)RealmProgress.Current`) | `int` | 0 (삼류) | 0~4 |

- 실제 저장 데이터(`SaveData`)·파일 형식(`SaveCodec`)에 넣는 것은 저장 기능이 한다. 그전까지 경지는 게임을 끄면 삼류로 돌아간다.
- 되살리기: `new RealmProgress(wallet, 저장된 realmLevel)`. 음수는 0, 4 초과는 4로 시작한다. 잠금(`Locked`)은 저장하지 않으며 항상 풀린 상태로 시작한다. 비용과 보너스 비율은 경지에서 계산되므로 저장하지 않는다.
- 2단계 저장 데이터와의 호환은 없다. 3단계는 저장 데이터를 지우고 새로 시작한다.
- 오프라인 보상: 꺼져 있는 동안 경지는 바뀌지 않는다. 오프라인 보상 금액에도 영향을 주지 않는다.

## 범위 밖

- 보너스를 실제 공격력·최대 체력에 적용하는 계산: 능력치 강화 기능이 한다. 그 사양을 고치기 전에는 돌파해도 능력치가 바뀌지 않는다.
- 돌파 때 현재 체력을 늘리는 처리: 전투 기능이 한다. 그 사양을 고치기 전에는 현재 체력이 바뀌지 않는다.
- 경지를 저장 데이터·파일에 넣고 불러오는 것, 저장 판 번호: 저장 기능이 한다.
- 공격 속도 강화 제거, 내공 강화, 이십사수매화검법: 3단계의 다른 기능이 한다. 스킬은 경지 보너스가 적용된 `StatUpgrades.Attack`을 쓴다.
- 실제 오오라·버튼·경지 이름 그림 제작: 사용자가 만든다. 그림을 넣으면 이 사양의 경로대로 바뀐다. 오오라를 Unity 파티클 같은 이펙트로 만드는 것은 하지 않는다.
- 한글 표기(`경지: 이류`, `최대`, `500금화`)와 한글 글꼴: 글꼴이 준비되면 `RealmText`의 글자만 바꾼다.
- 돌파 소리, 큰 안내 패널, 경지 되돌리기·환불, 여러 단계 한꺼번에 돌파, 스테이지 조건: 하지 않는다.
- 밸런스 조정: 4단계에서 한다.

## 완료 조건 초안

아래에서 따로 적지 않으면 `new RealmProgress(wallet)`으로 만든 것이다. 실수는 오차 1e-9 안에서 비교한다.

경지 로직 (`RealmProgressTests`)

1. 새로 만들면 `Current`는 `ThirdRate`, `IsMax`는 `false`, `BonusPercent`는 0, `NextCost`는 200, `Locked`는 `false`다.
2. 금화 3700에서 `TryBreakthrough`를 4번 부르면 모두 `true`이고, 부르기 전의 `NextCost`는 순서대로 200, 500, 1000, 2000이며, 끝난 뒤 `Current`는 `Transcendent`, `IsMax`는 `true`, `BonusPercent`는 40, `NextCost`는 0, 금화는 0이다.
3. 금화 1000에서 `TryBreakthrough`를 1번 부르면 `Current`는 `SecondRate`, `BonusPercent`는 10, `NextCost`는 500, 금화는 800이다.
4. 금화 199에서 `TryBreakthrough`는 `false`이고 `Current`는 `ThirdRate`, 금화는 199이며 `BrokeThrough`와 `BalanceChanged`는 0번 온다.
5. 금화 200에서 `TryBreakthrough`는 `true`이고 `Current`는 `SecondRate`, 금화는 0이다.
6. 금화 1000에서 `TryBreakthrough`를 3번 부르면 결과는 순서대로 `true`, `true`, `false`이고 `Current`는 `FirstRate`, 금화는 300이다.
7. `new RealmProgress(wallet, 4)`에 금화 10000이 있어도 `TryBreakthrough`는 `false`, `CanBreakthrough`는 `false`이고 금화는 10000, `Current`는 `Transcendent`이며 `BrokeThrough`와 `BalanceChanged`는 0번 온다.
8. 금화 1000에서 `TryBreakthrough`를 부르면 `BrokeThrough`가 `SecondRate`를 담아 1번 오고, 받는 쪽에서 읽은 `Current`는 `SecondRate`, `BonusPercent`는 10, `NextCost`는 500이다.
9. 금화 1000에서 `TryBreakthrough`를 부르면 `Wallet.BalanceChanged`가 `BrokeThrough`보다 먼저 오고, `BalanceChanged`를 받는 시점에 읽은 `Current`는 `ThirdRate`다.
10. 금화 200에서 `CanBreakthrough`는 `true`, 금화 199에서는 `false`다. 부른 뒤 금화와 경지는 그대로이고 알림은 0번 온다.
11. `SetLocked(true)`를 부르면 `Locked`는 `true`이고 `LockedChanged`가 `true`를 담아 1번 온다. 한 번 더 `SetLocked(true)`를 부르면 `LockedChanged`는 더 오지 않는다.
12. 금화 1000에서 `SetLocked(true)` 뒤 `CanBreakthrough`는 `false`, `TryBreakthrough`는 `false`이고 금화는 1000, `Current`는 `ThirdRate`이며 `BrokeThrough`는 0번 온다.
13. 12번 상태에서 `SetLocked(false)`를 부르면 `LockedChanged`가 `false`를 담아 1번 오고, `TryBreakthrough`는 `true`다.
14. 새로 만든 직후 `SetLocked(false)`를 부르면 `LockedChanged`는 0번 온다.
15. `new RealmProgress(wallet, 2)`의 `Current`는 `FirstRate`, `BonusPercent`는 20, `NextCost`는 1000이고, 만드는 동안 금화는 바뀌지 않고 알림은 0번 온다.
16. `new RealmProgress(wallet, -1)`의 `Current`는 `ThirdRate`, `new RealmProgress(wallet, 7)`의 `Current`는 `Transcendent`이고 둘 다 예외가 없다.
17. `new RealmProgress(null)`은 `ArgumentNullException`을 던진다.

규칙 (`RealmRulesTests`)

18. `RealmRules.BonusPercent`는 `ThirdRate`부터 순서대로 0, 10, 20, 30, 40을 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다.
19. `RealmRules.BreakthroughCost`는 `ThirdRate`부터 순서대로 200, 500, 1000, 2000, 0을 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다. `RealmRules.LevelCount`는 5다.

표시 글자 (`RealmTextTests`)

20. `RealmText.Name`은 `ThirdRate`부터 순서대로 `"THIRD RATE"`, `"SECOND RATE"`, `"FIRST RATE"`, `"PEAK"`, `"TRANSCENDENT"`를 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다.
21. `RealmText.Label(SecondRate)`는 `"REALM: SECOND RATE"`, `RealmText.Label(ThirdRate)`는 `"REALM: THIRD RATE"`이고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다.
22. `RealmText.AssetId`는 `ThirdRate`부터 순서대로 `"third_rate"`, `"second_rate"`, `"first_rate"`, `"peak"`, `"transcendent"`를 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다.
23. `RealmText.Cost`는 (500, `false`) → `"G 500"`, (1000, `false`) → `"G 1,000"`, (0, `true`) → `"MAX"`, (2000, `true`) → `"MAX"`, (-5, `false`) → `"G -5"`를 돌려준다.
24. `RealmText.ButtonLabel`은 (`SecondRate`, 500, `false`) → `"BREAKTHROUGH\nSECOND RATE\nG 500"`, (`Transcendent`, 0, `true`) → `"BREAKTHROUGH\nTRANSCENDENT\nMAX"`를 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다. `RealmText.ButtonTitle`은 `"BREAKTHROUGH"`다.

오오라 규칙 (`RealmAuraTests`)

25. `RealmAura.HasAura`는 `ThirdRate`에 `false`, 나머지 넷에 `true`를 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다.
26. `RealmAura.Color`는 `SecondRate` → (0.55, 0.85, 1.00, 0.35), `FirstRate` → (0.35, 0.55, 1.00, 0.40), `Peak` → (0.70, 0.40, 1.00, 0.45), `Transcendent` → (1.00, 0.55, 0.75, 0.50), `ThirdRate` → (0, 0, 0, 0)을 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다.
27. `RealmAura.PulseAlpha(SecondRate, t)`는 t = 0 → 0.2625, 0.4 → 0.35, 1.2 → 0.175, 1.6 → 0.2625이고, `PulseAlpha(ThirdRate, 0.4)`는 0이다.
28. `RealmAura.PulseAlpha(SecondRate, double.NaN)`과 `PulseAlpha(SecondRate, double.PositiveInfinity)`는 `ArgumentOutOfRangeException`을, `PulseAlpha((RealmLevel)99, 0)`도 `ArgumentOutOfRangeException`을 던진다.
29. `RealmAura.BurstScale`은 0 → 1.5, 0.2 → 1.25, 0.4 → 1.0, 1.0 → 1.0, -1 → 1.5, `double.PositiveInfinity` → 1.0을 돌려주고, `double.NaN`은 `ArgumentOutOfRangeException`을 던진다.

화면 (자동 테스트 없음. 사람이 실행해 확인)

30. 게임을 실행하면 화면 왼쪽 위에 `REALM: THIRD RATE`가 보이고, 아래쪽 HP 버튼 위에 돌파 버튼이 `BREAKTHROUGH` / `THIRD RATE` / 금화 아이콘 옆 `200`으로 보인다 (그림이 없으면 `G 200`). 금화가 200보다 적으면 어둡고 눌리지 않는다. 제자 주변에 오오라가 없다.
31. 금화 200 이상에서 돌파 버튼을 누르면 금화가 200 줄고, 왼쪽 위가 `REALM: SECOND RATE`, 버튼이 `SECOND RATE` / `500`으로 바뀌고, 제자 주변에 하늘색 오오라가 0.4초 동안 커졌다가 돌아온 뒤 은은하게 깜빡인다. 그림 모드면 강화 버튼과 같은 성공 연출이 나온다.
32. 초절정까지 돌파하면 버튼 비용 자리에 `MAX`가 보이고 눌리지 않으며, 오오라는 매화 분홍이다. 스테이지 전환 페이드 동안에는 돌파 버튼이 강화 버튼과 함께 어두워진다.
33. 기존 테스트 `CoreAssemblyTests.Core_DoesNotReferenceUnity`가 계속 통과한다.

## 결정 기록

결정 기록은 [records/realm.md](records/realm.md)의 "결정 기록"에 있다. 구현에 필요한 규칙은 위 절들에 모두 있다.
