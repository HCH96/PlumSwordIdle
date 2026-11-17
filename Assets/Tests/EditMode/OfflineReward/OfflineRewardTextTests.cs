using System;
using System.Globalization;
using NUnit.Framework;
using PlumSword.Core.OfflineReward;

namespace PlumSword.Core.Tests.OfflineReward
{
    public class OfflineRewardTextTests
    {
        [TestCase(324000000000L, "9:00:00")]
        [TestCase(6000000000L, "0:10:00")]
        [TestCase(250000000L, "0:00:25")]
        [TestCase(99999999L, "0:00:09")]
        [TestCase(0L, "0:00:00")]
        [TestCase(3600000000000L, "100:00:00")]
        public void FormatDuration_NonNegativeTicks_FormatsWholeSeconds(long ticks, string expected)
        {
            Assert.That(OfflineRewardText.FormatDuration(ticks), Is.EqualTo(expected));
        }

        [Test]
        public void FormatDuration_NegativeTicks_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => OfflineRewardText.FormatDuration(-1));
        }

        [Test]
        public void FormatDuration_OtherCulture_UsesInvariantFormatting()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
                Assert.That(OfflineRewardText.FormatDuration(3600000000000L), Is.EqualTo("100:00:00"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Test]
        public void ElapsedLine_NineHours_FormatsAwayTime()
        {
            Assert.That(OfflineRewardText.ElapsedLine(324000000000L), Is.EqualTo("AWAY 9:00:00"));
        }

        [Test]
        public void AppliedLine_EightHours_FormatsMaximumTime()
        {
            Assert.That(OfflineRewardText.AppliedLine(288000000000L), Is.EqualTo("REWARD FOR 8:00:00 (MAX)"));
        }

        [Test]
        public void GoldLine_Reward_UsesGoldFormat()
        {
            Assert.That(OfflineRewardText.GoldLine(28800), Is.EqualTo("G 28,800"));
        }

        [Test]
        public void Constants_Labels_MatchSpecifiedText()
        {
            Assert.That(OfflineRewardText.Title, Is.EqualTo("OFFLINE REWARD"));
            Assert.That(OfflineRewardText.SaveFailedMessage, Is.EqualTo("SAVE FAILED. TAP RETRY."));
            Assert.That(OfflineRewardText.ClaimButton, Is.EqualTo("CLAIM"));
            Assert.That(OfflineRewardText.RetryButton, Is.EqualTo("RETRY"));
        }

        [TestCase(OfflineRewardClaimState.SaveFailed, "RETRY")]
        [TestCase(OfflineRewardClaimState.Waiting, "CLAIM")]
        [TestCase(OfflineRewardClaimState.Done, "CLAIM")]
        [TestCase((OfflineRewardClaimState)99, "CLAIM")]
        public void ButtonLabel_State_ReturnsExpectedLabel(OfflineRewardClaimState state, string expected)
        {
            Assert.That(OfflineRewardText.ButtonLabel(state), Is.EqualTo(expected));
        }
    }
}
