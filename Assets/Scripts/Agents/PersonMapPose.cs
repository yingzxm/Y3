#nullable enable
using SocialEvolution.Domain;

namespace SocialEvolution.Agents
{
    /// <summary><see cref="IPersonMapPose"/> 的一次读取结果。格子和进楼状态都在读的那一刻定下来。</summary>
    public sealed class PersonMapPose : IPersonMapPose
    {
        public PersonMapPose(CitizenId citizenId, int cellX, int cellY, bool inBuilding)
        {
            CitizenId = citizenId;
            CellX = cellX;
            CellY = cellY;
            InBuilding = inBuilding;
        }

        public CitizenId CitizenId { get; }

        public int CellX { get; }

        public int CellY { get; }

        public bool InBuilding { get; }
    }
}
