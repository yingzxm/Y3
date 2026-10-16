#nullable enable
using System;
using System.Reflection;
using SocialEvolution.Agents;
using UnityEngine;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 用已经挂到时钟上的 <see cref="AgentsHourDriver"/> 发布地图姿态，
    /// 并写进场景里每个 CitizenMapVisualController.PoseSource。
    /// 不替换那个控制器，不另建市民，也不画精灵。
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
            AssignControllers(source);
            return source;
        }

        private static void AssignControllers(CitizenMapPoseSource source)
        {
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour.GetType().Name != "CitizenMapVisualController")
                {
                    continue;
                }

                PropertyInfo? property = behaviour.GetType().GetProperty("PoseSource");
                if (property != null && property.CanWrite)
                {
                    property.SetValue(behaviour, source);
                }
            }
        }
    }
}
