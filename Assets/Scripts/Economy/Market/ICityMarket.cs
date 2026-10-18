#nullable enable
using System.Collections.Generic;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using SocialEvolution.World;

namespace SocialEvolution.Economy
{
    /// <summary>
    /// 城市市场。集市建筑的库存是可交易供给。没有订单簿撮合。
    /// 把 <see cref="OnGameHour"/> 注册到 <see cref="ITimeService.RegisterGameHour"/>。
    /// </summary>
    public interface ICityMarket : IGovernmentMarketOps
    {
        IReadOnlyList<GoodMarketState> Goods { get; }

        long ClearingCash { get; }

        GoodMarketState GetState(Good good);

        MarketResult TryBuy(ICashLedger payer, IInventory destination, Good good, int quantity);

        MarketResult TrySell(ICashLedger payee, IInventory source, Good good, int quantity);

        void RunProduction();

        void SettlePrices();

        void OnGameHour(GameDateTime now);
    }
}
