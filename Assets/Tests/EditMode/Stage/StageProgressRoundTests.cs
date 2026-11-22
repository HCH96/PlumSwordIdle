using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Stage
{
    public class StageProgressRoundTests
    {
        private Wallet _wallet;
        private StatUpgrades _upgrades;
        private Battle _battle;
        private StageProgress _stage;
        private List<int> _started;
        private List<int> _cleared;
        private List<int> _failed;

        private void Create(long attack, long health, int startStage)
        {
            _wallet = new Wallet(0);
            _upgrades = new StatUpgrades(_wallet, attack, health);
            _battle = new Battle(_upgrades);
            _stage = new StageProgress(_wallet, _upgrades, _battle, new MinRandom(), startStage);
            _started = new List<int>();
            _cleared = new List<int>();
            _failed = new List<int>();
            _stage.StageStarted += n => _started.Add(n);
            _stage.StageCleared += n => _cleared.Add(n);
            _stage.StageFailed += n => _failed.Add(n);
        }

        [TestCase(1, 1, 1, 1)]
        [TestCase(2, 2, 1, 2)]
        [TestCase(3, 3, 1, 3)]
        [TestCase(4, 4, 1, 4)]
        [TestCase(5, 5, 1, 5)]
        [TestCase(6, 6, 2, 1)]
        [TestCase(0, 1, 1, 1)]
        [TestCase(-3, 1, 1, 1)]
        public void BeforeBegin_StageNumbersReflectClampedStart(int start, int total, int round, int display)
        {
            Create(10, 100, start);
            Assert.That(_stage.StageNumber, Is.EqualTo(total));
            Assert.That(_stage.RoundNumber, Is.EqualTo(round));
            Assert.That(_stage.DisplayStageNumber, Is.EqualTo(display));
        }

        [Test]
        public void FifthStageCleared_AdvancesToSecondRoundAtCoveredTransition()
        {
            Create(1500, 100, 5);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            _battle.AddEnemy(EnemyKind.Boss, 5, 1.0);
            _battle.Step();
            Assert.That(_wallet.Balance, Is.EqualTo(90));
            Assert.That(_cleared, Is.EqualTo(new[] { 5 }));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(_stage.StageNumber, Is.EqualTo(5));

            _stage.Tick(0.5);
            Assert.That(_stage.StageNumber, Is.EqualTo(6));
            Assert.That(_stage.RoundNumber, Is.EqualTo(2));
            Assert.That(_stage.DisplayStageNumber, Is.EqualTo(1));
            Assert.That(_started, Is.EqualTo(new[] { 5, 6 }));
            Assert.That(_wallet.Balance, Is.EqualTo(90));
            Assert.That(_upgrades.GetLevel(StatType.Attack), Is.Zero);
            Assert.That(_upgrades.GetLevel(StatType.Health), Is.Zero);
        }

        [Test]
        public void SixthStageEnemies_UseTotalStageNumberForStats()
        {
            Create(10, 30, 6);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
            foreach (Enemy enemy in _battle.Enemies)
            {
                Assert.That(enemy.Kind, Is.EqualTo(EnemyKind.Normal));
                Assert.That(enemy.MaxHealth, Is.EqualTo(80));
                Assert.That(enemy.Attack, Is.EqualTo(15));
            }
            Enemy boss = _battle.AddEnemy(EnemyKind.Boss, 6, 1.0);
            Assert.That(boss.MaxHealth, Is.EqualTo(250));
            Assert.That(boss.Attack, Is.EqualTo(24));
        }

        [Test]
        public void SixthStageRewards_UseTotalStageNumber()
        {
            Assert.That(StageRewards.NormalGold(6), Is.EqualTo(20));
            Assert.That(StageRewards.BossGold(6), Is.EqualTo(100));
        }

        [Test]
        public void SixthStageFailed_RetriesSameRoundAndResetsBattle()
        {
            Create(10, 24, 6);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            Enemy boss = _battle.AddEnemy(EnemyKind.Boss, 6, 1.0);
            _battle.Step();
            Assert.That(boss.Health, Is.EqualTo(240));
            Assert.That(boss.IsDead, Is.False);
            Assert.That(_failed, Is.EqualTo(new[] { 6 }));
            _stage.Tick(0.5);
            Assert.That(_stage.StageNumber, Is.EqualTo(6));
            Assert.That(_stage.RoundNumber, Is.EqualTo(2));
            Assert.That(_stage.DisplayStageNumber, Is.EqualTo(1));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
            foreach (Enemy enemy in _battle.Enemies)
                Assert.That(enemy.Kind, Is.EqualTo(EnemyKind.Normal));
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(24));
            Assert.That(_stage.DefeatedNormalCount, Is.Zero);
            Assert.That(_stage.Boss, Is.Null);
        }

        [Test]
        public void RestoredTotalStage_ReconstructsRoundAndEnemies()
        {
            Create(10, 100, 6);
            int savedStage = _stage.StageNumber;
            Assert.That(savedStage, Is.EqualTo(6));
            Create(10, 100, savedStage);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            Assert.That(_stage.StageNumber, Is.EqualTo(6));
            Assert.That(_stage.RoundNumber, Is.EqualTo(2));
            Assert.That(_stage.DisplayStageNumber, Is.EqualTo(1));
            Assert.That(_stage.DefeatedNormalCount, Is.Zero);
            Assert.That(_stage.Boss, Is.Null);
            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
            foreach (Enemy enemy in _battle.Enemies)
            {
                Assert.That(enemy.Kind, Is.EqualTo(EnemyKind.Normal));
                Assert.That(enemy.MaxHealth, Is.EqualTo(80));
            }
        }

        [Test]
        public void MaxStageCleared_RoundNumbersRemainValidWithoutOverflow()
        {
            Create(100000000000, 100000000000, int.MaxValue);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            _battle.AddEnemy(EnemyKind.Boss, int.MaxValue, 1.0);
            _battle.Step();
            Assert.That(_wallet.Balance, Is.EqualTo(21474836510L));
            Assert.That(_cleared, Is.EqualTo(new[] { int.MaxValue }));
            Assert.DoesNotThrow(() => _stage.Tick(0.5));
            Assert.That(_stage.StageNumber, Is.EqualTo(int.MaxValue));
            Assert.That(_stage.RoundNumber, Is.EqualTo(429496730));
            Assert.That(_stage.DisplayStageNumber, Is.EqualTo(2));
        }
    }
}
