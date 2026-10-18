#nullable enable

namespace SocialEvolution.Economy
{
    /// <summary>
    /// 市场账户的现金。市民和国企用不允许透支的账本；金库用 <see cref="TreasuryLedger"/>，可以为负。
    /// </summary>
    public interface ICashLedger
    {
        long Balance { get; }

        void Credit(long amount);

        /// <summary>扣款。拒绝时返回 false，余额不变。允许透支的实现始终返回 true。</summary>
        bool TryDebit(long amount);
    }
}
