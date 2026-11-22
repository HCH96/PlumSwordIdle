using System;

namespace PlumSword.Core.Balance
{
    public sealed class GrowthCurve
    {
        public GrowthCurve(double baseValue, double linear, double growth)
        {
            if (!IsFinite(baseValue) || baseValue < 0) throw new ArgumentOutOfRangeException(nameof(baseValue));
            if (!IsFinite(linear) || linear < 0) throw new ArgumentOutOfRangeException(nameof(linear));
            if (!IsFinite(growth) || growth < 1) throw new ArgumentOutOfRangeException(nameof(growth));
            Base = baseValue;
            Linear = linear;
            Growth = growth;
        }

        public double Base { get; }
        public double Linear { get; }
        public double Growth { get; }

        public long Evaluate(double n)
        {
            if (!IsFinite(n) || n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            // A zero curve stays zero even when the exponential term overflows.
            if (Base == 0 && Linear == 0) return 0;
            double value = (Base + Linear * n) * Math.Pow(Growth, n);
            if (double.IsInfinity(value) || value >= long.MaxValue) return long.MaxValue;
            return (long)Math.Floor(value);
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
