using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Combat.Systems;

/// <summary>
/// Тикает числа урона: движение вверх, затухание, удаление по истечении.
/// </summary>
public class DamageNumberSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly List<int> _buffer = new();

    public DamageNumberSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        var world = _ctx.World;
        var config = _ctx.Config;

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
