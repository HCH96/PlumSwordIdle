using System;
using PlumSword.Core.StatUpgrade;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.StatUpgrade
{
    public sealed class AutoUpgradeView : MonoBehaviour
    {
        private AutoUpgrade _autoUpgrade;
        private Image _image;
        private TextMeshProUGUI _label;
        private static TMP_FontAsset _font;

        public void Initialize(AutoUpgrade autoUpgrade, Canvas canvas)
        {
            if (autoUpgrade == null) throw new ArgumentNullException(nameof(autoUpgrade));
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (_autoUpgrade != null) _autoUpgrade.EnabledChanged -= OnEnabledChanged;
            if (_image != null) Destroy(_image.gameObject);
            _autoUpgrade = autoUpgrade;

            var buttonObject = new GameObject("AutoUpgradeButton", typeof(RectTransform));
            var rect = (RectTransform)buttonObject.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(216f, 86f);
            rect.sizeDelta = new Vector2(96f, 26f);
            _image = buttonObject.AddComponent<Image>();
            _image.sprite = Resources.Load<Sprite>("Art/UI/StatUpgrade/button");
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = _image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => _autoUpgrade.SetEnabled(!_autoUpgrade.Enabled));

            var labelObject = new GameObject("Label", typeof(RectTransform));
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            _label = labelObject.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 11f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.raycastTarget = false;
            if (_font == null)
            {
                var font = Resources.Load<Font>("Fonts/Galmuri11");
                if (font != null)
                {
                    _font = TMP_FontAsset.CreateFontAsset(font);
                    if (_font != null) _font.TryAddCharacters("자동 강화 켜짐꺼짐");
                }
            }
            if (_font != null) _label.font = _font;
            Refresh();
            _autoUpgrade.EnabledChanged += OnEnabledChanged;
        }

        private void OnEnabledChanged(bool enabled) { Refresh(); }

        private void Refresh()
        {
            bool enabled = _autoUpgrade.Enabled;
            _image.color = enabled ? new Color(0.20f, 0.45f, 0.25f) : new Color(0.25f, 0.25f, 0.25f);
            _label.text = AutoUpgradeText.Label(enabled);
            _label.color = enabled ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        }

        private void OnDestroy()
        {
            if (_autoUpgrade != null) _autoUpgrade.EnabledChanged -= OnEnabledChanged;
        }
    }
}
