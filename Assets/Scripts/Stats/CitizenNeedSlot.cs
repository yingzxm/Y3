#nullable enable
using SocialEvolution.Agents;

namespace SocialEvolution.Stats
{
    /// <summary>
    /// 详情面板上的一个需求槽。紧急度来自 <see cref="AgentNeeds.GetUrgency"/>。
    /// <see cref="Present"/> 为 false 时没有大脑，<see cref="Label"/> 为空。
    /// </summary>
    public readonly struct CitizenNeedSlot
    {
        public CitizenNeedSlot(bool present, NeedKind kind, float urgency)
        {
            Present = present;
            Kind = kind;
            Urgency = urgency;
        }

        public bool Present { get; }

        public NeedKind Kind { get; }

        public float Urgency { get; }

        public string Label => Present ? NeedText.Label(Kind) : string.Empty;
    }
}
