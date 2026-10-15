#nullable enable
using System.Collections.Generic;
using SocialEvolution.Agents;
using SocialEvolution.Domain;

namespace SocialEvolution.Stats
{
    /// <summary>
    /// 点选一位市民时看到的档案。现金是 <see cref="Citizen.Wealth"/>。
    /// 工作来自劳动力市场。最急需求按紧急度，并列时饥饿、疲劳、财富、安全靠前的胜出。
    /// 紧急度全是 0 时没有最急需求。
    /// </summary>
    public readonly struct CitizenView
    {
        public CitizenView(
            CitizenId id,
            string name,
            long cash,
            bool employed,
            string jobTitle,
            long wagePerHour,
            bool hasTopNeed,
            NeedKind topNeed,
            float topNeedUrgency,
            IReadOnlyList<CitizenLogLine> recentLogs)
        {
            Id = id;
            Name = name ?? string.Empty;
            Cash = cash;
            Employed = employed;
            JobTitle = jobTitle ?? string.Empty;
            WagePerHour = wagePerHour;
            HasTopNeed = hasTopNeed;
            TopNeed = topNeed;
            TopNeedUrgency = topNeedUrgency;
            RecentLogs = recentLogs ?? System.Array.Empty<CitizenLogLine>();
        }

        public CitizenId Id { get; }

        public string Name { get; }

        public long Cash { get; }

        public bool Employed { get; }

        public string JobTitle { get; }

        public long WagePerHour { get; }

        public bool HasTopNeed { get; }

        public NeedKind TopNeed { get; }

        public float TopNeedUrgency { get; }

        /// <summary>从旧到新。不足三行时列表更短。</summary>
        public IReadOnlyList<CitizenLogLine> RecentLogs { get; }

        public string TopNeedLabel => HasTopNeed ? NeedText.Label(TopNeed) : string.Empty;

        /// <summary>最近三行，从旧到新。不足三行时返回 false，三个 out 为空字符串。</summary>
        public bool TryGetThreeRecentLogs(out string first, out string second, out string third)
        {
            first = string.Empty;
            second = string.Empty;
            third = string.Empty;
            if (RecentLogs.Count < 3)
            {
                return false;
            }

            int start = RecentLogs.Count - 3;
            first = RecentLogs[start].Text;
            second = RecentLogs[start + 1].Text;
            third = RecentLogs[start + 2].Text;
            return true;
        }
    }
}
