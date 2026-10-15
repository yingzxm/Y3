#nullable enable
using System;
using SocialEvolution.Core;
using SocialEvolution.Economy;
using SocialEvolution.World;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 一局游戏的时钟、世界和市场。
    /// <see cref="CreateNew"/> 把 <see cref="CityMarket.OnGameHour"/> 注册到小时钩子。
    /// </summary>
    public sealed class GameSession
    {
        public GameSession(ITimeService time, WorldState world)
            : this(time, world, null)
        {
        }

        public GameSession(ITimeService time, WorldState world, CityMarket? market)
        {
            Time = time ?? throw new ArgumentNullException(nameof(time));
            World = world ?? throw new ArgumentNullException(nameof(world));
            if (market != null && !ReferenceEquals(market.World, world))
            {
                throw new ArgumentException("市场必须使用同一份世界。", nameof(market));
            }

            Market = market;
        }

        public ITimeService Time { get; }

        public WorldState World { get; }

        /// <summary>未接入市场时为空。CreateNew 总会带上。</summary>
        public CityMarket? Market { get; }

        public static GameSession CreateNew()
        {
            var time = new TimeService();
            WorldState world = OpeningSeed.Create();
            var market = new CityMarket(world);
            time.RegisterGameHour(SimNoOp.OnGameHour);
            time.RegisterGameHour(market.OnGameHour);
            time.RegisterGameDay(SimNoOp.OnGameDay);
            return new GameSession(time, world, market);
        }
    }
}
