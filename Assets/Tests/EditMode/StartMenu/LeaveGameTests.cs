using System;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Save;
using PlumSword.Core.Stage;
using PlumSword.Core.StartMenu;
using PlumSword.Core.StatUpgrade;
using PlumSword.Core.Tests.Save;

namespace PlumSword.Core.Tests.StartMenu
{
    public class LeaveGameTests
    {
        private Wallet wallet;
        private FakeSaveStore store;
        private AutoSave autoSave;

        private LeaveGame Create(bool enabled = true)
        {
            wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 10, 100, 1.0);
            var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new Random(0), 1, 1);
            store = new FakeSaveStore();
            autoSave = new AutoSave(store, wallet, upgrades, stage, enabled);
            return new LeaveGame(autoSave);
        }

        [Test]
        public void Constructor_NullAutoSave_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new LeaveGame(null));
        }

        [Test]
        public void Constructor_ValidAutoSave_DoesNotWriteOrFail()
        {
            var leave = Create();
            Assert.That(leave.HasFailed, Is.False);
            Assert.That(store.WriteCount, Is.Zero);
        }

        [Test]
        public void TryLeave_ChangedWallet_SavesCurrentData()
        {
            var leave = Create();
            wallet.Add(10);
            Assert.That(leave.TryLeave(100), Is.True);
            Assert.That(leave.HasFailed, Is.False);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            var result = SaveCodec.Decode(store.LastText);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.Data.Gold, Is.EqualTo(10));
            Assert.That(result.Data.SavedAtUtcTicks, Is.EqualTo(100));
        }

        [Test]
        public void TryLeave_FailureAndImmediateRetries_TrackEachResult()
        {
            var leave = Create();
            store.Succeeds = false;
            Assert.That(leave.TryLeave(100), Is.False);
            Assert.That(leave.HasFailed, Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            Assert.That(autoSave.HasFailed, Is.True);
            Assert.That(leave.TryLeave(200), Is.False);
            Assert.That(leave.HasFailed, Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(2));
            store.Succeeds = true;
            Assert.That(leave.TryLeave(300), Is.True);
            Assert.That(leave.HasFailed, Is.False);
            Assert.That(store.WriteCount, Is.EqualTo(3));
            var result = SaveCodec.Decode(store.LastText);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.Data.SavedAtUtcTicks, Is.EqualTo(300));
        }

        [Test]
        public void TryLeave_DisabledAutoSave_FailsWithoutWriting()
        {
            var leave = Create(enabled: false);
            Assert.That(leave.TryLeave(100), Is.False);
            Assert.That(leave.HasFailed, Is.True);
            Assert.That(store.WriteCount, Is.Zero);
        }
    }
}
