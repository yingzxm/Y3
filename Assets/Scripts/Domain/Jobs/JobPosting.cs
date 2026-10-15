#nullable enable
using System;

namespace SocialEvolution.Domain
{
    /// <summary>
    /// 岗位编制。只记录编制人数、时薪和要求的技能。
    /// 在职人数和雇佣记录不在这里，留给以后的 Labor 模块。
    /// 没有把某位市民派进岗位的方法。
    /// </summary>
    public sealed class JobPosting
    {
        private JobPosting(
            JobPostingId id,
            BuildingId buildingId,
            string title,
            int openings,
            long wagePerHour,
            int minSkillLevel,
            string requiredSkillId)
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
            RequiredSkillId = string.Empty;
            Configure(title, openings, wagePerHour, minSkillLevel, requiredSkillId);
        }

        public JobPostingId Id { get; }

        public BuildingId BuildingId { get; }

        public string Title { get; private set; }

        /// <summary>编制人数。不是在职人数。</summary>
        public int Openings { get; private set; }

        /// <summary>货币单位 / 游戏时。</summary>
        public long WagePerHour { get; private set; }

        public int MinSkillLevel { get; private set; }

        /// <summary><see cref="SkillIds"/> 中的技能编号。</summary>
        public string RequiredSkillId { get; private set; }

        public void Configure(string title, int openings, long wagePerHour, int minSkillLevel, string requiredSkillId)
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

            if (!SkillIds.IsKnown(requiredSkillId))
            {
                throw new ArgumentException("岗位要求的技能必须使用 SkillIds 常量。", nameof(requiredSkillId));
            }

            Title = title.Trim();
            Openings = openings;
            WagePerHour = wagePerHour;
            MinSkillLevel = minSkillLevel;
            RequiredSkillId = requiredSkillId;
        }

        internal static JobPosting Create(
            JobPostingId id,
            BuildingId buildingId,
            string title,
            int openings,
            long wagePerHour,
            int minSkillLevel,
            string requiredSkillId)
        {
            return new JobPosting(id, buildingId, title, openings, wagePerHour, minSkillLevel, requiredSkillId);
        }
    }
}
