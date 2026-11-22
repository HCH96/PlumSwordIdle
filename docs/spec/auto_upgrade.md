# 자동 강화 사양

- 기능 이름: auto_upgrade (네임스페이스 `PlumSword.Core.StatUpgrade`)
- 작성일: 2026-11-22

## 목표

켜 두면 금화가 생길 때마다 무력·체력·내공 가운데 가장 싼 강화를 자동으로 사는 기능이다. 금화 현판 옆의 버튼으로 켜고 끈다. 목표 플레이 시간(약 20분)은 자동 강화를 켠 상태가 기준이다.

출처: `docs/design/milestone_4.md` "목표와 범위", 2026-11-22 대화.

## 동작

- `AutoUpgrade`는 `StatUpgrades`와 `Wallet`을 받아 만든다. 처음 상태(`Enabled`)는 생성자 인자로 받는다 (게임은 저장된 값, 없으면 `true`). 생성자에서는 사지 않는다.
- **한꺼번에 사기** `BuyAll()`: 꺼져 있으면 아무것도 하지 않고 0을 돌려준다. 켜져 있으면 아래를 되풀이하고 산 횟수를 돌려준다.
  1. `upgrades.CanUpgrade(stat)`가 `true`인 능력치 가운데 `GetCost(stat)`가 가장 작은 것을 고른다. 비용이 같으면 무력(`Attack`) → 체력(`Health`) → 내공(`InnerPower`) 순서로 앞의 것.
  2. 고를 것이 없으면 끝낸다.
  3. `upgrades.TryUpgrade(stat)`를 부른다.
  4. 한 번의 `BuyAll`에서 1000번 샀으면 끝낸다 (비용이 0인 수치 표에서 끝없이 도는 것을 막는 안전장치, 임시).
- **자동으로 부르는 때**: 켜져 있으면 다음 알림을 받을 때마다 `BuyAll()`을 부른다.
  - `wallet.BalanceChanged`
  - `upgrades.LockedChanged`에서 `false`(잠금 풀림)를 받을 때
  - `SetEnabled(true)`로 켜질 때 (켠 직후 한 번)
- **다시 들어오기 막기**: `BuyAll` 안의 `TryUpgrade`가 금화를 써서 `BalanceChanged`가 다시 오면, 이미 사는 중이므로 무시한다. 바깥의 `BuyAll`이 이어서 처리한다.
- 잠긴 동안(스테이지 페이드)에는 `CanUpgrade`가 `false`라 사지 않는다. 잠금이 풀리면 바로 산다.
- **켜고 끄기** `SetEnabled(bool)`: 값이 실제로 바뀐 때에만 `EnabledChanged`를 새 값을 담아 한 번 보낸다. 켜질 때는 `EnabledChanged`를 보낸 다음 `BuyAll()`을 부른다. 같은 값이면 아무 일도 없다.
- 직접 강화 버튼을 누르는 것은 켜져 있어도 그대로 된다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| 꺼진 상태에서 금화가 늘어남 | 사지 않는다 | 금화 그대로 |
| 켜진 상태에서 금화가 어떤 비용보다도 적음 | 사지 않는다 (`BuyAll` 0) | 없음 |
| 셋 다 살 수 있고 비용이 같음 | 무력부터 산다 | |
| 잠긴 동안 금화가 늘어남 | 사지 않는다 | 잠금이 풀리면 산다 |
| 모든 능력치가 최대 단계 | 사지 않는다 | |
| 한 번에 여러 개를 살 금화 | 살 수 있는 동안 계속 산다 (가장 싼 것부터) | 예: 금화 100, 비용 모두 `20 + 5 × 단계` → 무력 20, 체력 20, 내공 20, 무력 25를 사고 15가 남는다 ("완료 조건" 3번) |
| `BuyAll`이 1000번 삼 | 거기서 끝낸다 | 다음 알림 때 다시 산다 |
| 생성자의 `upgrades`나 `wallet`이 `null` | `ArgumentNullException` | |
| `SetEnabled`를 같은 값으로 부름 | 아무 일도 없다 | `EnabledChanged` 없음 |

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 고르는 기준 | 가장 작은 `GetCost`, 같으면 무력 → 체력 → 내공 | |
| 한 번의 `BuyAll` 최대 구매 수 | 1000 | `int`, 임시 안전장치 |
| 처음 상태 | 켜짐 (`true`) | 저장 값이 없을 때 |
| 버튼 글자 `AutoUpgradeText.Label` | 켜짐 → `자동 강화 켜짐`, 꺼짐 → `자동 강화 꺼짐` | |

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.StatUpgrade
{
    public sealed class AutoUpgrade
    {
        // upgrades, wallet 이 null 이면 ArgumentNullException.
        // wallet.BalanceChanged 와 upgrades.LockedChanged 를 구독한다. 생성자에서는 사지 않고 알림도 보내지 않는다.
        public AutoUpgrade(StatUpgrades upgrades, PlumSword.Core.Currency.Wallet wallet, bool enabled = true);

        public bool Enabled { get; }

        // 값이 바뀐 때에만 EnabledChanged 를 보낸다. true 로 바뀌면 그다음 BuyAll() 을 부른다.
        public void SetEnabled(bool enabled);

        // 켜짐/꺼짐이 바뀐 뒤 한 번, 새 값을 담아 보낸다.
        public event System.Action<bool> EnabledChanged;

        // 꺼져 있으면 0. 켜져 있으면 살 수 있는 동안 가장 싼 강화를 사고 산 횟수를 돌려준다 (최대 1000).
        // 이미 BuyAll 안이면(알림으로 다시 들어옴) 아무것도 하지 않고 0.
        public int BuyAll();

        // 구독을 끊는다. 두 번 불러도 된다.
        public void Dispose();
    }

    public static class AutoUpgradeText
    {
        // true -> "자동 강화 켜짐", false -> "자동 강화 꺼짐"
        public static string Label(bool enabled);
    }
}
```

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스
    {
        // Awake 에서 StatUpgrades 를 만든 뒤 하나 만든다: new AutoUpgrade(StatUpgrades, Wallet, 저장된 켜짐 값).
        // 저장 칸이 생기기 전(docs/spec/regression.md 의 저장)까지는 true 를 넣는다.
        // OnDestroy 에서 Dispose() 를 부른다.
        public PlumSword.Core.StatUpgrade.AutoUpgrade AutoUpgrade { get; }
    }
}

namespace PlumSword.Game.StatUpgrade
{
    public sealed class AutoUpgradeView : MonoBehaviour
    {
        // canvas 아래에 버튼 "AutoUpgradeButton" 을 만들고 지금 상태로 그린다.
        // 누르면 autoUpgrade.SetEnabled(!autoUpgrade.Enabled). EnabledChanged 를 구독하고 OnDestroy 에서 끊는다.
        public void Initialize(PlumSword.Core.StatUpgrade.AutoUpgrade autoUpgrade, UnityEngine.Canvas canvas);
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot.Awake`가 `StatUpgrades`를 만든 바로 다음에 하나 만들어 `GameRoot.AutoUpgrade`로 들고 있는다. 게임에 하나뿐이다. 화면은 `[StatUpgradeView]` 다음에 `[Game]`의 자식 `[AutoUpgradeView]`로 만든다.
- 시뮬레이터도 같은 클래스를 만들어 쓴다.

### 필요한 것

- `StatUpgrades.CanUpgrade`, `GetCost`, `TryUpgrade`, `LockedChanged` (`docs/spec/stat_upgrade.md`).
- `Wallet.BalanceChanged` (`docs/spec/currency.md`).

## 화면

- **위치**: 아래쪽 강화 UI 영역, 금화 현판과 같은 줄의 오른쪽 8만큼 떨어진 곳. 캔버스 왼쪽 아래 기준 앵커 (0, 0), 피벗 (0, 1), 크기 96×26. 금화 현판의 너비가 늘어나면 버튼도 함께 오른쪽으로 이동한다. 현판이 없으면 `anchoredPosition`은 (132, 86)이다.
- **모양**: 전용 현판 `Art/UI/StatUpgrade/auto_upgrade`를 쓰고, 없으면 단색 사각형이다. 글자는 자식 `TextMeshProUGUI` `Label` 하나로, `AutoUpgradeText.Label(Enabled)` 그대로, Galmuri11, 크기 11, 가운데 정렬, `raycastTarget = false`.
- **켜짐**: 현판 원래 색과 밝은 상아색 글자. **꺼짐**: 현판과 글자를 어둡게 표시한다. 글자에도 켜짐·꺼짐을 표시한다.
- **누르면**: 켜짐과 꺼짐이 바뀐다. 잠금(페이드) 중에도 누를 수 있다. 소리와 연출은 없다.
- **바뀌는 때**: `Initialize`에서 한 번, 그 뒤 `EnabledChanged`가 올 때마다.
- 글자는 Core(`AutoUpgradeText`)에서 만들고 에딧 모드 테스트로 판정한다. 버튼 이름·위치·누름은 플레이 모드 테스트로 확인한다.

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| 자동 강화 켜짐 (`AutoUpgrade.Enabled`) | `bool` | `true` | 저장 칸은 `docs/spec/regression.md`의 저장 변경(`autoUpgrade` 줄)이 만든다 |

- 되살리기: `new AutoUpgrade(upgrades, wallet, 저장된 값)`.
- 켜고 끄기만으로는 저장하지 않는다. 다음 저장(금화가 바뀔 때마다 일어남) 때 함께 저장된다.
- 오프라인 보상: 꺼져 있는 동안에는 사지 않는다. 오프라인 보상을 받으면 금화가 바뀌므로 켜져 있으면 그때 산다.

## 범위 밖

- 능력치 비율로 사기, 특정 능력치만 자동, 예약 구매: 하지 않는다.
- 강화 연출: 자동 구매에도 기존 강화 버튼의 연출은 화면이 `Upgraded`를 받아 하던 대로 한다 (따로 끄지 않는다).
- 저장 칸 자체: `docs/spec/regression.md`.

## 완료 조건 초안

Core (`Assets/Tests/EditMode/StatUpgrade/AutoUpgradeTests.cs`). 따로 적지 않으면 `wallet = new Wallet(0)`, `upgrades = new StatUpgrades(wallet, 10, 100)`, 기본 수치 표(비용 `20 + 5 × 단계`)다.

1. `new AutoUpgrade(upgrades, wallet)`의 `Enabled`는 `true`이고, 만들 때 금화와 단계는 바뀌지 않는다.
2. 켜진 상태에서 `wallet.Add(20)`이면 무력 단계 1, 체력·내공 0, 금화 0이다.
3. 켜진 상태에서 `wallet.Add(100)`이면 무력·체력·내공 단계가 각각 2, 1, 1이고 금화는 15다 (20 + 20 + 20 + 25를 산 뒤 남은 15로는 다음 비용 25를 못 산다).
4. 꺼진 상태(`enabled: false`)에서 `wallet.Add(100)`이면 세 단계는 0이고 금화는 100이다.
5. 4번 상태에서 `SetEnabled(true)`를 부르면 `EnabledChanged`가 `true`를 담아 1번 오고, 그 뒤 세 단계는 2, 1, 1, 금화는 15다.
6. `upgrades.SetLocked(true)` 뒤 `wallet.Add(20)`이면 단계는 모두 0, 금화 20이다. 이어서 `SetLocked(false)`면 무력 단계 1, 금화 0이다.
7. 금화 0에서 `BuyAll()`은 0을 돌려준다.
8. `new StatUpgrades(wallet, 10, 100, 1, 0, 0)`(무력 비용 25)에서 `wallet.Add(20)`이면 체력 단계가 1이 된다 (더 싼 것 먼저).
9. `SetEnabled(true)`를 켜진 상태에서 부르면 `EnabledChanged`는 0번 온다.
10. `SetEnabled(false)` 뒤 `wallet.Add(100)`이면 사지 않는다.
11. 수치 표의 세 비용을 (0, 0, 1)로 바꾸고 `wallet.Add(1)`이면 이번 알림의 구매 수는 1000이다 (세 단계 합 1000). 테스트 끝에 표를 `Default`로 되돌린다.
12. `new AutoUpgrade(null, wallet)`과 `new AutoUpgrade(upgrades, null)`은 각각 `ArgumentNullException`을 던진다.
13. `Dispose()` 뒤 `wallet.Add(100)`이면 사지 않는다. `Dispose()`를 두 번 불러도 예외가 없다.
14. `AutoUpgradeText.Label(true)`는 `"자동 강화 켜짐"`, `Label(false)`는 `"자동 강화 꺼짐"`이다.

화면 (`Assets/Tests/PlayMode/StatUpgrade/AutoUpgradeViewTests.cs`, 플레이 모드)

15. 금화 현판이 없는 캔버스에서 꺼진 `AutoUpgrade`로 `AutoUpgradeView.Initialize`를 부르면 캔버스 아래 `AutoUpgradeButton`이 있고 `anchoredPosition`은 (132, 86), 크기는 (96, 26), `Label` 글자는 `"자동 강화 꺼짐"`이다.
16. 그 버튼을 누르면 `Enabled`는 `true`이고 `Label` 글자는 `"자동 강화 켜짐"`이다.
17. 첫 씬을 불러오면 `[Game]`의 `GameRoot.AutoUpgrade`가 `null`이 아니고 `Enabled`는 `true`다.

화면 (사람 확인)

18. 게임을 실행하고 10초 뒤 금화 현판 오른쪽에 `자동 강화 켜짐` 버튼이 현판과 겹치지 않게 보이고, 금화가 쌓이는 대로 강화 레벨이 저절로 오른다.

## 결정 기록

결정 기록은 [records/auto_upgrade.md](records/auto_upgrade.md)의 "결정 기록"에 있다. 구현에 필요한 규칙은 위 절들에 모두 있다.
