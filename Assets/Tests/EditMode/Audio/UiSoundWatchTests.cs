using System;
using NUnit.Framework;
using PlumSword.Core.Audio;

namespace PlumSword.Core.Tests.Audio
{
    public class UiSoundWatchTests
    {
        [Test]
        public void Constructor_NullQueue_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new UiSoundWatch(null));
        }

        [Test]
        public void Settings_OpeningAndClosingMakeSoundOnlyOnChange()
        {
            var queue = new SfxQueue(0); var watch = new UiSoundWatch(queue);
            watch.UpdateWindows(true, false, false, false);
            Assert.That(queue.Drain(0), Is.EqualTo(new[] { SfxId.WindowOpenClose }));
            watch.UpdateWindows(true, false, false, false);
            Assert.That(queue.Drain(0), Is.Empty);
            watch.UpdateWindows(false, false, false, false);
            Assert.That(queue.Drain(0), Is.EqualTo(new[] { SfxId.WindowOpenClose }));
        }

        [Test]
        public void RewardWindow_OpeningMakesSoundClosingDoesNot()
        {
            var queue = new SfxQueue(0); var watch = new UiSoundWatch(queue);
            watch.UpdateWindows(false, false, false, false);
            Assert.That(queue.Drain(0), Is.Empty);
            watch.UpdateWindows(false, true, false, false);
            Assert.That(queue.Drain(0), Is.EqualTo(new[] { SfxId.WindowOpenClose }));
            watch.UpdateWindows(false, false, false, false);
            Assert.That(queue.Drain(0), Is.Empty);
        }

        [Test]
        public void ErrorWindow_OnlyOpeningMakesSound()
        {
            var queue = new SfxQueue(0); var watch = new UiSoundWatch(queue);
            watch.UpdateWindows(false, false, true, false);
            Assert.That(queue.Drain(0), Is.EqualTo(new[] { SfxId.WindowOpenClose }));
            watch.UpdateWindows(false, false, false, false);
            Assert.That(queue.Drain(0), Is.Empty);
        }

        [Test]
        public void FirstUpdate_AlreadyShowingRewardWindowMakesSound()
        {
            var queue = new SfxQueue(0); var watch = new UiSoundWatch(queue);
            watch.UpdateWindows(false, true, false, false);
            Assert.That(queue.Drain(0), Is.EqualTo(new[] { SfxId.WindowOpenClose }));
        }

        [Test]
        public void RewardPaid_OnlyRisingEdgeMakesSound()
        {
            var queue = new SfxQueue(0); var watch = new UiSoundWatch(queue);
            watch.UpdateWindows(false, false, false, true);
            Assert.That(queue.Drain(0), Is.EqualTo(new[] { SfxId.OfflineReward }));
            watch.UpdateWindows(false, false, false, true);
            Assert.That(queue.Drain(0), Is.Empty);
            watch.UpdateWindows(false, false, false, false);
            Assert.That(queue.Drain(0), Is.Empty);
            watch.UpdateWindows(false, false, false, true);
            Assert.That(queue.Drain(0), Is.EqualTo(new[] { SfxId.OfflineReward }));
        }

        [Test]
        public void RewardPaid_WhenWindowCloses_OnlyRewardSound()
        {
            var queue = new SfxQueue(0); var watch = new UiSoundWatch(queue);
            watch.UpdateWindows(false, true, false, false);
            Assert.That(queue.Drain(0), Is.EqualTo(new[] { SfxId.WindowOpenClose }));
            watch.UpdateWindows(false, false, false, true);
            Assert.That(queue.Drain(0), Is.EqualTo(new[] { SfxId.OfflineReward }));
        }

        [Test]
        public void InteractableButton_SoundsOnReleaseOnly()
        {
            var queue = new SfxQueue(0); var watch = new UiSoundWatch(queue);
            watch.PressStarted(true, true);
            Assert.That(queue.Drain(0), Is.Empty);
            watch.PressEnded();
            Assert.That(queue.Drain(0), Is.EqualTo(new[] { SfxId.UiConfirm }));
            watch.PressEnded();
            Assert.That(queue.Drain(0), Is.Empty);
        }

        [Test]
        public void DisabledButton_SoundsUnavailableOnRelease()
        {
            var queue = new SfxQueue(0); var watch = new UiSoundWatch(queue);
            watch.PressStarted(true, false); watch.PressEnded();
            Assert.That(queue.Drain(0), Is.EqualTo(new[] { SfxId.UpgradeUnavailable }));
        }

        [Test]
        public void NonButtonPress_ClearsPreviousPress()
        {
            var queue = new SfxQueue(0); var watch = new UiSoundWatch(queue);
            watch.PressStarted(true, true); watch.PressStarted(false, true); watch.PressEnded();
            Assert.That(queue.Drain(0), Is.Empty);
        }

        [Test]
        public void ReleaseWithoutPress_DoesNothing()
        {
            var queue = new SfxQueue(0); var watch = new UiSoundWatch(queue);
            watch.PressEnded();
            Assert.That(queue.Drain(0), Is.Empty);
        }

        [Test]
        public void LaterPress_OverwritesRememberedSound()
        {
            var queue = new SfxQueue(0); var watch = new UiSoundWatch(queue);
            watch.PressStarted(true, true); watch.PressStarted(true, false); watch.PressEnded();
            Assert.That(queue.Drain(0), Is.EqualTo(new[] { SfxId.UpgradeUnavailable }));
        }

        [Test]
        public void WindowAndButtonSameFrame_QueueReturnsWindowSound()
        {
            var queue = new SfxQueue(0); var watch = new UiSoundWatch(queue);
            watch.PressStarted(true, true); watch.PressEnded();
            watch.UpdateWindows(true, false, false, false);
            Assert.That(queue.Drain(0), Is.EqualTo(new[] { SfxId.WindowOpenClose }));
        }
    }
}
