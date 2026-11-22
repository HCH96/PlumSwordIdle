# 능력치 강화 사양

- 기능 이름: stat_upgrade (네임스페이스 `PlumSword.Core.StatUpgrade`)
- 작성일: 2026-11-16
- 고친 날: 2026-11-18

## 목표

금화를 써서 제자의 공격력·체력·내공을 한 단계씩 올리는 기능이다. 이 기능은 세 능력치의 강화 단계와 비용, 구매(금화 소비), 강화와 경지 보너스가 반영된 최종 능력치(공격력, 최대 체력) 계산, 강화 버튼 세 개의 화면까지 맡는다. 내공 단계는 전투 기능(이십사수매화검법)이 읽을 수 있게 내준다. 현재 체력, 공격 간격과 공격 대기 시간, 스킬 피해 계산은 맡지 않는다 (전투 기능의 것). 경지 단계와 보너스 비율은 경지 기능의 것이고 이 기능은 읽기만 한다.

3단계 규칙이다. 1·2단계의 공격 속도 강화(`SPD`)는 없앤다. **없애는 일은 루프가 하지 않고 사람이 대화로 main 에서 직접 한다** ("범위 밖"의 "공격 속도 제거 — 사람이 직접 할 일"). 이 문서의 다른 절은 모두 제거가 끝난 뒤의 상태다.

출처: `docs/design/milestone_1.md` "1단계: 능력치 강화", "UI와 전환". `docs/design/milestone_3.md` "목표와 현재 방향", "경지 돌파", "자동 발동 스킬", "저장과 반복 진행", "3단계 화면". 경지 보너스 적용 방법은 `docs/spec/realm.md` "연결 지점".

## 동작

- 능력치는 세 가지다: 공격력(`StatType.Attack`), 체력(`StatType.Health`), 내공(`StatType.InnerPower`). 각각 강화 단계를 따로 가진다. 처음에는 모두 0이다. 공격 속도(`AttackSpeed`)는 없다.
- 한 능력치를 강화해도 다른 능력치의 단계와 비용은 바뀌지 않는다.
- **강화** `TryUpgrade(stat)`: 아래 순서로 처리한다.
  1. 잠겨 있으면(`Locked`) `false`.
  2. 그 능력치가 최대 단계면 `false`.
  3. `wallet.TrySpend(GetCost(stat))`가 `false`면(금화 부족) `false`.
  4. 그 능력치의 단계를 1 올린다.
  5. `Upgraded`를 그 능력치를 담아 한 번 보내고 `true`를 돌려준다.
- `false`를 돌려준 경우에는 금화도 단계도 바뀌지 않고 알림도 없다.
- 알림 순서: 강화 한 번에 `Wallet.BalanceChanged`(3번에서) → 단계 증가 → `Upgraded` 순서다. `BalanceChanged`를 받는 시점에는 단계가 아직 올라가기 전이다. `Upgraded`를 받는 쪽에서 `GetLevel`, `GetCost`, `Attack`, `MaxHealth`를 읽으면 새 값이다.
- **확인** `CanUpgrade(stat)`: 잠겨 있지 않고, 최대 단계가 아니고, `wallet.CanSpend(GetCost(stat))`가 `true`일 때만 `true`. 아무것도 바꾸지 않는다. 강화 버튼을 누를 수 있는지는 이 값 그대로다.
- **최종 능력치**: `Attack`, `MaxHealth`는 부를 때마다 "시작값, 지금 단계, 지금 경지 보너스"로 계산한 값을 돌려준다 (공식은 "수치"). 경지 보너스는 생성자로 받은 `RealmProgress`의 `BonusPercent`를 부를 때마다 읽는다. `realm`이 `null`이면 보너스는 0%다.
  - 강화가 성공하면 그 즉시 새 값이 된다. 돌파가 성공해도 그 즉시 새 값이 된다 (`BrokeThrough`를 받는 시점에 이미 새 값). 이 기능은 돌파 때 아무 알림도 보내지 않고 `realm`을 구독하지도 바꾸지도 않는다.
  - 공격력: 전투는 기본 공격과 스킬 피해를 줄 때마다 `Attack`을 읽는다. 그래서 강화·돌파 뒤 다음 피해부터 오른 공격력이 쓰인다.
  - 체력: 이 기능은 최대 체력만 올린다. 현재 체력을 늘리는 것은 전투 기능이 한다. 전투는 `Upgraded`를 받아 `stat == StatType.Health`이면 강화 전후 `MaxHealth`의 차이만큼 현재 체력을 늘린다. 이 차이는 경지 보너스가 적용된 **최종 최대 체력의 차이**다. 예: 이류(10%)에서 체력 0단계 → 1단계면 `MaxHealth`가 110 → 132이고 현재 체력은 22 늘어 `88/110 → 110/132`가 된다. 지금의 전투 코드(`Battle`)가 이미 이렇게 동작하므로 `MaxHealth`에 보너스만 넣으면 된다.
  - 내공: 공격력·최대 체력에 영향이 없다. 전투(스킬)가 `GetLevel(StatType.InnerPower)`를 읽어 스킬 피해 배율을 계산한다 (배율 공식은 전투 기능의 것). 단계가 오르면 `Upgraded`가 `StatType.InnerPower`를 담아 온다.
- 공격 간격은 이 기능에 없다. 제자의 기본 공격 간격 1초(임시)는 전투 기능의 상수 `Battle.DiscipleBaseAttackInterval`이다.
- **잠금** `SetLocked(locked)`: 잠긴 동안에는 `TryUpgrade`와 `CanUpgrade`가 모두 `false`다. 값이 실제로 바뀐 때에만 `LockedChanged`를 새 값을 담아 한 번 보낸다. 처음에는 잠겨 있지 않다. 잠금을 거는 쪽은 스테이지 기능이다 (페이드아웃 시작에 `SetLocked(true)`, 페이드인이 끝나면 `SetLocked(false)`).
- 스테이지 성공·실패, 천마 처치 뒤 1스테이지로 돌아가는 것은 강화 단계에 아무 영향을 주지 않는다. 단계를 되돌리는 동작은 없다.
- 같은 시점에 여러 번 부르면 부른 순서대로 한 번씩, 그때의 금화와 단계로 따로 판정한다.
- 화면은 버튼을 누르면 `TryUpgrade`를 부르고, `Wallet.BalanceChanged`, `Upgraded`, `LockedChanged`를 받을 때마다 버튼 세 개를 다시 그린다. 버튼은 능력치 값을 보여 주지 않으므로 돌파(`BrokeThrough`) 때 다시 그리지 않아도 된다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| 금화가 비용보다 적을 때 `TryUpgrade` | `false` | 금화·단계 그대로, 알림 없음 |
| 금화가 비용과 같을 때 `TryUpgrade` | `true` | 금화 0, 단계 +1, `BalanceChanged` 1번, `Upgraded` 1번 |
| 어떤 능력치든 `int.MaxValue` 단계에서 `TryUpgrade` | `false` (금화가 충분해도) | 금화·단계 그대로, 알림 없음 |
| 잠긴 동안 `TryUpgrade` | `false` | 금화·단계 그대로, 알림 없음 |
| 잠긴 동안 `CanUpgrade` | `false` | 변화 없음 |
| 최대 단계에서 `GetCost` | 공식 그대로의 값 (예외 없음). 예: `int.MaxValue` 단계 → 10,737,418,255 | 변화 없음. 화면은 이 값 대신 `MAX`를 보여 준다 |
| `SetLocked`를 지금과 같은 값으로 부름 | 아무 일도 없다 | `LockedChanged` 없음 |
| `TryUpgrade`를 연달아 부름 | 부를 때마다 그때의 금화·단계로 따로 판정 | 예: 금화 50에서 `TryUpgrade(Attack)` 세 번 → `true`(20), `true`(25), `false`(30 필요), 금화 5, 단계 2 |
| 정의되지 않은 `StatType` 값 (예: `(StatType)99`) | `ArgumentOutOfRangeException` (`GetLevel`, `GetMaxLevel`, `GetCost`, `IsMaxLevel`, `CanUpgrade`, `TryUpgrade`, `StatUpgradeText.Name`, `StatUpgradeText.ButtonLabel`) | 변화 없음 |
| 생성자의 `wallet`이 `null` | `ArgumentNullException` | |
| 생성자의 `baseAttack` 또는 `baseMaxHealth`가 1보다 작음 | `ArgumentOutOfRangeException` | |
| 생성자의 단계가 음수 | 0으로 만든다 (예외 없음) | 알림 없음, 금화 그대로 |
| 생성자의 `realm`이 `null` | 보너스 0%로 계산한다 (예외 없음) | |
| 아주 큰 단계와 초절정 보너스 | 넘치지 않는다 (`long`으로 계산) | 예: 공격력 `int.MaxValue` 단계, 초절정 → `Attack` 6,012,954,225 |
| `StatUpgradeText.Level(음수)`, `StatUpgradeText.Cost(음수, false)` | 그대로 표기 (예외 없음) | 예: `Lv.-1`, `G -5` |

## 수치

수치는 `docs/spec/balance.md`의 `BalanceTable`을 따른다 (기본값은 아래 값과 같다).

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 단계의 타입 | `int` | 처음 값 0 |
| 비용·공격력·최대 체력의 타입 | `long` | 정수만 쓴다. 계산은 `long`으로 한다 (단계를 `long`으로 바꾼 뒤 곱한다). 경지 보너스는 `long` 정수 나눗셈으로 적용한다 (= 소수점 내림). 그 밖의 반올림은 없다 |
| 다음 강화 비용 `GetCost` | `20 + 5 × 단계` (임시) | 세 능력치 모두 같은 식. 단계 0·1·2·3 → 20·25·30·35 |
| 공격력 `Attack` | `(baseAttack + 2 × 공격력 단계) × (100 + 보너스) / 100` (임시) | 증가량 2는 임시. 아래 예시 표 |
| 최대 체력 `MaxHealth` | `(baseMaxHealth + 20 × 체력 단계) × (100 + 보너스) / 100` (임시) | 증가량 20은 임시. 아래 예시 표 |
| 보너스 | `realm.BonusPercent` (정수 %) | 경지 기능의 수치: 삼류 0, 이류 10, 일류 20, 절정 30, 초절정 40 (모두 임시). `realm`이 `null`이면 0 |
| 내공 | 최종 능력치 없음 | 단계만 내준다. 스킬 배율(200% + 단계당 10%p)은 전투 기능의 수치다 |
| 세 능력치의 최대 단계 | `int.MaxValue` (2,147,483,647) | 기획상 상한은 없다. 넘침을 막는 기술적 상한이다. 이때 비용 10,737,418,255 |
| 시작 능력치 (`GameRoot`가 넣는 값) | 공격력 `Battle.DiscipleBaseAttack` (10), 최대 체력 `Battle.DiscipleBaseMaxHealth` (100) | 전투 기능의 수치다 |
| 이름 표기 `StatUpgradeText.Name` | `Attack` → `무력`, `Health` → `체력`, `InnerPower` → `내공` | 능력치 이름에 Galmuri11 글꼴을 쓴다 |
| 단계 표기 `StatUpgradeText.Level` | `"Lv." + 단계` | 0 → `Lv.0`, 10 → `Lv.10`. `level.ToString(CultureInfo.InvariantCulture)` |
| 비용 표기 `StatUpgradeText.Cost` | 최대 단계면 `MAX`, 아니면 `"G " + GoldText.FormatNumber(cost)` | 20 → `G 20`, 1234 → `G 1,234` |
| 버튼 글자 `StatUpgradeText.ButtonLabel` | `Name + "\n" + Level + "\n" + Cost` | `무력\nLv.3\nG 35`, `내공\nLv.1\nG 25`, `체력\nLv.5\nMAX` |
| 내공 아이콘 리소스 경로 | `Art/UI/StatUpgrade/inner_power` | 파일 `Assets/Resources/Art/UI/StatUpgrade/inner_power.png` ("화면") |

최종 능력치 예시 (시작 공격력 10, 최대 체력 100):

| 경지 (보너스) | 공격력 단계 | 체력 단계 | `Attack` | `MaxHealth` |
| --- | --- | --- | --- | --- |
| 삼류 (0%) 또는 `realm` 없음 | 0 | 0 | 10 | 100 |
| 삼류 (0%) 또는 `realm` 없음 | 1 | 1 | 12 | 120 |
| 이류 (10%) | 0 | 0 | 11 | 110 |
| 이류 (10%) | 1 | 1 | 13 (13.2 내림) | 132 |
| 절정 (30%) | 1 | 0 | 15 (15.6 내림) | 130 |
| 초절정 (40%) | 3 | 2 | 22 (22.4 내림) | 196 |
| 초절정 (40%) | `int.MaxValue` | `int.MaxValue` | 6,012,954,225 | 60,129,542,256 |

공격 간격, 몬스터 능력치, 보상 금액, 현재 체력, 공격 대기 시간, 스킬 범위·배율·재사용 대기, 돌파 조건과 보너스 %는 이 기능의 수치가 아니다 ("범위 밖").

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.StatUpgrade
{
    public enum StatType
    {
        Attack = 0,
        Health = 1,
        InnerPower = 2,   // 내공. 공격 속도(AttackSpeed)는 없다
    }

    public sealed class StatUpgrades
    {
        // wallet: 강화 비용을 쓸 지갑. null 이면 ArgumentNullException.
        // baseAttack, baseMaxHealth: 강화 전 시작값. 1 보다 작으면 ArgumentOutOfRangeException.
        // 단계 셋: 저장된 값으로 되살릴 때 넣는다. 음수면 0 으로 만든다 (예외 없음).
        // realm: 경지 보너스를 읽을 곳. null 이면 보너스 0% (예외 없음). 이 클래스는 realm 을 바꾸거나 구독하지 않는다.
        // 생성자는 금화를 쓰지 않고 알림을 보내지 않는다. 잠기지 않은 상태로 시작한다.
        public StatUpgrades(
            PlumSword.Core.Currency.Wallet wallet,
            long baseAttack,
            long baseMaxHealth,
            int attackLevel = 0,
            int healthLevel = 0,
            int innerPowerLevel = 0,
            PlumSword.Core.Realm.RealmProgress realm = null);

        // 지금 강화 단계. 0 이상 GetMaxLevel(stat) 이하.
        // 정의되지 않은 stat: ArgumentOutOfRangeException (아래 stat 을 받는 메서드 모두 같다).
        // 전투(스킬)는 GetLevel(StatType.InnerPower) 로 내공 단계를 읽는다.
        public int GetLevel(StatType stat);

        // 최대 단계. 세 능력치 모두 int.MaxValue.
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

        // 강화와 경지 보너스가 반영된 최종 능력치. 부를 때마다 지금 단계와 realm.BonusPercent 로 계산한다.
        public long Attack { get; }      // (baseAttack + 2 * 공격력 단계) * (100 + 보너스) / 100
        public long MaxHealth { get; }   // (baseMaxHealth + 20 * 체력 단계) * (100 + 보너스) / 100

        // 강화가 성공해 단계가 오른 뒤에 한 번, 오른 능력치를 담아 보낸다. 실패하면 보내지 않는다.
        // 돌파 때는 보내지 않는다.
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
        // Attack -> "무력", Health -> "체력", InnerPower -> "내공". 정의되지 않은 값: ArgumentOutOfRangeException.
        public static string Name(StatType stat);

        // "Lv." + level. 예: 0 -> "Lv.0", 10 -> "Lv.10". 음수도 그대로 (예외 없음).
        public static string Level(int level);

        // isMaxLevel 이 true 면 "MAX" (cost 는 보지 않는다).
        // 아니면 "G " + GoldText.FormatNumber(cost). 예: 20 -> "G 20", 1234 -> "G 1,234".
        public static string Cost(long cost, bool isMaxLevel);

        // Name(stat) + "\n" + Level(level) + "\n" + Cost(cost, isMaxLevel).
        // 예: (Attack, 3, 35, false) -> "무력\nLv.3\nG 35", (InnerPower, 1, 25, false) -> "내공\nLv.1\nG 25".
        public static string ButtonLabel(StatType stat, int level, long cost, bool isMaxLevel);
    }
}
```

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스
    {
        // Awake 에서 Wallet, Realm 을 만든 뒤 아래로 하나 만든다:
        // new StatUpgrades(Wallet, Battle.DiscipleBaseAttack, Battle.DiscipleBaseMaxHealth,
        //                  저장된 공격력 단계, 저장된 체력 단계, 0, Realm)
        // 내공 단계는 저장 기능이 내공 칸을 만들 때까지 0 을 넣는다 ("저장").
        public PlumSword.Core.StatUpgrade.StatUpgrades StatUpgrades { get; }
    }
}

namespace PlumSword.Game.StatUpgrade
{
    public sealed class StatUpgradeView : MonoBehaviour
    {
        // canvas 아래에 강화 버튼 세 개(무력, 체력, 내공)를 만들고 지금 상태로 바로 표시한다.
        // wallet.BalanceChanged, upgrades.Upgraded, upgrades.LockedChanged 를 구독하고, OnDestroy 에서 구독을 끊는다.
        public void Initialize(
            PlumSword.Core.StatUpgrade.StatUpgrades upgrades,
            PlumSword.Core.Currency.Wallet wallet,
            UnityEngine.Canvas canvas);
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot.Awake`가 `Wallet`과 `Realm`(`RealmProgress`)을 만든 뒤 `StatUpgrades`를 하나 만들어 `GameRoot.StatUpgrades`로 들고 있는다. 게임에 `StatUpgrades`는 하나뿐이다. 전투·스테이지·저장 기능은 `GameRoot`가 조립할 때 이것을 인자로 넘겨받는다.
- 전투 기능이 쓰는 방법: 기본 공격·스킬 피해를 줄 때 `Attack`, 스테이지 시작 때의 체력으로 `MaxHealth`를 읽는다. `Upgraded`에서 `StatType.Health`를 받으면 강화 전후 `MaxHealth`의 차이만큼 현재 체력을 늘린다 (전투가 직전의 최대 체력을 기억해 둔다). 스킬 배율에는 `GetLevel(StatType.InnerPower)`를 읽는다. 공격 간격은 읽지 않는다 (전투의 `Battle.DiscipleBaseAttackInterval`).
- 스테이지 기능이 쓰는 방법: 페이드아웃을 시작할 때 `SetLocked(true)`, 페이드인이 끝나면 `SetLocked(false)`.
- 저장 기능이 쓰는 방법: 저장할 때 `GetLevel`로 세 단계를 읽고, 불러올 때 생성자의 단계 인자 셋에 넣는다 ("저장").

### 필요한 것

- `PlumSword.Core.Currency.Wallet` (`docs/spec/currency.md`): `CanSpend(long)`, `TrySpend(long)`, `BalanceChanged`.
- `PlumSword.Core.Currency.GoldText.FormatNumber(long)` (`docs/spec/currency.md`).
- `PlumSword.Core.Realm.RealmProgress.BonusPercent` (`docs/spec/realm.md`). 테스트에서는 `new RealmProgress(wallet, startLevel)`로 원하는 경지를 만든다 (`startLevel` 1 = 이류, 3 = 절정, 4 = 초절정).
- `PlumSword.Core.Combat.Battle.DiscipleBaseAttack`, `DiscipleBaseMaxHealth` (`docs/spec/combat.md`): `GameRoot`가 시작 능력치로 넣는다.
- `PlumSword.Game.GameRoot.Wallet`, `UiCanvas`, `Realm` (이미 있다).

## 화면

### 선택한 강화 버튼 그림 (2026-11-18)

그림이 있으면 공통 반투명 무협풍 바탕과 공격·체력·내공 아이콘, 금화 아이콘을 사용한다. 각 버튼은 200×48이며 기존 가로 위치와 구매 가능 조건을 유지한다. 이름, 단계, 비용은 각각 갱신한다. 이름에는 Galmuri11을 쓰고 단계와 비용은 기존 글꼴을 유지한다. 그림이 없는 대체 버튼에는 한글을 표시하기 위해 Galmuri11을 쓴다. 비용은 금화 아이콘 옆 숫자로 표시하고 최대 단계에는 아이콘 없이 `MAX`를 보인다. 보유 금화 현판은 무력 위에 둔다 ([금화 사양](currency.md)). 바탕이나 금화 그림이 없으면 단색과 기존 세 줄 글자를 쓴다. 아래의 단색·글자 배치는 그때의 대체 설명이다.

- **그림 모드 조건**: 바탕 `Art/UI/StatUpgrade/button`과 금화 `Art/UI/StatUpgrade/coin`이 모두 있으면 그림 모드다. 능력치 아이콘은 조건에 넣지 않는다.
- **능력치 아이콘**: `Art/UI/StatUpgrade/` 아래 `attack`(무력), `health`(체력), `inner_power`(내공). 그 버튼의 아이콘 그림이 없으면 아이콘 오브젝트(`Visual/Icon`)를 만들되 `SetActive(false)`로 숨긴다. 자리만 비고 이름·단계·비용·금화 아이콘의 배치는 그대로다. `inner_power`는 매화빛 기운을 표현한 아이콘을 사용한다.

그림 버튼에 마우스를 올리면 밝아지고 1.025배 커지며, 누르는 동안 0.96배가 된다. 구매 성공 시 아이콘이 잠깐 위로 움직이고 레벨 글자가 금빛으로 변하며 기존 시작 화면 꽃잎 두 장이 0.55초 동안 날아가 사라진다. 반복 구매 시 새 연출을 시작한다. 금화 부족·최대 단계·잠김 상태에는 호버와 성공 연출을 하지 않는다. 소리는 추가하지 않는다.

- **조립**: `GameRoot.Awake`에서 `[CurrencyView]`를 만든 다음에 `[Game]`의 자식 `[StatUpgradeView]`를 만들어 `StatUpgradeView`를 붙이고 `Initialize(StatUpgrades, Wallet, UiCanvas)`를 부른다.
- **보여 주는 것**: 강화 버튼 세 개. 왼쪽부터 `무력`, `체력`, `내공` 순서로 가로 한 줄이다. 내공 버튼은 예전 공격 속도 버튼 자리다. 버튼 오브젝트 이름은 `StatButton_Attack`, `StatButton_Health`, `StatButton_InnerPower`다. 버튼마다 글자 세 줄(이름, 단계, 비용)을 보여 준다. 단색 모드 글자는 `StatUpgradeText.ButtonLabel(stat, upgrades.GetLevel(stat), upgrades.GetCost(stat), upgrades.IsMaxLevel(stat))` 그대로다 (예: `무력` / `Lv.3` / `G 35`, `내공` / `Lv.1` / `G 25`). 화면 코드에서 글자를 따로 만들거나 고치지 않는다.
- **위치**: 캔버스 왼쪽 아래에 앵커와 피벗 (0, 0)을 두고 버튼 크기 200×48, 위치는 `무력` (8, 8), `체력` (220, 8), `내공` (432, 8)이다.
- **모양**: 버튼은 `Image`(단색 사각형)와 `Button`을 붙인 오브젝트다. `Button.transition`은 `None`으로 두고 색은 화면 코드가 직접 바꾼다. 글자는 버튼의 자식 `TextMeshProUGUI` 하나로, 버튼을 꽉 채우고 가운데 정렬, 글자 크기 16 (임시), TMP 기본 글꼴, `raycastTarget = false`.
- **누를 수 있을 때** (`upgrades.CanUpgrade(stat)`가 `true`): `Button.interactable = true`, 버튼 색 RGB (0.20, 0.45, 0.25), 글자 흰색 (임시).
- **누를 수 없을 때** (`CanUpgrade`가 `false`: 금화 부족, 최대 단계, 잠김): `Button.interactable = false`, 버튼 색 RGB (0.25, 0.25, 0.25), 글자 색 RGB (0.6, 0.6, 0.6) (임시). 세 경우를 모양으로 구분하지 않는다. 최대 단계는 글자 `MAX`로 알 수 있다.
- **누르면**: 그 버튼의 능력치로 `upgrades.TryUpgrade(stat)`를 부른다. 성공했을 때 위의 강화 연출을 한다. 한 번 누르면 한 단계다. 길게 눌러 연속 강화는 없다.
- **바뀌는 때**: `Initialize`에서 한 번, 그 뒤로 `wallet.BalanceChanged`, `upgrades.Upgraded`, `upgrades.LockedChanged`가 올 때마다 버튼 내용을 갱신한다. 호버·강화 연출은 매 프레임 갱신하며 소리는 없다.
- **사람이 먼저 해 둘 것**: 금화 사양과 같다. TextMeshPro 기본 리소스(TMP Essentials)가 있어야 글자가 보인다. 없어도 예외로 게임이 멈추지 않아야 한다. 내공 아이콘은 그림이 준비되면 위 경로에 파일만 넣는다.
- 표시 글자와 누를 수 있는지의 판단은 모두 Core(`StatUpgradeText`, `StatUpgrades.CanUpgrade`)에 있고 에딧 모드 테스트로 판정한다. 버튼 이름·아이콘 숨김은 플레이 모드 테스트(`StatUpgradeViewTests`)로 확인한다. `GameRoot`의 조립은 자동 테스트 대상이 아니며 사람이 실행해 확인한다.

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| 공격력 강화 단계 (`GetLevel(StatType.Attack)`) | `int` | 0 | |
| 체력 강화 단계 (`GetLevel(StatType.Health)`) | `int` | 0 | |
| 내공 강화 단계 (`GetLevel(StatType.InnerPower)`) | `int` | 0 | 저장 기능이 내공 칸을 만들 때까지는 저장되지 않는다 (아래) |

- 저장할 값은 위 단계 셋이다. 최종 능력치와 비용은 단계와 경지에서 계산되므로 저장하지 않는다. 잠금(`Locked`)도 저장하지 않으며 항상 풀린 상태로 시작한다. 경지는 경지 기능의 저장 값이다.
- 공격 속도 단계는 저장하지 않는다. 3단계를 시작할 때 사용자가 기존 저장 데이터를 지우고 새로 시작하므로, 예전 저장 값을 옮기거나 공격 속도 강화를 환불하지 않는다.
- 되살리기: `new StatUpgrades(wallet, baseAttack, baseMaxHealth, 저장된 공격력 단계, 저장된 체력 단계, 저장된 내공 단계, realm)`. 음수는 0으로 시작한다. 저장 전체의 손상 처리는 저장 기능이 정한다.
- 저장하는 때: 저장 기능이 정한다. 강화는 언제나 금화를 써서 `BalanceChanged`가 가므로 지금의 자동 저장 규칙만으로 강화 직후의 단계가 저장된다.
- 지금의 저장 데이터(`SaveData`, 판 1)에는 내공 칸이 없다. 그래서 저장 기능을 고치기 전까지는 `GameRoot`가 내공 단계에 0을 넣고, 게임을 다시 켜면 내공은 0으로 돌아간다. `SaveData.AttackSpeedLevel` 칸과 코덱은 그대로 남는다. 공격 속도 제거 때 `SaveData.Capture`는 그 칸에 0을 넣고 `GameRoot`는 그 값을 읽지 않는다. 칸을 내공 단계로 바꾸는 것, 판 번호, 경지 저장은 저장 기능(`docs/spec/save.md`)이 정한다.
- 오프라인 보상: 꺼져 있는 동안 강화 단계는 바뀌지 않는다. 강화 단계와 경지 보너스는 오프라인 보상 금액에 영향을 주지 않는다.

## 범위 밖

- 제자의 현재 체력과 체력 강화·돌파 때 현재 체력을 늘리는 처리: 전투 기능이 정한다. 이 기능의 `MaxHealth`, `Upgraded`를 쓴다.
- 기본 공격 간격 1초(임시, `Battle.DiscipleBaseAttackInterval`)와 공격 대기 시간: 전투 기능이 정한다.
- 이십사수매화검법의 범위, 피해 배율(200% + 내공 단계당 10%p), 피해 내림, 재사용 대기: 전투 기능이 정한다. 이 기능은 `GetLevel(StatType.InnerPower)`, `Attack`, `Upgraded`만 내준다.
- 경지 단계, 돌파 조건, 보너스 %와 오오라: 경지 기능(`docs/spec/realm.md`)이 정한다.
- 시작 능력치(10 / 100)의 원래 값, 몬스터·보스 능력치: 전투 기능이 정한다.
- 페이드 때 실제로 잠금을 거는 것: 스테이지 기능이 정한다.
- 저장 데이터 형식의 변경(공격 속도 칸 → 내공 칸, 판 번호): 저장 기능이 정한다.
- 비용 단위의 한글 표기(`25금화`)는 하지 않는다. 능력치 이름에는 Galmuri11을 적용한다.
- 버튼에 현재 능력치 값을 보여 주는 것, 강화 소리, 길게 눌러 연속 강화, 여러 단계 한꺼번에 강화, 강화 되돌리기·환불(공격 속도 강화 환불 포함): 하지 않는다.
- 시작 능력치가 `long` 범위를 넘칠 만큼 큰 경우: 다루지 않는다.

### 공격 속도 제거 — 사람이 직접 할 일 (루프 밖)

루프의 관문은 있던 테스트를 지우거나 검사(Assert) 수를 줄이면 합치지 않는다. 공격 속도를 없애면 그런 테스트가 생기므로 **사람이 대화로 main 에서 직접 하고 커밋한다.** 루프 작업(내공·경지 보너스)은 이 제거가 main 에 커밋된 뒤에 만든다. 제거 뒤에 모든 에딧 모드·플레이 모드 테스트가 통과해야 한다.

제거가 끝난 중간 상태: `StatType`은 `Attack`, `Health` 둘, 생성자는 `StatUpgrades(Wallet wallet, long baseAttack, long baseMaxHealth, int attackLevel = 0, int healthLevel = 0)`, 강화 버튼은 `무력`, `체력` 두 개다. 내공과 `realm` 인자는 루프가 더한다.

| 파일 | 할 일 |
| --- | --- |
| `Assets/Scripts/Core/StatUpgrade/StatType.cs` | `AttackSpeed = 2` 지움 |
| `Assets/Scripts/Core/StatUpgrade/StatUpgrades.cs` | `AttackSpeedMaxLevel`, `MinAttackInterval`, 공격 간격 증가량 상수, `_baseAttackInterval`, `_attackSpeedLevel`, 생성자 인자 `baseAttackInterval`·`attackSpeedLevel`과 그 검사, `AttackInterval` 속성, 모든 `AttackSpeed` 분기 지움 |
| `Assets/Scripts/Core/StatUpgrade/StatUpgradeText.cs` | `SPD` 분기와 주석 예시 지움 |
| `Assets/Scripts/Core/Combat/Battle.cs` | `_upgrades.AttackInterval` 세 곳(`DiscipleAttackInterval`, 앞 동작 대기, 공격 뒤 대기)을 `DiscipleBaseAttackInterval`로 바꿈. `DiscipleAttackInterval` 속성은 남기고 1.0 을 돌려준다 |
| `Assets/Scripts/Core/Save/SaveData.cs` | `Capture`가 `AttackSpeedLevel` 자리에 `0`을 넣음. 칸·생성자·코덱(`SaveCodec.cs`)은 그대로 |
| `Assets/Scripts/Game/GameRoot.cs` | `StatUpgrades` 생성에서 `DiscipleBaseAttackInterval`과 `data.AttackSpeedLevel` 인자 지움 |
| `Assets/Scripts/Game/StatUpgrade/StatUpgradeView.cs` | `Stats`·`ButtonPositions`를 `무력`, `체력` 둘로, 아이콘 이름의 `speed` 분기 지움 |
| `Assets/Resources/Art/UI/StatUpgrade/speed.png` (+ `.meta`) | 지움 |
| `Assets/Tests/EditMode/StatUpgrade/StatUpgradesTests.cs` | `AllStats`를 `Attack`, `Health` 둘로. 지움: `TryUpgrade_AttackSpeed_IntervalDecreases`, `TryUpgrade_AttackSpeedTenTimes_ReachesMaxLevel`, `TryUpgrade_AttackSpeedAtMaxLevel_FailsWithoutChange`, `New_AttackSpeedLevelAboveMax_ClampedToTenWithoutException`, `AttackInterval_SmallBase_ClampedToMinimum`, `New_InvalidBaseAttackInterval_ThrowsOutOfRange`. 본문에서 `AttackSpeed`·`AttackInterval` 줄만 뺌: `New_Default_InitialState`, `GetMaxLevel_EachStat_ReturnsLimit`, `TryUpgrade_Attack_OtherStatsUnchanged`, `New_WithLevels_RestoresLevelsWithoutSpending`, `New_NegativeLevels_ClampedToZeroWithoutException`, `New_OtherBaseValues_FinalStatsUseBaseValues`. 모든 생성자 호출에서 `1.0`과 공격 속도 단계 인자를 뺌 |
| `Assets/Tests/EditMode/StatUpgrade/StatUpgradeTextTests.cs` | `Name_DefinedStat_ReturnsEnglishAbbreviation`의 `SPD` 경우 지움. `ButtonLabel_Values_JoinsNameLevelCostWithNewline`의 `SPD` 경우를 `(Health, 5, 45L, true, "체력\nLv.5\nMAX")`로 바꿈 |
| `Assets/Tests/EditMode/Combat/BattleAttackTests.cs` | `AttackSpeedUpgradeMidBattle_PendingCooldownUnchangedNextUsesNewInterval` 지움. `WindUp_ScalesWithAttackInterval`은 시작 간격을 넣을 수 없으므로 1.0 기준 기대값 `1.0 * 7.0 / 17.0`으로 (테스트 도우미의 간격 인자도 지움) |
| `Assets/Tests/EditMode/Combat/BattleUpgradeTests.cs` | `AttackAndAttackSpeedUpgrade_DoNotChangeCurrentHealth`에서 공격 속도 강화 줄 지움 (이름은 바꿔도 된다) |
| `Assets/Tests/EditMode/Combat/DiscipleAnimatorTests.cs` | `BattleDiscipleAttackInterval_FollowsAttackSpeedUpgrade` 지우거나 "`DiscipleAttackInterval`은 늘 1.0" 확인으로 바꿈 |
| `Assets/Tests/EditMode/Stage/StageProgressRoundTests.cs` | `GetLevel(StatType.AttackSpeed)` 확인 줄 지움 |
| `Assets/Tests/EditMode/Save/SaveDataTests.cs` | `Restore_OutOfRangeValues_FeatureConstructorsClamp`: 공격 속도 단계 인자와 `GetLevel(AttackSpeed)` 확인 지움 (`SaveData`에 15가 담기는 것은 그대로). `Capture_BeforeBegin_CopiesValuesWithoutChangesOrEvents`: `data.AttackSpeedLevel` 기대값 0, `GetLevel(AttackSpeed)` 확인 지움 |
| `Assets/Tests/PlayMode/StatUpgradeViewTests.cs` | `Artwork_UpgradeAndMax_UpdateLabelsAndAvailability`: `SPD` 대신 체력을 `int.MaxValue` 단계로 만든 `StatUpgrades`로 `체력` 버튼의 `MAX`·금화 아이콘 숨김·누를 수 없음을 확인 |
| 그 밖의 생성자 호출 (코드·테스트 약 25개 파일) | `new StatUpgrades(wallet, 10, 100, 1.0, …)`에서 `1.0`과 공격 속도 단계 인자를 뺌 |
| `SaveCodecTests.cs`, `FileSaveStoreTests.cs`, `EnemyStatsTests.cs`, `BattleTests.cs`의 `DiscipleBaseAttackInterval` 확인 | 그대로 (`SaveData` 칸, 적 공격 간격, 전투 상수는 남는다) |

끝났는지 확인: `git grep -n "AttackSpeed\|AttackInterval" -- Assets/Scripts`의 결과가 `SaveData`·`SaveCodec`의 `AttackSpeedLevel`, 전투의 `DiscipleBaseAttackInterval`·`DiscipleAttackInterval`·적의 `AttackInterval`뿐이고, 테스트가 모두 통과한다.

## 완료 조건 초안

아래에서 따로 적지 않으면 `new StatUpgrades(wallet, 10, 100)`으로 만든 것이다 (경지 없음, 보너스 0%). 경지가 필요한 조건은 같은 `wallet`으로 만든 `realm = new RealmProgress(wallet, n)`을 마지막 인자로 넣는다.

이미 구현되고 바뀌지 않은 완료 조건(3, 5, 9~13, 15~18, 27, 29, 30, 33)은 기록 파일로 옮겼다. 6, 7, 8, 21, 24는 공격 속도 조건이라 제거와 함께 없어졌다. 번호는 그대로 두고, 새 조건은 34번부터 붙인다.

강화 로직 (`StatUpgradesTests`)

1. 새로 만들면 `Attack`, `Health`, `InnerPower`의 `GetLevel`은 0, `GetCost`는 20, `IsMaxLevel`은 `false`이고, `Attack`은 10, `MaxHealth`는 100, `Locked`는 `false`다.
2. `GetMaxLevel`은 `Attack`, `Health`, `InnerPower` 모두 `int.MaxValue`다.
4. 금화 1000에서 `TryUpgrade(Attack)`을 1번 부른 뒤 `GetLevel(Health)`와 `GetLevel(InnerPower)`는 0, `GetCost(Health)`와 `GetCost(InnerPower)`는 20, `MaxHealth`는 100이다.
14. 금화 20에서 `CanUpgrade`는 `Attack`, `Health`, `InnerPower` 모두 `true`다. 금화 19에서는 모두 `false`다. 부른 뒤 금화와 단계는 그대로이고 알림은 0번 온다.
19. `new StatUpgrades(wallet, 10, 100, 3, 2, 4)`의 `GetLevel`은 `Attack`, `Health`, `InnerPower` 순서로 3, 2, 4이고 `Attack`은 16, `MaxHealth`는 140, `GetCost`는 순서대로 35, 30, 40이다. 만드는 동안 금화는 바뀌지 않고 알림은 0번 온다.
20. `new StatUpgrades(wallet, 10, 100, -1, -5, -2)`의 세 단계는 모두 0이고 예외가 없다.
22. `new StatUpgrades(wallet, 10, 100, int.MaxValue)`의 `IsMaxLevel(Attack)`은 `true`, `Attack`은 4294967304, `GetCost(Attack)`은 10737418255이고, 금화가 `long.MaxValue`여도 `TryUpgrade(Attack)`은 `false`이며 금화는 그대로다.
23. `new StatUpgrades(wallet, 50, 300, 1, 1)`의 `Attack`은 52, `MaxHealth`는 320이다.
25. `new StatUpgrades(null, 10, 100)`은 `ArgumentNullException`을 던진다.
26. `baseAttack`이 0인 경우와 `baseMaxHealth`가 0인 경우 생성자는 각각 `ArgumentOutOfRangeException`을 던진다.

내공 (`StatUpgradesTests` 또는 새 `StatUpgradesInnerPowerTests`)

34. 금화 1000에서 `TryUpgrade(InnerPower)`를 3번 부르면 모두 `true`이고, 부르기 전의 `GetCost(InnerPower)`는 순서대로 20, 25, 30이며, 끝난 뒤 `GetLevel(InnerPower)`는 3, `GetCost(InnerPower)`는 35, 금화는 925, `Attack`은 10, `MaxHealth`는 100, `GetLevel(Attack)`과 `GetLevel(Health)`는 0이다.
35. 금화 1000에서 `TryUpgrade(InnerPower)`를 부르면 `Wallet.BalanceChanged`가 먼저 오고(그 시점의 `GetLevel(InnerPower)`는 0), 이어서 `Upgraded`가 `StatType.InnerPower`를 담아 1번 오며, 받는 쪽에서 읽은 `GetLevel(InnerPower)`는 1, `GetCost(InnerPower)`는 25다.
36. 금화 19에서 `TryUpgrade(InnerPower)`는 `false`이고 단계 0, 금화 19, `Upgraded`와 `BalanceChanged`는 0번 온다. 금화 1000에서 `SetLocked(true)` 뒤 `CanUpgrade(InnerPower)`와 `TryUpgrade(InnerPower)`는 `false`이고 금화는 1000, 단계는 0이다.
37. `new StatUpgrades(wallet, 10, 100, 0, 0, int.MaxValue)`의 `GetLevel(InnerPower)`는 `int.MaxValue`, `IsMaxLevel(InnerPower)`는 `true`, `GetCost(InnerPower)`는 10737418255이고, 금화가 `long.MaxValue`여도 `TryUpgrade(InnerPower)`는 `false`이며 금화는 그대로다.

경지 보너스 (`StatUpgradesRealmTests`)

38. `realm = new RealmProgress(wallet, 1)`(이류)로 만든 `new StatUpgrades(wallet, 10, 100, 0, 0, 0, realm)`의 `Attack`은 11, `MaxHealth`는 110이다.
39. 이류(`startLevel` 1)에서 단계 (1, 1, 0)이면 `Attack`은 13, `MaxHealth`는 132다.
40. 절정(`startLevel` 3)에서 단계 (1, 0, 0)이면 `Attack`은 15, `MaxHealth`는 130이다.
41. 초절정(`startLevel` 4)에서 단계 (3, 2, 5)이면 `Attack`은 22, `MaxHealth`는 196이고, 단계 (3, 2, 0)으로 만들어도 `Attack` 22, `MaxHealth` 196으로 같다 (내공은 영향 없음).
42. 삼류(`startLevel` 0)에서 단계 (0, 0, 0)이면 `Attack` 10, `MaxHealth` 100이다. `realm`을 `null`로 넣은 `new StatUpgrades(wallet, 10, 100, 1, 1, 0, null)`은 예외 없이 `Attack` 12, `MaxHealth` 120이다.
43. 금화 200에서 `realm = new RealmProgress(wallet)`, `new StatUpgrades(wallet, 10, 100, 1, 1, 0, realm)`의 `Attack`은 12, `MaxHealth`는 120이다. `realm.RecordKill()`을 30번 부른 뒤 `Attack`은 13, `MaxHealth`는 132, 금화는 200이고, 그동안 `Upgraded`는 0번 온다.
44. 금화 1000에서 이류(`startLevel` 1)로 만든 `new StatUpgrades(wallet, 10, 100, 0, 0, 0, realm)`의 `MaxHealth`는 110이다. `TryUpgrade(Health)`를 부르면 `true`이고 `Upgraded`를 받는 쪽에서 읽은 `MaxHealth`는 132, 금화는 980이다.
45. 44번처럼 만든 `upgrades`로 `new Battle(upgrades)`를 만들면 `DiscipleHealth`와 `DiscipleMaxHealth`는 110이다. `TryUpgrade(Health)` 뒤 `DiscipleHealth`와 `DiscipleMaxHealth`는 132다 (22 늘어남). 이어서 `TryUpgrade(Attack)`과 `TryUpgrade(InnerPower)`를 불러도 `DiscipleHealth`는 132 그대로다.
46. 초절정(`startLevel` 4)에서 `new StatUpgrades(wallet, 10, 100, int.MaxValue, int.MaxValue, 0, realm)`의 `Attack`은 6012954225, `MaxHealth`는 60129542256이고 예외가 없다.

표시 글자 (`StatUpgradeTextTests`)

28. `StatUpgradeText.Name`은 `Attack` → `"무력"`, `Health` → `"체력"`, `InnerPower` → `"내공"`을 돌려주고, `(StatType)99`는 `ArgumentOutOfRangeException`을 던진다.
31. `StatUpgradeText.ButtonLabel`은 (`Attack`, 3, 35, `false`) → `"무력\nLv.3\nG 35"`, (`Health`, 0, 20, `false`) → `"체력\nLv.0\nG 20"`, (`Health`, 5, 45, `true`) → `"체력\nLv.5\nMAX"`, (`InnerPower`, 1, 25, `false`) → `"내공\nLv.1\nG 25"`를 돌려주고, `(StatType)99`는 `ArgumentOutOfRangeException`을 던진다.

화면 (`StatUpgradeViewTests`, 플레이 모드)

47. 금화 1000, `new StatUpgrades(wallet, 10, 100)`으로 `StatUpgradeView.Initialize`를 부르면 캔버스 아래에 `StatButton_Attack`, `StatButton_Health`, `StatButton_InnerPower`가 있고 `StatButton_AttackSpeed`는 없으며, `StatButton_InnerPower`의 `anchoredPosition`은 (432, 8)이다. `StatButton_InnerPower`의 `Visual/Name`은 `"내공"`, `Visual/Level`은 `"Lv.0"`, `Visual/Cost`는 `"20"`이다. `Visual/Icon`은 `Resources.Load<Sprite>("Art/UI/StatUpgrade/inner_power")`가 `null`이면 `activeSelf`가 `false`이고, 아니면 `true`이며 그 그림을 쓴다. 그 버튼을 누르면 `Visual/Level`은 `"Lv.1"`, `Visual/Cost`는 `"25"`, 금화는 980이다.

화면 (자동 테스트 없음. 사람이 실행해 확인)

32. 게임을 실행하면 화면 아래쪽 금화 현판 밑에 버튼 세 개가 왼쪽부터 `무력`, `체력`, `내공` 순서로 보이고, 각각 `Lv.0`과 금화 아이콘 옆 `20`이 표시된다. `내공`는 아이콘 자리가 비어 있다. 금화가 0이라 세 버튼 모두 어둡고 눌리지 않는다. 금화를 모아 이류로 돌파한 뒤 다음 스테이지가 시작되면 제자 체력 바가 `110/110`이다.

## 결정 기록

결정 기록은 [records/stat_upgrade.md](records/stat_upgrade.md)의 "결정 기록"에 있다. 구현에 필요한 규칙은 위 절들에 모두 있다.

## 변경 기록

### 2026-11-18 — 3단계: 공격 속도 제거, 내공 강화 추가, 경지 보너스 적용 (기준 커밋: c02cb2a)

| 절 | 바뀌기 전 | 바뀐 뒤 |
| --- | --- | --- |
| 목표 | 공격력·체력·공격 속도 강화, 최종 공격력·최대 체력·공격 간격 계산. `SPD`는 나중에 제거 예정 | 공격력·체력·내공 강화, 경지 보너스가 반영된 공격력·최대 체력 계산, 내공 단계 제공. 공격 속도 제거는 사람이 직접 |
| 동작 | 능력치 `Attack`, `Health`, `AttackSpeed` | `Attack`, `Health`, `InnerPower` |
| 동작 | `Attack`, `MaxHealth`, `AttackInterval`은 시작값과 단계로 계산 | `Attack`, `MaxHealth`는 시작값·단계·`realm.BonusPercent`로 계산. 돌파 즉시 새 값, 알림 없음. `AttackInterval` 없음 (전투 상수) |
| 동작 | 체력 강화 때 현재 체력 증가 = 강화 전후 `MaxHealth` 차이 | 같은 규칙이고, 그 차이는 경지 보너스가 적용된 최종 최대 체력의 차이 (이류 110 → 132, +22) |
| 경계 상황 | 공격 속도 10단계 상한, `attackSpeedLevel` 10 초과 → 10, `baseAttackInterval` 검사 | 세 능력치 모두 `int.MaxValue`가 상한. `realm`이 `null`이면 0%. 큰 단계와 초절정도 넘침 없음 |
| 수치 | 공격 간격 `Math.Max(0.5, 시작 간격 - 0.05 × 단계)`, `MinAttackInterval`, `AttackSpeedMaxLevel` | 없음. 공격력·최대 체력에 `× (100 + 보너스) / 100` (`long` 정수 나눗셈). 내공 비용 `20 + 5 × 단계`, 이름 `INNER`, 아이콘 경로 `Art/UI/StatUpgrade/inner_power` |
| 연결 지점 | `StatType.AttackSpeed = 2`, 생성자 `(wallet, baseAttack, baseMaxHealth, baseAttackInterval, attackLevel, healthLevel, attackSpeedLevel)`, `AttackInterval` | `StatType.InnerPower = 2`, 생성자 `(wallet, baseAttack, baseMaxHealth, attackLevel, healthLevel, innerPowerLevel, realm = null)`, `AttackInterval`·`MinAttackInterval`·`AttackSpeedMaxLevel` 없음. `GameRoot`가 `Realm`과 내공 0 을 넣음 |
| 화면 | 버튼 `ATK`, `HP`, `SPD`. 그림 모드는 바탕·아이콘·금화가 모두 있어야 함 | 버튼 `ATK`, `HP`, `INNER` (`SPD` 자리). 그림 모드는 바탕·금화만 있으면 되고, 아이콘이 없으면 아이콘 칸만 숨김 |
| 저장 | 공격력·체력·공격 속도 단계 | 공격력·체력·내공 단계. 저장 기능이 칸을 바꾸기 전까지 내공은 저장되지 않고 0 으로 시작. 제거 때 `Capture`는 공격 속도 칸에 0 |
| 범위 밖 | 경지 보너스와 공격 속도 → 내공은 3단계에서 | 스킬 배율·공격 간격은 전투, 저장 형식은 저장 기능. "공격 속도 제거 — 사람이 직접 할 일" 절 추가 |
| 완료 조건 | 1~33 (공격 속도 조건 포함) | 아래 |

- 완료 조건 초안: 새로 34~47 / 바뀜 1, 2, 4, 14, 19, 20, 22, 23, 25, 26, 28, 31, 32 / 없어짐 6, 7, 8, 21, 24 / 나머지는 기록 파일로 옮김 (이미 구현됨).
  - **사람이 공격 속도 제거 때 끝내는 것 (루프 작업 아님)**: 없어진 6, 7, 8, 21, 24와 바뀐 22, 23, 25, 26. 제거 뒤 테스트가 이 조건대로 통과한다.
  - **사람과 루프가 나눠 하는 것**: 바뀐 1, 2, 4, 14, 19, 20, 28, 31. 사람이 제거 때 `AttackSpeed`·`AttackInterval` 부분을 빼고, 루프가 `InnerPower` 부분을 더한다.
  - **루프가 하는 것**: 새로 34~47, 바뀐 32, 그리고 위 1, 2, 4, 14, 19, 20, 28, 31의 내공 부분.
  - 루프 작업은 사람의 제거가 main 에 커밋된 뒤에 만든다 (제거는 작업 문서가 아니라서 선행 작업으로 적을 수 없다). 제거가 끝난 표시는 main 의 `StatType`에 `AttackSpeed`가 없는 것이다.
- 바뀌는 연결 지점:
  - 사람이 제거 때: `StatType.AttackSpeed` 없어짐. `StatUpgrades.AttackInterval`, `MinAttackInterval`, `AttackSpeedMaxLevel` 없어짐. 생성자가 `(wallet, baseAttack, baseMaxHealth, attackLevel = 0, healthLevel = 0)`로. `Battle.DiscipleAttackInterval`은 `Battle.DiscipleBaseAttackInterval`을 돌려줌 (전투 연결 지점의 뜻이 바뀜).
  - 루프가: `StatType.InnerPower = 2` 추가. 생성자 끝에 `int innerPowerLevel = 0, RealmProgress realm = null` 추가. `Attack`, `MaxHealth`가 경지 보너스를 반영. `StatUpgradeText.Name(InnerPower)` → `"INNER"`.
- 고쳐야 할 기존 코드 (루프): `Assets/Scripts/Core/StatUpgrade/StatType.cs`, `StatUpgrades.cs`, `StatUpgradeText.cs` — 내공·경지 보너스. `Assets/Scripts/Game/StatUpgrade/StatUpgradeView.cs` — 세 번째 버튼 `INNER`, 그림 모드 조건, 아이콘 숨김. `Assets/Scripts/Game/GameRoot.cs` — `StatUpgrades` 생성에 `0, Realm` 넘김. 사람이 제거 때 고칠 코드는 "범위 밖"의 표.
- 기존 테스트 (사람의 제거가 끝난 뒤 기준. 제거 때 지우고 고칠 테스트는 "범위 밖"의 표):

| 테스트 | 제거 뒤 | 바뀐 뒤 | 처리 |
| --- | --- | --- | --- |
| `StatUpgradesTests` 의 `AllStats`를 쓰는 테스트 (`New_Default_InitialState`, `CanUpgrade_GoldEqualsCost_TrueForAllWithoutChange`, `CanUpgrade_GoldBelowCost_FalseForAllWithoutChange`, `UndefinedStat_EveryMethod_ThrowsOutOfRangeWithoutChange` 등) | `Attack`, `Health` 둘 | `InnerPower`까지 셋 | 이름 유지, `AllStats`에 `InnerPower`를 더함 (루프). 따로 새 테스트로 확인해도 된다 |
| `StatUpgradesTests.GetMaxLevel_EachStat_ReturnsLimit` | `Attack`, `Health` → `int.MaxValue` | `InnerPower`도 `int.MaxValue` | 이름 유지, 확인 추가 (루프) |
| `StatUpgradesTests.TryUpgrade_Attack_OtherStatsUnchanged` | `Health`만 확인 | `InnerPower`도 0 / 20 | 이름 유지, 확인 추가 (루프) |
| `StatUpgradesTests.New_WithLevels_RestoresLevelsWithoutSpending` | `(wallet, 10, 100, 3, 2)` | `(wallet, 10, 100, 3, 2, 4)`, 내공 4·비용 40 | 이름 유지, 인자·확인 추가 (루프) |
| `StatUpgradesTests.New_NegativeLevels_ClampedToZeroWithoutException` | `(wallet, 10, 100, -1, -5)` | `(wallet, 10, 100, -1, -5, -2)`, 내공 0 | 이름 유지, 인자·확인 추가 (루프) |
| `StatUpgradeTextTests.Name_DefinedStat_ReturnsEnglishAbbreviation` | `ATK`, `HP` | `INNER` 경우 추가 | 이름 유지, `TestCase` 추가 (루프) |
| `StatUpgradeTextTests.ButtonLabel_Values_JoinsNameLevelCostWithNewline` | `ATK`, `HP`, `HP MAX` 경우 | `INNER\nLv.1\nG 25` 경우 추가 | 이름 유지, `TestCase` 추가 (루프) |
| 그 밖의 `StatUpgrades`·`Battle`·스테이지·저장 테스트 | 경지 없음 (보너스 0%) | 그대로 통과 (`realm` 기본값 `null`) | 고치지 않음 |
