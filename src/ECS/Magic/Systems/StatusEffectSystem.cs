using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Combat;
using DungeonOfShadows.ECS.Magic.Components;

namespace DungeonOfShadows.ECS.Magic.Systems;

/// <summary>
/// Тикает статус-эффекты: Burn → DamageEvents, Slow → SlowDebuff, снятие истёкших.
/// </summary>
public class StatusEffectSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly List<int> _buffer = new();
    private readonly List<int> _removeBuffer = new();

    public StatusEffectSystem(GameContext ctx, World world, GameConfig config)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _world;
        int ts = _config.ScaledTileSize;
        _removeBuffer.Clear();

        world.QueryInto<StatusEffects>(_buffer);
        for (int i = 0; i < _buffer.Count; i++)
        {
            int id = _buffer[i];
            if (!world.IsAlive(id)) continue;

            ref var effects = ref world.Get<StatusEffects>(id);
            bool hasSlow = false;
            float worstSlow = 1f;

            for (int s = effects.ActiveCount - 1; s >= 0; s--)
            {
                var slot = effects.GetSlot(s);

                slot.Duration -= dt;
                if (slot.Duration <= 0)
                {
                    MagicHelper.RemoveEffectAt(ref effects, s);
                    continue;
                }

                if (slot.Type == SpellEffectType.Burn)
                {
                    slot.TickAccumulator += dt;
                    if (slot.TickAccumulator >= _config.BurnTickInterval)
                    {
                        slot.TickAccumulator -= _config.BurnTickInterval;

                        float wx = 0, wy = 0;
                        if (world.Has<Position>(id))
                        {
                            ref var pos = ref world.Get<Position>(id);
                            wx = pos.X + ts / 2f;
                            wy = pos.Y + ts / 2f;
                        }

                        _ctx.DamageEvents.Add(new DamageEvent(-1, id, slot.DamagePerTick, false, wx, wy));
                    }
                }

                if (slot.Type == SpellEffectType.Slow)
                {
                    hasSlow = true;
                    worstSlow = MathF.Min(worstSlow, slot.SlowFactor);
                }

                effects.SetSlot(s, slot);
            }

            // Управление SlowDebuff компонентом
            if (hasSlow)
            {
                if (world.Has<SlowDebuff>(id))
                {
                    ref var debuff = ref world.Get<SlowDebuff>(id);
                    debuff.Factor = worstSlow;
                }
                else
                {
                    world.Add(id, new SlowDebuff(worstSlow));
                }
            }
            else if (world.Has<SlowDebuff>(id))
            {
                world.Remove<SlowDebuff>(id);
            }

            // Если все эффекты истекли — удалить компонент
            if (effects.ActiveCount == 0)
                _removeBuffer.Add(id);
        }

        for (int i = 0; i < _removeBuffer.Count; i++)
        {
            int id = _removeBuffer[i];
            if (world.IsAlive(id) && world.Has<StatusEffects>(id))
                world.Remove<StatusEffects>(id);
        }
    }
}
