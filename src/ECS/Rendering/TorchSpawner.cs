using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Спавнит факелы как ECS-сущности с Animation.
/// Читает DecorationType.Torch из TileMap.Decorations.
/// </summary>
public static class TorchSpawner
{
    public static void SpawnTorches(World world, TileMap map, GameConfig config, AnimatorDatabase animDb)
    {
        int ts = config.ScaledTileSize;

        // Один клип на все факелы этажа (разделяемый, AnimationClip — reference type)
        AnimationClip? clip = null;
        if (animDb.TryGet("torch", out var torchDef))
            clip = AnimatorFactory.BuildSingleClip(torchDef);

        if (clip == null) return;

        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                if (map.Decorations[x, y] != DecorationType.Torch) continue;

                int id = world.CreateEntity();
                world.Add(id, new Position(x * ts, y * ts));
                world.Add(id, new TorchTag());
                world.Add(id, new Animation(clip));
            }
        }
    }
}
