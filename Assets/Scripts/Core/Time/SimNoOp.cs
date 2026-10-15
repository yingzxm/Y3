#nullable enable

namespace SocialEvolution.Core
{
    /// <summary>
    /// 空的模拟槽。经济、招聘、市场和数值 Utility 都不在 P0 里运行。
    /// </summary>
    public static class SimNoOp
    {
        public static void OnGameHour(GameDateTime _)
        {
        }

        public static void OnGameDay(GameDateTime _)
        {
        }
    }
}
