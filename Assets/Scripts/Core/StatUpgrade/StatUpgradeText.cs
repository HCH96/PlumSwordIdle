using System;
using System.Globalization;
using PlumSword.Core.Currency;

namespace PlumSword.Core.StatUpgrade
{
    /// <summary>
    /// 강화 버튼에 보여 줄 글자를 만든다. 상태가 없는 정적 메서드만 가진다.
    /// 1단계에는 한글 글꼴이 없어 영문 약자만 쓴다.
    /// </summary>
    public static class StatUpgradeText
    {
        /// <summary>
        /// 능력치 이름. Attack -> "ATK", Health -> "HP", AttackSpeed -> "SPD".
        /// 정의되지 않은 값은 ArgumentOutOfRangeException.
        /// </summary>
        public static string Name(StatType stat)
        {
            switch (stat)
            {
                case StatType.Attack: return "ATK";
                case StatType.Health: return "HP";
                case StatType.AttackSpeed: return "SPD";
                default: throw new ArgumentOutOfRangeException(nameof(stat), stat, "정의되지 않은 능력치");
            }
        }

        /// <summary>
        /// "Lv." + level. 예: 0 -> "Lv.0", 10 -> "Lv.10". 쉼표 없음. 음수도 그대로 (예외 없음).
        /// </summary>
        public static string Level(int level)
        {
            return "Lv." + level.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// isMaxLevel 이 true 면 "MAX" (cost 는 보지 않는다).
        /// 아니면 "G " + GoldText.FormatNumber(cost). 예: 20 -> "G 20", 1234 -> "G 1,234". 음수도 그대로.
        /// </summary>
        public static string Cost(long cost, bool isMaxLevel)
        {
            if (isMaxLevel)
            {
                return "MAX";
            }
            return "G " + GoldText.FormatNumber(cost);
        }

        /// <summary>
        /// Name + "\n" + Level + "\n" + Cost. 줄바꿈은 항상 '\n' 한 글자.
        /// 예: (Attack, 3, 35, false) -> "ATK\nLv.3\nG 35", (AttackSpeed, 10, 70, true) -> "SPD\nLv.10\nMAX".
        /// 정의되지 않은 stat 은 ArgumentOutOfRangeException.
        /// </summary>
        public static string ButtonLabel(StatType stat, int level, long cost, bool isMaxLevel)
        {
            return Name(stat) + "\n" + Level(level) + "\n" + Cost(cost, isMaxLevel);
        }
    }
}
