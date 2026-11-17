using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Stage
{
    public class StageProgressTests
    {
        private const double Tolerance = 1e-6;

        private Wallet _wallet;
        private StatUpgrades _upgrades;
        private Battle _battle;

        // 알림 기록. "StageStarted:1", "StageCleared:1", "StageFailed:1", "Changed" 모양으로 받은 순서대로 쌓는다.
        private List<string> _events;

        [SetUp]
        public void SetUp()
        {
            _wallet = new Wallet(0);
            _upgrades = new StatUpgrades(_wallet, 10, 100, 1.0);
            _battle = new Battle(_upgrades);
            _events = new List<string>();
        }

        private StageProgress Create(Random random = null, int startStage = 1)
        {
            var stage = new StageProgress(_wallet, _upgrades, _battle, random ?? new MinRandom(), startStage);
            stage.StageStarted += n => _events.Add("StageStarted:" + n);
            stage.StageCleared += n => _events.Add("StageCleared:" + n);
            stage.StageFailed += n => _events.Add("StageFailed:" + n);
            stage.Changed += () => _events.Add("Changed");
            return stage;
        }

        // 생성자

        [Test]
        public void New_Default_InitialState()
        {
            var stage = Create();

            Assert.That(stage.StageNumber, Is.EqualTo(1));
            Assert.That(stage.HasBegun, Is.False);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(stage.DefeatedNormalCount, Is.EqualTo(0));
            Assert.That(stage.Boss, Is.Null);
            Assert.That(stage.IsBossActive, Is.False);
            Assert.That(stage.BarRatio, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(stage.FadeAlpha, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(0));
            Assert.That(_events, Is.Empty);
        }

        [Test]
        public void FadeSeconds_IsHalfSecond()
        {
            Assert.That(StageProgress.FadeSeconds, Is.EqualTo(0.5).Within(Tolerance));
        }

        [Test]
        public void New_StartStageFive_StageNumberFive()
        {
            var stage = Create(startStage: 5);

            Assert.That(stage.StageNumber, Is.EqualTo(5));
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void New_StartStageBelowOne_StageNumberOne(int startStage)
        {
            StageProgress stage = null;

            Assert.DoesNotThrow(() => stage = Create(startStage: startStage));
            Assert.That(stage.StageNumber, Is.EqualTo(1));
            Assert.That(_events, Is.Empty);
        }

        [Test]
        public void New_NullWallet_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new StageProgress(null, _upgrades, _battle, new MinRandom()));
        }

        [Test]
        public void New_NullUpgrades_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new StageProgress(_wallet, null, _battle, new MinRandom()));
        }

        [Test]
        public void New_NullBattle_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new StageProgress(_wallet, _upgrades, null, new MinRandom()));
        }

        [Test]
        public void New_NullRandom_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new StageProgress(_wallet, _upgrades, _battle, null));
        }

        // Begin

        [Test]
        public void Begin_MinRandom_PlacesTwoNormalEnemiesAndNotifies()
        {
            var stage = Create();

            stage.Begin();

            Assert.That(stage.HasBegun, Is.True);
            AssertEnemies(30, 5.0, 7.0);
            Assert.That(_events, Is.EqualTo(new[] { "StageStarted:1", "Changed" }));
            Assert.That(_battle.Paused, Is.True);
            Assert.That(_upgrades.Locked, Is.False);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(stage.DefeatedNormalCount, Is.EqualTo(0));
        }

        [Test]
        public void Begin_Twice_SecondCallDoesNothing()
        {
            var stage = Create();

            stage.Begin();
            stage.Begin();

            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
            Assert.That(_events, Is.EqualTo(new[] { "StageStarted:1", "Changed" }));
            Assert.That(stage.HasBegun, Is.True);
        }

        [Test]
        public void Begin_StartStageThree_EnemiesHaveStageThreeHealth()
        {
            var stage = Create(startStage: 3);

            stage.Begin();

            AssertEnemies(50, 5.0, 7.0);
            Assert.That(_events, Is.EqualTo(new[] { "StageStarted:3", "Changed" }));
        }

        [Test]
        public void Begin_MaxRandom_PlacesThreeEnemiesInFirstGroup()
        {
            var stage = Create(new MaxRandom());

            stage.Begin();

            AssertEnemies(30, 4.0, 6.0, 8.0);
        }

        // Tick

        [Test]
        public void Tick_PlayingAfterBegin_DoesNothing()
        {
            var stage = Create();
            stage.Begin();
            stage.Tick(StageProgress.SubstageIntroSeconds);
            _events.Clear();

            stage.Tick(1.0);
            stage.Tick(0);

            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(stage.StageNumber, Is.EqualTo(1));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
            Assert.That(_events, Is.Empty);
        }

        [TestCase(-0.1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Tick_InvalidBeforeBegin_ThrowsArgumentOutOfRange(double deltaSeconds)
        {
            var stage = Create();

            Assert.Throws<ArgumentOutOfRangeException>(() => stage.Tick(deltaSeconds));
        }

        [TestCase(-0.1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Tick_InvalidAfterBegin_ThrowsArgumentOutOfRange(double deltaSeconds)
        {
            var stage = Create();
            stage.Begin();

            Assert.Throws<ArgumentOutOfRangeException>(() => stage.Tick(deltaSeconds));
        }

        [Test]
        public void Tick_BeforeBegin_DoesNothing()
        {
            var stage = Create();

            Assert.DoesNotThrow(() => stage.Tick(1.0));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(0));
            Assert.That(stage.HasBegun, Is.False);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(_events, Is.Empty);
        }

        private void AssertEnemies(long maxHealth, params double[] positions)
        {
            Assert.That(_battle.Enemies.Count, Is.EqualTo(positions.Length));
            for (int i = 0; i < positions.Length; i++)
            {
                Enemy enemy = _battle.Enemies[i];
                Assert.That(enemy.Position, Is.EqualTo(positions[i]).Within(Tolerance), $"index {i}");
                Assert.That(enemy.Kind, Is.EqualTo(EnemyKind.Normal), $"index {i}");
                Assert.That(enemy.MaxHealth, Is.EqualTo(maxHealth), $"index {i}");
            }
        }
    }
}
