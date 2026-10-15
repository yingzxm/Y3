#nullable enable

namespace SocialEvolution.World
{
    public enum InventoryRejection
    {
        None = 0,
        InvalidQuantity = 1,
        ExceedsCapacity = 2,
        InsufficientStock = 3,
        UnknownGood = 4,
        UnknownBuilding = 5,
    }
}
