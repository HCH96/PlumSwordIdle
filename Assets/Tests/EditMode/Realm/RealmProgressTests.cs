using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;

namespace PlumSword.Core.Tests.Realm
{
    public class RealmProgressTests
    {
        [Test]
        public void NewProgress_HasInitialValues()
        {
            var progress = new RealmProgress(new Wallet());
            Assert.That(progress.Current, Is.EqualTo(RealmLevel.ThirdRate));
            Assert.That(progress.IsMax, Is.False);
            Assert.That(progress.BonusPercent, Is.EqualTo(0));
            Assert.That(progress.NextCost, Is.EqualTo(200));
            Assert.That(progress.Locked, Is.False);
        }

        [Test]
        public void FourBreakthroughs_ReachMaximumAndSpendAll()
        {
            var wallet = new Wallet(3700);
            var progress = new RealmProgress(wallet);
            foreach (var cost in new long[] { 200, 500, 1000, 2000 })
            {
                Assert.That(progress.NextCost, Is.EqualTo(cost));
                Assert.That(progress.TryBreakthrough(), Is.True);
            }
            Assert.That(progress.Current, Is.EqualTo(RealmLevel.Transcendent));
            Assert.That(progress.IsMax, Is.True);
            Assert.That(progress.BonusPercent, Is.EqualTo(40));
            Assert.That(progress.NextCost, Is.EqualTo(0));
            Assert.That(wallet.Balance, Is.EqualTo(0));
        }

        [Test]
        public void Breakthrough_WithEnoughGold_UpdatesValues()
        {
            var wallet = new Wallet(1000);
            var progress = new RealmProgress(wallet);
            Assert.That(progress.TryBreakthrough(), Is.True);
            Assert.That(progress.Current, Is.EqualTo(RealmLevel.SecondRate));
            Assert.That(progress.BonusPercent, Is.EqualTo(10));
            Assert.That(progress.NextCost, Is.EqualTo(500));
            Assert.That(wallet.Balance, Is.EqualTo(800));
        }

        [Test]
        public void Breakthrough_InsufficientGold_ChangesNothing()
        {
            var wallet = new Wallet(199);
            var progress = new RealmProgress(wallet);
            int balanceEvents = 0, realmEvents = 0;
            wallet.BalanceChanged += _ => balanceEvents++;
            progress.BrokeThrough += _ => realmEvents++;
            Assert.That(progress.TryBreakthrough(), Is.False);
            Assert.That(progress.Current, Is.EqualTo(RealmLevel.ThirdRate));
            Assert.That(wallet.Balance, Is.EqualTo(199));
            Assert.That(balanceEvents, Is.Zero);
            Assert.That(realmEvents, Is.Zero);
        }

        [Test]
        public void Breakthrough_ExactCost_Succeeds()
        {
            var wallet = new Wallet(200);
            var progress = new RealmProgress(wallet);
            Assert.That(progress.TryBreakthrough(), Is.True);
            Assert.That(progress.Current, Is.EqualTo(RealmLevel.SecondRate));
            Assert.That(wallet.Balance, Is.Zero);
        }

        [Test]
        public void Breakthrough_RepeatedCalls_UseCurrentCostAndBalance()
        {
            var wallet = new Wallet(1000);
            var progress = new RealmProgress(wallet);
            Assert.That(progress.TryBreakthrough(), Is.True);
            Assert.That(progress.TryBreakthrough(), Is.True);
            Assert.That(progress.TryBreakthrough(), Is.False);
            Assert.That(progress.Current, Is.EqualTo(RealmLevel.FirstRate));
            Assert.That(wallet.Balance, Is.EqualTo(300));
        }

        [Test]
        public void Maximum_CannotBreakthroughOrNotify()
        {
            var wallet = new Wallet(10000);
            var progress = new RealmProgress(wallet, 4);
            int balanceEvents = 0, realmEvents = 0;
            wallet.BalanceChanged += _ => balanceEvents++;
            progress.BrokeThrough += _ => realmEvents++;
            Assert.That(progress.CanBreakthrough(), Is.False);
            Assert.That(progress.TryBreakthrough(), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(10000));
            Assert.That(progress.Current, Is.EqualTo(RealmLevel.Transcendent));
            Assert.That(balanceEvents, Is.Zero);
            Assert.That(realmEvents, Is.Zero);
        }

        [Test]
        public void Breakthrough_Notifications_ExposeStateInOrder()
        {
            var wallet = new Wallet(1000);
            var progress = new RealmProgress(wallet);
            var events = new List<string>();
            wallet.BalanceChanged += _ =>
            {
                events.Add("balance");
                Assert.That(progress.Current, Is.EqualTo(RealmLevel.ThirdRate));
            };
            progress.BrokeThrough += level =>
            {
                events.Add("realm");
                Assert.That(level, Is.EqualTo(RealmLevel.SecondRate));
                Assert.That(progress.Current, Is.EqualTo(level));
                Assert.That(progress.BonusPercent, Is.EqualTo(10));
                Assert.That(progress.NextCost, Is.EqualTo(500));
                Assert.That(progress.IsMax, Is.False);
            };
            Assert.That(progress.TryBreakthrough(), Is.True);
            CollectionAssert.AreEqual(new[] { "balance", "realm" }, events);
        }

        [Test]
        public void Breakthrough_ToMaximum_NotificationExposesMaximumValues()
        {
            var progress = new RealmProgress(new Wallet(2000), 3);
            int calls = 0;
            progress.BrokeThrough += level =>
            {
                calls++;
                Assert.That(level, Is.EqualTo(RealmLevel.Transcendent));
                Assert.That(progress.Current, Is.EqualTo(level));
                Assert.That(progress.IsMax, Is.True);
                Assert.That(progress.BonusPercent, Is.EqualTo(40));
                Assert.That(progress.NextCost, Is.Zero);
            };
            Assert.That(progress.TryBreakthrough(), Is.True);
            Assert.That(calls, Is.EqualTo(1));
        }

        [TestCase(200, true)]
        [TestCase(199, false)]
        public void CanBreakthrough_ChecksBalanceWithoutChanges(int balance, bool expected)
        {
            var wallet = new Wallet(balance);
            var progress = new RealmProgress(wallet);
            int balanceEvents = 0, realmEvents = 0;
            wallet.BalanceChanged += _ => balanceEvents++;
            progress.BrokeThrough += _ => realmEvents++;
            Assert.That(progress.CanBreakthrough(), Is.EqualTo(expected));
            Assert.That(wallet.Balance, Is.EqualTo(balance));
            Assert.That(progress.Current, Is.EqualTo(RealmLevel.ThirdRate));
            Assert.That(balanceEvents, Is.Zero);
            Assert.That(realmEvents, Is.Zero);
        }

        [Test]
        public void Lock_ChangesOnlyNotifyAfterStateChanges()
        {
            var progress = new RealmProgress(new Wallet(1000));
            var events = new List<bool>();
            progress.LockedChanged += value =>
            {
                Assert.That(progress.Locked, Is.EqualTo(value));
                events.Add(value);
            };
            progress.SetLocked(false);
            Assert.That(events, Is.Empty);
            progress.SetLocked(true);
            Assert.That(progress.Locked, Is.True);
            progress.SetLocked(true);
            CollectionAssert.AreEqual(new[] { true }, events);
            progress.SetLocked(false);
            CollectionAssert.AreEqual(new[] { true, false }, events);
            Assert.That(progress.TryBreakthrough(), Is.True);
        }

        [Test]
        public void Locked_BlocksChecksAndBreakthroughWithoutChanges()
        {
            var wallet = new Wallet(1000);
            var progress = new RealmProgress(wallet);
            int realmEvents = 0, balanceEvents = 0;
            progress.BrokeThrough += _ => realmEvents++;
            wallet.BalanceChanged += _ => balanceEvents++;
            progress.SetLocked(true);
            Assert.That(progress.CanBreakthrough(), Is.False);
            Assert.That(progress.TryBreakthrough(), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(1000));
            Assert.That(progress.Current, Is.EqualTo(RealmLevel.ThirdRate));
            Assert.That(realmEvents, Is.Zero);
            Assert.That(balanceEvents, Is.Zero);
            progress.SetLocked(false);
            Assert.That(progress.TryBreakthrough(), Is.True);
        }

        [Test]
        public void Constructor_RestoresLevelWithoutSpendingOrNotifying()
        {
            var wallet = new Wallet(500);
            int balanceEvents = 0;
            wallet.BalanceChanged += _ => balanceEvents++;
            var progress = new RealmProgress(wallet, 2);
            Assert.That(progress.Current, Is.EqualTo(RealmLevel.FirstRate));
            Assert.That(progress.BonusPercent, Is.EqualTo(20));
            Assert.That(progress.NextCost, Is.EqualTo(1000));
            Assert.That(progress.Locked, Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(500));
            Assert.That(balanceEvents, Is.Zero);
        }

        [TestCase(-1, RealmLevel.ThirdRate)]
        [TestCase(7, RealmLevel.Transcendent)]
        public void Constructor_OutOfRangeLevel_Clamps(int level, RealmLevel expected)
        {
            Assert.That(new RealmProgress(new Wallet(), level).Current, Is.EqualTo(expected));
        }

        [Test]
        public void Constructor_NullWallet_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new RealmProgress(null));
        }
    }
}
