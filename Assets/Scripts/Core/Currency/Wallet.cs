using System;

namespace PlumSword.Core.Currency
{
    /// <summary>
    /// 보유 금화(잔액)를 들고 있는 지갑. 지급·소비·확인과 잔액 변경 알림을 제공한다.
    /// 잔액은 항상 0 이상 long.MaxValue 이하다.
    /// </summary>
    public sealed class Wallet
    {
        /// <summary>
        /// 저장된 값으로 되살릴 때는 initialBalance 에 저장된 금화를 넣는다.
        /// 음수면 0 으로 만든다. 예외를 던지지 않는다. BalanceChanged 를 보내지 않는다.
        /// </summary>
        public Wallet(long initialBalance = 0)
        {
            Balance = initialBalance < 0 ? 0 : initialBalance;
        }

        /// <summary>지금 보유 금화. 항상 0 이상 long.MaxValue 이하.</summary>
        public long Balance { get; private set; }

        /// <summary>잔액이 실제로 바뀐 뒤에 한 번, 새 잔액을 담아 보낸다. 바뀌지 않으면 보내지 않는다.</summary>
        public event Action<long> BalanceChanged;

        /// <summary>
        /// 금화를 준다. 0: 아무 일도 없다 (알림 없음). 음수: ArgumentOutOfRangeException.
        /// long.MaxValue 를 넘으면 long.MaxValue 에서 멈춘다.
        /// </summary>
        public void Add(long amount)
        {
            ThrowIfNegative(amount, nameof(amount));

            var newBalance = Balance > long.MaxValue - amount ? long.MaxValue : Balance + amount;
            SetBalance(newBalance);
        }

        /// <summary>amount 만큼 쓸 수 있으면 true. 아무것도 바꾸지 않는다. 0: true. 음수: ArgumentOutOfRangeException.</summary>
        public bool CanSpend(long amount)
        {
            ThrowIfNegative(amount, nameof(amount));
            return Balance >= amount;
        }

        /// <summary>
        /// 잔액이 amount 이상이면 줄이고 true, 모자라면 그대로 두고 false (알림 없음).
        /// 0: true (알림 없음). 음수: ArgumentOutOfRangeException.
        /// </summary>
        public bool TrySpend(long amount)
        {
            if (!CanSpend(amount))
            {
                return false;
            }

            SetBalance(Balance - amount);
            return true;
        }

        private void SetBalance(long newBalance)
        {
            if (newBalance == Balance)
            {
                return;
            }

            Balance = newBalance;
            BalanceChanged?.Invoke(newBalance);
        }

        private static void ThrowIfNegative(long amount, string paramName)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, amount, "금액은 0 이상이어야 한다.");
            }
        }
    }
}
