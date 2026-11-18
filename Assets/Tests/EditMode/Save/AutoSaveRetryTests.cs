using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Save;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Save
{
    public class AutoSaveRetryTests
    {
        private Wallet wallet;
        private FakeSaveStore store;
        private AutoSave autoSave;
        private List<bool> changes;

        [SetUp]
        public void SetUp()
        {
            wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 10, 100);
            var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new Random(0));
            store = new FakeSaveStore();
            autoSave = new AutoSave(store, wallet, upgrades, stage);
            changes = new List<bool>();
            autoSave.FailedChanged += value =>
            {
                Assert.That(autoSave.HasFailed, Is.EqualTo(value));
                changes.Add(value);
            };
        }

        [Test]
        public void FailedFlush_WaitsAndRetriesWithCurrentTimestamp()
        {
            store.Succeeds = false;
            wallet.Add(10);
            Assert.That(autoSave.Flush(100), Is.False);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            Assert.That(autoSave.HasFailed, Is.True);
            Assert.That(autoSave.IsDirty, Is.True);
            CollectionAssert.AreEqual(new[] { true }, changes);

            store.Succeeds = true;
            Assert.That(autoSave.Flush(200), Is.False);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            autoSave.Tick(2.9);
            Assert.That(autoSave.Flush(250), Is.False);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            autoSave.Tick(0.1);
            Assert.That(autoSave.Flush(300), Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(2));
            Assert.That(autoSave.HasFailed, Is.False);
            Assert.That(autoSave.IsDirty, Is.False);
            Assert.That(SaveCodec.Decode(store.LastText).Data.SavedAtUtcTicks, Is.EqualTo(300));
            CollectionAssert.AreEqual(new[] { true, false }, changes);
        }

        [Test]
        public void RepeatedFailure_RestartsWaitWithoutDuplicateEvent()
        {
            store.Succeeds = false;
            wallet.Add(10);
            Assert.That(autoSave.Flush(100), Is.False);
            autoSave.Tick(3.0);
            Assert.That(autoSave.Flush(200), Is.False);
            Assert.That(store.WriteCount, Is.EqualTo(2));
            Assert.That(autoSave.HasFailed, Is.True);
            CollectionAssert.AreEqual(new[] { true }, changes);
            autoSave.Tick(2.9);
            Assert.That(autoSave.Flush(300), Is.False);
            Assert.That(store.WriteCount, Is.EqualTo(2));
        }

        [Test]
        public void ChangeDuringRetryWait_DoesNotWriteEarly()
        {
            store.Succeeds = false;
            wallet.Add(10);
            Assert.That(autoSave.Flush(100), Is.False);
            store.Succeeds = true;
            wallet.Add(5);
            autoSave.Tick(1.0);
            Assert.That(autoSave.Flush(400), Is.False);
            Assert.That(store.WriteCount, Is.EqualTo(1));
        }

        [Test]
        public void Flush_StoreThrows_ReportsFailureWithoutThrowing()
        {
            store.Throws = true;
            wallet.Add(10);
            Assert.That(autoSave.Flush(100), Is.False);
            Assert.That(autoSave.HasFailed, Is.True);
            Assert.That(autoSave.IsDirty, Is.True);
        }

        [Test]
        public void SaveNow_StoreThrows_ReportsFailureWithoutThrowing()
        {
            store.Throws = true;
            Assert.That(autoSave.SaveNow(100), Is.False);
            Assert.That(autoSave.HasFailed, Is.True);
        }

        [Test]
        public void SaveNow_DuringRetryWait_WritesAndClearsWait()
        {
            store.Succeeds = false;
            wallet.Add(10);
            Assert.That(autoSave.Flush(100), Is.False);
            store.Succeeds = true;
            Assert.That(autoSave.SaveNow(600), Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(2));
            Assert.That(autoSave.HasFailed, Is.False);
            Assert.That(autoSave.IsDirty, Is.False);
            Assert.That(SaveCodec.Decode(store.LastText).Data.SavedAtUtcTicks, Is.EqualTo(600));
            wallet.Add(1);
            Assert.That(autoSave.Flush(700), Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(3));
        }
    }
}
