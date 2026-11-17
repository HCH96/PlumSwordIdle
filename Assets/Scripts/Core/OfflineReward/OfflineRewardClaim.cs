using System;
using PlumSword.Core.Currency;
using PlumSword.Core.Save;

namespace PlumSword.Core.OfflineReward
{
    public sealed class OfflineRewardClaim
    {
        private readonly Wallet wallet;
        private readonly AutoSave autoSave;

        public OfflineRewardClaim(Wallet wallet, AutoSave autoSave, OfflineRewardResult reward)
        {
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.autoSave = autoSave ?? throw new ArgumentNullException(nameof(autoSave));
            Reward = reward ?? throw new ArgumentNullException(nameof(reward));
            if (!reward.HasReward) throw new ArgumentException("Reward must contain gold.", nameof(reward));
        }

        public OfflineRewardResult Reward { get; }
        public OfflineRewardClaimState State { get; private set; }
        public bool HasPaid { get; private set; }

        public bool TryClaim(long nowUtcTicks)
        {
            if (State == OfflineRewardClaimState.Done) return true;
            if (!HasPaid)
            {
                wallet.Add(Reward.Gold);
                HasPaid = true;
                autoSave.Enable();
            }

            bool saved = autoSave.SaveNow(nowUtcTicks);
            State = saved ? OfflineRewardClaimState.Done : OfflineRewardClaimState.SaveFailed;
            return saved;
        }
    }
}
