using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Stage
{
    /// <summary>소개 시간 세기(Tick)와 가려진 순간의 교체 뒤 소개. docs/spec/stage.md "시간", "소개 판단".</summary>
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
            upgrades = new StatUpgrades(wallet, attack, health);
            battle = new Battle(upgrades);
            stage = new StageProgress(wallet, upgrades, battle, new MinRandom(), start, seen);
            stage.StageStarted += n => { started.Add(n); seenAtStart.Add(stage.SeenIntroStage); };
            stage.StageCleared += n => cleared.Add(n);
            stage.StageFailed += n => failed.Add(n);
            stage.Changed += () => changed++;
        }

        /// <summary>"소개 뒤 성공 상태": (1000, 100), seen 0 → Begin → Tick(3.0) → 보스 → 1칸 진행.</summary>
        private void ClearFirstStage()
        {
            Create();
            stage.Begin();
            stage.Tick(3.0);
            stage.Tick(StageProgress.SubstageIntroSeconds);
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
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(stage.IsIntroShowing, Is.False);
            Assert.That(battle.Paused, Is.True);
            Assert.That(changed, Is.EqualTo(before + 1));
            stage.Tick(StageProgress.SubstageIntroSeconds);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(battle.Paused, Is.False);
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
            stage.Tick(StageProgress.SubstageIntroSeconds);
            battle.Step();
            Assert.That(enemy.Health, Is.EqualTo(20));
        }

        [Test]
        public void BeginIntro_TickThree_StartsPlaying()
        {
            Create();
            stage.Begin();
            stage.Tick(3.0);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            stage.Tick(StageProgress.SubstageIntroSeconds);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
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
            Assert.That(stage.FadeAlpha, Is.EqualTo(0.0));
            Assert.That(changed, Is.EqualTo(before + 2));
            stage.Tick(3.0);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            stage.Tick(StageProgress.SubstageIntroSeconds);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(battle.Paused, Is.False);
            Assert.That(changed, Is.EqualTo(before + 4));
        }

        [Test]
        public void TransitionTickOnePointFive_DiscardsRemainderBeforeIntro()
        {
            ClearFirstStage();
            stage.Tick(1.5);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
            Assert.That(stage.StageNumber, Is.EqualTo(2));
            stage.Tick(2.9);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
            stage.Tick(0.1);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            stage.Tick(StageProgress.SubstageIntroSeconds);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
        }

        [Test]
        public void TransitionTickFour_EntersIntroAtZero()
        {
            ClearFirstStage();
            stage.Tick(4.0);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
            Assert.That(stage.IsIntroShowing, Is.True);
            Assert.That(battle.Paused, Is.True);
            Assert.That(started, Is.EqualTo(new[] { 1, 2 }));
            stage.Tick(3.0);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            stage.Tick(StageProgress.SubstageIntroSeconds);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Playing));
            Assert.That(battle.Paused, Is.False);
        }

        [Test]
        public void Failure_RetryDoesNotRepeatIntro()
        {
            Create(10, 5);
            stage.Begin();
            stage.Tick(3.0);
            stage.Tick(StageProgress.SubstageIntroSeconds);
            battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            battle.Step();
            Assert.That(failed, Is.EqualTo(new[] { 1 }));
            stage.Tick(1.0);
            Assert.That(stage.StageNumber, Is.EqualTo(1));
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(battle.Paused, Is.True);
            Assert.That(upgrades.Locked, Is.False);
            Assert.That(stage.SeenIntroStage, Is.EqualTo(1));
            Assert.That(started, Is.EqualTo(new[] { 1, 1 }));
        }

        [Test]
        public void FifthStageCleared_SecondRoundHasNoIntro()
        {
            Create(attack: 1500, start: 5, seen: 4);
            stage.Begin();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
            Assert.That(stage.SeenIntroStage, Is.EqualTo(5));
            stage.Tick(3.0);
            stage.Tick(StageProgress.SubstageIntroSeconds);
            battle.AddEnemy(EnemyKind.Boss, 5, 1.0);
            battle.Step();
            Assert.That(cleared, Is.EqualTo(new[] { 5 }));
            stage.Tick(1.0);
            Assert.That(stage.StageNumber, Is.EqualTo(6));
            Assert.That(stage.RoundNumber, Is.EqualTo(2));
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(battle.Paused, Is.True);
            Assert.That(stage.SeenIntroStage, Is.EqualTo(5));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RestoreDuringFadeOrIntro_DoesNotRepeatSeenIntro(bool duringIntro)
        {
            ClearFirstStage();
            stage.Tick(0.5);
            if (duringIntro)
            {
                stage.Tick(0.5);
                stage.Tick(1.0);
                Assert.That(stage.Phase, Is.EqualTo(StagePhase.Intro));
            }
            int number = stage.StageNumber;
            int seen = stage.SeenIntroStage;
            Assert.That(number, Is.EqualTo(2));
            Assert.That(seen, Is.EqualTo(2));
            Create(start: number, seen: seen);
            stage.Begin();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(stage.SeenIntroStage, Is.EqualTo(2));
        }
    }
}
