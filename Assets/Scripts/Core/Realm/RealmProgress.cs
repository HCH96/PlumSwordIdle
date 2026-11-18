using System;
using PlumSword.Core.Currency;

namespace PlumSword.Core.Realm
{
    public sealed class RealmProgress
    {
        private readonly Wallet wallet;

        public RealmProgress(Wallet wallet, int startLevel = 0)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Current = (RealmLevel)Math.Max(0, Math.Min(RealmRules.LevelCount - 1, startLevel));
        }

        public RealmLevel Current { get; private set; }
        public bool IsMax => Current == RealmLevel.Transcendent;
        public int BonusPercent => RealmRules.BonusPercent(Current);
        public long NextCost => RealmRules.BreakthroughCost(Current);
        public bool Locked { get; private set; }

        public event Action<RealmLevel> BrokeThrough;
        public event Action<bool> LockedChanged;

        public bool CanBreakthrough()
        {
            return !Locked && !IsMax && wallet.CanSpend(NextCost);
        }

        public bool TryBreakthrough()
        {
            if (Locked || IsMax || !wallet.TrySpend(NextCost))
            {
                return false;
            }

            Current = (RealmLevel)((int)Current + 1);
            BrokeThrough?.Invoke(Current);
            return true;
        }

        public void SetLocked(bool locked)
        {
            if (Locked == locked)
            {
                return;
            }

            Locked = locked;
            LockedChanged?.Invoke(locked);
        }
    }
}
