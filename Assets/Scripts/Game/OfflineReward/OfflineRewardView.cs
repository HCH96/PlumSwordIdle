using System;
using PlumSword.Core.OfflineReward;
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

            var window = CreateRect("OfflineRewardWindow", overlayRect, Vector2.zero, new Vector2(360, 170));
            window.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);
            CreateText("Title", window, new Vector2(0, 60), new Vector2(340, 24), OfflineRewardText.Title, 18, Color.white);
            CreateText("Elapsed", window, new Vector2(0, 34), new Vector2(340, 18), OfflineRewardText.ElapsedLine(claim.Reward.ElapsedTicks), 14, Color.white);
            var applied = CreateText("Applied", window, new Vector2(0, 14), new Vector2(340, 16), OfflineRewardText.AppliedLine(claim.Reward.AppliedTicks), 12, Color.white);
            applied.gameObject.SetActive(claim.Reward.IsCapped);
            CreateText("Gold", window, new Vector2(0, -10), new Vector2(340, 26), OfflineRewardText.GoldLine(claim.Reward.Gold), 20, new Color(0.95f, 0.80f, 0.25f));
            failureLine = CreateText("SaveFailed", window, new Vector2(0, -34), new Vector2(340, 16), OfflineRewardText.SaveFailedMessage, 12, new Color(1f, 0.35f, 0.35f)).gameObject;
            failureLine.SetActive(claim.State == OfflineRewardClaimState.SaveFailed);

            var buttonRect = CreateRect("ClaimButton", window, new Vector2(0, -62), new Vector2(160, 32));
            buttonRect.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.45f, 0.25f);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var labelRect = CreateText("Text", buttonRect, Vector2.zero, new Vector2(160, 32), OfflineRewardText.ButtonLabel(claim.State), 16, Color.white);
            buttonLabel = labelRect.GetComponent<TextMeshProUGUI>();
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
            if (buttonLabel == null) return;
            try { buttonLabel.text = value; }
            catch (Exception e) { Debug.LogWarning("OfflineRewardView: TMP Essentials required: " + e.Message); }
        }

        private void OnDestroy()
        {
            if (overlay != null) Destroy(overlay);
        }
    }
}
