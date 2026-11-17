using System;
using NUnit.Framework;
using PlumSword.Core.StartMenu;

namespace PlumSword.Core.Tests.StartMenu
{
    public class ButtonHoverStateTests
    {
        private const double Frame = 1.0 / 60;

        private static void Run(ButtonHoverState state, double seconds)
        {
            for (double t = 0; t < seconds - 1e-9; t += Frame) state.Tick(Frame);
        }

        [Test]
        public void New_IsNormal()
        {
            var state = new ButtonHoverState();
            Assert.That(state.IsHovered, Is.False);
            Assert.That(state.IsPressed, Is.False);
            Assert.That(state.Scale, Is.EqualTo(1.0));
            Assert.That(state.Brightness, Is.EqualTo(ButtonHoverState.NormalBrightness));
            Assert.That(state.Lift, Is.EqualTo(0));
        }

        [Test]
        public void Hovered_ApproachesHoverValuesSmoothly()
        {
            var state = new ButtonHoverState();
            state.SetHovered(true);
            state.Tick(Frame);
            // 한 프레임에 바로 다 가지 않는다.
            Assert.That(state.Scale, Is.GreaterThan(1.0).And.LessThan(ButtonHoverState.HoverScale));
            double previous = state.Scale;
            for (int i = 0; i < 30; i++)
            {
                state.Tick(Frame);
                Assert.That(state.Scale, Is.GreaterThanOrEqualTo(previous));
                previous = state.Scale;
            }
            Run(state, 0.5);
            Assert.That(state.Scale, Is.EqualTo(ButtonHoverState.HoverScale).Within(1e-3));
            Assert.That(state.Brightness, Is.EqualTo(ButtonHoverState.HoverBrightness).Within(1e-3));
            Assert.That(state.Lift, Is.EqualTo(ButtonHoverState.HoverLift).Within(1e-2));
        }

        [Test]
        public void Hovered_ReachesMostOfTheWayInAboutPointTwoSeconds()
        {
            var state = new ButtonHoverState();
            state.SetHovered(true);
            Run(state, 0.1);
            Assert.That(state.Hover, Is.InRange(0.7, 0.8));
            Run(state, 0.1);
            Assert.That(state.Hover, Is.GreaterThan(0.9));
        }

        [Test]
        public void Exit_ReturnsToNormal()
        {
            var state = new ButtonHoverState();
            state.SetHovered(true);
            state.SetPressed(true);
            state.Snap();
            state.Exit();
            Assert.That(state.IsHovered, Is.False);
            Assert.That(state.IsPressed, Is.False);
            Run(state, 1.0);
            Assert.That(state.Scale, Is.EqualTo(1.0).Within(1e-4));
            Assert.That(state.Brightness, Is.EqualTo(ButtonHoverState.NormalBrightness).Within(1e-4));
            Assert.That(state.Lift, Is.EqualTo(0).Within(1e-3));
        }

        [Test]
        public void Pressed_OverridesHover()
        {
            var state = new ButtonHoverState();
            state.SetHovered(true);
            state.SetPressed(true);
            state.Snap();
            Assert.That(state.Scale, Is.EqualTo(ButtonHoverState.PressScale));
            Assert.That(state.Brightness, Is.EqualTo(ButtonHoverState.PressBrightness));
            Assert.That(state.Lift, Is.EqualTo(ButtonHoverState.PressLift));
            state.SetPressed(false);
            Run(state, 1.0);
            Assert.That(state.Scale, Is.EqualTo(ButtonHoverState.HoverScale).Within(1e-4));
        }

        // 터치처럼 올림 없이 바로 누르는 경우도 누름 모양이 된다.
        [Test]
        public void PressedWithoutHover_StillShowsPress()
        {
            var state = new ButtonHoverState();
            state.SetPressed(true);
            Run(state, 1.0);
            Assert.That(state.Scale, Is.EqualTo(ButtonHoverState.PressScale).Within(1e-4));
            Assert.That(state.Brightness, Is.EqualTo(ButtonHoverState.PressBrightness).Within(1e-4));
        }

        [Test]
        public void Values_StayWithinRanges()
        {
            var state = new ButtonHoverState();
            var random = new Random(3);
            for (int i = 0; i < 2000; i++)
            {
                if (random.Next(10) == 0) state.SetHovered(random.Next(2) == 0);
                if (random.Next(10) == 0) state.SetPressed(random.Next(2) == 0);
                if (random.Next(50) == 0) state.Exit();
                state.Tick(Frame);
                Assert.That(state.Hover, Is.InRange(0.0, 1.0));
                Assert.That(state.Press, Is.InRange(0.0, 1.0));
                Assert.That(state.Scale, Is.InRange(ButtonHoverState.PressScale, ButtonHoverState.HoverScale));
                Assert.That(state.Brightness, Is.InRange(ButtonHoverState.NormalBrightness, ButtonHoverState.HoverBrightness));
                Assert.That(state.Lift, Is.InRange(0.0, ButtonHoverState.HoverLift));
            }
        }

        [TestCase(0.0)]
        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Tick_InvalidDelta_DoesNothing(double delta)
        {
            var state = new ButtonHoverState();
            state.SetHovered(true);
            state.Tick(delta);
            Assert.That(state.Hover, Is.EqualTo(0));
        }

        [Test]
        public void Tick_LongDelta_MovesOnlyMaxStep()
        {
            var a = new ButtonHoverState();
            var b = new ButtonHoverState();
            a.SetHovered(true);
            b.SetHovered(true);
            a.Tick(5);
            b.Tick(ButtonHoverState.MaxStep);
            Assert.That(a.Hover, Is.EqualTo(b.Hover));
        }
    }
}
