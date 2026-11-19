using System;

namespace PlumSword.Core.Combat
{
    /// <summary>
    /// 전투 화면의 표시 규칙. 상태가 없다. 거리 계산에는 쓰지 않는다.
    /// 수치는 임시값. docs/spec/combat.md "수치".
    /// </summary>
    public static class CombatLayout
    {
        public const double SkillSlashSeconds = 0.25;

        public static double SkillSlashAlpha(double elapsedSeconds)
        {
            if (double.IsNaN(elapsedSeconds) || elapsedSeconds >= SkillSlashSeconds) return 0.0;
            if (elapsedSeconds <= 0.0) return 0.6;
            return 0.6 * (1.0 - elapsedSeconds / SkillSlashSeconds);
        }

        private const double EnemyOffsetStep = 0.15;

        /// <summary>
        /// 겹친 일반 몬스터의 화면상 위아래 어긋남. ((index % 3) - 1) * 0.15.
        /// 예: 0 -> -0.15, 1 -> 0, 2 -> 0.15, 3 -> -0.15. 음수: ArgumentOutOfRangeException.
        /// </summary>
        public static double EnemyOffsetY(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "순번은 0 이상이어야 한다.");
            }

            return ((index % 3) - 1) * EnemyOffsetStep;
        }

        /// <summary>
        /// 체력 바가 찬 비율. (double)health / maxHealth 를 0.0 ~ 1.0 으로 잘라낸다.
        /// maxHealth 가 0 이하면 0.0 (예외 없음).
        /// </summary>
        public static double HealthRatio(long health, long maxHealth)
        {
            if (maxHealth <= 0 || health <= 0)
            {
                return 0.0;
            }

            if (health >= maxHealth)
            {
                return 1.0;
            }

            return (double)health / maxHealth;
        }
    }
}
