using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Combat.Systems;

/// <summary>
/// Тикает числа урона: движение вверх, затухание, удаление по истечении.
/// </summary>
public class DamageNumberSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly List<int> _buffer = new();

    public DamageNumberSystem(GameContext ctx, World world, GameConfig config)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
    }

    public void Tick(float dt)
    {
        var world = _world;
        var config = _config;

        world.QueryInto<DamageNumber>(_buffer);
        foreach (int id in _buffer)
        {
            ref var num = ref world.Get<DamageNumber>(id);
            num.WorldY -= config.DamageNumberSpeed * dt;
            num.TimeRemaining -= dt;

            if (num.TimeRemaining <= 0)
                world.DestroyEntity(id);
        }
    }
}
