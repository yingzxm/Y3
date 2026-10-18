#nullable enable

namespace SocialEvolution.Core
{
    /// <summary>
    /// 空的模拟槽。招聘、发薪和数值 Utility 不在这里。
    /// 市场小时结算由 GameSession 另行注册 CityMarket.OnGameHour。
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
