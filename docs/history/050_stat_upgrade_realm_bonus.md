# 050_stat_upgrade_realm_bonus — 최종 능력치에 경지 보너스 적용

## 목표
`PlumSword.Core.StatUpgrade.StatUpgrades`가 생성자로 `RealmProgress`를 받아, `Attack`과 `MaxHealth`를 부를 때마다 경지 보너스(`BonusPercent`)를 반영한 값으로 돌려주게 한다. Core 작업이다.

## 묶음
stat_upgrade_m3

## 선행 작업
- 049_stat_upgrade_inner_power

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stat_upgrade.md ("동작"의 "최종 능력치", "경계 상황", "수치", "연결 지점", "완료 조건 초안", "변경 기록")
- docs/spec/realm.md ("연결 지점" 절)
- docs/spec/combat.md ("연결 지점" 절)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-18 — 3단계: 공격 속도 제거, 내공 강화 추가, 경지 보너스 적용` 항목 중 경지 보너스 부분만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. 내공(`StatType.InnerPower`, 생성자의 `innerPowerLevel`)은 049 가 이미 더했다.
- `Assets/Scripts/Core/StatUpgrade/StatUpgrades.cs`:
  - 생성자 끝(`innerPowerLevel` 뒤)에 `PlumSword.Core.Realm.RealmProgress realm = null`을 더한다. 모양: `public StatUpgrades(Wallet wallet, long baseAttack, long baseMaxHealth, int attackLevel = 0, int healthLevel = 0, int innerPowerLevel = 0, RealmProgress realm = null)`.
  - `realm`이 `null`이면 보너스 0%로 계산한다 (예외 없음).
  - `Attack` = `(baseAttack + 2 × 공격력 단계) × (100 + 보너스) / 100`.
  - `MaxHealth` = `(baseMaxHealth + 20 × 체력 단계) × (100 + 보너스) / 100`.
  - 보너스는 `realm.BonusPercent`(정수 %)를 `Attack`·`MaxHealth`를 부를 때마다 읽는다. 값을 저장해 두지 않는다. 그래서 `realm.TryBreakthrough()`가 성공하면 그 즉시 새 값이 된다.
  - 계산은 모두 `long`으로 한다: 단계를 `long`으로 바꾼 뒤 곱하고, 보너스 적용은 `long` 정수 나눗셈(= 소수점 내림)이다. 그 밖의 반올림은 없다. 공격력 `int.MaxValue` 단계와 초절정(40%)에서도 넘치지 않는다 (`Attack` 6012954225, `MaxHealth` 60129542256).
  - 이 클래스는 `realm`을 구독하지 않고 바꾸지도 않는다. 돌파 때 `Upgraded`나 다른 알림을 보내지 않는다.
  - 내공 단계는 `Attack`, `MaxHealth`에 영향이 없다.
  - 클래스와 생성자, `Attack`·`MaxHealth`의 주석을 새 공식에 맞게 고친다.
- 경지 수치(경지 기능의 것, 참고): 삼류 0%, 이류 10%, 일류 20%, 절정 30%, 초절정 40%. 테스트에서는 같은 `wallet`으로 `new RealmProgress(wallet, startLevel)`을 만든다 (`startLevel` 0 = 삼류, 1 = 이류, 3 = 절정, 4 = 초절정). 삼류 → 이류 돌파 비용은 200이다.
- 새 테스트는 `Assets/Tests/EditMode/StatUpgrade/StatUpgradesRealmTests.cs`(네임스페이스 `PlumSword.Core.Tests.StatUpgrade`)에 쓴다. 전투 확인(아래 `Battle` 조건)도 이 파일에 쓴다. `Battle`은 `PlumSword.Core.Combat.Battle`이고 `new Battle(upgrades)`로 만들며 `DiscipleHealth`, `DiscipleMaxHealth`를 읽는다 (이미 있는 코드다. 고치지 않는다).
- 기존 테스트는 고치지 않는다. `realm` 기본값이 `null`(보너스 0%)이라 기존 `StatUpgrades`·`Battle`·스테이지·저장 테스트의 기대값은 그대로 통과해야 한다.
- 구현하다 연결 지점 모양이 사양과 달라지면 사양 문서 `docs/spec/stat_upgrade.md`의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/StatUpgrade/StatUpgradesRealmTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 이류(`new RealmProgress(wallet, 1)`)로 만든 `new StatUpgrades(wallet, 10, 100, 0, 0, 0, realm)`의 `Attack`은 11, `MaxHealth`는 110이다.
- 이류(`startLevel` 1)에서 단계 (1, 1, 0)으로 만들면 `Attack`은 13, `MaxHealth`는 132다.
- 절정(`startLevel` 3)에서 단계 (1, 0, 0)으로 만들면 `Attack`은 15, `MaxHealth`는 130이다.
- 초절정(`startLevel` 4)에서 단계 (3, 2, 5)로 만들면 `Attack`은 22, `MaxHealth`는 196이다.
- 초절정(`startLevel` 4)에서 단계 (3, 2, 0)으로 만들어도 `Attack`은 22, `MaxHealth`는 196이다 (내공은 영향 없음).
- 삼류(`startLevel` 0)에서 단계 (0, 0, 0)으로 만들면 `Attack`은 10, `MaxHealth`는 100이다.
- `new StatUpgrades(wallet, 10, 100, 1, 1, 0, null)`은 예외 없이 만들어지고 `Attack`은 12, `MaxHealth`는 120이다.
- 금화 200에서 `realm = new RealmProgress(wallet)`, `new StatUpgrades(wallet, 10, 100, 1, 1, 0, realm)`의 `Attack`은 12, `MaxHealth`는 120이다.
- 위 상태에서 `realm.TryBreakthrough()`가 `true`를 돌려준 뒤 `Attack`은 13, `MaxHealth`는 132, 금화는 0이다.
- 위 돌파 동안 `upgrades.Upgraded`는 0번 온다.
- 금화 1000에서 이류(`startLevel` 1)로 만든 `new StatUpgrades(wallet, 10, 100, 0, 0, 0, realm)`의 `MaxHealth`는 110이고, `TryUpgrade(Health)`는 `true`이며, `Upgraded`를 받는 쪽에서 읽은 `MaxHealth`는 132, 강화 뒤 금화는 980이다.
- 바로 위처럼 만든 `upgrades`로 `new Battle(upgrades)`를 만들면 `DiscipleHealth`와 `DiscipleMaxHealth`는 110이다.
- 그 `Battle`에서 `upgrades.TryUpgrade(Health)` 뒤 `DiscipleHealth`와 `DiscipleMaxHealth`는 132다 (22 늘어남).
- 이어서 `TryUpgrade(Attack)`과 `TryUpgrade(InnerPower)`를 불러도 `DiscipleHealth`는 132 그대로다.
- 초절정(`startLevel` 4)에서 `new StatUpgrades(wallet, 10, 100, int.MaxValue, int.MaxValue, 0, realm)`은 예외 없이 만들어지고 `Attack`은 6012954225, `MaxHealth`는 60129542256이다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 경지 기능의 코드 `Assets/Scripts/Core/Realm/`은 고치지 않는다. `RealmProgress`는 생성자, `BonusPercent`, `TryBreakthrough`(테스트에서만)로만 쓴다.
- 전투 코드 `Assets/Scripts/Core/Combat/`은 고치지 않는다. 체력 증가 처리는 이미 `MaxHealth`의 차이로 동작한다.
- `RealmProgress`의 이벤트(`BrokeThrough` 등)를 구독하지 않는다. 돌파 때 알림을 보내지 않는다.
- 화면과 조립(`Assets/Scripts/Game/`, `GameRoot`)은 고치지 않는다 (051 이 한다).
- 저장(`Assets/Scripts/Core/Save/`)은 고치지 않는다.
- 있던 테스트 메서드를 지우거나 이름을 바꾸거나 검사(Assert)를 줄이지 않는다.
- `UnityEngine`을 쓰지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의는 바꾸지 않는다.

## 보고서

### 한 일
- 공격력과 최대 체력을 읽을 때마다 현재 경지 보너스를 long 정수 계산으로 적용하고, 경지가 없으면 0%를 적용했다.
- 경지별 계산, 내공의 무관성, 돌파와 알림, 체력 강화와 전투 연동, 최대 단계를 검증하는 에딧 모드 테스트를 추가했다.
- 기존 사양의 공식과 연결 지점에 맞게 구현했으며 사양 변경은 필요하지 않았다.

### 바뀐 파일
- Assets/Scripts/Core/StatUpgrade/StatUpgrades.cs
- Assets/Tests/EditMode/StatUpgrade/StatUpgradesRealmTests.cs
- Assets/Tests/EditMode/StatUpgrade/StatUpgradesRealmTests.cs.meta
- tasks/doing/050_stat_upgrade_realm_bonus.md (보고서)
- .loop/result.json

### 가정
없음.

### 사람이 확인할 것
없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프의 전체 테스트 판정을 기다린다.
- git diff --check 통과.


## 루프 기록 (2026-11-18 14:22)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/050_stat_upgrade_realm_bonus
- 루프 브랜치: loop/20261118-141612
