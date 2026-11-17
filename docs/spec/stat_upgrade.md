# 능력치 강화 사양

- 기능 이름: stat_upgrade (네임스페이스 `PlumSword.Core.StatUpgrade`)
- 작성일: 2026-11-16

## 목표

금화를 써서 제자의 공격력·체력·공격 속도를 한 단계씩 올리는 기능이다. 이 기능은 세 능력치의 강화 단계와 비용, 구매(금화 소비), 강화가 반영된 최종 능력치(공격력, 최대 체력, 공격 간격) 계산, 강화 버튼 세 개의 화면까지 맡는다. 현재 체력과 공격 대기 시간은 맡지 않는다 (전투 기능의 것).

현재 게임에는 `SPD` 공격 속도 강화가 남아 있다. 사용자가 나중에 제거할 예정임을 확인했으며, 제거 작업을 하기 전까지 이 문서와 게임은 현재 세 버튼을 유지한다.

출처: `docs/design/milestone_1.md` "1단계: 능력치 강화", "공격 타이밍과 동시 처치", "UI와 전환", "기능 간 연결과 작업 분할 시 확인할 점". 저장 대비는 `docs/design/milestone_2.md` "저장과 불러오기".

## 동작

- 능력치는 세 가지다: 공격력(`StatType.Attack`), 체력(`StatType.Health`), 공격 속도(`StatType.AttackSpeed`). 각각 강화 단계를 따로 가진다. 처음에는 모두 0이다.
- 한 능력치를 강화해도 다른 능력치의 단계와 비용은 바뀌지 않는다.
- **강화** `TryUpgrade(stat)`: 아래 순서로 처리한다.
  1. 잠겨 있으면(`Locked`) `false`.
  2. 그 능력치가 최대 단계면 `false`.
  3. `wallet.TrySpend(GetCost(stat))`가 `false`면(금화 부족) `false`.
  4. 그 능력치의 단계를 1 올린다.
  5. `Upgraded`를 그 능력치를 담아 한 번 보내고 `true`를 돌려준다.
- `false`를 돌려준 경우에는 금화도 단계도 바뀌지 않고 알림도 없다.
- 알림 순서: 강화 한 번에 `Wallet.BalanceChanged`(3번에서) → 단계 증가 → `Upgraded` 순서다. `BalanceChanged`를 받는 시점에는 단계가 아직 올라가기 전이다. `Upgraded`를 받는 쪽에서 `GetLevel`, `GetCost`, `Attack`, `MaxHealth`, `AttackInterval`을 읽으면 새 값이다.
- **확인** `CanUpgrade(stat)`: 잠겨 있지 않고, 최대 단계가 아니고, `wallet.CanSpend(GetCost(stat))`가 `true`일 때만 `true`. 아무것도 바꾸지 않는다. 강화 버튼을 누를 수 있는지는 이 값 그대로다.
- **최종 능력치**: `Attack`, `MaxHealth`, `AttackInterval`은 부를 때마다 "시작값과 지금 단계"로 계산한 값을 돌려준다 (공식은 "수치"). 강화가 성공하면 그 즉시 새 값이 된다.
  - 공격력: 전투는 피해를 줄 때마다 `Attack`을 읽는다. 그래서 강화 뒤 다음 피해부터 오른 공격력이 쓰인다.
  - 체력: 이 기능은 최대 체력만 올린다. 현재 체력을 같은 만큼 늘리는 것은 전투 기능이 한다. 전투는 `Upgraded`를 받아 `stat == StatType.Health`이면 강화 전후 `MaxHealth`의 차이만큼 현재 체력을 늘린다 (예: `40/100 → 60/120`).
  - 공격 속도: 전투는 공격을 한 뒤 다음 공격을 기다리기 시작할 때 `AttackInterval`을 읽는다. 이미 기다리는 중인 공격의 남은 시간은 바꾸지 않는다. 이 적용 시점은 전투 기능이 지킨다.
- **잠금** `SetLocked(locked)`: 잠긴 동안에는 `TryUpgrade`와 `CanUpgrade`가 모두 `false`다. 값이 실제로 바뀐 때에만 `LockedChanged`를 새 값을 담아 한 번 보낸다. 처음에는 잠겨 있지 않다. 잠금을 거는 쪽은 스테이지 기능이다 (페이드아웃 시작에 `SetLocked(true)`, 페이드인이 끝나면 `SetLocked(false)`). 스테이지 기능이 생기기 전에는 아무도 잠그지 않는다.
- 스테이지 성공·실패는 강화 단계에 아무 영향을 주지 않는다. 단계를 되돌리는 동작은 없다.
- 같은 시점에 여러 번 부르면 부른 순서대로 한 번씩, 그때의 금화와 단계로 따로 판정한다.
- 화면은 버튼을 누르면 `TryUpgrade`를 부르고, `Wallet.BalanceChanged`, `Upgraded`, `LockedChanged`를 받을 때마다 버튼 세 개를 다시 그린다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| 금화가 비용보다 적을 때 `TryUpgrade` | `false` | 금화·단계 그대로, 알림 없음 |
| 금화가 비용과 같을 때 `TryUpgrade` | `true` | 금화 0, 단계 +1, `BalanceChanged` 1번, `Upgraded` 1번 |
| 공격 속도 10단계에서 `TryUpgrade(AttackSpeed)` | `false` (금화가 충분해도) | 금화·단계 그대로, 알림 없음 |
| 공격력·체력이 `int.MaxValue` 단계에서 `TryUpgrade` | `false` | 금화·단계 그대로, 알림 없음 |
| 잠긴 동안 `TryUpgrade` | `false` | 금화·단계 그대로, 알림 없음 |
| 잠긴 동안 `CanUpgrade` | `false` | 변화 없음 |
| 최대 단계에서 `GetCost` | 공식 그대로의 값 (예외 없음). 예: 공격 속도 10단계 → 70 | 변화 없음. 화면은 이 값 대신 `MAX`를 보여 준다 |
| `SetLocked`를 지금과 같은 값으로 부름 | 아무 일도 없다 | `LockedChanged` 없음 |
| `TryUpgrade`를 연달아 부름 | 부를 때마다 그때의 금화·단계로 따로 판정 | 예: 금화 50에서 `TryUpgrade(Attack)` 세 번 → `true`(20), `true`(25), `false`(30 필요), 금화 5, 단계 2 |
| 정의되지 않은 `StatType` 값 (예: `(StatType)99`) | `ArgumentOutOfRangeException` (`GetLevel`, `GetMaxLevel`, `GetCost`, `IsMaxLevel`, `CanUpgrade`, `TryUpgrade`, `StatUpgradeText.Name`, `StatUpgradeText.ButtonLabel`) | 변화 없음 |
| 생성자의 `wallet`이 `null` | `ArgumentNullException` | |
| 생성자의 `baseAttack` 또는 `baseMaxHealth`가 1보다 작음 | `ArgumentOutOfRangeException` | |
| 생성자의 `baseAttackInterval`이 0.5보다 작거나 `NaN`이거나 무한대 | `ArgumentOutOfRangeException` | |
| 생성자의 단계가 음수 | 0으로 만든다 (예외 없음) | 알림 없음, 금화 그대로 |
| 생성자의 `attackSpeedLevel`이 10보다 큼 | 10으로 만든다 (예외 없음) | 알림 없음, 금화 그대로 |
| `StatUpgradeText.Level(음수)`, `StatUpgradeText.Cost(음수, false)` | 그대로 표기 (예외 없음) | 예: `Lv.-1`, `G -5` |

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 단계의 타입 | `int` | 처음 값 0 |
| 비용·공격력·최대 체력의 타입 | `long` | 정수만 쓴다. 반올림 없음. 계산은 `long`으로 한다 (단계를 `long`으로 바꾼 뒤 곱한다) |
| 공격 간격의 타입 | `double` (초) | 반올림하지 않는다. 테스트는 오차 1e-9 안에서 비교한다 |
| 다음 강화 비용 `GetCost` | `20 + 5 × 단계` (임시) | 세 능력치 모두 같은 식. 단계 0·1·2·3 → 20·25·30·35 |
| 공격력 `Attack` | `baseAttack + 2 × 공격력 단계` (임시) | 시작 10 기준 단계 0·1·2 → 10·12·14 |
| 최대 체력 `MaxHealth` | `baseMaxHealth + 20 × 체력 단계` (임시) | 시작 100 기준 단계 0·1·2 → 100·120·140 |
| 공격 간격 `AttackInterval` | `Math.Max(0.5, baseAttackInterval - 0.05 × 공격 속도 단계)` (임시) | 시작 1.0 기준 단계 0·1·3·10 → 1.0·0.95·0.85·0.5 |
| 최소 공격 간격 | 0.5초 (임시) | `StatUpgrades.MinAttackInterval` |
| 공격 속도 최대 단계 | 10 (임시) | `StatUpgrades.AttackSpeedMaxLevel` |
| 공격력·체력 최대 단계 | `int.MaxValue` (2,147,483,647) | 기획상 상한은 없다. 넘침을 막는 기술적 상한이다. 이때 비용 10,737,418,255, 공격력(시작 10) 4,294,967,304 |
| 시작 능력치 (`GameRoot`가 넣는 값) | 공격력 10, 최대 체력 100, 공격 간격 1.0초 (임시) | 원래 전투 기능의 수치다. 전투 사양이 생기면 그 값으로 바꾼다 |
| 이름 표기 `StatUpgradeText.Name` | `Attack` → `ATK`, `Health` → `HP`, `AttackSpeed` → `SPD` | 1단계는 한글 글꼴이 없어 한글을 쓰지 않는다 |
| 단계 표기 `StatUpgradeText.Level` | `"Lv." + 단계` | 0 → `Lv.0`, 10 → `Lv.10`. `level.ToString(CultureInfo.InvariantCulture)` |
| 비용 표기 `StatUpgradeText.Cost` | 최대 단계면 `MAX`, 아니면 `"G " + GoldText.FormatNumber(cost)` | 20 → `G 20`, 1234 → `G 1,234` |
| 버튼 글자 `StatUpgradeText.ButtonLabel` | `Name + "\n" + Level + "\n" + Cost` | `ATK\nLv.3\nG 35`, `SPD\nLv.10\nMAX` |

몬스터 능력치, 보상 금액, 현재 체력, 공격 대기 시간은 이 기능의 수치가 아니다 ("범위 밖").

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.StatUpgrade
{
    public enum StatType
    {
        Attack = 0,
        Health = 1,
        AttackSpeed = 2,
    }

    public sealed class StatUpgrades
    {
        public const int AttackSpeedMaxLevel = 10;
        public const double MinAttackInterval = 0.5;

        // wallet: 강화 비용을 쓸 지갑. null 이면 ArgumentNullException.
        // baseAttack, baseMaxHealth: 강화 전 시작값. 1 보다 작으면 ArgumentOutOfRangeException.
        // baseAttackInterval: 강화 전 공격 간격(초). 0.5 보다 작거나 NaN 이거나 무한대면 ArgumentOutOfRangeException.
        // 단계 셋: 저장된 값으로 되살릴 때 넣는다. 음수면 0, attackSpeedLevel 이 10 보다 크면 10 으로 만든다 (예외 없음).
        // 생성자는 금화를 쓰지 않고 알림을 보내지 않는다. 잠기지 않은 상태로 시작한다.
        public StatUpgrades(
            PlumSword.Core.Currency.Wallet wallet,
            long baseAttack,
            long baseMaxHealth,
            double baseAttackInterval,
            int attackLevel = 0,
            int healthLevel = 0,
            int attackSpeedLevel = 0);

        // 지금 강화 단계. 0 이상 GetMaxLevel(stat) 이하.
        // 정의되지 않은 stat: ArgumentOutOfRangeException (아래 stat 을 받는 메서드 모두 같다).
        public int GetLevel(StatType stat);

        // 최대 단계. AttackSpeed 는 10, Attack 과 Health 는 int.MaxValue.
        public int GetMaxLevel(StatType stat);

        // GetLevel(stat) >= GetMaxLevel(stat) 이면 true.
        public bool IsMaxLevel(StatType stat);

        // 다음 강화에 드는 금화. 20 + 5 * 단계 (long 으로 계산).
        // 최대 단계여도 같은 식의 값을 돌려준다 (예외 없음).
        public long GetCost(StatType stat);

        // 지금 강화할 수 있으면 true: 잠기지 않았고, 최대 단계가 아니고, 금화가 비용 이상.
        // 아무것도 바꾸지 않는다.
        public bool CanUpgrade(StatType stat);

        // 강화한다. 성공하면 금화를 쓰고 단계를 1 올린 뒤 Upgraded 를 보내고 true.
        // 잠김, 최대 단계, 금화 부족이면 아무것도 바꾸지 않고 false (알림 없음).
        public bool TryUpgrade(StatType stat);

        // 강화가 반영된 최종 능력치. 부를 때마다 지금 단계로 계산한다.
        public long Attack { get; }              // baseAttack + 2 * 공격력 단계
        public long MaxHealth { get; }           // baseMaxHealth + 20 * 체력 단계
        public double AttackInterval { get; }    // Math.Max(0.5, baseAttackInterval - 0.05 * 공격 속도 단계), 초

        // 강화가 성공해 단계가 오른 뒤에 한 번, 오른 능력치를 담아 보낸다. 실패하면 보내지 않는다.
        public event System.Action<StatType> Upgraded;

        // 잠겨 있으면 true. 처음 값 false.
        public bool Locked { get; }

        // 잠그거나 푼다. 값이 실제로 바뀐 때에만 LockedChanged 를 보낸다.
        public void SetLocked(bool locked);

        // 잠금 값이 바뀐 뒤에 한 번, 새 값을 담아 보낸다.
        public event System.Action<bool> LockedChanged;
    }

    public static class StatUpgradeText
    {
        // Attack -> "ATK", Health -> "HP", AttackSpeed -> "SPD". 정의되지 않은 값: ArgumentOutOfRangeException.
        public static string Name(StatType stat);

        // "Lv." + level. 예: 0 -> "Lv.0", 10 -> "Lv.10". 음수도 그대로 (예외 없음).
        public static string Level(int level);

        // isMaxLevel 이 true 면 "MAX" (cost 는 보지 않는다).
        // 아니면 "G " + GoldText.FormatNumber(cost). 예: 20 -> "G 20", 1234 -> "G 1,234".
        public static string Cost(long cost, bool isMaxLevel);

        // Name(stat) + "\n" + Level(level) + "\n" + Cost(cost, isMaxLevel).
        // 예: (Attack, 3, 35, false) -> "ATK\nLv.3\nG 35", (AttackSpeed, 10, 70, true) -> "SPD\nLv.10\nMAX".
        public static string ButtonLabel(StatType stat, int level, long cost, bool isMaxLevel);
    }
}
```

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래 하나를 더한다
    {
        // Awake 에서 Wallet 을 만든 뒤 new StatUpgrades(Wallet, 10, 100, 1.0) 으로 하나 만든다.
        public PlumSword.Core.StatUpgrade.StatUpgrades StatUpgrades { get; }
    }
}

namespace PlumSword.Game.StatUpgrade
{
    public sealed class StatUpgradeView : MonoBehaviour
    {
        // canvas 아래에 강화 버튼 세 개를 만들고 지금 상태로 바로 표시한다.
        // wallet.BalanceChanged, upgrades.Upgraded, upgrades.LockedChanged 를 구독하고, OnDestroy 에서 구독을 끊는다.
        public void Initialize(
            PlumSword.Core.StatUpgrade.StatUpgrades upgrades,
            PlumSword.Core.Currency.Wallet wallet,
            UnityEngine.Canvas canvas);
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot.Awake`가 `Wallet`을 만든 뒤 `StatUpgrades`를 하나 만들어 `GameRoot.StatUpgrades`로 들고 있는다. 게임에 `StatUpgrades`는 하나뿐이다. 전투·스테이지 기능은 `GameRoot`가 조립할 때 이것을 생성자나 초기화 메서드의 인자로 넘겨받는다. 2단계에서 저장 기능이 생기면 생성자의 단계 인자 셋에 저장된 값을 넣는다.
- 전투 기능이 쓰는 방법: 피해를 줄 때 `Attack`, 공격 뒤 다음 대기를 시작할 때 `AttackInterval`, 스테이지 시작 때의 체력으로 `MaxHealth`를 읽는다. `Upgraded`에서 `StatType.Health`를 받으면 강화 전후 `MaxHealth`의 차이만큼 현재 체력을 늘린다 (전투가 직전의 최대 체력을 기억해 둔다).
- 스테이지 기능이 쓰는 방법: 페이드아웃을 시작할 때 `SetLocked(true)`, 페이드인이 끝나면 `SetLocked(false)`.

### 필요한 것

- `PlumSword.Core.Currency.Wallet` (`docs/spec/currency.md`): `CanSpend(long)`, `TrySpend(long)`, `BalanceChanged`.
- `PlumSword.Core.Currency.GoldText.FormatNumber(long)` (`docs/spec/currency.md`).
- `PlumSword.Game.GameRoot.Wallet`, `PlumSword.Game.GameRoot.UiCanvas` (`docs/spec/currency.md`). 금화 기능이 먼저 구현되어 있어야 한다.
- 시작 능력치(공격력 10, 최대 체력 100, 공격 간격 1.0초)는 전투 기능의 수치지만 전투 사양이 아직 없다. 그때까지 `GameRoot`가 이 숫자를 직접 넣는다.

## 화면

### 선택한 강화 버튼 그림 (2026-11-18)

그림이 있으면 공통 반투명 무협풍 바탕과 공격·체력·공격 속도 아이콘, 금화 아이콘을 사용한다. 각 버튼은 200×48이며 기존 가로 위치와 구매 가능 조건을 유지한다. 이름, 단계, 비용은 각각 갱신한다. 비용은 금화 아이콘 옆 숫자로 표시하고 최대 단계에는 아이콘 없이 `MAX`를 보인다. 보유 금화 현판은 ATK 위에 둔다 ([금화 사양](currency.md)). 그림이 없으면 단색과 기존 세 줄 글자를 쓴다. 아래의 단색·글자 배치는 그림이 없을 때의 대체 설명이다.

그림 버튼에 마우스를 올리면 밝아지고 1.025배 커지며, 누르는 동안 0.96배가 된다. 구매 성공 시 아이콘이 잠깐 위로 움직이고 레벨 글자가 금빛으로 변하며 기존 시작 화면 꽃잎 두 장이 0.55초 동안 날아가 사라진다. 반복 구매 시 새 연출을 시작한다. 금화 부족·최대 단계·잠김 상태에는 호버와 성공 연출을 하지 않는다. 소리는 추가하지 않는다.

- **조립**: `GameRoot.Awake`에서 `[CurrencyView]`를 만든 다음에 `[Game]`의 자식 `[StatUpgradeView]`를 만들어 `StatUpgradeView`를 붙이고 `Initialize(StatUpgrades, Wallet, UiCanvas)`를 부른다.
- **보여 주는 것**: 강화 버튼 세 개. 왼쪽부터 `ATK`, `HP`, `SPD` 순서로 가로 한 줄이다. 버튼마다 글자 세 줄(이름, 단계, 비용)을 보여 준다. 글자는 `StatUpgradeText.ButtonLabel(stat, upgrades.GetLevel(stat), upgrades.GetCost(stat), upgrades.IsMaxLevel(stat))` 그대로다 (예: `ATK` / `Lv.3` / `G 35`, `SPD` / `Lv.10` / `MAX`). 화면 코드에서 글자를 따로 만들거나 고치지 않는다.
- **위치**: 캔버스 왼쪽 아래에 앵커와 피벗 (0, 0)을 두고 버튼 크기 200×48, 위치는 `ATK` (8, 8), `HP` (220, 8), `SPD` (432, 8)이다.
- **모양**: 버튼은 `Image`(단색 사각형)와 `Button`을 붙인 오브젝트다. `Button.transition`은 `None`으로 두고 색은 화면 코드가 직접 바꾼다. 글자는 버튼의 자식 `TextMeshProUGUI` 하나로, 버튼을 꽉 채우고 가운데 정렬, 글자 크기 16 (임시), TMP 기본 글꼴, `raycastTarget = false`.
- **누를 수 있을 때** (`upgrades.CanUpgrade(stat)`가 `true`): `Button.interactable = true`, 버튼 색 RGB (0.20, 0.45, 0.25), 글자 흰색 (임시).
- **누를 수 없을 때** (`CanUpgrade`가 `false`: 금화 부족, 최대 단계, 잠김): `Button.interactable = false`, 버튼 색 RGB (0.25, 0.25, 0.25), 글자 색 RGB (0.6, 0.6, 0.6) (임시). 세 경우를 모양으로 구분하지 않는다. 최대 단계는 글자 `MAX`로 알 수 있다.
- **누르면**: 그 버튼의 능력치로 `upgrades.TryUpgrade(stat)`를 부른다. 성공했을 때 위의 강화 연출을 한다. 한 번 누르면 한 단계다. 길게 눌러 연속 강화는 없다.
- **바뀌는 때**: `Initialize`에서 한 번, 그 뒤로 `wallet.BalanceChanged`, `upgrades.Upgraded`, `upgrades.LockedChanged`가 올 때마다 버튼 내용을 갱신한다. 호버·강화 연출은 매 프레임 갱신하며 소리는 없다.
- **사람이 먼저 해 둘 것**: 금화 사양과 같다. TextMeshPro 기본 리소스(TMP Essentials)가 있어야 글자가 보인다. 없어도 예외로 게임이 멈추지 않아야 한다.
- 표시 글자와 누를 수 있는지의 판단은 모두 Core(`StatUpgradeText`, `StatUpgrades.CanUpgrade`)에 있고 에딧 모드 테스트로 판정한다. `StatUpgradeView`와 `GameRoot`의 조립은 자동 테스트 대상이 아니며 사람이 실행해 확인한다.

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| 공격력 강화 단계 (`GetLevel(StatType.Attack)`) | `int` | 0 | 1단계에서는 저장하지 않는다. 게임을 끄면 0으로 돌아간다 |
| 체력 강화 단계 (`GetLevel(StatType.Health)`) | `int` | 0 | 위와 같다 |
| 공격 속도 강화 단계 (`GetLevel(StatType.AttackSpeed)`) | `int` | 0 | 위와 같다 |

- 저장과 불러오기는 2단계의 저장 기능이 만든다. 이 기능은 그때 필요한 것만 미리 제공한다.
  - 저장할 값: 위 단계 셋. 최종 능력치와 비용은 단계에서 계산되므로 저장하지 않는다. 잠금(`Locked`)도 저장하지 않으며 항상 풀린 상태로 시작한다.
  - 되살리기: `new StatUpgrades(wallet, baseAttack, baseMaxHealth, baseAttackInterval, 저장된 공격력 단계, 저장된 체력 단계, 저장된 공격 속도 단계)`. 음수는 0, 공격 속도 10 초과는 10으로 시작한다. 저장 전체의 손상 처리는 저장 기능이 정한다.
  - 저장할 때: 저장 기능이 `Upgraded`를 구독해 강화될 때 저장한다.
- 오프라인 보상: 꺼져 있는 동안 강화 단계는 바뀌지 않는다. 오프라인 보상 금액에도 영향을 주지 않는다.

## 범위 밖

- 제자의 현재 체력, 체력 강화 때 현재 체력을 늘리는 처리, 공격 대기 시간과 공격 속도 강화의 적용 시점: 전투 기능이 정한다. 이 기능의 `Attack`, `MaxHealth`, `AttackInterval`, `Upgraded`를 쓴다.
- 시작 능력치(10 / 100 / 1초)의 원래 값, 몬스터·보스 능력치: 전투 기능이 정한다.
- 페이드 때 실제로 잠금을 거는 것: 스테이지 기능이 정한다. `SetLocked`를 쓴다.
- 저장·불러오기: 2단계 저장 기능이 정한다.
- 경지 돌파 보너스(공격력·최대 체력에 +10% 등), 공격 속도 강화를 없애고 내공 강화로 바꾸는 것: 3단계에서 한다. 그때 이 사양을 고친다.
- 한글 표기(`공격력`, `최대`, `35금화`)와 한글 글꼴: 1단계에서는 하지 않는다. 글꼴이 준비되면 `StatUpgradeText`의 글자만 바꾼다.
- 버튼에 현재 능력치 값을 보여 주는 것(예: `ATK 16`), 강화 소리, 길게 눌러 연속 강화, 여러 단계 한꺼번에 강화, 강화 되돌리기·환불: 하지 않는다.
- 시작 능력치가 `long` 범위를 넘칠 만큼 큰 경우: 다루지 않는다.

## 완료 조건 초안

아래에서 따로 적지 않으면 `new StatUpgrades(wallet, 10, 100, 1.0)`으로 만든 것이다. 공격 간격은 오차 1e-9 안에서 비교한다.

강화 로직 (`StatUpgradesTests`)

1. 새로 만들면 세 능력치의 `GetLevel`은 0, `GetCost`는 20, `IsMaxLevel`은 `false`이고, `Attack`은 10, `MaxHealth`는 100, `AttackInterval`은 1.0, `Locked`는 `false`다.
2. `GetMaxLevel`은 `AttackSpeed` → 10, `Attack` → `int.MaxValue`, `Health` → `int.MaxValue`다.
3. 금화 1000에서 `TryUpgrade(Attack)`을 3번 부르면 모두 `true`이고, 부르기 전의 `GetCost(Attack)`은 순서대로 20, 25, 30이며, 끝난 뒤 `GetLevel(Attack)`은 3, `GetCost(Attack)`은 35, `Attack`은 16, 금화는 925다.
4. 금화 1000에서 `TryUpgrade(Attack)`을 1번 부른 뒤 `GetLevel(Health)`와 `GetLevel(AttackSpeed)`는 0, `GetCost(Health)`와 `GetCost(AttackSpeed)`는 20, `MaxHealth`는 100, `AttackInterval`은 1.0이다.
5. 금화 100에서 `TryUpgrade(Health)`를 1번 부르면 `MaxHealth`는 120이고 금화는 80이다. 한 번 더 부르면 `MaxHealth`는 140이고 금화는 55다.
6. 금화 1000에서 `TryUpgrade(AttackSpeed)`를 1번 부르면 `AttackInterval`은 0.95이고 금화는 980이다. 모두 3번 부르면 `AttackInterval`은 0.85다.
7. 금화 1000에서 `TryUpgrade(AttackSpeed)`를 10번 부르면 모두 `true`이고 `GetLevel(AttackSpeed)`는 10, `AttackInterval`은 0.5, `IsMaxLevel(AttackSpeed)`는 `true`, 금화는 575다.
8. 7번 상태에서 `TryUpgrade(AttackSpeed)`를 한 번 더 부르면 `false`이고 단계는 10, 금화는 575 그대로이며 `Upgraded`와 `BalanceChanged`는 0번 온다. `CanUpgrade(AttackSpeed)`는 `false`, `GetCost(AttackSpeed)`는 70이다.
9. 금화 19에서 `TryUpgrade(Attack)`은 `false`이고 `GetLevel(Attack)`은 0, 금화는 19이며 `Upgraded`와 `BalanceChanged`는 0번 온다.
10. 금화 20에서 `TryUpgrade(Attack)`은 `true`이고 `GetLevel(Attack)`은 1, 금화는 0이다.
11. 금화 50에서 `TryUpgrade(Attack)`을 3번 부르면 결과는 순서대로 `true`, `true`, `false`이고 `GetLevel(Attack)`은 2, `Attack`은 14, 금화는 5다.
12. 금화 1000에서 `TryUpgrade(Health)`를 부르면 `Upgraded`가 `StatType.Health`를 담아 1번 오고, 받는 쪽에서 읽은 `GetLevel(Health)`는 1, `MaxHealth`는 120, `GetCost(Health)`는 25다.
13. 금화 1000에서 `TryUpgrade(Attack)`을 부르면 `Wallet.BalanceChanged`가 `Upgraded`보다 먼저 오고, `BalanceChanged`를 받는 시점에 읽은 `GetLevel(Attack)`은 0이다.
14. 금화 20에서 `CanUpgrade`는 세 능력치 모두 `true`다. 금화 19에서는 세 능력치 모두 `false`다. 부른 뒤 금화와 단계는 그대로이고 알림은 0번 온다.
15. `SetLocked(true)`를 부르면 `Locked`는 `true`이고 `LockedChanged`가 `true`를 담아 1번 온다. 한 번 더 `SetLocked(true)`를 부르면 `LockedChanged`는 더 오지 않는다.
16. 금화 1000에서 `SetLocked(true)` 뒤 `CanUpgrade(Attack)`은 `false`, `TryUpgrade(Attack)`은 `false`이고 금화는 1000, 단계는 0이며 `Upgraded`는 0번 온다.
17. 16번 상태에서 `SetLocked(false)`를 부르면 `LockedChanged`가 `false`를 담아 1번 오고, `TryUpgrade(Attack)`은 `true`다.
18. 새로 만든 직후(`Locked`가 `false`) `SetLocked(false)`를 부르면 `LockedChanged`는 0번 온다.
19. `new StatUpgrades(wallet, 10, 100, 1.0, 3, 2, 4)`의 단계는 순서대로 3, 2, 4이고 `Attack`은 16, `MaxHealth`는 140, `AttackInterval`은 0.8, `GetCost`는 순서대로 35, 30, 40이다. 만드는 동안 금화는 바뀌지 않고 알림은 0번 온다.
20. `new StatUpgrades(wallet, 10, 100, 1.0, -1, -5, -2)`의 단계는 모두 0이고 예외가 없다.
21. `new StatUpgrades(wallet, 10, 100, 1.0, 0, 0, 11)`의 `GetLevel(AttackSpeed)`는 10이고 `AttackInterval`은 0.5, `IsMaxLevel(AttackSpeed)`는 `true`이며 예외가 없다.
22. `new StatUpgrades(wallet, 10, 100, 1.0, int.MaxValue, 0, 0)`의 `IsMaxLevel(Attack)`은 `true`, `Attack`은 4294967304, `GetCost(Attack)`은 10737418255이고, 금화가 `long.MaxValue`여도 `TryUpgrade(Attack)`은 `false`이며 금화는 그대로다.
23. `new StatUpgrades(wallet, 50, 300, 2.0, 1, 1, 10)`의 `Attack`은 52, `MaxHealth`는 320, `AttackInterval`은 1.5다.
24. `new StatUpgrades(wallet, 10, 100, 0.7, 0, 0, 2)`의 `AttackInterval`은 0.6이고, 단계를 10으로 만든 `new StatUpgrades(wallet, 10, 100, 0.7, 0, 0, 10)`의 `AttackInterval`은 0.5다.
25. `new StatUpgrades(null, 10, 100, 1.0)`은 `ArgumentNullException`을 던진다.
26. `baseAttack`이 0, `baseMaxHealth`가 0, `baseAttackInterval`이 0.49, `double.NaN`, `double.PositiveInfinity`인 경우 생성자는 각각 `ArgumentOutOfRangeException`을 던진다.
27. `(StatType)99`로 `GetLevel`, `GetMaxLevel`, `GetCost`, `IsMaxLevel`, `CanUpgrade`, `TryUpgrade`를 부르면 각각 `ArgumentOutOfRangeException`을 던지고 금화와 단계는 그대로다.

표시 글자 (`StatUpgradeTextTests`)

28. `StatUpgradeText.Name`은 `Attack` → `"ATK"`, `Health` → `"HP"`, `AttackSpeed` → `"SPD"`를 돌려주고, `(StatType)99`는 `ArgumentOutOfRangeException`을 던진다.
29. `StatUpgradeText.Level`은 0 → `"Lv.0"`, 3 → `"Lv.3"`, 10 → `"Lv.10"`, -1 → `"Lv.-1"`을 돌려준다.
30. `StatUpgradeText.Cost`는 (20, `false`) → `"G 20"`, (1234, `false`) → `"G 1,234"`, (70, `true`) → `"MAX"`, (-5, `false`) → `"G -5"`를 돌려준다.
31. `StatUpgradeText.ButtonLabel`은 (`Attack`, 3, 35, `false`) → `"ATK\nLv.3\nG 35"`, (`Health`, 0, 20, `false`) → `"HP\nLv.0\nG 20"`, (`AttackSpeed`, 10, 70, `true`) → `"SPD\nLv.10\nMAX"`를 돌려주고, `(StatType)99`는 `ArgumentOutOfRangeException`을 던진다.

화면 (자동 테스트 없음. 사람이 실행해 확인)

32. 게임을 실행하면 화면 아래쪽 금화 현판 밑에 버튼 세 개가 왼쪽부터 `ATK`, `HP`, `SPD` 순서로 보이고, 각각 `Lv.0`과 금화 아이콘 옆 `20`이 표시된다. 금화가 0이라 세 버튼 모두 어둡고 눌리지 않는다. 그림이 없으면 `G 20` 단색 대체 표시를 쓴다.
33. 기존 테스트 `CoreAssemblyTests.Core_DoesNotReferenceUnity`가 계속 통과한다.

## 결정 기록

| 질문 | 답 | 방식 |
| --- | --- | --- |
| 최종 능력치(공격력, 최대 체력, 공격 간격)는 어느 기능이 계산하나 | 강화가 시작값을 생성자 인자로 받아 최종값을 계산해 내준다 | 추천 수락 |
| 강화 버튼 화면을 이 사양에 넣나 | 넣는다 (표시 글자 규칙 + 버튼 UI) | 추천 수락 |
| 체력 강화 때 현재 체력 +20은 누가 하나 | 강화는 알림(`Upgraded`)과 새 최대 체력만 내주고, 전투가 현재 체력을 늘린다 | 추천 수락 |
| 페이드 중 잠금을 어떻게 만드나 | 로직에 잠금 스위치 `SetLocked(bool)`. 스테이지 기능이 부른다 | 추천 수락 |
| 강화할 수 없을 때(금화 부족, 최대 단계, 잠김) 강화를 시도하면 | `false`, 금화·단계 그대로, 알림 없음 | 추천 수락 |
| 한글 글꼴이 없을 때 버튼 글자 | 영문 약자: `ATK` / `HP` / `SPD`, `Lv.3`, `G 35`, `MAX` | 추천 수락 |
| 버튼에 현재 능력치 값도 보여 주나 | 기획대로 이름·단계·비용 세 가지만 | 추천 수락 |
| 버튼 배치 | 금화 글자 아래 가로 한 줄 (`ATK`, `HP`, `SPD`). 좌표와 크기는 임시값 | 추천 수락 / 임시값 |
| 잘못된 저장 값(음수 단계, 공격 속도 11 이상) | 범위 안으로 잘라낸다 (0, 10). 예외 없음 | 추천 수락 |
| 숫자 타입 | 단계 `int`, 비용·공격력·최대 체력 `long`, 공격 간격 `double` | 직접 정함 |
| 공격 간격 공식 | `Math.Max(0.5, 시작 간격 - 0.05 × 단계)`, 최대 10단계 | 직접 정함 (기획의 "최소 0.5초, 최대 10번"을 식으로 옮김) |
| 공격력·체력의 기술적 최대 단계 | `int.MaxValue`. 기획상 상한은 없고 넘침을 막는 용도 | 직접 정함 |
| 시작 능력치를 누가 넣나 | 전투 사양이 생길 때까지 `GameRoot`가 10 / 100 / 1.0을 직접 넣는다 | 직접 정함 / 임시값 |
| 알림 모양과 순서 | `Upgraded(StatType)`, `LockedChanged(bool)`. `BalanceChanged` → 단계 증가 → `Upgraded` | 직접 정함 |
| 잘못된 인자(없는 `StatType`, `null` 지갑, 잘못된 시작값) | 예외 (`ArgumentOutOfRangeException`, `ArgumentNullException`) | 직접 정함 |
| 타입·메서드 이름, 버튼 색과 글자 구성 | `StatType`, `StatUpgrades`, `StatUpgradeText`, `StatUpgradeView`. 버튼 글자는 세 줄 한 덩어리 | 직접 정함 |
