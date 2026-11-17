namespace PlumSword.Core.OfflineReward
{
    public sealed class OfflineRewardResult
    {
        internal OfflineRewardResult(long elapsedTicks, long appliedTicks, long killCount, long gold, bool isCapped)
        {
            ElapsedTicks = elapsedTicks;
            AppliedTicks = appliedTicks;
            KillCount = killCount;
            Gold = gold;
            IsCapped = isCapped;
        }

        public long ElapsedTicks { get; }
        public long AppliedTicks { get; }
        public long KillCount { get; }
        public long Gold { get; }
        public bool HasReward => Gold > 0;
        public bool IsCapped { get; }
    }
}
