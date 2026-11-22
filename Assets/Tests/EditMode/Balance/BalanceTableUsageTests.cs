using System;
using NUnit.Framework;
using PlumSword.Core.Balance;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Balance
{
    public class BalanceTableUsageTests
    {
        [TearDown]
        public void TearDown() => BalanceTable.Current = BalanceTable.Default;

        [Test]
        public void GetCost_ChangedAttackCurve_UsesIndependentCurvesOnExistingObject()
        {
            var upgrades = new StatUpgrades(new Wallet(0), 10, 100, 3);
            var table = BalanceTable.Default;
            table.AttackCost = new GrowthCurve(100, 0, 2);
            table.InnerPowerCost = new GrowthCurve(7, 0, 1);
            BalanceTable.Current = table;
            Assert.AreEqual(800, upgrades.GetCost(StatType.Attack));
            Assert.AreEqual(20, upgrades.GetCost(StatType.Health));
            Assert.AreEqual(7, upgrades.GetCost(StatType.InnerPower));
        }

        [Test]
        public void Stats_ChangedPerLevel_UsesCurrentTableOnExistingObject()
        {
            var upgrades = new StatUpgrades(new Wallet(0), 10, 100, 2, 2);
            var table = BalanceTable.Default;
            table.AttackPerLevel = 5;
            table.HealthPerLevel = 50;
            BalanceTable.Current = table;
            Assert.AreEqual(20, upgrades.Attack);
            Assert.AreEqual(200, upgrades.MaxHealth);
        }

        [Test]
        public void CalculateSkillDamage_ChangedPercentages_UsesCurrentTable()
        {
            var table = BalanceTable.Default;
            table.SkillBasePercent = 300;
            table.SkillPercentPerInnerLevel = 20;
            BalanceTable.Current = table;
            Assert.AreEqual(34, Battle.CalculateSkillDamage(10, 2));
        }

        [Test]
        public void RealmRules_ChangedArrays_UsesCurrentTableAndPreservesBounds()
        {
            var table = BalanceTable.Default;
            table.RealmKillsRequired = new[] { 1, 2, 3, 4 };
            table.RealmBonusPercent = new[] { 0, 50, 60, 70, 80 };
            BalanceTable.Current = table;
            Assert.AreEqual(1, RealmRules.KillsRequired(RealmLevel.ThirdRate));
            Assert.AreEqual(50, RealmRules.BonusPercent(RealmLevel.SecondRate));
            Assert.AreEqual(0, RealmRules.KillsRequired(RealmLevel.Transcendent));
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmRules.KillsRequired((RealmLevel)5));
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmRules.BonusPercent((RealmLevel)(-1)));
        }

        [Test]
        public void EnemyIntervals_ChangedValues_UsesKindSpecificValues()
        {
            var table = BalanceTable.Default;
            table.NormalAttackInterval = 3.0;
            table.BossAttackInterval = 4.0;
            BalanceTable.Current = table;
            Assert.AreEqual(3.0, EnemyStats.AttackInterval(EnemyKind.Normal));
            Assert.AreEqual(4.0, EnemyStats.AttackInterval(EnemyKind.Boss));
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyStats.AttackInterval((EnemyKind)(-1)));
        }

        [Test]
        public void DiscipleInterval_ChangedTable_UpdatesExistingBattleAndAttackCooldown()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(0), 10, 100));
            var table = BalanceTable.Default;
            table.DiscipleAttackInterval = 2.0;
            BalanceTable.Current = table;
            Assert.AreEqual(2.0, battle.DiscipleAttackInterval);
            battle.AddEnemy(EnemyKind.Boss, 1, 0.8);
            battle.Step();
            Assert.AreEqual(2.0, battle.DiscipleAttackCooldown);
        }

        [Test]
        public void DiscipleWindUp_ChangedInterval_UsesCurrentTable()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(0), 10, 100));
            var table = BalanceTable.Default;
            table.DiscipleAttackInterval = 2.0;
            BalanceTable.Current = table;
            battle.AddEnemy(EnemyKind.Boss, 1, 1.05);
            battle.Step();
            Assert.That(battle.DiscipleAttackCooldown, Is.EqualTo(2.0 * Battle.DiscipleAttackWindUpRatio).Within(1e-9));
        }

        [Test]
        public void SkillCooldown_ChangedTable_UsesValueWhenSkillFires()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(1000), 10, 100), null, true);
            var table = BalanceTable.Default;
            table.SkillCooldownSeconds = 3.0;
            table.DiscipleAttackInterval = 2.0;
            BalanceTable.Current = table;
            battle.AddEnemy(EnemyKind.Boss, 1, 0.8);
            battle.Step();
            Assert.AreEqual(3.0, battle.SkillCooldown);
            Assert.AreEqual(2.0, battle.DiscipleAttackCooldown);
        }
    }
}
