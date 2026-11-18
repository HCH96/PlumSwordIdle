using System;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Save;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Save
{
    public class AutoSaveTests
    {
        private Wallet wallet;
        private StatUpgrades upgrades;
        private StageProgress stage;
        private FakeSaveStore store;

        private AutoSave Create(long gold = 0, bool enabled = true, int seenIntroStage = 5)
        {
            wallet = new Wallet(gold);
            upgrades = new StatUpgrades(wallet, 10, 100);
            stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new Random(0), 1, seenIntroStage);
            store = new FakeSaveStore();
            return new AutoSave(store, wallet, upgrades, stage, enabled);
        }

        private SaveData ReadLast()
        {
            var result = SaveCodec.Decode(store.LastText);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            return result.Data;
        }

        [Test]
        public void Constructor_NullDependency_ThrowsArgumentNull()
        {
            Create();
            Assert.Throws<ArgumentNullException>(() => new AutoSave(null, wallet, upgrades, stage));
            Assert.Throws<ArgumentNullException>(() => new AutoSave(store, null, upgrades, stage));
            Assert.Throws<ArgumentNullException>(() => new AutoSave(store, wallet, null, stage));
            Assert.Throws<ArgumentNullException>(() => new AutoSave(store, wallet, upgrades, null));
        }

        [Test]
        public void Constructor_AndCleanFlush_DoNotWrite()
        {
            var save = Create();
            Assert.That(save.Enabled, Is.True);
            Assert.That(save.IsDirty, Is.False);
            Assert.That(save.HasFailed, Is.False);
            Assert.That(store.WriteCount, Is.Zero);
            Assert.That(save.Flush(100), Is.False);
            Assert.That(store.WriteCount, Is.Zero);
        }

        [Test]
        public void BalanceChange_FlushWritesOnceAndClearsDirty()
        {
            var save = Create();
            wallet.Add(10);
            Assert.That(save.IsDirty, Is.True);
            Assert.That(store.WriteCount, Is.Zero);
            Assert.That(save.Flush(100), Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            Assert.That(save.IsDirty, Is.False);
            var data = ReadLast();
            Assert.That(data.Gold, Is.EqualTo(10));
            Assert.That(data.SavedAtUtcTicks, Is.EqualTo(100));
            Assert.That(save.Flush(200), Is.False);
            Assert.That(store.WriteCount, Is.EqualTo(1));
        }

        [Test]
        public void UpgradeAndBalanceChange_FlushCombinesChanges()
        {
            var save = Create(100);
            Assert.That(upgrades.TryUpgrade(StatType.Attack), Is.True);
            wallet.Add(5);
            Assert.That(save.Flush(100), Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            var data = ReadLast();
            Assert.That(data.Gold, Is.EqualTo(85));
            Assert.That(data.AttackLevel, Is.EqualTo(1));
        }

        [Test]
        public void UnchangedOperations_DoNotMarkDirty()
        {
            var save = Create();
            wallet.Add(0);
            Assert.That(upgrades.TryUpgrade(StatType.Attack), Is.False);
            Assert.That(wallet.TrySpend(5), Is.False);
            Assert.That(save.IsDirty, Is.False);
        }

        [Test]
        public void StageBegin_FlushStoresIntroProgress()
        {
            var save = Create(seenIntroStage: 0);
            stage.Begin();
            Assert.That(save.IsDirty, Is.True);
            Assert.That(save.Flush(100), Is.True);
            var data = ReadLast();
            Assert.That(data.StageNumber, Is.EqualTo(1));
            Assert.That(data.SeenIntroStage, Is.EqualTo(1));
        }

        [Test]
        public void SaveNow_WithoutChanges_WritesTimestamp()
        {
            var save = Create();
            Assert.That(save.SaveNow(500), Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            Assert.That(ReadLast().SavedAtUtcTicks, Is.EqualTo(500));
        }

        [Test]
        public void Flush_StoreFails_LeavesDirty()
        {
            var save = Create();
            store.Succeeds = false;
            wallet.Add(10);
            Assert.That(save.Flush(100), Is.False);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            Assert.That(save.IsDirty, Is.True);
        }

        [Test]
        public void Disabled_AndEnabled_TracksOnlyLaterChanges()
        {
            var save = Create(enabled: false);
            wallet.Add(10);
            Assert.That(save.IsDirty, Is.False);
            Assert.That(save.Flush(100), Is.False);
            Assert.That(save.SaveNow(100), Is.False);
            Assert.That(store.WriteCount, Is.Zero);
            save.Enable();
            Assert.That(save.Enabled, Is.True);
            Assert.That(save.IsDirty, Is.False);
            wallet.Add(5);
            Assert.That(save.Flush(200), Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            Assert.That(ReadLast().Gold, Is.EqualTo(15));
            save.Enable();
            Assert.That(save.Enabled, Is.True);
            Assert.That(save.IsDirty, Is.False);
            Assert.That(store.WriteCount, Is.EqualTo(1));
        }

        [TestCase(-0.1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Tick_InvalidTime_ThrowsArgumentOutOfRange(double value)
        {
            var save = Create(enabled: false);
            Assert.Throws<ArgumentOutOfRangeException>(() => save.Tick(value));
        }

        [Test]
        public void Tick_WithoutWait_DoesNothing()
        {
            var save = Create();
            save.Tick(0);
            save.Tick(1.0);
            Assert.That(save.IsDirty, Is.False);
            Assert.That(save.HasFailed, Is.False);
            Assert.That(store.WriteCount, Is.Zero);
        }
    }
}
