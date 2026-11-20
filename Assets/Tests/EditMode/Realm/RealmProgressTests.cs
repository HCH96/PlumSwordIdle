using System;
using NUnit.Framework;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;

namespace PlumSword.Core.Tests.Realm
{
    public class RealmProgressTests
    {
        [Test]
        public void Kills_AdvanceAtEachThresholdWithoutSpendingGold()
        {
            var wallet = new Wallet(1234);
            var progress = new RealmProgress(wallet);
            int breakthroughs = 0, changes = 0, balanceChanges = 0;
            progress.BrokeThrough += _ => breakthroughs++;
            progress.ProgressChanged += () => changes++;
            wallet.BalanceChanged += _ => balanceChanges++;
            int[] thresholds = { 30, 50, 70, 100 };
            for (int level = 0; level < thresholds.Length; level++)
            {
                Assert.That(progress.Current, Is.EqualTo((RealmLevel)level));
                Assert.That(progress.KillsRequired, Is.EqualTo(thresholds[level]));
                for (int i = 0; i < thresholds[level] - 1; i++) progress.RecordKill();
                Assert.That(progress.Current, Is.EqualTo((RealmLevel)level));
                Assert.That(progress.Kills, Is.EqualTo(thresholds[level] - 1));
                progress.RecordKill();
                Assert.That(progress.Current, Is.EqualTo((RealmLevel)(level + 1)));
                Assert.That(progress.Kills, Is.Zero);
            }
            Assert.That(progress.IsMax, Is.True);
            Assert.That(progress.BonusPercent, Is.EqualTo(40));
            Assert.That(breakthroughs, Is.EqualTo(4));
            Assert.That(changes, Is.EqualTo(250));
            Assert.That(wallet.Balance, Is.EqualTo(1234));
            Assert.That(balanceChanges, Is.Zero);
            progress.RecordKill();
            Assert.That(changes, Is.EqualTo(250));
        }

        [Test]
        public void Constructor_RestoresAndClampsProgress()
        {
            var wallet = new Wallet();
            Assert.That(new RealmProgress(wallet, -1, -5).Current, Is.EqualTo(RealmLevel.ThirdRate));
            Assert.That(new RealmProgress(wallet, 0, -5).Kills, Is.Zero);
            Assert.That(new RealmProgress(wallet, 1, 49).Kills, Is.EqualTo(49));
            Assert.That(new RealmProgress(wallet, 1, 500).Kills, Is.EqualTo(49));
            Assert.That(new RealmProgress(wallet, 9, 20).Current, Is.EqualTo(RealmLevel.Transcendent));
            Assert.That(new RealmProgress(wallet, 9, 20).Kills, Is.Zero);
            Assert.Throws<ArgumentNullException>(() => new RealmProgress(null));
        }

        [Test]
        public void Breakthrough_EventObservesNewState()
        {
            var progress = new RealmProgress(new Wallet(), 3, 99);
            int events = 0;
            progress.BrokeThrough += level =>
            {
                events++;
                Assert.That(level, Is.EqualTo(RealmLevel.Transcendent));
                Assert.That(progress.Current, Is.EqualTo(level));
                Assert.That(progress.Kills, Is.Zero);
                Assert.That(progress.BonusPercent, Is.EqualTo(40));
            };
            progress.RecordKill();
            Assert.That(events, Is.EqualTo(1));
        }
    }
}
