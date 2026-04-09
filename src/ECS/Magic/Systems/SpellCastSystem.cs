using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Combat;
using DungeonOfShadows.ECS.Magic.Components;
using DungeonOfShadows.ECS.Rendering;

namespace DungeonOfShadows.ECS.Magic.Systems;

/// <summary>
/// Обрабатывает SpellCastRequest: спавнит снаряды, применяет мгновенные AoE, телепорт, хил.
/// </summary>
public class SpellCastSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly SpellDatabase _spellDb;
    private readonly AnimatorDatabase _animDb;
    private readonly List<int> _playerBuffer = new();
    private readonly List<int> _enemyBuffer = new();

    public SpellCastSystem(GameContext ctx, World world, GameConfig config, SpellDatabase spellDb,
        AnimatorDatabase animDb)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
        _spellDb = spellDb;
        _animDb = animDb;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _world;
        world.QueryInto<PlayerTag, SpellCastRequest>(_playerBuffer);
        if (_playerBuffer.Count == 0) return;

        int playerId = _playerBuffer[0];
        ref var req = ref world.Get<SpellCastRequest>(playerId);

        if (!_spellDb.TryGet(req.SpellId, out var def))
        {
            world.Remove<SpellCastRequest>(playerId);
            return;
        }

        int ts = _config.ScaledTileSize;
        ref var pos = ref world.Get<Position>(playerId);
        float centerX = pos.X + ts / 2f;
        float centerY = pos.Y + ts / 2f;

        int damage = CalcDamage(playerId, def, req.SpellLevel);

        switch (def.SpellId)
        {
            case SpellId.MagicBolt:
            case SpellId.Fireball:
            case SpellId.ChainLightning:
                SpawnProjectile(playerId, def, centerX, centerY, req.TargetX, req.TargetY, damage);
                break;

            case SpellId.FrostNova:
                CastFrostNova(playerId, def, centerX, centerY, damage);
                break;

            case SpellId.ShadowStep:
                CastShadowStep(playerId, centerX, centerY, req.TargetX, req.TargetY);
                break;

            case SpellId.Heal:
                CastHeal(playerId, def, req.SpellLevel);
                break;
        }

        world.Remove<SpellCastRequest>(playerId);
    }

    private int CalcDamage(int casterId, SpellDefinition def, int level)
    {
        int mInt = 0;
        if (_world.Has<Stats>(casterId))
        {
            ref var stats = ref _world.Get<Stats>(casterId);
            mInt = stats.INT;
        }

        float baseDmg = def.BaseDamage + mInt * def.IntScaling;
        if (level > 1)
            baseDmg *= 1f + (level - 1) * _config.SpellLevelDamageBonus;
        return (int)baseDmg;
    }

    private void SpawnProjectile(int ownerId, SpellDefinition def,
        float fromX, float fromY, float targetX, float targetY, int damage)
    {
        int ts = _config.ScaledTileSize;
        float dx = targetX - fromX;
        float dy = targetY - fromY;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.01f) { dx = 0; dy = 1; len = 1; }
        dx /= len;
        dy /= len;

        float speed = def.ProjectileSpeed * ts;
        float lifetime = def.Range * ts / speed;

        int projId = _world.CreateEntity();
        _world.Add(projId, new Position(fromX, fromY));
        _world.Add(projId, new Velocity(dx * speed, dy * speed));
        _world.Add(projId, new Projectile
        {
            OwnerId = ownerId,
            SpellId = def.Id,
            Damage = damage,
            LifeRemaining = lifetime,
            IsAoe = def.ExplodeOnHit,
            AoeRadius = def.AoeRadius * ts,
            OnHitEffect = def.OnHitEffect,
            EffectDuration = def.EffectDuration,
            EffectDamagePerTick = def.EffectDamagePerTick,
            SlowFactor = def.SlowFactor,
            ChainMaxTargets = def.ChainMaxTargets,
            ChainRadius = def.ChainRadius * ts
        });

        // Анимация снаряда (если есть спрайт)
        string? animId = def.SpellId switch
        {
            SpellId.MagicBolt => "magic_bolt",
            SpellId.Fireball => "fireball",
            _ => null
        };
        if (animId != null && _animDb.TryGet(animId, out var animDef))
        {
            var clip = AnimatorFactory.BuildSingleClip(animDef);
            _world.Add(projId, new Sprite(Raylib_cs.Color.White));
            _world.Add(projId, new Animation(clip));
            float angleDeg = MathF.Atan2(dy, dx) * (180f / MathF.PI);
            _world.Add(projId, new Rotation(angleDeg));
        }
    }

    private void CastFrostNova(int casterId, SpellDefinition def, float centerX, float centerY, int damage)
    {
        int ts = _config.ScaledTileSize;
        float radius = def.AoeRadius * ts;
        float radiusSq = radius * radius;

        _world.QueryInto<EnemyTag, Position>(_enemyBuffer);
        for (int i = 0; i < _enemyBuffer.Count; i++)
        {
            int enemyId = _enemyBuffer[i];
            ref var epos = ref _world.Get<Position>(enemyId);
            float ex = epos.X + ts / 2f;
            float ey = epos.Y + ts / 2f;

            float distSq = (ex - centerX) * (ex - centerX) + (ey - centerY) * (ey - centerY);
            if (distSq > radiusSq) continue;

            _ctx.DamageEvents.Add(new DamageEvent(casterId, enemyId, damage, false, ex, ey));
            MagicHelper.ApplyStatusEffect(_world, enemyId, def.OnHitEffect, def.EffectDuration, def.EffectDamagePerTick, def.SlowFactor);
        }

        MagicHelper.SpawnAoEVisual(_world, _config, centerX, centerY, radius, 100, 180, 255, 140);
    }

    private void CastShadowStep(int playerId, float fromX, float fromY, float targetX, float targetY)
    {
        int ts = _config.ScaledTileSize;
        float dx = targetX - fromX;
        float dy = targetY - fromY;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.01f) return;

        dx /= len;
        dy /= len;

        float maxDist = _config.ShadowStepTiles * ts;
        float dist = MathF.Min(len, maxDist);

        ref var pos = ref _world.Get<Position>(playerId);
        float stepX = pos.X;
        float stepY = pos.Y;
        float stepSize = ts / 2f;

        for (float d = stepSize; d <= dist; d += stepSize)
        {
            float testX = fromX + dx * d;
            float testY = fromY + dy * d;
            int tx = (int)(testX / ts);
            int ty = (int)(testY / ts);

            if (!_ctx.Map.IsWalkable(tx, ty)) break;
            stepX = testX - ts / 2f;
            stepY = testY - ts / 2f;
        }

        pos.X = stepX;
        pos.Y = stepY;

        if (_world.Has<Invincible>(playerId))
            _world.Remove<Invincible>(playerId);
        _world.Add(playerId, new Invincible(_config.ShadowStepIFrames));

        MagicHelper.SpawnAoEVisual(_world, _config, fromX, fromY, ts * 0.8f, 80, 40, 120, 160);
    }

    private void CastHeal(int playerId, SpellDefinition def, int level)
    {
        if (!_world.Has<Health>(playerId)) return;
        ref var health = ref _world.Get<Health>(playerId);

        int mInt = 0;
        if (_world.Has<Stats>(playerId))
        {
            ref var stats = ref _world.Get<Stats>(playerId);
            mInt = stats.INT;
        }

        float heal = def.BaseHeal + mInt * def.HealIntScaling;
        if (level > 1)
            heal *= 1f + (level - 1) * _config.SpellLevelHealBonus;
        health.HP = Math.Min(health.HP + (int)heal, health.MaxHP);

        int ts = _config.ScaledTileSize;
        ref var pos = ref _world.Get<Position>(playerId);
        MagicHelper.SpawnAoEVisual(_world, _config, pos.X + ts / 2f, pos.Y + ts / 2f, ts * 1.2f, 50, 220, 80, 140);
    }
}
