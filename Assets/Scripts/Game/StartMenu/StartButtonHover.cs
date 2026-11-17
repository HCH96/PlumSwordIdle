using System;
using PlumSword.Core.StartMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlumSword.Game.StartMenu
{
    /// <summary>
    /// 시작 화면 그림 버튼의 호버·누름 반응을 그린다. 판단과 값은 Core 의 ButtonHoverState 가 맡는다.
    /// 버튼(눌리는 영역)은 그대로 두고 그림 자식(visual)만 키우고 밝히고 띄운다. 그래서 그림이 움직여도
    /// 마우스가 버튼 가장자리에서 들어갔다 나갔다를 되풀이하지 않는다. 시간은 Time.unscaledDeltaTime.
    /// </summary>
    public sealed class StartButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform visual;
        private Image image;

        public ButtonHoverState State { get; } = new ButtonHoverState();

        // visual 이 null 이면 ArgumentNullException. visual 은 Image 가 붙은 자식이다.
        public void Initialize(RectTransform visual)
        {
            if (visual == null) throw new ArgumentNullException(nameof(visual));
            this.visual = visual;
            image = visual.GetComponent<Image>();
            State.Snap();
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData) => State.SetHovered(true);
        public void OnPointerExit(PointerEventData eventData) => State.Exit();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null || eventData.button == PointerEventData.InputButton.Left) State.SetPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null || eventData.button == PointerEventData.InputButton.Left) State.SetPressed(false);
        }

        // 다른 창(설정 창 등)이 열리며 꺼졌다 켜지면 평소 모양으로 되돌린다.
        private void OnDisable()
        {
            State.Exit();
            State.Snap();
            if (visual != null) Apply();
        }

        private void Update()
        {
            if (visual == null) return;
            State.Tick(Time.unscaledDeltaTime);
            Apply();
        }

        private void Apply()
        {
            float scale = (float)State.Scale;
            visual.localScale = new Vector3(scale, scale, 1);
            visual.anchoredPosition = new Vector2(0, (float)State.Lift);
            if (image != null)
            {
                float b = (float)State.Brightness;
                image.color = new Color(b, b, b, 1);
            }
        }
    }
}
