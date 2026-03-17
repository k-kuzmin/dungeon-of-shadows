using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Items;
using DungeonOfShadows.ECS.Rendering.Systems;

namespace DungeonOfShadows.ECS.Combat.Systems;

/// <summary>
/// Обрабатывает DamageEvents: применяет урон, спавнит DamageFlash/DamageNumber,
/// убивает мёртвых врагов, переключает GameState.Dead при смерти игрока.
/// </summary>
public class HealthSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly CameraSystem _cameraSystem;
    private readonly List<int> _healthBuffer = new();
    private readonly List<int> _flashBuffer = new();
    private readonly List<int> _deathBuffer = new();

    public HealthSystem(GameContext ctx, CameraSystem cameraSystem)
    {
        _ctx = ctx;
        _cameraSystem = cameraSystem;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _ctx.World;
        var config = _ctx.Config;

        // Обрабатываем очередь урона
        foreach (var evt in _ctx.DamageEvents)
        {
            if (!world.IsAlive(evt.TargetId)) continue;

            // Проверка неуязвимости
            if (world.Has<Invincible>(evt.TargetId)) continue;
            if (!world.Has<Health>(evt.TargetId)) continue;

            ref var health = ref world.Get<Health>(evt.TargetId);
            health.HP = Math.Max(0, health.HP - evt.Damage);

            // DamageFlash на цели
            if (world.Has<DamageFlash>(evt.TargetId))
                world.Remove<DamageFlash>(evt.TargetId);
            world.Add(evt.TargetId, new DamageFlash(config.DamageFlashDuration));

            // Спавн числа урона
            int numId = world.CreateEntity();
            world.Add(numId, new DamageNumber
            {
                Value = evt.Damage,
                CachedText = evt.Damage.ToString(),
                IsCrit = evt.IsCrit,
                WorldX = evt.WorldX,
                WorldY = evt.WorldY - 20f, // чуть выше точки удара
                TimeRemaining = config.DamageNumberLifetime,
                Lifetime = config.DamageNumberLifetime
            });

            // Screen shake при уроне по игроку
            if (world.Has<PlayerTag>(evt.TargetId))
                _cameraSystem.TriggerShake();
        }
        _ctx.DamageEvents.Clear();

        // Проверяем смерти
        world.QueryInto<Health>(_healthBuffer);
        foreach (int id in _healthBuffer)
        {
            ref var health = ref world.Get<Health>(id);
            if (health.HP > 0) continue;

            if (world.Has<PlayerTag>(id))
            {
                _ctx.State = GameState.Dead;
            }
            else
            {
                if (!world.Has<EnemyDeathState>(id))
                {
                    if (world.Has<EnemyTag>(id) && world.Has<Position>(id))
                    {
                        ref var enemy = ref world.Get<EnemyTag>(id);
                        ref var pos = ref world.Get<Position>(id);
                        _ctx.ItemDropRequests.Add(new ItemDropRequest(
                            enemy.Type,
                            pos.X,
                            pos.Y,
                            _ctx.CurrentFloor));
                    }

                    world.Add(id, new EnemyDeathState(config.EnemyDeathAnimationDuration));

                    // Отключаем боевую и физическую активность, оставляем Sprite+Position для анимации смерти.
                    if (world.Has<EnemyTag>(id)) world.Remove<EnemyTag>(id);
                    if (world.Has<Velocity>(id)) world.Remove<Velocity>(id);
                    if (world.Has<Collider>(id)) world.Remove<Collider>(id);
                    if (world.Has<DamageFlash>(id)) world.Remove<DamageFlash>(id);
                }
            }
        }

        // Тикаем DamageFlash
        world.QueryInto<DamageFlash>(_flashBuffer);
        foreach (int id in _flashBuffer)
        {
            ref var flash = ref world.Get<DamageFlash>(id);
            flash.TimeRemaining -= dt;
            if (flash.TimeRemaining <= 0)
                world.Remove<DamageFlash>(id);
        }

        // Тикаем анимацию смерти врагов
        world.QueryInto<EnemyDeathState>(_deathBuffer);
        foreach (int id in _deathBuffer)
        {
            ref var death = ref world.Get<EnemyDeathState>(id);
            death.TimeRemaining -= dt;
            if (death.TimeRemaining <= 0)
                world.DestroyEntity(id);
        }
    }
}
