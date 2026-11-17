using System;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Save
{
    public sealed class AutoSave
    {
        public const double RetrySeconds = 3.0;
        private readonly ISaveStore store;
        private readonly Wallet wallet;
        private readonly StatUpgrades upgrades;
        private readonly StageProgress stage;
        private double retryRemaining;

        public AutoSave(ISaveStore store, Wallet wallet, StatUpgrades upgrades, StageProgress stage, bool enabled = true)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            this.stage = stage ?? throw new ArgumentNullException(nameof(stage));
            Enabled = enabled;
            wallet.BalanceChanged += OnBalanceChanged;
            upgrades.Upgraded += OnUpgraded;
            stage.StageStarted += OnStageStarted;
        }

        public bool Enabled { get; private set; }
        public bool IsDirty { get; private set; }
        public bool HasFailed { get; private set; }
        public event Action<bool> FailedChanged;

        public void Enable() => Enabled = true;

        public void Tick(double deltaSeconds)
        {
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (retryRemaining <= 0) return;
            retryRemaining -= deltaSeconds;
            if (retryRemaining <= 1e-9) retryRemaining = 0;
        }

        public bool Flush(long nowUtcTicks) => Enabled && IsDirty && retryRemaining == 0 && Write(nowUtcTicks);
        public bool SaveNow(long nowUtcTicks) => Enabled && Write(nowUtcTicks);

        private bool Write(long nowUtcTicks)
        {
            if (!store.TryWrite(SaveCodec.Encode(SaveData.Capture(wallet, upgrades, stage, nowUtcTicks))))
            {
                IsDirty = true;
                return false;
            }
            IsDirty = false;
            retryRemaining = 0;
            HasFailed = false;
            return true;
        }

        private void MarkDirty() { if (Enabled) IsDirty = true; }
        private void OnBalanceChanged(long balance) => MarkDirty();
        private void OnUpgraded(StatType stat) => MarkDirty();
        private void OnStageStarted(int number) => MarkDirty();
    }
}
