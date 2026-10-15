#nullable enable
using System.Collections.Generic;

namespace SocialEvolution.Core
{
    /// <summary>
    /// 时间倍率。暂停不是倍率，见 <see cref="ITimeService.IsPaused"/>。
    /// 保证可用 1x 与 5x；任意有限正数都接受，预设里留了后续档位。
    /// </summary>
    public static class TimeScales
    {
        public const float X1 = 1f;
        public const float X5 = 5f;
        public const float X20 = 20f;
        public const float X100 = 100f;
        public const float X1000 = 1000f;

        public static IReadOnlyList<float> Presets { get; } = new[]
        {
            X1,
            X5,
            X20,
            X100,
            X1000,
        };

        public static bool IsSupported(float scale) => float.IsFinite(scale) && scale > 0f;
    }
}
