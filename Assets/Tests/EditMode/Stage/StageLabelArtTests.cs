using System;
using System.Globalization;
using NUnit.Framework;
using PlumSword.Core.Stage;

namespace PlumSword.Core.Tests.Stage
{
    public class StageLabelArtTests
    {
        [Test]
        public void Paths_ValidStages_ReturnInvariantPaths()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
                Assert.AreEqual("Art/UI/Substage/Label/title_1", StageLabelArt.TitlePath(1));
                Assert.AreEqual("Art/UI/Substage/Label/boss_name_5", StageLabelArt.BossNamePath(5));
                Assert.AreEqual("Art/UI/Substage/Label/header_blank", StageLabelArt.HeaderBlankPath);
                Assert.AreEqual("Art/UI/Substage/Label/stage_numbers", StageLabelArt.NumberSheetPath);
            }
            finally { CultureInfo.CurrentCulture = original; }
        }

        [Test]
        public void Paths_InvalidStages_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.TitlePath(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.TitlePath(6));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.BossNamePath(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.BossNamePath(6));
        }

        [TestCase(1, 1, false)] [TestCase(4, 6, false)]
        [TestCase(1, 7, true)] [TestCase(4, 7, true)]
        [TestCase(5, 7, true)] [TestCase(5, 1, true)]
        public void UsesBossName_ValidStageAndSubstage_SelectsArt(int stage, int substage, bool expected)
        {
            Assert.AreEqual(expected, StageLabelArt.UsesBossName(stage, substage));
        }

        [TestCase(0, 1)] [TestCase(6, 1)] [TestCase(1, 0)] [TestCase(1, 8)]
        public void UsesBossName_InvalidInput_Throws(int stage, int substage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.UsesBossName(stage, substage));
        }

        [Test]
        public void NumberGlyphs_NormalSubstages_ReturnFreshArrays()
        {
            CollectionAssert.AreEqual(new[] { 1, 10, 1 }, StageLabelArt.NumberGlyphs(1, 1));
            CollectionAssert.AreEqual(new[] { 4, 10, 6 }, StageLabelArt.NumberGlyphs(4, 6));
            Assert.AreNotSame(StageLabelArt.NumberGlyphs(1, 1), StageLabelArt.NumberGlyphs(1, 1));
        }

        [TestCase(5, 7)] [TestCase(5, 1)] [TestCase(1, 7)] [TestCase(0, 1)]
        [TestCase(6, 1)] [TestCase(1, 0)] [TestCase(1, 8)]
        public void NumberGlyphs_InvalidOrBossInput_Throws(int stage, int substage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.NumberGlyphs(stage, substage));
        }

        [Test]
        public void GlyphMetrics_AllGlyphs_ReturnSheetCoordinates()
        {
            int[] left = { 20, 225, 397, 594, 784, 985, 1190, 1382, 1574, 1766, 1962 };
            int[] widths = { 185, 152, 177, 170, 181, 185, 172, 172, 172, 176, 150 };
            for (int i = 0; i < left.Length; i++)
            {
                Assert.AreEqual(left[i], StageLabelArt.GlyphLeft(i));
                Assert.AreEqual(widths[i], StageLabelArt.GlyphWidth(i));
            }
            Assert.AreEqual(10, StageLabelArt.HyphenGlyph);
            Assert.AreEqual(8, StageLabelArt.GlyphTop);
            Assert.AreEqual(290, StageLabelArt.GlyphHeight);
            Assert.AreEqual(18, StageLabelArt.GlyphOverlap);
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.GlyphLeft(11));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.GlyphWidth(-1));
        }

        [Test]
        public void LabelWidth_ValidGlyphs_AppliesOverlapAndScale()
        {
            Assert.AreEqual(418, StageLabelArt.LabelWidth(new[] { 1, 10, 1 }, 1));
            Assert.AreEqual(233.5, StageLabelArt.LabelWidth(new[] { 4, 10, 6 }, 0.5));
            Assert.AreEqual(0, StageLabelArt.LabelWidth(new int[0], 1));
        }

        [Test]
        public void LabelWidth_InvalidInputs_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => StageLabelArt.LabelWidth(null, 1));
            foreach (double scale in new[] { 0.0, -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.LabelWidth(new[] { 1 }, scale));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.LabelWidth(new[] { 11 }, 1));
        }

        [Test]
        public void GlyphCenterX_ValidGlyphs_CentersTheWholeLabel()
        {
            int[] first = { 1, 10, 1 };
            int[] second = { 4, 10, 6 };
            double[] firstCenters = { -133, 0, 133 };
            double[] secondCenters = { -143, 4.5, 147.5 };
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(firstCenters[i], StageLabelArt.GlyphCenterX(first, i, 1), 1e-9);
                Assert.AreEqual(secondCenters[i], StageLabelArt.GlyphCenterX(second, i, 1), 1e-9);
                Assert.AreEqual(secondCenters[i] * 0.5, StageLabelArt.GlyphCenterX(second, i, 0.5), 1e-9);
            }
        }

        [Test]
        public void GlyphCenterX_InvalidInputs_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.GlyphCenterX(new[] { 1, 10, 1 }, 3, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.GlyphCenterX(new[] { 1 }, -1, 1));
            Assert.Throws<ArgumentNullException>(() => StageLabelArt.GlyphCenterX(null, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.GlyphCenterX(new[] { 1 }, 0, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLabelArt.GlyphCenterX(new[] { -1 }, 0, 1));
        }
    }
}
