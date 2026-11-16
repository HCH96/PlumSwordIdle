using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.StatUpgrade
{
    public class StatUpgradesTests
    {
        private const double Tolerance = 1e-9;
        private const StatType Undefined = (StatType)99;

        private static readonly StatType[] AllStats = { StatType.Attack, StatType.Health, StatType.AttackSpeed };

        private static StatUpgrades Create(Wallet wallet)
        {
            return new StatUpgrades(wallet, 10, 100, 1.0);
        }

        private static List<StatType> RecordUpgraded(StatUpgrades upgrades)
        {
            var received = new List<StatType>();
            upgrades.Upgraded += stat => received.Add(stat);
            return received;
        }

        private static List<bool> RecordLocked(StatUpgrades upgrades)
        {
            var received = new List<bool>();
            upgrades.LockedChanged += value => received.Add(value);
            return received;
        }

        private static List<long> RecordBalance(Wallet wallet)
        {
            var received = new List<long>();
            wallet.BalanceChanged += value => received.Add(value);
            return received;
        }

        // 새로 만들 때

        [Test]
        public void New_Default_InitialState()
        {
            var upgrades = Create(new Wallet(1000));

            foreach (var stat in AllStats)
            {
                Assert.That(upgrades.GetLevel(stat), Is.EqualTo(0), stat.ToString());
                Assert.That(upgrades.GetCost(stat), Is.EqualTo(20), stat.ToString());
                Assert.That(upgrades.IsMaxLevel(stat), Is.False, stat.ToString());
            }

            Assert.That(upgrades.Attack, Is.EqualTo(10));
            Assert.That(upgrades.MaxHealth, Is.EqualTo(100));
            Assert.That(upgrades.AttackInterval, Is.EqualTo(1.0).Within(Tolerance));
            Assert.That(upgrades.Locked, Is.False);
        }

        [Test]
        public void GetMaxLevel_EachStat_ReturnsLimit()
        {
            var upgrades = Create(new Wallet());

            Assert.That(upgrades.GetMaxLevel(StatType.AttackSpeed), Is.EqualTo(10));
            Assert.That(upgrades.GetMaxLevel(StatType.Attack), Is.EqualTo(int.MaxValue));
            Assert.That(upgrades.GetMaxLevel(StatType.Health), Is.EqualTo(int.MaxValue));
        }

        // 강화

        [Test]
        public void TryUpgrade_AttackThreeTimes_CostRisesAndAttackIncreases()
        {
            var wallet = new Wallet(1000);
            var upgrades = Create(wallet);
            var costs = new List<long>();

            for (var i = 0; i < 3; i++)
            {
                costs.Add(upgrades.GetCost(StatType.Attack));
                Assert.That(upgrades.TryUpgrade(StatType.Attack), Is.True);
            }

            Assert.That(costs, Is.EqualTo(new long[] { 20, 25, 30 }));
            Assert.That(upgrades.GetLevel(StatType.Attack), Is.EqualTo(3));
            Assert.That(upgrades.GetCost(StatType.Attack), Is.EqualTo(35));
            Assert.That(upgrades.Attack, Is.EqualTo(16));
            Assert.That(wallet.Balance, Is.EqualTo(925));
        }

        [Test]
        public void TryUpgrade_Attack_OtherStatsUnchanged()
        {
            var upgrades = Create(new Wallet(1000));

            upgrades.TryUpgrade(StatType.Attack);

            Assert.That(upgrades.GetLevel(StatType.Health), Is.EqualTo(0));
            Assert.That(upgrades.GetLevel(StatType.AttackSpeed), Is.EqualTo(0));
            Assert.That(upgrades.GetCost(StatType.Health), Is.EqualTo(20));
            Assert.That(upgrades.GetCost(StatType.AttackSpeed), Is.EqualTo(20));
            Assert.That(upgrades.MaxHealth, Is.EqualTo(100));
            Assert.That(upgrades.AttackInterval, Is.EqualTo(1.0).Within(Tolerance));
        }

        [Test]
        public void TryUpgrade_HealthTwice_MaxHealthIncreases()
        {
            var wallet = new Wallet(100);
            var upgrades = Create(wallet);

            upgrades.TryUpgrade(StatType.Health);
            Assert.That(upgrades.MaxHealth, Is.EqualTo(120));
            Assert.That(wallet.Balance, Is.EqualTo(80));

            upgrades.TryUpgrade(StatType.Health);
            Assert.That(upgrades.MaxHealth, Is.EqualTo(140));
            Assert.That(wallet.Balance, Is.EqualTo(55));
        }

        [Test]
        public void TryUpgrade_AttackSpeed_IntervalDecreases()
        {
            var wallet = new Wallet(1000);
            var upgrades = Create(wallet);

            upgrades.TryUpgrade(StatType.AttackSpeed);
            Assert.That(upgrades.AttackInterval, Is.EqualTo(0.95).Within(Tolerance));
            Assert.That(wallet.Balance, Is.EqualTo(980));

            upgrades.TryUpgrade(StatType.AttackSpeed);
            upgrades.TryUpgrade(StatType.AttackSpeed);
            Assert.That(upgrades.AttackInterval, Is.EqualTo(0.85).Within(Tolerance));
        }

        [Test]
        public void TryUpgrade_AttackSpeedTenTimes_ReachesMaxLevel()
        {
            var wallet = new Wallet(1000);
            var upgrades = Create(wallet);

            for (var i = 0; i < 10; i++)
            {
                Assert.That(upgrades.TryUpgrade(StatType.AttackSpeed), Is.True, $"call {i + 1}");
            }

            Assert.That(upgrades.GetLevel(StatType.AttackSpeed), Is.EqualTo(10));
            Assert.That(upgrades.AttackInterval, Is.EqualTo(0.5).Within(Tolerance));
            Assert.That(upgrades.IsMaxLevel(StatType.AttackSpeed), Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(575));
        }

        [Test]
        public void TryUpgrade_AttackSpeedAtMaxLevel_FailsWithoutChange()
        {
            var wallet = new Wallet(1000);
            var upgrades = Create(wallet);
            for (var i = 0; i < 10; i++)
            {
                upgrades.TryUpgrade(StatType.AttackSpeed);
            }

            var upgraded = RecordUpgraded(upgrades);
            var balance = RecordBalance(wallet);

            Assert.That(upgrades.TryUpgrade(StatType.AttackSpeed), Is.False);
            Assert.That(upgrades.GetLevel(StatType.AttackSpeed), Is.EqualTo(10));
            Assert.That(wallet.Balance, Is.EqualTo(575));
            Assert.That(upgraded, Is.Empty);
            Assert.That(balance, Is.Empty);
            Assert.That(upgrades.CanUpgrade(StatType.AttackSpeed), Is.False);
            Assert.That(upgrades.GetCost(StatType.AttackSpeed), Is.EqualTo(70));
        }

        [Test]
        public void TryUpgrade_NotEnoughGold_FailsWithoutChange()
        {
            var wallet = new Wallet(19);
            var upgrades = Create(wallet);
            var upgraded = RecordUpgraded(upgrades);
            var balance = RecordBalance(wallet);

            Assert.That(upgrades.TryUpgrade(StatType.Attack), Is.False);
            Assert.That(upgrades.GetLevel(StatType.Attack), Is.EqualTo(0));
            Assert.That(wallet.Balance, Is.EqualTo(19));
            Assert.That(upgraded, Is.Empty);
            Assert.That(balance, Is.Empty);
        }

        [Test]
        public void TryUpgrade_GoldEqualsCost_SucceedsAndNotifiesOnce()
        {
            var wallet = new Wallet(20);
            var upgrades = Create(wallet);
            var upgraded = RecordUpgraded(upgrades);
            var balance = RecordBalance(wallet);

            Assert.That(upgrades.TryUpgrade(StatType.Attack), Is.True);
            Assert.That(upgrades.GetLevel(StatType.Attack), Is.EqualTo(1));
            Assert.That(wallet.Balance, Is.EqualTo(0));
            Assert.That(balance.Count, Is.EqualTo(1));
            Assert.That(upgraded.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryUpgrade_RepeatedUntilGoldRunsOut_EachCallJudgedSeparately()
        {
            var wallet = new Wallet(50);
            var upgrades = Create(wallet);

            var results = new[]
            {
                upgrades.TryUpgrade(StatType.Attack),
                upgrades.TryUpgrade(StatType.Attack),
                upgrades.TryUpgrade(StatType.Attack),
            };

            Assert.That(results, Is.EqualTo(new[] { true, true, false }));
            Assert.That(upgrades.GetLevel(StatType.Attack), Is.EqualTo(2));
            Assert.That(upgrades.Attack, Is.EqualTo(14));
            Assert.That(wallet.Balance, Is.EqualTo(5));
        }

        [Test]
        public void TryUpgrade_Health_UpgradedCarriesStatAndNewValuesAreVisible()
        {
            var upgrades = Create(new Wallet(1000));
            var received = new List<StatType>();
            var level = -1;
            long maxHealth = -1;
            long cost = -1;
            upgrades.Upgraded += stat =>
            {
                received.Add(stat);
                level = upgrades.GetLevel(StatType.Health);
                maxHealth = upgrades.MaxHealth;
                cost = upgrades.GetCost(StatType.Health);
            };

            upgrades.TryUpgrade(StatType.Health);

            Assert.That(received, Is.EqualTo(new[] { StatType.Health }));
            Assert.That(level, Is.EqualTo(1));
            Assert.That(maxHealth, Is.EqualTo(120));
            Assert.That(cost, Is.EqualTo(25));
        }

        [Test]
        public void TryUpgrade_Attack_BalanceChangedBeforeLevelRisesAndBeforeUpgraded()
        {
            var wallet = new Wallet(1000);
            var upgrades = Create(wallet);
            var order = new List<string>();
            var levelAtBalanceChanged = -1;
            wallet.BalanceChanged += _ =>
            {
                order.Add("BalanceChanged");
                levelAtBalanceChanged = upgrades.GetLevel(StatType.Attack);
            };
            upgrades.Upgraded += _ => order.Add("Upgraded");

            upgrades.TryUpgrade(StatType.Attack);

            Assert.That(order, Is.EqualTo(new[] { "BalanceChanged", "Upgraded" }));
            Assert.That(levelAtBalanceChanged, Is.EqualTo(0));
        }

        [Test]
        public void TryUpgrade_NoSubscribers_DoesNotThrow()
        {
            var upgrades = Create(new Wallet(1000));

            Assert.DoesNotThrow(() => upgrades.TryUpgrade(StatType.Attack));
            Assert.DoesNotThrow(() => upgrades.SetLocked(true));
        }

        // 확인

        [Test]
        public void CanUpgrade_GoldEqualsCost_TrueForAllWithoutChange()
        {
            AssertCanUpgradeWithoutChange(20, true);
        }

        [Test]
        public void CanUpgrade_GoldBelowCost_FalseForAllWithoutChange()
        {
            AssertCanUpgradeWithoutChange(19, false);
        }

        private static void AssertCanUpgradeWithoutChange(long gold, bool expected)
        {
            var wallet = new Wallet(gold);
            var upgrades = Create(wallet);
            var upgraded = RecordUpgraded(upgrades);
            var balance = RecordBalance(wallet);

            foreach (var stat in AllStats)
            {
                Assert.That(upgrades.CanUpgrade(stat), Is.EqualTo(expected), stat.ToString());
            }

            Assert.That(wallet.Balance, Is.EqualTo(gold));
            foreach (var stat in AllStats)
            {
                Assert.That(upgrades.GetLevel(stat), Is.EqualTo(0), stat.ToString());
            }

            Assert.That(upgraded, Is.Empty);
            Assert.That(balance, Is.Empty);
        }

        // 잠금

        [Test]
        public void SetLocked_TrueTwice_NotifiesOnce()
        {
            var upgrades = Create(new Wallet());
            var locked = RecordLocked(upgrades);

            upgrades.SetLocked(true);
            Assert.That(upgrades.Locked, Is.True);
            Assert.That(locked, Is.EqualTo(new[] { true }));

            upgrades.SetLocked(true);
            Assert.That(locked, Is.EqualTo(new[] { true }));
        }

        [Test]
        public void TryUpgrade_WhileLocked_FailsWithoutChange()
        {
            var wallet = new Wallet(1000);
            var upgrades = Create(wallet);
            upgrades.SetLocked(true);
            var upgraded = RecordUpgraded(upgrades);

            Assert.That(upgrades.CanUpgrade(StatType.Attack), Is.False);
            Assert.That(upgrades.TryUpgrade(StatType.Attack), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(1000));
            Assert.That(upgrades.GetLevel(StatType.Attack), Is.EqualTo(0));
            Assert.That(upgraded, Is.Empty);
        }

        [Test]
        public void SetLocked_FalseAfterTrue_NotifiesAndUpgradeWorksAgain()
        {
            var upgrades = Create(new Wallet(1000));
            upgrades.SetLocked(true);
            var locked = RecordLocked(upgrades);

            upgrades.SetLocked(false);

            Assert.That(locked, Is.EqualTo(new[] { false }));
            Assert.That(upgrades.Locked, Is.False);
            Assert.That(upgrades.TryUpgrade(StatType.Attack), Is.True);
        }

        [Test]
        public void SetLocked_FalseWhenAlreadyUnlocked_DoesNotNotify()
        {
            var upgrades = Create(new Wallet());
            var locked = RecordLocked(upgrades);

            upgrades.SetLocked(false);

            Assert.That(locked, Is.Empty);
        }

        // 생성자: 단계 되살리기

        [Test]
        public void New_WithLevels_RestoresLevelsWithoutSpending()
        {
            var wallet = new Wallet(1000);
            var balance = RecordBalance(wallet);

            var upgrades = new StatUpgrades(wallet, 10, 100, 1.0, 3, 2, 4);

            Assert.That(upgrades.GetLevel(StatType.Attack), Is.EqualTo(3));
            Assert.That(upgrades.GetLevel(StatType.Health), Is.EqualTo(2));
            Assert.That(upgrades.GetLevel(StatType.AttackSpeed), Is.EqualTo(4));
            Assert.That(upgrades.Attack, Is.EqualTo(16));
            Assert.That(upgrades.MaxHealth, Is.EqualTo(140));
            Assert.That(upgrades.AttackInterval, Is.EqualTo(0.8).Within(Tolerance));
            Assert.That(upgrades.GetCost(StatType.Attack), Is.EqualTo(35));
            Assert.That(upgrades.GetCost(StatType.Health), Is.EqualTo(30));
            Assert.That(upgrades.GetCost(StatType.AttackSpeed), Is.EqualTo(40));
            Assert.That(wallet.Balance, Is.EqualTo(1000));
            Assert.That(balance, Is.Empty);
        }

        [Test]
        public void New_NegativeLevels_ClampedToZeroWithoutException()
        {
            StatUpgrades upgrades = null;
            Assert.DoesNotThrow(() => upgrades = new StatUpgrades(new Wallet(), 10, 100, 1.0, -1, -5, -2));

            foreach (var stat in AllStats)
            {
                Assert.That(upgrades.GetLevel(stat), Is.EqualTo(0), stat.ToString());
            }
        }

        [Test]
        public void New_AttackSpeedLevelAboveMax_ClampedToTenWithoutException()
        {
            StatUpgrades upgrades = null;
            Assert.DoesNotThrow(() => upgrades = new StatUpgrades(new Wallet(), 10, 100, 1.0, 0, 0, 11));

            Assert.That(upgrades.GetLevel(StatType.AttackSpeed), Is.EqualTo(10));
            Assert.That(upgrades.AttackInterval, Is.EqualTo(0.5).Within(Tolerance));
            Assert.That(upgrades.IsMaxLevel(StatType.AttackSpeed), Is.True);
        }

        [Test]
        public void New_AttackLevelIntMax_NoOverflowAndCannotUpgrade()
        {
            var wallet = new Wallet(long.MaxValue);
            var upgrades = new StatUpgrades(wallet, 10, 100, 1.0, int.MaxValue, 0, 0);

            Assert.That(upgrades.IsMaxLevel(StatType.Attack), Is.True);
            Assert.That(upgrades.Attack, Is.EqualTo(4294967304L));
            Assert.That(upgrades.GetCost(StatType.Attack), Is.EqualTo(10737418255L));
            Assert.That(upgrades.TryUpgrade(StatType.Attack), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void New_OtherBaseValues_FinalStatsUseBaseValues()
        {
            var upgrades = new StatUpgrades(new Wallet(), 50, 300, 2.0, 1, 1, 10);

            Assert.That(upgrades.Attack, Is.EqualTo(52));
            Assert.That(upgrades.MaxHealth, Is.EqualTo(320));
            Assert.That(upgrades.AttackInterval, Is.EqualTo(1.5).Within(Tolerance));
        }

        [Test]
        public void AttackInterval_SmallBase_ClampedToMinimum()
        {
            var level2 = new StatUpgrades(new Wallet(), 10, 100, 0.7, 0, 0, 2);
            var level10 = new StatUpgrades(new Wallet(), 10, 100, 0.7, 0, 0, 10);

            Assert.That(level2.AttackInterval, Is.EqualTo(0.6).Within(Tolerance));
            Assert.That(level10.AttackInterval, Is.EqualTo(0.5).Within(Tolerance));
        }

        // 생성자: 잘못된 인자

        [Test]
        public void New_NullWallet_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new StatUpgrades(null, 10, 100, 1.0));
        }

        [Test]
        public void New_ZeroBaseAttack_ThrowsOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatUpgrades(new Wallet(), 0, 100, 1.0));
        }

        [Test]
        public void New_ZeroBaseMaxHealth_ThrowsOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatUpgrades(new Wallet(), 10, 0, 1.0));
        }

        [TestCase(0.49)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void New_InvalidBaseAttackInterval_ThrowsOutOfRange(double interval)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatUpgrades(new Wallet(), 10, 100, interval));
        }

        // 정의되지 않은 StatType

        [Test]
        public void UndefinedStat_EveryMethod_ThrowsOutOfRangeWithoutChange()
        {
            var wallet = new Wallet(1000);
            var upgrades = Create(wallet);

            Assert.Throws<ArgumentOutOfRangeException>(() => upgrades.GetLevel(Undefined));
            Assert.Throws<ArgumentOutOfRangeException>(() => upgrades.GetMaxLevel(Undefined));
            Assert.Throws<ArgumentOutOfRangeException>(() => upgrades.GetCost(Undefined));
            Assert.Throws<ArgumentOutOfRangeException>(() => upgrades.IsMaxLevel(Undefined));
            Assert.Throws<ArgumentOutOfRangeException>(() => upgrades.CanUpgrade(Undefined));
            Assert.Throws<ArgumentOutOfRangeException>(() => upgrades.TryUpgrade(Undefined));

            Assert.That(wallet.Balance, Is.EqualTo(1000));
            foreach (var stat in AllStats)
            {
                Assert.That(upgrades.GetLevel(stat), Is.EqualTo(0), stat.ToString());
            }
        }

        [Test]
        public void UndefinedStat_WhileLocked_StillThrowsOutOfRange()
        {
            var upgrades = Create(new Wallet(1000));
            upgrades.SetLocked(true);

            Assert.Throws<ArgumentOutOfRangeException>(() => upgrades.CanUpgrade(Undefined));
            Assert.Throws<ArgumentOutOfRangeException>(() => upgrades.TryUpgrade(Undefined));
        }
    }
}
