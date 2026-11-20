# 060_realm_aura_plum — 매화 오오라 색과 상시 기운·꽃잎 (Core)

## 목표
`PlumSword.Core.Realm.RealmAura` 의 오오라 색을 매화 분홍 계열로 바꾸고, `BurstColor`, `Wisp`, `FloatingPetal` 을 더한다. 순수 C#(Core) 작업이다.

## 묶음
realm_fx

## 선행 작업
- 058_breakthrough_style_timing

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/realm.md ("경계 상황"의 `RealmAura`·`Wisp`·`FloatingPetal`·`index` 줄, "수치"의 오오라 색·`BurstColor`·`PulseAlpha`·"시간표와 공식"의 `Wisp`·`FloatingPetal`, "연결 지점"의 `RealmAura`, "완료 조건 초안" 26·46~48, "변경 기록"의 기존 테스트 표)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-20 — 매화 오오라와 단계별 돌파 연출` 항목만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. 이 작업은 그중 아래 범위만 맡는다.
- `Assets/Scripts/Core/Realm/RealmAura.cs` 를 고친다. 기존 상수와 `HasAura`, `PulseAlpha`, `BurstScale` 의 동작은 그대로 둔다.
- `Color(realm)` 값을 바꾼다 (R, G, B, A, 임시값): `SecondRate` (1.00, 0.72, 0.82, 0.35), `FirstRate` (1.00, 0.55, 0.75, 0.40), `Peak` (0.95, 0.35, 0.65, 0.45), `Transcendent` (1.00, 0.40, 0.70, 0.50), `ThirdRate` (0, 0, 0, 0). 알파는 이전과 같다.
- `public static AuraColor BurstColor(RealmLevel realm)`: `SecondRate`·`FirstRate`·`Peak` 는 `Color(realm)` 의 RGB 에 A 1.0, `Transcendent` 는 (1.00, 0.95, 0.97, 1.00), `ThirdRate` 는 (0, 0, 0, 0). 정의되지 않은 값은 `ArgumentOutOfRangeException`.
- `public static EffectPoint Wisp(RealmLevel realm, int index, double seconds)` (`EffectPoint` 는 058 이 만듦):
  - 검사 순서: 경지(정의되지 않으면 `ArgumentOutOfRangeException`) → `seconds` 가 `NaN`·무한대면 `ArgumentOutOfRangeException` → `index` 가 0 미만이거나 `n` 이상이면 `ArgumentOutOfRangeException`.
  - `n = BreakthroughEffect.Style(realm).AuraWisps` (058). 삼류는 `n` = 0 이라 어떤 `index` 든 예외.
  - `X = (index − (n − 1) / 2.0) × 0.45 + 0.12 × sin(2π × (seconds / 1.2 + index / (double)n))`, `Y = 0`, `Scale = 1 + 0.15 × sin(2π × (seconds / 0.9 + index / (double)n))`, `Alpha = min(1, 2 × PulseAlpha(realm, seconds + 0.4 × index))` (임시값).
- `public static EffectPoint FloatingPetal(RealmLevel realm, int index, double seconds)`:
  - 검사 순서와 예외는 `Wisp` 와 같다. 음수 `seconds` 는 그대로 계산한다.
  - `n = BreakthroughEffect.Style(realm).AuraPetals`. 이류·삼류는 `n` = 0 이라 어떤 `index` 든 예외.
  - `p = seconds / 3 + index / (double)n`, `f = p − Math.Floor(p)`. `X = 0.8 × sin(3π × p + index)`, `Y = −1.05 + 2.4 × f`, `Scale = 1`, `Alpha = sin(π × f)` (임시값).
- **기존 테스트**: `RealmAuraTests.Aura_ValidLevel_ReturnsPresenceAndColor`: 이름은 그대로 두고, `TestCase` 다섯 개 중 색 값만 바꾼다. `SecondRate` (0.55, 0.85, 1.0, 0.35) → (1.0, 0.72, 0.82, 0.35), `FirstRate` (0.35, 0.55, 1.0, 0.40) → (1.0, 0.55, 0.75, 0.40), `Peak` (0.70, 0.40, 1.0, 0.45) → (0.95, 0.35, 0.65, 0.45), `Transcendent` (1.0, 0.55, 0.75, 0.50) → (1.0, 0.40, 0.70, 0.50). `ThirdRate` 줄과 본문의 검사는 바꾸지 않는다. `TestCase` 수와 검사 수는 줄이지 않는다. 다른 기존 테스트는 고치지 않는다.
- Core 이므로 `UnityEngine` 을 쓰지 않는다. 시간은 인자로만 받는다.
- 구현하다 모양이 사양과 달라지면 사양 문서의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/Realm/RealmAuraTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다. 실수는 오차 1e-9 안에서 비교하고, `EffectPoint` 는 (X, Y, Scale, Alpha) 순서로 적는다.
- `RealmAura.Color` 는 `SecondRate` → (1.00, 0.72, 0.82, 0.35), `FirstRate` → (1.00, 0.55, 0.75, 0.40), `Peak` → (0.95, 0.35, 0.65, 0.45), `Transcendent` → (1.00, 0.40, 0.70, 0.50), `ThirdRate` → (0, 0, 0, 0) 을 돌려준다 (기존 `Aura_ValidLevel_ReturnsPresenceAndColor` 의 값 교체).
- `RealmAura.BurstColor` 는 `SecondRate` → (1.00, 0.72, 0.82, 1.00), `FirstRate` → (1.00, 0.55, 0.75, 1.00), `Peak` → (0.95, 0.35, 0.65, 1.00), `Transcendent` → (1.00, 0.95, 0.97, 1.00), `ThirdRate` → (0, 0, 0, 0) 을 돌려준다.
- `RealmAura.BurstColor((RealmLevel)99)` 는 `ArgumentOutOfRangeException` 을 던진다.
- `Wisp(SecondRate, 0, 0)` 은 (−0.225, 0, 1, 0.525) 다.
- `Wisp(SecondRate, 1, 0)` 은 (0.225, 0, 1, 0.7) 이다.
- `Wisp(SecondRate, 0, 0.3)` 은 (−0.105, 0, 1.1299038105676658, `0.7 × (0.75 + 0.25 × Math.Sin(2π × 0.3 / 1.6))` = 0.68667891…) 다.
- `Wisp(SecondRate, 2, 0)`, `Wisp(SecondRate, -1, 0)`, `Wisp(ThirdRate, 0, 0)`, `Wisp(SecondRate, 0, double.NaN)`, `Wisp(SecondRate, 0, double.PositiveInfinity)`, `Wisp((RealmLevel)99, 0, 0)` 은 각각 `ArgumentOutOfRangeException` 을 던진다.
- `FloatingPetal(FirstRate, 0, 0)` 은 (0, −1.05, 1, 0) 이다.
- `FloatingPetal(FirstRate, 0, 1.5)` 는 (−0.8, 0.15, 1, 1) 이다.
- `FloatingPetal(FirstRate, 1, 0)` 은 (`0.8 × Math.Sin(1.5π + 1)` = −0.43224184…, 0.15, 1, 1) 이다.
- `FloatingPetal(SecondRate, 0, 0)`, `FloatingPetal(FirstRate, 2, 0)`, `FloatingPetal(FirstRate, 0, double.NaN)`, `FloatingPetal((RealmLevel)99, 0, 0)` 은 각각 `ArgumentOutOfRangeException` 을 던진다.
- 기존 테스트가 모두 통과한다 (`Aura_ValidLevel_ReturnsPresenceAndColor` 는 이름 그대로, 값만 바뀐 채).

## 하지 말 것
- 기존 테스트 메서드를 지우거나 이름을 바꾸지 않는다. 검사 수를 줄이지 않는다.
- `HasAura`, `PulseAlpha`, `BurstScale` 와 상수의 동작을 바꾸지 않는다.
- `BreakthroughEffect`, `BreakthroughStyle`, `EffectPoint` (058·059) 와 그 테스트를 고치지 않는다.
- `RealmProgress`, `RealmRules`, `RealmText`, `AuraColor` 를 고치지 않는다.
- `Assets/Scripts/Game/` 은 고치지 않는다 (061·062 가 한다).
- `UnityEngine` 을 쓰지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 고치지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/` 를 고치지 않는다.

## 보고서

### 한 일
- 중단된 이전 실행의 작업 중 커밋(`25da913`)을 확인하고 이어서 완성했다. 구현과 테스트는 이미 들어 있었고, 요구사항·완료 조건과 한 줄씩 대조해 맞는 것을 확인한 뒤 에딧 모드 테스트를 돌렸다.
- `RealmAura.Color` 의 이류·일류·절정·초절정 색을 매화 분홍 계열로 바꿨다 (알파는 그대로).
- `RealmAura.BurstColor` 를 더했다: 이류·일류·절정은 `Color` 의 RGB + A 1.0, 초절정 (1.00, 0.95, 0.97, 1.00), 삼류 (0, 0, 0, 0), 정의되지 않은 값은 `ArgumentOutOfRangeException`.
- `RealmAura.Wisp`, `RealmAura.FloatingPetal` 을 더했다. 검사 순서는 경지(`BreakthroughEffect.Style` 호출에서 예외) → `seconds` 의 NaN·무한대 → `index` 범위. 공통 검사는 내부 함수 `CheckedCount` 로 묶었다.
- 기존 `Aura_ValidLevel_ReturnsPresenceAndColor` 의 `TestCase` 네 개의 색 값만 바꾸고(이름·개수·본문 검사 그대로), `BurstColor`·`Wisp`·`FloatingPetal` 테스트를 새로 넣었다.
- 첫 시도는 관문에서 "검사(Assert)가 하나도 없는 테스트" 위반을 받았다. `Wisp`·`FloatingPetal` 위치 테스트 여섯 개가 검사를 공용 도우미 `AssertPoint` 에 맡겨서, 메서드 본문에 `Assert` 가 직접 없었기 때문이다. 도우미를 없애고 여섯 테스트마다 본문에 `Assert.That` 네 줄(X, Y, Scale, Alpha)을 직접 썼다. 검사하는 값과 오차(1e-9)는 같다. 관문의 `test_methods`·`ASSERT_RE` 로 파일을 직접 검사해 검사 없는 테스트가 0개인 것을 확인했다.
- 사양 `docs/spec/realm.md` 는 이미 이번 변경(수치·시간표·연결 지점·경계 상황)을 담고 있어 고칠 것이 없었다.

### 바뀐 파일
- `Assets/Scripts/Core/Realm/RealmAura.cs`
- `Assets/Tests/EditMode/Realm/RealmAuraTests.cs`
- `tasks/doing/060_realm_aura_plum.md` (이 보고서)

### 가정
- 수치는 모두 작업 문서·사양의 임시값을 그대로 썼다. 새로 정한 수치는 없다.
- `Wisp`·`FloatingPetal` 의 경지 검사는 `BreakthroughEffect.Style(realm)` 이 정의되지 않은 경지에 던지는 `ArgumentOutOfRangeException` 에 맡겼다 (요구된 검사 순서와 같다).
- `BurstColor` 의 이류·일류·절정은 `Color(realm)` 을 불러 RGB 를 가져온다 (값을 따로 적지 않음). `Color` 가 바뀌면 함께 바뀐다.
- 연결 지점 모양은 사양과 같다.

### 사람이 확인할 것
- 완료 조건에 `[사람 확인]` 항목은 없다.
- 새 색(매화 분홍)과 기운·꽃잎의 움직임이 화면에서 보기 좋은지는 화면 코드(061·062)가 붙은 뒤 사람이 확인해야 한다.

### 테스트 결과
- 에딧 모드 전체: 783개 중 783개 통과, 0개 실패 (`tools/loop/run_unity_tests.py --platform EditMode`, 관문 위반을 고친 뒤 다시 실행).
- 관문 규칙 확인: `RealmAuraTests.cs` 의 테스트 메서드 18개 모두 본문에 검사가 있다 (`loop_gate.test_methods` + `ASSERT_RE` 로 확인. 관문 전체는 커밋끼리 비교해서 커밋 권한이 없는 이 단계에서는 돌리지 못했다).
- 이번 작업 관련: `Aura_ValidLevel_ReturnsPresenceAndColor` 5건, `BurstColor_ValidLevel_ReturnsColor` 5건, `BurstColor_InvalidLevel_Throws`, `Wisp_SecondRateFirstAtZero/SecondAtZero/FirstAtPointThree_ReturnsExpectedPoint`, `Wisp_InvalidInput_Throws` 6건, `FloatingPetal_FirstRateFirstAtZero/FirstAtOneAndHalf/SecondAtZero_ReturnsExpectedPoint`, `FloatingPetal_InvalidInput_Throws` 4건 모두 통과.


## 루프 기록 (2026-11-20 20:18)

- 작업 에이전트: claude(시도 1~2)
- 리뷰: claude (같은 에이전트) 통과
- 기능 브랜치: task/060_realm_aura_plum
- 루프 브랜치: loop/20261120-175740
