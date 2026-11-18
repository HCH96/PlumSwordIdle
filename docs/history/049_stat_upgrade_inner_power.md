# 049_stat_upgrade_inner_power — 내공 강화 추가 (로직·표시 글자)

## 목표
`PlumSword.Core.StatUpgrade`에 세 번째 능력치 내공(`StatType.InnerPower`)을 더한다. 내공 단계를 생성자로 되살리고, 공격력·체력과 같은 비용 규칙으로 강화하며, 표시 이름 `INNER`를 내준다. Core 작업이다.

## 묶음
stat_upgrade_m3

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stat_upgrade.md ("동작", "경계 상황", "수치", "연결 지점", "완료 조건 초안", "변경 기록")
- docs/spec/currency.md ("연결 지점" 절)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-18 — 3단계: 공격 속도 제거, 내공 강화 추가, 경지 보너스 적용` 항목 중 내공 부분만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. 공격 속도 제거는 이미 끝났다 (main 의 `StatType`에 `AttackSpeed`가 없다). 경지 보너스(`realm` 인자, `Attack`·`MaxHealth`의 보너스)는 050 이 한다.
- `Assets/Scripts/Core/StatUpgrade/StatType.cs`: `InnerPower = 2`를 더한다. 값은 `Attack = 0`, `Health = 1`, `InnerPower = 2`.
- `Assets/Scripts/Core/StatUpgrade/StatUpgrades.cs`:
  - 생성자 끝(`healthLevel` 뒤)에 `int innerPowerLevel = 0`을 더한다. 모양: `public StatUpgrades(Wallet wallet, long baseAttack, long baseMaxHealth, int attackLevel = 0, int healthLevel = 0, int innerPowerLevel = 0)`. 음수면 0으로 만든다 (예외 없음). 만드는 동안 금화를 쓰지 않고 알림을 보내지 않는다. `realm` 인자는 이 작업에서 더하지 않는다 (050).
  - `GetLevel(InnerPower)`는 내공 단계, `GetMaxLevel(InnerPower)`는 `int.MaxValue`, `GetCost(InnerPower)`는 `20 + 5 × 내공 단계` (`long`으로 계산), `IsMaxLevel`·`CanUpgrade`·`TryUpgrade`는 공격력·체력과 같은 순서와 규칙(잠김 → 최대 단계 → `wallet.TrySpend(GetCost)` → 단계 +1 → `Upgraded(InnerPower)`)으로 동작한다.
  - 내공 단계는 `Attack`, `MaxHealth`에 아무 영향을 주지 않는다. 내공의 최종 능력치 속성은 만들지 않는다 (전투가 `GetLevel(StatType.InnerPower)`를 읽는다).
  - 정의되지 않은 값(예: `(StatType)99`)은 지금처럼 `GetLevel`, `GetMaxLevel`, `GetCost`, `IsMaxLevel`, `CanUpgrade`, `TryUpgrade` 모두 `ArgumentOutOfRangeException`이다. `InnerPower`는 정의된 값으로 다룬다 (`ThrowIfUndefined` 포함).
  - 클래스와 생성자 주석을 세 능력치에 맞게 고친다.
- `Assets/Scripts/Core/StatUpgrade/StatUpgradeText.cs`: `Name(InnerPower)`는 `"INNER"`. `ButtonLabel(InnerPower, 1, 25, false)`는 `"INNER\nLv.1\nG 25"`. 주석 예시에 `INNER`를 더한다.
- 기존 테스트 (`Assets/Tests/EditMode/StatUpgrade/`). 모두 메서드 이름은 그대로 두고, 검사(Assert)를 지우거나 줄이지 않는다. 더하기만 한다:
  - `StatUpgradesTests.AllStats`: `{ StatType.Attack, StatType.Health, StatType.InnerPower }`로 바꾼다. 이것을 쓰는 `New_Default_InitialState`, `CanUpgrade_GoldEqualsCost_TrueForAllWithoutChange`, `CanUpgrade_GoldBelowCost_FalseForAllWithoutChange`, `New_NegativeLevels_ClampedToZeroWithoutException`, `UndefinedStat_EveryMethod_ThrowsOutOfRangeWithoutChange`가 내공까지 확인하게 된다.
  - `StatUpgradesTests.GetMaxLevel_EachStat_ReturnsLimit`: `GetMaxLevel(StatType.InnerPower)`가 `int.MaxValue`인지 확인을 더한다.
  - `StatUpgradesTests.TryUpgrade_Attack_OtherStatsUnchanged`: `GetLevel(StatType.InnerPower)` 0, `GetCost(StatType.InnerPower)` 20 확인을 더한다.
  - `StatUpgradesTests.New_WithLevels_RestoresLevelsWithoutSpending`: 생성자를 `new StatUpgrades(wallet, 10, 100, 3, 2, 4)`로 바꾸고 `GetLevel(StatType.InnerPower)` 4, `GetCost(StatType.InnerPower)` 40 확인을 더한다. 있던 기대값(공격력 단계 3, 체력 단계 2, `Attack` 16, `MaxHealth` 140, 비용 35·30, 금화 1000, 알림 0번)은 그대로다.
  - `StatUpgradesTests.New_NegativeLevels_ClampedToZeroWithoutException`: 생성자를 `new StatUpgrades(new Wallet(), 10, 100, -1, -5, -2)`로 바꾼다.
  - `StatUpgradeTextTests.Name_DefinedStat_ReturnsEnglishAbbreviation`: `[TestCase(StatType.InnerPower, "INNER")]`를 더한다.
  - `StatUpgradeTextTests.ButtonLabel_Values_JoinsNameLevelCostWithNewline`: `[TestCase(StatType.InnerPower, 1, 25L, false, "INNER\nLv.1\nG 25")]`를 더한다.
  - 그 밖의 `StatUpgrades`·`Battle`·스테이지·저장 테스트는 고치지 않는다 (생성자의 새 인자는 기본값이 있어 그대로 컴파일된다).
- 새 내공 테스트는 `Assets/Tests/EditMode/StatUpgrade/StatUpgradesInnerPowerTests.cs`(네임스페이스 `PlumSword.Core.Tests.StatUpgrade`)에 쓴다.
- 구현하다 연결 지점 모양이 사양과 달라지면 사양 문서 `docs/spec/stat_upgrade.md`의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/StatUpgrade/StatUpgradesTests.cs`, `StatUpgradeTextTests.cs`, `StatUpgradesInnerPowerTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 새로 만들면(`new StatUpgrades(wallet, 10, 100)`) `GetLevel(InnerPower)`는 0, `GetCost(InnerPower)`는 20, `IsMaxLevel(InnerPower)`는 `false`다.
- `GetMaxLevel(InnerPower)`는 `int.MaxValue`다.
- 금화 1000에서 `TryUpgrade(Attack)`을 1번 부른 뒤 `GetLevel(InnerPower)`는 0, `GetCost(InnerPower)`는 20이다.
- 금화 20에서 `CanUpgrade(InnerPower)`는 `true`이고, 부른 뒤 금화는 20, `GetLevel(InnerPower)`는 0, `Upgraded`와 `BalanceChanged`는 0번 온다.
- 금화 19에서 `CanUpgrade(InnerPower)`는 `false`이고, 부른 뒤 금화는 19, `GetLevel(InnerPower)`는 0, `Upgraded`와 `BalanceChanged`는 0번 온다.
- `new StatUpgrades(wallet, 10, 100, 3, 2, 4)`의 `GetLevel(InnerPower)`는 4, `GetCost(InnerPower)`는 40이고, 만드는 동안 금화는 바뀌지 않고 알림은 0번 온다.
- `new StatUpgrades(wallet, 10, 100, -1, -5, -2)`의 `GetLevel(InnerPower)`는 0이고 예외가 없다.
- 금화 1000에서 `TryUpgrade(InnerPower)`를 3번 부르면 모두 `true`이고, 부르기 전의 `GetCost(InnerPower)`는 순서대로 20, 25, 30이다.
- 위 3번 강화가 끝난 뒤 `GetLevel(InnerPower)`는 3, `GetCost(InnerPower)`는 35, 금화는 925다.
- 위 3번 강화가 끝난 뒤 `Attack`은 10, `MaxHealth`는 100, `GetLevel(Attack)`과 `GetLevel(Health)`는 0이다.
- 금화 1000에서 `TryUpgrade(InnerPower)`를 부르면 `Wallet.BalanceChanged`가 `Upgraded`보다 먼저 오고, `BalanceChanged`를 받는 시점의 `GetLevel(InnerPower)`는 0이다.
- 금화 1000에서 `TryUpgrade(InnerPower)`를 부르면 `Upgraded`가 `StatType.InnerPower`를 담아 1번 오고, 받는 쪽에서 읽은 `GetLevel(InnerPower)`는 1, `GetCost(InnerPower)`는 25다.
- 금화 19에서 `TryUpgrade(InnerPower)`는 `false`이고 단계 0, 금화 19, `Upgraded`와 `BalanceChanged`는 0번 온다.
- 금화 1000에서 `SetLocked(true)` 뒤 `CanUpgrade(InnerPower)`와 `TryUpgrade(InnerPower)`는 `false`이고 금화는 1000, `GetLevel(InnerPower)`는 0이다.
- `new StatUpgrades(wallet, 10, 100, 0, 0, int.MaxValue)`의 `GetLevel(InnerPower)`는 `int.MaxValue`, `IsMaxLevel(InnerPower)`는 `true`, `GetCost(InnerPower)`는 10737418255다.
- 위처럼 만들고 금화가 `long.MaxValue`일 때 `TryUpgrade(InnerPower)`는 `false`이고 금화는 `long.MaxValue` 그대로다.
- `(StatType)99`로 `GetLevel`, `GetMaxLevel`, `GetCost`, `IsMaxLevel`, `CanUpgrade`, `TryUpgrade`를 부르면 모두 `ArgumentOutOfRangeException`을 던지고, 그 뒤 금화와 세 능력치 단계(`InnerPower` 포함)는 그대로다.
- `StatUpgradeText.Name(InnerPower)`는 `"INNER"`다.
- `StatUpgradeText.ButtonLabel(InnerPower, 1, 25, false)`는 `"INNER\nLv.1\nG 25"`다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `realm` 인자와 경지 보너스는 더하지 않는다 (050 이 한다). `Attack`, `MaxHealth`의 공식은 바꾸지 않는다.
- 화면(`Assets/Scripts/Game/`)은 고치지 않는다 (051 이 한다).
- 다른 기능의 내부 코드: `Assets/Scripts/Core/Combat/`, `Assets/Scripts/Core/Save/`, `Assets/Scripts/Core/Stage/`, `Assets/Scripts/Core/Realm/`, `Assets/Scripts/Core/Currency/`는 고치지 않는다. 지갑은 `Wallet`의 연결 지점(`CanSpend`, `TrySpend`, `BalanceChanged`)으로만 쓴다.
- 저장 데이터(`SaveData`, `SaveCodec`)에 내공 칸을 만들지 않는다 (저장 기능의 일).
- 있던 테스트 메서드를 지우거나 이름을 바꾸거나 검사(Assert)를 줄이지 않는다.
- `UnityEngine`을 쓰지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의는 바꾸지 않는다.

## 보고서

### 한 일
- 내공 단계의 복원, 비용 조회, 강화 및 알림과 INNER 표시를 추가했다.
- 기존 테스트를 확장하고 내공 강화의 비용, 독립성, 알림 순서, 실패 및 최대 단계 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/StatUpgrade/StatType.cs`
- `Assets/Scripts/Core/StatUpgrade/StatUpgrades.cs`
- `Assets/Scripts/Core/StatUpgrade/StatUpgradeText.cs`
- `Assets/Tests/EditMode/StatUpgrade/StatUpgradesTests.cs`
- `Assets/Tests/EditMode/StatUpgrade/StatUpgradeTextTests.cs`
- `Assets/Tests/EditMode/StatUpgrade/StatUpgradesInnerPowerTests.cs` 및 `.meta`
- `tasks/doing/049_stat_upgrade_inner_power.md`, `.loop/result.json`

### 가정
- 없음. 사양의 경지 인자와 보너스는 작업 문서에 따라 후속 작업 050에서 구현한다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- `git diff --check` 통과.
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 실행과 판정을 기다린다.


## 루프 기록 (2026-11-18 14:19)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/049_stat_upgrade_inner_power
- 루프 브랜치: loop/20261118-141612
