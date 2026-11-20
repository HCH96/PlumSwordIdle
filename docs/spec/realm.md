# 경지 돌파 사양

- 기능 이름: realm (네임스페이스 `PlumSword.Core.Realm`)
- 작성일: 2026-11-18
- 고친 날: 2026-11-20

## 목표

금화를 모아 돌파 버튼을 눌러 제자의 경지를 `삼류 → 이류 → 일류 → 절정 → 초절정` 다섯 단계로 올리는 기능이다. 이 기능은 경지 단계, 돌파 비용과 구매(금화 소비), 경지별 공격력·최대 체력 보너스 비율(정수 %), 돌파 버튼, 전투 화면 왼쪽 위의 현재 경지 표시, 제자 주변의 경지별 오오라(기본 빛·기운·꽃잎)와 돌파 연출(1.8초. 경지가 오를수록 층이 하나씩 늘어난다)까지 맡는다.

보너스를 실제 공격력·최대 체력에 적용하는 계산(내림 포함)은 능력치 강화 기능이, 돌파 때 현재 체력을 늘리는 것은 전투 기능이 한다. 이 기능은 그쪽이 읽을 단계·비율·알림만 제공한다. 저장 데이터 형식에 경지를 넣는 것은 저장 기능이 한다.

출처: `docs/design/milestone_3.md` "목표와 현재 방향", "경지 돌파", "저장과 반복 진행", "3단계 화면", "3단계 마일스톤 달성 기준".

## 동작

- 경지는 다섯 단계다: `RealmLevel.ThirdRate`(삼류) → `SecondRate`(이류) → `FirstRate`(일류) → `Peak`(절정) → `Transcendent`(초절정). 처음에는 삼류다. 경지는 내려가지 않는다.
- 돌파 조건은 금화뿐이다. 스테이지 번호, 강화 단계, 전투 상태(패배, 구간 소개 중 등)는 보지 않는다.
- **돌파** `TryBreakthrough()`: 아래 순서로 처리한다.
  1. 잠겨 있으면(`Locked`) `false`.
  2. 초절정(`IsMax`)이면 `false`.
  3. `wallet.TrySpend(NextCost)`가 `false`면(금화 부족) `false`.
  4. `Current`를 한 단계 올린다.
  5. `BrokeThrough`를 새 경지를 담아 한 번 보내고 `true`를 돌려준다.
- `false`를 돌려준 경우에는 금화도 경지도 바뀌지 않고 알림도 없다.
- 알림 순서: 돌파 한 번에 `Wallet.BalanceChanged`(3번에서) → 경지 증가 → `BrokeThrough` 순서다. `BalanceChanged`를 받는 시점에는 `Current`가 아직 이전 경지다. `BrokeThrough`를 받는 쪽에서 `Current`, `BonusPercent`, `NextCost`, `IsMax`를 읽으면 새 값이다.
- **확인** `CanBreakthrough()`: 잠겨 있지 않고, 초절정이 아니고, `wallet.CanSpend(NextCost)`가 `true`일 때만 `true`. 아무것도 바꾸지 않는다. 돌파 버튼을 누를 수 있는지는 이 값 그대로다.
- **보너스 비율** `BonusPercent`: 지금 경지의 누적 보너스(정수 %). `RealmRules.BonusPercent(Current)`와 같다. 부를 때마다 지금 경지로 계산한다. 공격력과 최대 체력에 같은 비율이 쓰인다.
- **잠금** `SetLocked(locked)`: 잠긴 동안에는 `TryBreakthrough`와 `CanBreakthrough`가 모두 `false`다. 값이 실제로 바뀐 때에만 `LockedChanged`를 새 값을 담아 한 번 보낸다. 처음에는 잠겨 있지 않다. 잠금은 `GameRoot`가 `StatUpgrades.LockedChanged`를 구독해 같은 값으로 `SetLocked`를 불러 맞춘다. 그래서 스테이지 전환 페이드 동안 강화와 함께 잠긴다. 스테이지 기능은 이 기능을 모른다.
- 스테이지 성공·실패, 천마 처치 뒤 1스테이지로 돌아가는 것, 게임 재시작은 경지에 아무 영향을 주지 않는다.
- 같은 시점에 여러 번 부르면 부른 순서대로 한 번씩, 그때의 금화와 경지로 따로 판정한다.
- 화면은 버튼을 누르면 `TryBreakthrough`를 부르고, `Wallet.BalanceChanged`, `BrokeThrough`, `LockedChanged`를 받을 때마다 버튼과 경지 표시를 다시 그린다. 오오라는 `BrokeThrough`를 받으면 그때부터 1.8초 돌파 연출(`BreakthroughEffect`)을 하고, 0.5초(터지는 때)에 새 경지의 상시 오오라로 바꾼다. 연출은 그림만 덮는다: 전투·페이드·잠금·버튼은 평소대로 움직이고 게임을 멈추지 않는다(히트스톱 없음). 연출 중에 또 돌파하면 지금 연출을 끊고 새 경지로 0초부터 다시 한다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| 금화가 비용보다 적을 때 `TryBreakthrough` | `false` | 금화·경지 그대로, 알림 없음 |
| 금화가 비용과 같을 때 `TryBreakthrough` | `true` | 금화 0, 경지 +1, `BalanceChanged` 1번, `BrokeThrough` 1번 |
| 초절정에서 `TryBreakthrough` | `false` (금화가 충분해도) | 금화·경지 그대로, 알림 없음 |
| 초절정에서 `NextCost` | 0 (예외 없음) | 변화 없음. 화면은 `MAX`를 보여 준다 |
| 초절정에서 `CanBreakthrough` | `false` | 변화 없음 |
| 잠긴 동안 `TryBreakthrough` | `false` | 금화·경지 그대로, 알림 없음 |
| 잠긴 동안 `CanBreakthrough` | `false` | 변화 없음 |
| `SetLocked`를 지금과 같은 값으로 부름 | 아무 일도 없다 | `LockedChanged` 없음 |
| `TryBreakthrough`를 연달아 부름 | 부를 때마다 그때의 금화·경지로 따로 판정 | 예: 금화 1000에서 세 번 → `true`(200), `true`(500), `false`(1000 필요), 금화 300, 경지 일류 |
| 생성자의 `wallet`이 `null` | `ArgumentNullException` | |
| 생성자의 `startLevel`이 음수 | 0(삼류)으로 만든다 (예외 없음) | 알림 없음, 금화 그대로 |
| 생성자의 `startLevel`이 4보다 큼 | 4(초절정)로 만든다 (예외 없음) | 알림 없음, 금화 그대로 |
| 정의되지 않은 `RealmLevel` 값 (예: `(RealmLevel)99`) | `ArgumentOutOfRangeException` (`RealmRules.BonusPercent`, `RealmRules.BreakthroughCost`, `RealmText.Name`, `RealmText.Label`, `RealmText.AssetId`, `RealmText.ButtonLabel`, `RealmAura.HasAura`, `RealmAura.Color`, `RealmAura.PulseAlpha`) | 변화 없음 |
| `RealmAura.PulseAlpha`의 `seconds`가 `NaN`이거나 무한대 | `ArgumentOutOfRangeException` | |
| `RealmAura.BurstScale`의 `secondsSinceBreakthrough`가 `NaN` | `ArgumentOutOfRangeException` | |
| `RealmAura.BurstScale`에 음수 | 1.5 (예외 없음) | |
| `RealmAura.BurstScale`에 양의 무한대 | 1.0 (예외 없음) | |
| `RealmText.Cost(음수, false)` | 그대로 표기 (예외 없음) | 예: `G -5` |
| `BreakthroughEffect`의 시간 함수와 `RealmAura.Wisp`·`FloatingPetal`에 `NaN` | `ArgumentOutOfRangeException` | |
| `BreakthroughEffect` 시간 함수에 음수 / 음의 무한대 / 양의 무한대 | 음수·음의 무한대는 0초로, 양의 무한대는 끝난 뒤(1.8초 이상)로 계산한다 (예외 없음) | |
| `RealmAura.Wisp`·`FloatingPetal`의 `seconds`가 무한대 | `ArgumentOutOfRangeException` | `PulseAlpha`와 같다 |
| `BreakthroughEffect.DimMask`에 `NaN` / 음수 | `ArgumentOutOfRangeException` / 0 | |
| `BurstPetal`·`Wisp`·`FloatingPetal`의 `index`가 0 미만이거나 개수 이상 | `ArgumentOutOfRangeException` | 개수가 0인 경지(예: 이류의 상시 꽃잎, 삼류 전부)는 어떤 `index`든 예외 |
| `BreakthroughEffect.Style(ThirdRate)` | 네 `Has…`가 `false`, 수치는 모두 0 (예외 없음) | 돌파로 삼류가 되는 일은 없다 |
| 경지를 받는 새 함수에 정의되지 않은 `RealmLevel` | `ArgumentOutOfRangeException` (`Style`, `DimAlpha`, `LightIntensity`, `BloomIntensity`, `FlashAlpha`, `ShakePixels`, `RingRadius`, `ShockwaveRadius`, `ShockwaveAlpha`, `BurstPetal`, `RealmAura.BurstColor`, `RealmAura.Wisp`, `RealmAura.FloatingPetal`) | 변화 없음 |

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 경지의 타입 | `enum RealmLevel` (값 0~4) | 처음 값 `ThirdRate`(0). 단계 수 `RealmRules.LevelCount = 5` |
| 비용의 타입 | `long` | 정수. 반올림 없음 |
| 보너스 비율의 타입 | `int` (%) | 정수. 배율 `double`은 쓰지 않는다 |
| 돌파 비용 `RealmRules.BreakthroughCost(from)` | 삼류 → 이류 200, 이류 → 일류 500, 일류 → 절정 1,000, 절정 → 초절정 2,000, 초절정 0 (모두 임시) | `from`은 돌파하기 전 경지. 삼류에서 초절정까지 합계 3,700 |
| 누적 보너스 `RealmRules.BonusPercent(realm)` | 삼류 0, 이류 10, 일류 20, 절정 30, 초절정 40 (모두 임시) | 공격력·최대 체력 공통 |
| 보너스 적용 공식 (참고, 계산은 능력치 강화 기능) | `(기본값 + 일반 강화 증가량) × (100 + BonusPercent) / 100`을 `long` 정수 나눗셈으로 (= 내림) | 예: 이류에서 체력 120 → 132, 공격력 12 → 13. 100 → 110 |
| 이름 표기 `RealmText.Name` | `THIRD RATE`, `SECOND RATE`, `FIRST RATE`, `PEAK`, `TRANSCENDENT` | 한글 글꼴이 없어 영문 |
| 경지 표시 `RealmText.Label` | `"REALM: " + Name` | 예: `REALM: SECOND RATE` |
| 버튼 첫 줄 `RealmText.ButtonTitle` | `BREAKTHROUGH` | 상수 |
| 비용 표기 `RealmText.Cost` | 최대면 `MAX`, 아니면 `"G " + GoldText.FormatNumber(cost)` | 500 → `G 500`, 1000 → `G 1,000` |
| 버튼 글자 `RealmText.ButtonLabel` | `ButtonTitle + "\n" + Name(current) + "\n" + Cost(cost, isMax)` | `BREAKTHROUGH\nSECOND RATE\nG 500` |
| 에셋 ID `RealmText.AssetId` | `third_rate`, `second_rate`, `first_rate`, `peak`, `transcendent` | 리소스 경로에 쓴다 ("화면") |
| 오오라가 있는 경지 `RealmAura.HasAura` | 이류·일류·절정·초절정. 삼류는 없음 | |
| 오오라 색 `RealmAura.Color` (R, G, B, A) | 이류 (1.00, 0.72, 0.82, 0.35), 일류 (1.00, 0.55, 0.75, 0.40), 절정 (0.95, 0.35, 0.65, 0.45), 초절정 (1.00, 0.40, 0.70, 0.50), 삼류 (0, 0, 0, 0) (모두 임시) | `double` 0~1. 매화 분홍 계열, 경지가 오를수록 진하다. 기본 빛·기운·꽃잎·빛(Light2D)의 색 |
| 돌파 부품 색 `RealmAura.BurstColor` (R, G, B, A) | 이류·일류·절정은 `Color`의 RGB에 A 1.0, 초절정은 흰 중심광 (1.00, 0.95, 0.97, 1.00), 삼류 (0, 0, 0, 0) (임시) | 고리·기둥·충격파의 색 |
| 오오라 깜빡임 `RealmAura.PulseAlpha(realm, seconds)` | `Color(realm).A × (0.75 + 0.25 × Math.Sin(2π × seconds / 1.6))` (임시) | 주기 1.6초. 삼류는 0. 예: 이류 0초 0.2625, 0.4초 0.35, 1.2초 0.175 |
| 돌파 연출 크기 `RealmAura.BurstScale(t)` | `t ≤ 0` → 1.5, `0 < t < 0.4` → `1.5 − 0.5 × t / 0.4`, `t ≥ 0.4` → 1.0 (임시) | 예: 0 → 1.5, 0.2 → 1.25, 0.4 → 1.0 |
| 실수 비교 | 오차 1e-9 | 테스트 기준 |

**단계별 돌파 연출 구성 `BreakthroughEffect.Style(realm)`** (모두 임시). 경지가 오를수록 층이 하나씩 늘어난다.

| 항목 | 이류 | 일류 | 절정 | 초절정 |
| --- | --- | --- | --- | --- |
| `HasRing` 바닥 고리 | `false` | `true` | `true` | `true` |
| `HasPillar` 빛기둥 | `false` | `false` | `true` | `true` |
| `HasShockwave` 충격파 | `false` | `false` | `false` | `true` |
| `HasFlash` 흰 번쩍임 | `false` | `false` | `false` | `true` |
| `BurstPetals` 돌파 꽃잎 수 (`int`) | 12 | 18 | 24 | 36 |
| `DimAlpha` 배경 어둡게 최대 (`double`) | 0.25 | 0.35 | 0.45 | 0.60 |
| `ShakePixels` 흔들림 픽셀 (`int`) | 0 | 1 | 2 | 3 |
| `LightPeak` 빛 최대 세기 (`double`) | 1.0 | 1.5 | 2.0 | 3.0 |
| `RingRadius` 고리 반지름, 유닛 (`double`) | 0 | 1.5 | 2.0 | 2.5 |
| `PillarHeight` 기둥 높이, 유닛 (`double`) | 0 | 0 | 5.0 | 7.0 |
| `AuraWisps` 상시 기운 줄 수 (`int`) | 2 | 2 | 3 | 4 |
| `AuraPetals` 상시 꽃잎 수 (`int`) | 0 | 2 | 3 | 5 |

삼류는 네 `Has…`가 `false`이고 수치가 모두 0이다.

**시간표와 공식** (모두 임시). `t`는 돌파 뒤 흐른 초(`double`)다. `BreakthroughEffect`의 함수는 음수·음의 무한대를 0으로, 양의 무한대를 끝난 뒤로 계산한다. 아래 `D`, `L`, `R`, `N`은 그 경지 `Style`의 `DimAlpha`, `LightPeak`, `RingRadius`, `BurstPetals`다.

| 항목 | 공식 | 예 |
| --- | --- | --- |
| 구간 상수 | 모으기 0~0.5, 터지는 때 `BurstSeconds` = 0.5, 여운 끝 `AfterglowEndSeconds` = 1.3, 전체 `TotalSeconds` = 1.8. `FlashSeconds` = 0.15, `ShakeSeconds` = 0.3, `ShockwaveSeconds` = 0.4, `GatherRadius` = 3.0, `PillarWidth` = 1.2 | |
| `IsPlaying(t)` | `t < 1.8` | 1.79 → `true`, 1.8 → `false`, −1 → `true`, +∞ → `false` |
| `ShowsNewAura(t)` | `t ≥ 0.5` | 0.49 → `false`, 0.5 → `true` |
| `DimAlpha(realm, t)` | `t < 0.5`: `D × t / 0.5` · `0.5 ≤ t < 1.3`: `D` · `1.3 ≤ t < 1.8`: `D × (1.8 − t) / 0.5` · `t ≥ 1.8`: 0 | 이류: 0.25 → 0.125, 1.0 → 0.25, 1.55 → 0.125, 1.8 → 0. 초절정 1.0 → 0.6 |
| `DimMask(d)` 어둡게 가림막 모양 (`d` = 오오라 가운데에서 잰 유닛 거리) | `clamp((d − 1.5) / 2.5, 0, 1)` | −1 → 0, 1.5 → 0, 2.75 → 0.5, 4 → 1, 10 → 1 |
| `LightIntensity(realm, t)` | `t < 0.5`: `0.4 × L × t / 0.5` · `0.5 ≤ t < 1.3`: `L × (1.3 − t) / 0.8` · `t ≥ 1.3`: 0 | 초절정: 0 → 0, 0.25 → 0.6, 0.5 → 3.0, 0.9 → 1.5, 1.3 → 0. 이류 0.5 → 1.0 |
| `BloomIntensity(realm, t)` | `LightIntensity(realm, t) × 0.5` | 초절정 0.9 → 0.75 |
| `FlashAlpha(realm, t)` | `HasFlash`이고 `0.5 ≤ t < 0.65`: `0.8 × (0.65 − t) / 0.15` · 그 밖 0 | 초절정: 0.4 → 0, 0.5 → 0.8, 0.575 → 0.4, 0.65 → 0. 절정 0.5 → 0 |
| `ShakePixels(realm, t)` (`int`) | `0.5 ≤ t < 0.8`이면 `Style.ShakePixels`, 아니면 0 | 일류 0.6 → 1, 초절정 0.79 → 3, 초절정 0.8 → 0, 초절정 0.4 → 0, 이류 0.6 → 0 |
| `BurstFade(t)` | `0.5 ≤ t < 1.3`: `(1.3 − t) / 0.8` · 그 밖 0 | 0.4 → 0, 0.5 → 1, 0.9 → 0.5, 1.3 → 0 |
| `RingRadius(realm, t)` | `HasRing`이고 `0.5 ≤ t < 1.3`: `R × (0.3 + 0.7 × (t − 0.5) / 0.8)` · 그 밖 0. 고리 알파는 `BurstFade(t)` | 일류: 0.4 → 0, 0.5 → 0.45, 0.9 → 0.975, 1.3 → 0. 이류 0.9 → 0 |
| `PillarWidthAt(t)` | `BurstFade(t) > 0`: `1.2 × (0.3 + 0.7 × BurstFade(t))` · 그 밖 0. 기둥 알파는 `BurstFade(t)` | 0.4 → 0, 0.5 → 1.2, 0.9 → 0.78, 1.3 → 0 |
| `ShockwaveRadius(realm, t)` / `ShockwaveAlpha(realm, t)` | `HasShockwave`이고 `0.5 ≤ t < 0.9`: `p = (t − 0.5) / 0.4`, 반지름 `2 × R × p`, 알파 `1 − p` · 그 밖 둘 다 0 | 초절정: 0.5 → 0 / 1, 0.7 → 2.5 / 0.5, 0.9 → 0 / 0. 절정 0.7 → 0 / 0 |
| `BurstPetal(realm, i, t)` → `EffectPoint(X, Y, Scale, Alpha)` | `a = 2π × i / N`. **모으기** `t < 0.5`: 반지름 `3 × (1 − t / 0.5)`, 위치 `(cos a, sin a) × 반지름`, Scale 1, 알파 `t / 0.5`. **터짐** `0.5 ≤ t < 1.8`: `u = t − 0.5`, 속도 `s = 3 + 3 × ((i × 7) mod N) / N`, 위치 `(cos a × s × u, sin a × s × u − 1.5 × u²)`, Scale 1, 알파 `1 − u / 1.3`. `t ≥ 1.8`: (0, 0, 0, 0) | 이류 i = 0: 0 → (3, 0, 1, 0), 0.25 → (1.5, 0, 1, 0.5), 1.0 → (1.5, −0.375, 1, 0.6153846153846154), 1.8 → (0, 0, 0, 0). 이류 i = 3, 1.0 → (0, 2.25, 1, 0.6153846153846154) |
| `RealmAura.Wisp(realm, i, t)` 상시 기운 | `n = AuraWisps`. `X = (i − (n − 1) / 2) × 0.45 + 0.12 × sin(2π × (t / 1.2 + i / n))`, `Y = 0`, `Scale`(세로 배율) `= 1 + 0.15 × sin(2π × (t / 0.9 + i / n))`, `Alpha = min(1, 2 × PulseAlpha(realm, t + 0.4 × i))` | 이류 i = 0, t = 0 → (−0.225, 0, 1, 0.525). i = 1, t = 0 → (0.225, 0, 1, 0.7). i = 0, t = 0.3 → (−0.105, 0, 1.1299038105676658, 0.6866789…) |
| `RealmAura.FloatingPetal(realm, i, t)` 상시 꽃잎 | `n = AuraPetals`, `p = t / 3 + i / n`, `f = p − ⌊p⌋`. `X = 0.8 × sin(3π × p + i)`, `Y = −1.05 + 2.4 × f`, `Scale = 1`, `Alpha = sin(π × f)` | 일류 i = 0: t = 0 → (0, −1.05, 1, 0), t = 1.5 → (−0.8, 0.15, 1, 1). 일류 i = 1, t = 0 → (−0.4322418…, 0.15, 1, 1) |

`EffectPoint`의 X, Y는 오오라 가운데(발 위 1.25 유닛)를 기준으로 한 유닛 단위 위치다.

능력치 강화 단계·비용, 몬스터 능력치, 현재 체력은 이 기능의 수치가 아니다 ("범위 밖").

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.Realm
{
    public enum RealmLevel
    {
        ThirdRate = 0,     // 삼류
        SecondRate = 1,    // 이류
        FirstRate = 2,     // 일류
        Peak = 3,          // 절정
        Transcendent = 4,  // 초절정
    }

    public static class RealmRules
    {
        public const int LevelCount = 5;

        // 0, 10, 20, 30, 40 (%). 정의되지 않은 값: ArgumentOutOfRangeException.
        public static int BonusPercent(RealmLevel realm);

        // from 에서 다음 경지로 가는 비용. 200, 500, 1000, 2000, Transcendent 는 0.
        // 정의되지 않은 값: ArgumentOutOfRangeException.
        public static long BreakthroughCost(RealmLevel from);
    }

    public sealed class RealmProgress
    {
        // wallet: 돌파 비용을 쓸 지갑. null 이면 ArgumentNullException.
        // startLevel: 저장된 값으로 되살릴 때 넣는다 ((int)RealmLevel). 음수면 0, 4 보다 크면 4 (예외 없음).
        // 생성자는 금화를 쓰지 않고 알림을 보내지 않는다. 잠기지 않은 상태로 시작한다.
        public RealmProgress(PlumSword.Core.Currency.Wallet wallet, int startLevel = 0);

        public RealmLevel Current { get; }
        public bool IsMax { get; }            // Current == Transcendent
        public int BonusPercent { get; }      // RealmRules.BonusPercent(Current)
        public long NextCost { get; }         // RealmRules.BreakthroughCost(Current). 초절정이면 0

        // 지금 돌파할 수 있으면 true: 잠기지 않았고, 초절정이 아니고, 금화가 NextCost 이상. 아무것도 바꾸지 않는다.
        public bool CanBreakthrough();

        // 돌파한다. 성공하면 금화를 쓰고 경지를 한 단계 올린 뒤 BrokeThrough 를 보내고 true.
        // 잠김, 초절정, 금화 부족이면 아무것도 바꾸지 않고 false (알림 없음).
        public bool TryBreakthrough();

        // 돌파가 성공해 경지가 오른 뒤에 한 번, 새 경지를 담아 보낸다. 실패하면 보내지 않는다.
        public event System.Action<RealmLevel> BrokeThrough;

        public bool Locked { get; }           // 처음 값 false

        // 잠그거나 푼다. 값이 실제로 바뀐 때에만 LockedChanged 를 보낸다.
        public void SetLocked(bool locked);

        public event System.Action<bool> LockedChanged;
    }

    public static class RealmText
    {
        public const string ButtonTitle = "BREAKTHROUGH";

        // "THIRD RATE", "SECOND RATE", "FIRST RATE", "PEAK", "TRANSCENDENT". 정의되지 않은 값: ArgumentOutOfRangeException.
        public static string Name(RealmLevel realm);

        // "REALM: " + Name(realm).
        public static string Label(RealmLevel realm);

        // "third_rate", "second_rate", "first_rate", "peak", "transcendent". 리소스 경로용.
        public static string AssetId(RealmLevel realm);

        // isMax 가 true 면 "MAX" (cost 는 보지 않는다). 아니면 "G " + GoldText.FormatNumber(cost). 음수도 그대로.
        public static string Cost(long cost, bool isMax);

        // ButtonTitle + "\n" + Name(current) + "\n" + Cost(cost, isMax).
        public static string ButtonLabel(RealmLevel current, long cost, bool isMax);
    }

    // 오오라 대체 표시의 색. 0~1 의 double. 생성자는 값을 검사하지 않는다.
    public readonly struct AuraColor
    {
        public AuraColor(double r, double g, double b, double a);
        public double R { get; }
        public double G { get; }
        public double B { get; }
        public double A { get; }
    }

    public static class RealmAura
    {
        public const double PulsePeriodSeconds = 1.6;
        public const double BurstSeconds = 0.4;
        public const double BurstPeakScale = 1.5;

        // ThirdRate 만 false. 정의되지 않은 값: ArgumentOutOfRangeException.
        public static bool HasAura(RealmLevel realm);

        // "수치"의 색. ThirdRate 는 (0, 0, 0, 0).
        public static AuraColor Color(RealmLevel realm);

        // Color(realm).A * (0.75 + 0.25 * Math.Sin(2 * Math.PI * seconds / 1.6)). ThirdRate 는 0.
        // seconds 가 NaN·무한대면 ArgumentOutOfRangeException. 음수는 그대로 계산한다.
        public static double PulseAlpha(RealmLevel realm, double seconds);

        // 돌파 뒤 흐른 시간에 따른 크기 배율. t <= 0 -> 1.5, 0 < t < 0.4 -> 1.5 - 0.5 * t / 0.4, t >= 0.4 -> 1.0.
        // NaN 이면 ArgumentOutOfRangeException. 양의 무한대는 1.0.
        public static double BurstScale(double secondsSinceBreakthrough);

        // 아래 셋은 2026-11-20 에 더한다. "수치"의 공식 그대로.
        // 고리·기둥·충격파의 색. ThirdRate 는 (0, 0, 0, 0).
        public static AuraColor BurstColor(RealmLevel realm);

        // 상시 기운 한 줄. index 는 0 ~ Style(realm).AuraWisps - 1. 벗어나면 ArgumentOutOfRangeException.
        // seconds 가 NaN·무한대면 ArgumentOutOfRangeException.
        public static EffectPoint Wisp(RealmLevel realm, int index, double seconds);

        // 상시 꽃잎 한 장. index 는 0 ~ Style(realm).AuraPetals - 1. 벗어나면 ArgumentOutOfRangeException.
        // seconds 가 NaN·무한대면 ArgumentOutOfRangeException. 음수는 그대로 계산한다.
        public static EffectPoint FloatingPetal(RealmLevel realm, int index, double seconds);
    }

    // 위치(오오라 가운데 기준, 유닛)·크기 배율·알파 한 점. 생성자는 값을 검사하지 않는다.
    public readonly struct EffectPoint
    {
        public EffectPoint(double x, double y, double scale, double alpha);
        public double X { get; }
        public double Y { get; }
        public double Scale { get; }
        public double Alpha { get; }
    }

    // 경지별 돌파 연출 구성 ("수치"의 표). 생성자는 값을 검사하지 않는다.
    public readonly struct BreakthroughStyle
    {
        public BreakthroughStyle(bool hasRing, bool hasPillar, bool hasShockwave, bool hasFlash,
            int burstPetals, double dimAlpha, int shakePixels, double lightPeak,
            double ringRadius, double pillarHeight, int auraWisps, int auraPetals);
        public bool HasRing { get; }
        public bool HasPillar { get; }
        public bool HasShockwave { get; }
        public bool HasFlash { get; }
        public int BurstPetals { get; }
        public double DimAlpha { get; }
        public int ShakePixels { get; }
        public double LightPeak { get; }
        public double RingRadius { get; }
        public double PillarHeight { get; }
        public int AuraWisps { get; }
        public int AuraPetals { get; }
    }

    // 돌파 연출의 시간표. 모든 공식은 "수치" 그대로다.
    // 시간(seconds) 인자: NaN 이면 ArgumentOutOfRangeException. 음수·음의 무한대는 0 으로, 양의 무한대는 끝난 뒤로 계산한다.
    // 경지 인자: 정의되지 않은 값이면 ArgumentOutOfRangeException.
    public static class BreakthroughEffect
    {
        public const double BurstSeconds = 0.5;
        public const double AfterglowEndSeconds = 1.3;
        public const double TotalSeconds = 1.8;
        public const double FlashSeconds = 0.15;
        public const double ShakeSeconds = 0.3;
        public const double ShockwaveSeconds = 0.4;
        public const double GatherRadius = 3.0;
        public const double PillarWidth = 1.2;

        public static BreakthroughStyle Style(RealmLevel realm);       // ThirdRate: 모두 false·0
        public static bool IsPlaying(double seconds);                  // seconds < 1.8
        public static bool ShowsNewAura(double seconds);               // seconds >= 0.5
        public static double DimAlpha(RealmLevel realm, double seconds);
        public static double DimMask(double distance);                 // NaN: 예외, 음수: 0
        public static double LightIntensity(RealmLevel realm, double seconds);
        public static double BloomIntensity(RealmLevel realm, double seconds);
        public static double FlashAlpha(RealmLevel realm, double seconds);
        public static int ShakePixels(RealmLevel realm, double seconds);
        public static double BurstFade(double seconds);
        public static double RingRadius(RealmLevel realm, double seconds);
        public static double PillarWidthAt(double seconds);
        public static double ShockwaveRadius(RealmLevel realm, double seconds);
        public static double ShockwaveAlpha(RealmLevel realm, double seconds);

        // index 는 0 ~ Style(realm).BurstPetals - 1. 벗어나면 ArgumentOutOfRangeException.
        public static EffectPoint BurstPetal(RealmLevel realm, int index, double seconds);
    }
}
```

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래 하나를 더한다
    {
        // Awake 에서 Wallet 을 만든 직후, StatUpgrades 보다 먼저 new RealmProgress(Wallet) 으로 하나 만든다.
        // StatUpgrades 를 만든 뒤 StatUpgrades.LockedChanged 를 구독해 Realm.SetLocked(같은 값) 을 부른다.
        public PlumSword.Core.Realm.RealmProgress Realm { get; }   // private set
    }
}

namespace PlumSword.Game.Realm
{
    public sealed class RealmView : MonoBehaviour
    {
        // canvas 아래에 경지 표시와 돌파 버튼을 만들고 지금 상태로 바로 표시한다.
        // wallet.BalanceChanged, realm.BrokeThrough, realm.LockedChanged 를 구독하고 OnDestroy 에서 끊는다.
        // 인자 중 null 이 있으면 ArgumentNullException. 두 번 부르면 InvalidOperationException.
        public void Initialize(
            PlumSword.Core.Realm.RealmProgress realm,
            PlumSword.Core.Currency.Wallet wallet,
            UnityEngine.Canvas canvas);
    }

    public sealed class RealmAuraView : MonoBehaviour
    {
        // 제자 발밑을 따라다니는 오오라와 돌파 연출을 만든다 ("화면"). realm.BrokeThrough 를 구독하고 OnDestroy 에서 끊는다.
        // [DefaultExecutionOrder(100)] 을 붙인다 (CombatView.Update 가 카메라를 옮긴 뒤에 흔들림을 더하려고).
        // 인자 중 null 이 있으면 ArgumentNullException. 두 번 부르면 InvalidOperationException.
        public void Initialize(
            PlumSword.Core.Realm.RealmProgress realm,
            PlumSword.Core.Combat.Battle battle);
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot.Awake`가 `Wallet`을 만든 직후 `RealmProgress`를 하나 만들어 `GameRoot.Realm`으로 들고 있는다. 게임에 `RealmProgress`는 하나뿐이다. 저장 기능이 경지를 저장하게 되기 전에는 `new RealmProgress(Wallet)`(삼류)로 만든다. 그 뒤에는 `new RealmProgress(Wallet, 저장된 realmLevel)`로 만든다.
- 능력치 강화 기능이 쓰는 방법 (능력치 강화 사양을 고칠 때 반영): `RealmProgress`를 넘겨받아 `Attack`, `MaxHealth`를 계산할 때마다 `BonusPercent`를 읽어 `(기본값 + 증가량) × (100 + BonusPercent) / 100`(`long` 정수 나눗셈)을 돌려준다.
- 전투 기능이 쓰는 방법 (전투 사양을 고칠 때 반영): `BrokeThrough`를 받으면 체력 강화 때와 같이 `늘어난 양 = StatUpgrades.MaxHealth − 기억해 둔 직전 최대 체력`을 구해 기억을 바꾸고, 패배 상태가 아니면 현재 체력을 그만큼 늘린다. `BrokeThrough`를 받는 시점에 `Current`는 이미 새 경지라 `StatUpgrades.MaxHealth`는 새 값이다.
- 저장 기능이 쓰는 방법 (저장 사양을 고칠 때 반영): 저장할 때 `(int)Current`를 읽고, 불러올 때 생성자의 `startLevel`에 넣는다. 돌파는 언제나 금화를 써서 `BalanceChanged`가 가므로, 지금의 자동 저장 규칙(그 프레임 끝에 저장)만으로 돌파 직후의 경지가 저장된다. `BrokeThrough`를 따로 구독하지 않아도 된다.

### 필요한 것

- `PlumSword.Core.Currency.Wallet` (`docs/spec/currency.md`): `CanSpend(long)`, `TrySpend(long)`, `BalanceChanged`.
- `PlumSword.Core.Currency.GoldText.FormatNumber(long)` (`docs/spec/currency.md`).
- `PlumSword.Core.StatUpgrade.StatUpgrades.LockedChanged` (`docs/spec/stat_upgrade.md`): `GameRoot`가 잠금을 맞출 때 쓴다.
- `PlumSword.Core.Combat.Battle.DisciplePosition` (`docs/spec/combat.md`): 오오라 위치.
- `PlumSword.Game.GameRoot.Wallet`, `UiCanvas`, `StatUpgrades`, `Battle`.
- `PlumSword.Game.StatUpgrade.StatUpgradeButtonEffect` (이미 있는 화면 코드, `docs/spec/stat_upgrade.md` "화면"의 강화 연출): `Initialize(Button, RectTransform, Image, Image, TextMeshProUGUI)`, `SetEnabledAppearance(bool)`, `PlaySuccess()`를 그대로 쓴다. 이 클래스는 고치지 않는다.
- `UnityEngine.Camera.main`: 흔들림과 번쩍임 위치. 카메라 위치는 `CombatView.Update`가 매 프레임 다시 정한다 (`CombatView`는 고치지 않는다).
- URP 2D의 `UnityEngine.Rendering.Universal.Light2D`, `UnityEngine.Rendering.Volume`·`VolumeProfile`, `UnityEngine.Rendering.Universal.Bloom`, `UniversalAdditionalCameraData.renderPostProcessing`. `PlumSword.Game` 어셈블리는 이미 URP 2D 런타임을 참조하므로 어셈블리 정의는 바꾸지 않는다. 설정 에셋(렌더러, 볼륨 프로필)도 바꾸지 않고 모두 코드에서 만든다.

## 화면

- **조립**: `GameRoot.Awake`에서 `[StatUpgradeView]`를 만든 다음에 `[Game]`의 자식 `[RealmView]`를 만들어 `RealmView`를 붙이고 `Initialize(Realm, Wallet, UiCanvas)`를 부른다. `[CombatView]`를 만든 다음에 `[Game]`의 자식 `[RealmAuraView]`를 만들어 `RealmAuraView`를 붙이고 `Initialize(Realm, Battle)`를 부른다.
- 모든 그림은 `Resources.Load`로 읽는다. 없으면 아래 대체 표시를 쓰고 게임은 그대로 진행된다 (오류 로그를 남기지 않는다. 그림 오오라 시트의 크기가 틀린 경우만 `Debug.LogWarning`).

### 현재 경지 표시 (화면 왼쪽 위)

- 캔버스 왼쪽 위에 앵커와 피벗 (0, 1), 위치 (8, −8).
- 그림 `Art/UI/Realm/name_<AssetId(Current)>`(`Sprite`)가 있으면 `Image`로 보여 준다. 높이 24, 가로는 그림 비율대로(최대 160), `preserveAspect = true`, `raycastTarget = false`.
- 없으면 `TextMeshProUGUI` 한 줄: `RealmText.Label(Current)` (예: `REALM: SECOND RATE`), 크기 200×16, 글자 크기 14, 왼쪽 정렬, 색 RGB (1.0, 0.95, 0.80), TMP 기본 글꼴, `raycastTarget = false` (모두 임시).
- 그림은 경지마다 따로 확인한다. 이류 그림만 있으면 이류일 때만 그림이고 나머지는 글자다.

### 돌파 버튼 (화면 아래쪽 가운데, HP 버튼 위)

- 캔버스 왼쪽 아래에 앵커와 피벗 (0, 0), 위치 (220, 62), 크기 200×48 (임시).
- **그림 모드**: 바탕 `Art/UI/Realm/button`이 있으면 그것을, 없으면 강화 버튼 바탕 `Art/UI/StatUpgrade/button`을 쓴다. 바탕과 금화 아이콘 `Art/UI/StatUpgrade/coin`이 모두 있을 때만 그림 모드다. 배치는 강화 버튼과 같다:
  - 아이콘 `Art/UI/Realm/icon` (9, 8) 32×32. 없으면 아이콘 자리는 비운다 (`Image` 끔).
  - 첫 줄 `RealmText.ButtonTitle` (48, 23) 82×18, 글자 크기 15. 자동 크기 9~15.
  - 둘째 줄 `RealmText.Name(Current)` (48, 8) 82×15, 글자 크기 11. 자동 크기 8~11.
  - 금화 아이콘 (145, 15) 17×17과 비용 숫자 `GoldText.FormatNumber(NextCost)` (164, 11) 31×25, 글자 크기 12, 자동 크기 8~12. 초절정이면 금화 아이콘을 숨기고 `MAX`를 쓴다.
  - 호버·누름·성공 연출은 `StatUpgradeButtonEffect`를 붙여 강화 버튼과 똑같이 한다 (`SetEnabledAppearance(CanBreakthrough())`, 성공 때 `PlaySuccess()`). 둘째 줄 글자가 성공 때 금빛으로 변하는 글자다.
- **대체 모드** (그림이 없을 때): `Image`(단색) + `Button`, `transition = None`. 자식 `TextMeshProUGUI` 하나에 `RealmText.ButtonLabel(Current, NextCost, IsMax)` (예: `BREAKTHROUGH` / `SECOND RATE` / `G 500`), 버튼을 꽉 채우고 가운데 정렬, 글자 크기 14, `raycastTarget = false`. 누를 수 있을 때 RGB (0.55, 0.25, 0.35)에 흰 글자, 누를 수 없을 때 RGB (0.25, 0.25, 0.25)에 RGB (0.6, 0.6, 0.6) 글자 (모두 임시). 대체 모드에는 호버·성공 연출이 없다.
- **누를 수 있을 때**: `CanBreakthrough()`가 `true`일 때만 `Button.interactable = true`. 금화 부족·초절정·잠김을 모양으로 구분하지 않는다. 초절정은 `MAX` 글자로 알 수 있다.
- **누르면**: `TryBreakthrough()`를 부른다. 성공하면 위의 성공 연출을 한다. 한 번 누르면 한 단계다.
- **바뀌는 때**: `Initialize`에서 한 번, 그 뒤로 `wallet.BalanceChanged`, `realm.BrokeThrough`, `realm.LockedChanged`가 올 때마다 버튼과 경지 표시를 갱신한다.

### 오오라와 돌파 연출 (제자 주변, 월드 공간)

- **위치**: 매 프레임 `LateUpdate`에서 발 위치 `(battle.DisciplePosition − 2.25, −1.5)`를 따라간다. 이 값은 전투 사양 "화면"의 제자 표시 위치와 같다. 전투 쪽 값이 바뀌면 함께 고친다. 오오라의 가운데는 발 위 1.25 유닛이다. `EffectPoint`의 X, Y는 이 가운데에 더한다.
- 아래 "보여 줄 경지"는 돌파 연출 중 `ShowsNewAura(t)`가 `false`면 돌파 전 경지, 그 밖에는 `Current`다. 저장을 불러와 시작하면 연출 없이 `Current`의 상시 오오라만 보인다.
- 그림은 모두 `Resources.Load<Texture2D>`로 읽는다. 시트는 가로 한 줄이고 아래에 적은 칸 크기로 자른다. 가로가 칸 너비로 나누어떨어지지 않으면 `Debug.LogWarning`을 남기고 대체 그림을 쓴다. 그림은 색을 입히지 않고(흰색, 그림 그대로) 알파만 아래대로 곱한다. 대체 그림은 실행 중에 만든 텍스처로 PPU 32, Point 필터(어둡게 가림막만 Bilinear)이고, 거리는 각 픽셀의 중심에서 잰다. 대체 그림의 색은 아래에 적은 색의 RGB다.

**상시 오오라** (보여 줄 경지가 삼류면 아무것도 보이지 않는다)

- **기본 빛** (`sortingOrder` 9990): 그림 `Art/Effects/aura_<AssetId(보여 줄 경지)>`가 있으면 정사각형 칸(칸 한 변 = 시트 높이)으로 잘라 10 fps(임시)로 반복 재생한다. 피벗 칸 가운데, 화면에서 1.5배 (제자와 같다). 없으면 64×64 원형 그러데이션(알파 = `(1 − r)²`, r은 가운데에서 잰 거리 ÷ 32, 1 넘으면 0)을 가로 2.4 × 세로 3.0 유닛으로 늘려 그린다 (임시). 대체 그림의 색은 `RealmAura.Color`, 알파는 매 프레임 `RealmAura.PulseAlpha(보여 줄 경지, Time.time)`. 그림 애니메이션은 보여 줄 경지가 바뀔 때 첫 프레임부터 재생한다. 돌파 연출 중 `t ≥ 0.5`이면 크기에 `RealmAura.BurstScale(t − 0.5)`를 곱한다.
- **기운** (9991): `Style(보여 줄 경지).AuraWisps`줄. 줄마다 `RealmAura.Wisp(보여 줄 경지, i, Time.time)`의 위치·세로 배율(Scale)·알파. 그림 `Art/Effects/aura_wisp`(칸 32×64, 10 fps 반복)가 있으면 피벗 칸 가운데, 높이 3.0 유닛 × Scale, 비율 유지. 없으면 8×32 세로 물방울(알파 = `(1 − |dx| / 4)² × sin(π × (y + 0.5) / 32)`, `dx = x + 0.5 − 4`, 1 넘거나 0 미만이면 잘라냄)을 가로 0.5 × 세로 2.0 × Scale 유닛으로 그린다. 색 `RealmAura.Color`.
- **꽃잎** (10002): `Style(보여 줄 경지).AuraPetals`장. 장마다 `RealmAura.FloatingPetal(보여 줄 경지, i, Time.time)`. 그림 `Art/Effects/petal`(칸 8×8, 칸 하나가 꽃잎 종류 하나, `i % 칸 수`번째 칸, 재생하지 않음)이 있으면 그것을, 없으면 3×3 십자 모양(가운데와 상하좌우 다섯 픽셀 불투명, 모서리 투명)을 화면에서 1.5배로 그린다. 색 `RealmAura.Color`.

**돌파 연출** (`BrokeThrough` 때 시작. `t = Time.time − 시작 시각`. 연출 중에 또 돌파하면 시작 시각을 새로 잡는다. 연출의 경지는 새 경지 `r`이다. `IsPlaying(t)`가 `false`가 되면 아래 부품을 모두 숨기고 블룸 세기를 0, 카메라 후처리를 시작 전 값으로 돌려놓는다)

- **어둡게** (9980): 128×64 텍스처, 픽셀마다 알파 = `BreakthroughEffect.DimMask`(그 픽셀 중심이 나타내는 위치와 텍스처 가운데의 유닛 거리. 1 픽셀 = 0.5 유닛). 64×32 유닛으로, 오오라 가운데에 맞춘다. 색은 검정, `SpriteRenderer.color`의 알파 = `DimAlpha(r, t)`. 그래서 바닥·배경·적은 어두워지고 제자(10000)·투사체(20000)·피해 숫자(25000)·체력 바(30000)는 그대로다.
- **빛**: 오오라 가운데에 점 `Light2D`(안 반지름 0.5, 바깥 반지름 5.0 유닛, 색 `RealmAura.Color(r)`의 RGB, 세기 `LightIntensity(r, t)`, 모든 정렬 레이어에 적용). `Initialize` 때 장면에 전역(Global) `Light2D`가 없으면 세기 1, 흰색, 모든 정렬 레이어에 적용되는 것을 하나 만든다 (점광원 하나만 있으면 나머지 화면이 어두워지기 때문). 연출이 아닐 때 점광원 세기는 0이다.
- **블룸**: `Initialize` 때 `[RealmAuraView]` 아래에 전역 `Volume`(우선순위 100, 가중치 1)과 실행 중에 만든 `VolumeProfile`을 두고, 그 안의 `Bloom`을 문턱 0.8, 흩어짐 0.7, 세기 0으로 둔다 (임시). 연출 중에는 세기 = `BloomIntensity(r, t)`. 연출이 시작될 때 `Camera.main`의 `UniversalAdditionalCameraData.renderPostProcessing`을 기억했다가 `true`로 바꾸고, 끝나면 기억한 값으로 돌려놓는다.
- **고리** (9992, `HasRing`): 그림 `Art/Effects/breakthrough_ring`(칸 160×48)이 있으면 그것을, 없으면 64×16 타원 테두리(`u = (x + 0.5 − 32) / 32`, `v = (y + 0.5 − 8) / 8`, `d = √(u² + v²)`, 알파 = `max(0, 1 − |d − 0.85| / 0.15)`)를 쓴다. 가운데를 발 위치에 맞추고, 대체 그림은 가로 `2 × RingRadius(r, t)` × 세로 `0.6 × RingRadius(r, t)` 유닛, 그림은 가로 `2 × RingRadius(r, t)`에 맞추고 비율 유지. 알파 `BurstFade(t)`, 대체 그림의 색 `RealmAura.BurstColor(r)`. 반지름이 0이면 숨긴다.
- **기둥** (9993, `HasPillar`): 그림 `Art/Effects/breakthrough_pillar`(칸 64×192)이 있으면 그것을, 없으면 16×64 세로 기둥(알파 = `(1 − |dx| / 8)² × min(1, (63.5 − y) / 16)`, `dx = x + 0.5 − 8`)을 쓴다. 아래 끝을 발 위치에 맞추고, 대체 그림은 가로 `PillarWidthAt(t)` × 세로 `Style(r).PillarHeight` 유닛, 그림은 세로를 `PillarHeight`에 맞추고 비율 유지. 알파 `BurstFade(t)`, 대체 그림의 색 `RealmAura.BurstColor(r)`. 폭이 0이면 숨긴다.
- **충격파** (9994, `HasShockwave`): 그림 `Art/Effects/breakthrough_shockwave`(칸 192×64)가 있으면 그것을, 없으면 고리의 대체 텍스처를 쓴다. 가운데를 발 위치에 맞추고, 대체 그림은 가로 `2 × ShockwaveRadius(r, t)` × 세로 `0.6 × ShockwaveRadius(r, t)`, 그림은 가로를 `2 × ShockwaveRadius`에 맞추고 비율 유지. 알파 `ShockwaveAlpha(r, t)`, 대체 그림의 색 `RealmAura.BurstColor(r)`. 반지름이 0이면 숨긴다.
- **부품 그림 재생**: 고리·기둥·충격파 그림은 진행도에 맞춰 한 번 재생한다. 프레임 = `⌊진행도 × 칸 수⌋`(최대 칸 수 − 1). 진행도는 고리·기둥 `(t − 0.5) / 0.8`, 충격파 `(t − 0.5) / 0.4`.
- **돌파 꽃잎** (10003): `Style(r).BurstPetals`장. 장마다 `BreakthroughEffect.BurstPetal(r, i, t)`의 위치·Scale·알파. 그림은 상시 꽃잎과 같다. 색 `RealmAura.Color(r)`.
- **번쩍임** (29990, `HasFlash`): 1×1 흰 텍스처를 30×12 유닛으로 늘려 `Camera.main` 가운데(x, y)에 둔다. 알파 `FlashAlpha(r, t)`. 체력 바(30000)보다 아래다.
- **흔들림**: `ShakePixels(r, t)`가 0보다 크면 `LateUpdate`에서 `Camera.main` 위치의 x, y에 각각 {−1, 0, +1} 중 무작위(`UnityEngine.Random`) × 픽셀 수 ÷ 32 유닛을 더한다. 원래 위치는 `CombatView.Update`가 매 프레임 다시 정하므로 따로 되돌리지 않는다.
- 연출은 그림만 덮는다. 전투·스테이지 진행·페이드·버튼·저장 타이머에는 영향을 주지 않는다 (`Time.timeScale`을 바꾸지 않는다).

- 스테이지 전환 페이드, 패배, 구간 소개 중에도 오오라와 연출은 그대로 보인다 (페이드는 UI라 그 위에 덮인다).
- 표시 글자, 누를 수 있는지, 오오라·연출의 색·시간·위치·세기 판단은 모두 Core(`RealmText`, `RealmProgress.CanBreakthrough`, `RealmAura`, `BreakthroughEffect`)에 있고 에딧 모드 테스트로 판정한다. `RealmView`, `RealmAuraView`, `GameRoot` 조립과 모양은 사람이 실행해 확인한다.

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| 현재 경지 `realmLevel` (`(int)RealmProgress.Current`) | `int` | 0 (삼류) | 0~4 |

- 실제 저장 데이터(`SaveData`)·파일 형식(`SaveCodec`)에 넣는 것은 저장 기능이 한다. 그전까지 경지는 게임을 끄면 삼류로 돌아간다.
- 되살리기: `new RealmProgress(wallet, 저장된 realmLevel)`. 음수는 0, 4 초과는 4로 시작한다. 잠금(`Locked`)은 저장하지 않으며 항상 풀린 상태로 시작한다. 비용과 보너스 비율은 경지에서 계산되므로 저장하지 않는다.
- 2단계 저장 데이터와의 호환은 없다. 3단계는 저장 데이터를 지우고 새로 시작한다.
- 오프라인 보상: 꺼져 있는 동안 경지는 바뀌지 않는다. 오프라인 보상 금액에도 영향을 주지 않는다.
- 돌파 연출의 진행 상황은 저장하지 않는다. 연출 중에 끄고 다시 켜면 연출 없이 그 경지의 상시 오오라만 보인다.

## 범위 밖

- 보너스를 실제 공격력·최대 체력에 적용하는 계산: 능력치 강화 기능이 한다. 그 사양을 고치기 전에는 돌파해도 능력치가 바뀌지 않는다.
- 돌파 때 현재 체력을 늘리는 처리: 전투 기능이 한다. 그 사양을 고치기 전에는 현재 체력이 바뀌지 않는다.
- 경지를 저장 데이터·파일에 넣고 불러오는 것, 저장 판 번호: 저장 기능이 한다.
- 공격 속도 강화 제거, 내공 강화, 이십사수매화검법: 3단계의 다른 기능이 한다. 스킬은 경지 보너스가 적용된 `StatUpgrades.Attack`을 쓴다.
- 실제 오오라·버튼·경지 이름 그림 제작, 기운·꽃잎·고리·기둥·충격파 그림 제작: 사용자가 PixelLab 등으로 만든다. 그림을 넣으면 이 사양의 경로대로 바뀐다.
- `ParticleSystem`, 가산 혼합 재질·셰이더 에셋: 쓰지 않는다. 꽃잎은 Core 계산과 `SpriteRenderer`로 그린다.
- 히트스톱(게임 멈춤), 연출 중 버튼 잠금: 하지 않는다.
- 평소(연출이 아닐 때)의 블룸·Light2D 조명: 하지 않는다.
- 한글 표기(`경지: 이류`, `최대`, `500금화`)와 한글 글꼴: 글꼴이 준비되면 `RealmText`의 글자만 바꾼다.
- 돌파 소리, 큰 안내 패널, 경지 되돌리기·환불, 여러 단계 한꺼번에 돌파, 스테이지 조건: 하지 않는다.
- 밸런스 조정: 4단계에서 한다.

## 완료 조건 초안

이미 구현되고 바뀌지 않은 완료 조건(1~25, 27~30, 32, 33)은 기록 파일로 옮겼다. 번호는 그대로 두고, 새 조건은 34번부터 붙인다.

실수는 오차 1e-9 안에서 비교한다. `EffectPoint`는 (X, Y, Scale, Alpha) 순서로 적는다.

오오라 규칙 (`RealmAuraTests`)

26. `RealmAura.Color`는 `SecondRate` → (1.00, 0.72, 0.82, 0.35), `FirstRate` → (1.00, 0.55, 0.75, 0.40), `Peak` → (0.95, 0.35, 0.65, 0.45), `Transcendent` → (1.00, 0.40, 0.70, 0.50), `ThirdRate` → (0, 0, 0, 0)을 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다.
46. `RealmAura.BurstColor`는 `SecondRate` → (1.00, 0.72, 0.82, 1.00), `FirstRate` → (1.00, 0.55, 0.75, 1.00), `Peak` → (0.95, 0.35, 0.65, 1.00), `Transcendent` → (1.00, 0.95, 0.97, 1.00), `ThirdRate` → (0, 0, 0, 0)을 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다.
47. `RealmAura.Wisp(SecondRate, 0, 0)` → (−0.225, 0, 1, 0.525), `Wisp(SecondRate, 1, 0)` → (0.225, 0, 1, 0.7), `Wisp(SecondRate, 0, 0.3)` → (−0.105, 0, 1.1299038105676658, 0.68667891…)다 (마지막 알파는 `0.7 × (0.75 + 0.25 × sin(2π × 0.3 / 1.6))`). `Wisp(SecondRate, 2, 0)`, `Wisp(SecondRate, -1, 0)`, `Wisp(ThirdRate, 0, 0)`, `Wisp(SecondRate, 0, double.NaN)`, `Wisp(SecondRate, 0, double.PositiveInfinity)`, `Wisp((RealmLevel)99, 0, 0)`은 `ArgumentOutOfRangeException`을 던진다.
48. `RealmAura.FloatingPetal(FirstRate, 0, 0)` → (0, −1.05, 1, 0), `FloatingPetal(FirstRate, 0, 1.5)` → (−0.8, 0.15, 1, 1), `FloatingPetal(FirstRate, 1, 0)` → (`0.8 × sin(1.5π + 1)` = −0.43224184…, 0.15, 1, 1)이다. `FloatingPetal(SecondRate, 0, 0)`, `FloatingPetal(FirstRate, 2, 0)`, `FloatingPetal(FirstRate, 0, double.NaN)`, `FloatingPetal((RealmLevel)99, 0, 0)`은 `ArgumentOutOfRangeException`을 던진다.

돌파 연출 규칙 (`BreakthroughEffectTests`)

34. `BreakthroughEffect.Style`은 `SecondRate`, `FirstRate`, `Peak`, `Transcendent`에 "수치"의 단계별 표 값을 그대로 돌려준다 (네 경지 × 12칸 전부). 예: `Style(Peak)`는 `HasRing` `true`, `HasPillar` `true`, `HasShockwave` `false`, `HasFlash` `false`, `BurstPetals` 24, `DimAlpha` 0.45, `ShakePixels` 2, `LightPeak` 2.0, `RingRadius` 2.0, `PillarHeight` 5.0, `AuraWisps` 3, `AuraPetals` 3.
35. `Style(ThirdRate)`는 네 `Has…`가 `false`이고 숫자 칸이 모두 0이다. `Style((RealmLevel)99)`는 `ArgumentOutOfRangeException`을 던진다.
36. `IsPlaying`은 1.79 → `true`, 1.8 → `false`, −1 → `true`, `double.PositiveInfinity` → `false`이고, `ShowsNewAura`는 0.49 → `false`, 0.5 → `true`다.
37. `DimAlpha(SecondRate, t)`는 −1 → 0, 0.25 → 0.125, 1.0 → 0.25, 1.55 → 0.125, 1.8 → 0, `double.PositiveInfinity` → 0이고, `DimAlpha(Transcendent, 1.0)`은 0.6이다.
38. `DimMask`는 −1 → 0, 1.5 → 0, 2.75 → 0.5, 4 → 1, 10 → 1이고, `double.NaN`은 `ArgumentOutOfRangeException`을 던진다.
39. `LightIntensity(Transcendent, t)`는 0 → 0, 0.25 → 0.6, 0.5 → 3.0, 0.9 → 1.5, 1.3 → 0이고, `LightIntensity(SecondRate, 0.5)`는 1.0, `BloomIntensity(Transcendent, 0.9)`는 0.75다.
40. `FlashAlpha(Transcendent, t)`는 0.4 → 0, 0.5 → 0.8, 0.575 → 0.4, 0.65 → 0이고, `FlashAlpha(Peak, 0.5)`는 0이다.
41. `ShakePixels`는 (`FirstRate`, 0.6) → 1, (`Transcendent`, 0.79) → 3, (`Transcendent`, 0.8) → 0, (`Transcendent`, 0.4) → 0, (`SecondRate`, 0.6) → 0이다.
42. `BurstFade`는 0.4 → 0, 0.5 → 1, 0.9 → 0.5, 1.3 → 0이다. `RingRadius(FirstRate, t)`는 0.4 → 0, 0.5 → 0.45, 0.9 → 0.975, 1.3 → 0이고 `RingRadius(SecondRate, 0.9)`는 0이다. `PillarWidthAt`은 0.4 → 0, 0.5 → 1.2, 0.9 → 0.78, 1.3 → 0이다.
43. `ShockwaveRadius(Transcendent, t)` / `ShockwaveAlpha(Transcendent, t)`는 0.5 → 0 / 1, 0.7 → 2.5 / 0.5, 0.9 → 0 / 0이고, `ShockwaveRadius(Peak, 0.7)`과 `ShockwaveAlpha(Peak, 0.7)`은 0이다.
44. `BurstPetal(SecondRate, 0, t)`는 0 → (3, 0, 1, 0), 0.25 → (1.5, 0, 1, 0.5), 1.0 → (1.5, −0.375, 1, 0.6153846153846154), 1.8 → (0, 0, 0, 0)이고, `BurstPetal(SecondRate, 3, 1.0)`은 (0, 2.25, 1, 0.6153846153846154)다. `BurstPetal(SecondRate, 12, 0)`, `BurstPetal(SecondRate, -1, 0)`, `BurstPetal(ThirdRate, 0, 0)`은 `ArgumentOutOfRangeException`을 던진다.
45. `IsPlaying`, `ShowsNewAura`, `DimAlpha`, `LightIntensity`, `BloomIntensity`, `FlashAlpha`, `ShakePixels`, `BurstFade`, `RingRadius`, `PillarWidthAt`, `ShockwaveRadius`, `ShockwaveAlpha`, `BurstPetal`에 시간 `double.NaN`을 주면 `ArgumentOutOfRangeException`을 던진다. 경지를 받는 것(`DimAlpha`, `LightIntensity`, `BloomIntensity`, `FlashAlpha`, `ShakePixels`, `RingRadius`, `ShockwaveRadius`, `ShockwaveAlpha`, `BurstPetal`)에 `(RealmLevel)99`와 시간 0을 주면 `ArgumentOutOfRangeException`을 던진다.

화면 (자동 테스트 없음. 사람이 실행해 확인)

31. 금화 200 이상에서 돌파 버튼을 누르면 금화가 200 줄고, 왼쪽 위가 `REALM: SECOND RATE`, 버튼이 `SECOND RATE` / `500`으로 바뀐다. 0.5초 동안 배경과 적이 조금 어두워지며 분홍 꽃잎 12장이 제자에게 모인 뒤 바깥으로 흩어지고, 연분홍 오오라와 기운 2줄이 나타난다. 고리·기둥·번쩍임은 없다. 전투는 멈추지 않는다. 그림 모드면 버튼에 강화 버튼과 같은 성공 연출이 나온다.
49. 일류 → 절정 돌파 때 바닥 고리가 퍼지고 빛기둥이 솟으며, 화면이 2 픽셀 흔들리고 제자 주변의 바닥·배경이 분홍으로 밝아지며 기둥이 번져 보인다. 1.8초 뒤 블룸과 어둡게가 사라지고 화면이 원래 밝기로 돌아온다.
50. 절정 → 초절정 돌파 때 흰 번쩍임과 충격파가 더해지고 꽃잎 36장이 터지며 3 픽셀 흔들린다. 그 뒤 상시 오오라는 기운 4줄과 떠오르는 꽃잎 5장이다.
51. 연출 중에 다시 돌파하면 새 경지의 연출이 처음부터 나온다. 저장된 절정으로 이어하면 연출 없이 절정 오오라만 보인다. 돌파하지 않은 평소 화면의 밝기와 색은 이번 변경 전과 같다 (전역 Light2D 확인).

## 결정 기록

결정 기록은 [records/realm.md](records/realm.md)의 "결정 기록"에 있다. 구현에 필요한 규칙은 위 절들에 모두 있다.

## 변경 기록

### 2026-11-20 — 매화 오오라와 단계별 돌파 연출 (기준 커밋: b3d919e)

| 절 | 바뀌기 전 | 바뀐 뒤 |
| --- | --- | --- |
| 목표 | 제자 주변의 경지별 오오라까지 맡는다 | 오오라(기본 빛·기운·꽃잎)와 1.8초 돌파 연출(경지가 오를수록 층이 하나씩 늘어난다)까지 맡는다 |
| 동작 | `BrokeThrough` 때 새 경지의 모양으로 바꾸고 돌파 연출을 한다 | 1.8초 연출, 0.5초에 새 상시 오오라로 바꾼다. 게임을 멈추지 않고, 연출 중 또 돌파하면 처음부터 |
| 경계 상황 | (해당 줄 없음) | `BreakthroughEffect`·`Wisp`·`FloatingPetal`의 NaN·무한대·음수·index·삼류·정의되지 않은 경지 처리 |
| 수치 | 오오라 대체 색 이류 하늘·일류 파랑·절정 보라·초절정 분홍 | 매화 분홍 계열 `Color`, `BurstColor`, 단계별 연출 구성 표, 시간표와 공식 |
| 연결 지점 | `RealmAura` (색·깜빡임·크기) | `EffectPoint`, `BreakthroughStyle`, `BreakthroughEffect`, `RealmAura.BurstColor`·`Wisp`·`FloatingPetal` 추가. `RealmAuraView`에 `[DefaultExecutionOrder(100)]`. 필요한 것에 `Camera.main`, `Light2D`, `Volume`·`Bloom` 추가 |
| 화면 | 원형 빛 하나와 0.4초 크기 연출 | 기본 빛 + 기운 + 꽃잎, 어둡게 가림막·점광원(+전역 광원)·블룸·고리·기둥·충격파·돌파 꽃잎·번쩍임·흔들림, 부품 그림 경로와 대체 그림 |
| 저장 | (해당 줄 없음) | 연출 진행은 저장하지 않는다 |
| 범위 밖 | 오오라를 Unity 파티클 같은 이펙트로 만들지 않는다 | `ParticleSystem`·가산 재질·히트스톱·연출 중 잠금·평소 조명은 하지 않는다. 부품 그림은 사용자가 만든다 |

- 완료 조건 초안: 새로 34~51 / 바뀜 26, 31 / 나머지는 기록 파일로 옮김 (이미 구현됨)
- 바뀌는 연결 지점: 기존 멤버는 그대로 두고 위 타입·메서드만 더한다. `RealmAuraView.Initialize`의 모양과 `GameRoot` 조립은 같다.
- 고쳐야 할 기존 코드: `Assets/Scripts/Core/Realm/RealmAura.cs` — 색 값, 새 메서드. `Assets/Scripts/Game/Realm/RealmAuraView.cs` — 상시 오오라 층과 돌파 연출 전부.
- 기존 테스트:

| 테스트 | 지금 | 바뀐 뒤 | 처리 |
| --- | --- | --- | --- |
| `RealmAuraTests.Aura_ValidLevel_ReturnsPresenceAndColor` | `TestCase` 색 값이 하늘·파랑·보라·분홍 | 26번의 매화 분홍 계열 값 | 이름 유지, `TestCase` 값만 교체 |
