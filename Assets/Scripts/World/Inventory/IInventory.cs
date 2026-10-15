#nullable enable
using SocialEvolution.Domain;

namespace SocialEvolution.World
{
    /// <summary>
    /// 建筑库存。容量是全部商品件数之和。
    /// 入出库要么整笔成功，要么整笔失败，不做部分成交。
    /// </summary>
    public interface IInventory
    {
        int Capacity { get; }

        int TotalQuantity { get; }

        int GetQuantity(Good good);

        InventoryResult TryDeposit(Good good, int quantity);

        InventoryResult TryWithdraw(Good good, int quantity);
    }
}
