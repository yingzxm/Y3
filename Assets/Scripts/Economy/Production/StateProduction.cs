#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Domain;
using SocialEvolution.World;

namespace SocialEvolution.Economy
{
    /// <summary>
    /// 国企建筑的最小生产步进。不点名派工，本程序集也不引用劳动力程序集。
    /// 不传比例时按建筑类型满产。传入 0–1 的比例时向下取整；0 表示这一小时不产。
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
            Run(world, null);
        }

        /// <param name="outputScale">按建筑返回 0–1。空表示满产。小于 0 或非有限数按 0。</param>
        public static void Run(WorldState world, Func<BuildingId, float>? outputScale)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            IReadOnlyList<BuildingSite> sites = world.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                BuildingSite site = sites[i];
                float scale = ResolveScale(outputScale, site.Id);
                if (site.Building.Kind == BuildingKind.Farm)
                {
                    int food = ScaleQuantity(FarmFoodPerHour, scale);
                    int wood = ScaleQuantity(FarmWoodPerHour, scale);
                    if (food > 0)
                    {
                        TryConvert(site.Inventory, Good.Food, 0, Good.Food, food);
                    }

                    if (wood > 0)
                    {
                        TryConvert(site.Inventory, Good.Wood, 0, Good.Wood, wood);
                    }
                }
                else if (site.Building.Kind == BuildingKind.Factory)
                {
                    int tools = ScaleQuantity(FactoryToolOutput, scale);
                    if (tools > 0)
                    {
                        int wood = ScaleQuantity(FactoryWoodInput, scale);
                        TryConvert(site.Inventory, Good.Wood, wood, Good.Tool, tools);
                    }
                }
            }
        }

        private static float ResolveScale(Func<BuildingId, float>? outputScale, BuildingId buildingId)
        {
            if (outputScale == null)
            {
                return 1f;
            }

            float scale = outputScale(buildingId);
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
            {
                return 0f;
            }

            if (scale >= 1f)
            {
                return 1f;
            }

            return scale;
        }

        private static int ScaleQuantity(int amount, float scale)
        {
            if (amount <= 0 || scale <= 0f)
            {
                return 0;
            }

            if (scale >= 1f)
            {
                return amount;
            }

            int scaled = (int)Math.Floor(amount * (double)scale);
            return scaled < 0 ? 0 : scaled;
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
