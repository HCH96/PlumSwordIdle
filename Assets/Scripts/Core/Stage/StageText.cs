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
    }
}
