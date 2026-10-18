#nullable enable
using SocialEvolution.Core;

namespace SocialEvolution.Agents
{
    /// <summary>把一次行动写成日志行。场景和测试都读这一行字。</summary>
    public static class CitizenLogText
    {
        public static string Format(GameDateTime when, AgentAction action)
        {
            return when.ToString() + " " + Verb(action);
        }

        public static string Verb(AgentAction action)
        {
            switch (action)
            {
                case AgentAction.SeekJob:
                    return "求职";
                case AgentAction.Quit:
                    return "辞职";
                case AgentAction.Work:
                    return "上班";
                case AgentAction.Eat:
                    return "进食";
                case AgentAction.Sleep:
                    return "睡眠";
                case AgentAction.Shop:
                    return "购物";
                case AgentAction.Idle:
                    return "闲置";
                default:
                    return "无行动";
            }
        }
    }
}
