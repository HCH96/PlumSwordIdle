using System.Globalization;

namespace PlumSword.Core.Currency
{
    /// <summary>
    /// 금화 숫자를 화면 글자로 바꾼다. 상태가 없는 정적 메서드만 가진다.
    /// 기기의 문화권 설정과 상관없이 항상 쉼표(,)로 세 자리를 나눈다.
    /// </summary>
    public static class GoldText
    {
        /// <summary>
        /// 세 자리마다 쉼표. 예: 1234 -> "1,234", 0 -> "0", -1234 -> "-1,234". 예외 없음.
        /// </summary>
        public static string FormatNumber(long amount)
        {
            return amount.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 보유 금화 표기. "G " + FormatNumber(amount). 예: 1234 -> "G 1,234".
        /// </summary>
        public static string Format(long amount)
        {
            return "G " + FormatNumber(amount);
        }
    }
}
