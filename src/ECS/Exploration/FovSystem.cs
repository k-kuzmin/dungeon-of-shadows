using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Exploration.Systems;

/// <summary>
/// Система тумана войны. Bresenham raycasting.
/// Обновляется при смене тайла игрока. Детектит смену карты автоматически.
/// </summary>
public class FovSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly List<int> _queryBuffer = new();

    private int _lastTileX = -1;
    private int _lastTileY = -1;
    private TileMap? _lastMap;

    public FovSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _ctx.World;
        var map = _ctx.Map;
        int ts = _ctx.Config.ScaledTileSize;

        // Детектим смену карты (переход на новый этаж)
        if (map != _lastMap)
        {
            _lastMap = map;
            _lastTileX = -1;
            _lastTileY = -1;
        }

        world.QueryInto<PlayerTag, Position>(_queryBuffer);
        if (_queryBuffer.Count == 0) return;

        ref var pos = ref world.Get<Position>(_queryBuffer[0]);

        int playerTX = (int)((pos.X + ts / 2f) / ts);
        int playerTY = (int)((pos.Y + ts / 2f) / ts);

        if (playerTX == _lastTileX && playerTY == _lastTileY) return;
        _lastTileX = playerTX;
        _lastTileY = playerTY;

        RecalculateFov(map, playerTX, playerTY);
    }

    private void RecalculateFov(TileMap map, int originX, int originY)
    {
        int radius = _ctx.Config.FovRadius;
        int rayCount = _ctx.Config.FovRayCount;

        // Понижаем Visible (2) до Explored (1)
        for (int x = 0; x < map.Width; x++)
            for (int y = 0; y < map.Height; y++)
                if (map.Tiles[x, y].Visibility == 2)
                    map.Tiles[x, y].Visibility = 1;

        // Бросаем лучи
        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * (2f * MathF.PI / rayCount);
            CastRay(map, originX, originY, MathF.Cos(angle), MathF.Sin(angle), radius);
        }

        // Пост-проход: стена, смежная с видимым полом, тоже видима
        RevealAdjacentWalls(map, originX, originY, radius);
    }

    private static void RevealAdjacentWalls(TileMap map, int ox, int oy, int radius)
    {
        int minX = Math.Max(0, ox - radius - 1);
        int minY = Math.Max(0, oy - radius - 1);
        int maxX = Math.Min(map.Width - 1, ox + radius + 1);
        int maxY = Math.Min(map.Height - 1, oy + radius + 1);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (map.Tiles[x, y].Type != TileType.Wall) continue;
                if (map.Tiles[x, y].Visibility == 2) continue;

                if (HasVisibleFloorNeighbor(map, x, y))
                    map.Tiles[x, y].Visibility = 2;
            }
        }
    }

    private static bool HasVisibleFloorNeighbor(TileMap map, int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (!map.InBounds(nx, ny)) continue;
                ref var neighbor = ref map.Tiles[nx, ny];
                if (neighbor.Visibility == 2 && neighbor.Type != TileType.Wall)
                    return true;
            }
        }
        return false;
    }

    private static void CastRay(TileMap map, int ox, int oy, float dirX, float dirY, int radius)
    {
        int x1 = (int)MathF.Round(ox + dirX * radius);
        int y1 = (int)MathF.Round(oy + dirY * radius);

        int dx = Math.Abs(x1 - ox);
        int dy = Math.Abs(y1 - oy);
        int sx = ox < x1 ? 1 : -1;
        int sy = oy < y1 ? 1 : -1;
        int err = dx - dy;

        int cx = ox, cy = oy;

        for (int step = 0; step <= radius; step++)
        {
            if (!map.InBounds(cx, cy)) break;

            map.Tiles[cx, cy].Visibility = 2;

            if (map.Tiles[cx, cy].Type == TileType.Wall)
                break;

            if (cx == x1 && cy == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; cx += sx; }
            if (e2 < dx) { err += dx; cy += sy; }
        }
    }
}
