using System;
using PlumSword.Core.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Save
{
    public sealed class SaveErrorView : MonoBehaviour
    {
        private GameObject panel;
        private bool clicked;

        public void Initialize(Canvas canvas, Action onNewGame)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (onNewGame == null) throw new ArgumentNullException(nameof(onNewGame));
            panel = new GameObject("SaveErrorPanel", typeof(RectTransform), typeof(Image));
            var overlay = (RectTransform)panel.transform;
            overlay.SetParent(canvas.transform, false);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            overlay.SetAsLastSibling();
            var image = panel.GetComponent<Image>();
            image.color = new Color(0, 0, 0, 0.8f);
            image.raycastTarget = true;
            var window = CreateRect("SaveErrorWindow", overlay, Vector2.zero, new Vector2(360, 140));
            window.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);
            CreateText("Title", window, new Vector2(0, 40), new Vector2(340, 24), SaveText.CorruptTitle, 18, false);
            CreateText("Message", window, new Vector2(0, 8), new Vector2(340, 32), SaveText.CorruptMessage, 12, true);
            var buttonRect = CreateRect("NewGameButton", window, new Vector2(0, -40), new Vector2(160, 32));
            buttonRect.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.45f, 0.25f);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            CreateText("Text", buttonRect, Vector2.zero, new Vector2(160, 32), SaveText.NewGameButton, 16, false);
            button.onClick.AddListener(() =>
            {
                if (clicked) return;
                clicked = true;
                try { onNewGame(); }
                finally { Destroy(panel); Destroy(gameObject); }
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

        private static void CreateText(string name, Transform parent, Vector2 position, Vector2 size, string value, float fontSize, bool wrap)
        {
            var rect = CreateRect(name, parent, position, size);
            try
            {
                var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
                text.fontSize = fontSize;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                text.raycastTarget = false;
                text.text = value;
            }
            catch (Exception e) { Debug.LogWarning("SaveErrorView: TMP Essentials required: " + e.Message); }
        }
    }
}
