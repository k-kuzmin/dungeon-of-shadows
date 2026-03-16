using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Rendering.Systems;

namespace DungeonOfShadows.ECS.Combat.Systems;

/// <summary>
/// Обрабатывает активные атаки: тест сектора, урон, DamageEvent.
/// </summary>
public class MeleeAttackSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly CameraSystem _cameraSystem;
    private readonly List<int> _attackBuffer = new();
    private readonly List<int> _enemyBuffer = new();
    private readonly List<int> _cooldownBuffer = new();
    private readonly Random _rng = new();

    public MeleeAttackSystem(GameContext ctx, CameraSystem cameraSystem)
    {
        _ctx = ctx;
        _cameraSystem = cameraSystem;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _ctx.World;
        int ts = _ctx.Config.ScaledTileSize;

        // Тикаем кулдаун melee
        world.QueryInto<MeleeCooldown>(_cooldownBuffer);
        foreach (int id in _cooldownBuffer)
        {
            ref var cooldown = ref world.Get<MeleeCooldown>(id);
            cooldown.TimeRemaining -= dt;
            if (cooldown.TimeRemaining <= 0)
                world.Remove<MeleeCooldown>(id);
        }

        world.QueryInto<MeleeAttack, Position>(_attackBuffer);
        foreach (int attackerId in _attackBuffer)
        {
            ref var attack = ref world.Get<MeleeAttack>(attackerId);
            ref var attackPos = ref world.Get<Position>(attackerId);

            float originX = attackPos.X + ts / 2f;
            float originY = attackPos.Y + ts / 2f;

            // Сканируем попадания один раз за атаку
            if (!attack.HitScanned)
            {
                attack.HitScanned = true;

                // Определяем, атакует игрок или враг
                bool attackerIsPlayer = world.Has<PlayerTag>(attackerId);

                if (attackerIsPlayer)
                {
                    // Игрок бьёт врагов
                    world.QueryInto<EnemyTag, Position>(_enemyBuffer);
                    if (!world.Has<Stats>(attackerId))
                        continue;
                    ref var attackerStats = ref world.Get<Stats>(attackerId);

                    foreach (int targetId in _enemyBuffer)
                    {
                        if (attack.AlreadyHit != null && attack.AlreadyHit.Contains(targetId))
                            continue;

                        ref var targetPos = ref world.Get<Position>(targetId);
                        float targetCX = targetPos.X + ts / 2f;
                        float targetCY = targetPos.Y + ts / 2f;

                        if (!IsInSector(originX, originY, attack.DirX, attack.DirY,
                                attack.ArcRadians, attack.Radius, targetCX, targetCY))
                            continue;

                        attack.AlreadyHit?.Add(targetId);

                        if (!world.Has<Stats>(targetId))
                            continue;
                        ref var targetStats = ref world.Get<Stats>(targetId);
                        var (damage, isCrit) = DamageCalculator.Compute(
                            ref attackerStats, ref targetStats, _ctx.Config, _rng);

                        _ctx.DamageEvents.Add(new DamageEvent(
                            attackerId, targetId, damage, isCrit, targetCX, targetCY));

                        _cameraSystem.TriggerShake();
                    }
                }
            }

            // Тикаем таймер
            attack.TimeRemaining -= dt;
            if (attack.TimeRemaining <= 0)
            {
                attack.AlreadyHit?.Clear();
                world.Remove<MeleeAttack>(attackerId);
            }
        }
    }

    /// <summary>
    /// Проверяет, попадает ли точка (px, py) в сектор атаки.
    /// </summary>
    private static bool IsInSector(float ox, float oy, float dirX, float dirY,
        float arcRadians, float radius, float px, float py)
    {
        float dx = px - ox;
        float dy = py - oy;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist > radius || dist < 0.01f) return false;

        // Нормализуем
        float ndx = dx / dist;
        float ndy = dy / dist;

        // Скалярное произведение = cos угла между направлениями
        float dot = ndx * dirX + ndy * dirY;

        return dot >= MathF.Cos(arcRadians / 2f);
    }
}
