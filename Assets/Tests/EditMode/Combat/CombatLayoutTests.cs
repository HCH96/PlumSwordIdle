using System;
using NUnit.Framework;
using PlumSword.Core.Combat;

namespace PlumSword.Core.Tests.Combat
{
    public class CombatLayoutTests
    {
        private const double Tolerance = 1e-9;

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
