using System;
using System.Collections.Generic;

namespace PlumSword.Core.Audio
{
    public sealed class SfxQueue
    {
        private readonly double minIntervalSeconds;
        private readonly Func<SfxId, bool> isEnabled;
        private readonly HashSet<SfxId> pending = new HashSet<SfxId>();
        private readonly Dictionary<SfxId, double> lastPlayed = new Dictionary<SfxId, double>();

        public SfxQueue(double minIntervalSeconds = AudioCatalog.SfxMinIntervalSeconds, Func<SfxId, bool> isEnabled = null)
        {
            if (double.IsNaN(minIntervalSeconds) || double.IsInfinity(minIntervalSeconds) || minIntervalSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(minIntervalSeconds));
            this.minIntervalSeconds = minIntervalSeconds;
            this.isEnabled = isEnabled ?? AudioCatalog.IsSfxEnabled;
        }

        public bool HasPending { get { return pending.Count != 0; } }

        public void Request(SfxId id)
        {
            if (!Enum.IsDefined(typeof(SfxId), id)) throw new ArgumentOutOfRangeException(nameof(id));
            pending.Add(id);
        }

        public IReadOnlyList<SfxId> Drain(double nowSeconds)
        {
            if (double.IsNaN(nowSeconds) || double.IsInfinity(nowSeconds))
                throw new ArgumentOutOfRangeException(nameof(nowSeconds));

            bool skill = pending.Contains(SfxId.PlumSwordSkill);
            bool specific = pending.Contains(SfxId.UpgradeSuccess) || pending.Contains(SfxId.OfflineReward)
                || pending.Contains(SfxId.WindowOpenClose);
            var result = new List<SfxId>();
            foreach (SfxId id in pending)
            {
                if (skill && (id == SfxId.SwordSwing || id == SfxId.AttackHit)) continue;
                if (specific && (id == SfxId.UiConfirm || id == SfxId.UpgradeUnavailable)) continue;
                if (!isEnabled(id)) continue;
                double last;
                if (lastPlayed.TryGetValue(id, out last) && nowSeconds >= last && nowSeconds - last < minIntervalSeconds) continue;
                result.Add(id);
            }
            result.Sort();
            foreach (SfxId id in result) lastPlayed[id] = nowSeconds;
            pending.Clear();
            return result;
        }
    }
}
