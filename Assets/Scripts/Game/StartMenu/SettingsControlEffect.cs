using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlumSword.Game.StartMenu
{
    /// <summary>설정 조작부의 그림에만 반응을 주고 입력 영역은 그대로 둔다.</summary>
    public sealed class SettingsControlEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Selectable control;
        private RectTransform visual;
        private Image artwork, fill;
        private Color baseColor, baseFill;
        private bool hovered, pressed;
        private float valuePulse;

        public void InitializeButton(Button button)
        {
            control = button;
            var original = button.GetComponent<Image>();
            var rect = (RectTransform)new GameObject("InteractionArt", typeof(RectTransform), typeof(Image)).transform;
            rect.SetParent(button.transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();
            artwork = rect.GetComponent<Image>();
            artwork.sprite = original.sprite; artwork.preserveAspect = original.preserveAspect;
            artwork.type = original.type; artwork.color = original.color; artwork.raycastTarget = false;
            baseColor = artwork.color; visual = rect;
            var clear = original.color; clear.a = 0; original.color = clear;
        }

        public void InitializeSlider(Slider slider)
        {
            control = slider; slider.transition = Selectable.Transition.None;
            visual = slider.handleRect; artwork = visual.GetComponent<Image>(); baseColor = artwork.color;
            fill = slider.fillRect.GetComponent<Image>(); baseFill = fill.color;
            slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnValueChanged(float value) { valuePulse = 1f; }
        public void OnPointerEnter(PointerEventData eventData) { if (control != null && control.IsInteractable()) hovered = true; }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (control != null && control.IsInteractable() && (eventData == null || eventData.button == PointerEventData.InputButton.Left)) pressed = true;
        }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; }

        private void Update()
        {
            if (visual == null || artwork == null) return;
            if (control == null || !control.IsInteractable()) { hovered = false; pressed = false; }
            float target = pressed ? .96f : hovered ? 1.04f : 1f;
            float next = Mathf.Lerp(visual.localScale.x, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            visual.localScale = new Vector3(next, next, 1);
            float glow = pressed ? .34f : hovered ? .22f : 0f;
            glow = Mathf.Max(glow, valuePulse * .24f);
            artwork.color = Color.Lerp(baseColor, new Color(1f, .75f, .42f, baseColor.a), glow);
            if (fill != null) fill.color = Color.Lerp(baseFill, new Color(1f, .78f, .68f, baseFill.a), glow);
            valuePulse = Mathf.Max(0f, valuePulse - Time.unscaledDeltaTime * 4f);
        }

        private void OnDisable()
        {
            hovered = pressed = false; valuePulse = 0;
            if (visual != null) visual.localScale = Vector3.one;
            if (artwork != null) artwork.color = baseColor;
            if (fill != null) fill.color = baseFill;
        }

        private void OnDestroy()
        {
            if (control is Slider slider) slider.onValueChanged.RemoveListener(OnValueChanged);
        }
    }
}
