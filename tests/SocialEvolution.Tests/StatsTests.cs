using System;
using SocialEvolution.Agents;
using SocialEvolution.Bootstrap;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using SocialEvolution.Labor;
using SocialEvolution.Stats;
using SocialEvolution.World;
using Xunit;

namespace SocialEvolution.Tests
{
    public class StatsTests
    {
        private static readonly BuildingId HomeId = new BuildingId(5);
        private static readonly BuildingId FarmId = new BuildingId(2);
        private static readonly BuildingId FactoryId = new BuildingId(3);

        [Fact]
        public void CityStats_ReadsPopulationEmploymentWagePricesAndInequality()
        {
            Citizen poor = Person(1, 0L);
            Citizen farmer = Person(2, 0L);
            Citizen worker = Person(3, 0L);
            Citizen rich = Person(4, 100L);
            WorldState world = WorldOf(poor, farmer, worker, rich);
            var farm = new JobPostingId(1);
            var factory = new JobPostingId(2);
            world.SetJobPosting(FarmId, farm, "农民", 1, 15L, 0, SkillIds.Farming);
            world.SetJobPosting(FactoryId, factory, "工人", 1, 25L, 0, SkillIds.Manufacturing);
            var labor = new LaborMarket(world);
            labor.SetConditions(new JobConditions(farm, 8f, 1f, WorkShift.Day));
            labor.SetConditions(new JobConditions(factory, 8f, 1f, WorkShift.Day));
            Assert.Equal(LaborApplyResult.Hired, labor.TryApply(farmer, farm, GameDateTime.Origin));
            Assert.Equal(LaborApplyResult.Hired, labor.TryApply(worker, factory, GameDateTime.Origin));

            CityStats stats = CityStatsReader.Read(world, labor, GoodMidPrices.DemoOpening());
            Assert.Equal(4, stats.Population);
            Assert.Equal(2, stats.EmployedCount);
            Assert.Equal(0.5d, stats.EmploymentRate);
            Assert.Equal(20d, stats.AverageWage);
            Assert.Equal(GoodMidPrices.DemoFood, stats.FoodMidPrice);
            Assert.Equal(GoodMidPrices.DemoWood, stats.WoodMidPrice);
            Assert.Equal(GoodMidPrices.DemoTool, stats.ToolMidPrice);
            Assert.True(stats.HasMidPrices);
            Assert.Equal(0.75d, stats.RichPoorIndex, 10);
            Assert.Equal(stats.RichPoorIndex, RichPoor.Compute(world.Citizens), 10);
        }

        [Fact]
        public void RichPoor_IsZeroWhenWealthIsEven_AndShiftsNegatives()
        {
            WorldState even = WorldOf(Person(1, 40L), Person(2, 40L));
            Assert.Equal(0d, RichPoor.Compute(even.Citizens), 10);

            WorldState shifted = WorldOf(Person(1, -10L), Person(2, 10L));
            Assert.Equal(0.5d, RichPoor.Compute(shifted.Citizens), 10);

            WorldState broke = WorldOf(Person(1, 0L), Person(2, 0L));
            Assert.Equal(0d, RichPoor.Compute(broke.Citizens), 10);

            var empty = new WorldState(Array.Empty<Citizen>(), Array.Empty<BuildingSite>(), new Treasury(0));
            var labor = new LaborMarket(empty);
            CityStats stats = CityStatsReader.Read(empty, labor, new GoodMidPrices(1L, 2L, 3L));
            Assert.Equal(0, stats.Population);
            Assert.Equal(0d, stats.EmploymentRate);
            Assert.Equal(0d, stats.AverageWage);
            Assert.Equal(0d, stats.RichPoorIndex);
        }

        [Fact]
        public void MidPrices_RejectNegativesAndUnknownGoods()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GoodMidPrices(-1L, 0L, 0L));
            var prices = new GoodMidPrices(3L, 4L, 5L);
            long food;
            Assert.True(prices.TryGetMidPrice(Good.Food, out food));
            Assert.Equal(3L, food);
            long unknown;
            Assert.False(prices.TryGetMidPrice((Good)99, out unknown));
            Assert.Equal(0L, unknown);

            WorldState world = WorldOf(Person(1, 10L));
            CityStats stats = CityStatsReader.Read(world, new LaborMarket(world), new FoodOnlyPrices());
            Assert.Equal(7L, stats.FoodMidPrice);
            Assert.Equal(0L, stats.WoodMidPrice);
            Assert.Equal(0L, stats.ToolMidPrice);
            Assert.False(stats.HasMidPrices);
        }

        [Fact]
        public void Picker_ShowsCashJobTopNeedAndThreeLogLines()
        {
            Citizen hired = Person(1, 80L);
            Citizen other = Person(2, 20L);
            WorldState world = WorldOf(hired, other);
            var farm = new JobPostingId(1);
            world.SetJobPosting(FarmId, farm, "农民", 1, 15L, 0, SkillIds.Farming);
            var labor = new LaborMarket(world);
            labor.SetConditions(new JobConditions(farm, 8f, 1f, WorkShift.Day));
            Assert.Equal(LaborApplyResult.Hired, labor.TryApply(hired, farm, GameDateTime.Origin));

            var needs = new AgentNeeds
            {
                HungerSatisfaction = 0f,
                FatigueSatisfaction = 1f,
                WealthSatisfaction = 1f,
                SafetySatisfaction = 1f,
            };
            var script = new FixedDecision { Next = AgentAction.Eat };
            var brain = new AgentBrain(hired, labor, script, needs);
            var driver = new AgentsHourDriver(new[] { brain });
            var picker = new CitizenPicker(world, labor, driver);

            Assert.False(picker.Select(new CitizenId(99)));
            Assert.True(picker.Select(hired.Id));
            CitizenView view;
            Assert.True(picker.TryGetSelected(out view));
            Assert.Equal(80L, view.Cash);
            Assert.Equal("市民001", view.Name);
            Assert.True(view.Employed);
            Assert.Equal("农民", view.JobTitle);
            Assert.Equal(15L, view.WagePerHour);
            Assert.True(view.HasTopNeed);
            Assert.Equal(NeedKind.Hunger, view.TopNeed);
            Assert.Equal("饥饿", view.TopNeedLabel);
            Assert.Equal(1f, view.TopNeedUrgency);
            string first;
            string second;
            string third;
            Assert.False(view.TryGetThreeRecentLogs(out first, out second, out third));

            Assert.True(picker.TryRead(other.Id, out view));
            Assert.False(view.Employed);
            Assert.Equal(string.Empty, view.JobTitle);
            Assert.False(view.HasTopNeed);
            Assert.Equal(20L, view.Cash);

            var time = new TimeService();
            driver.Register(time);
            script.Next = AgentAction.Eat;
            time.StepHours(1);
            script.Next = AgentAction.Sleep;
            time.StepHours(1);
            script.Next = AgentAction.Shop;
            time.StepHours(1);

            Assert.True(picker.Select(hired.Id));
            Assert.True(picker.TryGetSelected(out view));
            Assert.True(view.TryGetThreeRecentLogs(out first, out second, out third));
            Assert.Equal("D1 01:00 进食", first);
            Assert.Equal("D1 02:00 睡眠", second);
            Assert.Equal("D1 03:00 购物", third);
            Assert.Equal(3, view.RecentLogs.Count);
            Assert.Equal(0, driver.Log.Count(other.Id));
        }

        [Fact]
        public void Picker_WrapsTheRosterAndIgnoresASatisfiedNeed()
        {
            Citizen first = Person(1, 10L);
            Citizen second = Person(2, 10L);
            WorldState world = WorldOf(first, second);
            var labor = new LaborMarket(world);
            var satisfied = new AgentNeeds
            {
                HungerSatisfaction = 1f,
                FatigueSatisfaction = 1f,
                WealthSatisfaction = 1f,
                SafetySatisfaction = 1f,
            };
            var brain = new AgentBrain(first, labor, needs: satisfied);
            var driver = new AgentsHourDriver(new[] { brain });
            var picker = new CitizenPicker(world, labor, driver);
            Assert.True(picker.SelectNext());
            CitizenView view;
            Assert.True(picker.TryGetSelected(out view));
            Assert.Equal(first.Id, view.Id);
            Assert.False(view.HasTopNeed);
            Assert.True(picker.SelectPrevious());
            Assert.True(picker.TryGetSelected(out view));
            Assert.Equal(second.Id, view.Id);
            Assert.True(picker.SelectNext());
            Assert.True(picker.TryGetSelected(out view));
            Assert.Equal(first.Id, view.Id);
            picker.Clear();
            Assert.False(picker.TryGetSelected(out view));
        }

        [Fact]
        public void HousingDetail_ReadsResidentsCapacityAndMapsOnlyKnownGoods()
        {
            Citizen first = Person(1, 10L);
            Citizen second = Person(2, 20L);
            var otherHome = new BuildingId(6);
            Citizen neighbor = new Citizen(
                new CitizenId(3),
                "邻里003",
                40,
                EducationLevel.Primary,
                Array.Empty<SkillLevel>(),
                1f,
                5L,
                otherHome);
            var housing = new Building(HomeId, BuildingKind.Housing, "公共住宅1", 30);
            var annex = new Building(otherHome, BuildingKind.Housing, "公共住宅2", 4);
            var farm = new Building(FarmId, BuildingKind.Farm, "城郊农场");
            WorldState world = new WorldState(
                new[] { first, second, neighbor },
                new[]
                {
                    new BuildingSite(housing, new Inventory(200)),
                    new BuildingSite(annex, new Inventory(20)),
                    new BuildingSite(farm, new Inventory(100, (Good.Food, 9), (Good.Wood, 8), (Good.Tool, 7))),
                },
                new Treasury(0));
            Assert.True(world.TryDeposit(HomeId, Good.Food, 4).Succeeded);
            Assert.True(world.TryDeposit(HomeId, Good.Wood, 7).Succeeded);
            Assert.True(world.TryDeposit(HomeId, Good.Tool, 2).Succeeded);

            var reader = new HousingDetailReader(world);
            HousingView view;
            Assert.True(reader.TryRead(HomeId, out view));
            Assert.Equal(HomeId, view.Id);
            Assert.Equal("公共住宅1", view.BuildingName);
            Assert.Equal(2, view.OccupantCount);
            Assert.Equal(30, view.Capacity);
            Assert.Equal(new[] { "市民001", "市民002" }, view.ResidentDisplayNames);
            Assert.Equal(view.ResidentDisplayNames, view.ResidentNames);
            Assert.Equal(4, view.Food);
            Assert.Equal(0, view.Firewood);
            Assert.Equal(7, view.Wood);
            Assert.Equal(0, view.Stone);
            Assert.Equal(0, view.Iron);
            Assert.Equal(2, view.Tool);
            Assert.Equal(0, view.Cloth);
            Assert.Equal(0, view.Water);
            Assert.Equal(4, world.GetQuantity(HomeId, Good.Food));

            Assert.True(reader.TryRead(otherHome, out view));
            Assert.Equal("公共住宅2", view.BuildingName);
            Assert.Equal(1, view.OccupantCount);
            Assert.Equal(4, view.Capacity);
            Assert.Equal(new[] { "邻里003" }, view.ResidentDisplayNames);
            Assert.Equal(0, view.Food);
            Assert.Equal(0, view.Wood);
            Assert.Equal(0, view.Tool);
            Assert.Equal(0, view.Firewood);
            Assert.Equal(0, view.Stone);
            Assert.Equal(0, view.Iron);
            Assert.Equal(0, view.Cloth);
            Assert.Equal(0, view.Water);

            HousingView missing;
            Assert.False(reader.TryRead(FarmId, out missing));
            Assert.Equal(string.Empty, missing.BuildingName);
            Assert.Empty(missing.ResidentDisplayNames);
            Assert.False(reader.TryRead(new BuildingId(99), out missing));
        }

        [Fact]
        public void HousingDetail_OnTheOpeningSeed_ListsEachHouseAndLeavesUnknownGoodsAtZero()
        {
            GameSession session = GameSession.CreateNew();
            DemoReadout readout = DemoReadout.Attach(session.Time, session.World);
            var firstHouse = new BuildingId(OpeningSeed.FirstHousingBuildingId);
            HousingView view;
            Assert.True(readout.Housing.TryRead(firstHouse, out view));
            Assert.Equal("公共住宅1", view.BuildingName);
            Assert.Equal(OpeningSeed.HousingResidentCapacity, view.OccupantCount);
            Assert.Equal(OpeningSeed.HousingResidentCapacity, view.Capacity);
            Assert.Equal(OpeningSeed.HousingResidentCapacity, view.ResidentNames.Count);
            Assert.Equal("市民001", view.ResidentDisplayNames[0]);
            Assert.Equal("市民030", view.ResidentDisplayNames[29]);
            Assert.Equal(0, view.Food);
            Assert.Equal(0, view.Firewood);
            Assert.Equal(0, view.Wood);
            Assert.Equal(0, view.Stone);
            Assert.Equal(0, view.Iron);
            Assert.Equal(0, view.Tool);
            Assert.Equal(0, view.Cloth);
            Assert.Equal(0, view.Water);

            var lastHouse = new BuildingId(OpeningSeed.FirstHousingBuildingId + OpeningSeed.HousingCount - 1);
            Assert.True(readout.Housing.TryRead(lastHouse, out view));
            Assert.Equal("公共住宅4", view.BuildingName);
            Assert.Equal("市民091", view.ResidentDisplayNames[0]);
            Assert.Equal("市民120", view.ResidentDisplayNames[29]);
            Assert.False(readout.Housing.TryRead(new BuildingId(OpeningSeed.FarmBuildingId), out view));
            Assert.Throws<ArgumentNullException>(() => new HousingDetailReader(null!));
        }

        [Fact]
        public void DemoReadout_OnTheOpeningSeed_FeedsThreeLogsAndDemoPrices()
        {
            GameSession session = GameSession.CreateNew();
            long food = GoodMidPrices.DemoFood;
            DemoReadout readout = DemoReadout.Attach(session.Time, session.World, new GoodMidPrices(food, 8L, 40L));
            readout.UseLivePrices(() => new GoodMidPrices(food, GoodMidPrices.DemoWood, GoodMidPrices.DemoTool));

            CityStats before = readout.Read();
            Assert.Equal(OpeningSeed.CitizenCount, before.Population);
            Assert.Equal(0, before.EmployedCount);
            Assert.Equal(0d, before.AverageWage);
            Assert.Equal(10L, before.FoodMidPrice);
            Assert.Equal(8L, before.WoodMidPrice);
            Assert.Equal(40L, before.ToolMidPrice);
            Assert.True(before.RichPoorIndex > 0d);
            Assert.True(before.RichPoorIndex < 1d);
            Assert.Equal(5, readout.Labor.GetOpenPostings().Count);

            CitizenView selected;
            Assert.True(readout.TryGetSelected(out selected));
            Assert.Equal(1, selected.Id.Value);
            Assert.Equal("市民001", selected.Name);
            Assert.Equal(OpeningSeed.WealthOf(0), selected.Cash);
            Assert.True(selected.HasTopNeed);
            Assert.Equal(NeedKind.Wealth, selected.TopNeed);

            session.Time.StepHours(3);
            food = 12L;
            CityStats after = readout.Read();
            Assert.Equal(12L, after.FoodMidPrice);
            Assert.Equal(OpeningSeed.CitizenCount, after.Population);
            Assert.InRange(after.EmploymentRate, 0d, 1d);

            Assert.True(readout.TryGetSelected(out selected));
            Assert.True(selected.RecentLogs.Count >= 3);
            string first;
            string second;
            string third;
            Assert.True(readout.TryGetThreeRecentLogs(out first, out second, out third));
            Assert.False(string.IsNullOrWhiteSpace(first));
            Assert.False(string.IsNullOrWhiteSpace(second));
            Assert.False(string.IsNullOrWhiteSpace(third));
            Assert.Equal(selected.RecentLogs[selected.RecentLogs.Count - 3].Text, first);
            Assert.Equal(selected.RecentLogs[selected.RecentLogs.Count - 1].Text, third);

            Assert.True(readout.Select(new CitizenId(OpeningSeed.CitizenCount)));
            Assert.True(readout.SelectNext());
            Assert.True(readout.TryGetSelected(out selected));
            Assert.Equal(1, selected.Id.Value);
            Assert.False(readout.Select(new CitizenId(999)));
            Assert.True(readout.TryGetSelected(out selected));
            Assert.Equal(1, selected.Id.Value);
        }

        private static Citizen Person(int id, long wealth)
        {
            return new Citizen(
                new CitizenId(id),
                "市民" + id.ToString("000"),
                30,
                EducationLevel.Secondary,
                Array.Empty<SkillLevel>(),
                1f,
                wealth,
                HomeId);
        }

        private static WorldState WorldOf(params Citizen[] citizens)
        {
            var housing = new Building(HomeId, BuildingKind.Housing, "公共住宅", 30);
            var farm = new Building(FarmId, BuildingKind.Farm, "城郊农场");
            var factory = new Building(FactoryId, BuildingKind.Factory, "工具工坊");
            var sites = new BuildingSite[]
            {
                new BuildingSite(housing, new Inventory(200)),
                new BuildingSite(farm, new Inventory(100)),
                new BuildingSite(factory, new Inventory(100)),
            };
            return new WorldState(citizens, sites, new Treasury(0));
        }

        private sealed class FoodOnlyPrices : IMidPriceSource
        {
            public bool TryGetMidPrice(Good good, out long midPrice)
            {
                if (good == Good.Food)
                {
                    midPrice = 7L;
                    return true;
                }

                midPrice = 0L;
                return false;
            }
        }

        private sealed class FixedDecision : IDecisionMaker
        {
            public AgentAction Next { get; set; }

            public AgentAction Decide(in AgentDecisionContext context)
            {
                return Next;
            }
        }
    }
}
