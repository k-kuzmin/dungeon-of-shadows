using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Combat;

namespace DungeonOfShadows.ECS.Items.Systems;

public class ItemDropSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly ItemDatabase _db;
    private Random _rng = new(0);
    private int _seed = int.MinValue;

    public ItemDropSystem(GameContext ctx, ItemDatabase db)
    {
        _ctx = ctx;
        _db = db;
    }

    public void Tick(float dt)
    {
        if (_seed != _ctx.DungeonSeed)
        {
            _seed = _ctx.DungeonSeed;
            _rng = new Random(_seed ^ 0x17E4);
        }

        if (_ctx.ItemDropRequests.Count == 0)
            return;

        var world = _ctx.World;
        var cfg = _ctx.Config;

        for (int i = 0; i < _ctx.ItemDropRequests.Count; i++)
        {
            var req = _ctx.ItemDropRequests[i];
            if (_rng.NextDouble() > GetEnemyDropChance(req.EnemyType, cfg))
                continue;

            if (!_db.TryRollDrop(req.EnemyType, req.Floor, _rng, out var stack))
                continue;

            int id = world.CreateEntity();
            world.Add(id, new Position(req.WorldX, req.WorldY));
            world.Add(id, new ItemOnGround());
            world.Add(id, stack);
        }

        _ctx.ItemDropRequests.Clear();
    }

    private static double GetEnemyDropChance(EnemyType enemyType, GameConfig cfg)
    {
        return enemyType switch
        {
            EnemyType.Rat => cfg.RatDropChance,
            EnemyType.Goblin => cfg.GoblinDropChance,
            EnemyType.Skeleton => cfg.SkeletonDropChance,
            _ => 0.2
        };
    }
}
