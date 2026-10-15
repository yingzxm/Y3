#nullable enable
using System;

namespace SocialEvolution.Domain
{
    public readonly struct JobPostingId : IEquatable<JobPostingId>
    {
        public int Value { get; }

        public JobPostingId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "岗位编号必须为正整数。");
            }

            Value = value;
        }

        public bool Equals(JobPostingId other) => Value == other.Value;

        public override bool Equals(object? obj) => obj is JobPostingId other && Equals(other);

        public override int GetHashCode() => Value;

        public static bool operator ==(JobPostingId left, JobPostingId right) => left.Equals(right);

        public static bool operator !=(JobPostingId left, JobPostingId right) => !left.Equals(right);

        public override string ToString() => Value.ToString();
    }
}
