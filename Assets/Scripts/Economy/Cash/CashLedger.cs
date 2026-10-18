#nullable enable
using System;

namespace SocialEvolution.Economy
{
    /// <summary>
    /// 独立现金。默认不允许透支。清算账户把 <see cref="AllowNegative"/> 设为 true，那不是金库，也不结束游戏。
    /// </summary>
    public sealed class CashLedger : ICashLedger
    {
        public CashLedger(long balance, bool allowNegative = false)
        {
            Balance = balance;
            AllowNegative = allowNegative;
        }

        public long Balance { get; private set; }

        public bool AllowNegative { get; }

        public void Credit(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "入账金额不能为负。");
            }

            Balance = checked(Balance + amount);
        }

        public bool TryDebit(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "出账金额不能为负。");
            }

            if (!AllowNegative && Balance < amount)
            {
                return false;
            }

            Balance = checked(Balance - amount);
            return true;
        }
    }
}
