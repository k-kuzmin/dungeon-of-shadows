using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;
using DungeonOfShadows.ECS.Rendering;

namespace DungeonOfShadows.ECS.Items;

public static class ChestSpawner
{
    public static void SpawnChests(World world, TileMap map, GameConfig config, int floor, int seed,
        AnimatorDatabase animDb)
    {
        // Строим клипы один раз (разделяются между всеми сундуками этажа)
        if (!animDb.TryGet("chest", out var chestDef)) return;
        var chestClips = AnimatorFactory.Build(chestDef);
        if (!chestClips.TryGetValue(chestDef.InitialClip, out var chestInitClip)) return;

        var rng = new Random(seed ^ (floor * 11_131) ^ 0x11CED);
        int target = rng.Next(config.ChestsPerFloorMin, config.ChestsPerFloorMax + 1);
        int spawned = 0;

        for (int i = 0; i < map.Rooms.Count && spawned < target; i++)
        {
            var room = map.Rooms[i];
            if (room.Type == RoomType.Spawn || room.Type == RoomType.StairDown) continue;

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

            world.Add(id, new Animator(chestClips, chestDef.InitialClip));
            world.Add(id, new Animation(chestInitClip));

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
