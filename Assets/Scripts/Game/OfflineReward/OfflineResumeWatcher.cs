using System;
using PlumSword.Core.OfflineReward;
using PlumSword.Core.Save;
using UnityEngine;

namespace PlumSword.Game.OfflineReward
{
    public sealed class OfflineResumeWatcher : MonoBehaviour
    {
        private GameRoot root;
        private AutoSave autoSave;
        private long? pausedAtUtcTicks;

        public void Initialize(GameRoot root, AutoSave autoSave)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (autoSave == null) throw new ArgumentNullException(nameof(autoSave));
            this.root = root;
            this.autoSave = autoSave;
            pausedAtUtcTicks = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (root == null || autoSave == null) return;
            if (paused)
            {
                pausedAtUtcTicks = DateTime.UtcNow.Ticks;
                return;
            }

            if (!pausedAtUtcTicks.HasValue) return;
            long pausedAt = pausedAtUtcTicks.Value;
            pausedAtUtcTicks = null;
            if (OfflineRewardCalculator.ShouldRestartOnResume(
                pausedAt, DateTime.UtcNow.Ticks, !autoSave.HasFailed && !autoSave.IsDirty))
                root.Restart();
        }
    }
}
