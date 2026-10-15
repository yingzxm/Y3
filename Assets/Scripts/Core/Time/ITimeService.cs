#nullable enable

namespace SocialEvolution.Core
{
    /// <summary>
    /// 游戏时钟。不依赖 UI，也不引用引擎类型。
    /// 1 游戏日 = 24 游戏时。播放模式走 <see cref="AdvanceRealtime"/>；
    /// 测试和脚本走 <see cref="StepHours"/>。
    /// </summary>
    public interface ITimeService : ISimStepHooks
    {
        GameDateTime Now { get; }

        int Day { get; }

        int Hour { get; }

        bool IsPaused { get; }

        /// <summary>当前倍率。暂停期间保持原倍率。</summary>
        float TimeScale { get; }

        /// <summary>1x 下，多少现实秒推进 1 个游戏时。</summary>
        float SecondsPerGameHour { get; }

        void Pause();

        void Resume();

        /// <summary>只接受有限正数。0、负数、NaN、无穷大都会拒绝，且不改变当前倍率。</summary>
        void SetTimeScale(float scale);

        /// <summary>
        /// 按现实秒推进。暂停、或秒数为 0 时返回 0。
        /// 返回本帧实际跨过的游戏时数量。
        /// </summary>
        int AdvanceRealtime(float deltaSeconds);

        /// <summary>
        /// 直接推进若干游戏时，不受暂停和倍率影响。
        /// 每个游戏时依次触发小时回调；跨日时在该小时回调之后触发日回调。
        /// 回调里再次推进会抛出，避免重入。
        /// </summary>
        int StepHours(int gameHours);
    }
}
