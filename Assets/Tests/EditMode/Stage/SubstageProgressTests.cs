using System;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Stage
{
    public sealed class SubstageProgressTests
    {
        [Test]
        public void RandomGroups_AlwaysHaveTwoOrThreeAndKeepTwoUnitSpacing()
        {
            var random = new Random(42);
            bool two = false, three = false;
            for (int i = 0; i < 100; i++)
            {
                var positions = StageLayout.SubstagePositions(random, 20);
                Assert.That(positions.Length, Is.InRange(2, 3));
                two |= positions.Length == 2; three |= positions.Length == 3;
                for (int j = 1; j < positions.Length; j++) Assert.That(positions[j] - positions[j - 1], Is.EqualTo(2).Within(1e-9));
                Assert.That((positions[0] + positions[positions.Length - 1]) / 2, Is.EqualTo(20).Within(1e-9));
            }
            Assert.That(two && three, Is.True);
        }

        [Test]
        public void SixGroupsThenBoss_DisplayIntroPauseAndRewards()
        {
            var wallet = new Wallet();
            var upgrades = new StatUpgrades(wallet, 1000, 10000, 1);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new Random(5));
            stage.Begin();
            int total = 0;
            for (int n = 1; n <= 6; n++)
            {
                Assert.That(stage.SubstageNumber, Is.EqualTo(n));
                Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
                Assert.That(battle.Paused, Is.True);
                Assert.That(battle.Enemies.Count, Is.InRange(2, 3));
                total += battle.Enemies.Count;
                stage.Tick(StageProgress.SubstageIntroSeconds);
                Assert.That(battle.Paused, Is.False);
                for (int step = 0; step < 5000 && stage.SubstageNumber == n; step++) battle.Step();
                Assert.That(stage.SubstageNumber, Is.EqualTo(n + 1));
                Assert.That(wallet.Balance, Is.EqualTo(total * 10));
            }
            Assert.That(stage.IsBossActive, Is.True);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(battle.Enemies.Count, Is.EqualTo(1));
            Assert.That(battle.Enemies[0].Kind, Is.EqualTo(EnemyKind.Boss));
            stage.Tick(StageProgress.SubstageIntroSeconds);
            for (int step = 0; step < 5000 && stage.Phase == StagePhase.Playing; step++) battle.Step();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(wallet.Balance, Is.EqualTo(total * 10 + 50));
            stage.Tick(1);
            Assert.That(stage.StageNumber, Is.EqualTo(2));
            Assert.That(stage.SubstageNumber, Is.EqualTo(1));
        }

        [Test]
        public void Failure_RetriesLargeStageFromFirstSubstage()
        {
            var wallet = new Wallet();
            var upgrades = new StatUpgrades(wallet, 1, 5, 1);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new Random(1));
            stage.Begin(); stage.Tick(StageProgress.SubstageIntroSeconds);
            battle.AddEnemy(EnemyKind.Normal, 1, .8); battle.Step();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            stage.Tick(1);
            Assert.That(stage.StageNumber, Is.EqualTo(1));
            Assert.That(stage.SubstageNumber, Is.EqualTo(1));
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(battle.DiscipleHealth, Is.EqualTo(5));
        }
    }
}
