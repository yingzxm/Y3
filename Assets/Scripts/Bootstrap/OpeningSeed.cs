#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Domain;
using SocialEvolution.World;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 开局种子。约 120 名市民、金库 500_000、五类建筑和最小库存。
    /// 岗位只设编制，全部空缺。没有私企，也没有派工。
    /// </summary>
    public static class OpeningSeed
    {
        public const int CitizenCount = 120;
        public const long InitialTreasury = 500_000L;

        /// <summary>食物件数按「每人每日 1 份、备 20 日」记账。P0 不消耗食物。</summary>
        public const int FoodReserveDays = 20;

        public const int GovernmentFood = CitizenCount * FoodReserveDays;
        public const int GovernmentWood = 100;
        public const int GovernmentTools = 20;
        public const int FarmFood = 100;
        public const int FarmTools = 10;
        public const int FactoryWood = 50;

        public const int GovernmentCapacity = 20_000;
        public const int FarmCapacity = 5_000;
        public const int FactoryCapacity = 2_000;
        public const int MarketCapacity = 8_000;
        public const int HousingInventoryCapacity = 200;

        public const int HousingCount = 4;
        public const int HousingResidentCapacity = 30;

        public const int GovernmentBuildingId = 1;
        public const int FarmBuildingId = 2;
        public const int FactoryBuildingId = 3;
        public const int MarketBuildingId = 4;
        public const int FirstHousingBuildingId = 5;

        public const int FarmerPostingId = 1;
        public const int FarmLeadPostingId = 2;
        public const int FactoryWorkerPostingId = 3;
        public const int MerchantPostingId = 4;
        public const int ClerkPostingId = 5;

        public const int FarmerOpenings = 20;
        public const int FarmLeadOpenings = 2;
        public const int FactoryWorkerOpenings = 12;
        public const int MerchantOpenings = 4;
        public const int ClerkOpenings = 5;

        public const long FarmerWage = 15L;
        public const long FarmLeadWage = 25L;
        public const long FactoryWorkerWage = 20L;
        public const long MerchantWage = 18L;
        public const long ClerkWage = 22L;

        public static WorldState Create()
        {
            var citizens = new Citizen[CitizenCount];
            for (int i = 0; i < CitizenCount; i++)
            {
                int id = i + 1;
                citizens[i] = new Citizen(
                    new CitizenId(id),
                    "市民" + id.ToString("000"),
                    AgeOf(i),
                    EducationOf(i),
                    SkillsOf(i),
                    HealthFactorOf(i),
                    WealthOf(i),
                    ResidenceOf(i));
            }

            BuildingSite government = CreateSite(
                GovernmentBuildingId,
                BuildingKind.Government,
                "政府厅",
                0,
                GovernmentCapacity,
                (Good.Food, GovernmentFood),
                (Good.Wood, GovernmentWood),
                (Good.Tool, GovernmentTools));
            BuildingSite farm = CreateSite(
                FarmBuildingId,
                BuildingKind.Farm,
                "城郊农场",
                0,
                FarmCapacity,
                (Good.Food, FarmFood),
                (Good.Tool, FarmTools));
            BuildingSite factory = CreateSite(
                FactoryBuildingId,
                BuildingKind.Factory,
                "工具工坊",
                0,
                FactoryCapacity,
                (Good.Wood, FactoryWood));
            BuildingSite market = CreateSite(
                MarketBuildingId,
                BuildingKind.Market,
                "集市",
                0,
                MarketCapacity);

            var sites = new List<BuildingSite>(4 + HousingCount)
            {
                government,
                farm,
                factory,
                market,
            };

            for (int i = 0; i < HousingCount; i++)
            {
                int id = FirstHousingBuildingId + i;
                sites.Add(CreateSite(
                    id,
                    BuildingKind.Housing,
                    $"公共住宅{i + 1}",
                    HousingResidentCapacity,
                    HousingInventoryCapacity));
            }

            WorldState world = new WorldState(citizens, sites, new Treasury(InitialTreasury));
            world.SetJobPosting(farm.Id, new JobPostingId(FarmerPostingId), "农民", FarmerOpenings, FarmerWage, 0, SkillIds.Farming);
            world.SetJobPosting(farm.Id, new JobPostingId(FarmLeadPostingId), "农场主管", FarmLeadOpenings, FarmLeadWage, 2, SkillIds.Farming);
            world.SetJobPosting(factory.Id, new JobPostingId(FactoryWorkerPostingId), "工人", FactoryWorkerOpenings, FactoryWorkerWage, 1, SkillIds.Manufacturing);
            world.SetJobPosting(market.Id, new JobPostingId(MerchantPostingId), "商人", MerchantOpenings, MerchantWage, 1, SkillIds.Trade);
            world.SetJobPosting(government.Id, new JobPostingId(ClerkPostingId), "公务员", ClerkOpenings, ClerkWage, 1, SkillIds.Administration);
            return world;
        }

        /// <summary>下标 0–119 的年龄。20 名 6–15 岁，25 名 16–25 岁，40 名 26–45 岁，25 名 46–60 岁，10 名 61–70 岁。</summary>
        public static int AgeOf(int index)
        {
            EnsureIndex(index);
            if (index < 20)
            {
                return 6 + (index % 10);
            }

            if (index < 45)
            {
                return 16 + ((index - 20) % 10);
            }

            if (index < 85)
            {
                return 26 + ((index - 45) % 20);
            }

            if (index < 110)
            {
                return 46 + ((index - 85) % 15);
            }

            return 61 + ((index - 110) % 10);
        }

        public static EducationLevel EducationOf(int index)
        {
            int age = AgeOf(index);
            if (age < 7)
            {
                return EducationLevel.None;
            }

            if (age < 13)
            {
                return EducationLevel.Primary;
            }

            if (age < 16)
            {
                return EducationLevel.Secondary;
            }

            switch (index % 10)
            {
                case 0:
                    return EducationLevel.Higher;
                case 1:
                case 2:
                    return EducationLevel.Vocational;
                case 3:
                case 4:
                case 5:
                    return EducationLevel.Secondary;
                case 6:
                case 7:
                case 8:
                    return EducationLevel.Primary;
                default:
                    return EducationLevel.None;
            }
        }

        public static float HealthFactorOf(int index)
        {
            EnsureIndex(index);
            return 0.85f + ((index % 5) * 0.05f);
        }

        public static long WealthOf(int index)
        {
            EnsureIndex(index);
            return 80L + ((index * 47) % 2400);
        }

        public static BuildingId ResidenceOf(int index)
        {
            EnsureIndex(index);
            return new BuildingId(FirstHousingBuildingId + (index / HousingResidentCapacity));
        }

        public static SkillLevel[] SkillsOf(int index)
        {
            if (AgeOf(index) < 16)
            {
                return Array.Empty<SkillLevel>();
            }

            string primary;
            switch (index % 4)
            {
                case 0:
                    primary = SkillIds.Farming;
                    break;
                case 1:
                    primary = SkillIds.Manufacturing;
                    break;
                case 2:
                    primary = SkillIds.Trade;
                    break;
                default:
                    primary = SkillIds.Administration;
                    break;
            }

            if (index % 7 != 0)
            {
                return new SkillLevel[] { new SkillLevel(primary, index % 5) };
            }

            string second = primary == SkillIds.Farming ? SkillIds.Trade : SkillIds.Farming;
            return new SkillLevel[]
            {
                new SkillLevel(primary, index % 5),
                new SkillLevel(second, 1),
            };
        }

        private static void EnsureIndex(int index)
        {
            if (index < 0 || index >= CitizenCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "市民下标必须在 0–119。");
            }
        }

        private static BuildingSite CreateSite(
            int buildingId,
            BuildingKind kind,
            string name,
            int housingResidentCapacity,
            int inventoryCapacity,
            params (Good Good, int Quantity)[] stock)
        {
            if (stock == null)
            {
                throw new ArgumentNullException(nameof(stock));
            }

            var building = new Building(new BuildingId(buildingId), kind, name, housingResidentCapacity);
            var inventory = new Inventory(inventoryCapacity, stock);
            return new BuildingSite(building, inventory);
        }
    }
}
