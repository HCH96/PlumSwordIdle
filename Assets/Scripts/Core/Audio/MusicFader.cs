using System;

namespace PlumSword.Core.Audio
{
    public sealed class MusicFader
    {
        private readonly double crossfadeSeconds;

        public MusicFader(double crossfadeSeconds = AudioCatalog.CrossfadeSeconds)
        {
            if (crossfadeSeconds <= 0 || double.IsNaN(crossfadeSeconds) || double.IsInfinity(crossfadeSeconds))
                throw new ArgumentOutOfRangeException(nameof(crossfadeSeconds));
            this.crossfadeSeconds = crossfadeSeconds;
        }

        public MusicTrack Current { get; private set; }
        public double CurrentWeight { get; private set; }
        public MusicTrack Previous { get; private set; }
        public double PreviousWeight { get; private set; }

        public bool Request(MusicTrack track)
        {
            // The catalog validates all defined track values, including None.
            AudioCatalog.MusicPath(track);
            if (track == Current) return false;

            if (Current != MusicTrack.None)
            {
                Previous = Current;
                PreviousWeight = CurrentWeight;
            }
            Current = track;
            CurrentWeight = 0;
            return true;
        }

        public void Tick(double deltaSeconds)
        {
            if (deltaSeconds < 0 || double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (deltaSeconds == 0) return;

            double change = deltaSeconds / crossfadeSeconds;
            if (Current != MusicTrack.None)
                CurrentWeight = Math.Min(1.0, CurrentWeight + change);
            PreviousWeight = Math.Max(0.0, PreviousWeight - change);
            if (PreviousWeight == 0) Previous = MusicTrack.None;
        }
    }
}
