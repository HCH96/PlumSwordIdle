using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Combat
{
    public class BattleSkillHitTests
    {
        private static void Steps(Battle battle, int count)
        {
            for (int i = 0; i < count; i++) battle.Step();
        }

        private static Battle CreateBattle(long attack = 10)
        {
            return new Battle(new StatUpgrades(new Wallet(1000), attack, 100), null, true);
        }

        [Test]
        public void HitDamage_DistributesTotalAndRejectsInvalidInputs()
        {
            for (int i = 0; i < 12; i++) Assert.AreEqual(1, Battle.CalculateSkillHitDamage(20, i));
            Assert.AreEqual(8, Battle.CalculateSkillHitDamage(20, 12));
            Assert.AreEqual(2, Battle.CalculateSkillHitDamage(26, 0));
            Assert.AreEqual(2, Battle.CalculateSkillHitDamage(26, 12));
            Assert.AreEqual(0, Battle.CalculateSkillHitDamage(12, 0));
            Assert.AreEqual(12, Battle.CalculateSkillHitDamage(12, 12));
            Assert.Throws<ArgumentOutOfRangeException>(() => Battle.CalculateSkillHitDamage(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Battle.CalculateSkillHitDamage(20, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Battle.CalculateSkillHitDamage(20, 13));
        }

        [Test]
        public void HitSchedule_ExposesExpectedSteps()
        {
            CollectionAssert.AreEqual(new[] { 0, 3, 5, 9, 13, 19, 23, 28, 36, 38, 42, 44, 46 }, Battle.SkillHitSteps);
            Assert.AreEqual(13, Battle.SkillHitCount);
            Assert.AreEqual(63, Battle.SkillActiveSteps);
        }

        [Test]
        public void Skill_ScheduledHitsAndEnd_MatchSteps()
        {
            var battle = CreateBattle();
            var boss = battle.AddEnemy(EnemyKind.Boss, 2, 1.0);
            battle.Step(); Assert.AreEqual(129, boss.Health);
            Steps(battle, 2); Assert.AreEqual(129, boss.Health);
            battle.Step(); Assert.AreEqual(128, boss.Health);
            Steps(battle, 6); Assert.AreEqual(126, boss.Health);
            Steps(battle, 37); Assert.AreEqual(110, boss.Health);
            Steps(battle, 16); Assert.AreEqual(110, boss.Health); Assert.IsTrue(battle.IsSkillActive);
            battle.Step(); Assert.AreEqual(100, boss.Health); Assert.IsFalse(battle.IsSkillActive);
        }

        [Test]
        public void DisabledSkill_NeverActivates()
        {
            Assert.IsFalse(CreateBattle().IsSkillActive);
            var battle = new Battle(new StatUpgrades(new Wallet(1000), 10, 100));
            battle.AddEnemy(EnemyKind.Boss, 2, 1.0);
            for (int i = 0; i < 64; i++) { battle.Step(); Assert.IsFalse(battle.IsSkillActive); }
        }

        [Test]
        public void Skill_UsesCurrentPositionsForEachHit()
        {
            var battle = CreateBattle();
            var boss = battle.AddEnemy(EnemyKind.Boss, 2, 1.0);
            var normal = battle.AddEnemy(EnemyKind.Normal, 1, 2.6);
            Steps(battle, 47);
            Assert.AreEqual(110, boss.Health);
            Assert.AreEqual(18, normal.Health);
        }

        [Test]
        public void Skill_EmptyFieldRemainsStationaryUntilEnd()
        {
            var battle = CreateBattle(200);
            int damageEvents = 0;
            battle.EnemyDamaged += e => damageEvents++;
            battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            battle.Step(); Assert.AreEqual(0, battle.Enemies.Count);
            Steps(battle, 62);
            Assert.AreEqual(0, battle.DisciplePosition);
            Assert.AreEqual(1, damageEvents);
            battle.Step(); Assert.AreEqual(0.1296, battle.DisciplePosition, 1e-6);
        }

        [Test]
        public void DamageNotifications_ExposeSkillState()
        {
            var battle = CreateBattle();
            var states = new List<bool>();
            battle.EnemyDamaged += e => states.Add(battle.IsSkillActive);
            battle.AddEnemy(EnemyKind.Boss, 2, 1.0);
            Steps(battle, 47);
            Assert.AreEqual(13, states.Count);
            Assert.IsTrue(states.TrueForAll(x => x));
            Steps(battle, 17);
            Assert.AreEqual(14, states.Count);
            Assert.IsFalse(states[13]);
        }

        [Test]
        public void PausedSkill_DoesNotAdvance()
        {
            var battle = CreateBattle();
            var boss = battle.AddEnemy(EnemyKind.Boss, 2, 1.0);
            battle.Step(); battle.SetPaused(true); Steps(battle, 10);
            Assert.AreEqual(129, boss.Health);
            Assert.IsTrue(battle.IsSkillActive);
            battle.SetPaused(false); Steps(battle, 3);
            Assert.AreEqual(128, boss.Health);
        }

        [Test]
        public void Reset_DiscardsActiveSkill()
        {
            var battle = CreateBattle();
            battle.AddEnemy(EnemyKind.Boss, 2, 1.0);
            Steps(battle, 10); battle.Reset();
            Assert.IsFalse(battle.IsSkillActive);
            Steps(battle, 50);
            Assert.IsFalse(battle.IsSkillActive);
        }

        [Test]
        public void UpgradeDuringSkill_DoesNotChangeStoredDamage()
        {
            var upgrades = new StatUpgrades(new Wallet(1000), 10, 100);
            var battle = new Battle(upgrades, null, true);
            var boss = battle.AddEnemy(EnemyKind.Boss, 2, 1.0);
            battle.Step(); Assert.IsTrue(upgrades.TryUpgrade(StatType.Attack));
            Steps(battle, 46); Assert.AreEqual(110, boss.Health);
        }
    }
}
