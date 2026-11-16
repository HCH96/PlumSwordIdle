using System.Globalization;
using NUnit.Framework;
using PlumSword.Core.Stage;

namespace PlumSword.Core.Tests.Stage
{
    public class StageTextTests
    {
        // StageLabel

        [TestCase(1, "STAGE 1")]
        [TestCase(12, "STAGE 12")]
        [TestCase(0, "STAGE 0")]
        public void StageLabel_Stage_PrefixesStageAndSpace(int stage, string expected)
        {
            string result = null;
            Assert.DoesNotThrow(() => result = StageText.StageLabel(stage));
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void StageLabel_Negative_WritesAsIsWithoutException()
        {
            string result = null;
            Assert.DoesNotThrow(() => result = StageText.StageLabel(-3));
            Assert.That(result, Is.EqualTo("STAGE -3"));
        }

        [Test]
        public void StageLabel_CurrentCultureIsGerman_HasNoDigitSeparator()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.That(StageText.StageLabel(1234), Is.EqualTo("STAGE 1234"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        // BossName

        [Test]
        public void BossName_Always_IsGatekeeper()
        {
            Assert.That(StageText.BossName, Is.EqualTo("GATEKEEPER"));
        }
    }
}
