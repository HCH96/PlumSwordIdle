# 006_stat_upgrade_view — 강화 버튼 화면과 GameRoot 조립

## 목표
`PlumSword.Game` 에서 `GameRoot` 가 강화 로직 `StatUpgrades` 하나를 만들어 들고 있게 하고, 강화 버튼 세 개(`ATK`, `HP`, `SPD`)를 보여 주는 `StatUpgradeView` 를 만든다. 화면 조립과 입력 전달·표시만 하고, 글자와 누를 수 있는지의 판단은 Core 의 `StatUpgradeText`, `StatUpgrades.CanUpgrade` 를 쓴다.

## 묶음
강화

## 선행 작업
- 001_wallet
- 003_currency_view
- 004_stat_upgrades
- 005_stat_upgrade_text

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다" 절)
- docs/dev/art.md ("기본 숫자" 절의 `CanvasScaler` 기준)
- docs/spec/stat_upgrade.md ("연결 지점", "화면" 절)
- docs/spec/currency.md ("연결 지점" 절의 `GameRoot.Wallet`, `GameRoot.UiCanvas`, `Wallet.BalanceChanged`)

## 요구사항
- 선행 작업이 만든 것을 쓴다. 내부는 고치지 않는다.
  - 001_wallet: `PlumSword.Core.Currency.Wallet` — `event System.Action<long> BalanceChanged` (잔액이 바뀐 뒤 온다).
  - 003_currency_view: `PlumSword.Game.GameRoot` 의 `Wallet Wallet { get; }` 와 `UnityEngine.Canvas UiCanvas { get; }`. `GameRoot.Awake` 가 `Wallet` → `[Canvas]` → `[EventSystem]` → `[CurrencyView]` 순서로 조립해 둔 상태다. 캔버스는 기준 해상도 640×360 이다.
  - 004_stat_upgrades: `PlumSword.Core.StatUpgrade.StatType` (`Attack`, `Health`, `AttackSpeed`) 과 `PlumSword.Core.StatUpgrade.StatUpgrades` — 생성자 `new StatUpgrades(Wallet wallet, long baseAttack, long baseMaxHealth, double baseAttackInterval)`, `int GetLevel(StatType)`, `long GetCost(StatType)`, `bool IsMaxLevel(StatType)`, `bool CanUpgrade(StatType)`, `bool TryUpgrade(StatType)`, `event System.Action<StatType> Upgraded`, `event System.Action<bool> LockedChanged`.
  - 005_stat_upgrade_text: `PlumSword.Core.StatUpgrade.StatUpgradeText.ButtonLabel(StatType stat, int level, long cost, bool isMaxLevel)` — 버튼 글자 세 줄을 `\n` 으로 이어 돌려준다 (예: `ATK\nLv.3\nG 35`, `SPD\nLv.10\nMAX`).
- 고칠 파일: `Assets/Scripts/Game/GameRoot.cs`. 만들 파일: `Assets/Scripts/Game/StatUpgrade/StatUpgradeView.cs` (네임스페이스 `PlumSword.Game.StatUpgrade`).
- 공개 모양은 사양의 "연결 지점" 절 그대로다. 뒤의 전투·스테이지 기능이 `GameRoot.StatUpgrades` 를 쓴다.

  ```csharp
  namespace PlumSword.Game
  {
      public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래 하나를 더한다
      {
          public PlumSword.Core.StatUpgrade.StatUpgrades StatUpgrades { get; }   // 밖에서는 읽기만 (private set)
      }
  }

  namespace PlumSword.Game.StatUpgrade
  {
      public sealed class StatUpgradeView : MonoBehaviour
      {
          public void Initialize(
              PlumSword.Core.StatUpgrade.StatUpgrades upgrades,
              PlumSword.Core.Currency.Wallet wallet,
              UnityEngine.Canvas canvas);
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/stat_upgrade.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 이름이 겹치는 것에 주의한다: `PlumSword.Game` 안에서 `StatUpgrade` 라고만 쓰면 네임스페이스 `PlumSword.Game.StatUpgrade` 로 읽힌다. Core 타입이 헷갈리게 읽혀 컴파일 오류가 나면 전체 이름(`PlumSword.Core.StatUpgrade.StatUpgrades`)이나 `using` 별칭을 쓴다.
- `GameRoot.Awake` 에 아래 두 가지를 더한다. 금화 기능이 만든 줄은 고치지 않는다.
  1. `Wallet` 을 만든 줄 뒤에서 `StatUpgrades = new StatUpgrades(Wallet, 10, 100, 1.0)` 으로 하나 만든다. 게임에 `StatUpgrades` 는 이것 하나뿐이다. 10 / 100 / 1.0 은 시작 공격력 / 시작 최대 체력 / 시작 공격 간격(초)이고 임시값이다 (원래 전투 기능의 수치다. 이 숫자에 이름 있는 상수를 붙이고 "임시값, 전투 기능이 생기면 그 값으로 바꾼다"는 주석을 단다).
  2. `[CurrencyView]` 를 만든 줄 뒤에서 `[Game]` 의 자식 오브젝트 `[StatUpgradeView]` 를 만들어 `StatUpgradeView` 를 붙이고 `Initialize(StatUpgrades, Wallet, UiCanvas)` 를 부른다.
- `StatUpgradeView.Initialize(upgrades, wallet, canvas)`:
  - `upgrades`, `wallet`, `canvas` 중 하나라도 `null` 이면 `System.ArgumentNullException` 을 던진다.
  - `canvas` 의 자식으로 버튼 오브젝트 세 개를 만든다. 왼쪽부터 `ATK`, `HP`, `SPD` 순서로 가로 한 줄이다. 오브젝트 이름은 `StatButton_Attack`, `StatButton_Health`, `StatButton_AttackSpeed`.
  - 버튼의 `RectTransform`: `anchorMin = anchorMax = (0, 0)`(캔버스 왼쪽 아래), `pivot = (0, 0)`, `sizeDelta = (200, 88)`, `anchoredPosition` 은 `Attack` (8, 8), `Health` (220, 8), `AttackSpeed` (432, 8). (위치와 크기는 임시값. 보유 금화 글자(아래에서 100~116 높이) 바로 아래 자리다)
  - 버튼 모양: `Image`(스프라이트 없이 단색 사각형)와 `Button` 을 붙인다. `Button.targetGraphic` 은 그 `Image`, `Button.transition = Selectable.Transition.None`. 색은 화면 코드가 직접 바꾼다.
  - 글자: 버튼의 자식 오브젝트 `Label` 하나에 `TextMeshProUGUI` 를 붙인다. 버튼을 꽉 채운다 (`anchorMin = (0, 0)`, `anchorMax = (1, 1)`, `offsetMin = offsetMax = (0, 0)`). 가운데 정렬(가로·세로), 글자 크기 16 (임시값), `raycastTarget = false`. 글꼴은 따로 지정하지 않고 TMP 기본 글꼴을 쓴다.
  - 버튼을 누르면(`Button.onClick`) 그 버튼의 능력치로 `upgrades.TryUpgrade(stat)` 를 부른다. 돌려준 값은 쓰지 않는다. 한 번 누르면 한 단계다. 길게 눌러 연속 강화는 없다.
  - 만들자마자 버튼 세 개를 지금 상태로 그린다 (아래 "다시 그리기").
  - `wallet.BalanceChanged`, `upgrades.Upgraded`, `upgrades.LockedChanged` 를 구독해 알림이 올 때마다 버튼 세 개를 모두 다시 그린다. `OnDestroy` 에서 세 구독을 모두 끊는다.
- **다시 그리기** (버튼마다):
  - 글자: `StatUpgradeText.ButtonLabel(stat, upgrades.GetLevel(stat), upgrades.GetCost(stat), upgrades.IsMaxLevel(stat))` 가 돌려준 것을 그대로 넣는다. 화면 코드에서 글자를 따로 만들거나 붙이거나 고치지 않는다.
  - `upgrades.CanUpgrade(stat)` 가 `true` 면: `Button.interactable = true`, 버튼(`Image`) 색 RGB (0.20, 0.45, 0.25), 글자 흰색 (임시값).
  - `false` 면(금화 부족, 최대 단계, 잠김): `Button.interactable = false`, 버튼 색 RGB (0.25, 0.25, 0.25), 글자 색 RGB (0.6, 0.6, 0.6) (임시값). 세 경우를 모양으로 구분하지 않는다.
  - 누를 수 있는지는 `CanUpgrade` 값 그대로다. 화면 코드에서 금화와 비용을 직접 비교하지 않는다.
- `Update` 로 매 프레임 다시 그리지 않는다. 강화 연출(반짝임, 소리)은 없다.
- TextMeshPro 기본 리소스(TMP Essentials)가 프로젝트에 없을 수 있다. 리소스가 없어도 예외로 게임이 멈추지 않아야 한다. 글자 오브젝트를 만드는 부분과 글자를 바꾸는 부분을 `try/catch` 로 감싸고, 실패하면 `Debug.LogWarning` 을 한 번만 남기고 넘어간다. 글자가 실패해도 버튼의 색과 `interactable` 은 바뀌어야 한다. 리소스를 대신 가져오거나 만들지 않는다 (사람이 에디터에서 가져온다).
- `Assets/Scripts/Game/StatUpgrade/` 폴더를 새로 만든다. `.meta` 파일은 유니티가 화면 없이 실행될 때(테스트 실행) 자동으로 만든다. 만들어진 `.meta` 는 지우지 않는다.
- 금화를 벌 방법(전투·스테이지)이 아직 없어 실행하면 금화가 0이고 세 버튼 모두 어둡고 눌리지 않는다. 이것이 정상이다. 확인용 금화 지급 코드, 초기 잔액 변경, 치트 키를 넣지 않는다.
- 이 작업에는 에딧 모드 테스트를 새로 쓰지 않는다 (테스트 어셈블리는 Core 만 참조한다). 글자와 누를 수 있는지의 판정은 004_stat_upgrades, 005_stat_upgrade_text 의 테스트가 이미 한다. 플레이 모드 테스트 어셈블리도 만들지 않는다.

## 완료 조건
- [컴파일] `GameRoot.cs` 와 `StatUpgradeView.cs` 가 컴파일 오류 없이 컴파일되고 기존 테스트가 모두 통과한다. (`StatUpgradesTests`, `StatUpgradeTextTests`, `WalletTests`, `GoldTextTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)
- [사람 확인] 게임을 실행하면 화면 아래쪽 보유 금화 글자(`G 0`) 밑에 버튼 세 개가 왼쪽부터 `ATK`, `HP`, `SPD` 순서로 겹치지 않고 보이고, 각각 `Lv.0`, `G 20` 이 적혀 있다. (TMP Essentials 를 에디터에서 먼저 가져와야 글자가 보인다: Window > TextMeshPro > Import TMP Essential Resources)
- [사람 확인] 금화가 0이라 세 버튼 모두 어두운 회색이고 글자도 회색이며, 눌러도 아무 일도 일어나지 않는다.
- [사람 확인] 실행 중 Hierarchy 의 `[Game]` 아래에 `[StatUpgradeView]` 가 하나 있고, `[Canvas]` 아래에 `StatButton_Attack`, `StatButton_Health`, `StatButton_AttackSpeed` 가 있다.
- [사람 확인] TMP Essentials 를 가져오지 않은 상태로 실행해도 콘솔에 예외(빨간 줄)가 반복해서 쌓이지 않고 게임이 멈추지 않는다.

## 하지 말 것
- `Assets/Scripts/Core/` 를 고치지 않는다. `StatUpgrades`, `StatUpgradeText`, `Wallet` 은 공개된 모양으로만 쓴다. 글자 규칙이나 판정을 바꿔야 하면 고치지 말고 보고서의 가정에 적는다.
- `Assets/Scripts/Game/Currency/` 와 `GameBootstrap.cs` 를 고치지 않는다.
- `GameRoot` 에는 자기 기능의 줄(`StatUpgrades` 속성과 생성, `[StatUpgradeView]` 조립)만 추가하고 금화 기능과 공용 UI 뿌리의 줄은 고치지 않는다. 전투·스테이지 화면이나 카메라 설정을 만들지 않는다.
- `SetLocked` 를 부르지 않는다. 잠금을 거는 것은 스테이지 기능이다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다. 화면은 코드로 조립한다. TMP Essentials 리소스를 직접 추가하지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다 (`PlumSword.Game` 은 이미 `Unity.TextMeshPro`, `Unity.InputSystem`, `UnityEngine.UI` 를 참조한다).
- 강화 UI 영역의 배경, 아이콘, 강화 연출, 현재 능력치 값 표시, 길게 눌러 연속 강화, 한글 표기를 만들지 않는다. 사양의 "범위 밖"이다.
- 저장·불러오기를 만들지 않는다. 2단계 저장 기능이 한다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `GameRoot` 에 `public PlumSword.Core.StatUpgrade.StatUpgrades StatUpgrades { get; private set; }` 를 더했다. `Awake` 에서 `Wallet` 을 만든 바로 뒤에 `new StatUpgrades(Wallet, BaseAttack, BaseMaxHealth, BaseAttackInterval)` 로 하나 만든다. 10 / 100 / 1.0 에는 `private const` 상수 이름을 붙이고 "임시값, 전투 기능이 생기면 그 값으로 바꾼다" 주석을 달았다.
- `CreateCurrencyView()` 바로 뒤에 `CreateStatUpgradeView()` 를 불러 `[Game]` 의 자식 `[StatUpgradeView]` 를 만들고, `StatUpgradeView` 를 붙여 `Initialize(StatUpgrades, Wallet, UiCanvas)` 를 부른다. 금화 기능과 공용 UI 뿌리의 줄은 고치지 않았다.
- `StatUpgradeView` (`PlumSword.Game.StatUpgrade`) 를 새로 만들었다.
  - 인자 중 하나라도 null 이면 `ArgumentNullException` 을 던진다.
  - 캔버스 아래에 `StatButton_Attack` / `StatButton_Health` / `StatButton_AttackSpeed` 를 만든다. 앵커·피벗은 (0,0), 크기는 200×88, 위치는 (8,8) / (220,8) / (432,8) 이다. 각 버튼에는 `Image`, `Button` 이 붙는다 (targetGraphic=Image, transition=None, onClick 에서 `TryUpgrade(stat)`).
  - 자식 `Label` 은 `TextMeshProUGUI` 다. 버튼을 꽉 채우고 가운데 정렬, 크기 16, raycastTarget=false 다.
  - 다시 그리기: 글자는 `StatUpgradeText.ButtonLabel(...)` 결과를 그대로 넣는다. `CanUpgrade` 값에 따라 interactable 과 버튼·글자 색이 바뀐다.
  - 다시 그리는 때는 `Initialize` 에서 한 번, 그 뒤로 `BalanceChanged`, `Upgraded`, `LockedChanged` 가 올 때다. 세 구독은 `OnDestroy` 에서 끊는다.
  - 글자 오브젝트 생성과 글자 변경은 try/catch 로 감쌌다. 실패하면 `Debug.LogWarning` 을 한 번만 남긴다. 버튼 색과 interactable 은 글자 처리보다 먼저, try 밖에서 바꾼다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs` (수정)
- `Assets/Scripts/Game/StatUpgrade/StatUpgradeView.cs` (새 파일)
- `Assets/Scripts/Game/StatUpgrade.meta`, `Assets/Scripts/Game/StatUpgrade/StatUpgradeView.cs.meta` (테스트 실행 때 유니티가 자동 생성)

### 가정
- `GameRoot.StatUpgrades` 는 `Wallet` 과 같이 `{ get; private set; }` 로 했다. 사양의 `{ get; }` 는 "밖에서는 읽기만"이라는 뜻으로 보았고, 연결 지점 모양은 바뀌지 않는다.
- 시작 능력치 상수 이름은 `BaseAttack`, `BaseMaxHealth`, `BaseAttackInterval` (private const) 로 정했다.
- 이름 충돌을 피하려고 Core 의 `StatUpgrades` 는 전체 이름 `PlumSword.Core.StatUpgrade.StatUpgrades` 로, 화면 쪽은 `PlumSword.Game.StatUpgrade.StatUpgradeView` 로 적었다.
- 버튼 오브젝트 이름은 `"StatButton_" + stat` (enum 이름) 으로 만든다.
- 글자 색 지정도 글자 변경 try 블록 안에 넣었다. TMP 가 실패해도 버튼 색과 interactable 에는 영향이 없다.
- 경고 문구: "StatUpgradeView: 강화 버튼 글자를 표시하지 못했습니다. TMP Essentials 가 필요합니다 ..." 에 예외 메시지를 붙인다.
- 위치, 크기, 글자 크기, 색은 작업 문서의 임시값 그대로다.
- 사양과 연결 지점이 구현과 같아서 문서는 고치지 않았다.

### 사람이 확인할 것
- [사람 확인] 게임을 실행하면 화면 아래쪽 보유 금화 글자(`G 0`) 밑에 버튼 세 개가 왼쪽부터 `ATK`, `HP`, `SPD` 순서로 겹치지 않고 보이고, 각각 `Lv.0`, `G 20` 이 적혀 있다. (TMP Essentials 를 에디터에서 먼저 가져와야 글자가 보인다: Window > TextMeshPro > Import TMP Essential Resources)
- [사람 확인] 금화가 0이라 세 버튼 모두 어두운 회색이고 글자도 회색이며, 눌러도 아무 일도 일어나지 않는다.
- [사람 확인] 실행 중 Hierarchy 의 `[Game]` 아래에 `[StatUpgradeView]` 가 하나 있고, `[Canvas]` 아래에 `StatButton_Attack`, `StatButton_Health`, `StatButton_AttackSpeed` 가 있다.
- [사람 확인] TMP Essentials 를 가져오지 않은 상태로 실행해도 콘솔에 예외(빨간 줄)가 반복해서 쌓이지 않고 게임이 멈추지 않는다.
- 세 줄 글자가 버튼 높이 88 안에 잘리지 않고 들어가는지 (글자 크기 16 임시값).
- 금화를 벌 방법이 아직 없어서 금화가 생겼을 때 버튼이 초록으로 바뀌는지와 누르면 강화되는지는 실행으로 확인하지 못했다 (판정 로직은 Core 테스트가 검사한다).

### 테스트 결과
- `python tools/loop/run_unity_tests.py`: EditMode 86개 중 86개 통과, 0개 실패. 컴파일 오류 없음.
- 이 작업에서 새 테스트는 쓰지 않았다 (작업 문서 지시).


## 루프 기록 (2026-11-16 16:23)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/006_stat_upgrade_view
- 루프 브랜치: loop/20261116-161636
