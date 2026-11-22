using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Combat
{
    public class BattleHoldPositionTests
    {
        private Battle _battle;

        [SetUp]
        public void SetUp()
        {
            _battle = new Battle(new StatUpgrades(new Wallet(1000), 10, 100));
        }

        private void Steps(int count)
        {
            for (int i = 0; i < count; i++) _battle.Step();
        }

        [Test]
        public void New_HoldPosition_IsFalse()
        {
            Assert.IsFalse(_battle.IsHoldingPosition);
        }

        [Test]
        public void Step_HoldingWithoutEnemies_DoesNotAdvance()
        {
            _battle.SetHoldPosition(true);
            Steps(10);
            Assert.AreEqual(0.0, _battle.DisciplePosition, 1e-6);
        }

        [Test]
        public void Step_AfterReleasingHold_Advances()
        {
            _battle.SetHoldPosition(true);
            Steps(10);
            _battle.SetHoldPosition(false);
            Steps(10);
            Assert.AreEqual(1.296, _battle.DisciplePosition, 1e-6);
        }

        [Test]
        public void Step_HoldingWithDistantBoss_DoesNotApproachOrAttack()
        {
            Enemy boss = _battle.AddEnemy(EnemyKind.Boss, 2, 5.0);
            _battle.SetHoldPosition(true);
            Steps(50);
            Assert.AreEqual(0.0, _battle.DisciplePosition, 1e-6);
            Assert.AreEqual(130L, boss.Health);
        }

        [Test]
        public void Step_HoldingWithBossInRange_DiscipleAttacks()
        {
            Enemy boss = _battle.AddEnemy(EnemyKind.Boss, 2, 1.0);
            _battle.SetHoldPosition(true);
            _battle.Step();
            Assert.AreEqual(120L, boss.Health);
        }

        [Test]
        public void Step_HoldingWithBossInRange_EnemyAttacks()
        {
            _battle.AddEnemy(EnemyKind.Boss, 2, 1.0);
            _battle.SetHoldPosition(true);
            _battle.Step();
            Assert.AreEqual(92L, _battle.DiscipleHealth);
        }

        [Test]
        public void Reset_Holding_ClearsHold()
        {
            _battle.SetHoldPosition(true);
            _battle.Reset();
            Assert.IsFalse(_battle.IsHoldingPosition);
        }

        [Test]
        public void SetHoldPosition_Paused_ChangesHold()
        {
            _battle.SetPaused(true);
            _battle.SetHoldPosition(true);
            Assert.IsTrue(_battle.IsHoldingPosition);
        }

        [Test]
        public void Step_Holding_EnemyApproachesAndNoWindUpIsAdded()
        {
            Enemy enemy = _battle.AddEnemy(EnemyKind.Normal, 1, 1.01);
            _battle.SetHoldPosition(true);
            _battle.Step();
            Assert.AreEqual(0.0, _battle.DisciplePosition, 1e-6);
            Assert.AreEqual(0.99, enemy.Position, 1e-6);
            Assert.AreEqual(20L, enemy.Health);
        }

        [Test]
        public void SetHoldPosition_Defeated_ChangesHold()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(1000), 10, 5));
            battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            battle.Step();
            Assert.IsTrue(battle.IsDiscipleDefeated);
            battle.SetHoldPosition(true);
            Assert.IsTrue(battle.IsHoldingPosition);
        }

        [Test]
        public void Step_SkillAndHold_BlockMovementIndependently()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(1000), 200, 100), null, true);
            battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            battle.Step();
            Assert.IsTrue(battle.IsSkillActive);
            battle.SetHoldPosition(true);
            battle.SetHoldPosition(false);
            battle.Step();
            Assert.AreEqual(0.0, battle.DisciplePosition, 1e-6);
            battle.SetHoldPosition(true);
            for (int i = 0; i < 62; i++) battle.Step();
            Assert.IsFalse(battle.IsSkillActive);
            Assert.AreEqual(0.0, battle.DisciplePosition, 1e-6);
            battle.SetHoldPosition(false);
            battle.Step();
            Assert.AreEqual(0.1296, battle.DisciplePosition, 1e-6);
        }
    }
}
