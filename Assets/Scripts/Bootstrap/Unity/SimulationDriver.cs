#nullable enable
using SocialEvolution.Core;
using UnityEngine;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 把 Unity 帧时间送进 <see cref="ITimeService"/>。播放模式才会走 <see cref="Update"/>。
    /// <see cref="GameSession.CreateNew"/> 已经把市场小时钩子挂上。代理人不在这里每帧决策。
    /// 统计和点选由同一物体上的 <see cref="StatsDebugPanel"/> 在 <c>Start</c> 里注册，
    /// 并把劳动力市场接到产量缩放和现价上。见 docs/stats.md。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationDriver : MonoBehaviour
    {
        public GameSession? Session { get; private set; }

        private void Awake()
        {
            Session = GameSession.CreateNew();
        }

        private void Update()
        {
            if (Session == null)
            {
                return;
            }

            Session.Time.AdvanceRealtime(Time.deltaTime);
        }

        public void Pause()
        {
            if (Session != null)
            {
                Session.Time.Pause();
            }
        }

        public void Resume()
        {
            if (Session != null)
            {
                Session.Time.Resume();
            }
        }

        public void SetTimeScale(float scale)
        {
            if (Session != null)
            {
                Session.Time.SetTimeScale(scale);
            }
        }
    }
}
