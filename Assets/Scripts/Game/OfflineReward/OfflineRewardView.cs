using System;
using System.Collections.Generic;
using System.Globalization;
using PlumSword.Core.OfflineReward;
using PlumSword.Game.StartMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.OfflineReward
{
    public sealed class OfflineRewardView : MonoBehaviour
    {
        private GameObject overlay;
        private GameObject failureLine;
        private TextMeshProUGUI buttonLabel;
        private Image buttonLabelImage;
        private Sprite[] goldDigits;
        private readonly List<Sprite> lettering = new List<Sprite>();
        private bool claimed;

        public void Initialize(OfflineRewardClaim claim, Canvas canvas, Action onClaimed)
        {
            if (claim == null) throw new ArgumentNullException(nameof(claim));
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (onClaimed == null) throw new ArgumentNullException(nameof(onClaimed));

            overlay = new GameObject("OfflineRewardOverlay", typeof(RectTransform), typeof(Image));
            var overlayRect = (RectTransform)overlay.transform;
            overlayRect.SetParent(canvas.transform, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
            overlayRect.SetAsLastSibling();
            var overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.8f);
            overlayImage.raycastTarget = true;

            var window = CreateRect("OfflineRewardWindow", overlayRect, Vector2.zero, new Vector2(350, 233.33f));
            var windowImage = window.gameObject.AddComponent<Image>();
            windowImage.sprite = Resources.Load<Sprite>("Art/UI/OfflineReward/window");
            windowImage.color = windowImage.sprite != null ? Color.white : new Color(0.12f, 0.12f, 0.12f);
            var ink = new Color(1f, .90f, .83f);
            CreateTrimmedLabel("Title", window, new Vector2(0, 83), new Vector2(104, 21), "title", OfflineRewardText.Title, ink, new Rect(136, 186, 1914, 362), new Vector2(2172, 724));
            CreateTrimmedLabel("ElapsedLabel", window, new Vector2(-43, 38), new Vector2(58, 16), "elapsed", "AWAY", ink, new Rect(424, 192, 1318, 350), new Vector2(2172, 724));
            var elapsed = CreateText("Elapsed", window, new Vector2(38, 38), new Vector2(90, 20), OfflineRewardText.FormatDuration(claim.Reward.ElapsedTicks), 12, ink).GetComponent<TextMeshProUGUI>();
            FitText(elapsed, 8, 12);
            CreateTrimmedLabel("AppliedLabel", window, new Vector2(-43, 18), new Vector2(58, 16), "applied", "REWARD FOR", ink, new Rect(244, 124, 1706, 454), new Vector2(2197, 716));
            var applied = CreateText("Applied", window, new Vector2(38, 18), new Vector2(90, 20), OfflineRewardText.FormatDuration(claim.Reward.AppliedTicks), 12, ink).GetComponent<TextMeshProUGUI>();
            FitText(applied, 8, 12);
            var max = CreateLabel("Maximum", window, new Vector2(95, 18), new Vector2(30, 14), "maximum", "(MAX)", ink);
            max.gameObject.SetActive(claim.Reward.IsCapped);
            var plaque = CreateRect("RewardPlaque", window, new Vector2(0, -17), new Vector2(240, 80));
            var plaqueImage = plaque.gameObject.AddComponent<Image>();
            plaqueImage.sprite = Resources.Load<Sprite>("Art/UI/OfflineReward/reward_plaque");
            plaqueImage.color = plaqueImage.sprite != null ? Color.white : new Color(.20f, .10f, .14f);
            plaqueImage.raycastTarget = false;
            CreateGoldDisplay(window, claim.Reward.Gold, ink);
            failureLine = CreateLabel("SaveFailed", window, new Vector2(0, -46), new Vector2(180, 26), "save_failed", OfflineRewardText.SaveFailedMessage, new Color(1f, .65f, .6f)).gameObject;
            failureLine.SetActive(claim.State == OfflineRewardClaimState.SaveFailed);

            var buttonRect = CreateRect("ClaimButton", window, new Vector2(0, -72), new Vector2(180, 60));
            var buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.sprite = Resources.Load<Sprite>("Art/UI/OfflineReward/claim_button");
            buttonImage.color = buttonImage.sprite != null ? Color.white : new Color(0.45f, 0.20f, 0.20f);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            buttonRect.gameObject.AddComponent<SettingsControlEffect>().InitializeButton(button);
            var labelRect = CreateLabel("Text", buttonRect.Find("InteractionArt"), Vector2.zero, new Vector2(50, 27), "claim", OfflineRewardText.ButtonLabel(claim.State), ink);
            buttonLabelImage = labelRect.GetComponent<Image>();
            buttonLabel = labelRect.GetComponent<TextMeshProUGUI>();
            SetButtonLabel(OfflineRewardText.ButtonLabel(claim.State));
            button.onClick.AddListener(() =>
            {
                if (claimed) return;
                if (claim.TryClaim(DateTime.UtcNow.Ticks))
                {
                    claimed = true;
                    try { onClaimed(); }
                    finally { Destroy(overlay); Destroy(gameObject); }
                }
                else
                {
                    failureLine.SetActive(claim.State == OfflineRewardClaimState.SaveFailed);
                    SetButtonLabel(OfflineRewardText.ButtonLabel(claim.State));
                }
            });
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private void CreateGoldDisplay(Transform window, long amount, Color color)
        {
            string value = amount.ToString("N0", CultureInfo.InvariantCulture);
            var group = CreateRect("GoldGroup", window, new Vector2(0, -17), new Vector2(1, 26));
            var sheet = Resources.Load<Sprite>("Art/UI/damage_numbers");
            var coin = Resources.Load<Sprite>("Art/UI/StatUpgrade/coin");
            if (sheet == null || sheet.texture.width != 2172 || sheet.texture.height != 724)
            {
                var fallback = CreateText("Gold", group, Vector2.zero, new Vector2(150, 25), value, 18, color).GetComponent<TextMeshProUGUI>();
                FitText(fallback, 7, 18);
                return;
            }

            int[] left = { 33, 232, 403, 587, 770, 957, 1150, 1342, 1518, 1705, 1904 };
            int[] right = { 217, 383, 579, 756, 950, 1141, 1321, 1513, 1689, 1880, 1994 };
            goldDigits = new Sprite[left.Length];
            for (int i = 0; i < left.Length; i++)
                goldDigits[i] = Sprite.Create(sheet.texture, new Rect(left[i], 214, right[i] - left[i] + 1, 300), new Vector2(.5f, .5f), 32);

            const float height = 20f;
            float width = coin != null ? 26f : 0f;
            foreach (char c in value) width += goldDigits[c == ',' ? 10 : c - '0'].rect.width * height / 300f;
            float scale = Mathf.Min(1f, 175f / width);
            group.localScale = Vector3.one * scale;
            float x = -width / 2f;
            if (coin != null)
            {
                var icon = CreateRect("Coin", group, new Vector2(x + 10, 0), new Vector2(20, 20)).gameObject.AddComponent<Image>();
                icon.sprite = coin; icon.preserveAspect = true; icon.raycastTarget = false;
                x += 26f;
            }
            var number = CreateRect("Gold", group, Vector2.zero, Vector2.zero);
            for (int i = 0; i < value.Length; i++)
            {
                var digit = goldDigits[value[i] == ',' ? 10 : value[i] - '0'];
                float glyphWidth = digit.rect.width * height / 300f;
                var image = CreateRect("Digit" + i, number, new Vector2(x + glyphWidth / 2f, 0), new Vector2(glyphWidth, height)).gameObject.AddComponent<Image>();
                image.sprite = digit; image.raycastTarget = false;
                x += glyphWidth;
            }
        }

        private static RectTransform CreateLabel(string name, Transform parent, Vector2 position, Vector2 size, string resource, string fallback, Color color)
        {
            var sprite = Resources.Load<Sprite>("Art/UI/OfflineReward/" + resource + "_label");
            if (sprite == null) return CreateText(name, parent, position, size, fallback, 12, color);
            var rect = CreateRect(name, parent, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return rect;
        }

        private RectTransform CreateTrimmedLabel(string name, Transform parent, Vector2 position, Vector2 size, string resource, string fallback, Color color, Rect bounds, Vector2 textureSize)
        {
            var source = Resources.Load<Sprite>("Art/UI/OfflineReward/" + resource + "_label");
            if (source == null || source.texture.width != textureSize.x || source.texture.height != textureSize.y)
                return CreateText(name, parent, position, size, fallback, 12, color);
            var sprite = Sprite.Create(source.texture, bounds, new Vector2(.5f, .5f), 32);
            sprite.name = source.name;
            lettering.Add(sprite);
            var rect = CreateRect(name, parent, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return rect;
        }

        private static void FitText(TextMeshProUGUI text, float min, float max)
        {
            if (text == null) return;
            text.enableAutoSizing = true; text.fontSizeMin = min; text.fontSizeMax = max;
        }

        private static RectTransform CreateText(string name, Transform parent, Vector2 position, Vector2 size, string value, float fontSize, Color color)
        {
            var rect = CreateRect(name, parent, position, size);
            try
            {
                var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
                text.fontSize = fontSize;
                text.color = color;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.raycastTarget = false;
                text.text = value;
            }
            catch (Exception e) { Debug.LogWarning("OfflineRewardView: TMP Essentials required: " + e.Message); }
            return rect;
        }

        private void SetButtonLabel(string value)
        {
            if (buttonLabelImage != null)
            {
                var sprite = Resources.Load<Sprite>("Art/UI/OfflineReward/" + (value == OfflineRewardText.RetryButton ? "retry" : "claim") + "_label");
                if (sprite != null) buttonLabelImage.sprite = sprite;
            }
            if (buttonLabel == null) return;
            try { buttonLabel.text = value; }
            catch (Exception e) { Debug.LogWarning("OfflineRewardView: TMP Essentials required: " + e.Message); }
        }

        private void OnDestroy()
        {
            if (overlay != null) Destroy(overlay);
            if (goldDigits != null) foreach (var digit in goldDigits) if (digit != null) Destroy(digit);
            foreach (var sprite in lettering) if (sprite != null) Destroy(sprite);
        }
    }
}
