# 052_battle_realm_health — 경지 돌파 때 제자 현재 체력 증가

## 목표
`PlumSword.Core.Combat.Battle`이 생성자로 `RealmProgress`를 받아, 경지 돌파(`BrokeThrough`) 때 늘어난 최대 체력만큼 제자의 현재 체력을 늘리게 한다. Core 작업이다.

## 묶음
combat_m3

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/combat.md ("동작"의 "강화·경지 반영", "경계 상황", "연결 지점", "완료 조건 초안"의 63~67, "변경 기록")
- docs/spec/realm.md ("연결 지점" 절)
- docs/spec/stat_upgrade.md ("연결 지점" 절)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-19 — 3단계: 이십사수매화검법, 돌파 때 현재 체력 증가, 공격 속도 제거 반영` 항목 중 **경지 돌파 때 현재 체력 증가** 부분만 구현한다. 그 밖의 규칙은 이미 구현되어 있고, 스킬은 053·054가 한다.
- `Assets/Scripts/Core/Combat/Battle.cs`:
  - 생성자 끝에 `PlumSword.Core.Realm.RealmProgress realm = null`을 더한다. 모양: `public Battle(StatUpgrades upgrades, RealmProgress realm = null)`. 053이 이 뒤에 `bool skillEnabled = false`를 더할 것이다.
  - `realm`이 `null`이 아니면 생성자에서 `realm.BrokeThrough`를 구독한다. `null`이면 구독하지 않는다 (예외 없음). `upgrades`가 `null`이면 지금처럼 `ArgumentNullException`.
  - `BrokeThrough`를 받으면 지금의 체력 강화 처리(`OnUpgraded`의 `StatType.Health` 경우)와 같은 일을 한다: `늘어난 양 = upgrades.MaxHealth − 기억해 둔 직전 최대 체력(_lastMaxHealth)`, 기억을 새 값으로 바꾸고, `IsDiscipleDefeated`가 `false`이면 `DiscipleHealth`를 그만큼 늘린다. 패배한 상태면 기억만 바꾸고 체력은 0 그대로다.
  - `BrokeThrough`를 받을 때 `realm.Current`는 이미 새 경지라 `upgrades.MaxHealth`는 새 값이다 (경지 보너스는 `StatUpgrades`가 이미 계산한다).
  - 멈춘 동안(`Paused`)에도 반영한다. 알림(이벤트)은 보내지 않는다.
  - 두 처리(체력 강화, 돌파)는 같은 비공개 메서드를 함께 써도 된다.
  - 생성자 주석에 `realm` 설명을 더한다.
- 수치(참고, 다른 기능의 것): 경지 보너스 삼류 0%, 이류 10%, 일류 20%, 절정 30%, 초절정 40%. 돌파 비용 삼류→이류 200, 이류→일류 500, 일류→절정 1000, 절정→초절정 2000. `StatUpgrades.MaxHealth` = `(baseMaxHealth + 20 × 체력 단계) × (100 + 보너스) / 100` (`long` 정수 나눗셈). 일반 몬스터(1스테이지) 공격력 5, 보스(1스테이지) 공격력 10.
- 테스트에서 "경지 전투"는 `wallet = new Wallet(1000)`, `realm = new RealmProgress(wallet)`, `upgrades = new StatUpgrades(wallet, 10, 100, 0, 0, 0, realm)`, `battle = new Battle(upgrades, realm)`이다. "n칸 뒤"는 처음부터 `Step()`을 모두 n번 부른 뒤다.
- 새 테스트는 `Assets/Tests/EditMode/Combat/BattleRealmTests.cs`(네임스페이스 `PlumSword.Core.Tests.Combat`)에 쓴다.
- 기존 테스트는 고치지 않는다. `realm` 기본값이 `null`이라 기존 전투·스테이지·저장 테스트는 그대로 통과해야 한다.
- 구현하다 연결 지점 모양이 사양과 달라지면 사양 문서 `docs/spec/combat.md`의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/Combat/BattleRealmTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 경지 전투에서 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하면 `DiscipleHealth`는 90이고, 이어서 `realm.TryBreakthrough()` 뒤 `DiscipleMaxHealth`는 110, `DiscipleHealth`는 100이다.
- 바로 위 상태에서 `upgrades.TryUpgrade(StatType.Health)` 뒤 `DiscipleMaxHealth`는 132, `DiscipleHealth`는 122다.
- 지갑을 `new Wallet(10000)`으로 한 경지 전투에서 `realm.TryBreakthrough()`를 4번 부르면 `DiscipleMaxHealth`는 140, `DiscipleHealth`는 140이다.
- 경지 전투를 `new StatUpgrades(wallet, 10, 10, 0, 0, 0, realm)`로 만들고 `AddEnemy(Normal, 1, 0.8)`을 2번 부른 뒤 1칸 뒤 `IsDiscipleDefeated`는 `true`, `DiscipleHealth`는 0이고, 이어서 `realm.TryBreakthrough()` 뒤 `DiscipleHealth`는 0 그대로, `DiscipleMaxHealth`는 11이다.
- 바로 위 상태에서 이어서 `Reset()` 뒤 `DiscipleHealth`는 11이다.
- 경지 전투의 `upgrades`로 `new Battle(upgrades)`(realm 없이)를 만들고 `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행한 뒤 `realm.TryBreakthrough()`를 부르면 `DiscipleHealth`는 90 그대로, `DiscipleMaxHealth`는 110이다.
- 경지 전투에서 `SetPaused(true)` 뒤 `realm.TryBreakthrough()`를 부르면 `DiscipleHealth`는 110이다.
- `new Battle(upgrades, null)`은 예외 없이 만들어지고 `DiscipleHealth`는 100이다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 경지 기능의 코드 `Assets/Scripts/Core/Realm/`과 강화 기능의 코드 `Assets/Scripts/Core/StatUpgrade/`는 고치지 않는다. `RealmProgress`는 생성자, `BrokeThrough`, `TryBreakthrough`(테스트에서만)로만 쓴다.
- 스킬(`skillEnabled`, `SkillCooldown`, `SkillUsed` 등)은 만들지 않는다. 053·054가 한다.
- 화면 코드 `Assets/Scripts/Game/`과 `GameRoot`는 고치지 않는다. 055가 한다.
- 저장(경지 저장)은 하지 않는다.
- `UnityEngine`을 쓰지 않는다.

## 보고서

### 한 일
- 전투 생성자에서 선택적으로 경지 돌파 알림을 구독하고, 체력 강화와 같은 최대 체력 차이를 현재 체력에 반영한다. 패배 중에는 직전 최대 체력만 갱신한다.
- 돌파, 강화, 연속 돌파, 패배와 초기화, 구독 생략, 멈춤, null 경지 및 알림 부재를 검사하는 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/Combat/Battle.cs`
- `Assets/Tests/EditMode/Combat/BattleRealmTests.cs` 및 `.meta`
- `tasks/doing/052_battle_realm_health.md` (보고서)
- `.loop/result.json`

### 가정
- 전투 사양의 생성자에는 후속 작업 053의 `skillEnabled` 인자가 미리 포함되어 있다. 이번 작업은 명시된 중간 단계인 `Battle(StatUpgrades upgrades, RealmProgress realm = null)`까지만 구현하며 사양은 후속 작업을 위해 유지한다.
- 그 밖에 없음.

### 사람이 확인할 것
없음.

### 테스트 결과
- `git diff --check` 통과.
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 신규 및 기존 테스트의 실행과 결과 판정은 루프 스크립트를 기다린다.


## 루프 기록 (2026-11-19 18:17)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/052_battle_realm_health
- 루프 브랜치: loop/20261119-181417
