using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.StatUpgrade
{
    public class StatUpgradesInnerPowerTests
    {
        [Test]
        public void TryUpgrade_ThreeTimes_IncreasesOnlyInnerPowerAndSpendsCosts()
        {
            var wallet = new Wallet(1000);
            var upgrades = new StatUpgrades(wallet, 10, 100);
            var costs = new List<long>();
            for (var i = 0; i < 3; i++)
            {
                costs.Add(upgrades.GetCost(StatType.InnerPower));
                Assert.That(upgrades.TryUpgrade(StatType.InnerPower), Is.True);
            }

            Assert.That(costs, Is.EqualTo(new long[] { 20, 25, 30 }));
            Assert.That(upgrades.GetLevel(StatType.InnerPower), Is.EqualTo(3));
            Assert.That(upgrades.GetCost(StatType.InnerPower), Is.EqualTo(35));
            Assert.That(wallet.Balance, Is.EqualTo(925));
            Assert.That(upgrades.Attack, Is.EqualTo(10));
            Assert.That(upgrades.MaxHealth, Is.EqualTo(100));
            Assert.That(upgrades.GetLevel(StatType.Attack), Is.EqualTo(0));
            Assert.That(upgrades.GetLevel(StatType.Health), Is.EqualTo(0));
        }

        [Test]
        public void TryUpgrade_Events_ExposeOldThenNewLevel()
        {
            var wallet = new Wallet(1000);
            var upgrades = new StatUpgrades(wallet, 10, 100);
            var order = new List<string>();
            var received = new List<StatType>();
            var oldLevel = -1;
            var newLevel = -1;
            long newCost = -1;
            wallet.BalanceChanged += _ => { order.Add("balance"); oldLevel = upgrades.GetLevel(StatType.InnerPower); };
            upgrades.Upgraded += stat =>
            {
                order.Add("upgraded");
                received.Add(stat);
                newLevel = upgrades.GetLevel(StatType.InnerPower);
                newCost = upgrades.GetCost(StatType.InnerPower);
            };

            Assert.That(upgrades.TryUpgrade(StatType.InnerPower), Is.True);
            Assert.That(order, Is.EqualTo(new[] { "balance", "upgraded" }));
            Assert.That(oldLevel, Is.EqualTo(0));
            Assert.That(received, Is.EqualTo(new[] { StatType.InnerPower }));
            Assert.That(newLevel, Is.EqualTo(1));
            Assert.That(newCost, Is.EqualTo(25));
        }

        [Test]
        public void TryUpgrade_InsufficientGold_DoesNotChangeOrNotify()
        {
            var wallet = new Wallet(19);
            var upgrades = new StatUpgrades(wallet, 10, 100);
            var notifications = 0;
            wallet.BalanceChanged += _ => notifications++;
            upgrades.Upgraded += _ => notifications++;

            Assert.That(upgrades.TryUpgrade(StatType.InnerPower), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(19));
            Assert.That(upgrades.GetLevel(StatType.InnerPower), Is.EqualTo(0));
            Assert.That(notifications, Is.EqualTo(0));
        }

        [Test]
        public void Upgrade_Locked_FailsWithoutChange()
        {
            var wallet = new Wallet(1000);
            var upgrades = new StatUpgrades(wallet, 10, 100);
            upgrades.SetLocked(true);

            Assert.That(upgrades.CanUpgrade(StatType.InnerPower), Is.False);
            Assert.That(upgrades.TryUpgrade(StatType.InnerPower), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(1000));
            Assert.That(upgrades.GetLevel(StatType.InnerPower), Is.EqualTo(0));
        }

        [Test]
        public void Upgrade_MaxLevel_CostDoesNotOverflowAndCannotSpend()
        {
            var wallet = new Wallet(long.MaxValue);
            var upgrades = new StatUpgrades(wallet, 10, 100, 0, 0, int.MaxValue);

            Assert.That(upgrades.GetLevel(StatType.InnerPower), Is.EqualTo(int.MaxValue));
            Assert.That(upgrades.IsMaxLevel(StatType.InnerPower), Is.True);
            Assert.That(upgrades.GetCost(StatType.InnerPower), Is.EqualTo(10737418255L));
            Assert.That(upgrades.TryUpgrade(StatType.InnerPower), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(long.MaxValue));
        }
    }
}
