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
            _upgrades = new StatUpgrades(_wallet, 10, 100, 1.0);
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
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(_battle.Paused, Is.False);
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
            Assert.That(_battle.Enemies.Count, Is.EqualTo(10));
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
            Assert.DoesNotThrow(() => stage.Tick(10.0));
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

        [TestCase(3, 3, StagePhase.Playing, false, 3)]
        [TestCase(3, 2, StagePhase.Intro, true, 3)]
        [TestCase(7, 0, StagePhase.Playing, false, 0)]
        public void Begin_SeenAndRound_DetermineIntro(int start, int seen, StagePhase phase, bool paused, int expectedSeen)
        {
            var stage = Create(start, seen);
            stage.Begin();
            Assert.That(stage.Phase, Is.EqualTo(phase));
            Assert.That(_battle.Paused, Is.EqualTo(paused));
            Assert.That(stage.SeenIntroStage, Is.EqualTo(expectedSeen));
        }

        [Test]
        public void StagePhase_Values_PreserveExistingNumbers()
        {
            Assert.That((int)StagePhase.Playing, Is.EqualTo(0));
            Assert.That((int)StagePhase.FadingOut, Is.EqualTo(1));
            Assert.That((int)StagePhase.FadingIn, Is.EqualTo(2));
            Assert.That((int)StagePhase.Intro, Is.EqualTo(3));
        }
    }

    public class StageProgressIntroTickTests
    {
        private Wallet wallet;
        private StatUpgrades upgrades;
        private Battle battle;
        private StageProgress stage;
        private readonly List<int> started = new List<int>();
        private readonly List<int> seenAtStart = new List<int>();
        private readonly List<int> cleared = new List<int>();
        private readonly List<int> failed = new List<int>();
        private int changed;

        private void Create(long attack = 1000, long health = 100, int start = 1, int seen = 0)
        {
            started.Clear();
            seenAtStart.Clear();
            cleared.Clear();
            failed.Clear();
            changed = 0;
            wallet = new Wallet(0);
            upgrades = new StatUpgrades(wallet, attack, health, 1.0);
            battle = new Battle(upgrades);
            stage = new StageProgress(wallet, upgrades, battle, new MinRandom(), start, seen);
            stage.StageStarted += n => { started.Add(n); seenAtStart.Add(stage.SeenIntroStage); };
            stage.StageCleared += n => cleared.Add(n);
            stage.StageFailed += n => failed.Add(n);
            stage.Changed += () => changed++;
        }

        private void ClearFirstStage()
        {
            Create();
            stage.Begin();
            stage.Tick(3.0);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            battle.Step();
            Assert.That(cleared, Is.EqualTo(new[] { 1 }));
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
        }

        [Test]
        public void BeginIntro_CountsThreeSeconds_AndNotifiesOnlyOnEnd()
        {
            Create(10);
            stage.Begin();
            int before = changed;
            stage.Tick(2.9);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
            Assert.That(battle.Paused, Is.True);
            Assert.That(changed, Is.EqualTo(before));
            stage.Tick(0.1);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(stage.IsIntroShowing, Is.False);
            Assert.That(battle.Paused, Is.False);
            Assert.That(changed, Is.EqualTo(before + 1));
        }

        [Test]
        public void Intro_PausesCombatUntilFinished()
        {
            Create(10);
            stage.Begin();
            var enemy = battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            battle.Step();
            Assert.That(enemy.Health, Is.EqualTo(30));
            stage.Tick(3.0);
            battle.Step();
            Assert.That(enemy.Health, Is.EqualTo(20));
        }

        [Test]
        public void Transition_IntroducesNextStageAfterFade()
        {
            ClearFirstStage();
            int before = changed;
            stage.Tick(0.5);
            Assert.That(stage.StageNumber, Is.EqualTo(2));
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingIn));
            Assert.That(stage.SeenIntroStage, Is.EqualTo(2));
            Assert.That(started, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(seenAtStart, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(changed, Is.EqualTo(before + 1));
            stage.Tick(0.5);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
            Assert.That(battle.Paused, Is.True);
            Assert.That(upgrades.Locked, Is.False);
            Assert.That(stage.FadeAlpha, Is.Zero);
            Assert.That(changed, Is.EqualTo(before + 2));
            stage.Tick(3.0);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(battle.Paused, Is.False);
            Assert.That(changed, Is.EqualTo(before + 3));
        }

        [TestCase(1.5)]
        [TestCase(4.0)]
        public void LongTransitionTick_DiscardsRemainderBeforeIntro(double seconds)
        {
            ClearFirstStage();
            stage.Tick(seconds);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
            Assert.That(stage.StageNumber, Is.EqualTo(2));
            Assert.That(stage.IsIntroShowing, Is.True);
            Assert.That(battle.Paused, Is.True);
            Assert.That(started, Is.EqualTo(new[] { 1, 2 }));
            stage.Tick(2.9);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
            stage.Tick(0.1);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(battle.Paused, Is.False);
        }

        [Test]
        public void Failure_RetryDoesNotRepeatIntro()
        {
            Create(10, 5);
            stage.Begin();
            stage.Tick(3.0);
            battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            battle.Step();
            Assert.That(failed, Is.EqualTo(new[] { 1 }));
            stage.Tick(1.0);
            Assert.That(stage.StageNumber, Is.EqualTo(1));
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(battle.Paused, Is.False);
            Assert.That(upgrades.Locked, Is.False);
            Assert.That(stage.SeenIntroStage, Is.EqualTo(1));
            Assert.That(started, Is.EqualTo(new[] { 1, 1 }));
        }

        [Test]
        public void FifthStageCleared_SecondRoundHasNoIntro()
        {
            Create(start: 5, seen: 4);
            stage.Begin();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
            Assert.That(stage.SeenIntroStage, Is.EqualTo(5));
            stage.Tick(3.0);
            battle.AddEnemy(EnemyKind.Boss, 5, 1.0);
            battle.Step();
            Assert.That(cleared, Is.EqualTo(new[] { 5 }));
            stage.Tick(1.0);
            Assert.That(stage.StageNumber, Is.EqualTo(6));
            Assert.That(stage.RoundNumber, Is.EqualTo(2));
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(battle.Paused, Is.False);
            Assert.That(stage.SeenIntroStage, Is.EqualTo(5));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RestoreDuringFadeOrIntro_DoesNotRepeatSeenIntro(bool duringIntro)
        {
            ClearFirstStage();
            stage.Tick(0.5);
            if (duringIntro) { stage.Tick(0.5); stage.Tick(1.0); }
            int number = stage.StageNumber;
            int seen = stage.SeenIntroStage;
            Assert.That(number, Is.EqualTo(2));
            Assert.That(seen, Is.EqualTo(2));
            Create(start: number, seen: seen);
            stage.Begin();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(stage.SeenIntroStage, Is.EqualTo(2));
        }
    }

}
