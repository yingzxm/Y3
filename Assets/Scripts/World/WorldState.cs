#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Domain;

namespace SocialEvolution.World
{
    /// <summary>
    /// 开局世界：市民、建筑工地、金库。
    /// 改岗位走 <see cref="SetJobPosting"/>，只改编制，不点名市民。
    /// </summary>
    public sealed class WorldState
    {
        private readonly Citizen[] _citizens;
        private readonly BuildingSite[] _sites;
        private readonly Dictionary<int, int> _citizenIndex;
        private readonly Dictionary<int, int> _siteIndex;

        public WorldState(IReadOnlyList<Citizen> citizens, IReadOnlyList<BuildingSite> sites, Treasury treasury)
        {
            if (citizens == null)
            {
                throw new ArgumentNullException(nameof(citizens));
            }

            if (sites == null)
            {
                throw new ArgumentNullException(nameof(sites));
            }

            Treasury = treasury ?? throw new ArgumentNullException(nameof(treasury));

            _citizens = new Citizen[citizens.Count];
            _citizenIndex = new Dictionary<int, int>(citizens.Count);
            for (int i = 0; i < citizens.Count; i++)
            {
                Citizen citizen = citizens[i] ?? throw new ArgumentException("市民列表含空项。", nameof(citizens));
                if (citizen.Id.Value <= 0)
                {
                    throw new ArgumentException("市民编号必须为正整数。", nameof(citizens));
                }

                if (_citizenIndex.ContainsKey(citizen.Id.Value))
                {
                    throw new ArgumentException($"市民编号重复：{citizen.Id.Value}。", nameof(citizens));
                }

                _citizenIndex.Add(citizen.Id.Value, i);
                _citizens[i] = citizen;
            }

            _sites = new BuildingSite[sites.Count];
            _siteIndex = new Dictionary<int, int>(sites.Count);
            var jobIds = new HashSet<int>();
            for (int i = 0; i < sites.Count; i++)
            {
                BuildingSite site = sites[i] ?? throw new ArgumentException("建筑列表含空项。", nameof(sites));
                int buildingId = site.Building.Id.Value;
                if (buildingId <= 0)
                {
                    throw new ArgumentException("建筑编号必须为正整数。", nameof(sites));
                }

                if (_siteIndex.ContainsKey(buildingId))
                {
                    throw new ArgumentException($"建筑编号重复：{buildingId}。", nameof(sites));
                }

                IReadOnlyList<JobPosting> postings = site.Building.JobPostings;
                for (int j = 0; j < postings.Count; j++)
                {
                    JobPosting posting = postings[j];
                    if (posting.BuildingId != site.Building.Id)
                    {
                        throw new ArgumentException($"岗位 {posting.Id.Value} 不属于建筑 {buildingId}。", nameof(sites));
                    }

                    if (!jobIds.Add(posting.Id.Value))
                    {
                        throw new ArgumentException($"岗位编号重复：{posting.Id.Value}。", nameof(sites));
                    }
                }

                _siteIndex.Add(buildingId, i);
                _sites[i] = site;
            }

            var residentCounts = new Dictionary<int, int>();
            for (int i = 0; i < _citizens.Length; i++)
            {
                Citizen citizen = _citizens[i];
                if (!_siteIndex.TryGetValue(citizen.ResidenceId.Value, out int homeIndex))
                {
                    throw new ArgumentException($"市民 {citizen.Id.Value} 的住所 {citizen.ResidenceId.Value} 不存在。", nameof(citizens));
                }

                Building home = _sites[homeIndex].Building;
                if (home.Kind != BuildingKind.Housing)
                {
                    throw new ArgumentException($"市民 {citizen.Id.Value} 的住所不是住宅。", nameof(citizens));
                }

                int count;
                if (!residentCounts.TryGetValue(home.Id.Value, out count))
                {
                    count = 0;
                }

                count++;
                if (count > home.HousingResidentCapacity)
                {
                    throw new ArgumentException($"住宅 {home.Id.Value} 超过居住容量 {home.HousingResidentCapacity}。", nameof(citizens));
                }

                residentCounts[home.Id.Value] = count;
            }
        }

        public IReadOnlyList<Citizen> Citizens => _citizens;

        public IReadOnlyList<BuildingSite> Sites => _sites;

        public Treasury Treasury { get; }

        public bool TryGetCitizen(CitizenId id, out Citizen? citizen)
        {
            if (_citizenIndex.TryGetValue(id.Value, out int index))
            {
                citizen = _citizens[index];
                return true;
            }

            citizen = null;
            return false;
        }

        public bool TryGetSite(BuildingId id, out BuildingSite? site)
        {
            if (_siteIndex.TryGetValue(id.Value, out int index))
            {
                site = _sites[index];
                return true;
            }

            site = null;
            return false;
        }

        public BuildingSite GetSite(BuildingId id)
        {
            if (!TryGetSite(id, out BuildingSite? site))
            {
                throw new ArgumentException($"没有建筑 {id.Value}。", nameof(id));
            }

            return site!;
        }

        public int GetQuantity(BuildingId buildingId, Good good)
        {
            return GetSite(buildingId).Inventory.GetQuantity(good);
        }

        public InventoryResult TryDeposit(BuildingId buildingId, Good good, int quantity)
        {
            if (!TryGetSite(buildingId, out BuildingSite? site))
            {
                return InventoryResult.Fail(InventoryRejection.UnknownBuilding);
            }

            return site!.Inventory.TryDeposit(good, quantity);
        }

        public InventoryResult TryWithdraw(BuildingId buildingId, Good good, int quantity)
        {
            if (!TryGetSite(buildingId, out BuildingSite? site))
            {
                return InventoryResult.Fail(InventoryRejection.UnknownBuilding);
            }

            return site!.Inventory.TryWithdraw(good, quantity);
        }

        /// <summary>设置或改写某建筑的岗位编制。不接收市民，也不记录在职人数。</summary>
        public JobPosting SetJobPosting(
            BuildingId buildingId,
            JobPostingId postingId,
            string title,
            int openings,
            long wagePerHour,
            int minSkillLevel,
            string requiredSkillId)
        {
            BuildingSite site = GetSite(buildingId);
            EnsureJobIdAvailable(buildingId, postingId);
            return site.Building.SetJobPosting(postingId, title, openings, wagePerHour, minSkillLevel, requiredSkillId);
        }

        public bool RemoveJobPosting(BuildingId buildingId, JobPostingId postingId)
        {
            return GetSite(buildingId).Building.RemoveJobPosting(postingId);
        }

        private void EnsureJobIdAvailable(BuildingId buildingId, JobPostingId postingId)
        {
            for (int i = 0; i < _sites.Length; i++)
            {
                Building building = _sites[i].Building;
                if (building.Id == buildingId)
                {
                    continue;
                }

                IReadOnlyList<JobPosting> postings = building.JobPostings;
                for (int j = 0; j < postings.Count; j++)
                {
                    if (postings[j].Id == postingId)
                    {
                        throw new InvalidOperationException($"岗位编号 {postingId.Value} 已用于建筑 {building.Id.Value}。");
                    }
                }
            }
        }
    }
}
