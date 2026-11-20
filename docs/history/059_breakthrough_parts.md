# 059_breakthrough_parts — 돌파 연출 부품과 꽃잎 시간표 (Core)

## 목표
058 이 만든 `PlumSword.Core.Realm.BreakthroughEffect` 에 고리·기둥·충격파·돌파 꽃잎 함수(`BurstFade`, `RingRadius`, `PillarWidthAt`, `ShockwaveRadius`, `ShockwaveAlpha`, `BurstPetal`)를 더한다. 순수 C#(Core) 작업이다.

## 묶음
realm_fx

## 선행 작업
- 058_breakthrough_style_timing

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/realm.md ("경계 상황"의 `BreakthroughEffect`·`index` 줄, "수치"의 "시간표와 공식", "연결 지점"의 `BreakthroughEffect`·`EffectPoint`, "완료 조건 초안" 42~45)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-20 — 매화 오오라와 단계별 돌파 연출` 항목만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. 이 작업은 그중 아래 범위만 맡는다.
- `Assets/Scripts/Core/Realm/BreakthroughEffect.cs` (058 이 만듦) 에 아래 공개 메서드를 더한다. 058 이 만든 상수·`Style`·다른 함수의 동작은 바꾸지 않는다.
- 시간 인자 규칙은 058 과 같다: `NaN` 이면 `ArgumentOutOfRangeException`, 음수·음의 무한대는 0 으로, 양의 무한대는 그대로 두어 "끝난 뒤"로 계산한다. 경지를 받는 함수는 정의되지 않은 경지면 `ArgumentOutOfRangeException`. 058 에 이 검사를 하는 비공개 도우미가 있으면 그것을 쓴다.
- `t` 는 정리된 시간이다. `R = Style(realm).RingRadius`, `N = Style(realm).BurstPetals`.
- `double BurstFade(double seconds)`: `0.5 ≤ t < 1.3` → `(1.3 − t) / 0.8`, 그 밖 0.
- `double RingRadius(RealmLevel realm, double seconds)`: `HasRing` 이고 `0.5 ≤ t < 1.3` → `R × (0.3 + 0.7 × (t − 0.5) / 0.8)`, 그 밖 0.
- `double PillarWidthAt(double seconds)`: `BurstFade(t) > 0` → `1.2 × (0.3 + 0.7 × BurstFade(t))`, 그 밖 0. (`PillarWidth` 상수 1.2)
- `double ShockwaveRadius(RealmLevel realm, double seconds)` / `double ShockwaveAlpha(RealmLevel realm, double seconds)`: `HasShockwave` 이고 `0.5 ≤ t < 0.9` 이면 `p = (t − 0.5) / 0.4`, 반지름 `2 × R × p`, 알파 `1 − p`. 그 밖 둘 다 0.
- `EffectPoint BurstPetal(RealmLevel realm, int index, double seconds)`:
  - `index` 가 0 미만이거나 `N` 이상이면 `ArgumentOutOfRangeException` (`N` 이 0 인 삼류는 어떤 `index` 든 예외). 검사 순서는 경지 → 시간(`NaN`) → `index`.
  - `a = 2π × index / N`.
  - 모으기 `t < 0.5`: 반지름 `3 × (1 − t / 0.5)`, 위치 `(cos a × 반지름, sin a × 반지름)`, Scale 1, 알파 `t / 0.5`.
  - 터짐 `0.5 ≤ t < 1.8`: `u = t − 0.5`, 속도 `s = 3 + 3 × ((index × 7) mod N) / N` (정수 나머지 뒤 `double` 나눗셈), 위치 `(cos a × s × u, sin a × s × u − 1.5 × u²)`, Scale 1, 알파 `1 − u / 1.3`.
  - `t ≥ 1.8`: `(0, 0, 0, 0)`.
- 구간 경계(0.9, 1.3 등)는 완료 조건의 경계값이 그대로 맞는 비교식으로 쓴다 (예: `t < 0.9`).
- Core 이므로 `UnityEngine` 을 쓰지 않는다.
- 구현하다 모양이 사양과 달라지면 사양 문서의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/Realm/BreakthroughEffectTests.cs` (058 이 만듦) 에 아래 조건마다 테스트가 하나 이상 더해지고 모두 통과한다. 058 의 테스트는 지우거나 고치지 않는다. 실수는 오차 1e-9 안에서 비교하고, `EffectPoint` 는 (X, Y, Scale, Alpha) 순서로 적는다.
- `BurstFade` 는 0.4 → 0, 0.5 → 1, 0.9 → 0.5, 1.3 → 0 이다.
- `RingRadius(FirstRate, t)` 는 0.4 → 0, 0.5 → 0.45, 0.9 → 0.975, 1.3 → 0 이다.
- `RingRadius(SecondRate, 0.9)` 는 0 이다.
- `PillarWidthAt` 은 0.4 → 0, 0.5 → 1.2, 0.9 → 0.78, 1.3 → 0 이다.
- `ShockwaveRadius(Transcendent, t)` 는 0.5 → 0, 0.7 → 2.5, 0.9 → 0 이다.
- `ShockwaveAlpha(Transcendent, t)` 는 0.5 → 1, 0.7 → 0.5, 0.9 → 0 이다.
- `ShockwaveRadius(Peak, 0.7)` 과 `ShockwaveAlpha(Peak, 0.7)` 은 0 이다.
- `BurstPetal(SecondRate, 0, t)` 는 0 → (3, 0, 1, 0), 0.25 → (1.5, 0, 1, 0.5), 1.0 → (1.5, −0.375, 1, 0.6153846153846154), 1.8 → (0, 0, 0, 0) 이다.
- `BurstPetal(SecondRate, 3, 1.0)` 은 (0, 2.25, 1, 0.6153846153846154) 다.
- `BurstPetal(SecondRate, 12, 0)`, `BurstPetal(SecondRate, -1, 0)`, `BurstPetal(ThirdRate, 0, 0)` 은 각각 `ArgumentOutOfRangeException` 을 던진다.
- `BurstFade`, `RingRadius`, `PillarWidthAt`, `ShockwaveRadius`, `ShockwaveAlpha`, `BurstPetal` 에 시간 `double.NaN` 을 주면 각각 `ArgumentOutOfRangeException` 을 던진다.
- `RingRadius`, `ShockwaveRadius`, `ShockwaveAlpha`, `BurstPetal` 에 `(RealmLevel)99` 와 시간 0 을 주면 각각 `ArgumentOutOfRangeException` 을 던진다.
- `BurstFade(-1)` 은 0 이고 `BurstFade(double.PositiveInfinity)` 는 0 이다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 058 이 만든 `EffectPoint`, `BreakthroughStyle`, `BreakthroughEffect` 의 기존 멤버 동작과 그 테스트를 바꾸지 않는다.
- `RealmAura.cs` 와 `RealmAuraTests.cs` 는 고치지 않는다 (060 이 한다).
- `RealmProgress`, `RealmRules`, `RealmText`, `AuraColor` 를 고치지 않는다.
- `Assets/Scripts/Game/` 은 고치지 않는다 (061·062 가 한다).
- `UnityEngine` 을 쓰지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 고치지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/` 를 고치지 않는다.

## 보고서

### 한 일
- `BreakthroughEffect` 에 `BurstFade`, `RingRadius`, `PillarWidthAt`, `ShockwaveRadius`, `ShockwaveAlpha`, `BurstPetal` 을 더했다. 시간 정리는 058 의 비공개 도우미 `Normalize` 를, 경지 검사는 `Style(realm)` 을 그대로 썼다. `BurstPetal` 의 검사 순서는 경지(`Style`) → 시간(`Normalize`) → `index` 다.
- 구간 경계는 `t < 0.5` / `t >= 1.3` / `t >= 0.9`(= `BurstSeconds + ShockwaveSeconds`) 식으로 써서 완료 조건의 경계값이 그대로 맞게 했다.
- 완료 조건마다 에딧 모드 테스트를 `BreakthroughEffectTests.cs` 에 더했다. 058 의 테스트는 건드리지 않았다.
- 사양 "연결 지점" 절에 이미 같은 모양의 선언이 있어 사양 문서는 고치지 않았다.

### 바뀐 파일
- `Assets/Scripts/Core/Realm/BreakthroughEffect.cs` (공개 메서드 6개, 비공개 상수 5개·도우미 1개 추가)
- `Assets/Tests/EditMode/Realm/BreakthroughEffectTests.cs` (테스트 추가)

### 가정
- 공식 속 숫자(고리·기둥 시작 비율 0.3, 꽃잎 기본 속도 3, 꽃잎 중력 1.5)는 비공개 상수로 두었다. 사양 연결 지점에 없는 이름이라 공개하지 않았다.
- 꽃잎 알파의 1.3 은 `TotalSeconds − BurstSeconds`, 모으기 반지름의 3 은 `GatherRadius` 로 썼다 (값은 같다).

### 사람이 확인할 것
없음

### 테스트 결과
- `tools/loop/run_unity_tests.py --project .`: EditMode 761/761 통과, PlayMode 80/80 통과.


## 루프 기록 (2026-11-20 18:06)

- 작업 에이전트: claude(시도 1)
- 리뷰: claude (같은 에이전트) 통과
- 기능 브랜치: task/059_breakthrough_parts
- 루프 브랜치: loop/20261120-175740
