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
    public class SaveDataTests
    {
        [Test]
        public void CurrentVersion_IsOne() => Assert.That(SaveData.CurrentVersion, Is.EqualTo(2));

        [Test]
        public void Constructor_StoresAllValues()
        {
            var data = new SaveData(1234, 3, 2, 4, 2, 7, 5, 638000000000000000L);
            Assert.That(data.Gold, Is.EqualTo(1234));
            Assert.That(data.AttackLevel, Is.EqualTo(3));
            Assert.That(data.HealthLevel, Is.EqualTo(2));
            Assert.That(data.InnerPowerLevel, Is.EqualTo(4));
            Assert.That(data.RealmLevel, Is.EqualTo(2));
            Assert.That(data.StageNumber, Is.EqualTo(7));
            Assert.That(data.SeenIntroStage, Is.EqualTo(5));
            Assert.That(data.SavedAtUtcTicks, Is.EqualTo(638000000000000000L));
        }

        [Test]
        public void Constructor_OutOfRangeValues_ArePreserved()
        {
            var data = new SaveData(-5, 0, 0, -3, 7, 0, 9, 0);
            Assert.That(data.Gold, Is.EqualTo(-5));
            Assert.That(data.InnerPowerLevel, Is.EqualTo(-3));
            Assert.That(data.RealmLevel, Is.EqualTo(7));
            Assert.That(data.StageNumber, Is.EqualTo(0));
            Assert.That(data.SeenIntroStage, Is.EqualTo(9));
        }

        [Test]
        public void Restore_OutOfRangeValues_FeatureConstructorsClamp()
        {
            var data = new SaveData(-5, 0, 0, -3, 7, 0, 9, 0);
            var wallet = new Wallet(data.Gold);
            var realm = new RealmProgress(wallet, data.RealmLevel);
            var upgrades = new StatUpgrades(wallet, 10, 100, data.AttackLevel, data.HealthLevel, data.InnerPowerLevel, realm);
            var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new Random(0), data.StageNumber, data.SeenIntroStage);
            Assert.That(wallet.Balance, Is.EqualTo(0));
            Assert.That(realm.Current, Is.EqualTo(RealmLevel.Transcendent));
            Assert.That(upgrades.GetLevel(StatType.InnerPower), Is.Zero);
            Assert.That(stage.StageNumber, Is.EqualTo(1));
            Assert.That(stage.SeenIntroStage, Is.EqualTo(5));
        }

        [Test]
        public void Restore_RealmAndUpgradeLevels_AppliesRealmBonus()
        {
            var data = new SaveData(1234, 3, 2, 4, 2, 1, 5, 100);
            var wallet = new Wallet(data.Gold);
            var realm = new RealmProgress(wallet, data.RealmLevel);
            var upgrades = new StatUpgrades(wallet, 10, 100, data.AttackLevel, data.HealthLevel, data.InnerPowerLevel, realm);

            Assert.That(wallet.Balance, Is.EqualTo(1234));
            Assert.That(realm.Current, Is.EqualTo(RealmLevel.FirstRate));
            Assert.That(upgrades.GetLevel(StatType.Attack), Is.EqualTo(3));
            Assert.That(upgrades.GetLevel(StatType.Health), Is.EqualTo(2));
            Assert.That(upgrades.GetLevel(StatType.InnerPower), Is.EqualTo(4));
            Assert.That(upgrades.Attack, Is.EqualTo(19));
            Assert.That(upgrades.MaxHealth, Is.EqualTo(168));
        }

        [Test]
        public void Capture_BeforeBegin_CopiesValuesWithoutChangesOrEvents()
        {
            var wallet = new Wallet(500);
            var realm = new RealmProgress(wallet, 2);
            var upgrades = new StatUpgrades(wallet, 10, 100, 2, 1, 3, realm);
            var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new Random(0), 4, 3);
            int events = 0;
            wallet.BalanceChanged += _ => events++;
            upgrades.Upgraded += _ => events++;
            realm.BrokeThrough += _ => events++;
            stage.StageStarted += _ => events++;
            var data = SaveData.Capture(wallet, upgrades, realm, stage, 100);
            Assert.That(data.Gold, Is.EqualTo(500));
            Assert.That(data.AttackLevel, Is.EqualTo(2));
            Assert.That(data.HealthLevel, Is.EqualTo(1));
            Assert.That(data.InnerPowerLevel, Is.EqualTo(3));
            Assert.That(data.RealmLevel, Is.EqualTo(2));
            Assert.That(data.StageNumber, Is.EqualTo(4));
            Assert.That(data.SeenIntroStage, Is.EqualTo(3));
            Assert.That(data.SavedAtUtcTicks, Is.EqualTo(100));
            Assert.That(wallet.Balance, Is.EqualTo(500));
            Assert.That(upgrades.GetLevel(StatType.Attack), Is.EqualTo(2));
            Assert.That(upgrades.GetLevel(StatType.Health), Is.EqualTo(1));
            Assert.That(upgrades.GetLevel(StatType.InnerPower), Is.EqualTo(3));
            Assert.That(realm.Current, Is.EqualTo(RealmLevel.FirstRate));
            Assert.That(stage.StageNumber, Is.EqualTo(4));
            Assert.That(stage.SeenIntroStage, Is.EqualTo(3));
            Assert.That(stage.HasBegun, Is.False);
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void Capture_NullDependencies_ThrowsArgumentNull()
        {
            var wallet = new Wallet();
            var upgrades = new StatUpgrades(wallet, 10, 100);
            var realm = new RealmProgress(wallet);
            var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new Random(0));
            Assert.Throws<ArgumentNullException>(() => SaveData.Capture(null, upgrades, realm, stage, 0));
            Assert.Throws<ArgumentNullException>(() => SaveData.Capture(wallet, null, realm, stage, 0));
            Assert.Throws<ArgumentNullException>(() => SaveData.Capture(wallet, upgrades, null, stage, 0));
            Assert.Throws<ArgumentNullException>(() => SaveData.Capture(wallet, upgrades, realm, null, 0));
        }
    }
}
