using System;
using PlumSword.Core.Currency;

namespace PlumSword.Core.StatUpgrade
{
    /// <summary>
    /// 공격력·체력·공격 속도의 강화 단계와 비용, 금화를 쓰는 강화, 강화가 반영된 최종 능력치, 잠금을 맡는다.
    /// 현재 체력과 공격 대기 시간은 들고 있지 않는다 (전투 기능의 것).
    /// </summary>
    public sealed class StatUpgrades
    {
        public const int AttackSpeedMaxLevel = 10;
        public const double MinAttackInterval = 0.5;

        // 수치 (임시값). docs/spec/stat_upgrade.md "수치".
        private const long BaseCost = 20;
        private const long CostPerLevel = 5;
        private const long AttackPerLevel = 2;
        private const long MaxHealthPerLevel = 20;
        private const double AttackIntervalPerLevel = 0.05;

        private readonly Wallet _wallet;
        private readonly long _baseAttack;
        private readonly long _baseMaxHealth;
        private readonly double _baseAttackInterval;

        private int _attackLevel;
        private int _healthLevel;
        private int _attackSpeedLevel;

        /// <summary>
        /// wallet: 강화 비용을 쓸 지갑. null 이면 ArgumentNullException.
        /// baseAttack, baseMaxHealth: 1 보다 작으면 ArgumentOutOfRangeException.
        /// baseAttackInterval: 0.5 보다 작거나 NaN 이거나 무한대면 ArgumentOutOfRangeException.
        /// 단계 셋: 음수면 0, attackSpeedLevel 이 10 보다 크면 10 으로 만든다 (예외 없음).
        /// 금화를 쓰지 않고 알림을 보내지 않는다. 잠기지 않은 상태로 시작한다.
        /// </summary>
        public StatUpgrades(
            Wallet wallet,
            long baseAttack,
            long baseMaxHealth,
            double baseAttackInterval,
            int attackLevel = 0,
            int healthLevel = 0,
            int attackSpeedLevel = 0)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            if (baseAttack < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(baseAttack), baseAttack, "시작 공격력은 1 이상이어야 한다.");
            }

            if (baseMaxHealth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(baseMaxHealth), baseMaxHealth, "시작 최대 체력은 1 이상이어야 한다.");
            }

            // NaN 은 모든 비교가 false 라서 따로 검사한다.
            if (double.IsNaN(baseAttackInterval) || double.IsInfinity(baseAttackInterval) || baseAttackInterval < MinAttackInterval)
            {
                throw new ArgumentOutOfRangeException(nameof(baseAttackInterval), baseAttackInterval, "시작 공격 간격은 0.5 이상의 유한한 값이어야 한다.");
            }

            _wallet = wallet;
            _baseAttack = baseAttack;
            _baseMaxHealth = baseMaxHealth;
            _baseAttackInterval = baseAttackInterval;

            _attackLevel = Math.Max(0, attackLevel);
            _healthLevel = Math.Max(0, healthLevel);
            _attackSpeedLevel = Math.Min(AttackSpeedMaxLevel, Math.Max(0, attackSpeedLevel));
        }

        /// <summary>강화가 성공해 단계가 오른 뒤에 한 번, 오른 능력치를 담아 보낸다.</summary>
        public event Action<StatType> Upgraded;

        /// <summary>잠금 값이 바뀐 뒤에 한 번, 새 값을 담아 보낸다.</summary>
        public event Action<bool> LockedChanged;

        /// <summary>baseAttack + 2 * 공격력 단계.</summary>
        public long Attack => _baseAttack + AttackPerLevel * (long)_attackLevel;

        /// <summary>baseMaxHealth + 20 * 체력 단계.</summary>
        public long MaxHealth => _baseMaxHealth + MaxHealthPerLevel * (long)_healthLevel;

        /// <summary>Math.Max(0.5, baseAttackInterval - 0.05 * 공격 속도 단계), 초.</summary>
        public double AttackInterval =>
            Math.Max(MinAttackInterval, _baseAttackInterval - AttackIntervalPerLevel * _attackSpeedLevel);

        /// <summary>잠겨 있으면 true. 처음 값 false.</summary>
        public bool Locked { get; private set; }

        /// <summary>지금 강화 단계. 정의되지 않은 stat: ArgumentOutOfRangeException.</summary>
        public int GetLevel(StatType stat)
        {
            switch (stat)
            {
                case StatType.Attack:
                    return _attackLevel;
                case StatType.Health:
                    return _healthLevel;
                case StatType.AttackSpeed:
                    return _attackSpeedLevel;
                default:
                    throw InvalidStat(stat);
            }
        }

        /// <summary>최대 단계. AttackSpeed 는 10, Attack 과 Health 는 int.MaxValue.</summary>
        public int GetMaxLevel(StatType stat)
        {
            switch (stat)
            {
                case StatType.Attack:
                case StatType.Health:
                    return int.MaxValue;
                case StatType.AttackSpeed:
                    return AttackSpeedMaxLevel;
                default:
                    throw InvalidStat(stat);
            }
        }

        /// <summary>GetLevel(stat) >= GetMaxLevel(stat) 이면 true.</summary>
        public bool IsMaxLevel(StatType stat)
        {
            return GetLevel(stat) >= GetMaxLevel(stat);
        }

        /// <summary>다음 강화에 드는 금화. 20 + 5 * 단계. 최대 단계여도 같은 식의 값.</summary>
        public long GetCost(StatType stat)
        {
            return BaseCost + CostPerLevel * (long)GetLevel(stat);
        }

        /// <summary>잠기지 않았고, 최대 단계가 아니고, 금화가 비용 이상이면 true. 아무것도 바꾸지 않는다.</summary>
        public bool CanUpgrade(StatType stat)
        {
            ThrowIfUndefined(stat);

            if (Locked || IsMaxLevel(stat))
            {
                return false;
            }

            return _wallet.CanSpend(GetCost(stat));
        }

        /// <summary>
        /// 강화한다. 성공하면 금화를 쓰고 단계를 1 올린 뒤 Upgraded 를 보내고 true.
        /// 잠김, 최대 단계, 금화 부족이면 아무것도 바꾸지 않고 false (알림 없음).
        /// </summary>
        public bool TryUpgrade(StatType stat)
        {
            ThrowIfUndefined(stat);

            if (Locked || IsMaxLevel(stat))
            {
                return false;
            }

            if (!_wallet.TrySpend(GetCost(stat)))
            {
                return false;
            }

            switch (stat)
            {
                case StatType.Attack:
                    _attackLevel++;
                    break;
                case StatType.Health:
                    _healthLevel++;
                    break;
                case StatType.AttackSpeed:
                    _attackSpeedLevel++;
                    break;
            }

            Upgraded?.Invoke(stat);
            return true;
        }

        /// <summary>잠그거나 푼다. 값이 실제로 바뀐 때에만, 바꾼 뒤에 LockedChanged 를 보낸다.</summary>
        public void SetLocked(bool locked)
        {
            if (Locked == locked)
            {
                return;
            }

            Locked = locked;
            LockedChanged?.Invoke(locked);
        }

        private static void ThrowIfUndefined(StatType stat)
        {
            if (stat != StatType.Attack && stat != StatType.Health && stat != StatType.AttackSpeed)
            {
                throw InvalidStat(stat);
            }
        }

        private static ArgumentOutOfRangeException InvalidStat(StatType stat)
        {
            return new ArgumentOutOfRangeException(nameof(stat), stat, "정의되지 않은 능력치다.");
        }
    }
}
