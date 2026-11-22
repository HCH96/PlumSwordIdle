using NUnit.Framework;
using PlumSword.Core.Balance;
using PlumSword.Core.Combat;
using PlumSword.Core.Stage;

namespace PlumSword.Core.Tests.Balance
{
    public class BalanceTunedDefaultsTests
    {
        [SetUp] public void SetUp() => BalanceTable.Current = BalanceTable.Default;
        [TearDown] public void TearDown() => BalanceTable.Current = BalanceTable.Default;

        [TestCase(0, 4L)]
        [TestCase(1, 8L)]
        [TestCase(2, 12L)]
        [TestCase(3, 16L)]
        public void BossAttackCurve_DefaultValues(int index, long expected) =>
            Assert.That(BalanceTable.Current.BossAttack.Evaluate(index), Is.EqualTo(expected));

        [Test]
        public void FourthStage_BossUsesCurves()
        {
            Assert.That(EnemyStats.Attack(EnemyKind.Boss, 4), Is.EqualTo(16));
            Assert.That(EnemyStats.MaxHealth(EnemyKind.Boss, 4), Is.EqualTo(190));
        }

        [TestCase(5)]
        [TestCase(10)]
        public void FinalStages_BossUsesFixedStats(int stage)
        {
            Assert.That(EnemyStats.MaxHealth(EnemyKind.Boss, stage), Is.EqualTo(1500));
            Assert.That(EnemyStats.Attack(EnemyKind.Boss, stage), Is.EqualTo(80));
        }

        [Test]
        public void SixthStage_BossUsesAttackCurve() =>
            Assert.That(EnemyStats.Attack(EnemyKind.Boss, 6), Is.EqualTo(24));

        [Test]
        public void NormalAttackAndBossGold_AreUnchanged()
        {
            Assert.That(EnemyStats.Attack(EnemyKind.Normal, 4), Is.EqualTo(11));
            Assert.That(StageRewards.BossGold(5), Is.EqualTo(90));
        }
    }
}
