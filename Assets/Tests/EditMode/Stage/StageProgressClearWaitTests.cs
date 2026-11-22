using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Stage
{
    public class StageProgressClearWaitTests
    {
        private Wallet _wallet;
        private StatUpgrades _upgrades;
        private Battle _battle;
        private StageProgress _stage;

        [SetUp]
        public void SetUp()
        {
            _wallet = new Wallet(0);
            _upgrades = new StatUpgrades(_wallet, 1000, 10000);
            _battle = new Battle(_upgrades);
            _stage = new StageProgress(_wallet, _upgrades, _battle, new MinRandom(), 2, 5, 1.5);
            _stage.Begin();
            _stage.Tick(1.2);
        }

        private void FightToDecision()
        {
            for (int i = 0; i < 5000 && _stage.Phase == StagePhase.Playing; i++) _battle.Step();
            Assert.That(_stage.Phase, Is.Not.EqualTo(StagePhase.Playing), "전투가 끝나지 않았습니다.");
        }

        [TestCase(-0.1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Constructor_InvalidWait_Throws(double seconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StageProgress(_wallet, _upgrades, _battle, new MinRandom(), 2, 5, seconds));
        }

        [Test]
        public void Constructor_DefaultWait_IsZeroAndGameDefaultIsOneAndHalf()
        {
            var stage = new StageProgress(_wallet, _upgrades, _battle, new MinRandom());
            Assert.That(stage.ClearWaitSeconds, Is.EqualTo(0));
            Assert.That(StageProgress.DefaultClearWaitSeconds, Is.EqualTo(1.5));
        }

        [Test]
        public void LastNormalDefeated_WaitsWithRewardsAndBattleRunning()
        {
            FightToDecision();
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.ClearWait));
            Assert.That(_stage.FadeAlpha, Is.Zero);
            Assert.That(_battle.Paused, Is.False);
            Assert.That(_battle.IsHoldingPosition, Is.True);
            Assert.That(_upgrades.Locked, Is.True);
            Assert.That(_wallet.Balance, Is.EqualTo(24));
        }

        [Test]
        public void Tick_WaitThenRemainder_StartsFadeAndNotifies()
        {
            FightToDecision();
            int changes = 0;
            _stage.Changed += () => changes++;
            _stage.Tick(1.4);
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.ClearWait));
            _stage.Tick(0.2);
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(_stage.FadeAlpha, Is.EqualTo(0.2).Within(1e-6));
            Assert.That(_battle.Paused, Is.True);
            Assert.That(changes, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Tick_FadeReachesReplacement_ReleasesHold()
        {
            FightToDecision();
            _stage.Tick(1.6);
            _stage.Tick(0.4);
            Assert.That(_stage.SubstageNumber, Is.EqualTo(2));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingIn));
            Assert.That(_battle.IsHoldingPosition, Is.False);
        }

        [Test]
        public void ClearWait_ExtraEnemyDefeated_IsIgnored()
        {
            FightToDecision();
            var enemy = _battle.AddEnemy(EnemyKind.Normal, 2, _battle.DisciplePosition + 0.8);
            for (int i = 0; i < 5000 && !enemy.IsDead; i++) _battle.Step();
            Assert.That(enemy.IsDead, Is.True);
            Assert.That(_wallet.Balance, Is.EqualTo(24));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.ClearWait));
        }

        [Test]
        public void BossDefeated_WaitsNotifiesAndReplacesStage()
        {
            var events = new List<string>();
            _stage.StageCleared += number => events.Add("clear:" + number + ":" + _stage.Phase);
            _stage.Changed += () => events.Add("changed");
            for (int i = 0; i < 6; i++)
            {
                FightToDecision();
                Assert.That(_stage.Phase, Is.EqualTo(StagePhase.ClearWait));
                _stage.Tick(1.5);
                _stage.Tick(1.0);
                _stage.Tick(1.2);
            }
            events.Clear();
            FightToDecision();
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.ClearWait));
            Assert.That(events.FindAll(e => e.StartsWith("clear:")), Is.EqualTo(new[] { "clear:2:ClearWait" }));
            int clearIndex = events.IndexOf("clear:2:ClearWait");
            Assert.That(events[clearIndex + 1], Is.EqualTo("changed"));
            Assert.That(_wallet.Balance, Is.EqualTo(204));
            Assert.That(_stage.IsBossActive, Is.True);
            Assert.That(_stage.Boss, Is.Not.Null);
            _stage.Tick(1.5);
            _stage.Tick(0.5);
            Assert.That(_stage.StageNumber, Is.EqualTo(3));
            Assert.That(_stage.SubstageNumber, Is.EqualTo(1));
            Assert.That(_battle.IsHoldingPosition, Is.False);
        }

        [Test]
        public void DefaultWait_LastNormalDefeated_FadesImmediately()
        {
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 1000, 10000);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new MinRandom(), 2, 5);
            stage.Begin();
            stage.Tick(1.2);
            for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(battle.Paused, Is.True);
        }

        [Test]
        public void DiscipleDefeated_FadesImmediatelyAndNotifiesFailure()
        {
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 1, 1);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new MinRandom(), 2, 5, 1.5);
            int failures = 0;
            stage.StageFailed += _ => failures++;
            stage.Begin();
            stage.Tick(1.2);
            for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(failures, Is.EqualTo(1));
        }

        [Test]
        public void Tick_LargeRemainder_ContinuesThroughBothFades()
        {
            FightToDecision();
            _stage.Tick(2.5);
            Assert.That(_stage.SubstageNumber, Is.EqualTo(2));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(_battle.IsHoldingPosition, Is.False);
        }
    }
}
