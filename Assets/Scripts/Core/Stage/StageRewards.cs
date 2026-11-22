using PlumSword.Core.Balance;

namespace PlumSword.Core.Stage
{
    /// <summary>
    /// 처치 금화를 계산한다. 상태가 없는 정적 메서드만 가진다.
    /// 수치는 임시값이다 (docs/spec/stage.md "수치").
    /// </summary>
    public static class StageRewards
    {
        /// <summary>
        /// 수치 표의 일반 몬스터 금화. 기존 호출은 1구간을 사용한다.
        /// </summary>
        public static long NormalGold(int stage)
            => NormalGold(stage, 1);

        /// <summary>수치 표의 구간별 일반 몬스터 금화. 유효하지 않은 번호는 예외다.</summary>
        public static long NormalGold(int stage, int substage)
        {
            var table = BalanceTable.Current;
            return table.NormalGold.Evaluate(table.SubstageIndex(stage, substage));
        }

        /// <summary>
        /// 수치 표의 보스 금화. 기존 호출은 1구간을 사용한다.
        /// </summary>
        public static long BossGold(int stage)
            => BossGold(stage, 1);

        /// <summary>수치 표의 구간별 보스 금화. 유효하지 않은 번호는 예외다.</summary>
        public static long BossGold(int stage, int substage)
        {
            var table = BalanceTable.Current;
            return table.BossGold.Evaluate(table.SubstageIndex(stage, substage));
        }
    }
}
