using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Stage
{
    public class StageProgressFinalTests
    {
        private const double Tolerance = 1e-6;
        private Wallet _wallet;
        private Battle _battle;
        private StageProgress _stage;
        private List<int> _started;
        private List<int> _cleared;
        private List<int> _failed;

        private void Create(int start, long attack = 1000, long health = 10000, int seen = 5, Random random = null)
        {
            _wallet = new Wallet(0);
            var upgrades = new StatUpgrades(_wallet, attack, health);
            _battle = new Battle(upgrades);
            _stage = new StageProgress(_wallet, upgrades, _battle, random ?? new MinRandom(), start, seen);
            _started = new List<int>();
            _cleared = new List<int>();
            _failed = new List<int>();
            _stage.StageStarted += n => _started.Add(n);
            _stage.StageCleared += n => _cleared.Add(n);
            _stage.StageFailed += n => _failed.Add(n);
        }

        private void FightUntilTransition()
        {
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.Playing));
            for (int i = 0; i < 5000 && _stage.Phase == StagePhase.Playing; i++) _battle.Step();
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
        }

        [Test]
        public void Begin_FifthStage_PlacesOnlyFinalBossAndStartsSubstageIntro()
        {
            Create(5);
            _stage.Begin();
            Assert.That(_stage.SubstageNumber, Is.EqualTo(7));
            Assert.That(_stage.IsBossActive, Is.True);
            Assert.That(_battle.Enemies.Count, Is.EqualTo(1));
            var boss = _battle.Enemies[0];
            Assert.That(boss, Is.SameAs(_stage.Boss));
            Assert.That(boss.Kind, Is.EqualTo(EnemyKind.Boss));
            Assert.That(boss.MaxHealth, Is.EqualTo(220));
            Assert.That(boss.Attack, Is.EqualTo(26));
            Assert.That(boss.Position, Is.EqualTo(_battle.DisciplePosition + 24).Within(Tolerance));
            Assert.That(_stage.DefeatedNormalCount, Is.Zero);
            Assert.That(_stage.BarRatio, Is.EqualTo(1.0).Within(Tolerance));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(_started, Is.EqualTo(new[] { 5 }));
        }

        [Test]
        public void TenthStage_BossUsesTotalStageStatsAndReward()
        {
            Create(10);
            _stage.Begin();
            Assert.That(_battle.Enemies.Count, Is.EqualTo(1));
            Assert.That(_battle.Enemies[0].Kind, Is.EqualTo(EnemyKind.Boss));
            Assert.That(_battle.Enemies[0].MaxHealth, Is.EqualTo(370));
            Assert.That(_battle.Enemies[0].Attack, Is.EqualTo(46));
            FightUntilTransition();
            Assert.That(_wallet.Balance, Is.EqualTo(140));
        }

        [Test]
        public void FifthStage_BossDefeated_AdvancesToNextRound()
        {
            Create(5);
            _stage.Begin();
            FightUntilTransition();
            Assert.That(_wallet.Balance, Is.EqualTo(90));
            Assert.That(_cleared, Is.EqualTo(new[] { 5 }));
            _stage.Tick(1.0);
            Assert.That(_stage.StageNumber, Is.EqualTo(6));
            Assert.That(_stage.DisplayStageNumber, Is.EqualTo(1));
            Assert.That(_stage.SubstageNumber, Is.EqualTo(1));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
            foreach (var enemy in _battle.Enemies) Assert.That(enemy.Kind, Is.EqualTo(EnemyKind.Normal));
        }

        [Test]
        public void FifthStage_DiscipleDefeated_RetriesWithFreshBossWithoutThemeIntro()
        {
            Create(5, 1, 1);
            _stage.Begin();
            var oldBoss = _stage.Boss;
            FightUntilTransition();
            Assert.That(_failed, Is.EqualTo(new[] { 5 }));
            _stage.Tick(1.0);
            Assert.That(_stage.StageNumber, Is.EqualTo(5));
            Assert.That(_stage.SubstageNumber, Is.EqualTo(7));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(1));
            Assert.That(_battle.Enemies[0], Is.Not.SameAs(oldBoss));
            Assert.That(_battle.Enemies[0].Kind, Is.EqualTo(EnemyKind.Boss));
            Assert.That(_battle.Enemies[0].MaxHealth, Is.EqualTo(220));
            Assert.That(_battle.Enemies[0].Health, Is.EqualTo(220));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
        }

        [Test]
        public void FourthStage_SixNormalSubstages_ProgressToBoss()
        {
            Create(4);
            _stage.Begin();
            Assert.That(_stage.SubstageNumber, Is.EqualTo(1));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
            foreach (var enemy in _battle.Enemies) Assert.That(enemy.Kind, Is.EqualTo(EnemyKind.Normal));
            for (int n = 1; n <= 6; n++)
            {
                FightUntilTransition();
                _stage.Tick(1.0);
                if (n == 3) Assert.That(_stage.BarRatio, Is.EqualTo(0.5).Within(Tolerance));
            }
            Assert.That(_stage.SubstageNumber, Is.EqualTo(7));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(1));
            Assert.That(_battle.Enemies[0].Kind, Is.EqualTo(EnemyKind.Boss));
        }

        [Test]
        public void FifthStage_UnseenTheme_ShowsBothIntrosWithBossAlreadyPlaced()
        {
            Create(5, seen: 4);
            _stage.Begin();
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.Intro));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(1));
            Assert.That(_battle.Enemies[0].Kind, Is.EqualTo(EnemyKind.Boss));
            _stage.Tick(3.0);
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            _stage.Tick(1.2);
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.Playing));
        }

        [Test]
        public void Begin_FifthStage_DoesNotRollGroupRandom()
        {
            var random = new CountingRandom();
            Create(5, random: random);
            _stage.Begin();
            Assert.That(random.Calls, Is.Zero);
        }

        private sealed class CountingRandom : Random
        {
            public int Calls { get; private set; }
            public override int Next(int minValue, int maxValue)
            {
                Calls++;
                return minValue;
            }
        }
    }
}
