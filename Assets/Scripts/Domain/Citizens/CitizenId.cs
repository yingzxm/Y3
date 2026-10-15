#nullable enable
using System;

namespace SocialEvolution.Domain
{
    public readonly struct CitizenId : IEquatable<CitizenId>
    {
        public int Value { get; }

        public CitizenId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "市民编号必须为正整数。");
            }

            Value = value;
        }

        public bool Equals(CitizenId other) => Value == other.Value;

        public override bool Equals(object? obj) => obj is CitizenId other && Equals(other);

        public override int GetHashCode() => Value;

        public static bool operator ==(CitizenId left, CitizenId right) => left.Equals(right);

        public static bool operator !=(CitizenId left, CitizenId right) => !left.Equals(right);

        public override string ToString() => Value.ToString();
    }
}
