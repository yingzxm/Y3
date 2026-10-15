#nullable enable
using System;

namespace SocialEvolution.Domain
{
    /// <summary>
    /// 政府金库。余额允许为负，表示负债。
    /// 负数不会结束游戏，本类型也不提供 Game Over。
    /// </summary>
    public sealed class Treasury
    {
        public Treasury(long balance)
        {
            Balance = balance;
        }

        public long Balance { get; private set; }

        public void Credit(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "入账金额不能为负。");
            }

            Balance = checked(Balance + amount);
        }

        public void Debit(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "出账金额不能为负。");
            }

            Balance = checked(Balance - amount);
        }
    }
}
