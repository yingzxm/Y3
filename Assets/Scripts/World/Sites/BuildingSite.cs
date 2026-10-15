#nullable enable
using System;
using SocialEvolution.Domain;

namespace SocialEvolution.World
{
    /// <summary>地图上的一座建筑及其库存。</summary>
    public sealed class BuildingSite
    {
        public BuildingSite(Building building, Inventory inventory)
        {
            Building = building ?? throw new ArgumentNullException(nameof(building));
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public Building Building { get; }

        public IInventory Inventory { get; }

        public BuildingId Id => Building.Id;
    }
}
