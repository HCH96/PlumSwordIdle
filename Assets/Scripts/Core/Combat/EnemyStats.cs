using System;

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

        private const long NormalBaseMaxHealth = 30;
        private const long NormalMaxHealthPerStage = 10;
        private const long BossBaseMaxHealth = 100;
        private const long BossMaxHealthPerStage = 30;

        private const long NormalBaseAttack = 5;
        private const long NormalAttackPerStage = 2;
        private const long BossBaseAttack = 10;
        private const long BossAttackPerStage = 4;

        private const double NormalAttackInterval = 2.0;
        private const double BossAttackInterval = 2.0;

        private const double NormalAttackRange = 0.8;
        private const double BossAttackRange = 1.0;

        /// <summary>
        /// Normal: 30 + 10 * (stage - 1), Boss: 100 + 30 * (stage - 1). long 으로 계산.
        /// stage 가 1 보다 작거나 kind 가 정의되지 않은 값이면 ArgumentOutOfRangeException.
        /// </summary>
        public static long MaxHealth(EnemyKind kind, int stage)
        {
            switch (kind)
            {
                case EnemyKind.Normal:
                    return NormalBaseMaxHealth + NormalMaxHealthPerStage * StagesAboveFirst(stage);
                case EnemyKind.Boss:
                    return BossBaseMaxHealth + BossMaxHealthPerStage * StagesAboveFirst(stage);
                default:
                    throw InvalidKind(kind);
            }
        }

        /// <summary>
        /// Normal: 5 + 2 * (stage - 1), Boss: 10 + 4 * (stage - 1). long 으로 계산.
        /// stage 가 1 보다 작거나 kind 가 정의되지 않은 값이면 ArgumentOutOfRangeException.
        /// </summary>
        public static long Attack(EnemyKind kind, int stage)
        {
            switch (kind)
            {
                case EnemyKind.Normal:
                    return NormalBaseAttack + NormalAttackPerStage * StagesAboveFirst(stage);
                case EnemyKind.Boss:
                    return BossBaseAttack + BossAttackPerStage * StagesAboveFirst(stage);
                default:
                    throw InvalidKind(kind);
            }
        }

        /// <summary>Normal, Boss 모두 2.0 (초). 정의되지 않은 kind: ArgumentOutOfRangeException.</summary>
        public static double AttackInterval(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Normal:
                    return NormalAttackInterval;
                case EnemyKind.Boss:
                    return BossAttackInterval;
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

        // stage - 1 을 long 으로 돌려준다. 곱하기 전에 long 으로 바꿔 int 넘침을 막는다.
        private static long StagesAboveFirst(int stage)
        {
            if (stage < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(stage), stage, "스테이지는 1 이상이어야 한다.");
            }

            return (long)stage - 1;
        }

        private static ArgumentOutOfRangeException InvalidKind(EnemyKind kind)
        {
            return new ArgumentOutOfRangeException(nameof(kind), kind, "정의되지 않은 몬스터 종류다.");
        }
    }
}
