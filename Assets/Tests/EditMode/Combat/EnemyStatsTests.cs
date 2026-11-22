using System;
using NUnit.Framework;
using PlumSword.Core.Combat;

namespace PlumSword.Core.Tests.Combat
{
    public class EnemyStatsTests
    {
        private const double Tolerance = 1e-9;
        private const EnemyKind Undefined = (EnemyKind)99;

        [TestCase(1, 30L)]
        [TestCase(2, 40L)]
        [TestCase(3, 50L)]
        public void MaxHealth_NormalByStage_IncreasesByTen(int stage, long expected)
        {
            Assert.AreEqual(expected, EnemyStats.MaxHealth(EnemyKind.Normal, stage));
        }

        [TestCase(1, 5L)]
        [TestCase(2, 7L)]
        [TestCase(3, 9L)]
        public void Attack_NormalByStage_IncreasesByTwo(int stage, long expected)
        {
            Assert.AreEqual(expected, EnemyStats.Attack(EnemyKind.Normal, stage));
        }

        [TestCase(1, 100L)]
        [TestCase(2, 130L)]
        [TestCase(3, 160L)]
        public void MaxHealth_BossByStage_IncreasesByThirty(int stage, long expected)
        {
            Assert.AreEqual(expected, EnemyStats.MaxHealth(EnemyKind.Boss, stage));
        }

        [TestCase(1, 4L)]
        [TestCase(2, 8L)]
        [TestCase(3, 12L)]
        public void Attack_BossByStage_IncreasesByFour(int stage, long expected)
        {
            Assert.AreEqual(expected, EnemyStats.Attack(EnemyKind.Boss, stage));
        }

        [Test]
        public void MaxHealth_NormalAtIntMaxStage_DoesNotOverflow()
        {
            Assert.AreEqual(21474836490L, EnemyStats.MaxHealth(EnemyKind.Normal, int.MaxValue));
        }

        [Test]
        public void Attack_BossAtIntMaxStage_DoesNotOverflow()
        {
            Assert.AreEqual(8589934588L, EnemyStats.Attack(EnemyKind.Boss, int.MaxValue));
        }

        [TestCase(EnemyKind.Normal)]
        [TestCase(EnemyKind.Boss)]
        public void AttackInterval_AnyKind_IsTwoSeconds(EnemyKind kind)
        {
            Assert.AreEqual(2.0, EnemyStats.AttackInterval(kind), Tolerance);
        }

        [Test]
        public void AttackRange_Normal_IsPointEight()
        {
            Assert.AreEqual(0.8, EnemyStats.AttackRange(EnemyKind.Normal), Tolerance);
        }

        [Test]
        public void AttackRange_Boss_IsOne()
        {
            Assert.AreEqual(1.0, EnemyStats.AttackRange(EnemyKind.Boss), Tolerance);
        }

        [Test]
        public void NormalReactRange_Constant_IsThree()
        {
            Assert.AreEqual(3.0, EnemyStats.NormalReactRange, Tolerance);
        }

        [Test]
        public void NormalMoveSpeed_Constant_IsOne()
        {
            Assert.AreEqual(1.0, EnemyStats.NormalMoveSpeed, Tolerance);
        }

        [Test]
        public void MaxHealth_NormalStageZero_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyStats.MaxHealth(EnemyKind.Normal, 0));
        }

        [Test]
        public void Attack_BossStageZero_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyStats.Attack(EnemyKind.Boss, 0));
        }

        [Test]
        public void MaxHealth_NormalNegativeStage_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyStats.MaxHealth(EnemyKind.Normal, -1));
        }

        [Test]
        public void MaxHealth_UndefinedKind_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyStats.MaxHealth(Undefined, 1));
        }

        [Test]
        public void Attack_UndefinedKind_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyStats.Attack(Undefined, 1));
        }

        [Test]
        public void AttackInterval_UndefinedKind_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyStats.AttackInterval(Undefined));
        }

        [Test]
        public void AttackRange_UndefinedKind_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyStats.AttackRange(Undefined));
        }
    }
}
