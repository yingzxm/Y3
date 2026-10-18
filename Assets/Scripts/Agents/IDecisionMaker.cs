#nullable enable

namespace SocialEvolution.Agents
{
    /// <summary>
    /// 一个游戏时选一个行动。同样的输入得到同样的结果。
    /// 由 <see cref="AgentsHourDriver"/> 在小时相位调用，不在渲染帧里调用。
    /// <see cref="Decide"/> 不要改市场或需求。同一小时有人辞职时，驱动会让还没入职的人再决策一次。
    /// </summary>
    public interface IDecisionMaker
    {
        AgentAction Decide(in AgentDecisionContext context);
    }

    /// <summary>
    /// 一次小时决策的输入。繁荣指数只作为 NeedScore 的软权重。
    /// </summary>
    public readonly struct AgentDecisionContext
    {
        public AgentDecisionContext(
            AgentNeeds? needs,
            PersonalityScalars? personality,
            bool isEmployed,
            bool hasOpenJobsAvailable,
            float? prosperityIndex)
        {
            Needs = needs;
            Personality = personality;
            IsEmployed = isEmployed;
            HasOpenJobsAvailable = hasOpenJobsAvailable;
            ProsperityIndex = prosperityIndex;
        }

        public AgentNeeds? Needs { get; }

        public PersonalityScalars? Personality { get; }

        public bool IsEmployed { get; }

        public bool HasOpenJobsAvailable { get; }

        /// <summary>
        /// 繁荣指数，约定在 [0, 1]。只软乘需求权重。
        /// 没有值、非有限数时，决策器当作不调整。
        /// </summary>
        public float? ProsperityIndex { get; }
    }
}
