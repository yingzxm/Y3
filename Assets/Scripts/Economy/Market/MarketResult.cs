#nullable enable
using System;
using SocialEvolution.Domain;

namespace SocialEvolution.Economy
{
    /// <summary>一笔买卖的结果。失败时数量和金额都是 0，调用前的库存、现金和中间价保持不变。</summary>
    public readonly struct MarketResult
    {
        private MarketResult(bool succeeded, MarketRejection rejection, Good good, int quantity, long unitPrice, long cashMoved)
        {
            Succeeded = succeeded;
            Rejection = rejection;
            Good = good;
            Quantity = quantity;
            UnitPrice = unitPrice;
            CashMoved = cashMoved;
        }

        public bool Succeeded { get; }

        public MarketRejection Rejection { get; }

        public Good Good { get; }

        public int Quantity { get; }

        /// <summary>成交单价。失败时为 0。</summary>
        public long UnitPrice { get; }

        /// <summary>本笔现金总额。买入是买方付出的钱，卖出是卖方收到的钱。</summary>
        public long CashMoved { get; }

        public static MarketResult Ok(Good good, int quantity, long unitPrice, long cashMoved)
        {
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "成功成交的数量必须为正。");
            }

            if (unitPrice <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "成交单价必须为正。");
            }

            return new MarketResult(true, MarketRejection.None, good, quantity, unitPrice, cashMoved);
        }

        public static MarketResult Fail(MarketRejection rejection, Good good)
        {
            if (rejection == MarketRejection.None)
            {
                throw new ArgumentException("失败结果需要具体原因。", nameof(rejection));
            }

            return new MarketResult(false, rejection, good, 0, 0L, 0L);
        }
    }
}
