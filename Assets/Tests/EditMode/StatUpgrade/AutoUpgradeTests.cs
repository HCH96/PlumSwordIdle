using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Balance;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.StatUpgrade
{
    public class AutoUpgradeTests
    {
        private Wallet wallet;
        private StatUpgrades upgrades;
        private AutoUpgrade auto;

        [SetUp]
        public void SetUp()
        {
            BalanceTable.Current = BalanceTable.Default;
            wallet = new Wallet();
            upgrades = new StatUpgrades(wallet, 10, 100);
        }

        [TearDown]
        public void TearDown()
        {
            auto?.Dispose();
            BalanceTable.Current = BalanceTable.Default;
        }

        private void AssertLevels(int attack, int health, int inner)
        {
            Assert.That(upgrades.GetLevel(StatType.Attack), Is.EqualTo(attack));
            Assert.That(upgrades.GetLevel(StatType.Health), Is.EqualTo(health));
            Assert.That(upgrades.GetLevel(StatType.InnerPower), Is.EqualTo(inner));
        }

        [Test]
        public void New_WithGold_DefaultEnabledAndDoesNotBuy()
        {
            wallet.Add(100);
            auto = new AutoUpgrade(upgrades, wallet);
            Assert.That(auto.Enabled, Is.True);
            AssertLevels(0, 0, 0);
            Assert.That(wallet.Balance, Is.EqualTo(100));
        }

        [Test]
        public void BalanceChanged_Twenty_BuysAttack()
        {
            auto = new AutoUpgrade(upgrades, wallet);
            wallet.Add(20);
            AssertLevels(1, 0, 0);
            Assert.That(wallet.Balance, Is.Zero);
        }

        [Test]
        public void BalanceChanged_Hundred_BuysCheapestInTieOrder()
        {
            auto = new AutoUpgrade(upgrades, wallet);
            wallet.Add(100);
            AssertLevels(2, 1, 1);
            Assert.That(wallet.Balance, Is.EqualTo(15));
        }

        [Test]
        public void BalanceChanged_Disabled_DoesNotBuy()
        {
            auto = new AutoUpgrade(upgrades, wallet, false);
            wallet.Add(100);
            AssertLevels(0, 0, 0);
            Assert.That(wallet.Balance, Is.EqualTo(100));
        }

        [Test]
        public void SetEnabled_True_NotifiesBeforeBuying()
        {
            auto = new AutoUpgrade(upgrades, wallet, false);
            wallet.Add(100);
            var values = new List<bool>();
            auto.EnabledChanged += value => { values.Add(value); AssertLevels(0, 0, 0); };
            auto.SetEnabled(true);
            Assert.That(values, Is.EqualTo(new[] { true }));
            AssertLevels(2, 1, 1);
            Assert.That(wallet.Balance, Is.EqualTo(15));
        }

        [Test]
        public void Unlock_WithGold_BuysAfterUnlock()
        {
            auto = new AutoUpgrade(upgrades, wallet);
            upgrades.SetLocked(true);
            wallet.Add(20);
            AssertLevels(0, 0, 0);
            Assert.That(wallet.Balance, Is.EqualTo(20));
            upgrades.SetLocked(false);
            AssertLevels(1, 0, 0);
            Assert.That(wallet.Balance, Is.Zero);
        }

        [Test]
        public void BuyAll_NoGold_ReturnsZero()
        {
            auto = new AutoUpgrade(upgrades, wallet);
            Assert.That(auto.BuyAll(), Is.Zero);
        }

        [Test]
        public void BalanceChanged_AttackMoreExpensive_BuysHealth()
        {
            upgrades = new StatUpgrades(wallet, 10, 100, 1, 0, 0);
            auto = new AutoUpgrade(upgrades, wallet);
            wallet.Add(20);
            AssertLevels(1, 1, 0);
            Assert.That(wallet.Balance, Is.Zero);
        }

        [Test]
        public void SetEnabled_AlreadyEnabled_DoesNotNotify()
        {
            auto = new AutoUpgrade(upgrades, wallet);
            int count = 0;
            auto.EnabledChanged += _ => count++;
            auto.SetEnabled(true);
            Assert.That(count, Is.Zero);
        }

        [Test]
        public void SetEnabled_False_StopsPurchases()
        {
            auto = new AutoUpgrade(upgrades, wallet);
            auto.SetEnabled(false);
            wallet.Add(100);
            AssertLevels(0, 0, 0);
            Assert.That(wallet.Balance, Is.EqualTo(100));
            Assert.That(auto.BuyAll(), Is.Zero);
        }

        [Test]
        public void BalanceChanged_FreeUpgrades_StopsAtThousand()
        {
            var table = BalanceTable.Default;
            table.AttackCost = new GrowthCurve(0, 0, 1);
            table.HealthCost = new GrowthCurve(0, 0, 1);
            table.InnerPowerCost = new GrowthCurve(0, 0, 1);
            BalanceTable.Current = table;
            auto = new AutoUpgrade(upgrades, wallet);
            wallet.Add(1);
            Assert.That(upgrades.GetLevel(StatType.Attack) + upgrades.GetLevel(StatType.Health) + upgrades.GetLevel(StatType.InnerPower), Is.EqualTo(1000));
            Assert.That(wallet.Balance, Is.EqualTo(1));
        }

        [Test]
        public void New_NullArguments_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new AutoUpgrade(null, wallet));
            Assert.Throws<ArgumentNullException>(() => new AutoUpgrade(upgrades, null));
        }

        [Test]
        public void Dispose_Twice_UnsubscribesFromBalanceAndUnlock()
        {
            auto = new AutoUpgrade(upgrades, wallet);
            auto.Dispose();
            Assert.DoesNotThrow(() => auto.Dispose());
            wallet.Add(100);
            AssertLevels(0, 0, 0);
            upgrades.SetLocked(true);
            upgrades.SetLocked(false);
            AssertLevels(0, 0, 0);
            Assert.That(wallet.Balance, Is.EqualTo(100));
        }

        [TestCase(true, "자동 강화 켜짐")]
        [TestCase(false, "자동 강화 꺼짐")]
        public void Label_State_ReturnsText(bool enabled, string expected)
        {
            Assert.That(AutoUpgradeText.Label(enabled), Is.EqualTo(expected));
        }

        [Test]
        public void BuyAll_ReentrantBalanceNotification_ReturnsZero()
        {
            wallet.Add(100);
            auto = new AutoUpgrade(upgrades, wallet);
            var nested = new List<int>();
            wallet.BalanceChanged += _ => nested.Add(auto.BuyAll());
            Assert.That(auto.BuyAll(), Is.EqualTo(4));
            Assert.That(nested, Is.EqualTo(new[] { 0, 0, 0, 0 }));
            AssertLevels(2, 1, 1);
        }
    }
}
