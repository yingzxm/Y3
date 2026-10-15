using SocialEvolution.Bootstrap;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using SocialEvolution.Economy;
using SocialEvolution.World;
using Xunit;

namespace SocialEvolution.Tests
{
    public class MarketTests
    {
        [Fact]
        public void Production_DepositsOutputsAndConsumesFactoryWood()
        {
            WorldState world = OpeningSeed.Create();
            var market = new CityMarket(world);
            BuildingId farm = new BuildingId(OpeningSeed.FarmBuildingId);
            BuildingId factory = new BuildingId(OpeningSeed.FactoryBuildingId);

            market.RunProduction();

            Assert.Equal(OpeningSeed.FarmFood + StateProduction.FarmFoodPerHour, world.GetQuantity(farm, Good.Food));
            Assert.Equal(StateProduction.FarmWoodPerHour, world.GetQuantity(farm, Good.Wood));
            Assert.Equal(OpeningSeed.FarmTools, world.GetQuantity(farm, Good.Tool));
            Assert.Equal(OpeningSeed.FactoryWood - StateProduction.FactoryWoodInput, world.GetQuantity(factory, Good.Wood));
            Assert.Equal(StateProduction.FactoryToolOutput, world.GetQuantity(factory, Good.Tool));
            Assert.Equal(OpeningSeed.InitialTreasury, world.Treasury.Balance);
        }

        [Fact]
        public void Production_SkipsFactoryWhenWoodIsMissing()
        {
            WorldState world = OpeningSeed.Create();
            BuildingId factory = new BuildingId(OpeningSeed.FactoryBuildingId);
            Assert.True(world.TryWithdraw(factory, Good.Wood, OpeningSeed.FactoryWood).Succeeded);

            new CityMarket(world).RunProduction();

            Assert.Equal(0, world.GetQuantity(factory, Good.Wood));
            Assert.Equal(0, world.GetQuantity(factory, Good.Tool));
        }

        [Fact]
        public void Production_RestoresInputWhenOutputDoesNotFit()
        {
            var inventory = new Inventory(2, (Good.Wood, 2));

            bool produced = StateProduction.TryConvert(inventory, Good.Wood, 1, Good.Tool, 2);

            Assert.False(produced);
            Assert.Equal(2, inventory.GetQuantity(Good.Wood));
            Assert.Equal(0, inventory.GetQuantity(Good.Tool));
        }

        [Fact]
        public void Sell_MovesStockAndCashAndLowersMidPrice()
        {
            WorldState world = OpeningSeed.Create();
            var market = new CityMarket(world);
            var seller = new CashLedger(0);
            var stock = new Inventory(100, (Good.Food, 50));
            BuildingId pit = new BuildingId(OpeningSeed.MarketBuildingId);

            MarketResult result = market.TrySell(seller, stock, Good.Food, 10);

            Assert.True(result.Succeeded);
            Assert.Equal(5L, result.UnitPrice);
            Assert.Equal(50L, result.CashMoved);
            Assert.Equal(5L, market.GetState(Good.Food).MidPrice);
            Assert.Equal(5L, market.GetState(Good.Food).LastTradePrice);
            Assert.Equal(10, result.Quantity);
            Assert.Equal(10, world.GetQuantity(pit, Good.Food));
            Assert.Equal(40, stock.GetQuantity(Good.Food));
            Assert.Equal(50L, seller.Balance);
            Assert.Equal(CityMarket.WoodMidPrice, market.GetState(Good.Wood).MidPrice);
        }

        [Fact]
        public void Buy_MovesStockAndCashAndRaisesMidPrice()
        {
            WorldState world = OpeningSeed.Create();
            var market = new CityMarket(world);
            BuildingId pit = new BuildingId(OpeningSeed.MarketBuildingId);
            Assert.True(world.TryDeposit(pit, Good.Food, 30).Succeeded);
            var buyer = new CashLedger(100_000);
            var bag = new Inventory(100);

            MarketResult result = market.TryBuy(buyer, bag, Good.Food, 10);

            Assert.True(result.Succeeded);
            Assert.Equal(15L, result.UnitPrice);
            Assert.Equal(150L, result.CashMoved);
            Assert.Equal(15L, market.GetState(Good.Food).MidPrice);
            Assert.Equal(20, world.GetQuantity(pit, Good.Food));
            Assert.Equal(10, bag.GetQuantity(Good.Food));
            Assert.Equal(99_850L, buyer.Balance);
        }

        [Fact]
        public void Buy_RejectsShortCashWithoutChangingState()
        {
            WorldState world = OpeningSeed.Create();
            var market = new CityMarket(world);
            BuildingId pit = new BuildingId(OpeningSeed.MarketBuildingId);
            Assert.True(world.TryDeposit(pit, Good.Food, 5).Succeeded);
            var buyer = new CashLedger(1);
            var bag = new Inventory(10);

            MarketResult result = market.TryBuy(buyer, bag, Good.Food, 1);

            Assert.False(result.Succeeded);
            Assert.Equal(MarketRejection.InsufficientCash, result.Rejection);
            Assert.Equal(1L, buyer.Balance);
            Assert.Equal(0, bag.GetQuantity(Good.Food));
            Assert.Equal(5, world.GetQuantity(pit, Good.Food));
            Assert.Equal(CityMarket.FoodMidPrice, market.GetState(Good.Food).MidPrice);
        }

        [Fact]
        public void Settle_HighStockLowersPriceAndEmptyStockRaisesIt()
        {
            WorldState supplied = OpeningSeed.Create();
            var suppliedMarket = new CityMarket(supplied);
            BuildingId pit = new BuildingId(OpeningSeed.MarketBuildingId);
            Assert.True(supplied.TryDeposit(pit, Good.Food, 1000).Succeeded);

            suppliedMarket.SettlePrices();

            Assert.Equal(CityMarket.FoodMidPrice - CityMarket.DriftStep, suppliedMarket.GetState(Good.Food).MidPrice);
            Assert.True(suppliedMarket.GetState(Good.Food).InventoryPressure < 0);

            var scarce = new CityMarket(OpeningSeed.Create());
            scarce.SettlePrices();

            Assert.Equal(CityMarket.FoodMidPrice + CityMarket.DriftStep, scarce.GetState(Good.Food).MidPrice);
            Assert.True(scarce.GetState(Good.Food).InventoryPressure > 0);
            Assert.Equal(0, scarce.GetState(Good.Food).RecentVolume);
        }

        [Fact]
        public void RepeatedSells_StopAtTheFloor()
        {
            var market = new CityMarket(OpeningSeed.Create());
            var seller = new CashLedger(0);
            var stock = new Inventory(500, (Good.Food, 200));

            for (int i = 0; i < 6; i++)
            {
                Assert.True(market.TrySell(seller, stock, Good.Food, 10).Succeeded);
            }

            long floor = market.GetState(Good.Food).FloorPrice;
            Assert.Equal(1L, floor);
            Assert.Equal(floor, market.GetState(Good.Food).MidPrice);

            Assert.True(market.TrySell(seller, stock, Good.Food, 10).Succeeded);
            Assert.Equal(floor, market.GetState(Good.Food).MidPrice);
        }

        [Fact]
        public void GovernmentOps_AreCallableAndFailedCallsDoNotMutate()
        {
            WorldState world = OpeningSeed.Create();
            var market = new CityMarket(world);
            IGovernmentMarketOps ops = market;
            BuildingId government = new BuildingId(OpeningSeed.GovernmentBuildingId);
            BuildingId pit = new BuildingId(OpeningSeed.MarketBuildingId);
            long treasury = world.Treasury.Balance;
            long foodMid = market.GetState(Good.Food).MidPrice;
            long woodMid = market.GetState(Good.Wood).MidPrice;
            long toolMid = market.GetState(Good.Tool).MidPrice;
            int governmentFood = world.GetQuantity(government, Good.Food);
            int pitFood = world.GetQuantity(pit, Good.Food);

            Assert.False(ops.TryGovernmentBuy(Good.Food, 0).Succeeded);
            Assert.Equal(MarketRejection.UnknownGood, ops.TryGovernmentBuy((Good)0, 5).Rejection);
            Assert.Equal(MarketRejection.InsufficientStock, ops.TryGovernmentBuy(Good.Food, 5).Rejection);

            Assert.Equal(treasury, world.Treasury.Balance);
            Assert.Equal(foodMid, market.GetState(Good.Food).MidPrice);
            Assert.Equal(woodMid, market.GetState(Good.Wood).MidPrice);
            Assert.Equal(toolMid, market.GetState(Good.Tool).MidPrice);
            Assert.Equal(governmentFood, world.GetQuantity(government, Good.Food));
            Assert.Equal(pitFood, world.GetQuantity(pit, Good.Food));
        }

        [Fact]
        public void GovernmentSellThenBuy_UpdatesStockCashAndCanLeaveTreasuryNegative()
        {
            WorldState world = OpeningSeed.Create();
            var market = new CityMarket(world);
            IGovernmentMarketOps ops = market;
            BuildingId government = new BuildingId(OpeningSeed.GovernmentBuildingId);
            BuildingId pit = new BuildingId(OpeningSeed.MarketBuildingId);

            MarketResult sell = ops.TryGovernmentSell(Good.Food, 10);

            Assert.True(sell.Succeeded);
            Assert.Equal(5L, market.GetState(Good.Food).MidPrice);
            Assert.Equal(OpeningSeed.GovernmentFood - 10, world.GetQuantity(government, Good.Food));
            Assert.Equal(10, world.GetQuantity(pit, Good.Food));
            Assert.True(world.Treasury.Balance > OpeningSeed.InitialTreasury);

            world.Treasury.Debit(world.Treasury.Balance);
            Assert.Equal(0L, world.Treasury.Balance);

            MarketResult buy = ops.TryGovernmentBuy(Good.Food, 10);

            Assert.True(buy.Succeeded);
            Assert.True(world.Treasury.Balance < 0);
            Assert.Equal(0, world.GetQuantity(pit, Good.Food));
            Assert.Equal(OpeningSeed.GovernmentFood, world.GetQuantity(government, Good.Food));
            Assert.True(market.GetState(Good.Food).MidPrice > 5L);
        }

        [Fact]
        public void HourHook_ProducesIntoTheMarketWithoutTouchingTreasury()
        {
            GameSession session = GameSession.CreateNew();
            BuildingId pit = new BuildingId(OpeningSeed.MarketBuildingId);
            BuildingId government = new BuildingId(OpeningSeed.GovernmentBuildingId);

            Assert.NotNull(session.Market);
            Assert.Equal(1, session.Time.StepHours(1));

            Assert.Equal(StateProduction.FarmFoodPerHour, session.World.GetQuantity(pit, Good.Food));
            Assert.Equal(StateProduction.FarmWoodPerHour, session.World.GetQuantity(pit, Good.Wood));
            Assert.Equal(StateProduction.FactoryToolOutput, session.World.GetQuantity(pit, Good.Tool));
            Assert.Equal(OpeningSeed.GovernmentFood, session.World.GetQuantity(government, Good.Food));
            Assert.Equal(OpeningSeed.InitialTreasury, session.World.Treasury.Balance);
            Assert.Equal(new GameDateTime(1, 1), session.Market!.LastSettled);
            Assert.True(session.Market.GetState(Good.Food).MidPrice < CityMarket.FoodMidPrice);
        }
    }
}
