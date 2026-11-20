using System;

namespace PlumSword.Core.Realm
{
    public static class RealmAura
    {
        public const double PulsePeriodSeconds = 1.6;
        public const double BurstSeconds = 0.4;
        public const double BurstPeakScale = 1.5;

        public static bool HasAura(RealmLevel realm)
        {
            return Color(realm).A > 0;
        }

        public static AuraColor Color(RealmLevel realm)
        {
            switch (realm)
            {
                case RealmLevel.ThirdRate: return new AuraColor(0, 0, 0, 0);
                case RealmLevel.SecondRate: return new AuraColor(1.00, 0.72, 0.82, 0.35);
                case RealmLevel.FirstRate: return new AuraColor(1.00, 0.55, 0.75, 0.40);
                case RealmLevel.Peak: return new AuraColor(0.95, 0.35, 0.65, 0.45);
                case RealmLevel.Transcendent: return new AuraColor(1.00, 0.40, 0.70, 0.50);
                default: throw new ArgumentOutOfRangeException(nameof(realm));
            }
        }

        // 돌파 부품(고리·기둥·충격파)의 색. 초절정은 흰 중심광.
        public static AuraColor BurstColor(RealmLevel realm)
        {
            switch (realm)
            {
                case RealmLevel.ThirdRate: return new AuraColor(0, 0, 0, 0);
                case RealmLevel.SecondRate:
                case RealmLevel.FirstRate:
                case RealmLevel.Peak:
                    var c = Color(realm);
                    return new AuraColor(c.R, c.G, c.B, 1.0);
                case RealmLevel.Transcendent: return new AuraColor(1.00, 0.95, 0.97, 1.00);
                default: throw new ArgumentOutOfRangeException(nameof(realm));
            }
        }

        // 상시 기운 한 줄. index 는 0 ~ Style(realm).AuraWisps - 1.
        public static EffectPoint Wisp(RealmLevel realm, int index, double seconds)
        {
            int n = CheckedCount(BreakthroughEffect.Style(realm).AuraWisps, index, seconds);
            double phase = index / (double)n;
            double x = (index - (n - 1) / 2.0) * 0.45 + 0.12 * Math.Sin(2 * Math.PI * (seconds / 1.2 + phase));
            double scale = 1 + 0.15 * Math.Sin(2 * Math.PI * (seconds / 0.9 + phase));
            double alpha = Math.Min(1, 2 * PulseAlpha(realm, seconds + 0.4 * index));
            return new EffectPoint(x, 0, scale, alpha);
        }

        // 상시 꽃잎 한 장. index 는 0 ~ Style(realm).AuraPetals - 1. 음수 seconds 도 그대로 계산한다.
        public static EffectPoint FloatingPetal(RealmLevel realm, int index, double seconds)
        {
            int n = CheckedCount(BreakthroughEffect.Style(realm).AuraPetals, index, seconds);
            double p = seconds / 3 + index / (double)n;
            double f = p - Math.Floor(p);
            double x = 0.8 * Math.Sin(3 * Math.PI * p + index);
            double y = -1.05 + 2.4 * f;
            double alpha = Math.Sin(Math.PI * f);
            return new EffectPoint(x, y, 1, alpha);
        }

        private static int CheckedCount(int count, int index, double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return count;
        }

        public static double PulseAlpha(RealmLevel realm, double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            double alpha = Color(realm).A;
            return alpha * (0.75 + 0.25 * Math.Sin(2 * Math.PI * seconds / PulsePeriodSeconds));
        }

        public static double BurstScale(double secondsSinceBreakthrough)
        {
            if (double.IsNaN(secondsSinceBreakthrough))
                throw new ArgumentOutOfRangeException(nameof(secondsSinceBreakthrough));
            if (secondsSinceBreakthrough <= 0) return BurstPeakScale;
            if (secondsSinceBreakthrough >= BurstSeconds) return 1.0;
            return BurstPeakScale - 0.5 * secondsSinceBreakthrough / BurstSeconds;
        }
    }
}
