#nullable enable
using SocialEvolution.Domain;

namespace SocialEvolution.Stats
{
    /// <summary>
    /// 三种商品的中间价。对齐 Economy 的 <c>ICityMarket.GetState(good).MidPrice</c>
    /// （<c>GoodMarketState.MidPrice</c>，<c>long</c>）。
    /// 本程序集不引用 Economy：市场还没进当前 main 时，由调用方把中间价填进来。
    /// </summary>
    public interface IMidPriceSource
    {
        bool TryGetMidPrice(Good good, out long midPrice);
    }
}
