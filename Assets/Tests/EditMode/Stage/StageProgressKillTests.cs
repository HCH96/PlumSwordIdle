using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Stage
{
    /// <summary>016_stage_progress_kills: 일반 몬스터 처치 금화·처치 수, 보스 등장, 보스 체력 바.</summary>
    public class StageProgressKillTests
    {
        private const double Tolerance = 1e-6;
        private const double NearPosition = 0.8;

        private Wallet _wallet;
        private StatUpgrades _upgrades;
        private Battle _battle;
        private StageProgress _stage;
        private int _changedCount;
        private List<long> _balances;

        private void Create(long attack, long maxHealth, int startStage = 1)
        {
            _wallet = new Wallet(0);
            _upgrades = new StatUpgrades(_wallet, attack, maxHealth);
            _battle = new Battle(_upgrades);
            _stage = new StageProgress(_wallet, _upgrades, _battle, new MinRandom(), startStage);
            _changedCount = 0;
            _balances = new List<long>();
            _stage.Changed += () => _changedCount++;
            _wallet.BalanceChanged += b => _balances.Add(b);
        }

        private void AddNormals(int count, int stageNumber = 1)
        {
            for (int i = 0; i < count; i++)
            {
                _battle.AddEnemy(EnemyKind.Normal, stageNumber, NearPosition);
            }
        }

        private void AdvanceToBoss()
        {
            for (int step = 0; step < 30000 && !_stage.IsBossActive; step++)
            {
                if (_stage.IsSubstageIntroShowing) _stage.Tick(StageProgress.SubstageIntroSeconds);
                else _battle.Step();
            }
            Assert.That(_stage.IsBossActive, Is.True);
        }

        [Test]
        public void NormalDefeatedBeforeBegin_Ignored()
        {
            Create(1000, 100);
            AddNormals(1);

            _battle.Step();

            Assert.That(_wallet.Balance, Is.EqualTo(0));
            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(0));
            Assert.That(_changedCount, Is.EqualTo(0));
        }

        [Test]
        public void ThreeNormalsDefeatedInOneStep_GoldCountAndChangedThreeTimes()
        {
            Create(1000, 100);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            Assert.That(_changedCount, Is.EqualTo(2));
            AddNormals(3);

            _battle.Step();

            Assert.That(_wallet.Balance, Is.EqualTo(30));
            Assert.That(_balances, Is.EqualTo(new long[] { 10, 20, 30 }));
            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(3));
            Assert.That(_stage.BarRatio, Is.EqualTo(0).Within(Tolerance));
            Assert.That(_stage.IsBossActive, Is.False);
            Assert.That(_changedCount, Is.EqualTo(2 + 3));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.Playing));
        }

        [Test]
        public void NormalDefeatedOnStageTwo_GivesStageTwoGold()
        {
            Create(1000, 100, startStage: 2);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            AddNormals(1, stageNumber: 2);

            _battle.Step();

            Assert.That(_wallet.Balance, Is.EqualTo(12));
        }

        [Test]
        public void SixGroupsDefeated_BossAppears()
        {
            Create(1000, 10000);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            AdvanceToBoss();

            Assert.That(_wallet.Balance, Is.EqualTo(120));
            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(12));
            Assert.That(_stage.IsBossActive, Is.True);
            Assert.That(_stage.Boss, Is.Not.Null);
            Assert.That(_stage.Boss.Kind, Is.EqualTo(EnemyKind.Boss));
            Assert.That(_stage.Boss.Position, Is.EqualTo(_battle.DisciplePosition + 18.0).Within(Tolerance));
            Assert.That(_stage.Boss.MaxHealth, Is.EqualTo(100));
            Assert.That(_stage.BarRatio, Is.EqualTo(1.0).Within(Tolerance));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(1));
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.SubstageIntro));
        }

        [Test]
        public void ExtraNormalsDoNotAdvanceCurrentGroup()
        {
            Create(1000, 100);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            AddNormals(11);

            _battle.Step();

            Assert.That(_wallet.Balance, Is.EqualTo(110));
            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(11));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(45));
            Assert.That(_changedCount, Is.EqualTo(2 + 11));
            Assert.That(_stage.SubstageNumber, Is.EqualTo(1));
        }

        [Test]
        public void TenthNormalAndDiscipleDefeatedTogether_NoBoss()
        {
            Create(1000, 50);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            AddNormals(10);

            _battle.Step();

            Assert.That(_battle.IsDiscipleDefeated, Is.True);
            Assert.That(_wallet.Balance, Is.EqualTo(100));
            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(10));
            Assert.That(_stage.Boss, Is.Null);
            Assert.That(_stage.IsBossActive, Is.False);
            Assert.That(_stage.BarRatio, Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(2));
        }

        [Test]
        public void BossDamaged_BarRatioIsBossHealthAndChangedSent()
        {
            Create(30, 100000);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            AdvanceToBoss();
            Assert.That(_stage.Boss, Is.Not.Null);
            _stage.Tick(StageProgress.SubstageIntroSeconds);

            int lastStepChanged = 0;
            for (int i = 0; i < 5000 && _stage.Boss.Health >= 100; i++)
            {
                int before = _changedCount;
                _battle.Step();
                lastStepChanged = _changedCount - before;
            }

            Assert.That(_stage.Boss.Health, Is.EqualTo(70));
            Assert.That(_stage.BarRatio, Is.EqualTo(0.7).Within(Tolerance));
            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(12));
            Assert.That(lastStepChanged, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void NonBossEnemyDamaged_NoChanged()
        {
            // 공격력 1 이면 일반 몬스터는 한 대에 쓰러지지 않고 체력만 준다.
            Create(1, 100000);
            _stage.Begin();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            AddNormals(1);
            int before = _changedCount;

            _battle.Step();

            Assert.That(_stage.DefeatedNormalCount, Is.EqualTo(0));
            Assert.That(_changedCount, Is.EqualTo(before));
        }
    }
}
