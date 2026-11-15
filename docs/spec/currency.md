# 금화 사양

- 기능 이름: currency (네임스페이스 `PlumSword.Core.Currency`)
- 작성일: 2026-11-16

## 목표

금화는 이 게임의 기본 재화다. 몬스터를 처치하면 늘고 강화에 쓰면 준다. 이 기능은 보유 금화를 들고 있는 지갑(지급·소비)과 보유 금화를 화면에 보여 주는 것까지 맡는다. 얼마를 줄지, 얼마가 드는지는 맡지 않는다.

출처: `docs/design/milestone_1.md` "1단계: 금화", "UI와 전환", "기능 간 연결과 작업 분할 시 확인할 점". 저장 대비는 `docs/design/milestone_2.md` "저장과 불러오기".

## 동작

- 지갑은 보유 금화(잔액) 하나를 가진다. 처음 값은 0이다.
- **지급** `Add(amount)`: 잔액이 `amount`만큼 즉시 는다. 줍는 과정이나 지연은 없다.
- **소비** `TrySpend(amount)`: 잔액이 `amount` 이상이면 그만큼 줄이고 `true`를 돌려준다. 모자라면 아무것도 바꾸지 않고 `false`를 돌려준다.
- **확인** `CanSpend(amount)`: 잔액이 `amount` 이상인지만 돌려준다. 아무것도 바꾸지 않는다.
- **알림** `BalanceChanged`: 잔액이 실제로 바뀐 때에만, 바뀐 뒤에 한 번, 새 잔액을 담아 보낸다. 알림을 받는 쪽에서 `Balance`를 읽으면 새 잔액이다. 값이 바뀌지 않으면 보내지 않는다. 생성자에서는 보내지 않는다.
- 같은 시점에 여러 번 부르면 부른 순서대로 한 번씩 처리한다. 예: 한 번에 세 마리를 처치해 `Add(10)`을 세 번 부르면 알림도 세 번(10, 20, 30) 간다.
- 스테이지 성공·실패는 지갑에 아무 영향을 주지 않는다. 지갑에는 초기화 동작이 없다.
- 지급과 소비를 부르는 쪽은 다른 기능이다. 지급은 전투·스테이지(처치 보상)와 오프라인 보상(2단계), 소비는 강화(1단계)와 경지 돌파(3단계)다. 금액은 그 기능들이 정한다.
- 화면은 `BalanceChanged`를 받아 보유 금화 글자를 바꾼다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| `Add(0)` | 아무 일도 없다 | 잔액 그대로, 알림 없음 |
| `Add(음수)` | `ArgumentOutOfRangeException` | 잔액 그대로, 알림 없음 |
| `Add` 결과가 `long.MaxValue`를 넘음 | `long.MaxValue`에서 멈춘다 (예외 없음) | 잔액 `long.MaxValue`, 알림 1번 |
| 잔액이 이미 `long.MaxValue`일 때 `Add(양수)` | 아무 일도 없다 | 잔액 그대로, 알림 없음 |
| `TrySpend(잔액보다 큰 값)` | `false` | 잔액 그대로, 알림 없음 |
| `TrySpend(잔액과 같은 값)` (양수) | `true` | 잔액 0, 알림 1번 |
| `TrySpend(0)` | `true` | 잔액 그대로, 알림 없음 |
| `TrySpend(음수)` | `ArgumentOutOfRangeException` | 잔액 그대로, 알림 없음 |
| `CanSpend(0)` | `true` | 변화 없음 |
| `CanSpend(음수)` | `ArgumentOutOfRangeException` | 변화 없음 |
| `new Wallet(음수)` | 잔액 0으로 만든다 (예외 없음) | 잔액 0, 알림 없음 |
| `TrySpend`를 연달아 부름 | 부를 때마다 그때의 잔액으로 따로 판정 | 예: 잔액 30에서 `TrySpend(20)` 두 번 → `true`, `false`, 잔액 10 |
| `GoldText.FormatNumber(음수)` | 빼기 기호를 붙여 그대로 표기 (예외 없음) | 예: -1234 → `-1,234` |

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 숫자 타입 | `long` | 정수만 쓴다. 반올림 없음 |
| 처음 잔액 | 0 | |
| 최소 잔액 | 0 | 음수가 되지 않는다 |
| 최대 잔액 | `long.MaxValue` (9,223,372,036,854,775,807) | 넘으면 이 값에서 멈춘다 |
| 지급 | `잔액 = (잔액 > long.MaxValue - amount) ? long.MaxValue : 잔액 + amount` | |
| 소비 | `잔액 >= amount` 일 때만 `잔액 = 잔액 - amount` | |
| 숫자 표기 `FormatNumber` | 세 자리마다 쉼표, 소수점 없음. `amount.ToString("N0", CultureInfo.InvariantCulture)` | 0 → `0`, 150 → `150`, 1234 → `1,234`, 28800 → `28,800` |
| 보유 금화 표기 `Format` | `"G " + FormatNumber(amount)` | 0 → `G 0`, 1234 → `G 1,234`. 1단계는 한글 글꼴이 없어 한글을 쓰지 않는다 |

몬스터·보스 보상 금액, 강화 비용, 돌파 비용, 오프라인 보상 금액은 이 기능의 수치가 아니다 ("범위 밖").

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.Currency
{
    public sealed class Wallet
    {
        // 저장된 값으로 되살릴 때는 initialBalance 에 저장된 금화를 넣는다.
        // 음수면 0 으로 만든다. 예외를 던지지 않는다. BalanceChanged 를 보내지 않는다.
        public Wallet(long initialBalance = 0);

        // 지금 보유 금화. 항상 0 이상 long.MaxValue 이하.
        public long Balance { get; }

        // 잔액이 실제로 바뀐 뒤에 한 번, 새 잔액을 담아 보낸다. 바뀌지 않으면 보내지 않는다.
        public event System.Action<long> BalanceChanged;

        // 금화를 준다.
        // 0: 아무 일도 없다 (알림 없음). 음수: ArgumentOutOfRangeException.
        // long.MaxValue 를 넘으면 long.MaxValue 에서 멈춘다.
        public void Add(long amount);

        // amount 만큼 쓸 수 있으면 true. 아무것도 바꾸지 않는다.
        // 0: true. 음수: ArgumentOutOfRangeException.
        public bool CanSpend(long amount);

        // 잔액이 amount 이상이면 줄이고 true, 모자라면 그대로 두고 false (알림 없음).
        // 0: true (알림 없음). 음수: ArgumentOutOfRangeException.
        public bool TrySpend(long amount);
    }

    public static class GoldText
    {
        // 세 자리마다 쉼표. 예: 1234 -> "1,234", 0 -> "0", -1234 -> "-1,234". 예외 없음.
        public static string FormatNumber(long amount);

        // 보유 금화 표기. "G " + FormatNumber(amount). 예: 1234 -> "G 1,234".
        public static string Format(long amount);
    }
}
```

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래 둘을 더한다
    {
        public PlumSword.Core.Currency.Wallet Wallet { get; }   // Awake 에서 new Wallet() 으로 하나 만든다
        public UnityEngine.Canvas UiCanvas { get; }             // Awake 에서 만든 공용 UI 캔버스
    }
}

namespace PlumSword.Game.Currency
{
    public sealed class CurrencyView : MonoBehaviour
    {
        // canvas 아래에 보유 금화 글자를 만들고 wallet.Balance 로 바로 표시한다.
        // wallet.BalanceChanged 를 구독하고, OnDestroy 에서 구독을 끊는다.
        public void Initialize(PlumSword.Core.Currency.Wallet wallet, UnityEngine.Canvas canvas);
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot.Awake`가 `Wallet`을 하나 만들어 `GameRoot.Wallet`으로 들고 있는다. 금화를 주거나 쓰는 기능(전투·스테이지, 강화 등)은 `GameRoot`가 조립할 때 이 `Wallet`을 생성자나 초기화 메서드의 인자로 넘겨받는다. 게임에 `Wallet`은 하나뿐이다. 2단계에서 저장 기능이 생기면 `new Wallet(저장된 금화)`로 바꾼다.
- 다른 기능이 금액을 글자로 보여 줄 때(예: 강화 비용)는 `GoldText.FormatNumber`를 쓴다.

### 필요한 것

없음. 다른 기능의 타입을 쓰지 않는다.

## 화면

- **공용 UI 뿌리** (이 기능이 첫 화면 기능이라 함께 만든다. 뒤의 화면 기능은 `GameRoot.UiCanvas`를 그대로 쓴다):
  - `GameRoot.Awake`에서 `[Game]`의 자식 `[Canvas]`를 만든다. `Canvas`(Screen Space - Overlay), `CanvasScaler`(Scale With Screen Size, 기준 해상도 640×360, Match = 1), `GraphicRaycaster`를 붙인다 (`docs/dev/art.md` 기준).
  - `[Game]`의 자식 `[EventSystem]`을 만든다. `EventSystem`과 `InputSystemUIInputModule`을 붙인다. 씬에 `EventSystem`이 이미 있으면 만들지 않는다.
  - 그 뒤 `[Game]`의 자식 `[CurrencyView]`를 만들어 `CurrencyView`를 붙이고 `Initialize(Wallet, UiCanvas)`를 부른다.
- **보여 주는 것**: 보유 금화 한 줄. 글자는 `GoldText.Format(wallet.Balance)` 그대로다 (예: `G 0`, `G 1,234`). 화면 코드에서 글자를 따로 만들거나 고치지 않는다.
- **위치**: 아래쪽 강화 UI 영역의 맨 위 왼쪽. 캔버스의 왼쪽 아래에 앵커를 두고(앵커와 피벗의 기준: 앵커 (0, 0), 피벗 (0, 1)), 위치 (8, 116), 크기 200×16 (임시). 강화 UI 영역의 높이를 120(임시)으로 보고 잡은 값이며, 영역의 실제 크기는 강화 사양이 정한다.
- **모양**: TextMeshPro(`TextMeshProUGUI`), 글자 크기 16 (임시), 흰색, 왼쪽 정렬, 줄바꿈 없음. 글꼴은 TMP 기본 글꼴을 쓴다.
- **바뀌는 때**: `Initialize`에서 한 번, 그 뒤로 `BalanceChanged`가 올 때마다. 매 프레임 다시 그리지 않는다. 숫자가 바뀔 때 연출(튀어오름, 숫자 굴리기)은 없다.
- **누르는 것**: 없다. 글자는 입력을 받지 않는다 (`raycastTarget = false`).
- **사람이 먼저 해 둘 것**: TextMeshPro 기본 리소스(TMP Essentials)가 프로젝트에 있어야 글자가 보인다. 에디터에서 사람이 가져온다. 리소스가 없어도 예외로 게임이 멈추지 않아야 한다.
- 표시 글자를 만드는 판단은 모두 Core의 `GoldText`에 있고 에딧 모드 테스트로 판정한다. `CurrencyView`와 `GameRoot`의 조립은 자동 테스트 대상이 아니며 사람이 실행해 확인한다.

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| 보유 금화 (`Wallet.Balance`) | `long` | 0 | 1단계에서는 저장하지 않는다. 게임을 끄면 0으로 돌아간다 |

- 저장과 불러오기는 2단계의 저장 기능이 만든다. 이 기능은 그때 필요한 것만 미리 제공한다.
  - 저장할 값: `Wallet.Balance` 하나.
  - 되살리기: `new Wallet(저장된 값)`. 음수가 들어오면 0으로 시작한다. 저장 전체의 손상 처리는 저장 기능이 정한다.
  - 저장할 때: 저장 기능이 `BalanceChanged`를 구독해 금화가 바뀔 때 저장한다.
- 오프라인 보상: 꺼져 있는 동안 지갑의 값은 스스로 바뀌지 않는다. 오프라인 보상 기능(2단계)이 다시 켰을 때 금액을 계산해 `Add`를 부른다.

## 범위 밖

- 몬스터·보스 처치 보상 금액과 스테이지별 증가, 동시 처치 때 보상을 주는지: 전투·스테이지 기능이 정한다.
- 강화 비용과 강화 버튼(비용 표시, 금화가 모자랄 때 어둡게): 강화 기능이 정한다. `Wallet.CanSpend`, `Wallet.TrySpend`, `GoldText.FormatNumber`를 쓴다.
- 경지 돌파 비용과 돌파 버튼: 3단계 경지 돌파 기능이 정한다.
- 저장·불러오기, 저장 시점, 손상 처리: 2단계 저장 기능이 정한다.
- 오프라인 보상 계산과 보상 창: 2단계 오프라인 보상 기능이 정한다.
- 한글 표기(`금화 1,234`)와 한글 글꼴: 1단계에서는 하지 않는다. 글꼴이 준비되면 `GoldText.Format`의 앞 글자만 바꾼다.
- 금화 아이콘, 획득 연출(날아오는 동전, 숫자 튀어오름), 줄임 표기(만·억, K·M): 하지 않는다. 필요하면 4단계 UI 개선에서 다룬다.
- 금화 외의 다른 재화: 없다.

## 완료 조건 초안

지갑 (`WalletTests`)

1. `new Wallet()`의 `Balance`는 0이다.
2. `new Wallet(500)`의 `Balance`는 500이다.
3. `new Wallet(-5)`의 `Balance`는 0이고 예외가 없다.
4. 잔액 0에서 `Add(10)`을 10번 부르면 `Balance`는 100이고, 이어서 `Add(50)`을 부르면 150이다.
5. 잔액 0에서 `Add(10)`을 3번 부르면 `BalanceChanged`가 3번 오고 값은 순서대로 10, 20, 30이다.
6. `BalanceChanged`를 받는 쪽에서 읽은 `Balance`는 알림에 담긴 값과 같다.
7. 잔액 100에서 `Add(0)`을 부르면 `Balance`는 100이고 `BalanceChanged`는 0번 온다.
8. `Add(-1)`은 `ArgumentOutOfRangeException`을 던지고, `Balance`는 그대로이며 `BalanceChanged`는 0번 온다.
9. `new Wallet(long.MaxValue - 5)`에서 `Add(10)`을 부르면 `Balance`는 `long.MaxValue`이고 `BalanceChanged`는 1번 온다.
10. `new Wallet(long.MaxValue)`에서 `Add(1)`을 부르면 `Balance`는 `long.MaxValue`이고 `BalanceChanged`는 0번 온다.
11. 잔액 150에서 `TrySpend(20)`은 `true`이고 `Balance`는 130이며 `BalanceChanged`가 값 130으로 1번 온다.
12. 잔액 15에서 `TrySpend(20)`은 `false`이고 `Balance`는 15이며 `BalanceChanged`는 0번 온다.
13. 잔액 20에서 `TrySpend(20)`은 `true`이고 `Balance`는 0이다.
14. 잔액 100에서 `TrySpend(0)`은 `true`이고 `Balance`는 100이며 `BalanceChanged`는 0번 온다.
15. `TrySpend(-1)`은 `ArgumentOutOfRangeException`을 던지고 `Balance`는 그대로다.
16. 잔액 30에서 `TrySpend(20)`을 두 번 부르면 결과는 순서대로 `true`, `false`이고 `Balance`는 10이다.
17. 잔액 20에서 `CanSpend(20)`은 `true`, `CanSpend(21)`은 `false`, `CanSpend(0)`은 `true`이고, 세 번 모두 `Balance`는 20 그대로이며 `BalanceChanged`는 0번 온다.
18. `CanSpend(-1)`은 `ArgumentOutOfRangeException`을 던진다.

표시 글자 (`GoldTextTests`)

19. `GoldText.FormatNumber`는 0 → `"0"`, 150 → `"150"`, 999 → `"999"`, 1000 → `"1,000"`, 1234 → `"1,234"`, 28800 → `"28,800"`, 1234567 → `"1,234,567"`을 돌려준다.
20. `GoldText.FormatNumber(long.MaxValue)`는 `"9,223,372,036,854,775,807"`이다.
21. `GoldText.FormatNumber(-1234)`는 `"-1,234"`이고 예외가 없다.
22. `GoldText.Format`은 0 → `"G 0"`, 1234 → `"G 1,234"`, 28800 → `"G 28,800"`을 돌려준다.
23. 현재 문화권을 `de-DE`로 바꾼 상태에서도 `GoldText.FormatNumber(1234)`는 `"1,234"`이다.

화면 (자동 테스트 없음. 사람이 실행해 확인)

24. 게임을 실행하면 화면 왼쪽 아래 강화 UI 자리 위쪽에 `G 0`이 보인다.
25. 기존 테스트 `CoreAssemblyTests.Core_DoesNotReferenceUnity`가 계속 통과한다.

## 결정 기록

| 질문 | 답 | 방식 |
| --- | --- | --- |
| 몬스터·보스 보상 계산은 어느 사양이 가지나 | 전투·스테이지 사양. 금화는 지급·소비만 | 추천 수락 |
| 보유 금화 표시를 이 사양에 넣나 | 넣는다 (표시 글자 규칙 + 표시 UI) | 추천 수락 |
| 잔액보다 많이 쓰려고 할 때 | `false`, 잔액 그대로, 알림 없음 | 추천 수락 |
| 0이나 음수 금액 | 음수는 `ArgumentOutOfRangeException`, 0은 조용히 통과(알림 없음) | 추천 수락 |
| 최대값을 넘는 지급 | `long.MaxValue`에서 멈춤 | 추천 수락 |
| 숫자 타입 | `long`, 반올림 없음 | 직접 정함 (넘침 질문에서 함께 알림) |
| 금화 표기 | 쉼표 넣은 전체 숫자 | 추천 수락 |
| 표시 위치 | 아래쪽 강화 UI 영역의 맨 위 왼쪽. 좌표와 크기는 임시값 | 추천 수락 / 임시값 |
| 잘못된 저장 값(음수) | 0으로 시작 | 추천 수락 |
| 한글 글꼴이 없는 문제 | 1단계는 한글 없이 표시. 앞 글자는 `G` (`G 1,234`) | 사용자 선택 (추천안과 다름). `G`는 직접 정함 |
| 타입·메서드 이름, `CanSpend` 추가, 알림 모양 `Action<long>` | `Wallet`, `GoldText`, `CurrencyView` | 직접 정함 |
| 공용 UI 캔버스와 `EventSystem`을 누가 만드나 | `GameRoot`가 만들고 `UiCanvas`로 내준다 | 직접 정함 |
