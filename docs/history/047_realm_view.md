# 047_realm_view — 현재 경지 표시와 돌파 버튼, GameRoot 연결 (Game)

## 목표
`GameRoot` 에 `RealmProgress` 를 만들어 들고 강화 잠금에 맞춰 잠그게 하고, 화면 왼쪽 위의 현재 경지 표시와 아래쪽 돌파 버튼을 그리는 `RealmView` 를 `PlumSword.Game.Realm` 에 만든다. 화면(Game) 작업이다. 표시 글자와 누를 수 있는지는 045·046 의 Core 가 판정하므로 이 작업에는 새 자동 테스트가 필요 없다.

## 묶음
realm

## 선행 작업
- 045_realm_rules_text
- 046_realm_progress

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/realm.md ("동작", "연결 지점"의 `GameRoot`·`RealmView`, "화면"의 "조립"·"현재 경지 표시"·"돌파 버튼", "완료 조건 초안" 30~32)
- docs/spec/currency.md ("연결 지점" 절의 `Wallet`, `GoldText`)
- docs/spec/stat_upgrade.md ("연결 지점" 절의 `StatUpgrades.LockedChanged`, "화면"의 강화 버튼 배치와 강화 연출)

## 요구사항
- **GameRoot** (`Assets/Scripts/Game/GameRoot.cs`) 에 자기 기능의 줄만 더한다:
  - 속성 `public PlumSword.Core.Realm.RealmProgress Realm { get; private set; }`.
  - `Awake` 에서 `Wallet` 을 만든 직후, `StatUpgrades` 를 만들기 전에 `Realm = new PlumSword.Core.Realm.RealmProgress(Wallet);` (삼류로 시작. 저장된 경지를 읽지 않는다).
  - `StatUpgrades` 를 만든 뒤 `StatUpgrades.LockedChanged += locked => Realm.SetLocked(locked);` 로 잠금을 맞춘다. 그래서 스테이지 전환 페이드 동안 강화 버튼과 함께 돌파도 잠긴다.
  - `CreateStatUpgradeView()` 호출 바로 뒤에 `CreateRealmView()` 를 부른다. `CreateRealmView` 는 `[Game]` 의 자식 `[RealmView]` 오브젝트를 만들어 `RealmView` 를 붙이고 `Initialize(Realm, Wallet, UiCanvas)` 를 부른다.
- **RealmView** (`Assets/Scripts/Game/Realm/RealmView.cs`, 네임스페이스 `PlumSword.Game.Realm`, 폴더와 파일의 `.meta` 함께):
  - `public void Initialize(PlumSword.Core.Realm.RealmProgress realm, PlumSword.Core.Currency.Wallet wallet, UnityEngine.Canvas canvas)`. 인자 중 `null` 이 있으면 `ArgumentNullException`, 두 번 부르면 `InvalidOperationException`.
  - `canvas` 아래에 경지 표시(오브젝트 이름 `RealmLabel`)와 돌파 버튼(오브젝트 이름 `RealmButton`)을 만들고 지금 상태로 바로 그린다. 이름은 다른 기능의 오브젝트 이름(`StatButton_*`, `GoldPlaque` 등)과 겹치지 않게 한다.
  - `wallet.BalanceChanged`, `realm.BrokeThrough`, `realm.LockedChanged` 를 구독하고, 받을 때마다 버튼과 경지 표시를 다시 그린다. `OnDestroy` 에서 구독을 끊는다.
- **현재 경지 표시** (사양 "현재 경지 표시"):
  - 캔버스 왼쪽 위, 앵커와 피벗 (0, 1), 위치 (8, -8).
  - `Resources.Load<Sprite>("Art/UI/Realm/name_" + RealmText.AssetId(realm.Current))` 가 있으면 `Image`: 높이 24, 가로는 그림 비율대로(최대 160), `preserveAspect = true`, `raycastTarget = false`.
  - 없으면 `TextMeshProUGUI` 한 줄에 `RealmText.Label(realm.Current)`: 크기 200×16, 글자 크기 14, 왼쪽 정렬, 색 RGB (1.0, 0.95, 0.80), TMP 기본 글꼴, `raycastTarget = false` (모두 임시값).
  - 그림은 경지마다 따로 확인한다 (다시 그릴 때마다 지금 경지의 그림을 찾는다). 이류 그림만 있으면 이류일 때만 그림이고 나머지는 글자다.
- **돌파 버튼** (사양 "돌파 버튼"):
  - 캔버스 왼쪽 아래, 앵커와 피벗 (0, 0), 위치 (220, 62), 크기 200×48 (임시값). HP 강화 버튼(위치 (220, 8)) 바로 위다.
  - **그림 모드**: 바탕은 `Art/UI/Realm/button`(`Sprite`)이 있으면 그것, 없으면 `Art/UI/StatUpgrade/button`. 바탕과 금화 아이콘 `Art/UI/StatUpgrade/coin` 이 모두 있을 때만 그림 모드다. 배치는 강화 버튼과 같다 (`Assets/Scripts/Game/StatUpgrade/StatUpgradeView.cs` 의 그림 모드 배치를 읽고 같은 방식으로 만든다. 그 파일은 고치지 않는다):
    - 아이콘 `Art/UI/Realm/icon` (9, 8) 32×32. 없으면 아이콘 `Image` 를 끈다.
    - 첫 줄 `RealmText.ButtonTitle` (48, 23) 82×18, 글자 크기 15, 자동 크기 9~15.
    - 둘째 줄 `RealmText.Name(realm.Current)` (48, 8) 82×15, 글자 크기 11, 자동 크기 8~11.
    - 금화 아이콘 (145, 15) 17×17 과 비용 숫자 `GoldText.FormatNumber(realm.NextCost)` (164, 11) 31×25, 글자 크기 12, 자동 크기 8~12. `realm.IsMax` 면 금화 아이콘을 숨기고 비용 자리에 `MAX` 를 쓴다.
    - 호버·누름·성공 연출은 `PlumSword.Game.StatUpgrade.StatUpgradeButtonEffect` 를 붙여 강화 버튼과 똑같이 한다: `Initialize(Button, RectTransform, Image, Image, TextMeshProUGUI)` 의 마지막 인자에 둘째 줄(경지 이름) 글자를 넘기고, 다시 그릴 때 `SetEnabledAppearance(realm.CanBreakthrough())`, 돌파 성공 때 `PlaySuccess()` 를 부른다.
  - **대체 모드** (그림 모드 조건이 안 될 때): `Image`(단색) + `Button`, `transition = None`. 자식 `TextMeshProUGUI` 하나에 `RealmText.ButtonLabel(realm.Current, realm.NextCost, realm.IsMax)`, 버튼을 꽉 채우고 가운데 정렬, 글자 크기 14, `raycastTarget = false`. 누를 수 있을 때 바탕 RGB (0.55, 0.25, 0.35)에 흰 글자, 누를 수 없을 때 바탕 RGB (0.25, 0.25, 0.25)에 RGB (0.6, 0.6, 0.6) 글자 (모두 임시값). 호버·성공 연출은 없다.
  - `Button.interactable = realm.CanBreakthrough()`. 금화 부족·초절정·잠김을 모양으로 구분하지 않는다.
  - 누르면 `realm.TryBreakthrough()` 를 한 번 부른다. `true` 면 (그림 모드일 때) 성공 연출을 한다.
- 모든 그림은 `Resources.Load` 로 읽는다. 지금 `Assets/Resources/Art/UI/Realm/` 에는 그림이 하나도 없다 (`Art/UI/StatUpgrade/button`, `Art/UI/StatUpgrade/coin` 은 있다). 그림이 없을 때 오류 로그(`Debug.LogError`, 예외)를 남기지 않고 위의 대체 표시로 그린다. 보고서의 가정에 이 작업이 읽는 리소스 경로를 모두 적고, 경로마다 없을 때 무엇을 보여 주는지 적는다.
- 표시 글자와 누를 수 있는지는 `RealmText`, `RealmProgress.CanBreakthrough` 등 Core 의 값을 그대로 쓰고 화면 코드에서 다시 계산하지 않는다.
- 이 작업의 완료 조건은 `[컴파일]` 과 `[사람 확인]` 뿐이다. 새 자동 테스트(에딧 모드·플레이 모드)를 쓰지 않아도 된다.

## 완료 조건
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다 (플레이 모드 `GameBootTests` 포함: 게임을 켜서 `[Game]` 이 조립되는 동안 오류 로그가 없다).
- [컴파일] `GameRoot` 에 `public PlumSword.Core.Realm.RealmProgress Realm { get; private set; }` 가 있고, `Awake` 가 `Wallet` 을 만든 직후 `StatUpgrades` 보다 먼저 `Realm` 을 만들며, `StatUpgrades.LockedChanged` 를 구독해 `Realm.SetLocked` 를 같은 값으로 부르고, `CreateStatUpgradeView()` 바로 뒤에 `[RealmView]` 를 조립한다.
- [컴파일] `RealmView` 의 모든 그림은 `Resources.Load` 로 읽고, 읽은 값이 `null` 이면 요구사항의 대체 표시(경지 글자, 단색 버튼)로 그린다.
- [사람 확인] 실행하고 10초 뒤 화면 왼쪽 위 구석에 `REALM: THIRD RATE` 글자가 보인다 (경지 이름 그림이 없으므로 글자).
- [사람 확인] 실행하고 10초 뒤 화면 아래쪽 가운데, HP 강화 버튼 바로 위에 돌파 버튼이 강화 버튼과 같은 바탕 그림으로 겹치지 않고 보이고, `BREAKTHROUGH` / `THIRD RATE` / 금화 아이콘 옆 `200` 이 적혀 있다 (그림 모드 조건이 안 되면 단색 바탕에 `G 200`).
- [사람 확인] 에디터에서 실행해 금화가 200 보다 적을 때 돌파 버튼이 어둡고 눌러도 아무 일이 없는지 본다.
- [사람 확인] 에디터에서 금화 200 이상일 때 돌파 버튼을 눌러 본다: 금화가 200 줄고, 왼쪽 위가 `REALM: SECOND RATE`, 버튼이 `SECOND RATE` / `500` 으로 바뀌고, 강화 버튼과 같은 성공 연출(눌림, 경지 이름 글자가 금빛으로 변함)이 나온다.
- [사람 확인] 에디터에서 초절정까지 돌파해 본다: 버튼 비용 자리에 `MAX` 가 보이고 금화 아이콘이 숨고 눌리지 않는다.
- [사람 확인] 에디터에서 스테이지 전환 페이드 동안 돌파 버튼이 강화 버튼과 함께 어두워지고 눌리지 않는지 본다.

## 하지 말 것
- 오오라(`RealmAuraView`)는 만들지 않는다 (048 이 한다). `GameRoot` 에서 `[CombatView]` 조립 부분은 고치지 않는다.
- `GameRoot` 에는 자기 기능의 줄(`Realm` 속성, 생성, 잠금 구독, `CreateRealmView`)만 추가하고 다른 기능의 줄은 고치지 않는다. 저장 데이터에서 경지를 읽지 않는다.
- `Assets/Scripts/Core/` 는 고치지 않는다 (045·046 의 `PlumSword.Core.Realm` 포함).
- `StatUpgrades`·`Battle`·`SaveData`·`SaveCodec`·`AutoSave` 를 고치지 않는다 (보너스 적용은 능력치 강화, 돌파 때 현재 체력 증가는 전투, 경지 저장은 저장 기능 사양의 일이다).
- `StatUpgradeButtonEffect`, `StatUpgradeView`, `CombatView` 를 고치지 않는다. `StatUpgradeButtonEffect` 는 공개 메서드를 부르기만 한다.
- `docs/dev/assets.md` 를 고치지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)과 어셈블리 정의를 고치지 않는다. 그림 파일을 만들거나 넣지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/` 를 고치지 않는다.

## 보고서

### 한 일
- GameRoot에 경지 진행을 생성하고 강화 잠금과 경지 화면을 연결했다.
- 경지 표시와 돌파 버튼의 그림 및 대체 표시, 상태 갱신과 성공 연출을 구현했다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs`
- `Assets/Scripts/Game/Realm.meta`, `Assets/Scripts/Game/Realm/RealmView.cs`, `Assets/Scripts/Game/Realm/RealmView.cs.meta`
- `tasks/doing/047_realm_view.md`, `.loop/result.json`

### 가정
- 사양의 위치, 크기, 색과 글자 크기는 임시값을 그대로 사용했다. 공개 연결 지점은 사양과 같다.
- `Art/UI/Realm/name_<AssetId>`가 없으면 해당 경지는 글자로 표시한다.
- `Art/UI/Realm/button`이 없으면 `Art/UI/StatUpgrade/button`을 사용하고, 그것도 없거나 `Art/UI/StatUpgrade/coin`이 없으면 단색 버튼과 글자를 표시한다.
- `Art/UI/Realm/icon`이 없으면 아이콘 자리를 비운다.
- 재사용하는 성공 연출이 읽는 `Art/StartMenu/petal_open`, `Art/StartMenu/petal_curled`가 없으면 해당 꽃잎을 표시하지 않는다.

### 사람이 확인할 것
- 실행 10초 뒤 왼쪽 위에 `REALM: THIRD RATE`가 보이는지 확인한다.
- 실행 10초 뒤 HP 버튼 위에 돌파 버튼과 `BREAKTHROUGH` / `THIRD RATE` / 금화 아이콘 옆 `200`이 겹치지 않고 보이는지 확인한다. 그림 모드가 아니면 단색과 `G 200`을 확인한다.
- 금화 200 미만일 때 버튼이 어둡고 눌리지 않는지 확인한다.
- 금화 200 이상에서 돌파하면 금화가 200 줄고 경지 표시와 버튼이 `SECOND RATE`, `500`으로 바뀌며 성공 연출이 나오는지 확인한다.
- 초절정에서 `MAX`가 보이고 금화 아이콘이 숨으며 버튼이 눌리지 않는지 확인한다.
- 스테이지 전환 페이드 동안 강화 버튼과 함께 돌파 버튼이 어두워지고 눌리지 않는지 확인한다.

### 테스트 결과
- `git diff --check` 통과. 새 자동 테스트는 작업 문서에 따라 추가하지 않았다.
- Unity 테스트와 컴파일은 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-18 12:21)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/047_realm_view
- 루프 브랜치: loop/20261118-121225
