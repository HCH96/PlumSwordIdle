using System;

namespace PlumSword.Core.Audio
{
    public sealed class SfxVoicePool
    {
        private readonly bool[] occupied;
        private readonly double[] starts;
        private readonly double[] ends;

        public SfxVoicePool(int count = AudioCatalog.MaxSfxVoices)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            occupied = new bool[count];
            starts = new double[count];
            ends = new double[count];
        }

        public int Count { get { return occupied.Length; } }

        public int Acquire(double nowSeconds, double lengthSeconds)
        {
            if (double.IsNaN(nowSeconds) || double.IsInfinity(nowSeconds)) throw new ArgumentOutOfRangeException(nameof(nowSeconds));
            if (double.IsNaN(lengthSeconds) || double.IsInfinity(lengthSeconds) || lengthSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(lengthSeconds));

            int selected = -1;
            for (int i = 0; i < Count; i++)
                if (!occupied[i] || ends[i] <= nowSeconds) { selected = i; break; }
            if (selected < 0)
            {
                selected = 0;
                for (int i = 1; i < Count; i++)
                    if (starts[i] < starts[selected]) selected = i;
            }
            occupied[selected] = true;
            starts[selected] = nowSeconds;
            ends[selected] = nowSeconds + lengthSeconds;
            return selected;
        }
    }
}
