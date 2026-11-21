using System;

namespace PlumSword.Core.Audio
{
    public sealed class UiSoundWatch
    {
        private readonly SfxQueue queue;
        private bool settingsOpen;
        private bool rewardWindowShowing;
        private bool errorWindowShowing;
        private bool rewardPaid;
        private SfxId? pressSound;

        public UiSoundWatch(SfxQueue queue)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public void UpdateWindows(bool settingsOpen, bool rewardWindowShowing, bool errorWindowShowing, bool rewardPaid)
        {
            if (this.settingsOpen != settingsOpen) queue.Request(SfxId.WindowOpenClose);
            if (!this.rewardWindowShowing && rewardWindowShowing) queue.Request(SfxId.WindowOpenClose);
            if (!this.errorWindowShowing && errorWindowShowing) queue.Request(SfxId.WindowOpenClose);
            if (!this.rewardPaid && rewardPaid) queue.Request(SfxId.OfflineReward);

            this.settingsOpen = settingsOpen;
            this.rewardWindowShowing = rewardWindowShowing;
            this.errorWindowShowing = errorWindowShowing;
            this.rewardPaid = rewardPaid;
        }

        public void PressStarted(bool onButton, bool interactable)
        {
            pressSound = onButton ? (interactable ? SfxId.UiConfirm : SfxId.UpgradeUnavailable) : (SfxId?)null;
        }

        public void PressEnded()
        {
            if (!pressSound.HasValue) return;
            queue.Request(pressSound.Value);
            pressSound = null;
        }
    }
}
