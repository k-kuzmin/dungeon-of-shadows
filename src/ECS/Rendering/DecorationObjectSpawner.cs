using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Спавнит декоративные объекты (бочки, ящики, мешки, камни) как ECS-сущности с блокировкой тайла.
/// </summary>
public static class DecorationObjectSpawner
{
    // Определения объектов: (тип, col в Objects.png, row, ширина, высота в px)
    private static readonly (DecorationObjectType Type, int Col, int Row, int W, int H)[] _templates =
    {
        (DecorationObjectType.Barrel,      4, 2, 16, 16),
        (DecorationObjectType.Barrel,      5, 2, 16, 16),
        (DecorationObjectType.BarrelBroken, 6, 2, 16, 16),
        (DecorationObjectType.Crate,       14, 0, 16, 16),
        (DecorationObjectType.Crate,       14, 2, 16, 16),
        (DecorationObjectType.CrateStack,  15, 0, 32, 32),
        (DecorationObjectType.Sack,        19, 2, 16, 16),
        (DecorationObjectType.Sack,        20, 2, 16, 16),
        (DecorationObjectType.Rock,        8, 0, 16, 16),
        (DecorationObjectType.Rock,        9, 0, 16, 16),
        (DecorationObjectType.RockSmall,   10, 0, 16, 16),
        (DecorationObjectType.RockSmall,   11, 0, 16, 16),
    };

    public static void SpawnObjects(World world, TileMap map, GameConfig config, int floor, int seed)
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

                    bool nearWall = IsAdjacentToWall(map, x, y);
                    int effectiveChance = nearWall ? chancePercent * 2 : chancePercent;

                    if (rng.Next(100) >= effectiveChance) continue;

                    var template = _templates[rng.Next(_templates.Length)];

                    int entityId = world.CreateEntity();
                    world.Add(entityId, new Position(x * ts, y * ts));
                    world.Add(entityId, new DecorationObject
                    {
                        Type = template.Type,
                        AtlasCol = template.Col,
                        AtlasRow = template.Row,
                        SrcWidth = template.W,
                        SrcHeight = template.H,
                    });

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
