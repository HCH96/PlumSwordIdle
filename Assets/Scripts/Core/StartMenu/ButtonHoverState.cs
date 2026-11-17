using System;

namespace PlumSword.Core.StartMenu
{
    /// <summary>
    /// 시작 화면 그림 버튼의 마우스 올림(호버)·누름 반응 (docs/spec/start_menu.md "버튼 반응").
    /// 올리면 살짝 커지고 밝아지며 위로 뜨고, 누르면 조금 작아지며 내려앉는다. 값은 목표를 향해 부드럽게(지수 감쇠) 따라간다.
    /// 화면 코드는 Scale, Brightness, Lift 를 읽어 그리기만 한다. 수치는 모두 임시다.
    /// </summary>
    public sealed class ButtonHoverState
    {
        public const double HoverScale = 1.06;      // 올렸을 때 배율
        public const double PressScale = 0.97;      // 누르는 동안 배율 (올림보다 우선)
        public const double NormalBrightness = 0.88; // 평소 밝기 (색 곱)
        public const double HoverBrightness = 1.0;  // 올렸을 때 밝기
        public const double PressBrightness = 0.95; // 누르는 동안 밝기
        public const double HoverLift = 2;          // 올렸을 때 위로 뜨는 거리 (UI 단위)
        public const double PressLift = 0;          // 누르는 동안 (원래 자리로 내려앉음)
        public const double Response = 14;          // 따라가는 빠르기 (1/초). 0.1초면 약 75%, 0.2초면 약 94%
        public const double MaxStep = 0.1;

        public bool IsHovered { get; private set; }
        public bool IsPressed { get; private set; }

        // 0 = 평소, 1 = 올림. 누름은 Press 가 따로 0~1.
        public double Hover { get; private set; }
        public double Press { get; private set; }

        public double Scale => Blend(1.0, HoverScale, PressScale);
        public double Brightness => Blend(NormalBrightness, HoverBrightness, PressBrightness);
        public double Lift => Blend(0, HoverLift, PressLift);

        public void SetHovered(bool hovered)
        {
            IsHovered = hovered;
        }

        // 누름은 올라가 있을 때만 의미가 있다. 손을 떼거나 밖으로 나가면 SetPressed(false).
        public void SetPressed(bool pressed)
        {
            IsPressed = pressed;
        }

        // 손가락(터치)처럼 올림 없이 바로 누르는 경우도 누름으로 본다. 밖으로 나가면 둘 다 풀린다.
        public void Exit()
        {
            IsHovered = false;
            IsPressed = false;
        }

        // 0 이하·NaN·무한이면 아무것도 하지 않는다. MaxStep 보다 길면 MaxStep 만큼만 따라간다.
        public void Tick(double deltaSeconds)
        {
            if (!(deltaSeconds > 0) || double.IsInfinity(deltaSeconds)) return;
            double k = 1 - Math.Exp(-Response * Math.Min(deltaSeconds, MaxStep));
            Hover += ((IsHovered || IsPressed ? 1 : 0) - Hover) * k;
            Press += ((IsPressed ? 1 : 0) - Press) * k;
        }

        // 바로 목표 값으로 맞춘다 (테스트, 처음 표시).
        public void Snap()
        {
            Hover = IsHovered || IsPressed ? 1 : 0;
            Press = IsPressed ? 1 : 0;
        }

        // 평소 → 올림(Hover) 으로 섞고, 그 위에 누름(Press) 을 섞는다.
        private double Blend(double normal, double hover, double press)
        {
            double hovered = normal + (hover - normal) * Hover;
            return hovered + (press - hovered) * Press;
        }
    }
}
