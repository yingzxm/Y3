#nullable enable

namespace SocialEvolution.Labor
{
    /// <summary>
    /// 求职评分的维度权重。数值只放在这个类型里，评分和应聘流程不写死权重。
    /// 正式表由 JSON 或 ScriptableObject 填进这些属性；本程序集不引用引擎，
    /// ScriptableObject 适配留在后续的引擎层。<see cref="DemoDefaults"/> 只是占位。
    /// </summary>
    public sealed class JobSeekScoreWeights
    {
        public JobSeekScoreWeights()
        {
            Wage = 1f;
            HoursPenalty = 0.1f;
            CommutePenalty = 0.5f;
            IntensityPenalty = 0.2f;
            Safety = 0.3f;
            Development = 0.2f;
            Status = 0.1f;
            SkillMatch = 0.4f;
            Stability = 0.2f;
            FullSkillMatch = 1f;
        }

        public float Wage { get; set; }

        public float HoursPenalty { get; set; }

        public float CommutePenalty { get; set; }

        public float IntensityPenalty { get; set; }

        public float Safety { get; set; }

        public float Development { get; set; }

        public float Status { get; set; }

        public float SkillMatch { get; set; }

        public float Stability { get; set; }

        /// <summary>技能匹配比的上限。超过门槛后不再加分。</summary>
        public float FullSkillMatch { get; set; }

        public static JobSeekScoreWeights DemoDefaults => new JobSeekScoreWeights();

        public JobSeekScoreWeights Clone()
        {
            return new JobSeekScoreWeights
            {
                Wage = Wage,
                HoursPenalty = HoursPenalty,
                CommutePenalty = CommutePenalty,
                IntensityPenalty = IntensityPenalty,
                Safety = Safety,
                Development = Development,
                Status = Status,
                SkillMatch = SkillMatch,
                Stability = Stability,
                FullSkillMatch = FullSkillMatch,
            };
        }
    }
}
