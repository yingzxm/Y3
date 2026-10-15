#nullable enable

namespace SocialEvolution.Agents
{
    /// <summary>
    /// 人格只用三个 [0, 1] 标量：风险偏好、工作积极性、储蓄倾向。
    /// 决策器把它们乘进 NeedScore，不另开一条行动表。
    /// </summary>
    public sealed class PersonalityScalars
    {
        public PersonalityScalars()
        {
            RiskPreference = 0.5f;
            WorkDrive = 0.5f;
            SavingTendency = 0.5f;
        }

        public PersonalityScalars(float riskPreference, float workDrive, float savingTendency)
        {
            RiskPreference = riskPreference;
            WorkDrive = workDrive;
            SavingTendency = savingTendency;
        }

        /// <summary>0 避险，1 冒险。安全需求的权重和是否辞职会读它。</summary>
        public float RiskPreference { get; set; }

        /// <summary>0 不愿干活，1 很想干活。财富需求的权重会读它。</summary>
        public float WorkDrive { get; set; }

        /// <summary>0 爱花，1 爱存。财富需求的权重会读它。</summary>
        public float SavingTendency { get; set; }
    }
}
