#nullable enable
using System;
using SocialEvolution.Agents;
using SocialEvolution.Core;
using SocialEvolution.Stats;
using SocialEvolution.World;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 一局游戏的时钟和世界。创建时挂上空的小时/日回调，不跑经济。
    /// </summary>
    public sealed class GameSession
    {
        private PersonMapPresence? _mapPresence;
        private DemoReadout? _readout;

        public GameSession(ITimeService time, WorldState world)
        {
            Time = time ?? throw new ArgumentNullException(nameof(time));
            World = world ?? throw new ArgumentNullException(nameof(world));
        }

        public ITimeService Time { get; }

        public WorldState World { get; }

        /// <summary>
        /// 地图只读姿态。没有调用 <see cref="AttachMapPresence"/> 时为空。
        /// 创建会话本身不会建市民大脑。
        /// </summary>
        public IPersonMapPresence? MapPresence => _mapPresence;

        /// <summary>
        /// 为这一局准备唯一的 <see cref="DemoReadout"/> 和小时驱动。再次调用返回同一份，避免重复注册。
        /// <see cref="CreateNew"/> 本身不会调用它。
        /// </summary>
        public DemoReadout EnsureReadout()
        {
            if (_readout != null)
            {
                return _readout;
            }

            _readout = DemoReadout.Attach(Time, World);
            return _readout;
        }

        /// <summary>
        /// 用已经在跑的小时驱动发布地图姿态，不另建一套市民。
        /// 再次调用会解绑上一份监听。
        /// </summary>
        public IPersonMapPresence AttachMapPresence(AgentsHourDriver agents)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            if (_mapPresence != null)
            {
                _mapPresence.UnfollowHours();
            }

            var presence = new PersonMapPresence(agents);
            presence.FollowHours();
            _mapPresence = presence;
            return presence;
        }

        public static GameSession CreateNew()
        {
            var time = new TimeService();
            time.RegisterGameHour(SimNoOp.OnGameHour);
            time.RegisterGameDay(SimNoOp.OnGameDay);
            return new GameSession(time, OpeningSeed.Create());
        }
    }
}
