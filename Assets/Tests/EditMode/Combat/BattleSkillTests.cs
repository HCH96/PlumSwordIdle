using System;
using System.Collections.Generic;
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
            // 053에서는 스킬 발동이 아직 구현되지 않아 기존 이름을 유지한다.
            Assert.AreEqual(enemy.MaxHealth - 1, enemy.Health);
            Assert.AreEqual(5.0, battle.SkillCooldown);
            battle.Reset();
            Assert.AreEqual(0.0, battle.SkillCooldown);
            Assert.IsTrue(battle.IsSkillReady);
        }

        private static void Steps(Battle battle, int count)
        {
            for (int i = 0; i < count; i++) battle.Step();
        }

        private static Battle SkillBattle(long attack = 10, long health = 100)
        {
            return new Battle(new StatUpgrades(new Wallet(1000), attack, health), null, true);
        }

        [Test]
        public void SkillAttack_Boss_CooldownsAndSubsequentTurns()
        {
            var battle = SkillBattle();
            var events = new List<string>();
            battle.EnemyDamaged += e => events.Add("damage");
            battle.SkillUsed += () => events.Add("skill");
            Enemy boss = battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            battle.Step();
            Assert.AreEqual(99, boss.Health);
            Assert.AreEqual(90, battle.DiscipleHealth);
            CollectionAssert.AreEqual(new[] { "damage", "skill" }, events);
            Assert.AreEqual(5.0, battle.SkillCooldown, 1e-6);
            Assert.IsFalse(battle.IsSkillReady);
            Assert.AreEqual(1.0, battle.DiscipleAttackCooldown, 1e-6);
            Steps(battle, 50);
            Assert.AreEqual(80, boss.Health);
            Assert.AreEqual(1, events.FindAll(x => x == "skill").Count);
            Assert.AreEqual(4.0, battle.SkillCooldown, 1e-6);
            Steps(battle, 199);
            Assert.AreEqual(40, boss.Health);
            Assert.IsFalse(battle.IsSkillReady);
            Steps(battle, 14);
            Assert.AreEqual(39, boss.Health);
            Assert.AreEqual(2, events.FindAll(x => x == "skill").Count);
        }

        [Test]
        public void SkillAttack_AffectsAllEnemiesInRange_InOrder()
        {
            var battle = SkillBattle();
            var indices = new List<int>();
            int used = 0;
            battle.EnemyDamaged += e => indices.Add(e.Index);
            battle.SkillUsed += () => used++;
            Enemy first = battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Enemy second = battle.AddEnemy(EnemyKind.Boss, 1, 1.9);
            Enemy third = battle.AddEnemy(EnemyKind.Boss, 1, 2.5);
            battle.Step();
            Assert.AreEqual(29, first.Health);
            Assert.AreEqual(99, second.Health);
            Assert.AreEqual(100, third.Health);
            CollectionAssert.AreEqual(new[] { 0, 1 }, indices);
            Assert.AreEqual(1, used);
        }

        [Test]
        public void SkillReady_OutsideAttackRange_WaitsForWindUp()
        {
            var battle = SkillBattle();
            int used = 0;
            battle.SkillUsed += () => used++;
            Enemy boss = battle.AddEnemy(EnemyKind.Boss, 2, 1.5);
            battle.Step();
            Assert.AreEqual(130, boss.Health);
            Assert.AreEqual(0, used);
            Assert.IsTrue(battle.IsSkillReady);
            Steps(battle, 3);
            Assert.AreEqual(0.5, battle.DisciplePosition, 1e-6);
            Steps(battle, 20);
            Assert.AreEqual(130, boss.Health);
            battle.Step();
            Assert.AreEqual(129, boss.Health);
            Assert.AreEqual(1, used);
        }

        [Test]
        public void SkillCooldown_ExpiresDuringMovement_WaitsForNextAttack()
        {
            var battle = SkillBattle();
            int used = 0;
            battle.SkillUsed += () => used++;
            Enemy normal = battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Enemy boss = battle.AddEnemy(EnemyKind.Boss, 2, 8.0);
            battle.Step();
            Assert.AreEqual(29, normal.Health);
            Steps(battle, 50);
            Assert.AreEqual(2, battle.Enemies.Count);
            Steps(battle, 55);
            Assert.AreEqual(5.4432, battle.DisciplePosition, 1e-6);
            Steps(battle, 20);
            Assert.AreEqual(130, boss.Health);
            battle.Step();
            Assert.AreEqual(130, boss.Health);
            Steps(battle, 124);
            Assert.IsTrue(battle.IsSkillReady);
            Assert.AreEqual(100, boss.Health);
            Steps(battle, 38);
            Assert.AreEqual(100, boss.Health);
            battle.Step();
            Assert.AreEqual(99, boss.Health);
            Assert.AreEqual(2, used);
        }

        [Test]
        public void DisabledSkill_UsesOnlyBasicAttacks()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(1000), 10, 100));
            int used = 0;
            battle.SkillUsed += () => used++;
            Enemy boss = battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            Steps(battle, 251);
            Assert.AreEqual(40, boss.Health);
            Assert.AreEqual(0, used);
            Assert.AreEqual(0, battle.SkillCooldown);
        }

        [Test]
        public void SkillAttack_SimultaneousDefeat_NotifiesInOrder()
        {
            var battle = SkillBattle(200, 5);
            var events = new List<string>();
            bool defeatedAtEnemyEvent = false;
            battle.EnemyDamaged += e => events.Add("damage");
            battle.SkillUsed += () => events.Add("skill");
            battle.EnemyDefeated += e => { events.Add("enemy"); defeatedAtEnemyEvent = battle.IsDiscipleDefeated; };
            battle.DiscipleDefeated += () => events.Add("disciple");
            battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            battle.Step();
            CollectionAssert.AreEqual(new[] { "damage", "skill", "enemy", "disciple" }, events);
            Assert.IsTrue(defeatedAtEnemyEvent);
        }

        [Test]
        public void InnerPowerUpgrade_PreservesCooldown_ChangesNextSkillDamage()
        {
            var upgrades = new StatUpgrades(new Wallet(1000), 10, 100);
            var battle = new Battle(upgrades, null, true);
            Enemy boss = battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            battle.Step();
            Assert.IsTrue(upgrades.TryUpgrade(StatType.InnerPower));
            Assert.AreEqual(5.0, battle.SkillCooldown, 1e-6);
            Steps(battle, 309);
            Assert.AreEqual(19, boss.Health);
        }

        [Test]
        public void Reset_AfterSkill_EnablesImmediateSkillAgain()
        {
            var battle = SkillBattle();
            battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            battle.Step();
            battle.Reset();
            Assert.AreEqual(0, battle.SkillCooldown);
            Assert.IsTrue(battle.IsSkillReady);
            Enemy boss = battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            battle.Step();
            Assert.AreEqual(99, boss.Health);
        }

        [Test]
        public void SkillCooldown_PausedOrDefeated_DoesNotAdvance()
        {
            var paused = SkillBattle();
            paused.AddEnemy(EnemyKind.Boss, 1, 1.0);
            paused.Step();
            paused.SetPaused(true);
            Steps(paused, 10);
            paused.Tick(0.1);
            Assert.AreEqual(5.0, paused.SkillCooldown, 1e-6);
            var defeated = SkillBattle(10, 5);
            defeated.AddEnemy(EnemyKind.Normal, 1, 0.8);
            defeated.Step();
            Assert.IsTrue(defeated.IsDiscipleDefeated);
            Assert.AreEqual(5.0, defeated.SkillCooldown, 1e-6);
            Steps(defeated, 10);
            Assert.AreEqual(5.0, defeated.SkillCooldown, 1e-6);
        }

        [Test]
        public void SkillBattle_NoEnemies_MovesAndRemainsReady()
        {
            var battle = SkillBattle();
            Steps(battle, 50);
            Assert.AreEqual(0, battle.SkillCooldown);
            Assert.IsTrue(battle.IsSkillReady);
            Assert.AreEqual(6.48, battle.DisciplePosition, 1e-6);
        }

        [Test]
        public void SkillUsed_ObservesDamageBeforeCooldown()
        {
            var battle = SkillBattle();
            Enemy boss = battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            long health = -1;
            double cooldown = -1;
            double damageCooldown = -1;
            battle.EnemyDamaged += e => damageCooldown = battle.SkillCooldown;
            battle.SkillUsed += () => { health = boss.Health; cooldown = battle.SkillCooldown; };
            battle.Step();
            Assert.AreEqual(99, health);
            Assert.AreEqual(0, cooldown);
            Assert.AreEqual(0, damageCooldown);
        }

        [Test]
        public void EnemyDamaged_ResetDuringSkill_StopsNotificationsAndAttack()
        {
            var battle = SkillBattle();
            int used = 0, damaged = 0;
            battle.SkillUsed += () => used++;
            battle.DiscipleDamaged += () => damaged++;
            battle.EnemyDamaged += e => battle.Reset();
            battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            battle.Step();
            Assert.AreEqual(0, used);
            Assert.AreEqual(0, damaged);
            Assert.AreEqual(0, battle.SkillCooldown);
            Assert.AreEqual(0, battle.Enemies.Count);
        }

        [Test]
        public void SkillUsed_Reset_StopsRemainingStep()
        {
            var battle = SkillBattle();
            int resets = 0, damage = 0;
            battle.ResetCompleted += () => resets++;
            battle.DiscipleDamaged += () => damage++;
            battle.SkillUsed += () => battle.Reset();
            battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            Assert.DoesNotThrow(() => battle.Step());
            Assert.AreEqual(0, battle.SkillCooldown);
            Assert.AreEqual(0, battle.DiscipleAttackCooldown);
            Assert.AreEqual(100, battle.DiscipleHealth);
            Assert.AreEqual(0, battle.Enemies.Count);
            Assert.AreEqual(1, resets);
            Assert.AreEqual(0, damage);
        }
    }
}
