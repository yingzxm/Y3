#nullable enable
using SocialEvolution.Domain;

namespace SocialEvolution.Agents
{
    /// <summary>
    /// 一位市民当前的地图姿态。格子是整数地图格，进楼上班时 <see cref="InBuilding"/> 为 true。
    /// 地图应在 <see cref="InBuilding"/> 为 true 时隐藏此人，否则画在 <see cref="CellX"/> / <see cref="CellY"/>。
    /// </summary>
    public interface IPersonMapPose
    {
        CitizenId CitizenId { get; }

        int CellX { get; }

        int CellY { get; }

        /// <summary>只在进楼上班时为 true。户外、通勤、闲置、失业都是 false。</summary>
        bool InBuilding { get; }
    }
}
