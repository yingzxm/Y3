#nullable enable
using SocialEvolution.Domain;

namespace SocialEvolution.Agents
{
    /// <summary>
    /// 演示用地图格。仓库里还没有 Construction / DemoMap 的格子，
    /// 市民户外位置先用这套整数格，和以后的施工格同一套：城心是 (16, 16)，原点在西南。
    /// 没有寻路，也没有随机数。按 <see cref="CitizenId"/> 从城心附近铺开，进楼不改格子。
    /// 市民 1 在 (<see cref="OriginX"/>, <see cref="OriginY"/>)，之后每行 <see cref="Columns"/> 人。
    /// 120 人占 (10, 11) 到 (21, 20)，城心落在这片里。
    /// </summary>
    public static class PersonMapCells
    {
        public const int CityCenterX = 16;

        public const int CityCenterY = 16;

        public const int Columns = 12;

        public const int OriginX = 10;

        public const int OriginY = 11;

        public static void OutdoorCell(CitizenId citizenId, out int cellX, out int cellY)
        {
            int slot = citizenId.Value - 1;
            cellX = OriginX + (slot % Columns);
            cellY = OriginY + (slot / Columns);
        }
    }

    /// <summary>
    /// 地图格到世界坐标。施工 DemoMap 还没有自己的换算时，地图层用这一套：
    /// 地面是 XZ，格大小 <see cref="CellSize"/>，坐标取格心而不是格角。
    /// cell (x, y) → world ((x + 0.5) * CellSize, 0, (y + 0.5) * CellSize)。
    /// 市民 1 的格子 (10, 11) 对应世界 (10.5, 0, 11.5)。
    /// </summary>
    public static class DemoMapCellSpace
    {
        public const float CellSize = 1f;

        public static void ToWorld(int cellX, int cellY, out float worldX, out float worldY, out float worldZ)
        {
            worldX = (cellX + 0.5f) * CellSize;
            worldY = 0f;
            worldZ = (cellY + 0.5f) * CellSize;
        }
    }
}
