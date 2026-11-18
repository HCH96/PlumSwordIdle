using System;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.OfflineReward;
using PlumSword.Core.Save;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;
using PlumSword.Core.Tests.Save;

namespace PlumSword.Core.Tests.OfflineReward
{
    public class OfflineRewardClaimTests
    {
        private const long T0 = 638000000000000000L;
        private Wallet wallet;
        private AutoSave autoSave;
        private FakeSaveStore store;
        private OfflineRewardResult reward;
        private OfflineRewardClaim claim;

        [SetUp]
        public void SetUp()
        {
            wallet = new Wallet(100);
            var upgrades = new StatUpgrades(wallet, 10, 100);
            var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new Random(0), 1, 1);
            store = new FakeSaveStore();
            autoSave = new AutoSave(store, wallet, upgrades, stage, enabled: false);
            reward = OfflineRewardCalculator.Calculate(T0, T0 + 6000000000L, 1);
            claim = new OfflineRewardClaim(wallet, autoSave, reward);
        }

        [Test]
        public void Constructor_NullDependencies_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new OfflineRewardClaim(null, autoSave, reward));
            Assert.Throws<ArgumentNullException>(() => new OfflineRewardClaim(wallet, null, reward));
            Assert.Throws<ArgumentNullException>(() => new OfflineRewardClaim(wallet, autoSave, null));
        }

        [Test]
        public void Constructor_NoReward_ThrowsArgumentException()
        {
            var empty = OfflineRewardCalculator.Calculate(T0, T0, 1);
            Assert.Throws<ArgumentException>(() => new OfflineRewardClaim(wallet, autoSave, empty));
        }

        [Test]
        public void Prepared_BeforeClaim_DoesNotPayOrSave()
        {
            Assert.That(claim.Reward, Is.SameAs(reward));
            Assert.That(claim.State, Is.EqualTo(OfflineRewardClaimState.Waiting));
            Assert.That(claim.HasPaid, Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(100));
            Assert.That(store.WriteCount, Is.Zero);
            Assert.That(autoSave.Enabled, Is.False);
            Assert.That(autoSave.SaveNow(T0 + 6500000000L), Is.False);
            Assert.That(store.WriteCount, Is.Zero);
            Assert.That(wallet.Balance, Is.EqualTo(100));
        }

        [Test]
        public void Claim_Succeeds_PaysOnceAndSavesTimestamp()
        {
            Assert.That(claim.TryClaim(T0 + 7000000000L), Is.True);
            Assert.That(claim.State, Is.EqualTo(OfflineRewardClaimState.Done));
            Assert.That(claim.HasPaid, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(700));
            Assert.That(autoSave.Enabled, Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            var data = ReadLast();
            Assert.That(data.Gold, Is.EqualTo(700));
            Assert.That(data.SavedAtUtcTicks, Is.EqualTo(T0 + 7000000000L));
            Assert.That(OfflineRewardCalculator.Calculate(data.SavedAtUtcTicks, data.SavedAtUtcTicks + 50000000L, 1).HasReward, Is.False);
            Assert.That(claim.TryClaim(T0 + 8000000000L), Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(700));
            Assert.That(store.WriteCount, Is.EqualTo(1));
        }

        [Test]
        public void Claim_TwoFailuresThenSuccess_RetriesOnlySave()
        {
            store.Succeeds = false;
            Assert.That(claim.TryClaim(T0 + 7000000000L), Is.False);
            Assert.That(claim.State, Is.EqualTo(OfflineRewardClaimState.SaveFailed));
            Assert.That(claim.HasPaid, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(700));
            Assert.That(store.WriteCount, Is.EqualTo(1));
            Assert.That(autoSave.HasFailed, Is.True);
            Assert.That(autoSave.IsDirty, Is.True);
            Assert.That(claim.TryClaim(T0 + 7500000000L), Is.False);
            Assert.That(claim.State, Is.EqualTo(OfflineRewardClaimState.SaveFailed));
            Assert.That(wallet.Balance, Is.EqualTo(700));
            Assert.That(store.WriteCount, Is.EqualTo(2));
            store.Succeeds = true;
            Assert.That(claim.TryClaim(T0 + 8000000000L), Is.True);
            Assert.That(claim.State, Is.EqualTo(OfflineRewardClaimState.Done));
            Assert.That(wallet.Balance, Is.EqualTo(700));
            Assert.That(store.WriteCount, Is.EqualTo(3));
            Assert.That(autoSave.HasFailed, Is.False);
            var data = ReadLast();
            Assert.That(data.Gold, Is.EqualTo(700));
            Assert.That(data.SavedAtUtcTicks, Is.EqualTo(T0 + 8000000000L));
        }

        private SaveData ReadLast()
        {
            var result = SaveCodec.Decode(store.LastText);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            return result.Data;
        }
    }
}
