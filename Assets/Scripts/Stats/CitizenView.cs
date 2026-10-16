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
    /// 详情面板只读这一份：<see cref="CitizenPicker.TryRead"/> 或 <see cref="DemoReadout.TryGetSelected"/>。
    /// 模拟没有单独的心理 P，<see cref="PsychologyP"/> 等于 <see cref="WorkDrive"/>。
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
            IReadOnlyList<CitizenLogLine> recentLogs,
            BuildingId residenceId,
            string residenceLabel,
            EducationLevel education,
            bool hasWorkplace,
            BuildingId workplaceId,
            CitizenNeedSlot need1,
            CitizenNeedSlot need2,
            CitizenNeedSlot need3,
            bool hasPsychology,
            float riskPreference,
            float workDrive,
            float savingTendency)
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
            ResidenceId = residenceId;
            ResidenceLabel = residenceLabel ?? string.Empty;
            Education = education;
            HasWorkplace = hasWorkplace;
            WorkplaceId = workplaceId;
            Need1 = need1;
            Need2 = need2;
            Need3 = need3;
            HasPsychology = hasPsychology;
            RiskPreference = riskPreference;
            WorkDrive = workDrive;
            SavingTendency = savingTendency;
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

        /// <summary>住所建筑。名字来自建筑；没有名字时用建筑编号。</summary>
        public BuildingId ResidenceId { get; }

        public string ResidenceLabel { get; }

        public EducationLevel Education { get; }

        public string EducationLabel => LabelEducation(Education);

        /// <summary>在职且岗位还在时，工作地点是岗位所在建筑。否则 <see cref="WorkplaceId"/> 无意义。</summary>
        public bool HasWorkplace { get; }

        public BuildingId WorkplaceId { get; }

        /// <summary>紧急度前三。没有 <c>AgentBrain</c> 时 <see cref="CitizenNeedSlot.Present"/> 为 false。</summary>
        public CitizenNeedSlot Need1 { get; }

        public CitizenNeedSlot Need2 { get; }

        public CitizenNeedSlot Need3 { get; }

        /// <summary>没有大脑时三个标量和 <see cref="PsychologyP"/> 都是 0。</summary>
        public bool HasPsychology { get; }

        public float RiskPreference { get; }

        public float WorkDrive { get; }

        public float SavingTendency { get; }

        /// <summary>演示面板的心理 P。等于 <see cref="WorkDrive"/>。</summary>
        public float PsychologyP => WorkDrive;

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

        private static string LabelEducation(EducationLevel education)
        {
            switch (education)
            {
                case EducationLevel.None:
                    return "无";
                case EducationLevel.Primary:
                    return "小学";
                case EducationLevel.Secondary:
                    return "中学";
                case EducationLevel.Vocational:
                    return "职业教育";
                case EducationLevel.Higher:
                    return "高等教育";
                default:
                    return string.Empty;
            }
        }
    }
}
