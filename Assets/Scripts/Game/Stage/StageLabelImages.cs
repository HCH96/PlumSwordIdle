using System.Globalization;
using PlumSword.Core.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Stage
{
    internal sealed class StageLabelImages
    {
        private static Texture2D cachedTexture;
        private static Sprite[] cachedGlyphs;
        public Sprite Header { get; private set; }
        public Sprite[] Titles { get; private set; }
        private Sprite[] bosses;
        private Sprite[] glyphs;
        private int lastStage, lastSubstage;

        public static StageLabelImages Load(bool includeHeaderAndTitles)
        {
            var sheet = Resources.Load<Texture2D>(StageLabelArt.NumberSheetPath);
            if (sheet == null || sheet.width != 2132 || sheet.height != 300) return null;
            var result = new StageLabelImages { bosses = new Sprite[5] };
            if (includeHeaderAndTitles)
            {
                result.Header = Resources.Load<Sprite>(StageLabelArt.HeaderBlankPath);
                result.Titles = new Sprite[5];
                if (result.Header == null) return null;
            }
            for (int i = 0; i < 5; i++)
            {
                result.bosses[i] = Resources.Load<Sprite>(StageLabelArt.BossNamePath(i + 1));
                if (result.bosses[i] == null) return null;
                if (includeHeaderAndTitles)
                {
                    result.Titles[i] = Resources.Load<Sprite>(StageLabelArt.TitlePath(i + 1));
                    if (result.Titles[i] == null) return null;
                }
            }
            if (cachedTexture != sheet || cachedGlyphs == null || cachedGlyphs[0] == null)
            {
                cachedTexture = sheet;
                cachedGlyphs = new Sprite[11];
                for (int i = 0; i < cachedGlyphs.Length; i++)
                    cachedGlyphs[i] = Sprite.Create(sheet,
                        new Rect(StageLabelArt.GlyphLeft(i), 300 - StageLabelArt.GlyphTop - StageLabelArt.GlyphHeight,
                            StageLabelArt.GlyphWidth(i), StageLabelArt.GlyphHeight), new Vector2(.5f, .5f), 32);
            }
            result.glyphs = cachedGlyphs;
            return result;
        }

        public static RectTransform MakeRect(Transform parent, string name, Vector2 anchor, Vector2 position)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            return rect;
        }

        public static Image MakeImage(Transform parent, string name, Vector2 anchor, Vector2 position)
        {
            var image = MakeRect(parent, name, anchor, position).gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        public static void SetImage(Image image, Sprite sprite, float height)
        {
            image.sprite = sprite;
            image.rectTransform.sizeDelta = new Vector2(height * sprite.rect.width / sprite.rect.height, height);
        }

        public void SetNumber(RectTransform container, Image boss, int stage, int substage, float scale, float bossHeight)
        {
            if (stage == lastStage && substage == lastSubstage) return;
            lastStage = stage;
            lastSubstage = substage;
            bool isBoss = StageLabelArt.UsesBossName(stage, substage);
            container.gameObject.SetActive(!isBoss);
            boss.gameObject.SetActive(isBoss);
            if (isBoss)
            {
                SetImage(boss, bosses[stage - 1], bossHeight);
                return;
            }
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var child = container.GetChild(i);
                child.SetParent(null, false);
                Object.Destroy(child.gameObject);
            }
            var values = StageLabelArt.NumberGlyphs(stage, substage);
            for (int i = 0; i < values.Length; i++)
            {
                int glyph = values[i];
                string name = glyph == StageLabelArt.HyphenGlyph ? "-" : glyph.ToString(CultureInfo.InvariantCulture);
                var image = MakeImage(container, name, new Vector2(.5f, .5f),
                    new Vector2((float)StageLabelArt.GlyphCenterX(values, i, scale), 0));
                image.sprite = glyphs[glyph];
                image.rectTransform.sizeDelta = new Vector2(StageLabelArt.GlyphWidth(glyph) * scale, StageLabelArt.GlyphHeight * scale);
            }
        }
    }
}
