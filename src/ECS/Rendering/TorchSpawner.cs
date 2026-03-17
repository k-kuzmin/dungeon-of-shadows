using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Спавнит факелы как ECS-сущности с Animation.
/// Читает DecorationType.Torch из TileMap.Decorations.
/// </summary>
public static class TorchSpawner
{
    public static void SpawnTorches(World world, TileMap map, GameConfig config)
    {
        int ts = config.ScaledTileSize;

        // Один клип на все факелы этажа (разделяемый, AnimationClip — reference type)
        var frames = new Raylib_cs.Rectangle[config.TorchFrameCount];
        for (int i = 0; i < config.TorchFrameCount; i++)
            frames[i] = TileAtlas.GetTorchSource((byte)i);

        var clip = new AnimationClip("fire_large", frames, config.TorchFrameDuration, loop: true);

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
