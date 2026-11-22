using System;
using PlumSword.Core.Balance;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;

namespace PlumSword.Core.StatUpgrade
{
    /// <summary>
    /// 공격력·체력·내공의 강화 단계와 비용, 금화를 쓰는 강화, 공격력·체력의 최종 능력치, 잠금을 맡는다.
    /// 최종 공격력과 최대 체력은 읽을 때마다 현재 경지 보너스를 적용한다.
    /// 현재 체력, 공격 간격과 공격 대기 시간은 들고 있지 않는다 (전투 기능의 것).
    /// </summary>
    public sealed class StatUpgrades
    {
        private readonly Wallet _wallet;
        private readonly RealmProgress _realm;
        private readonly long _baseAttack;
        private readonly long _baseMaxHealth;

        private int _attackLevel;
        private int _healthLevel;
        private int _innerPowerLevel;

        /// <summary>
        /// wallet: 강화 비용을 쓸 지갑. null 이면 ArgumentNullException.
        /// baseAttack, baseMaxHealth: 1 보다 작으면 ArgumentOutOfRangeException.
        /// 단계 셋: 음수면 0 으로 만든다 (예외 없음).
        /// realm: 경지 보너스를 읽을 곳. null 이면 0%; 구독하거나 변경하지 않는다.
        /// 금화를 쓰지 않고 알림을 보내지 않는다. 잠기지 않은 상태로 시작한다.
        /// </summary>
        public StatUpgrades(
            Wallet wallet,
            long baseAttack,
            long baseMaxHealth,
            int attackLevel = 0,
            int healthLevel = 0,
            int innerPowerLevel = 0,
            RealmProgress realm = null)
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

            _wallet = wallet;
            _realm = realm;
            _baseAttack = baseAttack;
            _baseMaxHealth = baseMaxHealth;

            _attackLevel = Math.Max(0, attackLevel);
            _healthLevel = Math.Max(0, healthLevel);
            _innerPowerLevel = Math.Max(0, innerPowerLevel);
        }

        /// <summary>강화가 성공해 단계가 오른 뒤에 한 번, 오른 능력치를 담아 보낸다.</summary>
        public event Action<StatType> Upgraded;

        /// <summary>잠금 값이 바뀐 뒤에 한 번, 새 값을 담아 보낸다.</summary>
        public event Action<bool> LockedChanged;

        /// <summary>수치 표의 단계당 공격력과 현재 경지 보너스를 적용한다.</summary>
        public long Attack => (_baseAttack + BalanceTable.Current.AttackPerLevel * (long)_attackLevel) * (100L + (_realm?.BonusPercent ?? 0)) / 100L;

        /// <summary>수치 표의 단계당 체력과 현재 경지 보너스를 적용한다.</summary>
        public long MaxHealth => (_baseMaxHealth + BalanceTable.Current.HealthPerLevel * (long)_healthLevel) * (100L + (_realm?.BonusPercent ?? 0)) / 100L;

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
                case StatType.InnerPower:
                    return _innerPowerLevel;
                default:
                    throw InvalidStat(stat);
            }
        }

        /// <summary>최대 단계. 세 능력치 모두 int.MaxValue.</summary>
        public int GetMaxLevel(StatType stat)
        {
            switch (stat)
            {
                case StatType.Attack:
                case StatType.Health:
                case StatType.InnerPower:
                    return int.MaxValue;
                default:
                    throw InvalidStat(stat);
            }
        }

        /// <summary>GetLevel(stat) >= GetMaxLevel(stat) 이면 true.</summary>
        public bool IsMaxLevel(StatType stat)
        {
            return GetLevel(stat) >= GetMaxLevel(stat);
        }

        /// <summary>다음 강화에 드는 금화. 수치 표의 해당 비용 곡선을 현재 단계에 적용한다.</summary>
        public long GetCost(StatType stat)
        {
            int level = GetLevel(stat);
            switch (stat)
            {
                case StatType.Attack: return BalanceTable.Current.AttackCost.Evaluate(level);
                case StatType.Health: return BalanceTable.Current.HealthCost.Evaluate(level);
                case StatType.InnerPower: return BalanceTable.Current.InnerPowerCost.Evaluate(level);
                default: throw InvalidStat(stat);
            }
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
                case StatType.InnerPower:
                    _innerPowerLevel++;
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
            if (stat != StatType.Attack && stat != StatType.Health && stat != StatType.InnerPower)
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
