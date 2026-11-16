# 001_wallet — 금화 지갑 (지급·소비·알림)

## 목표
`PlumSword.Core` 에 보유 금화를 들고 있는 지갑 `Wallet` 을 만든다. 지급(`Add`), 소비(`TrySpend`), 확인(`CanSpend`), 잔액 변경 알림(`BalanceChanged`)을 제공하고 에딧 모드 테스트로 검사한다.

## 묶음
금화

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/currency.md ("동작", "경계 상황", "수치", "연결 지점" 절)

## 요구사항
- 파일: `Assets/Scripts/Core/Currency/Wallet.cs`. 네임스페이스 `PlumSword.Core.Currency`. 파일 하나에 공개 타입 하나.
- 테스트 파일: `Assets/Tests/EditMode/Currency/WalletTests.cs`. 네임스페이스 `PlumSword.Core.Tests.Currency`, 클래스 `WalletTests`. 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다.
- 공개 모양은 사양의 "연결 지점" 절 그대로다. 다른 작업(002, 003과 뒤의 강화·스테이지 기능)이 이 이름으로 부른다.

  ```csharp
  namespace PlumSword.Core.Currency
  {
      public sealed class Wallet
      {
          public Wallet(long initialBalance = 0);
          public long Balance { get; }
          public event System.Action<long> BalanceChanged;
          public void Add(long amount);
          public bool CanSpend(long amount);
          public bool TrySpend(long amount);
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/currency.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 숫자 타입은 `long` 이다. 잔액은 항상 0 이상 `long.MaxValue` 이하다.
- 생성자 `Wallet(long initialBalance = 0)`: 잔액을 `initialBalance` 로 시작한다. 음수면 잔액 0으로 만들고 예외를 던지지 않는다. `BalanceChanged` 를 보내지 않는다.
- `Add(long amount)`:
  - 공식: `잔액 = (잔액 > long.MaxValue - amount) ? long.MaxValue : 잔액 + amount`. 넘침 예외가 나지 않아야 한다.
  - `amount` 가 0이면 아무 일도 없다 (잔액 그대로, 알림 없음).
  - `amount` 가 음수면 `System.ArgumentOutOfRangeException` 을 던진다 (잔액 그대로, 알림 없음).
  - 결과가 `long.MaxValue` 를 넘으면 `long.MaxValue` 에서 멈춘다 (예외 없음, 알림 1번).
  - 잔액이 이미 `long.MaxValue` 일 때 양수를 주면 아무 일도 없다 (잔액 그대로, 알림 없음).
- `CanSpend(long amount)`: 잔액이 `amount` 이상이면 `true`, 아니면 `false`. 아무것도 바꾸지 않고 알림도 보내지 않는다. 0이면 `true`. 음수면 `System.ArgumentOutOfRangeException`.
- `TrySpend(long amount)`:
  - 잔액이 `amount` 이상이면 `잔액 = 잔액 - amount` 를 하고 `true` 를 돌려준다.
  - 모자라면 아무것도 바꾸지 않고 `false` 를 돌려준다 (알림 없음).
  - 0이면 `true` 를 돌려주고 잔액은 그대로다 (알림 없음).
  - 음수면 `System.ArgumentOutOfRangeException` 을 던진다 (잔액 그대로, 알림 없음).
  - 연달아 부르면 부를 때마다 그때의 잔액으로 따로 판정한다.
- `BalanceChanged`: 잔액이 실제로 바뀐 때에만, 잔액을 바꾼 **뒤에** 한 번, 새 잔액을 담아 보낸다. 알림을 받는 쪽에서 `Balance` 를 읽으면 새 잔액이어야 한다. 값이 바뀌지 않으면 보내지 않는다. 구독자가 없어도 예외가 나지 않아야 한다.
- 여러 번 부르면 부른 순서대로 한 번씩 처리한다. 예: `Add(10)` 을 세 번 부르면 알림도 세 번(10, 20, 30) 간다. 모아서 한 번에 보내지 않는다.
- 지갑에는 초기화(리셋) 동작이 없다. 사양에 없는 공개 멤버를 더하지 않는다.
- 시간·난수·저장을 쓰지 않는다. 저장은 2단계의 저장 기능이 `new Wallet(저장된 값)` 과 `BalanceChanged` 로 처리하므로 이 작업에서 저장 코드를 만들지 않는다.

## 완료 조건
- `Assets/Tests/EditMode/Currency/WalletTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `new Wallet()` 의 `Balance` 는 0이다.
- `new Wallet(500)` 의 `Balance` 는 500이다.
- `new Wallet(-5)` 의 `Balance` 는 0이고 예외가 없다.
- 잔액 0에서 `Add(10)` 을 10번 부르면 `Balance` 는 100이고, 이어서 `Add(50)` 을 부르면 150이다.
- 잔액 0에서 `Add(10)` 을 3번 부르면 `BalanceChanged` 가 3번 오고 값은 순서대로 10, 20, 30이다.
- `BalanceChanged` 를 받는 쪽에서 읽은 `Balance` 는 알림에 담긴 값과 같다 (잔액 0에서 `Add(10)` 이면 둘 다 10).
- 잔액 100에서 `Add(0)` 을 부르면 `Balance` 는 100이고 `BalanceChanged` 는 0번 온다.
- 잔액 100에서 `Add(-1)` 은 `ArgumentOutOfRangeException` 을 던지고, `Balance` 는 100 그대로이며 `BalanceChanged` 는 0번 온다.
- `new Wallet(long.MaxValue - 5)` 에서 `Add(10)` 을 부르면 `Balance` 는 `long.MaxValue` 이고 `BalanceChanged` 는 1번 온다.
- `new Wallet(long.MaxValue)` 에서 `Add(1)` 을 부르면 `Balance` 는 `long.MaxValue` 이고 `BalanceChanged` 는 0번 온다.
- 잔액 150에서 `TrySpend(20)` 은 `true` 이고 `Balance` 는 130이며 `BalanceChanged` 가 값 130으로 1번 온다.
- 잔액 15에서 `TrySpend(20)` 은 `false` 이고 `Balance` 는 15이며 `BalanceChanged` 는 0번 온다.
- 잔액 20에서 `TrySpend(20)` 은 `true` 이고 `Balance` 는 0이며 `BalanceChanged` 가 값 0으로 1번 온다.
- 잔액 100에서 `TrySpend(0)` 은 `true` 이고 `Balance` 는 100이며 `BalanceChanged` 는 0번 온다.
- 잔액 100에서 `TrySpend(-1)` 은 `ArgumentOutOfRangeException` 을 던지고 `Balance` 는 100 그대로이며 `BalanceChanged` 는 0번 온다.
- 잔액 30에서 `TrySpend(20)` 을 두 번 부르면 결과는 순서대로 `true`, `false` 이고 `Balance` 는 10이다.
- 잔액 20에서 `CanSpend(20)` 은 `true`, `CanSpend(21)` 은 `false`, `CanSpend(0)` 은 `true` 이고, 세 번 모두 `Balance` 는 20 그대로이며 `BalanceChanged` 는 0번 온다.
- 잔액 20에서 `CanSpend(-1)` 은 `ArgumentOutOfRangeException` 을 던지고 `Balance` 는 20 그대로다.
- 기존 테스트가 모두 통과한다. (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다. `PlumSword.Core` 는 순수 C# 이다.
- `GoldText`(금화 표시 글자)를 만들지 않는다. 002_gold_text 가 한다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. `GameRoot`, `CurrencyView` 는 003_currency_view 가 한다.
- 저장·불러오기, 오프라인 보상, 처치 보상 금액, 강화 비용을 만들지 않는다. 다른 기능의 일이다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `PlumSword.Core.Currency.Wallet` 을 사양 "연결 지점"의 공개 모양 그대로 구현했다 (생성자, `Balance`, `BalanceChanged`, `Add`, `CanSpend`, `TrySpend`). 공개 멤버는 사양 외에 더하지 않았다.
- 잔액 변경은 내부 `SetBalance` 한 곳에서만 하며, 값이 실제로 바뀐 때에만 잔액을 먼저 바꾼 뒤 새 잔액으로 `BalanceChanged` 를 보낸다. 넘침은 `잔액 > long.MaxValue - amount` 비교로 막는다 (산술 넘침 없음).
- 음수 금액은 `Add`·`CanSpend`·`TrySpend` 모두 상태를 바꾸기 전에 `ArgumentOutOfRangeException` 을 던진다. `TrySpend` 는 `CanSpend` 로 판정한다.
- 완료 조건마다 에딧 모드 테스트를 하나 이상 작성했다 (19개, 구독자 없음 확인 테스트 1개 추가).

### 바뀐 파일
- `Assets/Scripts/Core/Currency.meta` (새 폴더)
- `Assets/Scripts/Core/Currency/Wallet.cs`, `Wallet.cs.meta` (새 파일)
- `Assets/Tests/EditMode/Currency.meta` (새 폴더)
- `Assets/Tests/EditMode/Currency/WalletTests.cs`, `WalletTests.cs.meta` (새 파일)
- `tasks/doing/001_wallet.md` (이 보고서)
- 사양·개발 문서는 바꾸지 않았다 (구현 모양이 사양과 같다).

### 가정
- 유니티를 실행할 수 없어 `.meta` 파일 4개를 기존 `.meta` 와 같은 형식으로 직접 만들었다 (GUID는 무작위로 새로 만듦). 유니티가 열 때 그대로 받아들여야 한다.
- 예외 메시지는 `"금액은 0 이상이어야 한다."`, 인자 이름은 `amount` 로 정했다.
- 연결 지점 모양은 사양과 같다. 바꾼 것 없음.

### 사람이 확인할 것
- 완료 조건에 [사람 확인] 항목은 없다.
- 직접 만든 `.meta` 파일을 유니티가 문제없이 받아들이는지 (에디터를 열었을 때 경고나 GUID 재생성이 없는지).

### 테스트 결과
- `python tools/loop/run_unity_tests.py` (이어서 한 실행에서 다시 돌림): EditMode 통과 — 20개 중 20개 통과, 0개 실패 (80초). `WalletTests` 19개와 기존 `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함.
- 처음 실행 때는 유니티 라이선스 오류(종료 코드 198)로 러너가 시작되지 않아, 그때 대신 확인한 것: `.loop/` 안에 임시 .NET 9 + NUnit 3.14 프로젝트를 만들어 `Wallet.cs` 와 `WalletTests.cs` 만 컴파일해 `dotnet test` 로 돌렸다. 결과: 실패 0, 통과 19, 전체 19. 확인한 뒤 임시 프로젝트는 지웠다. 유니티의 컴파일러·테스트 러너와 완전히 같은 환경은 아니다.


## 루프 기록 (2026-11-16 15:59)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/001_wallet
- 루프 브랜치: loop/20261116-155015
