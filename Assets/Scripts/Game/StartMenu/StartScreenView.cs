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

        // 그림 버튼 크기와 위치 (기준 해상도 640×360, 캔버스 가운데 기준). 버튼 그림 1436×262 와 같은 비율이다.
        public static readonly Vector2 ArtButtonSize = new Vector2(230, 42);
        public static readonly Vector2 ArtStartButtonPosition = new Vector2(44, -28);
        public static readonly Vector2 ArtSettingsButtonPosition = new Vector2(44, -80);

        // 시작 화면 그림(StartScreenArt.Load)을 읽어 만든다. 그림이 없으면 단색 바탕과 영문 글자로 만든다.
        public void Initialize(Canvas canvas, StartButtonKind button, Action onStart, Action onOpenSettings)
        {
            Initialize(canvas, button, onStart, onOpenSettings, StartScreenArt.Load());
        }

        // art 가 null 이면 그림 없이(대체 표시) 만든다.
        public void Initialize(Canvas canvas, StartButtonKind button, Action onStart, Action onOpenSettings, StartScreenArt art)
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

            Button start;
            Button settings;
            if (art != null)
            {
                CreateBackground(panelRect, art.Background);
                start = CreateArtButton("StartButton", panelRect, ArtStartButtonPosition, art.StartButtonSprite(button));
                settings = CreateArtButton("SettingsButton", panelRect, ArtSettingsButtonPosition, art.SettingsButton);
            }
            else
            {
                CreateText("Title", panelRect, new Vector2(0, 70), new Vector2(600, 48), StartMenuText.Title, 36, new Color(1f, 0.78f, 0.86f));
                start = CreateButton("StartButton", panelRect, new Vector2(0, -20), new Vector2(200, 40), new Color(0.20f, 0.45f, 0.25f), StartMenuText.StartButtonLabel(button), 18);
                settings = CreateButton("SettingsButton", panelRect, new Vector2(0, -72), new Vector2(200, 32), new Color(0.25f, 0.25f, 0.30f), StartMenuText.SettingsButton, 14);
            }

            start.onClick.AddListener(() =>
            {
                if (started) return;
                started = true;
                try { onStart(); }
                finally { Destroy(panel); Destroy(gameObject); }
            });
            settings.onClick.AddListener(() => onOpenSettings());
        }

        // 제목이 그려진 배경. 화면을 빈틈없이 덮도록(EnvelopeParent) 비율을 지켜 키우고, 넘치는 쪽은 가운데 기준으로 잘린다.
        private static void CreateBackground(RectTransform parent, Sprite sprite)
        {
            var obj = new GameObject("Background", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            var fitter = obj.GetComponent<AspectRatioFitter>();
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        }

        // 글자가 그림에 들어 있는 버튼. 글자 오브젝트는 만들지 않는다.
        private static Button CreateArtButton(string name, Transform parent, Vector2 position, Sprite sprite)
        {
            var rect = CreateRect(name, parent, position, ArtButtonSize);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            return button;
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
