using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Stage
{
    public class StageProgressRegressionTests
    {
        private Wallet wallet;
        private StatUpgrades upgrades;
        private Battle battle;
        private StageProgress stage;

        private void Create(int start = 5, bool regression = true, int count = 0, long stats = 1, double wait = 0)
        {
            wallet = new Wallet(0);
            upgrades = new StatUpgrades(wallet, stats, stats);
            battle = new Battle(upgrades);
            stage = new StageProgress(wallet, upgrades, battle, new MinRandom(), start, 5, wait, regression, count);
        }

        private void Fight()
        {
            stage.Begin();
            while (stage.Phase == StagePhase.Intro || stage.Phase == StagePhase.SubstageIntro)
                stage.Tick(StageProgress.IntroSeconds);
            for (int i = 0; i < 20000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
            Assert.That(stage.Phase, Is.Not.EqualTo(StagePhase.Playing), "전투가 결판나지 않았다");
        }

        [Test]
        public void FifthStageDefeat_RegressesInOrderAndPreservesResources()
        {
            Create();
            var events = new List<string>();
            bool failed = false;
            stage.StageFailed += n => { failed = true; events.Add("failed:" + n); };
            stage.Regressed += n => events.Add("regressed:" + n);
            stage.Begin();
            stage.Tick(StageProgress.SubstageIntroSeconds);
            long gold = wallet.Balance;
            int attack = upgrades.GetLevel(StatType.Attack);
            int health = upgrades.GetLevel(StatType.Health);
            int inner = upgrades.GetLevel(StatType.InnerPower);
            stage.Changed += () => { if (failed) events.Add("changed"); };
            for (int i = 0; i < 20000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(events, Is.EqualTo(new[] { "failed:5", "regressed:1", "changed" }));
            stage.Tick(0.5);
            Assert.That(stage.StageNumber, Is.EqualTo(1));
            Assert.That(stage.SubstageNumber, Is.EqualTo(1));
            Assert.That(stage.RegressionCount, Is.EqualTo(1));
            Assert.That(stage.SeenIntroStage, Is.EqualTo(5));
            stage.Tick(0.5);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(wallet.Balance, Is.EqualTo(gold));
            Assert.That(upgrades.GetLevel(StatType.Attack), Is.EqualTo(attack));
            Assert.That(upgrades.GetLevel(StatType.Health), Is.EqualTo(health));
            Assert.That(upgrades.GetLevel(StatType.InnerPower), Is.EqualTo(inner));
        }

        [Test]
        public void DefaultRegression_FifthStageDefeatRetries()
        {
            wallet = new Wallet(0);
            upgrades = new StatUpgrades(wallet, 1, 1);
            battle = new Battle(upgrades);
            stage = new StageProgress(wallet, upgrades, battle, new MinRandom(), 5, 5, 0.0);
            int calls = 0;
            stage.Regressed += _ => calls++;
            Fight();
            stage.Tick(0.5);
            Assert.That(stage.StageNumber, Is.EqualTo(5));
            Assert.That(calls, Is.Zero);
            Assert.That(stage.RegressionCount, Is.Zero);
        }

        [Test]
        public void EarlierStageDefeat_RetriesWithoutRegression()
        {
            Create(start: 3);
            int calls = 0;
            stage.Regressed += _ => calls++;
            Fight();
            stage.Tick(0.5);
            Assert.That(stage.StageNumber, Is.EqualTo(3));
            Assert.That(calls, Is.Zero);
        }

        [TestCase(2, 3)]
        [TestCase(int.MaxValue, int.MaxValue)]
        public void FifthStageDefeat_IncrementsWithoutOverflow(int initial, int expected)
        {
            Create(count: initial);
            var counts = new List<int>();
            stage.Regressed += n => counts.Add(n);
            Fight();
            Assert.That(counts, Is.EqualTo(new[] { expected }));
            Assert.That(stage.RegressionCount, Is.EqualTo(expected));
        }

        [TestCase(7, true, 2)]
        [TestCase(10, true, 5)]
        [TestCase(7, false, 7)]
        public void Constructor_NormalizesStageOnlyForRegression(int start, bool enabled, int expected)
        {
            Create(start, enabled, -4);
            Assert.That(stage.StageNumber, Is.EqualTo(expected));
            Assert.That(stage.UseRegression, Is.EqualTo(enabled));
            Assert.That(stage.RegressionCount, Is.Zero);
        }

        [Test]
        public void FifthStageVictory_WaitsThenClearsInOrderAndStaysCleared()
        {
            Create(stats: 100000, wait: 1.5);
            var clearedStages = new List<int>();
            var events = new List<string>();
            StagePhase observed = StagePhase.Playing;
            stage.StageCleared += n => clearedStages.Add(n);
            stage.GameCleared += () => { observed = stage.Phase; events.Add("game"); };
            Fight();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.ClearWait));
            Assert.That(clearedStages, Is.EqualTo(new[] { 5 }));
            Assert.That(wallet.Balance, Is.EqualTo(StageRewards.BossGold(5, 7)));
            stage.Tick(1.4);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.ClearWait));
            Assert.That(events, Is.Empty);
            stage.Changed += () => events.Add("changed");
            stage.Tick(1.6);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Cleared));
            Assert.That(observed, Is.EqualTo(StagePhase.Cleared));
            Assert.That(events, Is.EqualTo(new[] { "game", "changed" }));
            Assert.That(battle.Paused, Is.True);
            Assert.That(upgrades.Locked, Is.True);
            stage.Tick(10);
            battle.Step();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Cleared));
            Assert.That(stage.StageNumber, Is.EqualTo(5));
            Assert.That(events, Is.EqualTo(new[] { "game", "changed" }));
            Assert.Throws<ArgumentOutOfRangeException>(() => stage.Tick(-1));
        }

        [Test]
        public void FifthStageVictory_ZeroWaitClearsImmediately()
        {
            Create(stats: 100000);
            var events = new List<string>();
            stage.StageCleared += n => events.Add("stage:" + n);
            stage.GameCleared += () => events.Add("game");
            Fight();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Cleared));
            Assert.That(events, Is.EqualTo(new[] { "stage:5", "game" }));
        }

        [Test]
        public void RegressionDisabled_FifthStageVictoryAdvances()
        {
            Create(regression: false, count: 2, stats: 100000, wait: 1.5);
            int calls = 0;
            stage.GameCleared += () => calls++;
            Fight();
            stage.Tick(1.5);
            stage.Tick(0.5);
            Assert.That(stage.StageNumber, Is.EqualTo(6));
            Assert.That(stage.RegressionCount, Is.EqualTo(2));
            Assert.That(calls, Is.Zero);
        }
    }
}
