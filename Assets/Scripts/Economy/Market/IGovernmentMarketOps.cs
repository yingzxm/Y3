#nullable enable
using SocialEvolution.Domain;

namespace SocialEvolution.Economy
{
    /// <summary>
    /// 政府买卖的稳定入口，留给 P0-7 调用。
    /// 这里已经能成交：买入从集市入库到政府厅并扣金库，卖出相反。
    /// 金库可以为负，不结束游戏。完整的建设、设岗和财政面板不在这里。
    /// </summary>
    public interface IGovernmentMarketOps
    {
        MarketResult TryGovernmentBuy(Good good, int quantity);

        MarketResult TryGovernmentSell(Good good, int quantity);
    }
}
