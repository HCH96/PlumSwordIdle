using System;
using PlumSword.Core.Balance;

namespace PlumSword.Core.Combat
{
    /// <summary>
    /// 몬스터 종류와 스테이지 번호로 몬스터 능력치를 계산한다. 상태가 없다.
    /// 수치는 모두 임시값. docs/spec/combat.md "수치".
    /// </summary>
    public static class EnemyStats
    {
        /// <summary>일반 몬스터가 제자에게 다가오기 시작하는 거리 (유닛).</summary>
        public const double NormalReactRange = 3.0;

        /// <summary>일반 몬스터의 이동 속도 (초당 유닛).</summary>
        public const double NormalMoveSpeed = 1.0;

        private const double NormalAttackRange = 0.8;
        private const double BossAttackRange = 1.0;

        /// <summary>
        /// 수치 표의 종류별 체력 곡선. 기존 호출은 1구간을 사용한다.
        /// </summary>
        public static long MaxHealth(EnemyKind kind, int stage)
            => MaxHealth(kind, stage, 1);

        /// <summary>수치 표의 구간별 체력. 유효하지 않은 stage, substage, kind는 예외다.</summary>
        public static long MaxHealth(EnemyKind kind, int stage, int substage)
        {
            var table = BalanceTable.Current;
            double n = table.SubstageIndex(stage, substage);
            switch (kind)
            {
                case EnemyKind.Normal:
                    return table.NormalHealth.Evaluate(n);
                case EnemyKind.Boss:
                    return (stage - 1) % 5 == 4 && table.FinalBossHealth > 0
                        ? table.FinalBossHealth : table.BossHealth.Evaluate(n);
                default:
                    throw InvalidKind(kind);
            }
        }

        /// <summary>
        /// 수치 표의 종류별 공격력 곡선. 기존 호출은 1구간을 사용한다.
        /// </summary>
        public static long Attack(EnemyKind kind, int stage)
            => Attack(kind, stage, 1);

        /// <summary>수치 표의 구간별 공격력. 유효하지 않은 stage, substage, kind는 예외다.</summary>
        public static long Attack(EnemyKind kind, int stage, int substage)
        {
            var table = BalanceTable.Current;
            double n = table.SubstageIndex(stage, substage);
            switch (kind)
            {
                case EnemyKind.Normal:
                    return table.NormalAttack.Evaluate(n);
                case EnemyKind.Boss:
                    return (stage - 1) % 5 == 4 && table.FinalBossAttack > 0
                        ? table.FinalBossAttack : table.BossAttack.Evaluate(n);
                default:
                    throw InvalidKind(kind);
            }
        }

        /// <summary>수치 표의 종류별 공격 간격(초). 정의되지 않은 kind: ArgumentOutOfRangeException.</summary>
        public static double AttackInterval(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Normal:
                    return BalanceTable.Current.NormalAttackInterval;
                case EnemyKind.Boss:
                    return BalanceTable.Current.BossAttackInterval;
                default:
                    throw InvalidKind(kind);
            }
        }

        /// <summary>Normal: 0.8, Boss: 1.0 (유닛). 정의되지 않은 kind: ArgumentOutOfRangeException.</summary>
        public static double AttackRange(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Normal:
                    return NormalAttackRange;
                case EnemyKind.Boss:
                    return BossAttackRange;
                default:
                    throw InvalidKind(kind);
            }
        }

        private static ArgumentOutOfRangeException InvalidKind(EnemyKind kind)
        {
            return new ArgumentOutOfRangeException(nameof(kind), kind, "정의되지 않은 몬스터 종류다.");
        }
    }
}
