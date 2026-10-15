#nullable enable
using System;
using System.Collections.Generic;

namespace SocialEvolution.Domain
{
    /// <summary>
    /// 建筑。编制只能由 World 程序集里的 <see cref="SetJobPosting"/> 写入，调用方应走 WorldState。
    /// 住宅容量是上限。住户记在市民的 ResidenceId 上，建筑不保存住户名单，也不保存雇员。
    /// </summary>
    public sealed class Building
    {
        private readonly List<JobPosting> _jobPostings = new List<JobPosting>();

        public Building(BuildingId id, BuildingKind kind, string name, int housingResidentCapacity = 0)
        {
            if (id.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "建筑编号必须为正整数。");
            }

            if (!Enum.IsDefined(typeof(BuildingKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "未知建筑类型。");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("建筑名称不能为空。", nameof(name));
            }

            if (housingResidentCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(housingResidentCapacity), housingResidentCapacity, "居住容量不能为负。");
            }

            if (kind == BuildingKind.Housing && housingResidentCapacity == 0)
            {
                throw new ArgumentException("住宅必须给出大于 0 的居住容量。", nameof(housingResidentCapacity));
            }

            if (kind != BuildingKind.Housing && housingResidentCapacity != 0)
            {
                throw new ArgumentException("只有住宅可以设置居住容量。", nameof(housingResidentCapacity));
            }

            Id = id;
            Kind = kind;
            Name = name.Trim();
            HousingResidentCapacity = housingResidentCapacity;
        }

        public BuildingId Id { get; }

        public BuildingKind Kind { get; }

        public string Name { get; }

        /// <summary>非住宅为 0。不表示已入住人数。</summary>
        public int HousingResidentCapacity { get; }

        public IReadOnlyList<JobPosting> JobPostings => _jobPostings;

        /// <summary>新增或按编号改写编制。internal，仅供 WorldState 在校验编号后调用。不招人。</summary>
        internal JobPosting SetJobPosting(
            JobPostingId id,
            string title,
            int openings,
            long wagePerHour,
            int minSkillLevel,
            string requiredSkillId)
        {
            if (id.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "岗位编号必须为正整数。");
            }

            for (int i = 0; i < _jobPostings.Count; i++)
            {
                if (_jobPostings[i].Id == id)
                {
                    _jobPostings[i].Configure(title, openings, wagePerHour, minSkillLevel, requiredSkillId);
                    return _jobPostings[i];
                }
            }

            JobPosting created = JobPosting.Create(id, Id, title, openings, wagePerHour, minSkillLevel, requiredSkillId);
            _jobPostings.Add(created);
            return created;
        }

        /// <summary>撤下编制。没有在职人员可被解雇。</summary>
        public bool RemoveJobPosting(JobPostingId id)
        {
            for (int i = 0; i < _jobPostings.Count; i++)
            {
                if (_jobPostings[i].Id == id)
                {
                    _jobPostings.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
    }
}
