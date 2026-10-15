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

        public static WorldState Create()
        {
            var citizens = new Citizen[CitizenCount];
            for (int i = 0; i < CitizenCount; i++)
            {
                int id = i + 1;
                citizens[i] = new Citizen(new CitizenId(id), $"市民{id:000}");
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

            farm.Building.SetJobPosting(new JobPostingId(FarmerPostingId), "农民", FarmerOpenings, 15, 0);
            farm.Building.SetJobPosting(new JobPostingId(FarmLeadPostingId), "农场主管", FarmLeadOpenings, 25, 2);
            factory.Building.SetJobPosting(new JobPostingId(FactoryWorkerPostingId), "工人", FactoryWorkerOpenings, 20, 1);
            market.Building.SetJobPosting(new JobPostingId(MerchantPostingId), "商人", MerchantOpenings, 18, 1);
            government.Building.SetJobPosting(new JobPostingId(ClerkPostingId), "公务员", ClerkOpenings, 22, 1);

            return new WorldState(citizens, sites, new Treasury(InitialTreasury));
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
