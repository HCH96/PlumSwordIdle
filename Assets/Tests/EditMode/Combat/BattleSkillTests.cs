using System;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Combat
{
    public class BattleSkillTests
    {
        [TestCase(10L, 0, 20L)]
        [TestCase(10L, 1, 21L)]
        [TestCase(13L, 2, 28L)]
        [TestCase(11L, 0, 22L)]
        [TestCase(0L, 5, 0L)]
        [TestCase(10L, int.MaxValue, 2147483667L)]
        public void CalculateSkillDamage_ValidInputs_ReturnsTruncatedDamage(long attack, int level, long expected)
        {
            Assert.AreEqual(expected, Battle.CalculateSkillDamage(attack, level));
        }

        [Test]
        public void CalculateSkillDamage_NegativeInputs_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Battle.CalculateSkillDamage(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Battle.CalculateSkillDamage(10, -1));
        }

        [Test]
        public void New_EnabledSkill_IsReadyWithInitialDamage()
        {
            var upgrades = new StatUpgrades(new Wallet(1000), 10, 100);
            var battle = new Battle(upgrades, null, true);

            Assert.IsTrue(battle.SkillEnabled);
            Assert.AreEqual(0.0, battle.SkillCooldown);
            Assert.IsTrue(battle.IsSkillReady);
            Assert.AreEqual(20L, battle.SkillDamage);
        }

        [Test]
        public void New_DefaultSkill_IsDisabledButDamageIsAvailable()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(1000), 10, 100));

            Assert.IsFalse(battle.SkillEnabled);
            Assert.AreEqual(0.0, battle.SkillCooldown);
            Assert.IsFalse(battle.IsSkillReady);
            Assert.AreEqual(20L, battle.SkillDamage);
        }

        [Test]
        public void SkillDamage_AfterUpgrades_ReflectsCurrentLevels()
        {
            var upgrades = new StatUpgrades(new Wallet(1000), 10, 100);
            var battle = new Battle(upgrades, null, true);

            Assert.IsTrue(upgrades.TryUpgrade(StatType.InnerPower));
            Assert.AreEqual(21L, battle.SkillDamage);
            Assert.IsTrue(upgrades.TryUpgrade(StatType.Attack));
            Assert.AreEqual(25L, battle.SkillDamage);
        }

        [Test]
        public void SkillConstants_Values_MatchSpec()
        {
            Assert.AreEqual(2.0, Battle.SkillRange);
            Assert.AreEqual(5.0, Battle.SkillCooldownSeconds);
            Assert.AreEqual(200, Battle.SkillBasePercent);
            Assert.AreEqual(10, Battle.SkillPercentPerInnerLevel);
        }

        [Test]
        public void EnabledSkill_StepAndReset_DoNotActivateSkillYet()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(1000), 10, 100), null, true);
            Enemy enemy = battle.AddEnemy(EnemyKind.Boss, 1, 0.8);

            battle.Step();
            Assert.AreEqual(enemy.MaxHealth - 10, enemy.Health);
            Assert.AreEqual(0.0, battle.SkillCooldown);
            battle.Reset();
            Assert.AreEqual(0.0, battle.SkillCooldown);
            Assert.IsTrue(battle.IsSkillReady);
        }
    }
}
