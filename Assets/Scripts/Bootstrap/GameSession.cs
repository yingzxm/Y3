#nullable enable
using System;
using SocialEvolution.Core;
using SocialEvolution.World;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 一局游戏的时钟和世界。创建时挂上空的小时/日回调，不跑经济。
    /// </summary>
    public sealed class GameSession
    {
        public GameSession(ITimeService time, WorldState world)
        {
            Time = time ?? throw new ArgumentNullException(nameof(time));
            World = world ?? throw new ArgumentNullException(nameof(world));
        }

        public ITimeService Time { get; }

        public WorldState World { get; }

        public static GameSession CreateNew()
        {
            var time = new TimeService();
            time.RegisterGameHour(SimNoOp.OnGameHour);
            time.RegisterGameDay(SimNoOp.OnGameDay);
            return new GameSession(time, OpeningSeed.Create());
        }
    }
}
