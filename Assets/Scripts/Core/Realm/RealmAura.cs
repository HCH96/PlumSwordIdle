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
                case RealmLevel.SecondRate: return new AuraColor(0.55, 0.85, 1.00, 0.35);
                case RealmLevel.FirstRate: return new AuraColor(0.35, 0.55, 1.00, 0.40);
                case RealmLevel.Peak: return new AuraColor(0.70, 0.40, 1.00, 0.45);
                case RealmLevel.Transcendent: return new AuraColor(1.00, 0.55, 0.75, 0.50);
                default: throw new ArgumentOutOfRangeException(nameof(realm));
            }
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
