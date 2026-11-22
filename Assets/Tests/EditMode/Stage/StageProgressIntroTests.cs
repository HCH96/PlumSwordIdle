using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Stage
{
    public class StageProgressIntroTests
    {
        private Wallet _wallet;
        private StatUpgrades _upgrades;
        private Battle _battle;

        private StageProgress Create(int start = 1, int seen = 5, long gold = 0)
        {
            _wallet = new Wallet(gold);
            _upgrades = new StatUpgrades(_wallet, 10, 100);
            _battle = new Battle(_upgrades);
            return new StageProgress(_wallet, _upgrades, _battle, new MinRandom(), start, seen);
        }

        [Test]
        public void Constructor_DefaultSeen_StartsAtFiveWithoutIntro()
        {
            Create();
            var stage = new StageProgress(_wallet, _upgrades, _battle, new MinRandom());
            Assert.That(stage.SeenIntroStage, Is.EqualTo(5));
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(stage.IsIntroShowing, Is.False);
            stage.Begin();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(_battle.Paused, Is.True);
            Assert.That(stage.SeenIntroStage, Is.EqualTo(5));
        }

        [TestCase(0, 0)]
        [TestCase(3, 3)]
        [TestCase(-2, 0)]
        [TestCase(9, 5)]
        public void Constructor_SeenValue_ClampsAndStartsPlaying(int seen, int expected)
        {
            StageProgress stage = null;
            Assert.DoesNotThrow(() => stage = Create(seen: seen));
            Assert.That(stage.SeenIntroStage, Is.EqualTo(expected));
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(stage.IsIntroShowing, Is.False);
        }

        [Test]
        public void Begin_UnseenFirstStage_PausesAfterPlacementAndNotifiesInOrder()
        {
            var stage = Create(seen: 0);
            var events = new List<string>();
            stage.StageStarted += n => {
                events.Add("started:" + n);
                Assert.That(stage.SeenIntroStage, Is.EqualTo(1));
                Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
                Assert.That(stage.HasBegun, Is.True);
            };
            stage.Changed += () => events.Add("changed");
            stage.Begin();
            Assert.That(stage.SeenIntroStage, Is.EqualTo(1));
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
            Assert.That(stage.IsIntroShowing, Is.True);
            Assert.That(_battle.Paused, Is.True);
            Assert.That(_upgrades.Locked, Is.False);
            Assert.That(stage.FadeAlpha, Is.EqualTo(0.0));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
            Assert.That(events, Is.EqualTo(new[] { "started:1", "changed" }));
            stage.Begin();
            Assert.That(events.Count, Is.EqualTo(2));
        }

        [Test]
        public void Intro_BattleStepAndTick_LeaveBattleAndPhaseUnchanged()
        {
            var stage = Create(seen: 0);
            int changed = 0;
            stage.Changed += () => changed++;
            stage.Begin();
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            _battle.Step();
            Assert.DoesNotThrow(() => stage.Tick(2.9));
            Assert.That(_wallet.Balance, Is.Zero);
            Assert.That(stage.DefeatedNormalCount, Is.Zero);
            Assert.That(_battle.DisciplePosition, Is.Zero);
            Assert.That(changed, Is.EqualTo(1));
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
        }

        [Test]
        public void Intro_UpgradeWithGold_SucceedsWithoutEndingIntro()
        {
            var stage = Create(seen: 0, gold: 20);
            stage.Begin();
            Assert.That(_upgrades.TryUpgrade(StatType.Attack), Is.True);
            Assert.That(_wallet.Balance, Is.Zero);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
        }

        [TestCase(3, 3, StagePhase.SubstageIntro, true, 3)]
        [TestCase(3, 2, StagePhase.Intro, true, 3)]
        [TestCase(7, 0, StagePhase.SubstageIntro, true, 0)]
        public void Begin_SeenAndRound_DetermineIntro(int start, int seen, StagePhase phase, bool paused, int expectedSeen)
        {
            var stage = Create(start, seen);
            stage.Begin();
            Assert.That(stage.Phase, Is.EqualTo(phase));
            Assert.That(_battle.Paused, Is.EqualTo(paused));
            Assert.That(stage.SeenIntroStage, Is.EqualTo(expectedSeen));
        }

        [Test]
        public void Begin_ThemeIntroDisabled_StartsSubstageWithoutDelay()
        {
            Create();
            var stage = new StageProgress(_wallet, _upgrades, _battle, new MinRandom(),
                startStage: 2, seenIntroStage: 0, showThemeIntro: false);
            stage.Begin();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(stage.IsIntroShowing, Is.False);
            stage.Tick(StageProgress.SubstageIntroSeconds);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
        }

        [Test]
        public void Transition_ThemeIntroDisabled_StartsNextSubstageAfterFade()
        {
            _wallet = new Wallet(0);
            _upgrades = new StatUpgrades(_wallet, 1000, 100);
            _battle = new Battle(_upgrades);
            var stage = new StageProgress(_wallet, _upgrades, _battle, new MinRandom(),
                seenIntroStage: 0, showThemeIntro: false);
            stage.Begin();
            stage.Tick(StageProgress.SubstageIntroSeconds);
            _battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            _battle.Step();
            stage.Tick(StageProgress.FadeSeconds * 2);
            Assert.That(stage.StageNumber, Is.EqualTo(2));
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(stage.IsIntroShowing, Is.False);
        }

        [Test]
        public void StagePhase_Values_PreserveExistingNumbers()
        {
            Assert.That((int)StagePhase.Playing, Is.EqualTo(0));
            Assert.That((int)StagePhase.FadingOut, Is.EqualTo(1));
            Assert.That((int)StagePhase.FadingIn, Is.EqualTo(2));
            Assert.That((int)StagePhase.Intro, Is.EqualTo(3));
            Assert.That((int)StagePhase.SubstageIntro, Is.EqualTo(4));
        }
    }
}
