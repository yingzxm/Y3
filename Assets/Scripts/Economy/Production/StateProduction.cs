#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Domain;
using SocialEvolution.World;

namespace SocialEvolution.Economy
{
    /// <summary>
    /// 国企建筑的最小生产步进。产能按建筑类型固定，不读在职人数，也不点名派工。
    /// 日后接 Labor：用到岗人数相对编制的比例缩放这里的产量；没招到人就少产。
    /// 没有林场。农场在食物之外旁路产出木材，工厂消耗木材产出工具。
    /// 有输入时先出库，出库成功才入库；入库失败则把输入退回，本步不产。
    /// </summary>
    public static class StateProduction
    {
        public const int FarmFoodPerHour = 4;

        public const int FarmWoodPerHour = 1;

        public const int FactoryWoodInput = 1;

        public const int FactoryToolOutput = 1;

        public static void Run(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            IReadOnlyList<BuildingSite> sites = world.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                BuildingSite site = sites[i];
                if (site.Building.Kind == BuildingKind.Farm)
                {
                    TryConvert(site.Inventory, Good.Food, 0, Good.Food, FarmFoodPerHour);
                    TryConvert(site.Inventory, Good.Wood, 0, Good.Wood, FarmWoodPerHour);
                }
                else if (site.Building.Kind == BuildingKind.Factory)
                {
                    TryConvert(site.Inventory, Good.Wood, FactoryWoodInput, Good.Tool, FactoryToolOutput);
                }
            }
        }

        /// <summary>
        /// 在同一库存上做一次转换。inputQuantity 为 0 时不消耗输入。
        /// 返回 false 表示本步没有产出，库存与调用前一致。
        /// </summary>
        public static bool TryConvert(IInventory inventory, Good input, int inputQuantity, Good output, int outputQuantity)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            if (outputQuantity <= 0)
            {
                return false;
            }

            if (inputQuantity > 0)
            {
                InventoryResult withdrawn = inventory.TryWithdraw(input, inputQuantity);
                if (!withdrawn.Succeeded)
                {
                    return false;
                }
            }

            InventoryResult deposited = inventory.TryDeposit(output, outputQuantity);
            if (deposited.Succeeded)
            {
                return true;
            }

            if (inputQuantity > 0)
            {
                InventoryResult restored = inventory.TryDeposit(input, inputQuantity);
                if (!restored.Succeeded)
                {
                    throw new InvalidOperationException("生产回滚失败，库存可能已不一致。");
                }
            }

            return false;
        }
    }
}
