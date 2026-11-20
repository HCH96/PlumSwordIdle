namespace PlumSword.Core.Combat
{
    /// <summary>
    /// 전투 중인 적 하나. Battle.AddEnemy 만 만든다 (생성자는 internal).
    /// 값은 Battle 만 바꾼다 (설정은 internal). docs/spec/combat.md "연결 지점".
    /// </summary>
    public sealed class Enemy
    {
        internal Enemy(
            EnemyKind kind,
            int index,
            double position,
            long maxHealth,
            long attack,
            double attackInterval,
            bool isJongnamBoss)
        {
            Kind = kind;
            Index = index;
            Position = position;
            MaxHealth = maxHealth;
            Health = maxHealth;
            Attack = attack;
            AttackInterval = attackInterval;
            AttackCooldown = 0.0;
            IsDead = false;
            IsJongnamBoss = isJongnamBoss;
        }

        public EnemyKind Kind { get; }

        public bool IsJongnamBoss { get; }

        public int AttackCount { get; internal set; }

        /// <summary>Reset 뒤 0 부터, AddEnemy 마다 1 씩 는다.</summary>
        public int Index { get; }

        /// <summary>진행 방향 좌표 (유닛).</summary>
        public double Position { get; internal set; }

        public long MaxHealth { get; }

        /// <summary>0 이상 MaxHealth 이하.</summary>
        public long Health { get; internal set; }

        public long Attack { get; }

        /// <summary>공격 간격 (초).</summary>
        public double AttackInterval { get; }

        /// <summary>다음 공격까지 남은 시간(초). 0 이면 바로 공격할 수 있다.</summary>
        public double AttackCooldown { get; internal set; }

        /// <summary>쓰러짐 판정에서 Enemies 에서 빠질 때 true.</summary>
        public bool IsDead { get; internal set; }
    }
}
