#nullable enable
using System;

namespace SocialEvolution.Core
{
    /// <summary>
    /// 对外游戏时间。1 游戏日 = <see cref="HoursPerDay"/> 游戏时，最小单位是游戏时。
    /// 游戏日从 1 起算，游戏时是当日的 0–23。
    /// </summary>
    public readonly struct GameDateTime : IEquatable<GameDateTime>
    {
        public const int HoursPerDay = 24;

        public int Day { get; }

        public int Hour { get; }

        public GameDateTime(int day, int hour)
        {
            if (day < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(day), day, "游戏日从 1 起算。");
            }

            if ((uint)hour >= HoursPerDay)
            {
                throw new ArgumentOutOfRangeException(nameof(hour), hour, "游戏时必须在 0–23。");
            }

            Day = day;
            Hour = hour;
        }

        /// <summary>第 1 游戏日第 0 时。</summary>
        public static GameDateTime Origin => new GameDateTime(1, 0);

        /// <summary>从开局起累计的游戏时。开局为 0。</summary>
        public long TotalHours => ((long)(Day - 1) * HoursPerDay) + Hour;

        public GameDateTime AddHours(int hours)
        {
            if (hours < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hours), hours, "只能向前推进游戏时。");
            }

            long absolute = ((long)(Day - 1) * HoursPerDay) + Hour + hours;
            long dayIndex = absolute / HoursPerDay;
            if (dayIndex > int.MaxValue - 1)
            {
                throw new OverflowException("游戏日超出 int 范围。");
            }

            return new GameDateTime((int)dayIndex + 1, (int)(absolute % HoursPerDay));
        }

        public bool Equals(GameDateTime other) => Day == other.Day && Hour == other.Hour;

        public override bool Equals(object? obj) => obj is GameDateTime other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Day, Hour);

        public static bool operator ==(GameDateTime left, GameDateTime right) => left.Equals(right);

        public static bool operator !=(GameDateTime left, GameDateTime right) => !left.Equals(right);

        public override string ToString() => $"D{Day} {Hour:00}:00";
    }
}
