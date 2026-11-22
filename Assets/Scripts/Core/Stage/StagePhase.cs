namespace PlumSword.Core.Stage
{
    /// <summary>스테이지 진행의 단계 (docs/spec/stage.md "동작").</summary>
    public enum StagePhase
    {
        /// <summary>전투 중.</summary>
        Playing = 0,

        /// <summary>성공·실패 뒤 화면을 가리는 중.</summary>
        FadingOut = 1,

        /// <summary>새 배치를 마치고 화면을 여는 중.</summary>
        FadingIn = 2,

        /// <summary>1회차 첫 진입의 소개를 보여 주는 중. 전투는 멈추고 강화는 열려 있다.</summary>
        Intro = 3,

        /// <summary>작은 스테이지 번호를 잠시 표시하는 동안 전투를 멈춘다.</summary>
        SubstageIntro = 4,

        /// <summary>마지막 적을 이긴 뒤 전환까지 기다리는 중. 전투는 계속되고 제자는 제자리에 선다.</summary>
        ClearWait = 5,
    }
}
