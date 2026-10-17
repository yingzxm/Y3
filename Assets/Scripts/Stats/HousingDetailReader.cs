#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Domain;
using SocialEvolution.World;

namespace SocialEvolution.Stats
{
    /// <summary>
    /// 按建筑编号读一栋住宅：名称、入住人数、容量、住户名字，以及八项库存。
    /// 住户没有反向索引，扫描 <see cref="WorldState.Citizens"/> 里 <c>ResidenceId</c> 等于该建筑的人。
    /// 食物、木材、工具走现有库存；柴火、石料、铁、布、水在 <see cref="Good"/> 里还不存在，读作 0。
    /// 建筑不存在，或类型不是住宅时返回 false。不改世界，也不建第二套库存。
    /// </summary>
    public sealed class HousingDetailReader
    {
        private readonly WorldState _world;

        public HousingDetailReader(WorldState world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        public bool TryRead(BuildingId id, out HousingView view)
        {
            BuildingSite? site;
            if (!_world.TryGetSite(id, out site) || site == null || site.Building.Kind != BuildingKind.Housing)
            {
                view = default;
                return false;
            }

            IReadOnlyList<Citizen> citizens = _world.Citizens;
            var names = new List<string>();
            for (int i = 0; i < citizens.Count; i++)
            {
                Citizen citizen = citizens[i];
                if (citizen.ResidenceId == id)
                {
                    names.Add(citizen.Name);
                }
            }

            Building building = site.Building;
            view = new HousingView(
                building.Id,
                building.Name,
                building.HousingResidentCapacity,
                names,
                _world.GetQuantity(id, Good.Food),
                0,
                _world.GetQuantity(id, Good.Wood),
                0,
                0,
                _world.GetQuantity(id, Good.Tool),
                0,
                0);
            return true;
        }
    }
}
