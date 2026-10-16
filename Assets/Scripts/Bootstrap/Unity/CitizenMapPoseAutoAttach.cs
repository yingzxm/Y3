#nullable enable
using System;
using SocialEvolution.Agents;
using UnityEngine;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 用已经挂到时钟上的 <see cref="AgentsHourDriver"/> 发布地图姿态，
    /// 并写进场景里每个 <see cref="CitizenMapVisualController.PoseSource"/>。
    /// 不另建市民，也不画精灵。
    /// </summary>
    public static class CitizenMapPoseAutoAttach
    {
        /// <summary>最近一次 <see cref="Apply"/> 的适配器。控制器若来得更晚，读这个即可。</summary>
        public static CitizenMapPoseSource? Current { get; private set; }

        public static CitizenMapPoseSource Apply(GameSession session, AgentsHourDriver agents)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            IPersonMapPresence presence = session.AttachMapPresence(agents);
            var source = new CitizenMapPoseSource(presence);
            Current = source;
            CitizenMapVisualController[] controllers = Object.FindObjectsByType<CitizenMapVisualController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                controllers[i].PoseSource = source;
            }

            return source;
        }
    }
}
