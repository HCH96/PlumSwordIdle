using System;
using NUnit.Framework;
using PlumSword.Core.Stage;

namespace PlumSword.Core.Tests.Stage
{
    public class StageRewardsTests
    {
        // NormalGold

        [TestCase(1, 10L)]
        [TestCase(2, 12L)]
        [TestCase(3, 14L)]
        public void NormalGold_Stage_ReturnsTenPlusTwoPerStage(int stage, long expected)
        {
            Assert.That(StageRewards.NormalGold(stage), Is.EqualTo(expected));
        }

        [Test]
        public void NormalGold_IntMaxValue_ComputesInLongWithoutOverflow()
        {
            Assert.That(StageRewards.NormalGold(int.MaxValue), Is.EqualTo(4294967302L));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NormalGold_StageBelowOne_ThrowsArgumentOutOfRange(int stage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StageRewards.NormalGold(stage));
        }

        // BossGold

        [TestCase(1, 50L)]
        [TestCase(2, 60L)]
        [TestCase(3, 70L)]
        public void BossGold_Stage_ReturnsFiftyPlusTenPerStage(int stage, long expected)
        {
            Assert.That(StageRewards.BossGold(stage), Is.EqualTo(expected));
        }

        [Test]
        public void BossGold_IntMaxValue_ComputesInLongWithoutOverflow()
        {
            Assert.That(StageRewards.BossGold(int.MaxValue), Is.EqualTo(21474836510L));
        }

        [Test]
        public void BossGold_StageZero_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StageRewards.BossGold(0));
        }
    }
}
