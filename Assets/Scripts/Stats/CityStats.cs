#nullable enable

namespace SocialEvolution.Stats
{
    /// <summary>
    /// 演示用的城市读数。就业率是在职人数 / 人口。
    /// 平均时薪只统计在职市民。贫富指标是财富的基尼系数，0 均等，1 极分化。
    /// </summary>
    public readonly struct CityStats
    {
        public CityStats(
            int population,
            int employedCount,
            double employmentRate,
            double averageWage,
            long foodMidPrice,
            long woodMidPrice,
            long toolMidPrice,
            bool hasMidPrices,
            double richPoorIndex)
        {
            Population = population;
            EmployedCount = employedCount;
            EmploymentRate = employmentRate;
            AverageWage = averageWage;
            FoodMidPrice = foodMidPrice;
            WoodMidPrice = woodMidPrice;
            ToolMidPrice = toolMidPrice;
            HasMidPrices = hasMidPrices;
            RichPoorIndex = richPoorIndex;
        }

        public int Population { get; }

        public int EmployedCount { get; }

        /// <summary>在职人数 / 人口。没有市民时为 0。</summary>
        public double EmploymentRate { get; }

        /// <summary>在职市民 <c>WagePerHour</c> 的算术平均。没有在职时为 0。</summary>
        public double AverageWage { get; }

        public long FoodMidPrice { get; }

        public long WoodMidPrice { get; }

        public long ToolMidPrice { get; }

        public bool HasMidPrices { get; }

        /// <summary>财富基尼系数，夹在 [0, 1]。</summary>
        public double RichPoorIndex { get; }
    }
}
