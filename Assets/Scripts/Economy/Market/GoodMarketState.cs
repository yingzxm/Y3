#nullable enable
using SocialEvolution.Domain;

namespace SocialEvolution.Economy
{
    /// <summary>
    /// 一种商品的市场状态。中间价是 long。
    /// <see cref="InventoryPressure"/> 为正表示集市库存低于目标（涨价压力），为负表示库存偏高（降价压力）。
    /// </summary>
    public sealed class GoodMarketState
    {
        internal GoodMarketState(Good good, long midPrice, long floorPrice, long ceilingPrice, int targetStock)
        {
            Good = good;
            MidPrice = midPrice;
            AnchorPrice = midPrice;
            FloorPrice = floorPrice;
            CeilingPrice = ceilingPrice;
            TargetStock = targetStock;
            LastTradePrice = midPrice;
        }

        public Good Good { get; }

        public long MidPrice { get; internal set; }

        /// <summary>开局锚定价。无成交时，库存压力价向这个锚回看。</summary>
        public long AnchorPrice { get; }

        public long FloorPrice { get; }

        public long CeilingPrice { get; }

        public int TargetStock { get; }

        public long InventoryPressure { get; internal set; }

        /// <summary>最近一个游戏时的成交量。本小时有成交时，显示本小时累计。</summary>
        public int RecentVolume { get; internal set; }

        public long LastTradePrice { get; internal set; }

        internal int HourVolume { get; set; }
    }
}
