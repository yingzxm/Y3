#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Domain;
using DomainJobPosting = SocialEvolution.Domain.JobPosting;

namespace SocialEvolution.Labor
{
    /// <summary>
    /// Domain 编制的劳动视图。<see cref="Openings"/>、<see cref="WagePerHour"/>、
    /// <see cref="RequiredSkillId"/> 和 <see cref="MinSkillLevel"/> 直接读 Domain 岗位。
    /// 工时、强度、班次、学历和年龄来自 <see cref="JobConditions"/>。在职人数不在这里。
    /// </summary>
    public sealed class JobPosting
    {
        public JobPosting(DomainJobPosting source, JobConditions conditions)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (conditions == null)
            {
                throw new ArgumentNullException(nameof(conditions));
            }

            if (source.Id != conditions.PostingId)
            {
                throw new ArgumentException("岗位条件和编制不是同一个编号。", nameof(conditions));
            }

            Source = source;
            Conditions = conditions;
        }

        public DomainJobPosting Source { get; }

        public JobConditions Conditions { get; }

        public JobPostingId Id => Source.Id;

        public BuildingId BuildingId => Source.BuildingId;

        public string Title => Source.Title;

        public int Openings => Source.Openings;

        public long WagePerHour => Source.WagePerHour;

        public int MinSkillLevel => Source.MinSkillLevel;

        public string RequiredSkillId => Source.RequiredSkillId;

        public float WorkHours => Conditions.WorkHours;

        public float WorkIntensity => Conditions.WorkIntensity;

        public WorkShift Shift => Conditions.Shift;

        public EducationLevel MinEducation => Conditions.MinEducation;

        public int MinAge => Conditions.MinAge;

        public int? MaxAge => Conditions.MaxAge;

        public int Priority => Conditions.Priority;

        /// <summary>年龄、学历，以及 RequiredSkillId 上的 Domain 技能等级门槛。</summary>
        public bool IsEligible(IPersonReadModel person)
        {
            if (person == null)
            {
                return false;
            }

            if (person.Age < MinAge)
            {
                return false;
            }

            if (MaxAge.HasValue && person.Age > MaxAge.Value)
            {
                return false;
            }

            if ((int)person.Education < (int)MinEducation)
            {
                return false;
            }

            return SkillLevelOf(person, RequiredSkillId) >= MinSkillLevel;
        }

        private static int SkillLevelOf(IPersonReadModel person, string skillId)
        {
            IReadOnlyList<SkillLevel> skills = person.Skills;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].SkillId == skillId)
                {
                    return skills[i].Level;
                }
            }

            return 0;
        }
    }
}
