#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Core;
using SocialEvolution.Domain;

namespace SocialEvolution.Agents
{
    /// <summary>
    /// 每位市民最近的行动。默认保留 8 行，至少 3 行。
    /// 超过容量时丢掉最旧的一行。顺序是从旧到新。
    /// </summary>
    public sealed class CitizenActionLog
    {
        public const int MinimumCapacity = 3;
        public const int DefaultCapacity = 8;

        private readonly Dictionary<int, List<CitizenLogLine>> _byCitizen = new Dictionary<int, List<CitizenLogLine>>();

        public CitizenActionLog(int capacityPerCitizen = DefaultCapacity)
        {
            if (capacityPerCitizen < MinimumCapacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacityPerCitizen),
                    capacityPerCitizen,
                    "每位市民至少保留 3 行日志。");
            }

            CapacityPerCitizen = capacityPerCitizen;
        }

        public int CapacityPerCitizen { get; }

        public void Append(CitizenId citizenId, GameDateTime when, AgentAction action, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("日志正文不能为空。", nameof(text));
            }

            List<CitizenLogLine>? lines;
            if (!_byCitizen.TryGetValue(citizenId.Value, out lines) || lines == null)
            {
                lines = new List<CitizenLogLine>();
                _byCitizen.Add(citizenId.Value, lines);
            }

            if (lines.Count == CapacityPerCitizen)
            {
                lines.RemoveAt(0);
            }

            lines.Add(new CitizenLogLine(when, citizenId, action, text));
        }

        public int Count(CitizenId citizenId)
        {
            List<CitizenLogLine>? lines;
            if (!_byCitizen.TryGetValue(citizenId.Value, out lines) || lines == null)
            {
                return 0;
            }

            return lines.Count;
        }

        public IReadOnlyList<CitizenLogLine> GetRecent(CitizenId citizenId)
        {
            List<CitizenLogLine>? lines;
            if (!_byCitizen.TryGetValue(citizenId.Value, out lines) || lines == null || lines.Count == 0)
            {
                return Array.Empty<CitizenLogLine>();
            }

            var copy = new CitizenLogLine[lines.Count];
            for (int i = 0; i < lines.Count; i++)
            {
                copy[i] = lines[i];
            }

            return copy;
        }
    }
}
