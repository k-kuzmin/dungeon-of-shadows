using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;
using DungeonOfShadows.ECS.Rendering;

namespace DungeonOfShadows.ECS.Combat;

/// <summary>
/// Размещает врагов на этаже при генерации.
/// </summary>
public static class EnemySpawner
{
    public static void SpawnEnemies(World world, TileMap map, GameConfig config, int floor, int seed)
    {
        var rng = new Random(seed ^ (floor * 7_919) ^ 0xC0BA7);

        foreach (var room in map.Rooms)
        {
            // Spawn-комната безопасна
            if (room.Type == RoomType.Spawn) continue;

            int count = rng.Next(config.EnemiesPerRoomMin, config.EnemiesPerRoomMax + 1);

            for (int i = 0; i < count; i++)
            {
                var type = EnemyRegistry.PickType(floor, rng);
                var template = EnemyRegistry.Get(type);

                // Случайная позиция внутри комнаты (с отступом 1 от стен)
                int tx = rng.Next(room.X + 1, room.X + room.Width - 1);
                int ty = rng.Next(room.Y + 1, room.Y + room.Height - 1);

                if (!map.IsWalkable(tx, ty)) continue;

                SpawnEnemy(world, template, tx, ty, floor, config);
            }
        }
    }

    private static void SpawnEnemy(World world, EnemyTemplate template, int tx, int ty,
        int floor, GameConfig config)
    {
        int ts = config.ScaledTileSize;
        float scale = config.EnemyStatFloorScale;

        int id = world.CreateEntity();
        float worldX = tx * ts;
        float worldY = ty * ts;

        world.Add(id, new Position(worldX, worldY));
        world.Add(id, new Velocity(0, 0));
        world.Add(id, new Sprite(template.Tint));
        world.Add(id, new Collider(ts * 0.8f, ts * 0.8f, ts * 0.1f, ts * 0.1f));

        int hp = DamageCalculator.ScaleStat(template.BaseHP, floor, scale);
        world.Add(id, new Health(hp, hp));

        int atk = DamageCalculator.ScaleStat(template.BaseATK, floor, scale);
        int def = DamageCalculator.ScaleStat(template.BaseDEF, floor, scale);
        world.Add(id, new Stats(atk, def, template.Speed, 0.02f));

        world.Add(id, new EnemyTag
        {
            Type = template.Type,
            State = AiState.Idle,
            DetectionRadius = template.DetectionRadius,
            AttackRange = template.AttackRange,
            AttackCooldown = template.AttackCooldown,
            AttackCooldownMax = template.AttackCooldown,
            PathUpdateTimer = 0,
            PatrolTargetTX = -1,
            PatrolTargetTY = -1,
            PatrolWaitTimer = 0,
            BehaviorTimer = 0,
            StrafeDir = 1,
            AttackWindupTimer = 0
        });

        // Анимация врага
        var clips = CharacterAnimationBuilder.BuildEnemy(template.SpritePrefix, config);
        var initialClip = clips["Idle_Down"];
        world.Add(id, new Animator(clips, "Idle_Down"));
        world.Add(id, new Animation(initialClip));
    }
}
