#nullable enable
using System;
using SocialEvolution.Domain;

namespace SocialEvolution.Stats
{
    /// <summary>
    /// 食物、木材、工具的中间价。
    /// <see cref="DemoOpening"/> 使用市场开局锚定价：食物 10、木材 8、工具 40。
    /// 场景面板通过 <c>DemoReadout.UseLivePrices</c> 改为每个游戏时读 <c>GoodMarketState.MidPrice</c>。
    /// </summary>
    public sealed class GoodMidPrices : IMidPriceSource
    {
        public const long DemoFood = 10L;
        public const long DemoWood = 8L;
        public const long DemoTool = 40L;

        public GoodMidPrices(long food, long wood, long tool)
        {
            if (food < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(food), food, "中间价不能为负。");
            }

            if (wood < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(wood), wood, "中间价不能为负。");
            }

            if (tool < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(tool), tool, "中间价不能为负。");
            }

            Food = food;
            Wood = wood;
            Tool = tool;
        }

        public static GoodMidPrices DemoOpening()
        {
            return new GoodMidPrices(DemoFood, DemoWood, DemoTool);
        }

        public long Food { get; }

        public long Wood { get; }

        public long Tool { get; }

        public bool TryGetMidPrice(Good good, out long midPrice)
        {
            switch (good)
            {
                case Good.Food:
                    midPrice = Food;
                    return true;
                case Good.Wood:
                    midPrice = Wood;
                    return true;
                case Good.Tool:
                    midPrice = Tool;
                    return true;
                default:
                    midPrice = 0L;
                    return false;
            }
        }
    }
}
