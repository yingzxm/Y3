#nullable enable
using SocialEvolution.Core;
using UnityEngine;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 把 Unity 帧时间送进 <see cref="ITimeService"/>。播放模式才会走 <see cref="Update"/>。
    /// 代理人不在这里每帧决策。统计面板若在，会在 <c>Start</c> 里确保小时驱动。
    /// 地图姿态由 <see cref="CitizenMapPoseHook"/> 发布，不依赖面板是否在场景里。见 docs/agents.md。
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
