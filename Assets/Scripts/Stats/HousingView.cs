#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Domain;

namespace SocialEvolution.Stats
{
    /// <summary>
    /// 一栋住宅的只读档案。住户名字来自 <see cref="SocialEvolution.Domain.Citizen.Name"/>。
    /// 领域里没有单独的 DisplayName，所以展示名就是这个名字。
    /// 食物、木材、工具来自该建筑的库存。柴火、石料、铁、布、水在领域商品里还不存在，数量是 0。
    /// </summary>
    public readonly struct HousingView
    {
        private readonly string _buildingName;
        private readonly string[] _residentDisplayNames;

        public HousingView(
            BuildingId id,
            string buildingName,
            int capacity,
            IReadOnlyList<string> residentDisplayNames,
            int food,
            int firewood,
            int wood,
            int stone,
            int iron,
            int tool,
            int cloth,
            int water)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "居住容量不能为负。");
            }

            if (food < 0 || firewood < 0 || wood < 0 || stone < 0 || iron < 0 || tool < 0 || cloth < 0 || water < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(food), "库存件数不能为负。");
            }

            Id = id;
            _buildingName = buildingName ?? string.Empty;
            Capacity = capacity;
            _residentDisplayNames = CopyNames(residentDisplayNames);
            OccupantCount = _residentDisplayNames.Length;
            Food = food;
            Firewood = firewood;
            Wood = wood;
            Stone = stone;
            Iron = iron;
            Tool = tool;
            Cloth = cloth;
            Water = water;
        }

        public BuildingId Id { get; }

        public string BuildingName => _buildingName ?? string.Empty;

        /// <summary>当前住户人数，等于 <see cref="ResidentDisplayNames"/> 的长度。</summary>
        public int OccupantCount { get; }

        /// <summary><see cref="SocialEvolution.Domain.Building.HousingResidentCapacity"/>。</summary>
        public int Capacity { get; }

        /// <summary>住户展示名，按在册顺序。展示名即 <c>Citizen.Name</c>。</summary>
        public IReadOnlyList<string> ResidentDisplayNames => _residentDisplayNames ?? Array.Empty<string>();

        /// <summary>与 <see cref="ResidentDisplayNames"/> 同一份名单。</summary>
        public IReadOnlyList<string> ResidentNames => ResidentDisplayNames;

        public int Food { get; }

        public int Firewood { get; }

        public int Wood { get; }

        public int Stone { get; }

        public int Iron { get; }

        public int Tool { get; }

        public int Cloth { get; }

        public int Water { get; }

        private static string[] CopyNames(IReadOnlyList<string>? names)
        {
            if (names == null || names.Count == 0)
            {
                return Array.Empty<string>();
            }

            var copy = new string[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                copy[i] = names[i] ?? string.Empty;
            }

            return copy;
        }
    }
}
