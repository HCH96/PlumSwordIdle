# 002_gold_text — 금화 표시 글자

## 목표
`PlumSword.Core` 에 금화 숫자를 화면 글자로 바꾸는 표시용 클래스 `GoldText` 를 만든다. 세 자리 쉼표 표기(`FormatNumber`)와 보유 금화 표기(`Format`)를 제공하고 에딧 모드 테스트로 검사한다.

## 묶음
금화

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/currency.md ("수치", "연결 지점" 절과 "경계 상황" 표의 `GoldText.FormatNumber(음수)` 줄)

## 요구사항
- 파일: `Assets/Scripts/Core/Currency/GoldText.cs`. 네임스페이스 `PlumSword.Core.Currency`. 파일 하나에 공개 타입 하나.
- 테스트 파일: `Assets/Tests/EditMode/Currency/GoldTextTests.cs`. 네임스페이스 `PlumSword.Core.Tests.Currency`, 클래스 `GoldTextTests`. 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다.
- `Assets/Scripts/Core/Currency/` 와 `Assets/Tests/EditMode/Currency/` 폴더는 같은 묶음의 다른 작업(001_wallet)이 이미 만들었을 수 있다. 있으면 그대로 쓰고 없으면 만든다. 이 작업은 `Wallet` 을 쓰지 않는다.
- 공개 모양은 사양의 "연결 지점" 절 그대로다. 화면 작업(003_currency_view)과 뒤의 강화 기능이 이 이름으로 부른다.

  ```csharp
  namespace PlumSword.Core.Currency
  {
      public static class GoldText
      {
          public static string FormatNumber(long amount);
          public static string Format(long amount);
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/currency.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- `FormatNumber(long amount)`: 세 자리마다 쉼표를 넣고 소수점은 없다. `amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)` 와 같은 결과다. 기기의 문화권 설정과 상관없이 항상 쉼표(`,`)를 쓴다.
  - 예: 0 → `0`, 150 → `150`, 1234 → `1,234`, 28800 → `28,800`.
  - 음수가 들어오면 빼기 기호를 붙여 그대로 표기한다. 예외를 던지지 않는다. 예: -1234 → `-1,234`.
- `Format(long amount)`: `"G " + FormatNumber(amount)`. `G` 뒤에 띄어쓰기 한 칸. 예: 0 → `G 0`, 1234 → `G 1,234`.
  - 1단계에는 한글 글꼴이 없어 한글(`금화`)을 쓰지 않는다. 줄임 표기(만·억, K·M)도 하지 않는다.
- 문화권을 바꾸는 테스트는 테스트가 끝날 때 원래 문화권으로 되돌린다 (`try/finally` 또는 `[TearDown]`). 다른 테스트에 영향을 주지 않기 위해서다.
- 시간·난수·저장을 쓰지 않는다. 상태가 없는 정적 메서드다.

## 완료 조건
- `Assets/Tests/EditMode/Currency/GoldTextTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `GoldText.FormatNumber` 는 0 → `"0"`, 150 → `"150"`, 999 → `"999"`, 1000 → `"1,000"`, 1234 → `"1,234"`, 28800 → `"28,800"`, 1234567 → `"1,234,567"` 을 돌려준다.
- `GoldText.FormatNumber(long.MaxValue)` 는 `"9,223,372,036,854,775,807"` 이다.
- `GoldText.FormatNumber(-1234)` 는 `"-1,234"` 이고 예외가 없다.
- `GoldText.Format` 은 0 → `"G 0"`, 1234 → `"G 1,234"`, 28800 → `"G 28,800"` 을 돌려준다.
- 현재 문화권(`CultureInfo.CurrentCulture`)을 `de-DE` 로 바꾼 상태에서도 `GoldText.FormatNumber(1234)` 는 `"1,234"` 이다.
- 기존 테스트가 모두 통과한다. (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다. `PlumSword.Core` 는 순수 C# 이다.
- `Wallet` 을 만들거나 고치지 않는다. 001_wallet 이 한다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. `GameRoot`, `CurrencyView` 는 003_currency_view 가 한다.
- 한글 표기, 금화 아이콘, 줄임 표기, 강화 비용 글자를 만들지 않는다. 사양의 "범위 밖"이다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `PlumSword.Core.Currency.GoldText` 정적 클래스를 만들었다. `FormatNumber(long)`는 `amount.ToString("N0", CultureInfo.InvariantCulture)`로 세 자리 쉼표를 넣고, `Format(long)`은 `"G " + FormatNumber(amount)`를 돌려준다.
- 에딧 모드 테스트 `GoldTextTests`를 작성했다 (테스트 케이스 12개). 완료 조건마다 테스트가 하나 이상 있다.
  - `FormatNumber_PositiveOrZero_InsertsCommaEveryThreeDigits` (0, 150, 999, 1000, 1234, 28800, 1234567)
  - `FormatNumber_LongMaxValue_FormatsAllDigits`
  - `FormatNumber_Negative_PrefixesMinusWithoutException`
  - `Format_Amount_PrefixesGAndSpace` (0, 1234, 28800)
  - `FormatNumber_CurrentCultureIsGerman_StillUsesComma` (`try/finally`로 원래 문화권을 되돌린다)
- 공개 모양이 사양의 "연결 지점"과 같아서 사양 문서는 고치지 않았다.

### 바뀐 파일
- `Assets/Scripts/Core/Currency/GoldText.cs` (새 파일)
- `Assets/Scripts/Core/Currency/GoldText.cs.meta` (유니티가 자동으로 만듦)
- `Assets/Tests/EditMode/Currency/GoldTextTests.cs` (새 파일)
- `Assets/Tests/EditMode/Currency/GoldTextTests.cs.meta` (유니티가 자동으로 만듦)
- `tasks/doing/002_gold_text.md` (이 보고서)

### 가정
- 값이 여러 개인 완료 조건은 하나하나 `[Test]`로 나누지 않고 NUnit `[TestCase]`로 묶었다 (값마다 따로 실행·보고된다).
- 문화권 테스트에서는 `CultureInfo.CurrentCulture`(현재 스레드의 문화권)만 바꾸고 되돌린다. `CurrentUICulture`는 건드리지 않는다.

### 사람이 확인할 것
- 완료 조건에 [사람 확인] 항목은 없다.
- 없음

### 테스트 결과
- `python tools/loop/run_unity_tests.py` → EditMode: 33개 중 33개 통과, 0개 실패 (`CoreAssemblyTests.Core_DoesNotReferenceUnity`, 기존 `WalletTests` 포함).


## 루프 기록 (2026-11-16 16:01)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/002_gold_text
- 루프 브랜치: loop/20261116-155015
