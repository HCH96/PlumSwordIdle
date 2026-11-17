using System;
using PlumSword.Core.StartMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.StartMenu
{
    public sealed class StartScreenView : MonoBehaviour
    {
        private GameObject panel;
        private bool started;

        public void Initialize(Canvas canvas, StartButtonKind button, Action onStart, Action onOpenSettings)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (onStart == null) throw new ArgumentNullException(nameof(onStart));
            if (onOpenSettings == null) throw new ArgumentNullException(nameof(onOpenSettings));

            panel = new GameObject("StartScreenPanel", typeof(RectTransform), typeof(Image));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(canvas.transform, false);
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            panelRect.SetAsLastSibling();
            var background = panel.GetComponent<Image>();
            background.color = new Color(0.10f, 0.07f, 0.12f);
            background.raycastTarget = true;

            CreateText("Title", panelRect, new Vector2(0, 70), new Vector2(600, 48), StartMenuText.Title, 36, new Color(1f, 0.78f, 0.86f));
            var start = CreateButton("StartButton", panelRect, new Vector2(0, -20), new Vector2(200, 40), new Color(0.20f, 0.45f, 0.25f), StartMenuText.StartButtonLabel(button), 18);
            start.onClick.AddListener(() =>
            {
                if (started) return;
                started = true;
                try { onStart(); }
                finally { Destroy(panel); Destroy(gameObject); }
            });
            var settings = CreateButton("SettingsButton", panelRect, new Vector2(0, -72), new Vector2(200, 32), new Color(0.25f, 0.25f, 0.30f), StartMenuText.SettingsButton, 14);
            settings.onClick.AddListener(() => onOpenSettings());
        }

        private static Button CreateButton(string name, Transform parent, Vector2 position, Vector2 size, Color color, string label, float fontSize)
        {
            var rect = CreateRect(name, parent, position, size);
            rect.gameObject.AddComponent<Image>().color = color;
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            CreateText("Text", rect, Vector2.zero, size, label, fontSize, Color.white);
            return button;
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

        private static void CreateText(string name, Transform parent, Vector2 position, Vector2 size, string value, float fontSize, Color color)
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
            catch (Exception e) { Debug.LogWarning("StartScreenView: TMP Essentials required: " + e.Message); }
        }

        private void OnDestroy()
        {
            if (panel != null) Destroy(panel);
        }
    }
}
