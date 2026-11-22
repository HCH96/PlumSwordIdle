# 경지 돌파 사양

- 기능 이름: realm (네임스페이스 `PlumSword.Core.Realm`)
- 작성일: 2026-11-18
- 고친 날: 2026-11-20

## 목표와 동작

경지는 삼류 → 이류 → 일류 → 절정 → 초절정이다. 일반 몬스터와 보스 처치를 각각 한 마리로 세고, 각 경지에서 30·50·70·100마리를 처치하면 금화 소비나 버튼 조작 없이 다음 경지로 자동 돌파한다. 돌파할 때마다 처치 수는 0으로 돌아간다. 150은 추후 단계용으로 남겨 두며 현재 사용하지 않는다. 초절정에서는 처치를 더 세지 않는다. 스테이지 이동·실패와 재시작 후에도 진행도는 유지한다.

`GameRoot`가 `Battle.EnemyDefeated`마다 `RecordKill()`을 한 번 부른다. 동시 처치도 각 적마다 센다. 돌파 시 `Current`를 바꾸고 `BrokeThrough`를 보낸 다음 `ProgressChanged`를 보낸다. 일반 처치에서는 `ProgressChanged`만 보낸다. 기존 돌파 연출과 최대 체력 증가 알림은 그대로 사용한다. 금화와 강화 잠금은 돌파 조건이 아니다.

## 수치

수치는 `docs/spec/balance.md`의 `BalanceTable`을 따른다 (기본값은 아래 값과 같다).

| 항목 | 값 |
| --- | --- |
| 경지별 다음 돌파 처치 수 | 삼류 30, 이류 50, 일류 70, 절정 100, 초절정 0 |
| 누적 보너스 | 삼류 0%, 이류 10%, 일류 20%, 절정 30%, 초절정 40% |
| 실제 화면 글자 | 삼류, 이류, 일류, 절정, 초절정 그림 글자 |
| 에셋 ID | `third_rate`, `second_rate`, `first_rate`, `peak`, `transcendent` |

| 항목 | 값 | 비고 |
| --- | --- | --- |
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

        // 현재 경지에서 다음 단계까지 필요한 처치 수. 초절정은 0.
        public static int KillsRequired(RealmLevel from);
    }

    public sealed class RealmProgress
    {
        public RealmProgress(PlumSword.Core.Currency.Wallet wallet, int startLevel = 0, int kills = 0);
        public RealmLevel Current { get; }
        public bool IsMax { get; }
        public int BonusPercent { get; }
        public int Kills { get; }
        public int KillsRequired { get; }
        public void RecordKill();
        public event System.Action<RealmLevel> BrokeThrough;
        public event System.Action ProgressChanged;
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
        // canvas 아래에 입력을 받지 않는 경지 표시를 만들고 지금 상태로 바로 표시한다.
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

- `GameRoot`가 저장된 경지와 처치 수로 생성하고 `Battle.EnemyDefeated`를 연결한다.
- 능력치 강화는 `BonusPercent`를 읽고 전투는 `BrokeThrough`를 받아 현재 체력을 갱신한다.
- 저장은 `Current`, `Kills`를 보관하고 `ProgressChanged`를 구독한다.

### 필요한 것

- `Battle.EnemyDefeated`와 `Wallet` (생성자 호환성).
- 오오라를 위한 `Battle.DisciplePosition`, 카메라, URP 2D의 빛·볼륨 API.

## 화면

- **조립**: `GameRoot.Awake`에서 `[StatUpgradeView]`를 만든 다음에 `[Game]`의 자식 `[RealmView]`를 만들어 `RealmView`를 붙이고 `Initialize(Realm, Wallet, UiCanvas)`를 부른다. `[CombatView]`를 만든 다음에 `[Game]`의 자식 `[RealmAuraView]`를 만들어 `RealmAuraView`를 붙이고 `Initialize(Realm, Battle)`를 부른다.
- 모든 그림은 `Resources.Load`로 읽는다. 없으면 아래 대체 표시를 쓰고 게임은 그대로 진행된다 (오류 로그를 남기지 않는다. 그림 오오라 시트의 크기가 틀린 경우만 `Debug.LogWarning`).

### 현재 경지 표시 (화면 왼쪽 위)

- `Art/UI/RealmStatus/brush_plum`과 `name_<AssetId>`를 겹쳐 D안의 매화 붓터치와 한글 그림 글자를 표시한다. 두 이미지는 입력을 받지 않는다.
- 640×360 기준 왼쪽 위 앵커: 배경 위치 (4, -2), 140×47; 글자 위치 (24, -9), 100×33. 원본 투명 여백을 유지한다.
- 해당 글자 그림이 없으면 `RealmText.Label(Current)` 영문 대체 표시를 사용한다. 경지가 오를 때 갱신한다. 기존 하단 돌파 버튼은 만들지 않는다.

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
- **빛**: 오오라 가운데에 점 `Light2D`(안 반지름 0.5, 바깥 반지름 5.0 유닛, 색 `RealmAura.Color(r)`의 RGB, 세기 `LightIntensity(r, t)`, 모든 정렬 레이어에 적용). `Initialize` 때 장면에 전역(Global) `Light2D`가 없으면 세기 1, 흰색, 모든 정렬 레이어에 적용되는 것을 하나 만든다 (점광원 하나만 있으면 나머지 화면이 어두워지기 때문). 연출이 아닐 때 점광원 세기는 0이다(컴포넌트도 끈다). 만든 전역 광원은 꺼 둔 채로 두고, 연출이 시작될 때 장면에 켜진 다른 전역 광원이 없을 때만 켜며 연출이 끝나면 끈다 (`[Game]`은 씬을 넘어 살아 있어서, 늘 켜 두면 나중에 불러온 씬의 전역 광원과 겹쳐 URP가 "More than one global light" 오류를 남긴다).
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

`realmLevel`과 현재 경지에서 누적한 `realmKills`를 저장한다. 기존 판 2 저장에 `realmKills`가 없으면 0으로 복원하고 다른 값은 유지한다. 음수 처치 수는 0, 요구치를 넘는 값은 요구치 - 1로 자른다. 초절정은 0이다. 오프라인 동안 처치와 경지는 바뀌지 않는다. 돌파 연출의 진행은 저장하지 않는다.

## 범위 밖

- 150마리 조건을 적용할 추가 경지, 오프라인 처치, 처치 진행도 표시.
- 돌파 소리, 경지 되돌리기·환불.

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

31. 삼류에서 30번째 몬스터를 처치하면 금화 변화 없이 경지가 이류로 바뀌고 왼쪽 위 한글 표시와 돌파 연출이 갱신된다. 하단에는 돌파 버튼이 없다.
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
- 고쳐야 할 기존 코드: `Assets/Scripts/Core/Realm/RealmAura.cs` — 색 값, 새 메서드. `Assets/Scripts/Game/Realm/RealmAuraView.cs` — 상시 오오라 층과 돌파 연출 전부. `Assets/Scripts/Game/PlumSword.Game.asmdef` — URP 참조 두 줄 추가 ("연결 지점"의 "필요한 것").
- 기존 테스트:

| 테스트 | 지금 | 바뀐 뒤 | 처리 |
| --- | --- | --- | --- |
| `RealmAuraTests.Aura_ValidLevel_ReturnsPresenceAndColor` | `TestCase` 색 값이 하늘·파랑·보라·분홍 | 26번의 매화 분홍 계열 값 | 이름 유지, `TestCase` 값만 교체 |
