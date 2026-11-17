using System;
using PlumSword.Core.Stage;

namespace PlumSword.Core.OfflineReward
{
    public static class OfflineRewardCalculator
    {
        public const long IntervalTicks = 100_000_000L;
        public const long MaxTicks = 288_000_000_000L;

        public static OfflineRewardResult Calculate(long savedAtUtcTicks, long nowUtcTicks, int stageNumber)
        {
            if (stageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(stageNumber));

            long elapsed = Elapsed(savedAtUtcTicks, nowUtcTicks);
            long limited = Math.Min(elapsed, MaxTicks);
            long applied = limited - limited % IntervalTicks;
            long kills = applied / IntervalTicks;
            return new OfflineRewardResult(elapsed, applied, kills,
                kills * StageRewards.NormalGold(stageNumber), elapsed > MaxTicks);
        }

        public static bool ShouldRestartOnResume(long pausedAtUtcTicks, long resumedAtUtcTicks, bool isSaveUpToDate)
        {
            return isSaveUpToDate && Elapsed(pausedAtUtcTicks, resumedAtUtcTicks) >= IntervalTicks;
        }

        private static long Elapsed(long start, long end)
        {
            return start < 0 || end < 0 || end < start ? 0 : end - start;
        }
    }
}
