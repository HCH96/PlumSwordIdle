using System;

namespace PlumSword.Core.Stage
{
    public sealed class PlayTime
    {
        public PlayTime(double initialSeconds = 0.0)
        {
            Seconds = IsInvalid(initialSeconds) ? 0.0 : initialSeconds;
        }

        public double Seconds { get; private set; }

        public long Milliseconds => (long)Math.Floor(Seconds * 1000.0);

        public void Add(double deltaSeconds)
        {
            if (IsInvalid(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            Seconds += deltaSeconds;
        }

        private static bool IsInvalid(double value)
        {
            return value < 0.0 || double.IsNaN(value) || double.IsInfinity(value);
        }
    }
}
