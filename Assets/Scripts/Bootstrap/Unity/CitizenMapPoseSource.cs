#nullable enable
using System;
using SocialEvolution.Agents;
using SocialEvolution.Domain;
using UnityEngine;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 把 <see cref="IPersonMapPresence"/> 的地图格换成 DemoMap 的 XY 世界坐标。
    /// 场景里有放在 (-20, -12, 0) 的 Grid 时，用 <c>GetCellCenterWorld</c>，否则用 <see cref="DemoMapCellSpace"/>。
    /// 不保存第二份进楼状态，也不生成精灵。
    /// </summary>
    public sealed class CitizenMapPoseSource : ICitizenMapPoseSource
    {
        private readonly IPersonMapPresence _presence;
        private Grid? _grid;

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

            world = CellToWorld(pose.CellX, pose.CellY);
            inBuilding = pose.InBuilding;
            return true;
        }

        private Vector3 CellToWorld(int cellX, int cellY)
        {
            Grid? grid = DemoGrid();
            if (grid != null)
            {
                return grid.GetCellCenterWorld(new Vector3Int(cellX, cellY, 0));
            }

            float x;
            float y;
            float z;
            DemoMapCellSpace.ToWorld(cellX, cellY, out x, out y, out z);
            return new Vector3(x, y, z);
        }

        private Grid? DemoGrid()
        {
            if (_grid != null)
            {
                return _grid;
            }

            Grid[] grids = Object.FindObjectsByType<Grid>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < grids.Length; i++)
            {
                Vector3 position = grids[i].transform.position;
                if (Mathf.Abs(position.x - DemoMapCellSpace.MapOriginX) < 0.01f
                    && Mathf.Abs(position.y - DemoMapCellSpace.MapOriginY) < 0.01f)
                {
                    _grid = grids[i];
                    return _grid;
                }
            }

            return null;
        }
    }
}
