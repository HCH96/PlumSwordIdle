using System.Globalization;

namespace PlumSword.Core.StartMenu
{
    public static class StartMenuText
    {
        public const string Title = "PLUM BLOSSOM SWORD";
        public const string NewGameButton = "START";
        public const string ContinueButton = "CONTINUE";
        public const string SettingsButton = "SETTINGS";
        public const string SettingsTitle = "SETTINGS";
        public const string ToTitleButton = "TO TITLE";
        public const string QuitButton = "QUIT GAME";
        public const string CloseButton = "CLOSE";
        public const string LeaveFailedMessage = "SAVE FAILED. TRY AGAIN.";

        public static string StartButtonLabel(StartButtonKind kind)
        {
            return kind == StartButtonKind.Continue ? ContinueButton : NewGameButton;
        }

        public static string BgmLine(int volume)
        {
            return "BGM " + volume.ToString(CultureInfo.InvariantCulture);
        }

        public static string SfxLine(int volume)
        {
            return "SFX " + volume.ToString(CultureInfo.InvariantCulture);
        }
    }
}
