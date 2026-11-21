using System;
using PlumSword.Core.Stage;

namespace PlumSword.Core.Audio
{
    public static class MusicRules
    {
        public static MusicTrack TrackFor(bool isStartScreenShowing, int displayStageNumber, bool hasBoss)
        {
            if (displayStageNumber < 1 || displayStageNumber > StageLayout.FinalDisplayStage)
                throw new ArgumentOutOfRangeException(nameof(displayStageNumber));
            if (isStartScreenShowing) return MusicTrack.Title;
            if (!hasBoss) return MusicTrack.BattleNormal;
            return displayStageNumber == StageLayout.FinalDisplayStage ? MusicTrack.BattleFinalBoss : MusicTrack.BattleBoss;
        }
    }
}
