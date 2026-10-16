#nullable enable
using System;
using SocialEvolution.Agents;
using SocialEvolution.Domain;
using UnityEngine;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 把 <see cref="IPersonMapPresence"/> 的地图格换成 <see cref="DemoMapCellSpace"/> 的世界坐标，
    /// 实现 <see cref="ICitizenMapPoseSource"/>。不保存第二份进楼状态。
    /// </summary>
    public sealed class CitizenMapPoseSource : ICitizenMapPoseSource
    {
        private readonly IPersonMapPresence _presence;

        public CitizenMapPoseSource(IPersonMapPresence presence)
        {
            if (presence == null)
            {
                throw new ArgumentNullException(nameof(presence));
            }

            _presence = presence;
        }

        public IPersonMapPresence Presence => _presence;

        public bool TryGet(CitizenId id, out Vector3 world, out bool inBuilding)
        {
            IPersonMapPose? pose;
            if (!_presence.TryGet(id, out pose) || pose == null)
            {
                world = default;
                inBuilding = false;
                return false;
            }

            float x;
            float y;
            float z;
            DemoMapCellSpace.ToWorld(pose.CellX, pose.CellY, out x, out y, out z);
            world = new Vector3(x, y, z);
            inBuilding = pose.InBuilding;
            return true;
        }
    }
}
