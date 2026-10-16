#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Core;
using SocialEvolution.Domain;

namespace SocialEvolution.Agents
{
    /// <summary>
    /// 全体市民的地图姿态。轮询 <see cref="Poses"/> 或 <see cref="TryGet"/> 即可，
    /// 数据从已有的 <see cref="AgentBrain"/> 读出，不另建市民管理器。
    /// 要跟着游戏时刷新，先 <see cref="FollowHours"/>，再听 <see cref="Changed"/>。
    /// 每个游戏时、行动执行完之后触发一次。开局时刻不触发。不订阅也可以直接轮询。
    /// </summary>
    public interface IPersonMapPresence
    {
        int Count { get; }

        IReadOnlyList<IPersonMapPose> Poses { get; }

        bool TryGet(CitizenId citizenId, out IPersonMapPose? pose);

        event Action<GameDateTime>? Changed;

        void FollowHours();

        void UnfollowHours();
    }
}
