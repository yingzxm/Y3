using SocialEvolution.Domain;
using SocialEvolution.World;
using Xunit;

namespace SocialEvolution.Tests
{
    public class InventoryTests
    {
        [Fact]
        public void DepositAndWithdraw_AreAtomic()
        {
            var inventory = new Inventory(10);

            InventoryResult food = inventory.TryDeposit(Good.Food, 6);
            InventoryResult wood = inventory.TryDeposit(Good.Wood, 4);
            InventoryResult overflow = inventory.TryDeposit(Good.Tool, 1);

            Assert.True(food.Succeeded);
            Assert.True(wood.Succeeded);
            Assert.False(overflow.Succeeded);
            Assert.Equal(InventoryRejection.ExceedsCapacity, overflow.Rejection);
            Assert.Equal(6, inventory.GetQuantity(Good.Food));
            Assert.Equal(4, inventory.GetQuantity(Good.Wood));
            Assert.Equal(0, inventory.GetQuantity(Good.Tool));
            Assert.Equal(10, inventory.TotalQuantity);

            InventoryResult tooMuch = inventory.TryWithdraw(Good.Food, 7);
            Assert.Equal(InventoryRejection.InsufficientStock, tooMuch.Rejection);
            Assert.Equal(6, inventory.GetQuantity(Good.Food));

            InventoryResult emptied = inventory.TryWithdraw(Good.Food, 6);
            Assert.True(emptied.Succeeded);
            Assert.Equal(0, inventory.GetQuantity(Good.Food));

            InventoryResult tool = inventory.TryDeposit(Good.Tool, 1);
            Assert.True(tool.Succeeded);
            Assert.Equal(1, inventory.GetQuantity(Good.Tool));
            Assert.Equal(4, inventory.GetQuantity(Good.Wood));
        }

        [Fact]
        public void RejectsNonPositiveQuantityWithoutChangingStock()
        {
            var inventory = new Inventory(5, (Good.Food, 2));

            Assert.Equal(InventoryRejection.InvalidQuantity, inventory.TryDeposit(Good.Food, 0).Rejection);
            Assert.Equal(InventoryRejection.InvalidQuantity, inventory.TryDeposit(Good.Wood, -3).Rejection);
            Assert.Equal(InventoryRejection.InvalidQuantity, inventory.TryWithdraw(Good.Food, 0).Rejection);
            Assert.Equal(2, inventory.GetQuantity(Good.Food));
            Assert.Equal(0, inventory.GetQuantity(Good.Wood));
        }

        [Fact]
        public void UnknownGood_DoesNotChangeStock()
        {
            var inventory = new Inventory(5, (Good.Wood, 1));
            var bad = (Good)99;

            Assert.Equal(InventoryRejection.UnknownGood, inventory.TryDeposit(bad, 1).Rejection);
            Assert.Equal(InventoryRejection.UnknownGood, inventory.TryWithdraw(bad, 1).Rejection);
            Assert.Throws<ArgumentOutOfRangeException>(() => inventory.GetQuantity(bad));
            Assert.Equal(1, inventory.GetQuantity(Good.Wood));
        }

        [Fact]
        public void ExactCapacity_SucceedsAndOneMoreFails()
        {
            var inventory = new Inventory(3);
            Assert.True(inventory.TryDeposit(Good.Tool, 3).Succeeded);
            Assert.Equal(InventoryRejection.ExceedsCapacity, inventory.TryDeposit(Good.Tool, 1).Rejection);
            Assert.Equal(3, inventory.GetQuantity(Good.Tool));
        }

        [Fact]
        public void World_UnknownBuilding_DoesNotTouchOtherStock()
        {
            var world = new WorldState(
                Array.Empty<Citizen>(),
                new[]
                {
                    new BuildingSite(
                        new Building(new BuildingId(1), BuildingKind.Farm, "农场"),
                        new Inventory(10, (Good.Food, 4))),
                },
                new Treasury(0));

            InventoryResult missing = world.TryWithdraw(new BuildingId(9), Good.Food, 1);
            Assert.Equal(InventoryRejection.UnknownBuilding, missing.Rejection);
            Assert.Equal(4, world.GetQuantity(new BuildingId(1), Good.Food));

            InventoryResult deposited = world.TryDeposit(new BuildingId(1), Good.Food, 2);
            Assert.True(deposited.Succeeded);
            Assert.Equal(6, world.GetQuantity(new BuildingId(1), Good.Food));
        }
    }
}
