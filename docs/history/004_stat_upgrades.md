# 004_stat_upgrades — 능력치 강화 로직 (단계·비용·구매·최종 능력치·잠금)

## 목표
`PlumSword.Core` 에 능력치 종류 `StatType` 과 강화 로직 `StatUpgrades` 를 만든다. 세 능력치(공격력·체력·공격 속도)의 강화 단계와 비용, 금화를 쓰는 강화(`TryUpgrade`), 강화가 반영된 최종 능력치(`Attack`, `MaxHealth`, `AttackInterval`), 잠금(`SetLocked`)을 제공하고 에딧 모드 테스트로 검사한다.

## 묶음
강화

## 선행 작업
- 001_wallet

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stat_upgrade.md ("동작", "경계 상황", "수치", "연결 지점" 절)
- docs/spec/currency.md ("연결 지점" 절의 `Wallet`)

## 요구사항
- 만들 파일 (파일 하나에 공개 타입 하나, 네임스페이스 `PlumSword.Core.StatUpgrade`):
  - `Assets/Scripts/Core/StatUpgrade/StatType.cs`
  - `Assets/Scripts/Core/StatUpgrade/StatUpgrades.cs`
- 테스트 파일: `Assets/Tests/EditMode/StatUpgrade/StatUpgradesTests.cs`. 네임스페이스 `PlumSword.Core.Tests.StatUpgrade`, 클래스 `StatUpgradesTests`. 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다.
- 선행 작업 001_wallet 이 만든 `PlumSword.Core.Currency.Wallet` 을 쓴다. 내부는 고치지 않는다. 쓰는 것:
  - `new Wallet(long initialBalance = 0)`, `long Balance { get; }`
  - `bool CanSpend(long amount)`: 잔액이 `amount` 이상이면 `true`. 아무것도 바꾸지 않는다.
  - `bool TrySpend(long amount)`: 잔액이 `amount` 이상이면 줄이고 `true`, 모자라면 그대로 두고 `false`.
  - `event System.Action<long> BalanceChanged`: 잔액이 실제로 바뀐 뒤에 한 번 온다.
  - 테스트에서 "금화 1000"은 `new Wallet(1000)` 으로 만든다.
- 공개 모양은 사양의 "연결 지점" 절 그대로다. 뒤 작업(005_stat_upgrade_text, 006_stat_upgrade_view)과 전투·스테이지 기능이 이 이름으로 부른다.

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

          public StatUpgrades(
              PlumSword.Core.Currency.Wallet wallet,
              long baseAttack,
              long baseMaxHealth,
              double baseAttackInterval,
              int attackLevel = 0,
              int healthLevel = 0,
              int attackSpeedLevel = 0);

          public int GetLevel(StatType stat);
          public int GetMaxLevel(StatType stat);
          public bool IsMaxLevel(StatType stat);
          public long GetCost(StatType stat);
          public bool CanUpgrade(StatType stat);
          public bool TryUpgrade(StatType stat);

          public long Attack { get; }
          public long MaxHealth { get; }
          public double AttackInterval { get; }

          public event System.Action<StatType> Upgraded;

          public bool Locked { get; }
          public void SetLocked(bool locked);
          public event System.Action<bool> LockedChanged;
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/stat_upgrade.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 사양에 없는 공개 멤버를 더하지 않는다.
- **타입**: 단계는 `int`, 비용·공격력·최대 체력은 `long`, 공격 간격은 `double`(초). 계산은 단계를 `long` 으로 바꾼 뒤 곱한다 (`int` 넘침이 없어야 한다). 반올림하지 않는다.
- **생성자**:
  - `wallet` 이 `null` 이면 `System.ArgumentNullException`.
  - `baseAttack` 또는 `baseMaxHealth` 가 1보다 작으면 `System.ArgumentOutOfRangeException`.
  - `baseAttackInterval` 이 0.5보다 작거나 `NaN` 이거나 무한대(양·음)면 `System.ArgumentOutOfRangeException`.
  - 단계 인자 셋(`attackLevel`, `healthLevel`, `attackSpeedLevel`)은 저장된 값으로 되살릴 때 쓴다. 음수면 0으로, `attackSpeedLevel` 이 10보다 크면 10으로 만든다 (예외 없음).
  - 생성자는 금화를 쓰지 않고 알림을 보내지 않는다. 잠기지 않은 상태(`Locked == false`)로 시작한다.
- **단계**: 세 능력치가 단계를 따로 가진다. `GetLevel(stat)` 은 지금 단계다. 한 능력치를 강화해도 다른 능력치의 단계와 비용은 바뀌지 않는다.
- **최대 단계** `GetMaxLevel(stat)`: `AttackSpeed` 는 10(`AttackSpeedMaxLevel`), `Attack` 과 `Health` 는 `int.MaxValue`. `IsMaxLevel(stat)` 은 `GetLevel(stat) >= GetMaxLevel(stat)`.
- **비용** `GetCost(stat)`: `20 + 5 × 단계` (임시값). 세 능력치 모두 같은 식이다. 단계 0·1·2·3 → 20·25·30·35. 최대 단계여도 같은 식의 값을 돌려준다 (예외 없음. 공격 속도 10단계 → 70).
- **최종 능력치** (부를 때마다 시작값과 지금 단계로 계산한다. 값을 따로 저장해 두지 않는다):
  - `Attack` = `baseAttack + 2 × 공격력 단계` (임시값). 시작 10 기준 단계 0·1·2 → 10·12·14.
  - `MaxHealth` = `baseMaxHealth + 20 × 체력 단계` (임시값). 시작 100 기준 단계 0·1·2 → 100·120·140.
  - `AttackInterval` = `Math.Max(0.5, baseAttackInterval - 0.05 × 공격 속도 단계)` (임시값). 시작 1.0 기준 단계 0·1·3·10 → 1.0·0.95·0.85·0.5. 0.5는 `MinAttackInterval` 상수를 쓴다.
- **확인** `CanUpgrade(stat)`: 잠겨 있지 않고, 최대 단계가 아니고, `wallet.CanSpend(GetCost(stat))` 가 `true` 일 때만 `true`. 아무것도 바꾸지 않고 알림도 보내지 않는다.
- **강화** `TryUpgrade(stat)`: 아래 순서로 처리한다.
  1. 잠겨 있으면(`Locked`) `false`.
  2. 그 능력치가 최대 단계면 `false` (금화가 충분해도).
  3. `wallet.TrySpend(GetCost(stat))` 가 `false` 면(금화 부족) `false`.
  4. 그 능력치의 단계를 1 올린다.
  5. `Upgraded` 를 그 능력치를 담아 한 번 보내고 `true` 를 돌려준다.
  - `false` 를 돌려준 경우에는 금화도 단계도 바뀌지 않고 알림(`Upgraded`, `Wallet.BalanceChanged`)도 없다.
  - 알림 순서: `Wallet.BalanceChanged`(3번에서) → 단계 증가 → `Upgraded`. `BalanceChanged` 를 받는 시점에는 단계가 아직 올라가기 전이다. `Upgraded` 를 받는 쪽에서 `GetLevel`, `GetCost`, `Attack`, `MaxHealth`, `AttackInterval` 을 읽으면 새 값이다.
  - 연달아 부르면 부를 때마다 그때의 금화와 단계로 따로 판정한다. 한 번 부르면 한 단계만 오른다.
  - 금화가 비용과 같으면 성공한다 (금화 0, 단계 +1).
- **잠금** `SetLocked(locked)`: `Locked` 를 바꾼다. 값이 실제로 바뀐 때에만, 바꾼 뒤에 `LockedChanged` 를 새 값을 담아 한 번 보낸다. 지금과 같은 값으로 부르면 아무 일도 없다 (알림 없음). 잠긴 동안에는 `TryUpgrade` 와 `CanUpgrade` 가 모두 `false` 다. 잠금은 단계와 금화를 바꾸지 않는다.
- **정의되지 않은 `StatType`** (예: `(StatType)99`): `GetLevel`, `GetMaxLevel`, `GetCost`, `IsMaxLevel`, `CanUpgrade`, `TryUpgrade` 모두 `System.ArgumentOutOfRangeException` 을 던진다. 금화와 단계는 그대로다. 이 검사는 다른 판정(잠금, 최대 단계, 금화)보다 먼저 한다. 그래서 잠긴 상태에서도 정의되지 않은 값은 예외다.
- 구독자가 없어도 `Upgraded`, `LockedChanged` 를 보내는 곳에서 예외가 나지 않아야 한다.
- 단계를 되돌리는 동작, 환불, 여러 단계 한꺼번에 강화는 없다.
- 시간·난수·저장을 쓰지 않는다. 저장은 2단계의 저장 기능이 생성자의 단계 인자와 `Upgraded` 로 처리하므로 이 작업에서 저장 코드를 만들지 않는다.
- 제자의 현재 체력과 공격 대기 시간은 이 클래스가 들고 있지 않는다 (전투 기능의 것).

## 완료 조건
아래에서 따로 적지 않으면 `new StatUpgrades(wallet, 10, 100, 1.0)` 으로 만든 것이고, "금화 N"은 `wallet = new Wallet(N)` 이다. 공격 간격은 오차 1e-9 안에서 비교한다.

- `Assets/Tests/EditMode/StatUpgrade/StatUpgradesTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 새로 만들면 세 능력치의 `GetLevel` 은 0, `GetCost` 는 20, `IsMaxLevel` 은 `false` 이고, `Attack` 은 10, `MaxHealth` 는 100, `AttackInterval` 은 1.0, `Locked` 는 `false` 다.
- `GetMaxLevel` 은 `AttackSpeed` → 10, `Attack` → `int.MaxValue`, `Health` → `int.MaxValue` 다.
- 금화 1000에서 `TryUpgrade(Attack)` 을 3번 부르면 모두 `true` 이고, 부르기 전의 `GetCost(Attack)` 은 순서대로 20, 25, 30이며, 끝난 뒤 `GetLevel(Attack)` 은 3, `GetCost(Attack)` 은 35, `Attack` 은 16, 금화는 925다.
- 금화 1000에서 `TryUpgrade(Attack)` 을 1번 부른 뒤 `GetLevel(Health)` 와 `GetLevel(AttackSpeed)` 는 0, `GetCost(Health)` 와 `GetCost(AttackSpeed)` 는 20, `MaxHealth` 는 100, `AttackInterval` 은 1.0이다.
- 금화 100에서 `TryUpgrade(Health)` 를 1번 부르면 `MaxHealth` 는 120이고 금화는 80이다. 한 번 더 부르면 `MaxHealth` 는 140이고 금화는 55다.
- 금화 1000에서 `TryUpgrade(AttackSpeed)` 를 1번 부르면 `AttackInterval` 은 0.95이고 금화는 980이다. 모두 3번 부르면 `AttackInterval` 은 0.85다.
- 금화 1000에서 `TryUpgrade(AttackSpeed)` 를 10번 부르면 모두 `true` 이고 `GetLevel(AttackSpeed)` 는 10, `AttackInterval` 은 0.5, `IsMaxLevel(AttackSpeed)` 는 `true`, 금화는 575다.
- 금화 1000에서 `TryUpgrade(AttackSpeed)` 를 10번 부른 뒤(금화 575) 한 번 더 부르면 `false` 이고 `GetLevel(AttackSpeed)` 는 10, 금화는 575 그대로이며 그 11번째 호출 동안 `Upgraded` 와 `Wallet.BalanceChanged` 는 0번 온다. 이때 `CanUpgrade(AttackSpeed)` 는 `false`, `GetCost(AttackSpeed)` 는 70이다.
- 금화 19에서 `TryUpgrade(Attack)` 은 `false` 이고 `GetLevel(Attack)` 은 0, 금화는 19이며 `Upgraded` 와 `Wallet.BalanceChanged` 는 0번 온다.
- 금화 20에서 `TryUpgrade(Attack)` 은 `true` 이고 `GetLevel(Attack)` 은 1, 금화는 0이며 `Wallet.BalanceChanged` 가 1번, `Upgraded` 가 1번 온다.
- 금화 50에서 `TryUpgrade(Attack)` 을 3번 부르면 결과는 순서대로 `true`, `true`, `false` 이고 `GetLevel(Attack)` 은 2, `Attack` 은 14, 금화는 5다.
- 금화 1000에서 `TryUpgrade(Health)` 를 부르면 `Upgraded` 가 `StatType.Health` 를 담아 1번 오고, 받는 쪽에서 읽은 `GetLevel(Health)` 는 1, `MaxHealth` 는 120, `GetCost(Health)` 는 25다.
- 금화 1000에서 `TryUpgrade(Attack)` 을 부르면 `Wallet.BalanceChanged` 가 `Upgraded` 보다 먼저 오고, `BalanceChanged` 를 받는 시점에 읽은 `GetLevel(Attack)` 은 0이다.
- 금화 20에서 `CanUpgrade` 는 세 능력치 모두 `true` 다. 금화 19에서는 세 능력치 모두 `false` 다. 두 경우 모두 부른 뒤 금화와 단계는 그대로이고 `Upgraded` 와 `Wallet.BalanceChanged` 는 0번 온다.
- `SetLocked(true)` 를 부르면 `Locked` 는 `true` 이고 `LockedChanged` 가 `true` 를 담아 1번 온다. 한 번 더 `SetLocked(true)` 를 부르면 `LockedChanged` 는 더 오지 않는다 (모두 1번).
- 금화 1000에서 `SetLocked(true)` 뒤 `CanUpgrade(Attack)` 은 `false`, `TryUpgrade(Attack)` 은 `false` 이고 금화는 1000, `GetLevel(Attack)` 은 0이며 `Upgraded` 는 0번 온다.
- 금화 1000에서 `SetLocked(true)` 뒤 `SetLocked(false)` 를 부르면 `LockedChanged` 가 `false` 를 담아 1번 오고 `Locked` 는 `false` 이며, 이어서 `TryUpgrade(Attack)` 은 `true` 다.
- 새로 만든 직후(`Locked` 가 `false`) `SetLocked(false)` 를 부르면 `LockedChanged` 는 0번 온다.
- `new StatUpgrades(wallet, 10, 100, 1.0, 3, 2, 4)` (금화 1000)의 `GetLevel` 은 `Attack`, `Health`, `AttackSpeed` 순서대로 3, 2, 4이고 `Attack` 은 16, `MaxHealth` 는 140, `AttackInterval` 은 0.8, `GetCost` 는 순서대로 35, 30, 40이다. 만든 뒤 금화는 1000 그대로이고, 만드는 동안 `Wallet.BalanceChanged` 는 0번 온다.
- `new StatUpgrades(wallet, 10, 100, 1.0, -1, -5, -2)` 의 단계는 모두 0이고 예외가 없다.
- `new StatUpgrades(wallet, 10, 100, 1.0, 0, 0, 11)` 의 `GetLevel(AttackSpeed)` 는 10이고 `AttackInterval` 은 0.5, `IsMaxLevel(AttackSpeed)` 는 `true` 이며 예외가 없다.
- `wallet = new Wallet(long.MaxValue)` 로 만든 `new StatUpgrades(wallet, 10, 100, 1.0, int.MaxValue, 0, 0)` 의 `IsMaxLevel(Attack)` 은 `true`, `Attack` 은 4294967304, `GetCost(Attack)` 은 10737418255이고, `TryUpgrade(Attack)` 은 `false` 이며 금화는 `long.MaxValue` 그대로다.
- `new StatUpgrades(wallet, 50, 300, 2.0, 1, 1, 10)` 의 `Attack` 은 52, `MaxHealth` 는 320, `AttackInterval` 은 1.5다.
- `new StatUpgrades(wallet, 10, 100, 0.7, 0, 0, 2)` 의 `AttackInterval` 은 0.6이고, `new StatUpgrades(wallet, 10, 100, 0.7, 0, 0, 10)` 의 `AttackInterval` 은 0.5다.
- `new StatUpgrades(null, 10, 100, 1.0)` 은 `ArgumentNullException` 을 던진다.
- 생성자는 `baseAttack` 이 0일 때, `baseMaxHealth` 가 0일 때, `baseAttackInterval` 이 0.49일 때, `double.NaN` 일 때, `double.PositiveInfinity` 일 때 각각 `ArgumentOutOfRangeException` 을 던진다 (나머지 인자는 10, 100, 1.0).
- 금화 1000에서 `(StatType)99` 로 `GetLevel`, `GetMaxLevel`, `GetCost`, `IsMaxLevel`, `CanUpgrade`, `TryUpgrade` 를 부르면 각각 `ArgumentOutOfRangeException` 을 던지고, 금화는 1000, 세 능력치의 단계는 0 그대로다.
- 금화 1000에서 `SetLocked(true)` 뒤 `(StatType)99` 로 `CanUpgrade` 와 `TryUpgrade` 를 부르면 각각 `ArgumentOutOfRangeException` 을 던진다.
- 기존 테스트가 모두 통과한다. (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다. `PlumSword.Core` 는 순수 C# 이다.
- `Assets/Scripts/Core/Currency/` 를 고치지 않는다. `Wallet` 은 공개된 모양으로만 쓴다.
- `StatUpgradeText`(버튼 글자)를 만들지 않는다. 005_stat_upgrade_text 가 한다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. `GameRoot.StatUpgrades`, `StatUpgradeView` 는 006_stat_upgrade_view 가 한다.
- 현재 체력, 체력 강화 때 현재 체력을 늘리는 처리, 공격 대기 시간, 페이드 때 잠금을 거는 코드를 만들지 않는다. 전투·스테이지 기능의 일이다.
- 저장·불러오기, 경지 돌파 보너스, 강화 되돌리기·환불, 여러 단계 한꺼번에 강화를 만들지 않는다. 사양의 "범위 밖"이다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `PlumSword.Core.StatUpgrade` 에 능력치 종류 `StatType`(Attack=0, Health=1, AttackSpeed=2)과 강화 로직 `StatUpgrades` 를 만들었다. 공개 모양은 사양 "연결 지점" 절 그대로다 (공개 멤버 추가 없음).
  - 생성자 검사(`null` 지갑, 시작값 1 미만, 공격 간격 0.5 미만·NaN·±무한대 → 예외), 단계 인자 잘라내기(음수 → 0, 공격 속도 10 초과 → 10).
  - `GetLevel`/`GetMaxLevel`/`IsMaxLevel`/`GetCost`(20 + 5 × 단계, `long` 계산), 최종 능력치 `Attack`/`MaxHealth`/`AttackInterval`(부를 때마다 계산).
  - `CanUpgrade`, `TryUpgrade`(정의되지 않은 값 검사 → 잠금 → 최대 단계 → `wallet.TrySpend` → 단계 +1 → `Upgraded`), `SetLocked`/`LockedChanged`(값이 바뀐 때만 알림).
- 에딧 모드 테스트 `StatUpgradesTests` 에 완료 조건마다 테스트를 하나 이상 썼다 (테스트 메서드 32개, 공격 간격 잘못된 값 4가지는 `TestCase`).

### 바뀐 파일
- `Assets/Scripts/Core/StatUpgrade/StatType.cs` (새 파일)
- `Assets/Scripts/Core/StatUpgrade/StatUpgrades.cs` (새 파일)
- `Assets/Tests/EditMode/StatUpgrade/StatUpgradesTests.cs` (새 파일)
- 위 파일과 폴더의 `.meta` (유니티가 테스트 실행 때 자동 생성: `Assets/Scripts/Core/StatUpgrade.meta`, `Assets/Tests/EditMode/StatUpgrade.meta`, 폴더 안 `.cs.meta`)

### 가정
- 수치(비용 20 + 5 × 단계, 공격력 +2, 최대 체력 +20, 공격 간격 −0.05, 최소 0.5초, 공격 속도 최대 10단계)는 작업 문서·사양의 임시값 그대로이며, 클래스 안의 `private const` 로 모아 두었다.
- 생성자의 `baseAttackInterval` 검사에서 `double.NegativeInfinity` 도 (0.5 미만이므로) 예외로 처리하고 테스트에 넣었다 (작업 문서의 "무한대(양·음)"와 일치).
- 예외 메시지 문구는 직접 정했다 (한국어 한 줄).
- 연결 지점은 사양과 같아 사양 문서는 고치지 않았다.

### 사람이 확인할 것
- 완료 조건에 [사람 확인] 항목 없음. 화면 코드는 이 작업 범위 밖이라 없음.

### 테스트 결과
- `python tools/loop/run_unity_tests.py` 실행: EditMode 68개 중 68개 통과, 0개 실패 (`CoreAssemblyTests.Core_DoesNotReferenceUnity`, 기존 금화 테스트 포함).


## 루프 기록 (2026-11-16 16:19)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/004_stat_upgrades
- 루프 브랜치: loop/20261116-161636
