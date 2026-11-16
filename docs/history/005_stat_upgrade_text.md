# 005_stat_upgrade_text — 강화 버튼 표시 글자

## 목표
`PlumSword.Core` 에 강화 버튼에 보여 줄 글자를 만드는 표시용 클래스 `StatUpgradeText` 를 만든다. 이름(`Name`), 단계(`Level`), 비용(`Cost`), 버튼 글자 세 줄(`ButtonLabel`)을 제공하고 에딧 모드 테스트로 검사한다.

## 묶음
강화

## 선행 작업
- 002_gold_text
- 004_stat_upgrades

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stat_upgrade.md ("수치" 절의 표기 줄 넷, "연결 지점" 절의 `StatUpgradeText`, "경계 상황" 표의 `StatType`·`StatUpgradeText` 줄)
- docs/spec/currency.md ("연결 지점" 절의 `GoldText.FormatNumber`)

## 요구사항
- 파일: `Assets/Scripts/Core/StatUpgrade/StatUpgradeText.cs`. 네임스페이스 `PlumSword.Core.StatUpgrade`. 파일 하나에 공개 타입 하나.
- 테스트 파일: `Assets/Tests/EditMode/StatUpgrade/StatUpgradeTextTests.cs`. 네임스페이스 `PlumSword.Core.Tests.StatUpgrade`, 클래스 `StatUpgradeTextTests`. 테스트 이름은 `<상황>_<기대 결과>` 모양으로 짓는다.
- 선행 작업이 만든 것을 쓴다. 내부는 고치지 않는다.
  - 004_stat_upgrades: `PlumSword.Core.StatUpgrade.StatType` — `Attack = 0`, `Health = 1`, `AttackSpeed = 2`. 이 작업은 `StatUpgrades` 클래스를 쓰지 않는다 (값은 인자로 받는다).
  - 002_gold_text: `PlumSword.Core.Currency.GoldText.FormatNumber(long amount)` — 세 자리마다 쉼표를 넣은 글자를 돌려준다 (예: 20 → `20`, 1234 → `1,234`, -5 → `-5`). 문화권과 상관없이 항상 쉼표다.
- 공개 모양은 사양의 "연결 지점" 절 그대로다. 화면 작업(006_stat_upgrade_view)이 이 이름으로 부른다.

  ```csharp
  namespace PlumSword.Core.StatUpgrade
  {
      public static class StatUpgradeText
      {
          public static string Name(StatType stat);
          public static string Level(int level);
          public static string Cost(long cost, bool isMaxLevel);
          public static string ButtonLabel(StatType stat, int level, long cost, bool isMaxLevel);
      }
  }
  ```

- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/stat_upgrade.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- `Name(stat)`: `Attack` → `ATK`, `Health` → `HP`, `AttackSpeed` → `SPD`. 정의되지 않은 값(예: `(StatType)99`)은 `System.ArgumentOutOfRangeException`. 1단계에는 한글 글꼴이 없어 한글을 쓰지 않는다.
- `Level(level)`: `"Lv." + level.ToString(System.Globalization.CultureInfo.InvariantCulture)`. 쉼표를 넣지 않는다. 예: 0 → `Lv.0`, 10 → `Lv.10`. 음수도 그대로 표기한다 (예외 없음. -1 → `Lv.-1`).
- `Cost(cost, isMaxLevel)`: `isMaxLevel` 이 `true` 면 `MAX` (`cost` 는 보지 않는다). 아니면 `"G " + GoldText.FormatNumber(cost)`. `G` 뒤에 띄어쓰기 한 칸. 예: 20 → `G 20`, 1234 → `G 1,234`. 음수도 그대로 표기한다 (예외 없음. -5 → `G -5`).
- `ButtonLabel(stat, level, cost, isMaxLevel)`: `Name(stat) + "\n" + Level(level) + "\n" + Cost(cost, isMaxLevel)`. 줄바꿈은 `\n` 한 글자다 (`Environment.NewLine` 을 쓰지 않는다). 예: (`Attack`, 3, 35, `false`) → `ATK\nLv.3\nG 35`, (`AttackSpeed`, 10, 70, `true`) → `SPD\nLv.10\nMAX`. 정의되지 않은 `stat` 은 `System.ArgumentOutOfRangeException`.
- 시간·난수·저장을 쓰지 않는다. 상태가 없는 정적 메서드다.
- 사양에 없는 공개 멤버를 더하지 않는다.

## 완료 조건
- `Assets/Tests/EditMode/StatUpgrade/StatUpgradeTextTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `StatUpgradeText.Name` 은 `Attack` → `"ATK"`, `Health` → `"HP"`, `AttackSpeed` → `"SPD"` 를 돌려준다.
- `StatUpgradeText.Name((StatType)99)` 는 `ArgumentOutOfRangeException` 을 던진다.
- `StatUpgradeText.Level` 은 0 → `"Lv.0"`, 3 → `"Lv.3"`, 10 → `"Lv.10"`, 1234 → `"Lv.1234"`, -1 → `"Lv.-1"` 을 돌려준다.
- `StatUpgradeText.Cost` 는 (20, `false`) → `"G 20"`, (1234, `false`) → `"G 1,234"`, (70, `true`) → `"MAX"`, (-5, `false`) → `"G -5"` 를 돌려준다.
- `StatUpgradeText.ButtonLabel` 은 (`Attack`, 3, 35, `false`) → `"ATK\nLv.3\nG 35"`, (`Health`, 0, 20, `false`) → `"HP\nLv.0\nG 20"`, (`AttackSpeed`, 10, 70, `true`) → `"SPD\nLv.10\nMAX"` 를 돌려준다.
- `StatUpgradeText.ButtonLabel((StatType)99, 0, 20, false)` 는 `ArgumentOutOfRangeException` 을 던진다.
- 기존 테스트가 모두 통과한다. (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)

## 하지 말 것
- `UnityEngine` 을 쓰지 않는다. `PlumSword.Core` 는 순수 C# 이다.
- `StatType.cs`, `StatUpgrades.cs` 와 `Assets/Scripts/Core/Currency/` 를 고치지 않는다. 공개된 모양으로만 쓴다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. `StatUpgradeView` 는 006_stat_upgrade_view 가 한다.
- 한글 표기(`공격력`, `최대`, `35금화`), 현재 능력치 값 표기(예: `ATK 16`), 줄임 표기(K·M)를 만들지 않는다. 사양의 "범위 밖"이다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다.
- 새 어셈블리 정의를 만들지 않는다. 기존 `.asmdef` 를 고치지 않는다.
- 사람용 문서(`docs/design/`, `docs/project/`, `docs/archive/`)를 읽거나 고치지 않는다.

## 보고서

### 한 일
- `PlumSword.Core.StatUpgrade.StatUpgradeText` 정적 클래스를 만들었다. `Name`, `Level`, `Cost`, `ButtonLabel` 네 메서드로, 사양 "연결 지점" 절의 모양과 같다.
- `Cost` 는 `GoldText.FormatNumber` 를 써서 쉼표를 넣고, `Level` 은 `InvariantCulture` 로 쉼표 없이 표기한다. `ButtonLabel` 은 세 줄을 `\n` 으로 잇고, 정의되지 않은 `stat` 은 `Name` 을 거치므로 `ArgumentOutOfRangeException` 을 던진다.
- 완료 조건마다 에딧 모드 테스트를 썼다 (`StatUpgradeTextTests`).

### 바뀐 파일
- `Assets/Scripts/Core/StatUpgrade/StatUpgradeText.cs` (+ `.meta`, 유니티가 자동 생성)
- `Assets/Tests/EditMode/StatUpgrade/StatUpgradeTextTests.cs` (+ `.meta`, 유니티가 자동 생성)

### 가정
- `ArgumentOutOfRangeException` 의 메시지는 `"정의되지 않은 능력치"` 로 정했다 (테스트는 예외 종류만 검사한다).
- 완료 조건 밖으로 테스트 하나를 더했다: 현재 문화권이 `de-DE` 여도 `Level(1234)` 가 `Lv.1234` 인지 확인한다.
- 사양·연결 지점은 바꾸지 않았다 (사양 그대로 구현). 공용 문서 갱신은 필요 없었다 (`docs/spec/index.md` 에 `StatUpgradeText` 가 이미 있다).

### 사람이 확인할 것
- 없음 (이 작업의 완료 조건에 [사람 확인] 항목이 없다. 버튼에 실제로 보이는 모습은 006_stat_upgrade_view 에서 확인한다).

### 테스트 결과
- `python tools/loop/run_unity_tests.py` — EditMode 86개 중 86개 통과, 0개 실패 (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함).


## 루프 기록 (2026-11-16 16:21)

- 작업 에이전트: claude(시도 1)
- 기능 브랜치: task/005_stat_upgrade_text
- 루프 브랜치: loop/20261116-161636
