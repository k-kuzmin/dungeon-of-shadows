using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Magic.Components;

namespace DungeonOfShadows.ECS.Magic.Systems;

/// <summary>
/// Регенерация маны: 1 MP каждые ManaRegenInterval секунд.
/// </summary>
public class ManaSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly List<int> _buffer = new();

    public ManaSystem(GameContext ctx, World world, GameConfig config)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        _world.QueryInto<Mana>(_buffer);
        for (int i = 0; i < _buffer.Count; i++)
        {
            int id = _buffer[i];
            ref var mana = ref _world.Get<Mana>(id);

            // Регенерация
            if (mana.MP < mana.MaxMP)
            {
                mana.RegenAccumulator += dt;
                if (mana.RegenAccumulator >= _config.ManaRegenInterval)
                {
                    mana.RegenAccumulator -= _config.ManaRegenInterval;
                    mana.MP = Math.Min(mana.MP + 1, mana.MaxMP);
                }
            }
            else
            {
                mana.RegenAccumulator = 0f;
            }
        }
    }
}
