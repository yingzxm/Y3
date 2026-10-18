#nullable enable

namespace SocialEvolution.Labor
{
    /// <summary>班次。挂在 Labor 的岗位条件上，不进 Domain 编制。</summary>
    public enum WorkShift
    {
        Day = 0,
        Evening = 1,
        Night = 2,
        Flexible = 3,
    }
}
