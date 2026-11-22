using System;
using System.Globalization;

namespace PlumSword.Core.Stage
{
    public static class StageLabelArt
    {
        public const string HeaderBlankPath = "Art/UI/Substage/Label/header_blank";
        public const string NumberSheetPath = "Art/UI/Substage/Label/stage_numbers";
        public const int HyphenGlyph = 10;
        public const int GlyphTop = 8;
        public const int GlyphHeight = 290;
        public const int GlyphOverlap = 18;

        private static readonly int[] Lefts = { 20, 225, 397, 594, 784, 985, 1190, 1382, 1574, 1766, 1962 };
        private static readonly int[] Widths = { 185, 152, 177, 170, 181, 185, 172, 172, 172, 176, 150 };

        public static string TitlePath(int stage)
        {
            ValidateStage(stage);
            return "Art/UI/Substage/Label/title_" + stage.ToString(CultureInfo.InvariantCulture);
        }

        public static string BossNamePath(int stage)
        {
            ValidateStage(stage);
            return "Art/UI/Substage/Label/boss_name_" + stage.ToString(CultureInfo.InvariantCulture);
        }

        public static bool UsesBossName(int stage, int substage)
        {
            ValidateStage(stage);
            ValidateSubstage(substage);
            return stage == 5 || substage == 7;
        }

        public static int[] NumberGlyphs(int stage, int substage)
        {
            ValidateStage(stage);
            ValidateSubstage(substage);
            if (stage == 5) throw new ArgumentOutOfRangeException(nameof(stage));
            if (substage == 7) throw new ArgumentOutOfRangeException(nameof(substage));
            return new[] { stage, HyphenGlyph, substage };
        }

        public static int GlyphLeft(int glyph)
        {
            ValidateGlyph(glyph);
            return Lefts[glyph];
        }

        public static int GlyphWidth(int glyph)
        {
            ValidateGlyph(glyph);
            return Widths[glyph];
        }

        public static double LabelWidth(int[] glyphs, double scale)
        {
            if (glyphs == null) throw new ArgumentNullException(nameof(glyphs));
            if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale))
                throw new ArgumentOutOfRangeException(nameof(scale));
            double width = 0;
            for (int i = 0; i < glyphs.Length; i++) width += GlyphWidth(glyphs[i]);
            if (glyphs.Length > 0) width -= GlyphOverlap * (glyphs.Length - 1);
            return width * scale;
        }

        public static double GlyphCenterX(int[] glyphs, int index, double scale)
        {
            double width = LabelWidth(glyphs, scale);
            if (index < 0 || index >= glyphs.Length) throw new ArgumentOutOfRangeException(nameof(index));
            double offset = 0;
            for (int i = 0; i < index; i++) offset += GlyphWidth(glyphs[i]) - GlyphOverlap;
            return -width / 2 + (offset + GlyphWidth(glyphs[index]) / 2.0) * scale;
        }

        private static void ValidateStage(int stage)
        {
            if (stage < 1 || stage > 5) throw new ArgumentOutOfRangeException(nameof(stage));
        }

        private static void ValidateSubstage(int substage)
        {
            if (substage < 1 || substage > 7) throw new ArgumentOutOfRangeException(nameof(substage));
        }

        private static void ValidateGlyph(int glyph)
        {
            if (glyph < 0 || glyph > HyphenGlyph) throw new ArgumentOutOfRangeException(nameof(glyph));
        }
    }
}
