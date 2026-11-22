using System;
using PlumSword.Core.Currency;

namespace PlumSword.Core.StatUpgrade
{
    public sealed class AutoUpgrade
    {
        private const int MaxPurchasesPerCall = 1000;
        private static readonly StatType[] Stats = { StatType.Attack, StatType.Health, StatType.InnerPower };
        private readonly StatUpgrades _upgrades;
        private readonly Wallet _wallet;
        private bool _buying;

        public AutoUpgrade(StatUpgrades upgrades, Wallet wallet, bool enabled = true)
        {
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Enabled = enabled;
            _wallet.BalanceChanged += OnBalanceChanged;
            _upgrades.LockedChanged += OnLockedChanged;
        }

        public bool Enabled { get; private set; }
        public event Action<bool> EnabledChanged;

        public void SetEnabled(bool enabled)
        {
            if (Enabled == enabled) return;
            Enabled = enabled;
            EnabledChanged?.Invoke(enabled);
            if (enabled) BuyAll();
        }

        public int BuyAll()
        {
            if (!Enabled || _buying) return 0;
            _buying = true;
            try
            {
                int count = 0;
                while (Enabled && count < MaxPurchasesPerCall)
                {
                    bool found = false;
                    StatType cheapest = default(StatType);
                    long lowestCost = long.MaxValue;
                    foreach (var stat in Stats)
                    {
                        if (!_upgrades.CanUpgrade(stat)) continue;
                        long cost = _upgrades.GetCost(stat);
                        if (!found || cost < lowestCost)
                        {
                            found = true;
                            cheapest = stat;
                            lowestCost = cost;
                        }
                    }
                    if (!found || !_upgrades.TryUpgrade(cheapest)) break;
                    count++;
                }
                return count;
            }
            finally
            {
                _buying = false;
            }
        }

        public void Dispose()
        {
            _wallet.BalanceChanged -= OnBalanceChanged;
            _upgrades.LockedChanged -= OnLockedChanged;
        }

        private void OnBalanceChanged(long balance) { BuyAll(); }
        private void OnLockedChanged(bool locked) { if (!locked) BuyAll(); }
    }
}
