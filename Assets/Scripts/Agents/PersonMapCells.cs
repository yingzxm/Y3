#nullable enable
using SocialEvolution.Domain;

namespace SocialEvolution.Agents
{
    /// <summary>
    /// 演示用地图格，和 DemoMap 的 Tilemap 格同一套整数坐标。城心是 (16, 16)。
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
    /// DemoMap 是 Unity 2D Tilemap，地面是 XY。Grid 在 (-20, -12, 0)，cellSize (1, 1, 0)，地图 40×24。
    /// 与 <c>Grid.GetCellCenterWorld</c> 相同：cell (x, y) → world (x - 19.5, y - 11.5, 0)。
    /// 市民 1 的格子 (10, 11) 对应世界 (-9.5, -0.5, 0)。
    /// </summary>
    public static class DemoMapCellSpace
    {
        public const float CellSizeX = 1f;

        public const float CellSizeY = 1f;

        public const float MapOriginX = -20f;

        public const float MapOriginY = -12f;

        public const int MapWidth = 40;

        public const int MapHeight = 24;

        public static void ToWorld(int cellX, int cellY, out float worldX, out float worldY, out float worldZ)
        {
            worldX = MapOriginX + ((cellX + 0.5f) * CellSizeX);
            worldY = MapOriginY + ((cellY + 0.5f) * CellSizeY);
            worldZ = 0f;
        }
    }
}
