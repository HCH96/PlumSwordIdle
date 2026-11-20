using System;

namespace PlumSword.Core.Realm
{
    // 돌파 연출의 시간표. 시간 인자: NaN 이면 ArgumentOutOfRangeException,
    // 음수·음의 무한대는 0 으로, 양의 무한대는 끝난 뒤로 계산한다.
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

        private const double FlashEndSeconds = BurstSeconds + FlashSeconds;
        private const double ShakeEndSeconds = BurstSeconds + ShakeSeconds;
        private const double FlashPeakAlpha = 0.8;
        private const double GatherLightRatio = 0.4;
        private const double BloomRatio = 0.5;
        private const double DimMaskInner = 1.5;
        private const double DimMaskWidth = 2.5;
        private const double ShockwaveEndSeconds = BurstSeconds + ShockwaveSeconds;
        private const double RingStartRatio = 0.3;
        private const double PillarStartRatio = 0.3;
        private const double PetalBaseSpeed = 3.0;
        private const double PetalGravity = 1.5;

        public static BreakthroughStyle Style(RealmLevel realm)
        {
            switch (realm)
            {
                case RealmLevel.ThirdRate:
                    return new BreakthroughStyle(false, false, false, false, 0, 0, 0, 0, 0, 0, 0, 0);
                case RealmLevel.SecondRate:
                    return new BreakthroughStyle(false, false, false, false, 12, 0.25, 0, 1.0, 0, 0, 2, 0);
                case RealmLevel.FirstRate:
                    return new BreakthroughStyle(true, false, false, false, 18, 0.35, 1, 1.5, 1.5, 0, 2, 2);
                case RealmLevel.Peak:
                    return new BreakthroughStyle(true, true, false, false, 24, 0.45, 2, 2.0, 2.0, 5.0, 3, 3);
                case RealmLevel.Transcendent:
                    return new BreakthroughStyle(true, true, true, true, 36, 0.60, 3, 3.0, 2.5, 7.0, 4, 5);
                default:
                    throw new ArgumentOutOfRangeException(nameof(realm));
            }
        }

        public static bool IsPlaying(double seconds)
        {
            return Normalize(seconds) < TotalSeconds;
        }

        public static bool ShowsNewAura(double seconds)
        {
            return Normalize(seconds) >= BurstSeconds;
        }

        public static double DimAlpha(RealmLevel realm, double seconds)
        {
            double d = Style(realm).DimAlpha;
            double t = Normalize(seconds);
            if (t < BurstSeconds) return d * t / BurstSeconds;
            if (t < AfterglowEndSeconds) return d;
            if (t < TotalSeconds) return d * (TotalSeconds - t) / (TotalSeconds - AfterglowEndSeconds);
            return 0;
        }

        public static double DimMask(double distance)
        {
            if (double.IsNaN(distance))
                throw new ArgumentOutOfRangeException(nameof(distance));
            if (double.IsPositiveInfinity(distance)) return 1;
            double value = (distance - DimMaskInner) / DimMaskWidth;
            if (value < 0) return 0;
            if (value > 1) return 1;
            return value;
        }

        public static double LightIntensity(RealmLevel realm, double seconds)
        {
            double l = Style(realm).LightPeak;
            double t = Normalize(seconds);
            if (t < BurstSeconds) return GatherLightRatio * l * t / BurstSeconds;
            if (t < AfterglowEndSeconds) return l * (AfterglowEndSeconds - t) / (AfterglowEndSeconds - BurstSeconds);
            return 0;
        }

        public static double BloomIntensity(RealmLevel realm, double seconds)
        {
            return LightIntensity(realm, seconds) * BloomRatio;
        }

        public static double FlashAlpha(RealmLevel realm, double seconds)
        {
            bool hasFlash = Style(realm).HasFlash;
            double t = Normalize(seconds);
            if (!hasFlash || t < BurstSeconds || t >= FlashEndSeconds) return 0;
            return FlashPeakAlpha * (FlashEndSeconds - t) / FlashSeconds;
        }

        public static int ShakePixels(RealmLevel realm, double seconds)
        {
            int pixels = Style(realm).ShakePixels;
            double t = Normalize(seconds);
            if (t < BurstSeconds || t >= ShakeEndSeconds) return 0;
            return pixels;
        }

        public static double BurstFade(double seconds)
        {
            double t = Normalize(seconds);
            if (t < BurstSeconds || t >= AfterglowEndSeconds) return 0;
            return (AfterglowEndSeconds - t) / (AfterglowEndSeconds - BurstSeconds);
        }

        public static double RingRadius(RealmLevel realm, double seconds)
        {
            var style = Style(realm);
            double t = Normalize(seconds);
            if (!style.HasRing || t < BurstSeconds || t >= AfterglowEndSeconds) return 0;
            double progress = (t - BurstSeconds) / (AfterglowEndSeconds - BurstSeconds);
            return style.RingRadius * (RingStartRatio + (1 - RingStartRatio) * progress);
        }

        public static double PillarWidthAt(double seconds)
        {
            double fade = BurstFade(seconds);
            if (fade <= 0) return 0;
            return PillarWidth * (PillarStartRatio + (1 - PillarStartRatio) * fade);
        }

        public static double ShockwaveRadius(RealmLevel realm, double seconds)
        {
            var style = Style(realm);
            double t = Normalize(seconds);
            if (!style.HasShockwave || t < BurstSeconds || t >= ShockwaveEndSeconds) return 0;
            return 2 * style.RingRadius * ShockwaveProgress(t);
        }

        public static double ShockwaveAlpha(RealmLevel realm, double seconds)
        {
            var style = Style(realm);
            double t = Normalize(seconds);
            if (!style.HasShockwave || t < BurstSeconds || t >= ShockwaveEndSeconds) return 0;
            return 1 - ShockwaveProgress(t);
        }

        // 검사 순서: 경지 → 시간(NaN) → index.
        public static EffectPoint BurstPetal(RealmLevel realm, int index, double seconds)
        {
            int n = Style(realm).BurstPetals;
            double t = Normalize(seconds);
            if (index < 0 || index >= n)
                throw new ArgumentOutOfRangeException(nameof(index));

            double a = 2 * Math.PI * index / n;
            if (t < BurstSeconds)
            {
                double radius = GatherRadius * (1 - t / BurstSeconds);
                return new EffectPoint(Math.Cos(a) * radius, Math.Sin(a) * radius, 1, t / BurstSeconds);
            }
            if (t < TotalSeconds)
            {
                double u = t - BurstSeconds;
                double speed = PetalBaseSpeed + PetalBaseSpeed * ((index * 7) % n) / (double)n;
                double x = Math.Cos(a) * speed * u;
                double y = Math.Sin(a) * speed * u - PetalGravity * u * u;
                return new EffectPoint(x, y, 1, 1 - u / (TotalSeconds - BurstSeconds));
            }
            return new EffectPoint(0, 0, 0, 0);
        }

        private static double ShockwaveProgress(double t)
        {
            return (t - BurstSeconds) / ShockwaveSeconds;
        }

        // NaN 은 예외, 음수·음의 무한대는 0, 양의 무한대는 그대로(끝난 뒤).
        private static double Normalize(double seconds)
        {
            if (double.IsNaN(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            return seconds < 0 ? 0 : seconds;
        }
    }
}
