#nullable enable
using System;

namespace SocialEvolution.Domain
{
    /// <summary>一项技能及其等级。等级从 0 起。</summary>
    public readonly struct SkillLevel : IEquatable<SkillLevel>
    {
        public SkillLevel(string skillId, int level)
        {
            if (!SkillIds.IsKnown(skillId))
            {
                throw new ArgumentOutOfRangeException(nameof(skillId), skillId, "未知技能。");
            }

            if (level < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "技能等级不能为负。");
            }

            SkillId = skillId;
            Level = level;
        }

        public string SkillId { get; }

        public int Level { get; }

        public bool Equals(SkillLevel other) => SkillId == other.SkillId && Level == other.Level;

        public override bool Equals(object? obj) => obj is SkillLevel other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(SkillId, Level);

        public static bool operator ==(SkillLevel left, SkillLevel right) => left.Equals(right);

        public static bool operator !=(SkillLevel left, SkillLevel right) => !left.Equals(right);
    }
}
