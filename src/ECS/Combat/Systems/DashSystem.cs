using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Combat.Systems;

/// <summary>
/// Обрабатывает дэш: переопределяет скорость, спавнит afterimage, управляет кулдауном.
/// </summary>
public class DashSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly List<int> _dashBuffer = new();
    private readonly List<int> _cooldownBuffer = new();
    private readonly List<int> _invBuffer = new();
    private readonly List<int> _afterBuffer = new();

    public DashSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _ctx.World;
        var config = _ctx.Config;

        // Активные дэши
        world.QueryInto<DashState, Velocity>(_dashBuffer);
        foreach (int id in _dashBuffer)
        {
            ref var dash = ref world.Get<DashState>(id);
            ref var vel = ref world.Get<Velocity>(id);

            // Переопределяем скорость
            vel.X = dash.DirX * dash.Speed;
            vel.Y = dash.DirY * dash.Speed;

            // Спавн afterimage
            dash.AfterimageTimer -= dt;
            if (dash.AfterimageTimer <= 0 && world.Has<Position>(id) && world.Has<Sprite>(id))
            {
                dash.AfterimageTimer = config.AfterimageSpawnInterval;
                ref var pos = ref world.Get<Position>(id);
                ref var sprite = ref world.Get<Sprite>(id);

                int ghostId = world.CreateEntity();
                world.Add(ghostId, new Position(pos.X, pos.Y));
                world.Add(ghostId, new Sprite(sprite.Tint, sprite.Width, sprite.Height));
                world.Add(ghostId, new AfterimageParticle
                {
                    Alpha = config.AfterimageStartAlpha,
                    TimeRemaining = config.AfterimageLifetime,
                    Lifetime = config.AfterimageLifetime
                });
            }

            dash.DurationRemaining -= dt;
            if (dash.DurationRemaining <= 0)
            {
                world.Remove<DashState>(id);
                world.Add(id, new DashCooldown(config.PlayerDashCooldown));
            }
        }

        // Тикаем кулдаун дэша
        world.QueryInto<DashCooldown>(_cooldownBuffer);
        foreach (int id in _cooldownBuffer)
        {
            ref var cd = ref world.Get<DashCooldown>(id);
            cd.TimeRemaining -= dt;
            if (cd.TimeRemaining <= 0)
                world.Remove<DashCooldown>(id);
        }

        // Тикаем неуязвимость
        world.QueryInto<Invincible>(_invBuffer);
        foreach (int id in _invBuffer)
        {
            ref var inv = ref world.Get<Invincible>(id);
            inv.TimeRemaining -= dt;
            if (inv.TimeRemaining <= 0)
                world.Remove<Invincible>(id);
        }

        // Тикаем afterimage частицы
        world.QueryInto<AfterimageParticle>(_afterBuffer);
        foreach (int id in _afterBuffer)
        {
            ref var after = ref world.Get<AfterimageParticle>(id);
            after.TimeRemaining -= dt;
            after.Alpha = Math.Max(0, after.TimeRemaining / after.Lifetime * config.AfterimageStartAlpha);
            if (after.TimeRemaining <= 0)
                world.DestroyEntity(id);
        }
    }
}
