using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlumSword.Game.StatUpgrade
{
    /// <summary>강화 버튼의 입력 반응과 성공 연출. 구매 판단은 StatUpgradeView가 맡는다.</summary>
    public sealed class StatUpgradeButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Button button;
        private RectTransform visual, icon;
        private Image frame;
        private TextMeshProUGUI level;
        private Image[] petals;
        private Vector2 iconPosition;
        private bool hovered, pressed, available;
        private float successTime = -1f;
        private const float Duration = .55f;

        public void Initialize(Button target, RectTransform artwork, Image frameImage, Image iconImage, TextMeshProUGUI levelText)
        {
            button = target; visual = artwork; frame = frameImage;
            icon = iconImage != null ? iconImage.rectTransform : null;
            if (icon != null) iconPosition = icon.anchoredPosition;
            level = levelText;
            var sprites = new[] { Resources.Load<Sprite>("Art/StartMenu/petal_open"), Resources.Load<Sprite>("Art/StartMenu/petal_curled") };
            petals = new Image[2];
            for (int i = 0; i < petals.Length; i++)
            {
                if (sprites[i] == null) continue;
                var obj = new GameObject("SuccessPetal" + i, typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)obj.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(8, 8);
                var image = obj.GetComponent<Image>(); image.sprite = sprites[i]; image.preserveAspect = true;
                image.raycastTarget = false; image.enabled = false; petals[i] = image;
            }
        }

        public void SetEnabledAppearance(bool canUpgrade)
        {
            available = canUpgrade;
            if (!available) { hovered = false; pressed = false; }
        }

        public void PlaySuccess() { successTime = 0f; UpdateVisual(); }
        public void OnPointerEnter(PointerEventData eventData) { if (available) hovered = true; }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (available && (eventData == null || eventData.button == PointerEventData.InputButton.Left)) pressed = true;
        }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; }

        private void Update()
        {
            if (successTime >= 0f) successTime += Time.unscaledDeltaTime;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (visual == null || button == null) return;
            float t = successTime < 0f ? 1f : Mathf.Clamp01(successTime / Duration);
            float pulse = successTime < 0f ? 0f : Mathf.Sin(t * Mathf.PI);
            float scale = pressed && available ? .96f : available && hovered ? 1.025f : 1f;
            if (successTime >= 0f && t < .25f) scale *= Mathf.Lerp(.96f, 1f, t / .25f);
            visual.localScale = Vector3.one * scale;
            var baseColor = available ? new Color(1f, 1f, 1f, .86f) : new Color(.55f, .55f, .55f, .86f);
            frame.color = available && hovered ? Color.Lerp(baseColor, new Color(1f, .82f, .55f, .96f), .22f) : baseColor;
            if (icon != null) icon.anchoredPosition = iconPosition + new Vector2(0, pulse * 4f);
            if (level != null) level.color = Color.Lerp(available ? Color.white : new Color(.6f, .6f, .6f), new Color(1f, .78f, .42f), pulse);
            for (int i = 0; i < petals.Length; i++)
            {
                var p = petals[i]; if (p == null) continue;
                p.enabled = successTime >= 0f && t < 1f;
                if (!p.enabled) continue;
                float side = i == 0 ? -1f : 1f;
                p.rectTransform.anchoredPosition = new Vector2(side * (40f + 24f * t), 10f + 28f * t - 16f * t * t);
                p.rectTransform.localRotation = Quaternion.Euler(0, 0, side * (30f + 150f * t));
                p.color = new Color(1, 1, 1, 1f - t);
            }
            if (successTime >= 0f && t >= 1f) successTime = -1f;
        }

        private void OnDisable()
        {
            hovered = pressed = false; successTime = -1f;
            if (visual != null) { visual.localScale = Vector3.one; UpdateVisual(); }
        }
    }
}
