using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Combat;
using DungeonOfShadows.ECS.Magic.Components;

namespace DungeonOfShadows.ECS.Magic.Systems;

/// <summary>
/// Коллизия снарядов со стенами и врагами, AoE взрывы, Chain Lightning.
/// Движение — через PhysicsSystem (Position+Velocity без Collider).
/// </summary>
public class ProjectileSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly List<int> _projBuffer = new();
    private readonly List<int> _enemyBuffer = new();
    private readonly List<int> _toDestroy = new();
    private readonly List<int> _aoeBuffer = new();
    private readonly int[] _chainHitIds = new int[8];
    private int _chainHitCount;

    public ProjectileSystem(GameContext ctx, World world, GameConfig config)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _world;
        var map = _ctx.Map;
        int ts = _config.ScaledTileSize;

        _toDestroy.Clear();

        // Один раз запрашиваем врагов на весь кадр
        world.QueryInto<EnemyTag, Position>(_enemyBuffer);

        world.QueryInto<Projectile, Position>(_projBuffer);
        for (int i = 0; i < _projBuffer.Count; i++)
        {
            int id = _projBuffer[i];
            ref var proj = ref world.Get<Projectile>(id);
            ref var pos = ref world.Get<Position>(id);

            // Lifetime
            proj.LifeRemaining -= dt;
            if (proj.LifeRemaining <= 0)
            {
                _toDestroy.Add(id);
                continue;
            }

            // Проверяем текущую позицию на стену
            int tx = (int)(pos.X / ts);
            int ty = (int)(pos.Y / ts);
            if (!map.IsWalkable(tx, ty))
            {
                if (proj.IsAoe)
                    TriggerAoE(ref proj, pos.X, pos.Y, ts, -1);
                _toDestroy.Add(id);
                continue;
            }

            // AABB коллизия с врагами
            if (CheckEnemyHit(id, ref proj, ref pos, ts))
            {
                _toDestroy.Add(id);
                continue;
            }
        }

        // Тик AoE-визуалов (lifetime)
        TickAoEVisuals(world, dt);

        // Уничтожение
        for (int i = 0; i < _toDestroy.Count; i++)
        {
            int id = _toDestroy[i];
            if (world.IsAlive(id))
                world.DestroyEntity(id);
        }
    }

    private bool CheckEnemyHit(int projId, ref Projectile proj, ref Position projPos, int ts)
    {
        var world = _world;

        for (int i = 0; i < _enemyBuffer.Count; i++)
        {
            int enemyId = _enemyBuffer[i];
            if (enemyId == proj.OwnerId) continue;
            if (!world.Has<Collider>(enemyId)) continue;

            ref var epos = ref world.Get<Position>(enemyId);
            ref var col = ref world.Get<Collider>(enemyId);

            float left = epos.X + col.OffsetX;
            float top = epos.Y + col.OffsetY;
            float right = left + col.Width;
            float bottom = top + col.Height;

            if (projPos.X >= left && projPos.X <= right &&
                projPos.Y >= top && projPos.Y <= bottom)
            {
                float hitX = epos.X + ts / 2f;
                float hitY = epos.Y + ts / 2f;

                _ctx.DamageEvents.Add(new DamageEvent(proj.OwnerId, enemyId, proj.Damage, false, hitX, hitY));
                MagicHelper.ApplyStatusEffect(_world, enemyId, proj.OnHitEffect, proj.EffectDuration, proj.EffectDamagePerTick, proj.SlowFactor);

                if (proj.IsAoe)
                    TriggerAoE(ref proj, projPos.X, projPos.Y, ts, enemyId);

                if (proj.ChainMaxTargets > 0)
                    TriggerChainLightning(ref proj, hitX, hitY, enemyId, ts);

                return true;
            }
        }

        return false;
    }

    private void TriggerAoE(ref Projectile proj, float x, float y, int ts, int skipEnemyId)
    {
        float radiusSq = proj.AoeRadius * proj.AoeRadius;
        var world = _world;

        for (int i = 0; i < _enemyBuffer.Count; i++)
        {
            int enemyId = _enemyBuffer[i];
            if (enemyId == proj.OwnerId) continue;
            if (enemyId == skipEnemyId) continue;

            ref var epos = ref world.Get<Position>(enemyId);
            float ex = epos.X + ts / 2f;
            float ey = epos.Y + ts / 2f;

            float distSq = (ex - x) * (ex - x) + (ey - y) * (ey - y);
            if (distSq > radiusSq) continue;

            _ctx.DamageEvents.Add(new DamageEvent(proj.OwnerId, enemyId, proj.Damage / 2, false, ex, ey));
            MagicHelper.ApplyStatusEffect(_world, enemyId, proj.OnHitEffect, proj.EffectDuration, proj.EffectDamagePerTick, proj.SlowFactor);
        }

        // AoE визуал
        byte r = proj.OnHitEffect == SpellEffectType.Burn ? (byte)255 : (byte)100;
        byte g = proj.OnHitEffect == SpellEffectType.Burn ? (byte)120 : (byte)180;
        byte b = proj.OnHitEffect == SpellEffectType.Burn ? (byte)30 : (byte)255;
        MagicHelper.SpawnAoEVisual(_world, _config, x, y, proj.AoeRadius, r, g, b, 140);
    }

    private void TriggerChainLightning(ref Projectile proj, float hitX, float hitY, int hitEnemyId, int ts)
    {
        _chainHitCount = 0;
        _chainHitIds[_chainHitCount++] = hitEnemyId;

        var world = _world;
        float searchRadiusSq = proj.ChainRadius * proj.ChainRadius;
        float lastX = hitX;
        float lastY = hitY;

        for (int c = 0; c < proj.ChainMaxTargets; c++)
        {
            float bestDistSq = float.MaxValue;
            int bestId = -1;
            float bestX = 0, bestY = 0;

            for (int i = 0; i < _enemyBuffer.Count; i++)
            {
                int eid = _enemyBuffer[i];
                if (eid == proj.OwnerId) continue;

                bool alreadyHit = false;
                for (int h = 0; h < _chainHitCount; h++)
                    if (_chainHitIds[h] == eid) { alreadyHit = true; break; }
                if (alreadyHit) continue;

                ref var epos = ref world.Get<Position>(eid);
                float ex = epos.X + ts / 2f;
                float ey = epos.Y + ts / 2f;

                float distSq = (ex - lastX) * (ex - lastX) + (ey - lastY) * (ey - lastY);
                if (distSq > searchRadiusSq) continue;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestId = eid;
                    bestX = ex;
                    bestY = ey;
                }
            }

            if (bestId < 0) break;

            if (_chainHitCount < _chainHitIds.Length)
                _chainHitIds[_chainHitCount++] = bestId;

            _ctx.DamageEvents.Add(new DamageEvent(proj.OwnerId, bestId, proj.Damage, false, bestX, bestY));
            MagicHelper.ApplyStatusEffect(_world, bestId, proj.OnHitEffect, proj.EffectDuration, proj.EffectDamagePerTick, proj.SlowFactor);
            lastX = bestX;
            lastY = bestY;
        }
    }

    private void TickAoEVisuals(World world, float dt)
    {
        world.QueryInto<AoEVisual, Position>(_aoeBuffer);
        for (int i = 0; i < _aoeBuffer.Count; i++)
        {
            int id = _aoeBuffer[i];
            ref var aoe = ref world.Get<AoEVisual>(id);

            aoe.TimeRemaining -= dt;
            if (aoe.TimeRemaining <= 0)
                _toDestroy.Add(id);
        }
    }
}
