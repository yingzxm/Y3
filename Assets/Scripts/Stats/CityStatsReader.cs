#nullable enable
using System;
using SocialEvolution.Domain;
using SocialEvolution.Labor;
using SocialEvolution.World;
using LaborJobPosting = SocialEvolution.Labor.JobPosting;

namespace SocialEvolution.Stats
{
    /// <summary>
    /// 从世界、劳动力市场和中间价读出演示统计。不改库存、金库或在职名单。
    /// </summary>
    public static class CityStatsReader
    {
        public static CityStats Read(WorldState world, ILaborMarket labor, IMidPriceSource prices)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (labor == null)
            {
                throw new ArgumentNullException(nameof(labor));
            }

            if (prices == null)
            {
                throw new ArgumentNullException(nameof(prices));
            }

            int population = world.Citizens.Count;
            int employed = 0;
            double wageSum = 0d;
            for (int i = 0; i < population; i++)
            {
                Citizen citizen = world.Citizens[i];
                JobEmployment? employment;
                if (!labor.TryGetEmployment(citizen.Id, out employment) || employment == null)
                {
                    continue;
                }

                employed++;
                LaborJobPosting? posting;
                if (labor.TryGetPosting(employment.PostingId, out posting) && posting != null)
                {
                    wageSum += posting.WagePerHour;
                }
            }

            double employmentRate = population == 0 ? 0d : (double)employed / population;
            double averageWage = employed == 0 ? 0d : wageSum / employed;
            long food = ReadPrice(prices, Good.Food);
            long wood = ReadPrice(prices, Good.Wood);
            long tool = ReadPrice(prices, Good.Tool);
            bool hasPrices = prices.TryGetMidPrice(Good.Food, out _)
                && prices.TryGetMidPrice(Good.Wood, out _)
                && prices.TryGetMidPrice(Good.Tool, out _);
            return new CityStats(
                population,
                employed,
                employmentRate,
                averageWage,
                food,
                wood,
                tool,
                hasPrices,
                RichPoor.Compute(world.Citizens));
        }

        private static long ReadPrice(IMidPriceSource prices, Good good)
        {
            long price;
            if (!prices.TryGetMidPrice(good, out price))
            {
                return 0L;
            }

            return price;
        }
    }
}
