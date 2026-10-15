#nullable enable
using System;
using SocialEvolution.Domain;

namespace SocialEvolution.Economy
{
    /// <summary>
    /// 把政府金库当成市场账户。扣款始终成功，余额可以为负，不结束游戏。
    /// </summary>
    public sealed class TreasuryLedger : ICashLedger
    {
        private readonly Treasury _treasury;

        public TreasuryLedger(Treasury treasury)
        {
            _treasury = treasury ?? throw new ArgumentNullException(nameof(treasury));
        }

        public long Balance => _treasury.Balance;

        public void Credit(long amount) => _treasury.Credit(amount);

        public bool TryDebit(long amount)
        {
            _treasury.Debit(amount);
            return true;
        }
    }
}
