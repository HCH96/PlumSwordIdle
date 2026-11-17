using System;
using NUnit.Framework;
using PlumSword.Core.OfflineReward;

namespace PlumSword.Core.Tests.OfflineReward
{
    public class OfflineRewardCalculatorTests
    {
        private const long T0 = 638000000000000000L;

        [Test]
        public void Constants_MatchSpecifiedIntervals()
        {
            Assert.AreEqual(100000000L, OfflineRewardCalculator.IntervalTicks);
            Assert.AreEqual(288000000000L, OfflineRewardCalculator.MaxTicks);
        }

        [TestCase(6000000000L, 1, 6000000000L, 60L, 600L, true, false)]
        [TestCase(250000000L, 1, 200000000L, 2L, 20L, true, false)]
        [TestCase(99999999L, 1, 0L, 0L, 0L, false, false)]
        [TestCase(100000000L, 1, 100000000L, 1L, 10L, true, false)]
        [TestCase(324000000000L, 1, 288000000000L, 2880L, 28800L, true, true)]
        [TestCase(288000000000L, 1, 288000000000L, 2880L, 28800L, true, false)]
        [TestCase(6000000000L, 3, 6000000000L, 60L, 840L, true, false)]
        [TestCase(6000000000L, 6, 6000000000L, 60L, 1200L, true, false)]
        [TestCase(324000000000L, int.MaxValue, 288000000000L, 2880L, 12369505829760L, true, true)]
        public void Calculate_ElapsedAndStage_ReturnsExpectedReward(long elapsed, int stage, long applied, long kills, long gold, bool hasReward, bool capped)
        {
            var result = OfflineRewardCalculator.Calculate(T0, T0 + elapsed, stage);
            Assert.AreEqual(elapsed, result.ElapsedTicks);
            Assert.AreEqual(applied, result.AppliedTicks);
            Assert.AreEqual(kills, result.KillCount);
            Assert.AreEqual(gold, result.Gold);
            Assert.AreEqual(hasReward, result.HasReward);
            Assert.AreEqual(capped, result.IsCapped);
        }

        [TestCase(T0, T0)]
        [TestCase(T0, T0 - 1)]
        [TestCase(-1L, T0)]
        [TestCase(T0, -1L)]
        [TestCase(long.MinValue, long.MaxValue)]
        [TestCase(long.MaxValue, long.MinValue)]
        public void Calculate_InvalidOrEqualTimes_ReturnsNoReward(long saved, long now)
        {
            var result = OfflineRewardCalculator.Calculate(saved, now, 1);
            Assert.AreEqual(0L, result.ElapsedTicks);
            Assert.AreEqual(0L, result.AppliedTicks);
            Assert.AreEqual(0L, result.KillCount);
            Assert.AreEqual(0L, result.Gold);
            Assert.IsFalse(result.HasReward);
            Assert.IsFalse(result.IsCapped);
        }

        [Test]
        public void Calculate_ZeroSavedTime_CapsElapsedTime()
        {
            var result = OfflineRewardCalculator.Calculate(0, T0, 1);
            Assert.AreEqual(T0, result.ElapsedTicks);
            Assert.AreEqual(288000000000L, result.AppliedTicks);
            Assert.AreEqual(2880L, result.KillCount);
            Assert.AreEqual(28800L, result.Gold);
            Assert.IsTrue(result.HasReward);
            Assert.IsTrue(result.IsCapped);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Calculate_InvalidStage_ThrowsEvenWithNoElapsedTime(int stage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => OfflineRewardCalculator.Calculate(T0, T0, stage));
        }

        [TestCase(T0, T0 + 100000000L, true, true)]
        [TestCase(T0, T0 + 99999999L, true, false)]
        [TestCase(T0, T0 + 100000000L, false, false)]
        [TestCase(T0, T0 - 1, true, false)]
        [TestCase(-1L, T0, true, false)]
        [TestCase(T0, -1L, true, false)]
        [TestCase(long.MinValue, long.MaxValue, true, false)]
        public void ShouldRestartOnResume_TimesAndSaveState_ReturnsExpected(long paused, long resumed, bool upToDate, bool expected)
        {
            Assert.AreEqual(expected, OfflineRewardCalculator.ShouldRestartOnResume(paused, resumed, upToDate));
        }
    }
}
