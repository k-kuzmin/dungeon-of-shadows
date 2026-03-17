using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Items;

public static class ChestSpawner
{
    public static void SpawnChests(World world, TileMap map, GameConfig config, int floor, int seed)
    {
        var rng = new Random(seed ^ (floor * 11_131) ^ 0x11CED);
        int target = rng.Next(config.ChestsPerFloorMin, config.ChestsPerFloorMax + 1);
        int spawned = 0;

        for (int i = 0; i < map.Rooms.Count && spawned < target; i++)
        {
            var room = map.Rooms[i];
            if (room.Type == RoomType.Spawn) continue;

            if (rng.NextDouble() > 0.5)
                continue;

            int id = world.CreateEntity();
            int ts = config.ScaledTileSize;
            float x = room.CenterX * ts;
            float y = room.CenterY * ts;

            world.Add(id, new Position(x, y));
            world.Add(id, new Chest
            {
                Opened = false,
                GuaranteedDefinitionId = PickGuaranteedLootId(rng, floor)
            });

            spawned++;
        }
    }

    private static int PickGuaranteedLootId(Random rng, int floor)
    {
        if (floor <= 2)
            return rng.Next(2) == 0 ? 200 : 100;

        if (floor <= 5)
            return rng.Next(3) switch
            {
                0 => 200,
                1 => 101,
                _ => 300
            };

        return rng.Next(4) switch
        {
            0 => 200,
            1 => 101,
            2 => 102,
            _ => 300
        };
    }
}
