using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Stage
{
    /// <summary>018_stage_progress_fade: 페이드 시간, 가려진 순간의 교체, 전환 끝, FadeAlpha.</summary>
    public class StageProgressFadeTests
    {
        private const double Tolerance = 1e-6;
        private const double NearPosition = 0.8;
        private const double BossPosition = 1.0;

        private Wallet _wallet;
        private StatUpgrades _upgrades;
        private Battle _battle;
        private StageProgress _stage;

        private int _changedCount;
        private List<int> _started;

        /// <summary>알림 순서 기록 ("Changed", "Started").</summary>
        private List<string> _events;

        private void Create(long attack, long maxHealth, int startStage = 1)
        {
            _wallet = new Wallet(0);
            _upgrades = new StatUpgrades(_wallet, attack, maxHealth);
            _battle = new Battle(_upgrades);
            _stage = new StageProgress(_wallet, _upgrades, _battle, new MinRandom(), startStage);

            _changedCount = 0;
            _started = new List<int>();
            _events = new List<string>();

            _stage.Changed += () =>
            {
                _changedCount++;
                _events.Add("Changed");
            };
            _stage.StageStarted += n =>
            {
                _started.Add(n);
                _events.Add("Started");
            };
        }

        /// <summary>(1000, 100) 시작 뒤 보스를 직접 넣고 1칸 진행한 "성공 상태".</summary>
        private void ArrangeClearedState()
        {
            Create(1000, 100);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            _battle.AddEnemy(EnemyKind.Boss, 1, BossPosition);
            _battle.Step();
        }

        /// <summary>(10, 5) 시작 뒤 일반 몬스터를 직접 넣고 1칸 진행한 "실패 상태".</summary>
        private void ArrangeFailedState()
        {
            Create(10, 5);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            _battle.AddEnemy(EnemyKind.Normal, 1, NearPosition);
            _battle.Step();
        }

        [Test]
        public void ArrangedStates_MatchPreconditions()
        {
            ArrangeClearedState();
            Assert.That(_wallet.Balance, Is.EqualTo(50));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(_stage.StageNumber, Is.EqualTo(1));
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(90));
            Assert.That(_started, Is.EqualTo(new[] { 1 }));

            ArrangeFailedState();
            Assert.That(_wallet.Balance, Is.EqualTo(0));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(_battle.IsDiscipleDefeated, Is.True);
        }

        [Test]
        public void ClearedFadeOut_HalfThenCovered_ReplacesWithNextStage()
        {
            ArrangeClearedState();

            _stage.Tick(0.25);
            Assert.That(_stage.FadeAlpha, Is.EqualTo(0.5).Within(Tolerance));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(_stage.StageNumber, Is.EqualTo(1));
            Assert.That(_started, Is.EqualTo(new[] { 1 }));

            _stage.Tick(0.25);
            Assert.That(_stage.StageNumber, Is.EqualTo(2));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingIn));
            Assert.That(_stage.FadeAlpha, Is.EqualTo(1.0).Within(Tolerance));
            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(0));
            Assert.That(_stage.Boss, Is.Null);
            Assert.That(_stage.BarRatio, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
            for (int i = 0; i < _battle.Enemies.Count; i++)
            {
                Assert.That(_battle.Enemies[i].MaxHealth, Is.EqualTo(40));
                Assert.That(_battle.Enemies[i].Position, Is.EqualTo(5.0 + 2.0 * i).Within(Tolerance));
            }

            Assert.That(_battle.DisciplePosition, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(100));
            Assert.That(_battle.Paused, Is.True);
            Assert.That(_upgrades.Locked, Is.True);
            Assert.That(_wallet.Balance, Is.EqualTo(50));
            Assert.That(_started, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void ClearedFadeIn_HalfThenDone_ReturnsToPlayingAndUnlocks()
        {
            ArrangeClearedState();
            _stage.Tick(0.25);
            _stage.Tick(0.25);

            _stage.Tick(0.25);
            Assert.That(_stage.FadeAlpha, Is.EqualTo(0.5).Within(Tolerance));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingIn));

            _stage.Tick(0.25);
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(_stage.FadeAlpha, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(_battle.Paused, Is.True);
            Assert.That(_upgrades.Locked, Is.False);
            Assert.That(_started, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void FailedFade_ReplacesSameStageWithFullHealth_ThenPlaying()
        {
            ArrangeFailedState();

            _stage.Tick(0.5);
            Assert.That(_stage.StageNumber, Is.EqualTo(1));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingIn));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(5));
            Assert.That(_battle.IsDiscipleDefeated, Is.False);
            Assert.That(_started, Is.EqualTo(new[] { 1, 1 }));

            _stage.Tick(0.5);
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(_battle.Paused, Is.True);
            Assert.That(_upgrades.Locked, Is.False);
        }

        [Test]
        public void TwoTicksOfPointThree_CarriesRemainderIntoFadeIn()
        {
            ArrangeClearedState();

            _stage.Tick(0.3);
            _stage.Tick(0.3);

            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingIn));
            Assert.That(_stage.StageNumber, Is.EqualTo(2));
            Assert.That(_stage.FadeAlpha, Is.EqualTo(0.8).Within(Tolerance));
        }

        [Test]
        public void LongTick_GoesThroughBothStepsInOneTick()
        {
            ArrangeClearedState();
            _events.Clear();
            _started.Clear();
            int changedBefore = _changedCount;

            _stage.Tick(2.0);

            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
            Assert.That(_stage.StageNumber, Is.EqualTo(2));
            Assert.That(_stage.FadeAlpha, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(_battle.Paused, Is.True);
            Assert.That(_upgrades.Locked, Is.False);
            Assert.That(_started, Is.EqualTo(new[] { 2 }));
            Assert.That(_changedCount - changedBefore, Is.EqualTo(2));
            Assert.That(_events, Is.EqualTo(new[] { "Started", "Changed", "Changed" }));
        }

        [Test]
        public void ZeroTick_ChangesNothingAndSendsNoNotification()
        {
            ArrangeClearedState();
            int changedBefore = _changedCount;
            int eventsBefore = _events.Count;

            _stage.Tick(0);

            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(_stage.FadeAlpha, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(_stage.StageNumber, Is.EqualTo(1));
            Assert.That(_changedCount, Is.EqualTo(changedBefore));
            Assert.That(_events.Count, Is.EqualTo(eventsBefore));
        }

        [Test]
        public void UpgradeDuringFade_Fails_AfterTransition_Succeeds()
        {
            ArrangeClearedState();

            Assert.That(_upgrades.TryUpgrade(StatType.Attack), Is.False);
            Assert.That(_wallet.Balance, Is.EqualTo(50));

            _stage.Tick(1.0);

            Assert.That(_upgrades.TryUpgrade(StatType.Attack), Is.True);
            Assert.That(_wallet.Balance, Is.EqualTo(30));
        }

        [Test]
        public void AfterTransition_KillsAreCountedAgain()
        {
            ArrangeClearedState();
            _stage.Tick(1.0);
            Assert.That(_stage.StageNumber, Is.EqualTo(2));
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.Playing));

            _battle.AddEnemy(EnemyKind.Normal, 2, NearPosition);
            _battle.Step();

            Assert.That(_wallet.Balance, Is.EqualTo(62));
            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(1));
        }

        [Test]
        public void MaxStageCleared_StaysAtMaxWithoutException()
        {
            Create(100000000000, 100000000000, int.MaxValue);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            _battle.AddEnemy(EnemyKind.Boss, int.MaxValue, BossPosition);
            _battle.Step();

            Assert.DoesNotThrow(() => _stage.Tick(0.5));

            Assert.That(_stage.StageNumber, Is.EqualTo(int.MaxValue));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingIn));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
        }
    }
}
