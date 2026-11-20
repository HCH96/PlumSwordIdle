using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.StatUpgrade
{
    public class StatUpgradesRealmTests
    {
        [TestCase(1, 0, 0, 0, 11L, 110L)]
        [TestCase(1, 1, 1, 0, 13L, 132L)]
        [TestCase(3, 1, 0, 0, 15L, 130L)]
        [TestCase(4, 3, 2, 5, 22L, 196L)]
        [TestCase(4, 3, 2, 0, 22L, 196L)]
        [TestCase(0, 0, 0, 0, 10L, 100L)]
        [TestCase(4, int.MaxValue, int.MaxValue, 0, 6012954225L, 60129542256L)]
        public void FinalStats_RealmAndLevels_ApplyBonus(int realmLevel, int attack, int health, int innerPower, long expectedAttack, long expectedHealth)
        {
            var wallet = new Wallet();
            var realm = new RealmProgress(wallet, realmLevel);
            var upgrades = new StatUpgrades(wallet, 10, 100, attack, health, innerPower, realm);

            Assert.That(upgrades.Attack, Is.EqualTo(expectedAttack));
            Assert.That(upgrades.MaxHealth, Is.EqualTo(expectedHealth));
        }

        [Test]
        public void FinalStats_NullRealm_UsesZeroBonus()
        {
            StatUpgrades upgrades = null;
            Assert.DoesNotThrow(() => upgrades = new StatUpgrades(new Wallet(), 10, 100, 1, 1, 0, null));
            Assert.That(upgrades.Attack, Is.EqualTo(12));
            Assert.That(upgrades.MaxHealth, Is.EqualTo(120));
        }

        [Test]
        public void Breakthrough_ExistingUpgrades_ReadNewBonusWithoutNotification()
        {
            var wallet = new Wallet(200);
            var realm = new RealmProgress(wallet);
            var upgrades = new StatUpgrades(wallet, 10, 100, 1, 1, 0, realm);
            int notifications = 0;
            upgrades.Upgraded += _ => notifications++;

            Assert.That(upgrades.Attack, Is.EqualTo(12));
            Assert.That(upgrades.MaxHealth, Is.EqualTo(120));
            for (int i = 0; i < 30; i++) realm.RecordKill();
            Assert.That(realm.Current, Is.EqualTo(RealmLevel.SecondRate));
            Assert.That(upgrades.Attack, Is.EqualTo(13));
            Assert.That(upgrades.MaxHealth, Is.EqualTo(132));
            Assert.That(wallet.Balance, Is.EqualTo(200));
            Assert.That(notifications, Is.EqualTo(0));
        }

        [Test]
        public void HealthUpgrade_RealmBonus_NotificationSeesNewMaxHealth()
        {
            var wallet = new Wallet(1000);
            var upgrades = new StatUpgrades(wallet, 10, 100, 0, 0, 0, new RealmProgress(wallet, 1));
            long observedHealth = -1;
            upgrades.Upgraded += _ => observedHealth = upgrades.MaxHealth;

            Assert.That(upgrades.MaxHealth, Is.EqualTo(110));
            Assert.That(upgrades.TryUpgrade(StatType.Health), Is.True);
            Assert.That(observedHealth, Is.EqualTo(132));
            Assert.That(wallet.Balance, Is.EqualTo(980));
        }

        [Test]
        public void Battle_RealmBonus_HealthUpgradeAddsFinalMaxHealthDifference()
        {
            var wallet = new Wallet(1000);
            var upgrades = new StatUpgrades(wallet, 10, 100, 0, 0, 0, new RealmProgress(wallet, 1));
            var battle = new Battle(upgrades);

            Assert.That(battle.DiscipleHealth, Is.EqualTo(110));
            Assert.That(battle.DiscipleMaxHealth, Is.EqualTo(110));
            Assert.That(upgrades.TryUpgrade(StatType.Health), Is.True);
            Assert.That(battle.DiscipleHealth, Is.EqualTo(132));
            Assert.That(battle.DiscipleMaxHealth, Is.EqualTo(132));
            Assert.That(upgrades.TryUpgrade(StatType.Attack), Is.True);
            Assert.That(upgrades.TryUpgrade(StatType.InnerPower), Is.True);
            Assert.That(battle.DiscipleHealth, Is.EqualTo(132));
        }
    }
}
