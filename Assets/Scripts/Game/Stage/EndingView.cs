using System;
using PlumSword.Core.Stage;
using PlumSword.Game.StartMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Stage
{
    public sealed class EndingView : MonoBehaviour
    {
        private GameObject panel;
        private bool returned;
        private static TMP_FontAsset font;

        public void Initialize(Canvas canvas, double playSeconds, int regressionCount, Action onReturn)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (onReturn == null) throw new ArgumentNullException(nameof(onReturn));
            panel = new GameObject("EndingPanel", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            var background = panel.GetComponent<Image>();
            background.color = new Color(0.05f, 0.02f, 0.06f, 0.92f);
            background.raycastTarget = true;

            var time = EndingText.PlayTime(playSeconds);
            var regressions = EndingText.Regressions(regressionCount);
            if (font == null)
            {
                var source = Resources.Load<Font>("Fonts/Galmuri11");
                if (source != null) font = TMP_FontAsset.CreateFontAsset(source);
            }
            if (font != null) font.TryAddCharacters(EndingText.Title + time + regressions + EndingText.ReturnButton + "0123456789:");
            CreateText("EndingTitle", rect, new Vector2(0, 60), new Vector2(600, 48), EndingText.Title, 36);
            CreateText("EndingPlayTime", rect, Vector2.zero, new Vector2(600, 28), time, 18);
            CreateText("EndingRegressions", rect, new Vector2(0, -30), new Vector2(600, 28), regressions, 18);
            var buttonRect = CreateRect("EndingReturnButton", rect, new Vector2(0, -90), new Vector2(160, 40));
            var image = buttonRect.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>(StartScreenArt.StartButtonPath);
            image.color = image.sprite != null ? Color.white : new Color(0.35f, 0.12f, 0.2f);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => { if (returned) return; returned = true; onReturn(); });
            CreateText("Label", buttonRect, Vector2.zero, new Vector2(160, 40), EndingText.ReturnButton, 16);
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void CreateText(string name, Transform parent, Vector2 position, Vector2 size, string value, float fontSize)
        {
            var text = CreateRect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.95f, 0.85f);
            text.raycastTarget = false;
            text.text = value;
        }

        private void OnDestroy()
        {
            if (panel != null) Destroy(panel);
        }
    }
}
