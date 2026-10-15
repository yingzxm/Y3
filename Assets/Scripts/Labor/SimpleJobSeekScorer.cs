#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Domain;

namespace SocialEvolution.Labor
{
    /// <summary>
    /// 工资、工时、通勤、强度、安全、发展、地位、技能匹配、稳定性的加权和。
    /// 权重全部来自构造注入的 <see cref="JobSeekScoreWeights"/>。
    /// </summary>
    public sealed class SimpleJobSeekScorer : IJobSeekScorer
    {
        private readonly JobSeekScoreWeights _weights;

        public SimpleJobSeekScorer(JobSeekScoreWeights weights)
        {
            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }

            _weights = weights;
        }

        public JobSeekScoreWeights Weights => _weights;

        public float Score(IPersonReadModel person, JobPosting posting, JobSeekContext context)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            if (posting == null)
            {
                throw new ArgumentNullException(nameof(posting));
            }

            if (!posting.IsEligible(person))
            {
                return float.NegativeInfinity;
            }

            if (context == null)
            {
                context = new JobSeekContext();
            }

            float skillMatch = SkillMatch(person, posting, _weights);
            float wage = (float)posting.WagePerHour;
            return (wage * _weights.Wage)
                - (posting.WorkHours * _weights.HoursPenalty)
                - (context.CommuteHours * _weights.CommutePenalty)
                - (posting.WorkIntensity * _weights.IntensityPenalty)
                + (context.WorkplaceSafety * _weights.Safety)
                + (context.Development * _weights.Development)
                + (context.Status * _weights.Status)
                + (skillMatch * _weights.SkillMatch)
                + (context.Stability * _weights.Stability);
        }

        private static float SkillMatch(IPersonReadModel person, JobPosting posting, JobSeekScoreWeights weights)
        {
            int need = posting.MinSkillLevel;
            if (need <= 0)
            {
                return weights.FullSkillMatch;
            }

            int have = SkillLevelOf(person, posting.RequiredSkillId);
            float ratio = have / (float)need;
            if (ratio > weights.FullSkillMatch)
            {
                return weights.FullSkillMatch;
            }

            return ratio;
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
