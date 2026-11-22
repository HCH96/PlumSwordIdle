using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Stage
{
    /// <summary>017_stage_progress_outcome: 보스 처치 성공, 제자 쓰러짐 실패, 동시에 쓰러짐, 전환 시작.</summary>
    public class StageProgressOutcomeTests
    {
        private const double Tolerance = 1e-6;
        private const double NearPosition = 0.8;
        private const double BossPosition = 1.0;

        private Wallet _wallet;
        private StatUpgrades _upgrades;
        private Battle _battle;
        private StageProgress _stage;

        private int _changedCount;
        private List<int> _cleared;
        private List<int> _failed;

        /// <summary>알림 순서 기록 ("Changed", "Cleared", "Failed", "Started").</summary>
        private List<string> _events;

        private StagePhase _phaseAtCleared;
        private int _stageNumberAtCleared;
        private StagePhase _phaseAtFailed;

        private void Create(long attack, long maxHealth, int startStage = 1)
        {
            _wallet = new Wallet(0);
            _upgrades = new StatUpgrades(_wallet, attack, maxHealth);
            _battle = new Battle(_upgrades);
            _stage = new StageProgress(_wallet, _upgrades, _battle, new MinRandom(), startStage);

            _changedCount = 0;
            _cleared = new List<int>();
            _failed = new List<int>();
            _events = new List<string>();

            _stage.Changed += () =>
            {
                _changedCount++;
                _events.Add("Changed");
            };
            _stage.StageCleared += n =>
            {
                _cleared.Add(n);
                _events.Add("Cleared");
                _phaseAtCleared = _stage.Phase;
                _stageNumberAtCleared = _stage.StageNumber;
            };
            _stage.StageFailed += n =>
            {
                _failed.Add(n);
                _events.Add("Failed");
                _phaseAtFailed = _stage.Phase;
            };
            _stage.StageStarted += n => _events.Add("Started");
        }

        private void AddNormals(int count, int stageNumber = 1)
        {
            for (int i = 0; i < count; i++)
            {
                _battle.AddEnemy(EnemyKind.Normal, stageNumber, NearPosition);
            }
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

        [Test]
        public void BossDefeated_StageClearedAndTransitionStarted()
        {
            ArrangeClearedState();

            Assert.That(_wallet.Balance, Is.EqualTo(50));
            Assert.That(_cleared, Is.EqualTo(new[] { 1 }));
            Assert.That(_failed, Is.Empty);
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(_stage.StageNumber, Is.EqualTo(1));
            Assert.That(_stage.FadeAlpha, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(_battle.Paused, Is.True);
            Assert.That(_upgrades.Locked, Is.True);
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(96));
        }

        [Test]
        public void BossDefeated_StageClearedSeesFadingOutAndChangedFollows()
        {
            ArrangeClearedState();

            Assert.That(_phaseAtCleared, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(_stageNumberAtCleared, Is.EqualTo(1));
            Assert.That(_events, Is.EqualTo(new[] { "Started", "Changed", "Changed", "Cleared", "Changed" }));
            Assert.That(_changedCount, Is.EqualTo(3));
        }

        [Test]
        public void DiscipleDefeated_StageFailedAndTransitionStarted()
        {
            Create(10, 5);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            AddNormals(1);

            _battle.Step();

            Assert.That(_battle.IsDiscipleDefeated, Is.True);
            Assert.That(_wallet.Balance, Is.EqualTo(0));
            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(0));
            Assert.That(_failed, Is.EqualTo(new[] { 1 }));
            Assert.That(_cleared, Is.Empty);
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(_stage.StageNumber, Is.EqualTo(1));
            Assert.That(_battle.Paused, Is.True);
            Assert.That(_upgrades.Locked, Is.True);
            Assert.That(_phaseAtFailed, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(_events, Is.EqualTo(new[] { "Started", "Changed", "Changed", "Failed", "Changed" }));
        }

        [Test]
        public void NormalAndDiscipleDefeatedTogether_KillCountedThenFailed()
        {
            Create(30, 5);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            AddNormals(1);

            _battle.Step();

            Assert.That(_wallet.Balance, Is.EqualTo(10));
            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(1));
            Assert.That(_failed.Count, Is.EqualTo(1));
            Assert.That(_cleared, Is.Empty);
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
        }

        [Test]
        public void BossAndDiscipleDefeatedTogether_FailedWithoutBossGold()
        {
            Create(100, 4);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            _battle.AddEnemy(EnemyKind.Boss, 1, BossPosition);

            _battle.Step();

            Assert.That(_battle.IsDiscipleDefeated, Is.True);
            Assert.That(_wallet.Balance, Is.EqualTo(0));
            Assert.That(_cleared, Is.Empty);
            Assert.That(_failed, Is.EqualTo(new[] { 1 }));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
        }

        [Test]
        public void TenthNormalAndDiscipleDefeatedTogether_NoBossAndFailed()
        {
            Create(1000, 50);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            AddNormals(10);

            _battle.Step();

            Assert.That(_wallet.Balance, Is.EqualTo(100));
            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(10));
            Assert.That(_stage.Boss, Is.Null);
            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
            Assert.That(_failed.Count, Is.EqualTo(1));
            Assert.That(_cleared, Is.Empty);
        }

        [Test]
        public void UpgradeDuringFadingOut_RejectedAndGoldUnchanged()
        {
            ArrangeClearedState();

            bool upgraded = _upgrades.TryUpgrade(StatType.Attack);

            Assert.That(upgraded, Is.False);
            Assert.That(_wallet.Balance, Is.EqualTo(50));
        }

        [Test]
        public void NormalDefeatedDuringFadingOut_Ignored()
        {
            ArrangeClearedState();
            int changedBefore = _changedCount;
            _battle.SetPaused(false);
            Enemy enemy = _battle.AddEnemy(EnemyKind.Normal, 1, NearPosition);

            for (int i = 0; i < 60 && !enemy.IsDead; i++)
            {
                _battle.Step();
            }

            Assert.That(enemy.IsDead, Is.True);
            Assert.That(_wallet.Balance, Is.EqualTo(50));
            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(0));
            Assert.That(_changedCount, Is.EqualTo(changedBefore));
            Assert.That(_cleared.Count, Is.EqualTo(1));
            Assert.That(_failed, Is.Empty);
        }

        [Test]
        public void BossDefeatedOnMaxStage_NoOverflowAndCleared()
        {
            _wallet = new Wallet(0);
            _upgrades = new StatUpgrades(_wallet, 100000000000, 100000000000);
            _battle = new Battle(_upgrades);
            _stage = new StageProgress(_wallet, _upgrades, _battle, new MinRandom(), int.MaxValue);
            var cleared = new List<int>();
            _stage.StageCleared += n => cleared.Add(n);

            Assert.DoesNotThrow(() =>
            {
                _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
                _battle.AddEnemy(EnemyKind.Boss, int.MaxValue, BossPosition);
                _battle.Step();
            });

            Assert.That(_wallet.Balance, Is.EqualTo(21474836510L));
            Assert.That(cleared, Is.EqualTo(new[] { int.MaxValue }));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(_stage.StageNumber, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void FullStagePlayed_SixGroupsThenBossCleared()
        {
            Create(1000, 100000);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);

            for (int i = 0; i < 20000 && _cleared.Count == 0; i++)
            {
                if (_stage.Phase == StagePhase.FadingOut || _stage.Phase == StagePhase.FadingIn) _stage.Tick(StageProgress.FadeSeconds * 2);
                else if (_stage.IsSubstageIntroShowing) _stage.Tick(StageProgress.SubstageIntroSeconds);
                else _battle.Step();
            }

            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(_wallet.Balance, Is.EqualTo(170));
            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(12));
            Assert.That(_cleared, Is.EqualTo(new[] { 1 }));
            Assert.That(_failed, Is.Empty);
            Assert.That(_stage.IsBossActive, Is.True);
            Assert.That(_stage.BarRatio, Is.EqualTo(0.0).Within(Tolerance));
        }
    }
}
