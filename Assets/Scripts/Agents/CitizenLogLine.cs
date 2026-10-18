#nullable enable
using SocialEvolution.Core;
using SocialEvolution.Domain;

namespace SocialEvolution.Agents
{
    /// <summary>一位市民在某个游戏时执行过的一行行动。</summary>
    public readonly struct CitizenLogLine
    {
        public CitizenLogLine(GameDateTime when, CitizenId citizenId, AgentAction action, string text)
        {
            When = when;
            CitizenId = citizenId;
            Action = action;
            Text = text ?? string.Empty;
        }

        public GameDateTime When { get; }

        public CitizenId CitizenId { get; }

        public AgentAction Action { get; }

        public string Text { get; }
    }
}
