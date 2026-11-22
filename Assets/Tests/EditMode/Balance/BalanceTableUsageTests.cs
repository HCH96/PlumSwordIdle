using System;
using NUnit.Framework;
using PlumSword.Core.Balance;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;
using PlumSword.Core.StatUpgrade;
using PlumSword.Core.Stage;
using PlumSword.Core.Tests.Stage;

namespace PlumSword.Core.Tests.Balance
{
    public class BalanceTableUsageTests
    {
        [TearDown]
        public void TearDown() => BalanceTable.Current = BalanceTable.Default;

        [Test]
        public void EnemyHealth_UsesSubstageAndLegacyFirstSubstage()
        {
            var table = BalanceTable.Default;
            table.SubstageStep = 1;
            table.NormalHealth = new GrowthCurve(30, 0, 2);
            BalanceTable.Current = table;
            Assert.AreEqual(120, EnemyStats.MaxHealth(EnemyKind.Normal, 1, 3));
            Assert.AreEqual(30, EnemyStats.MaxHealth(EnemyKind.Normal, 1));
        }

        [Test]
        public void AddEnemy_UsesSubstageForHealthAndAttack()
        {
            var table = BalanceTable.Default;
            table.SubstageStep = 1;
            table.NormalHealth = new GrowthCurve(30, 0, 2);
            table.NormalAttack = new GrowthCurve(5, 0, 2);
            BalanceTable.Current = table;
            var battle = new Battle(new StatUpgrades(new Wallet(0), 10, 100));
            var enemy = battle.AddEnemy(EnemyKind.Normal, 1, 1.0, 3);
            Assert.AreEqual(120, enemy.MaxHealth);
            Assert.AreEqual(20, enemy.Attack);
        }

        [Test]
        public void FinalBoss_UsesFixedValuesOnEveryFifthStage()
        {
            var table = BalanceTable.Default;
            table.FinalBossHealth = 5000;
            table.FinalBossAttack = 300;
            BalanceTable.Current = table;
            Assert.AreEqual(5000, EnemyStats.MaxHealth(EnemyKind.Boss, 5));
            Assert.AreEqual(5000, EnemyStats.MaxHealth(EnemyKind.Boss, 10));
            Assert.AreEqual(300, EnemyStats.Attack(EnemyKind.Boss, 5));
            Assert.AreEqual(190, EnemyStats.MaxHealth(EnemyKind.Boss, 4));
            Assert.AreEqual(70, EnemyStats.MaxHealth(EnemyKind.Normal, 5));
        }

        [Test]
        public void Rewards_UseSubstageAndLegacyFirstSubstage()
        {
            var table = BalanceTable.Default;
            table.SubstageStep = 1;
            table.NormalGold = new GrowthCurve(10, 0, 2);
            table.BossGold = new GrowthCurve(50, 0, 2);
            BalanceTable.Current = table;
            Assert.AreEqual(40, StageRewards.NormalGold(1, 3));
            Assert.AreEqual(10, StageRewards.NormalGold(1));
            Assert.AreEqual(200, StageRewards.BossGold(1, 3));
            Assert.AreEqual(50, StageRewards.BossGold(1));
        }

        [Test]
        public void InvalidSubstage_ThrowsWithoutAddingOrNotifying()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(0), 10, 100));
            int added = 0;
            battle.EnemyAdded += enemy => added++;
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyStats.MaxHealth(EnemyKind.Normal, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyStats.Attack(EnemyKind.Boss, 1, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageRewards.NormalGold(1, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageRewards.BossGold(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => battle.AddEnemy(EnemyKind.Normal, 1, 1.0, 0));
            Assert.AreEqual(0, battle.Enemies.Count);
            Assert.AreEqual(0, added);
        }

        [TestCase(2, 3, false, 40)]
        [TestCase(1, 2, true, 60)]
        public void StageProgress_SpawnsWithCurrentSubstage(int stage, int target, bool scaled, long health)
        {
            var table = BalanceTable.Default;
            if (scaled)
            {
                table.SubstageStep = 1;
                table.NormalHealth = new GrowthCurve(30, 0, 2);
            }
            BalanceTable.Current = table;
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 100000, 100000);
            var battle = new Battle(upgrades);
            var progress = new StageProgress(wallet, upgrades, battle, new MinRandom(), stage);
            progress.Begin();
            for (int i = 0; i < 20000 && progress.SubstageNumber < target; i++)
            {
                progress.Tick(Battle.StepSeconds);
                battle.Step();
            }
            Assert.AreEqual(target, progress.SubstageNumber);
            Assert.AreEqual(EnemyKind.Normal, battle.Enemies[0].Kind);
            Assert.AreEqual(health, battle.Enemies[0].MaxHealth);
        }

        [TestCase(1, 2, 20)]
        [TestCase(5, 7, 51200)]
        public void StageProgress_AwardsCurrentSubstageGold(int stage, int target, long expectedReward)
        {
            var table = BalanceTable.Default;
            table.SubstageStep = 1;
            table.NormalGold = new GrowthCurve(10, 0, 2);
            table.BossGold = new GrowthCurve(50, 0, 2);
            BalanceTable.Current = table;
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 100000, 100000);
            var battle = new Battle(upgrades);
            var progress = new StageProgress(wallet, upgrades, battle, new MinRandom(), stage);
            progress.Begin();
            for (int i = 0; i < 20000 && progress.SubstageNumber < target; i++)
            {
                progress.Tick(Battle.StepSeconds);
                battle.Step();
            }
            Assert.AreEqual(target, progress.SubstageNumber);
            long before = wallet.Balance;
            for (int i = 0; i < 20000 && wallet.Balance == before; i++)
            {
                progress.Tick(Battle.StepSeconds);
                battle.Step();
            }
            Assert.AreEqual(expectedReward, wallet.Balance - before);
        }

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
