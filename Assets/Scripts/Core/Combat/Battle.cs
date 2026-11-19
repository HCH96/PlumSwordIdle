using System;
using System.Collections.Generic;
using PlumSword.Core.Realm;
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
        public const double DiscipleMoveSpeed = 6.48;

        /// <summary>제자 공격 범위 (앞쪽 유닛, 임시).</summary>
        public const double DiscipleAttackRange = 1.0;

        /// <summary>
        /// 앞 동작 비율 (임시). 제자가 전진해 범위 안에 적이 없다가 생긴 칸(달려와 멈춘 칸)에는 바로 공격하지 않고,
        /// 공격 대기를 적어도 공격 간격 × 이 값으로 올린다. 공격 애니메이션 17 프레임 중 타격 프레임이 7번이라 7/17 이다.
        /// </summary>
        public const double DiscipleAttackWindUpRatio = 7.0 / 17.0;

        private readonly StatUpgrades _upgrades;
        private readonly List<Enemy> _enemies = new List<Enemy>();

        private double _accumulatedSeconds;
        private int _nextIndex;

        // 체력 강화·돌파의 늘어난 양을 구하려고 기억해 두는 직전 최대 체력.
        private long _lastMaxHealth;

        // Reset 마다 1 씩 는다. 한 칸 진행 중 알림을 보낸 뒤 값이 바뀌었으면 알림 안에서 Reset 이 불린 것이므로 그 칸을 그만둔다.
        private int _resetGeneration;

        // 직전 칸의 이동이 끝났을 때 제자의 공격 범위 안에 적이 있었는가. 앞 동작을 시작할 칸을 가린다.
        private bool _wasEngaged;

        /// <summary>
        /// upgrades: 제자의 공격력·최대 체력을 읽을 곳. null 이면 ArgumentNullException.
        /// realm: 돌파 알림을 받을 곳. null 이면 구독하지 않는다.
        /// 시작 상태: 제자 좌표 0, 체력 = upgrades.MaxHealth, 공격 대기 0, 적 없음, 멈추지 않음, 패배 아님.
        /// </summary>
        public Battle(StatUpgrades upgrades, RealmProgress realm = null)
        {
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            DisciplePosition = 0.0;
            DiscipleHealth = _upgrades.MaxHealth;
            DiscipleAttackCooldown = 0.0;
            IsDiscipleDefeated = false;
            Paused = false;
            _lastMaxHealth = _upgrades.MaxHealth;

            // Battle 과 StatUpgrades 는 게임에 하나씩이고 수명이 같아 구독을 끊지 않는다.
            _upgrades.Upgraded += OnUpgraded;
            if (realm != null)
            {
                realm.BrokeThrough += OnBrokeThrough;
            }
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

        /// <summary>DiscipleBaseAttackInterval 그대로 (고정 1초, 임시). 다음 공격 뒤에 정해질 대기 시간(초). 화면의 공격 애니메이션 속도에 쓴다.</summary>
        public double DiscipleAttackInterval => DiscipleBaseAttackInterval;

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
        /// 정확히 한 칸(0.02 초) 진행한다: 대기 감소 -> 이동 -> 앞 동작 시작 -> 공격 -> 쓰러짐 판정.
        /// 멈췄거나 패배한 상태면 아무것도 하지 않는다.
        /// </summary>
        public void Step()
        {
            if (IsHalted)
            {
                return;
            }

            // 한 칸 동안 적 목록은 칸을 시작할 때 떠 둔 복사본을 돈다 (알림 안에서 목록이 바뀌어도 안전하게).
            // 알림 안에서 추가된 적은 이 복사본에 없으므로 다음 칸부터 움직이고 공격한다.
            var snapshot = new List<Enemy>(_enemies);
            int generation = _resetGeneration;

            ReduceCooldowns(snapshot);
            double positionBeforeMove = DisciplePosition;
            MoveDisciple(snapshot);
            MoveEnemies(snapshot);
            BeginWindUp(snapshot, DisciplePosition > positionBeforeMove);

            if (!AttackByDisciple(snapshot, generation))
            {
                return;
            }

            if (!AttackByEnemies(snapshot, generation))
            {
                return;
            }

            ResolveDefeats(snapshot, generation);
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
            _lastMaxHealth = _upgrades.MaxHealth;
            _wasEngaged = false;
            _resetGeneration++;

            ResetCompleted?.Invoke();
        }

        /// <summary>멈추거나 푼다. 같은 값이면 아무 일도 없다. 알림은 없다.</summary>
        public void SetPaused(bool paused)
        {
            Paused = paused;
        }

        // 1. 대기 감소: 제자와 모든 적의 공격 대기를 한 칸만큼 줄인다. 0 아래로 내려가지 않는다.
        private void ReduceCooldowns(List<Enemy> enemies)
        {
            DiscipleAttackCooldown = Math.Max(0.0, DiscipleAttackCooldown - StepSeconds);

            foreach (Enemy enemy in enemies)
            {
                enemy.AttackCooldown = Math.Max(0.0, enemy.AttackCooldown - StepSeconds);
            }
        }

        // 2-1. 제자 이동: 가장 가까운 적의 사거리에 딱 맞춰 멈춘다. 적이 없으면 계속 전진한다.
        private void MoveDisciple(List<Enemy> enemies)
        {
            double maxMove = DiscipleMoveSpeed * StepSeconds;

            if (enemies.Count == 0)
            {
                DisciplePosition += maxMove;
                return;
            }

            double nearest = double.PositiveInfinity;
            foreach (Enemy enemy in enemies)
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
        private void MoveEnemies(List<Enemy> enemies)
        {
            double normalRange = EnemyStats.AttackRange(EnemyKind.Normal);
            double maxMove = EnemyStats.NormalMoveSpeed * StepSeconds;

            foreach (Enemy enemy in enemies)
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

        // 2-3. 앞 동작: 이번 칸에 제자가 전진했고, 직전 칸에는 범위 안에 적이 없다가 이번 칸에 생겼으면(달려와 멈춤)
        // 공격 대기를 적어도 공격 간격 × 앞 동작 비율로 올린다. 그래서 피해는 멈추자마자가 아니라 앞 동작 시간 뒤에 들어간다.
        // 제자가 서 있는 채로 범위 안에 적이 생기는 경우(예: 처음부터 범위 안에 놓인 적)는 지금처럼 바로 공격한다.
        private void BeginWindUp(List<Enemy> enemies, bool discipleMoved)
        {
            bool engaged = false;
            foreach (Enemy enemy in enemies)
            {
                if (enemy.Position - DisciplePosition <= DiscipleAttackRange + Epsilon)
                {
                    engaged = true;
                    break;
                }
            }

            if (engaged && !_wasEngaged && discipleMoved)
            {
                DiscipleAttackCooldown = Math.Max(DiscipleAttackCooldown, DiscipleBaseAttackInterval * DiscipleAttackWindUpRatio);
            }

            _wasEngaged = engaged;
        }

        // 3-1. 제자 공격: 대기가 끝났고 범위 안에 적이 있으면, 범위 안의 모든 적에게 같은 피해를 준다.
        // 알림 안에서 Reset 이 불려 칸을 그만둬야 하면 false.
        private bool AttackByDisciple(List<Enemy> enemies, int generation)
        {
            if (DiscipleAttackCooldown > Epsilon)
            {
                return true;
            }

            var hits = new List<Enemy>();
            foreach (Enemy enemy in enemies)
            {
                if (enemy.Position - DisciplePosition <= DiscipleAttackRange + Epsilon)
                {
                    hits.Add(enemy);
                }
            }

            if (hits.Count == 0)
            {
                return true;
            }

            // 공격력은 피해를 줄 때마다 읽는다 (따로 저장해 두지 않는다).
            long damage = _upgrades.Attack;
            foreach (Enemy enemy in hits)
            {
                enemy.Health = ReduceHealth(enemy.Health, damage);
            }

            foreach (Enemy enemy in hits)
            {
                EnemyDamaged?.Invoke(enemy);
                if (generation != _resetGeneration)
                {
                    return false;
                }
            }

            // 다음 대기는 고정 공격 간격(DiscipleBaseAttackInterval)이다.
            DiscipleAttackCooldown = DiscipleBaseAttackInterval;
            return true;
        }

        // 3-2. 적 공격: 추가된 순서대로. 이번 칸에 체력이 0이 된 적도 공격한다.
        // 알림 안에서 Reset 이 불려 칸을 그만둬야 하면 false.
        private bool AttackByEnemies(List<Enemy> enemies, int generation)
        {
            foreach (Enemy enemy in enemies)
            {
                if (enemy.AttackCooldown > Epsilon)
                {
                    continue;
                }

                double distance = enemy.Position - DisciplePosition;
                if (distance > EnemyStats.AttackRange(enemy.Kind) + Epsilon)
                {
                    continue;
                }

                DiscipleHealth = ReduceHealth(DiscipleHealth, enemy.Attack);
                DiscipleDamaged?.Invoke();
                if (generation != _resetGeneration)
                {
                    return false;
                }

                enemy.AttackCooldown = enemy.AttackInterval;
            }

            return true;
        }

        // 4. 쓰러짐 판정: 값을 모두 바꾼 뒤 알림을 보낸다 (EnemyDefeated 를 추가된 순서대로, 그다음 DiscipleDefeated).
        // 알림 안에서 Reset 이 불리면 남은 알림을 보내지 않는다.
        private void ResolveDefeats(List<Enemy> enemies, int generation)
        {
            var defeated = new List<Enemy>();
            foreach (Enemy enemy in enemies)
            {
                if (enemy.Health <= 0)
                {
                    defeated.Add(enemy);
                }
            }

            foreach (Enemy enemy in defeated)
            {
                _enemies.Remove(enemy);
                enemy.IsDead = true;
            }

            bool discipleDefeatedNow = DiscipleHealth <= 0;
            if (discipleDefeatedNow)
            {
                IsDiscipleDefeated = true;
            }

            foreach (Enemy enemy in defeated)
            {
                EnemyDefeated?.Invoke(enemy);
                if (generation != _resetGeneration)
                {
                    return;
                }
            }

            if (discipleDefeatedNow)
            {
                DiscipleDefeated?.Invoke();
            }
        }

        // 체력 강화와 돌파는 멈춘 동안에도 반영하고 알림을 보내지 않는다.
        private void OnUpgraded(StatType stat)
        {
            if (stat != StatType.Health)
            {
                return;
            }

            ApplyMaxHealthIncrease();
        }

        private void OnBrokeThrough(RealmLevel level)
        {
            ApplyMaxHealthIncrease();
        }

        // 패배한 상태면 직전 최대 체력의 기억만 바꾼다.
        private void ApplyMaxHealthIncrease()
        {
            long maxHealth = _upgrades.MaxHealth;
            long increase = maxHealth - _lastMaxHealth;
            _lastMaxHealth = maxHealth;

            if (IsDiscipleDefeated)
            {
                return;
            }

            DiscipleHealth += increase;
        }

        // 체력을 피해만큼 줄인다. 0 아래로 내려가지 않는다.
        private static long ReduceHealth(long health, long damage)
        {
            return Math.Max(0L, health - damage);
        }
    }
}
