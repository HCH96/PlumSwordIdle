# 091_auto_upgrade — 자동 강화 AutoUpgrade 와 버튼 글자 AutoUpgradeText

## 목표
Core(`PlumSword.Core.StatUpgrade`)에 켜 두면 금화가 생길 때마다 가장 싼 강화를 사는 `AutoUpgrade`와, 켜고 끄는 버튼의 글자를 만드는 `AutoUpgradeText`를 만든다. 화면과 `GameRoot` 조립은 092 가 한다.

## 묶음
auto_upgrade

## 선행 작업
- 088_balance_usage

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/auto_upgrade.md ("동작", "경계 상황", "수치", "연결 지점"의 `PlumSword.Core.StatUpgrade`)
- docs/spec/stat_upgrade.md ("연결 지점" 절)
- docs/spec/currency.md ("연결 지점" 절)
- docs/spec/balance.md ("연결 지점" 절, 11번 조건에서 표를 바꿀 때)

## 요구사항
- `Assets/Scripts/Core/StatUpgrade/AutoUpgrade.cs`, `Assets/Scripts/Core/StatUpgrade/AutoUpgradeText.cs`를 만든다. 네임스페이스 `PlumSword.Core.StatUpgrade`. 모양은 사양 "연결 지점" 그대로다.
  - `public AutoUpgrade(StatUpgrades upgrades, PlumSword.Core.Currency.Wallet wallet, bool enabled = true)`
  - `public bool Enabled { get; }`, `public void SetEnabled(bool enabled)`, `public event System.Action<bool> EnabledChanged`, `public int BuyAll()`, `public void Dispose()`
  - `public static class AutoUpgradeText { public static string Label(bool enabled); }`
- 생성자: `upgrades`나 `wallet`이 `null`이면 `ArgumentNullException`. `wallet.BalanceChanged`와 `upgrades.LockedChanged`를 구독한다. 생성자에서는 사지 않고 알림도 보내지 않는다.
- **`BuyAll()`**: 꺼져 있으면 아무것도 하지 않고 0. 켜져 있으면 아래를 되풀이하고 산 횟수를 돌려준다.
  1. `upgrades.CanUpgrade(stat)`가 `true`인 능력치 가운데 `upgrades.GetCost(stat)`가 가장 작은 것을 고른다. 비용이 같으면 `StatType.Attack` → `StatType.Health` → `StatType.InnerPower` 순서로 앞의 것.
  2. 고를 것이 없으면 끝낸다.
  3. `upgrades.TryUpgrade(stat)`를 부른다.
  4. 한 번의 `BuyAll`에서 1000번 샀으면 끝낸다 (임시 안전장치, `const int` 1000).
- **다시 들어오기 막기**: `BuyAll` 실행 중에 알림(`BalanceChanged` 등)으로 다시 `BuyAll`이 불리면 아무것도 하지 않고 0을 돌려준다. 바깥의 `BuyAll`이 이어서 처리한다.
- **자동으로 부르는 때** (켜져 있을 때만): `wallet.BalanceChanged`를 받을 때, `upgrades.LockedChanged`에서 `false`를 받을 때, `SetEnabled(true)`로 켜질 때.
- **`SetEnabled(bool)`**: 값이 실제로 바뀐 때에만 값을 바꾸고 `EnabledChanged`를 새 값을 담아 한 번 보낸다. 켜질 때는 `EnabledChanged`를 보낸 **다음** `BuyAll()`을 부른다. 같은 값이면 아무 일도 없다.
- **`Dispose()`**: 두 구독을 끊는다. 두 번 불러도 예외가 없다. 끊은 뒤에는 알림으로 사지 않는다.
- 잠긴 동안에는 `CanUpgrade`가 `false`라 사지 않는다 (따로 검사하지 않아도 된다).
- `AutoUpgradeText.Label(true)` = `"자동 강화 켜짐"`, `Label(false)` = `"자동 강화 꺼짐"`.
- 비용은 088 이후 `StatUpgrades.GetCost`가 `BalanceTable.Current`의 곡선을 읽는다. 11번 테스트는 `var table = BalanceTable.Default; table.AttackCost = new GrowthCurve(0, 0, 1); table.HealthCost = …; table.InnerPowerCost = …; BalanceTable.Current = table;`로 바꾸고 `[TearDown]`(또는 `finally`)에서 `BalanceTable.Current = BalanceTable.Default`로 되돌린다. 비용 0의 `Wallet.TrySpend(0)`은 `true`이고 알림이 없다.
- 테스트의 공통 준비: 따로 적지 않으면 `wallet = new Wallet(0)`, `upgrades = new StatUpgrades(wallet, 10, 100)`, 기본 수치 표(비용 `20 + 5 × 단계`).
- 구현하다 모양이 사양과 달라지면 `docs/spec/auto_upgrade.md`의 "연결 지점" 절을 고치고 보고서의 가정에 적는다. `docs/spec/stat_upgrade.md` "연결 지점"에 "자동 강화는 `docs/spec/auto_upgrade.md`" 한 줄을 더한다.

## 완료 조건
- `Assets/Tests/EditMode/StatUpgrade/AutoUpgradeTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 1. `new AutoUpgrade(upgrades, wallet)`의 `Enabled`는 `true`이고, 만들 때 금화와 단계는 바뀌지 않는다.
- 2. 켜진 상태에서 `wallet.Add(20)`이면 무력 단계 1, 체력·내공 0, 금화 0이다.
- 3. 켜진 상태에서 `wallet.Add(100)`이면 무력·체력·내공 단계가 각각 2, 1, 1이고 금화는 15다 (20 + 20 + 20 + 25를 산 뒤 남은 15로는 다음 비용 25를 못 산다).
- 4. 꺼진 상태(`enabled: false`)에서 `wallet.Add(100)`이면 세 단계는 0이고 금화는 100이다.
- 5. 4번 상태에서 `SetEnabled(true)`를 부르면 `EnabledChanged`가 `true`를 담아 1번 오고, 그 뒤 세 단계는 2, 1, 1, 금화는 15다.
- 6. `upgrades.SetLocked(true)` 뒤 `wallet.Add(20)`이면 단계는 모두 0, 금화 20이다. 이어서 `SetLocked(false)`면 무력 단계 1, 금화 0이다.
- 7. 금화 0에서 `BuyAll()`은 0을 돌려준다.
- 8. `new StatUpgrades(wallet, 10, 100, 1, 0, 0)`(무력 비용 25)에서 `wallet.Add(20)`이면 체력 단계가 1이 된다 (더 싼 것 먼저).
- 9. `SetEnabled(true)`를 켜진 상태에서 부르면 `EnabledChanged`는 0번 온다.
- 10. `SetEnabled(false)` 뒤 `wallet.Add(100)`이면 사지 않는다.
- 11. 수치 표의 세 비용을 (0, 0, 1)로 바꾸고 `wallet.Add(1)`이면 이번 알림의 구매 수는 1000이다 (세 단계 합 1000). 테스트 끝에 표를 `Default`로 되돌린다.
- 12. `new AutoUpgrade(null, wallet)`과 `new AutoUpgrade(upgrades, null)`은 각각 `ArgumentNullException`을 던진다.
- 13. `Dispose()` 뒤 `wallet.Add(100)`이면 사지 않는다. `Dispose()`를 두 번 불러도 예외가 없다.
- 14. `AutoUpgradeText.Label(true)`는 `"자동 강화 켜짐"`, `Label(false)`는 `"자동 강화 꺼짐"`이다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `StatUpgrades`, `Wallet`의 내부 코드를 고치지 않는다. 공개된 연결 지점(`CanUpgrade`, `GetCost`, `TryUpgrade`, `LockedChanged`, `BalanceChanged`)만 쓴다.
- 화면(`AutoUpgradeView`)과 `GameRoot` 조립은 만들지 않는다 (092).
- 저장 칸은 만들지 않는다 (`docs/spec/regression.md`, 095).
- 능력치 비율로 사기, 특정 능력치만 자동 같은 다른 규칙을 더하지 않는다.
- `UnityEngine`을 쓰지 않는다. 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 가장 싼 강화 자동 구매, 동률 순서, 재진입 방지, 구매 횟수 제한과 구독 해제를 구현했다.
- 버튼 글자와 완료 조건별 에딧 모드 테스트를 추가하고 강화 사양에 자동 강화 연결을 명시했다.

### 바뀐 파일
- `Assets/Scripts/Core/StatUpgrade/AutoUpgrade.cs`, `AutoUpgradeText.cs` 및 각 `.meta`
- `Assets/Tests/EditMode/StatUpgrade/AutoUpgradeTests.cs` 및 `.meta`
- `docs/spec/stat_upgrade.md`
- `tasks/doing/091_auto_upgrade.md`, `.loop/result.json`

### 가정
- 구매 알림을 받는 외부 코드가 구매 도중 자동 강화를 끄면 현재 구매를 마친 뒤 반복을 중단한다.
- 선택 후 `TryUpgrade`가 실패하면 무한 반복을 피하도록 이번 구매를 종료한다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- 완료 조건과 재진입을 검증하는 에딧 모드 테스트를 작성했다. Unity 테스트는 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.
- `git diff --check` 통과 (추적 중인 변경 대상 기준).


## 루프 기록 (2026-11-22 16:37)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/091_auto_upgrade
- 루프 브랜치: loop/20261122-163234
