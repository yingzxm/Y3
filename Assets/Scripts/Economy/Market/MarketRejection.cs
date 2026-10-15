#nullable enable

namespace SocialEvolution.Economy
{
    public enum MarketRejection
    {
        None = 0,
        InvalidQuantity = 1,
        UnknownGood = 2,
        InsufficientCash = 3,
        InsufficientStock = 4,
        ExceedsCapacity = 5,
        NoGovernment = 6,
        NoMarket = 7,
    }
}
