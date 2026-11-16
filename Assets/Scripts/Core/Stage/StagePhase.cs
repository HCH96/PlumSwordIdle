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
    }
}
