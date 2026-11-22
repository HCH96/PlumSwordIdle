using System;
using System.Globalization;

namespace PlumSword.Core.Stage
{
    public static class EndingText
    {
        public const string Title = "천마 토벌";
        public const string ReturnButton = "처음으로";

        public static string PlayTime(double seconds)
        {
            if (seconds < 0.0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));

            double minutes = Math.Floor(seconds / 60.0);
            double remainingSeconds = Math.Floor(seconds) % 60.0;
            return "플레이 시간 " + minutes.ToString("00", CultureInfo.InvariantCulture)
                + ":" + remainingSeconds.ToString("00", CultureInfo.InvariantCulture);
        }

        public static string Regressions(int count)
        {
            return "회귀 " + count.ToString(CultureInfo.InvariantCulture) + "회";
        }
    }
}
