#nullable enable
using System;

namespace SocialEvolution.Core
{
    /// <summary>
    /// 模拟步进注册点。每推进一个游戏时、每跨过一个游戏日各回调一次。
    /// P0 不在这里做经济、招聘或决策；未注册时什么都不发生。
    /// </summary>
    public interface ISimStepHooks
    {
        /// <summary>进入新的游戏时后调用。同一委托重复注册只保留一份。</summary>
        void RegisterGameHour(Action<GameDateTime> callback);

        void UnregisterGameHour(Action<GameDateTime> callback);

        /// <summary>从 23 时进入下一日 0 时，且该次小时回调之后调用。</summary>
        void RegisterGameDay(Action<GameDateTime> callback);

        void UnregisterGameDay(Action<GameDateTime> callback);
    }
}
