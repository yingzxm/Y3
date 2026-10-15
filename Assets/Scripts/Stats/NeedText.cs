#nullable enable
using SocialEvolution.Agents;

namespace SocialEvolution.Stats
{
    public static class NeedText
    {
        public static string Label(NeedKind kind)
        {
            switch (kind)
            {
                case NeedKind.Hunger:
                    return "饥饿";
                case NeedKind.Fatigue:
                    return "疲劳";
                case NeedKind.Wealth:
                    return "财富";
                case NeedKind.Safety:
                    return "安全";
                default:
                    return "未知";
            }
        }
    }
}
