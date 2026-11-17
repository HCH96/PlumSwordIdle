using System;
using System.Globalization;

namespace PlumSword.Core.Stage
{
    /// <summary>
    /// 스테이지 화면에 보여 줄 글자를 정한다. 상태가 없는 정적 멤버만 가진다.
    /// 1단계에는 한글 글꼴이 없어 영문만 쓴다.
    /// </summary>
    public static class StageText
    {
        /// <summary>보스 이름 (임시값). 모든 스테이지에서 같다.</summary>
        public const string BossName = "GATEKEEPER";

        /// <summary>
        /// "STAGE " + 번호. 자릿수 구분 기호 없음, 기기 문화권과 상관없다.
        /// 0 이하도 그대로 표기하고 예외를 던지지 않는다. 예: 1 -> "STAGE 1", 0 -> "STAGE 0".
        /// </summary>
        public static string StageLabel(int stage)
        {
            return "STAGE " + stage.ToString(CultureInfo.InvariantCulture);
        }

        public static string RoundStageLabel(int round, int stage)
        {
            if (round < 1) throw new ArgumentOutOfRangeException(nameof(round));
            return "ROUND " + round.ToString(CultureInfo.InvariantCulture)
                + " · STAGE " + stage.ToString(CultureInfo.InvariantCulture)
                + " · " + ThemeName(stage);
        }

        public static string ThemeName(int stage)
        {
            switch (stage)
            {
                case 1: return "JONGNAM";
                case 2: return "BEGGAR CLAN";
                case 3: return "NAMGUNG";
                case 4: return "DEMON CULT";
                case 5: return "HEAVENLY DEMON";
                default: throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        public static string IntroLine(int stage)
        {
            switch (stage)
            {
                case 1: return "A Mount Hua disciple faces a first sparring match at Jongnam.";
                case 2: return "Pass the Beggar Clan's trial and hear of the Demon Cult's movements.";
                case 3: return "Cross swords with Namgung to prepare for the battle to come.";
                case 4: return "The battle to hold back the Demon Cult's army begins.";
                case 5: return "At the end of the war, the Heavenly Demon awaits.";
                default: throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }

        public static string ThemedBossName(int stage)
        {
            switch (stage)
            {
                case 1: return "JONGNAM MASTER";
                case 2: return "BEGGAR CLAN MASTER";
                case 3: return "NAMGUNG SWORDSMAN";
                case 4: return "DEMON CULT COMMANDER";
                case 5: return "HEAVENLY DEMON";
                default: throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }
    }
}
