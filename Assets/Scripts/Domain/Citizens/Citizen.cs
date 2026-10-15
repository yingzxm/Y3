#nullable enable
using System;
using System.Collections.Generic;

namespace SocialEvolution.Domain
{
    /// <summary>
    /// 市民。档案含年龄、学历、技能、健康系数、个人财富和住所。
    /// 没有职业，也没有被指派到某个岗位的字段。
    /// </summary>
    public sealed class Citizen : IPersonReadModel
    {
        public Citizen(
            CitizenId id,
            string name,
            int age,
            EducationLevel education,
            IReadOnlyList<SkillLevel> skills,
            float healthFactor,
            long wealth,
            BuildingId residenceId)
        {
            if (id.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "市民编号必须为正整数。");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("市民名字不能为空。", nameof(name));
            }

            if (age < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(age), age, "年龄不能为负。");
            }

            if (!Enum.IsDefined(typeof(EducationLevel), education))
            {
                throw new ArgumentOutOfRangeException(nameof(education), education, "未知学历。");
            }

            if (skills == null)
            {
                throw new ArgumentNullException(nameof(skills));
            }

            if (float.IsNaN(healthFactor) || float.IsInfinity(healthFactor) || healthFactor <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(healthFactor), healthFactor, "健康系数必须是有限正数。");
            }

            if (residenceId.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(residenceId), "住所编号必须为正整数。");
            }

            var copy = new SkillLevel[skills.Count];
            var seen = new HashSet<string>();
            for (int i = 0; i < skills.Count; i++)
            {
                SkillLevel skill = skills[i];
                if (!SkillIds.IsKnown(skill.SkillId) || !seen.Add(skill.SkillId))
                {
                    throw new ArgumentException("技能必须是已知编号，且不能重复。", nameof(skills));
                }

                copy[i] = skill;
            }

            Id = id;
            Name = name.Trim();
            Age = age;
            Education = education;
            Skills = copy;
            HealthFactor = healthFactor;
            Wealth = wealth;
            ResidenceId = residenceId;
        }

        public CitizenId Id { get; }

        public string Name { get; }

        public int Age { get; }

        public EducationLevel Education { get; }

        public IReadOnlyList<SkillLevel> Skills { get; }

        public float HealthFactor { get; }

        public long Wealth { get; }

        public BuildingId ResidenceId { get; }
    }
}
