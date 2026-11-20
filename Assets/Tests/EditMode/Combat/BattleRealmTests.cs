using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Combat
{
    public class BattleRealmTests
    {
        private Wallet _wallet;
        private RealmProgress _realm;
        private StatUpgrades _upgrades;
        private Battle _battle;

        [SetUp]
        public void SetUp()
        {
            _wallet = new Wallet(1000);
            _realm = new RealmProgress(_wallet);
            _upgrades = new StatUpgrades(_wallet, 10, 100, 0, 0, 0, _realm);
            _battle = new Battle(_upgrades, _realm);
        }

        private static void Breakthrough(RealmProgress realm)
        {
            var before = realm.Current;
            int needed = realm.KillsRequired - realm.Kills;
            for (int i = 0; i < needed; i++) realm.RecordKill();
            Assert.AreEqual((int)before + 1, (int)realm.Current);
        }

        private void TakeBossHit()
        {
            _battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            _battle.Step();
            Assert.AreEqual(90L, _battle.DiscipleHealth);
        }

        private void DefeatDisciple()
        {
            _upgrades = new StatUpgrades(_wallet, 10, 10, 0, 0, 0, _realm);
            _battle = new Battle(_upgrades, _realm);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            _battle.Step();
            Assert.IsTrue(_battle.IsDiscipleDefeated);
            Assert.AreEqual(0L, _battle.DiscipleHealth);
        }

        [Test]
        public void Breakthrough_AfterDamage_IncreasesCurrentHealthByMaxDifference()
        {
            TakeBossHit();
            Breakthrough(_realm);
            Assert.AreEqual(110L, _battle.DiscipleMaxHealth);
            Assert.AreEqual(100L, _battle.DiscipleHealth);
        }

        [Test]
        public void HealthUpgrade_AfterBreakthrough_UsesUpdatedMaxHealth()
        {
            TakeBossHit();
            Breakthrough(_realm);
            Assert.IsTrue(_upgrades.TryUpgrade(StatType.Health));
            Assert.AreEqual(132L, _battle.DiscipleMaxHealth);
            Assert.AreEqual(122L, _battle.DiscipleHealth);
        }

        [Test]
        public void Breakthrough_FourTimes_IncreasesHealthToFinalMaximum()
        {
            var wallet = new Wallet(10000);
            var realm = new RealmProgress(wallet);
            var upgrades = new StatUpgrades(wallet, 10, 100, 0, 0, 0, realm);
            var battle = new Battle(upgrades, realm);
            for (int i = 0; i < 4; i++)
            {
                Breakthrough(realm);
            }
            Assert.AreEqual(140L, battle.DiscipleMaxHealth);
            Assert.AreEqual(140L, battle.DiscipleHealth);
        }

        [Test]
        public void Breakthrough_WhileDefeated_LeavesCurrentHealthAtZero()
        {
            DefeatDisciple();
            Breakthrough(_realm);
            Assert.AreEqual(0L, _battle.DiscipleHealth);
            Assert.AreEqual(11L, _battle.DiscipleMaxHealth);
        }

        [Test]
        public void Reset_AfterDefeatAndBreakthrough_RestoresNewMaximum()
        {
            DefeatDisciple();
            Breakthrough(_realm);
            _battle.Reset();
            Assert.AreEqual(11L, _battle.DiscipleHealth);
        }

        [Test]
        public void Breakthrough_WithoutRealmSubscription_LeavesCurrentHealthUnchanged()
        {
            _battle = new Battle(_upgrades);
            TakeBossHit();
            Breakthrough(_realm);
            Assert.AreEqual(90L, _battle.DiscipleHealth);
            Assert.AreEqual(110L, _battle.DiscipleMaxHealth);
        }

        [Test]
        public void Breakthrough_WhilePaused_IncreasesHealthWithoutBattleNotifications()
        {
            int notifications = 0;
            _battle.EnemyAdded += _ => notifications++;
            _battle.EnemyDamaged += _ => notifications++;
            _battle.DiscipleDamaged += () => notifications++;
            _battle.EnemyDefeated += _ => notifications++;
            _battle.DiscipleDefeated += () => notifications++;
            _battle.ResetCompleted += () => notifications++;
            _battle.SetPaused(true);
            Breakthrough(_realm);
            Assert.AreEqual(110L, _battle.DiscipleHealth);
            Assert.AreEqual(0, notifications);
        }

        [Test]
        public void New_NullRealm_StartsWithFullHealth()
        {
            Battle battle = null;
            Assert.DoesNotThrow(() => battle = new Battle(_upgrades, null));
            Assert.AreEqual(100L, battle.DiscipleHealth);
        }
    }
}
