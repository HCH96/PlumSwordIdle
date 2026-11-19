using System;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;
using PlumSword.Core.Save;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Save
{
    public class AutoSaveRealmTests
    {
        private Wallet wallet;
        private RealmProgress realm;
        private StatUpgrades upgrades;
        private FakeSaveStore store;

        private AutoSave Create(long gold)
        {
            wallet = new Wallet(gold);
            realm = new RealmProgress(wallet);
            upgrades = new StatUpgrades(wallet, 10, 100, 0, 0, 0, realm);
            var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new Random(0), 1, 5);
            store = new FakeSaveStore();
            return new AutoSave(store, wallet, upgrades, realm, stage);
        }

        [Test]
        public void Breakthrough_FlushStoresRealmAndGold()
        {
            var save = Create(1000);
            Assert.That(realm.TryBreakthrough(), Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(800));
            Assert.That(realm.Current, Is.EqualTo(RealmLevel.SecondRate));
            Assert.That(save.IsDirty, Is.True);
            Assert.That(store.WriteCount, Is.Zero);
            Assert.That(save.Flush(100), Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            var result = SaveCodec.Decode(store.LastText);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.Data.RealmLevel, Is.EqualTo(1));
            Assert.That(result.Data.Gold, Is.EqualTo(800));
            Assert.That(result.Data.SavedAtUtcTicks, Is.EqualTo(100));
        }

        [Test]
        public void InnerPowerUpgrade_FlushStoresLevelAndGold()
        {
            var save = Create(100);
            Assert.That(upgrades.TryUpgrade(StatType.InnerPower), Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(80));
            Assert.That(upgrades.GetLevel(StatType.InnerPower), Is.EqualTo(1));
            Assert.That(save.Flush(100), Is.True);
            Assert.That(store.WriteCount, Is.EqualTo(1));
            var result = SaveCodec.Decode(store.LastText);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.Data.InnerPowerLevel, Is.EqualTo(1));
            Assert.That(result.Data.Gold, Is.EqualTo(80));
        }

        [Test]
        public void Breakthrough_InsufficientGold_DoesNotWrite()
        {
            var save = Create(100);
            Assert.That(realm.TryBreakthrough(), Is.False);
            Assert.That(save.IsDirty, Is.False);
            Assert.That(save.Flush(100), Is.False);
            Assert.That(store.WriteCount, Is.Zero);
        }
    }
}
