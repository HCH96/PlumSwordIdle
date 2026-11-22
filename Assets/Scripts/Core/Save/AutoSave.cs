using System;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;
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
        private readonly RealmProgress realm;
        private readonly StageProgress stage;
        private readonly PlayTime playTime;
        private readonly AutoUpgrade autoUpgrade;
        private double retryRemaining;

        public AutoSave(ISaveStore store, Wallet wallet, StatUpgrades upgrades, RealmProgress realm, StageProgress stage, bool enabled = true,
            PlayTime playTime = null, AutoUpgrade autoUpgrade = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            this.realm = realm ?? throw new ArgumentNullException(nameof(realm));
            this.stage = stage ?? throw new ArgumentNullException(nameof(stage));
            this.playTime = playTime;
            this.autoUpgrade = autoUpgrade;
            Enabled = enabled;
            wallet.BalanceChanged += OnBalanceChanged;
            upgrades.Upgraded += OnUpgraded;
            stage.StageStarted += OnStageStarted;
            realm.ProgressChanged += MarkDirty;
        }

        public bool Enabled { get; private set; }
        public bool IsDirty { get; private set; }
        public bool HasFailed { get; private set; }
        public event Action<bool> FailedChanged;

        public void Enable() => Enabled = true;
        public void Disable() => Enabled = false;

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
            var text = SaveCodec.Encode(SaveData.Capture(wallet, upgrades, realm, stage, nowUtcTicks, playTime, autoUpgrade));
            bool succeeded;
            try { succeeded = store.TryWrite(text); }
            catch (Exception) { succeeded = false; }
            if (!succeeded)
            {
                IsDirty = true;
                retryRemaining = RetrySeconds;
                SetFailed(true);
                return false;
            }
            IsDirty = false;
            retryRemaining = 0;
            SetFailed(false);
            return true;
        }

        private void SetFailed(bool failed)
        {
            if (HasFailed == failed) return;
            HasFailed = failed;
            FailedChanged?.Invoke(failed);
        }

        private void MarkDirty() { if (Enabled) IsDirty = true; }
        private void OnBalanceChanged(long balance) => MarkDirty();
        private void OnUpgraded(StatType stat) => MarkDirty();
        private void OnStageStarted(int number) => MarkDirty();
    }
}
