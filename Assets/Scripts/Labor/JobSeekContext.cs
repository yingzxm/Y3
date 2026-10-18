#nullable enable

namespace SocialEvolution.Labor
{
    /// <summary>
    /// 求职时由外部填入的环境量（通勤、安全、地位、稳定、发展）。
    /// 未填时为 0，不在评分器里另设偏好。
    /// </summary>
    public sealed class JobSeekContext
    {
        public float CommuteHours { get; set; }

        public float WorkplaceSafety { get; set; }

        public float Status { get; set; }

        public float Stability { get; set; }

        public float Development { get; set; }
    }
}
