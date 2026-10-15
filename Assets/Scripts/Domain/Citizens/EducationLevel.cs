#nullable enable

namespace SocialEvolution.Domain
{
    /// <summary>学历。数值越大表示受教育越久，不做硬性阶段门。</summary>
    public enum EducationLevel
    {
        None = 0,
        Primary = 1,
        Secondary = 2,
        Vocational = 3,
        Higher = 4,
    }
}
