using System;
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

        [TestCase(2, 1, "ROUND 2 · STAGE 1 · JONGNAM")]
        [TestCase(1, 5, "ROUND 1 · STAGE 5 · HEAVENLY DEMON")]
        public void RoundStageLabel_ValidInputs_FormatsRoundStageAndTheme(int round, int stage, string expected)
        {
            Assert.That(StageText.RoundStageLabel(round, stage), Is.EqualTo(expected));
        }

        [Test]
        public void RoundStageLabel_CurrentCultureIsGerman_UsesInvariantNumbers()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.That(StageText.RoundStageLabel(1234, 3), Is.EqualTo("ROUND 1234 · STAGE 3 · NAMGUNG"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [TestCase(1, "JONGNAM")]
        [TestCase(2, "BEGGAR CLAN")]
        [TestCase(3, "NAMGUNG")]
        [TestCase(4, "DEMON CULT")]
        [TestCase(5, "HEAVENLY DEMON")]
        public void ThemeName_ValidStage_ReturnsTheme(int stage, string expected)
        {
            Assert.That(StageText.ThemeName(stage), Is.EqualTo(expected));
        }

        [TestCase(1, "JONGNAM MASTER")]
        [TestCase(2, "BEGGAR CLAN MASTER")]
        [TestCase(3, "NAMGUNG SWORDSMAN")]
        [TestCase(4, "DEMON CULT COMMANDER")]
        [TestCase(5, "HEAVENLY DEMON")]
        public void ThemedBossName_ValidStage_ReturnsBoss(int stage, string expected)
        {
            Assert.That(StageText.ThemedBossName(stage), Is.EqualTo(expected));
        }

        [TestCase(0)]
        [TestCase(6)]
        public void ThemeName_InvalidStage_Throws(int stage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StageText.ThemeName(stage));
        }

        [TestCase(0)]
        [TestCase(6)]
        public void ThemedBossName_InvalidStage_Throws(int stage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StageText.ThemedBossName(stage));
        }

        [TestCase(1, "A Mount Hua disciple faces a first sparring match at Jongnam.")]
        [TestCase(2, "Pass the Beggar Clan's trial and hear of the Demon Cult's movements.")]
        [TestCase(3, "Cross swords with Namgung to prepare for the battle to come.")]
        [TestCase(4, "The battle to hold back the Demon Cult's army begins.")]
        [TestCase(5, "At the end of the war, the Heavenly Demon awaits.")]
        public void IntroLine_ValidStage_ReturnsExactLine(int stage, string expected)
        {
            Assert.That(StageText.IntroLine(stage), Is.EqualTo(expected));
        }

        [TestCase(0)]
        [TestCase(6)]
        public void IntroLine_InvalidStage_Throws(int stage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StageText.IntroLine(stage));
        }

        [TestCase(1, 0)]
        [TestCase(1, 6)]
        [TestCase(0, 1)]
        [TestCase(-1, 1)]
        public void RoundStageLabel_InvalidInput_Throws(int round, int stage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StageText.RoundStageLabel(round, stage));
        }
    }
}
