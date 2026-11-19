using System;
using NUnit.Framework;
using PlumSword.Core.Combat;

namespace PlumSword.Core.Tests.Combat
{
    public class CombatLayoutTests
    {
        private const double Tolerance = 1e-9;

        [TestCase(0.0, 0.6)]
        [TestCase(0.125, 0.3)]
        [TestCase(0.25, 0.0)]
        [TestCase(0.3, 0.0)]
        [TestCase(-0.1, 0.6)]
        public void SkillSlashAlpha_ElapsedTime_ClampsAndFades(double elapsed, double expected)
        {
            Assert.AreEqual(expected, CombatLayout.SkillSlashAlpha(elapsed), Tolerance);
        }

        [Test]
        public void SkillSlashAlpha_NaN_IsZero()
        {
            Assert.AreEqual(0.0, CombatLayout.SkillSlashAlpha(double.NaN));
        }

        [Test]
        public void SkillSlashSeconds_IsQuarterSecond()
        {
            Assert.AreEqual(0.25, CombatLayout.SkillSlashSeconds);
        }

        [TestCase(0, -0.15)]
        [TestCase(1, 0.0)]
        [TestCase(2, 0.15)]
        [TestCase(3, -0.15)]
        [TestCase(4, 0.0)]
        public void EnemyOffsetY_ByIndex_RepeatsEveryThree(int index, double expected)
        {
            Assert.AreEqual(expected, CombatLayout.EnemyOffsetY(index), Tolerance);
        }

        [Test]
        public void EnemyOffsetY_NegativeIndex_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CombatLayout.EnemyOffsetY(-1));
        }

        [TestCase(50L, 100L, 0.5)]
        [TestCase(0L, 100L, 0.0)]
        [TestCase(100L, 100L, 1.0)]
        [TestCase(30L, 40L, 0.75)]
        public void HealthRatio_WithinRange_IsHealthOverMax(long health, long maxHealth, double expected)
        {
            Assert.AreEqual(expected, CombatLayout.HealthRatio(health, maxHealth), Tolerance);
        }

        [Test]
        public void HealthRatio_HealthAboveMax_ClampsToOne()
        {
            Assert.AreEqual(1.0, CombatLayout.HealthRatio(150, 100), Tolerance);
        }

        [Test]
        public void HealthRatio_NegativeHealth_ClampsToZero()
        {
            Assert.AreEqual(0.0, CombatLayout.HealthRatio(-5, 100), Tolerance);
        }

        [TestCase(10L, 0L)]
        [TestCase(10L, -5L)]
        public void HealthRatio_MaxHealthNotPositive_IsZero(long health, long maxHealth)
        {
            Assert.AreEqual(0.0, CombatLayout.HealthRatio(health, maxHealth), Tolerance);
        }
    }
}
