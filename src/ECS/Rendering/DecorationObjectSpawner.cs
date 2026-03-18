using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Спавнит декоративные объекты (бочки, ящики, мешки, камни) как ECS-сущности с блокировкой тайла.
/// </summary>
public static class DecorationObjectSpawner
{
    // Определения объектов: (тип, srcX, srcY, ширина, высота в px).
    // Координаты определены вручную по atlas_viewer.html — спрайты в Objects.png нестрого по сетке.
    private static readonly (DecorationObjectType Type, int X, int Y, int W, int H)[] _templates =
    {
        (DecorationObjectType.Barrel,      111, 79, 17, 25),
        (DecorationObjectType.Crate,       176, 79, 15, 23),
        (DecorationObjectType.CrateStack,  335, 47, 32, 32),
    };

    public static void SpawnObjects(World world, TileMap map, GameConfig config, int floor, int seed,
        HashSet<(int, int)>? occupiedTiles = null)
    {
        var rng = new Random(seed ^ (floor * 7_919) ^ 0xDEC0);
        int ts = config.ScaledTileSize;
        int chancePercent = config.DecoObjectChancePercent;

        for (int i = 0; i < map.Rooms.Count; i++)
        {
            var room = map.Rooms[i];
            if (room.Type == RoomType.Spawn) continue;

            for (int x = room.X + 1; x < room.X + room.Width - 1; x++)
            {
                for (int y = room.Y + 1; y < room.Y + room.Height - 1; y++)
                {
                    if (!map.InBounds(x, y)) continue;
                    if (map.Tiles[x, y].Type != TileType.Floor) continue;
                    if (!map.Tiles[x, y].Walkable) continue;
                    if (map.Decorations[x, y] != DecorationType.None) continue;
                    if (occupiedTiles != null && occupiedTiles.Contains((x, y))) continue;

                    bool nearWall = IsAdjacentToWall(map, x, y);
                    int effectiveChance = nearWall ? chancePercent * 2 : chancePercent;

                    if (rng.Next(100) >= effectiveChance) continue;

                    var template = _templates[rng.Next(_templates.Length)];

                    int entityId = world.CreateEntity();
                    world.Add(entityId, new Position(x * ts, y * ts));
                    world.Add(entityId, new DecorationObject
                    {
                        Type = template.Type,
                        SrcX = template.X,
                        SrcY = template.Y,
                        SrcWidth = template.W,
                        SrcHeight = template.H,
                    });

                    // Блокируем тайл только для явно массивных объектов.
                    bool blocksTile = template.Type is DecorationObjectType.Crate
                        or DecorationObjectType.CrateStack;
                    if (blocksTile)
                        map.Tiles[x, y].Walkable = false;
                }
            }
        }
    }

    private static bool IsAdjacentToWall(TileMap map, int x, int y)
    {
        return (map.InBounds(x - 1, y) && map.Tiles[x - 1, y].Type == TileType.Wall) ||
               (map.InBounds(x + 1, y) && map.Tiles[x + 1, y].Type == TileType.Wall) ||
               (map.InBounds(x, y - 1) && map.Tiles[x, y - 1].Type == TileType.Wall) ||
               (map.InBounds(x, y + 1) && map.Tiles[x, y + 1].Type == TileType.Wall);
    }
}
