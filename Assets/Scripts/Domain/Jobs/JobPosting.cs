#nullable enable
using System;

namespace SocialEvolution.Domain
{
    /// <summary>
    /// 岗位编制。只记录空缺人数、时薪和技能门槛。
    /// 没有入职对象；<see cref="VacantSlots"/> 在本里程碑始终等于编制。
    /// 门槛不够时保持空岗，不提供把某位市民派进岗位的方法。
    /// </summary>
    public sealed class JobPosting
    {
        private JobPosting(JobPostingId id, BuildingId buildingId, string title, int openings, int wagePerHour, int minSkillLevel)
        {
            if (id.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (buildingId.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(buildingId));
            }

            Id = id;
            BuildingId = buildingId;
            Title = string.Empty;
            Configure(title, openings, wagePerHour, minSkillLevel);
        }

        public JobPostingId Id { get; }

        public BuildingId BuildingId { get; }

        public string Title { get; private set; }

        public int Openings { get; private set; }

        /// <summary>货币单位 / 游戏时。</summary>
        public int WagePerHour { get; private set; }

        public int MinSkillLevel { get; private set; }

        /// <summary>无人入职。空缺等于编制。</summary>
        public int VacantSlots => Openings;

        public void Configure(string title, int openings, int wagePerHour, int minSkillLevel)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("岗位名称不能为空。", nameof(title));
            }

            if (openings < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(openings), openings, "编制不能为负。");
            }

            if (wagePerHour < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(wagePerHour), wagePerHour, "时薪不能为负。");
            }

            if (minSkillLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minSkillLevel), minSkillLevel, "技能门槛不能为负。");
            }

            Title = title.Trim();
            Openings = openings;
            WagePerHour = wagePerHour;
            MinSkillLevel = minSkillLevel;
        }

        internal static JobPosting Create(JobPostingId id, BuildingId buildingId, string title, int openings, int wagePerHour, int minSkillLevel)
        {
            return new JobPosting(id, buildingId, title, openings, wagePerHour, minSkillLevel);
        }
    }
}
