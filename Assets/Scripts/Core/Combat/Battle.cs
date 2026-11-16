using System;
using System.Collections.Generic;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Combat
{
    /// <summary>
    /// 제자와 적이 한 줄 위에서 싸우는 전투 로직. 0.02 초 칸 단위로 진행한다.
    /// docs/spec/combat.md "동작", "연결 지점".
    /// </summary>
    public sealed class Battle
    {
        /// <summary>한 칸의 길이 (초, 임시).</summary>
        public const double StepSeconds = 0.02;

        /// <summary>Tick 한 번에 받는 시간의 상한 (초, 임시).</summary>
        public const double MaxTickSeconds = 0.25;

        /// <summary>거리·대기 비교의 허용 오차.</summary>
        public const double Epsilon = 1e-9;

        public const long DiscipleBaseAttack = 10;
        public const long DiscipleBaseMaxHealth = 100;
        public const double DiscipleBaseAttackInterval = 1.0;

        /// <summary>제자 이동 속도 (초당 유닛, 임시).</summary>
        public const double DiscipleMoveSpeed = 2.0;

        /// <summary>제자 공격 범위 (앞쪽 유닛, 임시).</summary>
        public const double DiscipleAttackRange = 1.0;

        private readonly StatUpgrades _upgrades;
        private readonly List<Enemy> _enemies = new List<Enemy>();

        private double _accumulatedSeconds;
        private int _nextIndex;

        /// <summary>
        /// upgrades: 제자의 공격력·최대 체력·공격 간격을 읽을 곳. null 이면 ArgumentNullException.
        /// 시작 상태: 제자 좌표 0, 체력 = upgrades.MaxHealth, 공격 대기 0, 적 없음, 멈추지 않음, 패배 아님.
        /// </summary>
        public Battle(StatUpgrades upgrades)
        {
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            DisciplePosition = 0.0;
            DiscipleHealth = _upgrades.MaxHealth;
            DiscipleAttackCooldown = 0.0;
            IsDiscipleDefeated = false;
            Paused = false;
        }

        public event Action<Enemy> EnemyAdded;
        public event Action<Enemy> EnemyDamaged;
        public event Action DiscipleDamaged;
        public event Action<Enemy> EnemyDefeated;
        public event Action DiscipleDefeated;
        public event Action ResetCompleted;

        public double DisciplePosition { get; private set; }

        /// <summary>0 이상.</summary>
        public long DiscipleHealth { get; private set; }

        /// <summary>upgrades.MaxHealth 그대로.</summary>
        public long DiscipleMaxHealth => _upgrades.MaxHealth;

        /// <summary>다음 공격까지 남은 시간(초).</summary>
        public double DiscipleAttackCooldown { get; private set; }

        public bool IsDiscipleDefeated { get; private set; }

        public bool Paused { get; private set; }

        /// <summary>살아 있는 적. 추가된 순서.</summary>
        public IReadOnlyList<Enemy> Enemies => _enemies;

        private bool IsHalted => Paused || IsDiscipleDefeated;

        /// <summary>
        /// 시간을 받아 모으고, 0.02 초마다 Step 을 한 번씩 부른다. 남은 시간은 다음으로 넘긴다.
        /// 0: 아무 일도 없다. 음수·NaN·무한대: ArgumentOutOfRangeException. 0.25 보다 크면 0.25 로 잘라낸다.
        /// 멈췄거나 패배한 상태면 아무것도 하지 않고 시간도 모으지 않는다.
        /// </summary>
        public void Tick(double deltaSeconds)
        {
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "경과 시간은 0 이상의 유한한 값이어야 한다.");
            }

            if (IsHalted)
            {
                return;
            }

            if (deltaSeconds > MaxTickSeconds)
            {
                deltaSeconds = MaxTickSeconds;
            }

            _accumulatedSeconds += deltaSeconds;

            while (_accumulatedSeconds >= StepSeconds - Epsilon)
            {
                Step();
                _accumulatedSeconds = Math.Max(0.0, _accumulatedSeconds - StepSeconds);

                if (IsHalted)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 정확히 한 칸(0.02 초) 진행한다: 대기 감소 -> 이동 -> 공격 -> 쓰러짐 판정.
        /// 멈췄거나 패배한 상태면 아무것도 하지 않는다.
        /// </summary>
        public void Step()
        {
            if (IsHalted)
            {
                return;
            }

            ReduceCooldowns();
            MoveDisciple();
            MoveEnemies();
            // 공격 (009_battle_attack)
            // 쓰러짐 판정 (009_battle_attack)
        }

        /// <summary>
        /// 적을 추가하고 EnemyAdded 를 보낸 뒤 그 적을 돌려준다. 능력치는 EnemyStats 로 계산한다.
        /// stage &lt; 1, 정의되지 않은 kind, position 이 NaN·무한대이거나 DisciplePosition - Epsilon 보다 작으면
        /// ArgumentOutOfRangeException (아무것도 바꾸지 않는다). 멈췄거나 패배한 상태에서도 추가된다.
        /// </summary>
        public Enemy AddEnemy(EnemyKind kind, int stage, double position)
        {
            // 제자 좌표와의 비교에는 Epsilon 을 허용한다 (칸마다 더한 좌표의 소수 오차로 "같은 좌표" 가 거부되지 않게).
            if (double.IsNaN(position) || double.IsInfinity(position) || position < DisciplePosition - Epsilon)
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, "적의 좌표는 제자의 좌표 이상인 유한한 값이어야 한다.");
            }

            // EnemyStats 가 stage 와 kind 를 검사해 ArgumentOutOfRangeException 을 던진다. 상태를 바꾸기 전에 모두 계산한다.
            long maxHealth = EnemyStats.MaxHealth(kind, stage);
            long attack = EnemyStats.Attack(kind, stage);
            double attackInterval = EnemyStats.AttackInterval(kind);

            var enemy = new Enemy(kind, _nextIndex, position, maxHealth, attack, attackInterval);
            _nextIndex++;
            _enemies.Add(enemy);

            EnemyAdded?.Invoke(enemy);
            return enemy;
        }

        /// <summary>
        /// 제자를 좌표 0, 체력 가득, 공격 대기 0, 패배 아님으로 되돌리고 모든 적을 없앤 뒤
        /// ResetCompleted 를 보낸다. EnemyDefeated 는 보내지 않는다. Paused 는 바꾸지 않는다.
        /// </summary>
        public void Reset()
        {
            DisciplePosition = 0.0;
            DiscipleHealth = _upgrades.MaxHealth;
            DiscipleAttackCooldown = 0.0;
            IsDiscipleDefeated = false;
            _enemies.Clear();
            _nextIndex = 0;
            _accumulatedSeconds = 0.0;

            ResetCompleted?.Invoke();
        }

        /// <summary>멈추거나 푼다. 같은 값이면 아무 일도 없다. 알림은 없다.</summary>
        public void SetPaused(bool paused)
        {
            Paused = paused;
        }

        // 1. 대기 감소: 제자와 모든 적의 공격 대기를 한 칸만큼 줄인다. 0 아래로 내려가지 않는다.
        private void ReduceCooldowns()
        {
            DiscipleAttackCooldown = Math.Max(0.0, DiscipleAttackCooldown - StepSeconds);

            foreach (Enemy enemy in _enemies)
            {
                enemy.AttackCooldown = Math.Max(0.0, enemy.AttackCooldown - StepSeconds);
            }
        }

        // 2-1. 제자 이동: 가장 가까운 적의 사거리에 딱 맞춰 멈춘다. 적이 없으면 계속 전진한다.
        private void MoveDisciple()
        {
            double maxMove = DiscipleMoveSpeed * StepSeconds;

            if (_enemies.Count == 0)
            {
                DisciplePosition += maxMove;
                return;
            }

            double nearest = double.PositiveInfinity;
            foreach (Enemy enemy in _enemies)
            {
                nearest = Math.Min(nearest, enemy.Position - DisciplePosition);
            }

            if (nearest <= DiscipleAttackRange + Epsilon)
            {
                return;
            }

            DisciplePosition += Math.Min(maxMove, nearest - DiscipleAttackRange);
        }

        // 2-2. 적 이동: 일반 몬스터만, 추가된 순서대로, 제자가 움직인 뒤의 좌표로 계산한다. 보스는 움직이지 않는다.
        private void MoveEnemies()
        {
            double normalRange = EnemyStats.AttackRange(EnemyKind.Normal);
            double maxMove = EnemyStats.NormalMoveSpeed * StepSeconds;

            foreach (Enemy enemy in _enemies)
            {
                if (enemy.Kind != EnemyKind.Normal)
                {
                    continue;
                }

                double distance = enemy.Position - DisciplePosition;
                if (distance <= EnemyStats.NormalReactRange + Epsilon && distance > normalRange + Epsilon)
                {
                    enemy.Position -= Math.Min(maxMove, distance - normalRange);
                }
            }
        }
    }
}
