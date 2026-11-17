using System;
using PlumSword.Core.Save;

namespace PlumSword.Core.StartMenu
{
    public sealed class LeaveGame
    {
        private readonly AutoSave autoSave;

        public LeaveGame(AutoSave autoSave)
        {
            this.autoSave = autoSave ?? throw new ArgumentNullException(nameof(autoSave));
        }

        public bool HasFailed { get; private set; }

        public bool TryLeave(long nowUtcTicks)
        {
            bool succeeded = autoSave.SaveNow(nowUtcTicks);
            HasFailed = !succeeded;
            return succeeded;
        }
    }
}
