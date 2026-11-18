# 051_stat_upgrade_view_inner — 강화 화면에 INNER 버튼, 조립에 경지 연결

## 목표
`PlumSword.Game`의 강화 화면(`StatUpgradeView`)에 세 번째 버튼 `INNER`를 더하고 아이콘이 없어도 그림 모드로 보이게 하며, `GameRoot`가 `StatUpgrades`를 만들 때 내공 단계 0과 `Realm`을 넘기게 한다. Game(화면) 작업이다.

## 묶음
stat_upgrade_m3

## 선행 작업
- 049_stat_upgrade_inner_power
- 050_stat_upgrade_realm_bonus

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/dev/art.md
- docs/spec/stat_upgrade.md ("화면", "저장", "연결 지점", "완료 조건 초안", "변경 기록")
- docs/spec/realm.md ("연결 지점" 절)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-18 — 3단계: 공격 속도 제거, 내공 강화 추가, 경지 보너스 적용` 항목 중 화면과 `GameRoot` 조립 부분만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. Core 쪽 `StatType.InnerPower`, 생성자 `StatUpgrades(Wallet wallet, long baseAttack, long baseMaxHealth, int attackLevel = 0, int healthLevel = 0, int innerPowerLevel = 0, RealmProgress realm = null)`, `StatUpgradeText.Name(InnerPower)` = `"INNER"`는 049·050 이 이미 만들었다.
- `Assets/Scripts/Game/GameRoot.cs`: `StatUpgrades` 생성을 `new StatUpgrades(Wallet, Battle.DiscipleBaseAttack, Battle.DiscipleBaseMaxHealth, 저장된 공격력 단계, 저장된 체력 단계, 0, Realm)`으로 바꾼다. 지금 코드의 저장된 단계 인자(`data != null ? data.AttackLevel : 0`, `data != null ? data.HealthLevel : 0`)는 그대로 두고, 그 뒤에 `0`(내공 단계. 저장 데이터에 내공 칸이 없으므로 항상 0)과 `Realm`을 더한다. `Realm`은 이미 그 위 줄에서 만들어진다. 이 생성 줄 말고 `GameRoot`의 다른 줄은 고치지 않는다.
- `Assets/Scripts/Game/StatUpgrade/StatUpgradeView.cs`:
  - 버튼을 세 개로: `Stats`를 `{ StatType.Attack, StatType.Health, StatType.InnerPower }`, `ButtonPositions`를 `(8, 8)`, `(220, 8)`, `(432, 8)`로. 오브젝트 이름은 지금 규칙(`"StatButton_" + stat`) 그대로라 `StatButton_Attack`, `StatButton_Health`, `StatButton_InnerPower`가 된다. 크기 200×48, 앵커·피벗 (0, 0)은 그대로다.
  - 능력치 아이콘 리소스 이름: `Attack` → `attack`, `Health` → `health`, `InnerPower` → `inner_power` (경로 `Art/UI/StatUpgrade/<이름>`). 지금의 `stat == StatType.Attack ? "attack" : "health"` 분기를 세 경우로 바꾼다.
  - 그림 모드 조건을 바탕 `Art/UI/StatUpgrade/button`과 금화 `Art/UI/StatUpgrade/coin`이 **모두 있을 때**로 바꾼다. 능력치 아이콘은 조건에서 뺀다.
  - 그림 모드에서 그 버튼의 아이콘 그림이 없으면(`Resources.Load<Sprite>`가 `null`) 아이콘 오브젝트 `Visual/Icon`은 지금처럼 같은 위치·크기로 만들되 `SetActive(false)`로 숨긴다. 이름·단계·비용·금화 아이콘의 위치와 크기는 아이콘이 있을 때와 같다. 아이콘 그림이 있으면 지금처럼 보이고 그 그림을 쓴다. 지금 `Assets/Resources/Art/UI/StatUpgrade/inner_power.png`는 없으므로 INNER 버튼은 아이콘 없이 보인다. 나중에 파일만 넣으면 코드 변경 없이 보여야 한다.
  - 아이콘이 숨겨진 버튼에서도 갱신(`Refresh`)과 강화 연출(`StatUpgradeButtonEffect`)이 예외 없이 동작해야 한다. 숨겨진 아이콘의 활성 상태를 갱신이나 연출이 다시 켜지 않게 한다.
  - 버튼을 누르면 그 능력치로 `TryUpgrade`를 부르는 것, 다시 그리는 때(`Initialize`, `BalanceChanged`, `Upgraded`, `LockedChanged`), 색, 호버·성공 연출, 단색 모드(바탕이나 금화 그림이 없을 때 `StatUpgradeText.ButtonLabel` 세 줄)는 지금 동작 그대로 세 버튼에 적용한다.
  - 클래스와 `Initialize`의 주석(버튼 두 개 → 세 개)을 고친다.
- 플레이 모드 테스트는 `Assets/Tests/PlayMode/StatUpgradeViewTests.cs`(이미 있는 파일, 네임스페이스 `PlumSword.Game.Tests`)에 새 메서드로 더한다. 쓰는 법은 `docs/dev/architecture.md` "테스트" 절과 같은 파일의 기존 테스트(`Artwork_UpgradeAndMax_UpdateLabelsAndAvailability`: 테스트용 `Canvas`와 `StatUpgradeView`를 직접 만들고 `finally`에서 지움)를 따른다. 글자는 같은 파일의 `ReadText` 도우미로 읽는다.
- 기존 플레이 모드 테스트(`Artwork_UpgradeAndMax_UpdateLabelsAndAvailability`, `ButtonEffect_HoverPressAndPurchase_OnlyAnimatesWhenAvailable`, `GameBootTests` 등)는 고치지 않고 그대로 통과해야 한다.
- 화면 코드에서 글자를 따로 만들거나 계산하지 않는다. 표시 글자는 `StatUpgradeText`, 누를 수 있는지는 `StatUpgrades.CanUpgrade` 그대로 쓴다.

## 완료 조건
- [컴파일] 화면 코드가 컴파일되고 기존 테스트(에딧 모드·플레이 모드, `GameBootTests` 포함)가 모두 통과한다.
- [플레이 모드] `Assets/Tests/PlayMode/StatUpgradeViewTests.cs`에 아래 [플레이 모드] 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- [플레이 모드] 금화 1000, `new StatUpgrades(wallet, 10, 100)`으로 `StatUpgradeView.Initialize`를 부르면 캔버스 아래에 `StatButton_Attack`, `StatButton_Health`, `StatButton_InnerPower`가 있다.
- [플레이 모드] 같은 상태에서 캔버스 아래에 `StatButton_AttackSpeed`는 없다.
- [플레이 모드] 같은 상태에서 `StatButton_InnerPower`의 `RectTransform.anchoredPosition`은 (432, 8)이다.
- [플레이 모드] 같은 상태에서 `StatButton_InnerPower`의 `Visual/Name` 글자는 `"INNER"`, `Visual/Level`은 `"Lv.0"`, `Visual/Cost`는 `"20"`이다.
- [플레이 모드] 같은 상태에서 `StatButton_InnerPower`의 `Visual/Icon`은 `Resources.Load<Sprite>("Art/UI/StatUpgrade/inner_power")`가 `null`이면 `activeSelf`가 `false`이고, `null`이 아니면 `activeSelf`가 `true`이며 `Image.sprite`가 그 그림이다.
- [플레이 모드] 같은 상태에서 `StatButton_InnerPower`의 `Button.onClick`을 부르면 `Visual/Level`은 `"Lv.1"`, `Visual/Cost`는 `"25"`, 금화는 980이다.
- [사람 확인] 실행하고 10초 뒤 화면 아래쪽 금화 현판 밑에 버튼 세 개가 왼쪽부터 `ATK`, `HP`, `INNER` 순서로 겹치지 않고 보이고, 각각 `Lv.0`과 금화 아이콘 옆 `20`이 표시된다 (저장 데이터가 없는 새 게임 기준).
- [사람 확인] 같은 화면에서 `INNER` 버튼은 아이콘 자리가 비어 있고, 이름·단계·비용 글자 배치는 `ATK`·`HP` 버튼과 같다.
- [사람 확인] 같은 화면에서 금화가 0이라 세 버튼 모두 어둡다 (눌리지 않는지는 에디터에서 눌러 본다).
- [사람 확인] 에디터에서 금화를 모아 이류로 돌파한 뒤 다음 스테이지가 시작되면 제자 체력 바가 `110/110`이다.

## 하지 말 것
- Core(`Assets/Scripts/Core/`)는 고치지 않는다. 049·050 이 끝냈다.
- `GameRoot`에서는 `StatUpgrades` 생성 줄만 고친다. 다른 기능의 조립 줄, `Realm`·`Battle`·저장 조립은 고치지 않는다.
- 다른 기능의 화면 코드(`Assets/Scripts/Game/Realm/`, `Assets/Scripts/Game/Currency/`, 전투·스테이지·저장 화면)는 고치지 않는다.
- 저장 데이터(`SaveData`, `SaveCodec`)에 내공 칸을 만들거나 `data.AttackSpeedLevel`을 읽지 않는다. 내공 단계는 0을 넣는다.
- 내공 아이콘 그림(`inner_power.png`)을 만들거나 넣지 않는다. 다른 리소스 파일도 더하거나 지우지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`), 어셈블리 정의는 바꾸지 않는다. 화면은 코드로 조립한다.
- 있던 테스트 메서드를 지우거나 이름을 바꾸거나 검사(Assert)를 줄이지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/`는 고치지 않는다.

## 보고서

### 한 일
- INNER 강화 버튼과 선택적인 능력치 아이콘을 추가하고, 그림 모드에서 아이콘이 없어도 나머지 표시와 연출을 유지했다.
- GameRoot에서 내공 초기 단계 0과 Realm을 강화 객체에 전달했다.
- 버튼 구성, 표시, 아이콘 상태, 구매 및 갱신과 연출을 확인하는 플레이 모드 테스트를 추가했다. 기존 사양은 이미 구현과 일치한다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs`
- `Assets/Scripts/Game/StatUpgrade/StatUpgradeView.cs`
- `Assets/Tests/PlayMode/StatUpgradeViewTests.cs`
- `tasks/doing/051_stat_upgrade_view_inner.md`, `.loop/result.json`

### 가정
없음.

### 사람이 확인할 것
- 실행하고 10초 뒤 화면 아래쪽 금화 현판 밑에 버튼 세 개가 왼쪽부터 `ATK`, `HP`, `INNER` 순서로 겹치지 않고 보이고, 각각 `Lv.0`과 금화 아이콘 옆 `20`이 표시된다 (저장 데이터가 없는 새 게임 기준).
- 같은 화면에서 `INNER` 버튼은 아이콘 자리가 비어 있고, 이름·단계·비용 글자 배치는 `ATK`·`HP` 버튼과 같다.
- 같은 화면에서 금화가 0이라 세 버튼 모두 어둡다 (눌리지 않는지는 에디터에서 눌러 본다).
- 에디터에서 금화를 모아 이류로 돌파한 뒤 다음 스테이지가 시작되면 제자 체력 바가 `110/110`이다.

### 테스트 결과
- `git diff --check` 통과.
- Unity 에딧 모드·플레이 모드 테스트는 지시대로 직접 실행하지 않았으며 루프의 실행과 판정을 기다린다.


## 루프 기록 (2026-11-18 14:25)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/051_stat_upgrade_view_inner
- 루프 브랜치: loop/20261118-141612
