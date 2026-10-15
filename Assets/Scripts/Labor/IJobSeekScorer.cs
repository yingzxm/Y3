#nullable enable
using SocialEvolution.Domain;

namespace SocialEvolution.Labor
{
    /// <summary>
    /// 给申请人给岗位打分。不修改市场。不合格返回 <see cref="float.NegativeInfinity"/>。
    /// 权重来自注入的 <see cref="JobSeekScoreWeights"/>。
    /// </summary>
    public interface IJobSeekScorer
    {
        float Score(IPersonReadModel person, JobPosting posting, JobSeekContext context);
    }
}
