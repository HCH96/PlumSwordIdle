using PlumSword.Core.Save;

namespace PlumSword.Core.StartMenu
{
    public static class StartMenuRules
    {
        public static bool ShowsStartScreen(StartEntry entry, bool isBatchMode)
        {
            if (entry == StartEntry.Launch) return !isBatchMode;
            return entry != StartEntry.ResumeInGame;
        }

        public static bool CalculatesOfflineReward(StartEntry entry)
        {
            return entry != StartEntry.TitleReturn;
        }

        public static StartButtonKind StartButton(SaveLoadStatus status)
        {
            return status == SaveLoadStatus.Loaded ? StartButtonKind.Continue : StartButtonKind.NewGame;
        }

        public static bool AutoSaveStartsEnabled(SaveLoadStatus status, bool hasOfflineReward)
        {
            return status == SaveLoadStatus.Loaded && !hasOfflineReward;
        }

        public static StartEntry ResumeEntry(bool isStartScreenShowing)
        {
            return isStartScreenShowing ? StartEntry.ResumeOnTitle : StartEntry.ResumeInGame;
        }

        public static BackKeyAction BackKey(bool isSettingsOpen, bool isStartScreenShowing, bool hasBegun)
        {
            if (isSettingsOpen) return BackKeyAction.CloseSettings;
            return !isStartScreenShowing && hasBegun ? BackKeyAction.OpenSettings : BackKeyAction.None;
        }
    }
}
