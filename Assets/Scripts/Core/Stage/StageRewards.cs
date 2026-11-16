using System;

namespace PlumSword.Core.Stage
{
    /// <summary>
    /// 처치 금화를 계산한다. 상태가 없는 정적 메서드만 가진다.
    /// 수치는 임시값이다 (docs/spec/stage.md "수치").
    /// </summary>
    public static class StageRewards
    {
        /// <summary>
        /// 일반 몬스터 한 마리의 금화. 10 + 2 * (stage - 1). long 으로 계산한다.
        /// stage 가 1 보다 작으면 ArgumentOutOfRangeException.
        /// </summary>
        public static long NormalGold(int stage)
        {
            ValidateStage(stage);
            return 10L + 2L * ((long)stage - 1L);
        }

        /// <summary>
        /// 보스의 금화. 50 + 10 * (stage - 1). long 으로 계산한다.
        /// stage 가 1 보다 작으면 ArgumentOutOfRangeException.
        /// </summary>
        public static long BossGold(int stage)
        {
            ValidateStage(stage);
            return 50L + 10L * ((long)stage - 1L);
        }

        private static void ValidateStage(int stage)
        {
            if (stage < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(stage), stage, "stage must be 1 or greater.");
            }
        }
    }
}
