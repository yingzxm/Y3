#nullable enable
using System.Collections.Generic;

namespace SocialEvolution.Domain
{
    /// <summary>
    /// 市民只读视图。Id 使用 <see cref="CitizenId"/>。
    /// 这里没有岗位、雇主，也没有对话。
    /// </summary>
    public interface IPersonReadModel
    {
        CitizenId Id { get; }

        string Name { get; }

        int Age { get; }

        EducationLevel Education { get; }

        IReadOnlyList<SkillLevel> Skills { get; }

        /// <summary>疲劳公式中的健康系数。1 表示常态，必须是有限正数。</summary>
        float HealthFactor { get; }

        /// <summary>个人财富，货币单位，long。可以是 0。</summary>
        long Wealth { get; }

        /// <summary>住所建筑。不是岗位，也不是派工。</summary>
        BuildingId ResidenceId { get; }
    }
}
