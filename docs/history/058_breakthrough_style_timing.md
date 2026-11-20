# 058_breakthrough_style_timing — 돌파 연출 구성과 화면 효과 시간표 (Core)

## 목표
`PlumSword.Core.Realm` 에 `EffectPoint`, `BreakthroughStyle` 과, `BreakthroughEffect` 의 상수·`Style`·`IsPlaying`·`ShowsNewAura`·어둡게·빛·블룸·번쩍임·흔들림 함수를 만든다. 순수 C#(Core) 작업이다. 부품(고리·기둥·충격파·꽃잎) 함수는 059 가 같은 클래스에 더한다.

## 묶음
realm_fx

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/realm.md ("경계 상황"의 `BreakthroughEffect` 줄, "수치"의 "단계별 돌파 연출 구성"·"시간표와 공식", "연결 지점"의 `EffectPoint`·`BreakthroughStyle`·`BreakthroughEffect`, "완료 조건 초안" 34~41·45)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-20 — 매화 오오라와 단계별 돌파 연출` 항목만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. 이 작업은 그중 아래 범위만 맡는다.
- 파일 하나에 공개 타입 하나: `Assets/Scripts/Core/Realm/EffectPoint.cs`, `BreakthroughStyle.cs`, `BreakthroughEffect.cs` (`.meta` 함께). 네임스페이스 `PlumSword.Core.Realm`. 기존 `AuraColor.cs` 처럼 쓴다.
- `public readonly struct EffectPoint` — 생성자 `EffectPoint(double x, double y, double scale, double alpha)`, 속성 `X`, `Y`, `Scale`, `Alpha`. 값을 검사하지 않는다.
- `public readonly struct BreakthroughStyle` — 생성자 `BreakthroughStyle(bool hasRing, bool hasPillar, bool hasShockwave, bool hasFlash, int burstPetals, double dimAlpha, int shakePixels, double lightPeak, double ringRadius, double pillarHeight, int auraWisps, int auraPetals)`, 같은 이름의 속성 12개(`HasRing`, `HasPillar`, `HasShockwave`, `HasFlash`, `BurstPetals`, `DimAlpha`, `ShakePixels`, `LightPeak`, `RingRadius`, `PillarHeight`, `AuraWisps`, `AuraPetals`). 값을 검사하지 않는다.
- `public static class BreakthroughEffect`:
  - 상수: `BurstSeconds = 0.5`, `AfterglowEndSeconds = 1.3`, `TotalSeconds = 1.8`, `FlashSeconds = 0.15`, `ShakeSeconds = 0.3`, `ShockwaveSeconds = 0.4`, `GatherRadius = 3.0`, `PillarWidth = 1.2` (모두 `double`, 임시값).
  - `BreakthroughStyle Style(RealmLevel realm)` — 표 값 (임시값). 칸 순서는 생성자 순서:
    - `ThirdRate`: `false, false, false, false, 0, 0, 0, 0, 0, 0, 0, 0`
    - `SecondRate`: `false, false, false, false, 12, 0.25, 0, 1.0, 0, 0, 2, 0`
    - `FirstRate`: `true, false, false, false, 18, 0.35, 1, 1.5, 1.5, 0, 2, 2`
    - `Peak`: `true, true, false, false, 24, 0.45, 2, 2.0, 2.0, 5.0, 3, 3`
    - `Transcendent`: `true, true, true, true, 36, 0.60, 3, 3.0, 2.5, 7.0, 4, 5`
    - 정의되지 않은 값: `ArgumentOutOfRangeException`.
  - 시간 인자 규칙(아래 모든 시간 함수 공통): `NaN` 이면 `ArgumentOutOfRangeException`. 음수·음의 무한대는 0 으로 바꿔 계산하고, 양의 무한대는 그대로 두어 "끝난 뒤"(1.8 이상)로 계산한다. 경지를 받는 함수는 시간 검사와 별개로 정의되지 않은 경지면 `ArgumentOutOfRangeException` (`Style(realm)` 을 불러 검사하면 된다).
  - `bool IsPlaying(double seconds)` = `t < 1.8`. `bool ShowsNewAura(double seconds)` = `t ≥ 0.5`.
  - `double DimAlpha(RealmLevel realm, double seconds)`: `D = Style(realm).DimAlpha`. `t < 0.5` → `D × t / 0.5`, `0.5 ≤ t < 1.3` → `D`, `1.3 ≤ t < 1.8` → `D × (1.8 − t) / 0.5`, `t ≥ 1.8` → 0.
  - `double DimMask(double distance)` = `clamp((d − 1.5) / 2.5, 0, 1)`. `NaN` 은 `ArgumentOutOfRangeException`. 음수는 0 (공식대로 0 이 나온다). 양의 무한대는 1.
  - `double LightIntensity(RealmLevel realm, double seconds)`: `L = Style(realm).LightPeak`. `t < 0.5` → `0.4 × L × t / 0.5`, `0.5 ≤ t < 1.3` → `L × (1.3 − t) / 0.8`, `t ≥ 1.3` → 0.
  - `double BloomIntensity(RealmLevel realm, double seconds)` = `LightIntensity(realm, seconds) × 0.5`.
  - `double FlashAlpha(RealmLevel realm, double seconds)`: `Style(realm).HasFlash` 이고 `0.5 ≤ t < 0.65` 면 `0.8 × (0.65 − t) / 0.15`, 그 밖 0.
  - `int ShakePixels(RealmLevel realm, double seconds)`: `0.5 ≤ t < 0.8` 이면 `Style(realm).ShakePixels`, 아니면 0.
  - 구간 경계 비교에는 위 상수를 쓴다 (`0.65` = `BurstSeconds + FlashSeconds`, `0.8` = `BurstSeconds + ShakeSeconds`). 실수 오차로 경계값(0.65, 0.8)이 엇갈리지 않게, 예를 들어 `t − BurstSeconds < FlashSeconds` 대신 `t < 0.65` 처럼 완료 조건의 경계값이 그대로 맞는 식을 쓴다.
- Core 이므로 `UnityEngine` 을 쓰지 않는다. 시간은 인자로만 받는다.
- 구현하다 모양이 사양과 달라지면 사양 문서의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/Realm/BreakthroughEffectTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다. 실수는 오차 1e-9 안에서 비교한다.
- `Style(SecondRate)`, `Style(FirstRate)`, `Style(Peak)`, `Style(Transcendent)` 는 요구사항 표의 12칸 값을 그대로 돌려준다 (네 경지 × 12칸 전부 검사).
- `Style(ThirdRate)` 는 네 `Has…` 가 `false` 이고 숫자 8칸이 모두 0 이다.
- `Style((RealmLevel)99)` 는 `ArgumentOutOfRangeException` 을 던진다.
- `IsPlaying` 은 1.79 → `true`, 1.8 → `false`, −1 → `true`, `double.PositiveInfinity` → `false` 다.
- `ShowsNewAura` 는 0.49 → `false`, 0.5 → `true` 다.
- `DimAlpha(SecondRate, t)` 는 −1 → 0, 0.25 → 0.125, 1.0 → 0.25, 1.55 → 0.125, 1.8 → 0, `double.PositiveInfinity` → 0 이다.
- `DimAlpha(Transcendent, 1.0)` 은 0.6 이다.
- `DimMask` 는 −1 → 0, 1.5 → 0, 2.75 → 0.5, 4 → 1, 10 → 1 이다.
- `DimMask(double.NaN)` 은 `ArgumentOutOfRangeException` 을 던진다.
- `LightIntensity(Transcendent, t)` 는 0 → 0, 0.25 → 0.6, 0.5 → 3.0, 0.9 → 1.5, 1.3 → 0 이다.
- `LightIntensity(SecondRate, 0.5)` 는 1.0 이다.
- `BloomIntensity(Transcendent, 0.9)` 는 0.75 다.
- `FlashAlpha(Transcendent, t)` 는 0.4 → 0, 0.5 → 0.8, 0.575 → 0.4, 0.65 → 0 이다.
- `FlashAlpha(Peak, 0.5)` 는 0 이다.
- `ShakePixels` 는 (`FirstRate`, 0.6) → 1, (`Transcendent`, 0.79) → 3, (`Transcendent`, 0.8) → 0, (`Transcendent`, 0.4) → 0, (`SecondRate`, 0.6) → 0 이다.
- `IsPlaying`, `ShowsNewAura`, `DimAlpha`, `LightIntensity`, `BloomIntensity`, `FlashAlpha`, `ShakePixels` 에 시간 `double.NaN` 을 주면 각각 `ArgumentOutOfRangeException` 을 던진다.
- `DimAlpha`, `LightIntensity`, `BloomIntensity`, `FlashAlpha`, `ShakePixels` 에 `(RealmLevel)99` 와 시간 0 을 주면 각각 `ArgumentOutOfRangeException` 을 던진다.
- `new EffectPoint(1, 2, 3, 4)` 의 `X`, `Y`, `Scale`, `Alpha` 는 1, 2, 3, 4 이고, `new EffectPoint(-1, double.NaN, -2, 5)` 도 예외 없이 만들어진다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `BurstFade`, `RingRadius`, `PillarWidthAt`, `ShockwaveRadius`, `ShockwaveAlpha`, `BurstPetal` 은 만들지 않는다 (059 가 한다).
- `RealmAura.cs` 와 `RealmAuraTests.cs` 는 고치지 않는다 (060 이 한다).
- `RealmProgress`, `RealmRules`, `RealmText`, `AuraColor` 와 그 테스트를 고치지 않는다.
- `Assets/Scripts/Game/` 은 고치지 않는다 (061·062 가 한다).
- `UnityEngine` 을 쓰지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 고치지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/` 를 고치지 않는다.

## 보고서

### 한 일
- `PlumSword.Core.Realm` 에 `EffectPoint`(읽기 전용 구조체, 값 검사 없음), `BreakthroughStyle`(12칸 읽기 전용 구조체, 값 검사 없음), `BreakthroughEffect`(정적 클래스)를 만들었다.
- `BreakthroughEffect` 에 상수 8개, `Style`(경지별 표, 정의되지 않은 값은 `ArgumentOutOfRangeException`), `IsPlaying`, `ShowsNewAura`, `DimAlpha`, `DimMask`, `LightIntensity`, `BloomIntensity`, `FlashAlpha`, `ShakePixels` 를 사양 공식 그대로 구현했다.
- 시간 인자는 공통 내부 함수 하나에서 처리한다: `NaN` → 예외, 음수·음의 무한대 → 0, 양의 무한대 → 그대로(끝난 뒤). 경지를 받는 함수는 먼저 `Style(realm)` 을 불러 경지를 검사한다.
- 구간 경계는 `t < 0.65`, `t < 0.8` 꼴로 비교한다. 내부 상수 `BurstSeconds + FlashSeconds`, `BurstSeconds + ShakeSeconds` 가 각각 정확히 0.65, 0.8 이 되는 것을 확인했다 (`0.5 + 0.15 == 0.65`, `0.5 + 0.3 == 0.8`, `1.8 − 1.3 == 0.5`, `1.3 − 0.5 == 0.8` 모두 참).
- 완료 조건마다 에딧 모드 테스트를 `BreakthroughEffectTests.cs` 에 썼다.
- 사양 문서의 "연결 지점"과 구현 모양이 같아서 사양은 고치지 않았다.

### 바뀐 파일
- `Assets/Scripts/Core/Realm/EffectPoint.cs` (+ `.meta`) — 새 파일
- `Assets/Scripts/Core/Realm/BreakthroughStyle.cs` (+ `.meta`) — 새 파일
- `Assets/Scripts/Core/Realm/BreakthroughEffect.cs` (+ `.meta`) — 새 파일
- `Assets/Tests/EditMode/Realm/BreakthroughEffectTests.cs` (+ `.meta`) — 새 파일
- `tasks/doing/058_breakthrough_style_timing.md` — 보고서 덧붙임

### 가정
- 모든 수치(상수, 경지별 표)는 작업 문서·사양의 임시값을 그대로 썼다.
- 공식 안의 숫자(0.4, 0.5, 0.8, 1.5, 2.5)는 `BreakthroughEffect` 안의 비공개 상수(`GatherLightRatio`, `BloomRatio`, `FlashPeakAlpha`, `DimMaskInner`, `DimMaskWidth`)로 두었다. 공개 연결 지점은 바뀌지 않는다.
- `DimMask` 는 양의 무한대일 때 바로 1 을 돌려준다 (공식으로 계산해도 1 이지만 명시적으로 처리). 음의 무한대는 공식대로 0 이다.
- `RealmAura.Color` 의 현재 코드 값이 사양의 새 매화 색과 다르지만, 060 의 범위라 건드리지 않았다.

### 사람이 확인할 것
- 없음 (이 작업은 순수 C# 로직이라 모든 완료 조건을 에딧 모드 테스트로 판정했다. 화면 연출은 061·062 에서 확인한다).

### 테스트 결과
- `python tools/loop/run_unity_tests.py --project .` 실행.
- EditMode: 729개 중 729개 통과, 0개 실패 (새 `BreakthroughEffectTests` 45개 케이스 포함).
- PlayMode: 80개 중 80개 통과, 0개 실패.


## 루프 기록 (2026-11-20 18:02)

- 작업 에이전트: claude(시도 1)
- 리뷰: claude (같은 에이전트) 통과
- 기능 브랜치: task/058_breakthrough_style_timing
- 루프 브랜치: loop/20261120-175740
