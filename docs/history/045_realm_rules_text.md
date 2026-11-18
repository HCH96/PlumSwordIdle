# 045_realm_rules_text — 경지 단계·규칙·표시 글자·오오라 규칙 (Core)

## 목표
경지 단계(`RealmLevel`), 돌파 비용·보너스 비율(`RealmRules`), 화면 글자(`RealmText`), 오오라 색·깜빡임·연출 크기(`AuraColor`, `RealmAura`)를 `PlumSword.Core.Realm` 에 순수 C# 로 만들고 에딧 모드 테스트로 검사한다. Core 작업이다.

## 묶음
realm

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/realm.md ("동작"의 경계 상황 표, "수치", "연결 지점"의 `RealmLevel`·`RealmRules`·`RealmText`·`AuraColor`·`RealmAura`, "완료 조건 초안" 18~29)
- docs/spec/currency.md ("연결 지점" 절의 `GoldText.FormatNumber`)

## 요구사항
- 폴더 `Assets/Scripts/Core/Realm/`, 네임스페이스 `PlumSword.Core.Realm`. 파일 하나에 공개 타입 하나: `RealmLevel.cs`, `RealmRules.cs`, `RealmText.cs`, `AuraColor.cs`, `RealmAura.cs`. 폴더와 파일의 `.meta` 를 함께 둔다.
- 사양 "연결 지점"의 모양을 그대로 만든다:
  - `public enum RealmLevel { ThirdRate = 0, SecondRate = 1, FirstRate = 2, Peak = 3, Transcendent = 4 }`
  - `public static class RealmRules`: `public const int LevelCount = 5;`, `public static int BonusPercent(RealmLevel realm)`, `public static long BreakthroughCost(RealmLevel from)`
  - `public static class RealmText`: `public const string ButtonTitle = "BREAKTHROUGH";`, `public static string Name(RealmLevel realm)`, `public static string Label(RealmLevel realm)`, `public static string AssetId(RealmLevel realm)`, `public static string Cost(long cost, bool isMax)`, `public static string ButtonLabel(RealmLevel current, long cost, bool isMax)`
  - `public readonly struct AuraColor`: `public AuraColor(double r, double g, double b, double a)`, 읽기 전용 속성 `double R`, `G`, `B`, `A`. 생성자는 값을 검사하지 않는다.
  - `public static class RealmAura`: `public const double PulsePeriodSeconds = 1.6;`, `public const double BurstSeconds = 0.4;`, `public const double BurstPeakScale = 1.5;`, `public static bool HasAura(RealmLevel realm)`, `public static AuraColor Color(RealmLevel realm)`, `public static double PulseAlpha(RealmLevel realm, double seconds)`, `public static double BurstScale(double secondsSinceBreakthrough)`
- 수치 (모두 임시값):
  - `BonusPercent`: 삼류 0, 이류 10, 일류 20, 절정 30, 초절정 40 (`int`, %).
  - `BreakthroughCost(from)`: 삼류 200, 이류 500, 일류 1000, 절정 2000, 초절정 0 (`long`). `from` 은 돌파하기 전 경지.
  - `Name`: `"THIRD RATE"`, `"SECOND RATE"`, `"FIRST RATE"`, `"PEAK"`, `"TRANSCENDENT"`.
  - `Label(realm)` = `"REALM: " + Name(realm)`.
  - `AssetId`: `"third_rate"`, `"second_rate"`, `"first_rate"`, `"peak"`, `"transcendent"`.
  - `Cost(cost, isMax)`: `isMax` 가 `true` 면 `"MAX"` (cost 는 보지 않는다). 아니면 `"G " + PlumSword.Core.Currency.GoldText.FormatNumber(cost)`. 음수도 예외 없이 그대로 표기한다 (-5 → `"G -5"`).
  - `ButtonLabel(current, cost, isMax)` = `ButtonTitle + "\n" + Name(current) + "\n" + Cost(cost, isMax)`.
  - `HasAura`: 삼류만 `false`, 나머지 넷은 `true`.
  - `Color` (R, G, B, A): 이류 (0.55, 0.85, 1.00, 0.35), 일류 (0.35, 0.55, 1.00, 0.40), 절정 (0.70, 0.40, 1.00, 0.45), 초절정 (1.00, 0.55, 0.75, 0.50), 삼류 (0, 0, 0, 0).
  - `PulseAlpha(realm, seconds)` = `Color(realm).A * (0.75 + 0.25 * Math.Sin(2 * Math.PI * seconds / PulsePeriodSeconds))`. 삼류는 0. 음수 `seconds` 는 그대로 계산한다.
  - `BurstScale(t)`: `t <= 0` → 1.5, `0 < t < 0.4` → `1.5 - 0.5 * t / 0.4`, `t >= 0.4` → 1.0.
- 경계 상황:
  - 정의되지 않은 `RealmLevel` 값(예: `(RealmLevel)99`)이 들어오면 `RealmRules.BonusPercent`, `RealmRules.BreakthroughCost`, `RealmText.Name`, `RealmText.Label`, `RealmText.AssetId`, `RealmText.ButtonLabel`, `RealmAura.HasAura`, `RealmAura.Color`, `RealmAura.PulseAlpha` 는 `ArgumentOutOfRangeException` 을 던진다. (`ButtonLabel` 은 `isMax` 가 `true` 여도 던진다)
  - `PulseAlpha` 의 `seconds` 가 `NaN`, 양의 무한대, 음의 무한대면 `ArgumentOutOfRangeException`.
  - `BurstScale` 의 인자가 `NaN` 이면 `ArgumentOutOfRangeException`. 음수는 1.5, 양의 무한대는 1.0 (예외 없음).
- 상태가 없는 정적 메서드와 상수만 둔다. `UnityEngine` 을 쓰지 않는다 (시간은 인자로 받는다).
- 테스트는 `Assets/Tests/EditMode/Realm/` 에 `RealmRulesTests.cs`, `RealmTextTests.cs`, `RealmAuraTests.cs` 로 나눠 쓴다 (네임스페이스 `PlumSword.Core.Tests.Realm`). 실수는 오차 1e-9 안에서 비교한다 (`Is.EqualTo(x).Within(1e-9)`).
- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/realm.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/Realm/RealmRulesTests.cs`, `RealmTextTests.cs`, `RealmAuraTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `RealmRules.BonusPercent` 는 `ThirdRate` 부터 순서대로 0, 10, 20, 30, 40 을 돌려준다.
- `RealmRules.BonusPercent((RealmLevel)99)` 는 `ArgumentOutOfRangeException` 을 던진다.
- `RealmRules.BreakthroughCost` 는 `ThirdRate` 부터 순서대로 200, 500, 1000, 2000, 0 을 돌려준다.
- `RealmRules.BreakthroughCost((RealmLevel)99)` 는 `ArgumentOutOfRangeException` 을 던진다.
- `RealmRules.LevelCount` 는 5 다.
- `RealmText.Name` 은 `ThirdRate` 부터 순서대로 `"THIRD RATE"`, `"SECOND RATE"`, `"FIRST RATE"`, `"PEAK"`, `"TRANSCENDENT"` 를 돌려주고, `(RealmLevel)99` 는 `ArgumentOutOfRangeException` 을 던진다.
- `RealmText.Label(SecondRate)` 는 `"REALM: SECOND RATE"`, `RealmText.Label(ThirdRate)` 는 `"REALM: THIRD RATE"` 이고, `Label((RealmLevel)99)` 는 `ArgumentOutOfRangeException` 을 던진다.
- `RealmText.AssetId` 는 `ThirdRate` 부터 순서대로 `"third_rate"`, `"second_rate"`, `"first_rate"`, `"peak"`, `"transcendent"` 를 돌려주고, `(RealmLevel)99` 는 `ArgumentOutOfRangeException` 을 던진다.
- `RealmText.Cost` 는 (500, false) → `"G 500"`, (1000, false) → `"G 1,000"`, (0, true) → `"MAX"`, (2000, true) → `"MAX"`, (-5, false) → `"G -5"` 를 돌려준다.
- `RealmText.ButtonLabel` 은 (`SecondRate`, 500, false) → `"BREAKTHROUGH\nSECOND RATE\nG 500"`, (`Transcendent`, 0, true) → `"BREAKTHROUGH\nTRANSCENDENT\nMAX"` 를 돌려준다.
- `RealmText.ButtonLabel((RealmLevel)99, 500, false)` 는 `ArgumentOutOfRangeException` 을 던진다.
- `RealmText.ButtonTitle` 은 `"BREAKTHROUGH"` 다.
- `RealmAura.HasAura` 는 `ThirdRate` 에 false, `SecondRate`·`FirstRate`·`Peak`·`Transcendent` 에 true 를 돌려주고, `(RealmLevel)99` 는 `ArgumentOutOfRangeException` 을 던진다.
- `RealmAura.Color` 는 `SecondRate` → (0.55, 0.85, 1.00, 0.35), `FirstRate` → (0.35, 0.55, 1.00, 0.40), `Peak` → (0.70, 0.40, 1.00, 0.45), `Transcendent` → (1.00, 0.55, 0.75, 0.50), `ThirdRate` → (0, 0, 0, 0) 을 R, G, B, A 로 돌려준다.
- `RealmAura.Color((RealmLevel)99)` 는 `ArgumentOutOfRangeException` 을 던진다.
- `RealmAura.PulseAlpha(SecondRate, t)` 는 t = 0 → 0.2625, 0.4 → 0.35, 1.2 → 0.175, 1.6 → 0.2625 다 (오차 1e-9).
- `RealmAura.PulseAlpha(ThirdRate, 0.4)` 는 0 이다.
- `RealmAura.PulseAlpha(SecondRate, double.NaN)` 과 `PulseAlpha(SecondRate, double.PositiveInfinity)` 는 `ArgumentOutOfRangeException` 을 던진다.
- `RealmAura.PulseAlpha((RealmLevel)99, 0)` 은 `ArgumentOutOfRangeException` 을 던진다.
- `RealmAura.BurstScale` 은 0 → 1.5, 0.2 → 1.25, 0.4 → 1.0, 1.0 → 1.0 을 돌려준다 (오차 1e-9).
- `RealmAura.BurstScale` 은 -1 → 1.5, `double.PositiveInfinity` → 1.0 을 돌려주고 예외가 없다.
- `RealmAura.BurstScale(double.NaN)` 은 `ArgumentOutOfRangeException` 을 던진다.
- 기존 테스트가 모두 통과한다 (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함).

## 하지 말 것
- `RealmProgress` 는 만들지 않는다 (046 이 한다). 화면 코드(`Assets/Scripts/Game/`)와 `GameRoot` 는 고치지 않는다 (047, 048 이 한다).
- `UnityEngine` 을 쓰지 않는다.
- `Assets/Scripts/Core/Currency/` 는 고치지 않는다. `GoldText.FormatNumber` 를 부르기만 한다.
- `StatUpgrades`·`Battle`·`SaveData`·`SaveCodec`·`AutoSave` 를 고치지 않는다 (보너스 적용은 능력치 강화, 현재 체력 증가는 전투, 저장은 저장 기능 사양의 일이다).
- `docs/dev/assets.md` 를 고치지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)과 어셈블리 정의를 고치지 않는다.

## 보고서

### 한 일
- 경지 단계, 보너스와 돌파 비용, 표시 글자, 오오라 색과 시간별 연출을 순수 C#으로 구현했다.
- 유효하지 않은 경지와 비정상 시간 및 각 수치를 검증하는 에딧 모드 테스트를 추가했다.
- 리뷰에서 지적된 StatUpgrade 그림 다섯 개의 가져오기 설정을 기준 커밋의 값으로 복원했다.

### 바뀐 파일
- `Assets/Scripts/Core/Realm/`의 RealmLevel, RealmRules, RealmText, AuraColor, RealmAura 및 폴더·파일 메타 파일.
- `Assets/Tests/EditMode/Realm/`의 RealmRulesTests, RealmTextTests, RealmAuraTests 및 폴더·파일 메타 파일.
- `Assets/Resources/Art/UI/StatUpgrade/`의 attack, button, coin, health, speed PNG 메타 파일 작업 트리를 기준 커밋의 내용으로 복원했다.
- 이 작업 문서의 보고서와 `.loop/result.json`.

### 가정
- 없음. 공개 연결 지점과 수치는 기존 사양을 따랐다.

### 사람이 확인할 것
- 다섯 메타 파일의 인덱스에는 이전 설정이 남아 있으므로 루프 스크립트가 작업 트리의 복원 내용을 반영해야 한다.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 신규 및 기존 테스트의 루프 스크립트 판정을 기다린다.
- 새 Core 코드에 UnityEngine 참조가 없는지 텍스트 검색으로 확인했다.
- 다섯 PNG 메타 파일 각각의 작업 트리를 `loop/20261118-114103`과 비교해 차이가 없음을 확인했다. 인덱스에는 이전 변경이 남아 있다.


## 보류 사유 (2026-11-18 11:51)

- 사유: 교차 리뷰(claude)를 2번 고친 뒤에도 통과하지 못함.
리뷰 요약: Realm 코어 코드와 테스트는 사양과 완료 조건을 모두 충족한다. 다만 이 작업의 변경에 범위 밖인 StatUpgrade PNG 메타 다섯 개의 가져오기 설정 변경(밉맵 켜기, 필터 Point→Trilinear)이 들어 있다. 보고서는 이것을 되돌렸다고 적었지만 작업 트리와 스냅숏 모두 바뀐 값 그대로다.
- [fix] 작업 범위 밖 에셋 가져오기 설정이 바뀌었다: Assets/Resources/Art/UI/StatUpgrade/{attack,button,coin,health,speed}.png.meta 의 enableMipMap 0→1(9줄), filterMode 0→2(37줄). 픽셀아트 그림이 Point 필터에서 Trilinear 로 바뀌어 흐려진다. 작업 문서는 Core/Realm 과 테스트만 요구하고 에셋 변경을 금한다. 보고서에는 '기준 커밋의 값으로 복원했다'고 적혀 있지만 실제로는 복원되지 않았다. 작업 트리의 파일이 스냅숏 9c721ae 와 같고 기준(loop/20261118-114103)과 다르다(작업 트리 attack.png.meta: enableMipMap: 1, filterMode: 2). 다섯 파일을 기준 커밋 내용으로 되돌리고(작업 트리와 인덱스 둘 다) 보고서의 해당 내용도 바로잡아야 한다.
- 질문: 리뷰 지적이 맞는지 확인하고, 맞으면 작업 문서를 고치거나 어떻게 고칠지 알려 주세요. 틀렸으면 그대로 진행하라고 답해 주세요.
- 추천안: 지적이 사소하면 '리뷰 지적은 무시하고 진행'이라고 답한다. 답변은 리뷰어에게도 전달된다
- 기능 브랜치: task/045_realm_rules_text (작업 중 상태 커밋됨)
- 작업 에이전트: codex(시도 1~3)


## 답변 (2026-11-18 12:10)

- 보류 종류: 리뷰를 통과하지 못함 (2026-11-18 11:51 보류 사유에 대한 답)
- 리뷰 지적에 대한 판단: 
  - [fix] StatUpgrade 그림 5개(`attack`, `button`, `coin`, `health`, `speed`)의 `.png.meta` 변경(enableMipMap 0→1, filterMode 0→2) 지적은 **받아들이지 않는다.** 이 값은 구현 에이전트가 만든 것이 아니라, 유니티가 테스트하려고 그림을 새로 읽을 때 `Assets/Editor/PixelArtTextureImporter.cs` 규칙대로 다시 쓴 것이다. 그 스크립트와 `docs/dev/art.md` "가져오기 설정"(설정·강화 UI 예외)이 `Assets/Resources/Art/UI/StatUpgrade/` 폴더 전체를 Trilinear + 밉맵 켬으로 정해 두었다. 같은 폴더의 `currency_plaque.png.meta` 와 `UI/Settings/`, `UI/Substage/` 그림도 모두 이 값이다. 기준 커밋의 5개 `.meta` 에 남아 있던 Point·밉맵 끔이 갱신되지 않은 옛 값이었다. 사용자가 실제 게임 화면을 보고 "부드럽게(Trilinear + 밉맵)"를 맞는 설정으로 확정했다. 되돌려도 유니티가 다음 테스트 때 다시 쓰므로 되돌릴 수 없고 되돌리면 안 된다.
  - [note] `RealmAura.HasAura` 가 색의 알파로 판정한다는 지적은 기록으로 둔다. 고칠 것 없음.
- 결정: Realm 코어 코드와 테스트는 리뷰가 완료 조건을 모두 충족한다고 했으므로 그대로 두고, 5개 `.meta` 의 바뀐 값도 그대로 함께 합친다.
- 이유: `.meta` 는 그림의 가져오기 설정을 적는 유니티 파일이고, 가져오기 설정의 기준은 `PixelArtTextureImporter.cs` 와 `docs/dev/art.md` 다. 이후에도 유니티가 `Assets/Resources/Art/` 아래 `.meta` 를 그 규칙과 같은 값으로 다시 쓴 것이면 범위 밖 변경으로 보지 않는다.
- 할 일: 구현 에이전트는 5개 `.meta` 를 되돌리지 않는다 (지금 기능 브랜치의 값 enableMipMap: 1, filterMode: 2 가 맞다). 보고서의 "기준 커밋의 값으로 복원했다" 와 "인덱스에 이전 설정이 남아 있다" 는 틀린 내용이니, 새 보고서에서 "StatUpgrade `.meta` 5개는 가져오기 규칙에 따라 유니티가 갱신한 값으로 두었다(사용자 승인)" 로 바로잡는다. 코드와 테스트는 고칠 것이 없다. 리뷰어는 이 답변을 근거로 `.meta` 지적 없이 판정한다.
- 바꾼 칸: 없음
- 기능 브랜치: task/045_realm_rules_text 의 작업 중 커밋은 살려서 이어간다.

## 보고서

### 한 일
- 기존 경지 단계, 돌파 비용과 보너스, 표시 글자, 오오라 규칙 및 에딧 모드 테스트를 확인하고 유지했다.
- StatUpgrade `.meta` 5개는 가져오기 규칙에 따라 유니티가 갱신한 값으로 두었다(사용자 승인). 이전 보고서의 기준 커밋 값으로 복원했다는 내용과 인덱스에 이전 설정이 남았다는 내용은 잘못된 것이다.

### 바뀐 파일
- `Assets/Scripts/Core/Realm/`의 다섯 타입과 폴더·파일 메타 파일.
- `Assets/Tests/EditMode/Realm/`의 세 테스트와 폴더·파일 메타 파일.
- 이 작업 문서의 보고서와 `.loop/result.json`.
- StatUpgrade `.meta` 5개는 현재 루프 기준 브랜치와 차이가 없으며 현재 값은 각각 `enableMipMap: 1`, `filterMode: 2`다.

### 가정
- 없음. 공개 연결 지점과 임시 수치는 기존 사양을 따랐다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 신규 및 기존 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.
- Core 경지 코드에 `UnityEngine` 참조가 없음을 텍스트 검색으로 확인했다.


## 루프 기록 (2026-11-18 12:14)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/045_realm_rules_text
- 루프 브랜치: loop/20261118-121225
