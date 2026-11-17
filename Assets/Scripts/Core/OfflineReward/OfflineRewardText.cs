using System;
using System.Globalization;
using PlumSword.Core.Currency;

namespace PlumSword.Core.OfflineReward
{
    public static class OfflineRewardText
    {
        public const string Title = "OFFLINE REWARD";
        public const string SaveFailedMessage = "SAVE FAILED. TAP RETRY.";
        public const string ClaimButton = "CLAIM";
        public const string RetryButton = "RETRY";

        public static string FormatDuration(long ticks)
        {
            if (ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(ticks));

            long seconds = ticks / TimeSpan.TicksPerSecond;
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}",
                seconds / 3600, seconds / 60 % 60, seconds % 60);
        }

        public static string ElapsedLine(long elapsedTicks)
        {
            return "AWAY " + FormatDuration(elapsedTicks);
        }

        public static string AppliedLine(long appliedTicks)
        {
            return "REWARD FOR " + FormatDuration(appliedTicks) + " (MAX)";
        }

        public static string GoldLine(long gold)
        {
            return GoldText.Format(gold);
        }

        public static string ButtonLabel(OfflineRewardClaimState state)
        {
            return state == OfflineRewardClaimState.SaveFailed ? RetryButton : ClaimButton;
        }
    }
}
