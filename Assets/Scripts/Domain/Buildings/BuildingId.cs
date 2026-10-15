#nullable enable
using System;

namespace SocialEvolution.Domain
{
    public readonly struct BuildingId : IEquatable<BuildingId>
    {
        public int Value { get; }

        public BuildingId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "建筑编号必须为正整数。");
            }

            Value = value;
        }

        public bool Equals(BuildingId other) => Value == other.Value;

        public override bool Equals(object? obj) => obj is BuildingId other && Equals(other);

        public override int GetHashCode() => Value;

        public static bool operator ==(BuildingId left, BuildingId right) => left.Equals(right);

        public static bool operator !=(BuildingId left, BuildingId right) => !left.Equals(right);

        public override string ToString() => Value.ToString();
    }
}
