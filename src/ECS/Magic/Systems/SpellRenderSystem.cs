using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Magic.Components;
using DungeonOfShadows.ECS.Rendering;

namespace DungeonOfShadows.ECS.Magic.Systems;

/// <summary>
/// Рендер снарядов (цветные круги) и AoE-визуалов (расширяющиеся кольца). World-phase.
/// </summary>
public class SpellRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly List<int> _projBuffer = new(16);
    private readonly List<int> _aoeBuffer = new(16);
    private readonly List<int> _statusBuffer = new(64);

    public RenderPhase Phase => RenderPhase.World;

    // Цвета снарядов
    private static readonly Color MagicBoltColor = new(160, 80, 220, 240);
    private static readonly Color MagicBoltGlow = new(160, 80, 220, 60);
    private static readonly Color FireballColor = new(255, 100, 20, 240);
    private static readonly Color FireballGlow = new(255, 100, 20, 60);
    private static readonly Color LightningColor = new(80, 200, 255, 240);
    private static readonly Color LightningGlow = new(80, 200, 255, 60);
    private static readonly Color DefaultProjColor = new(200, 200, 200, 240);
    private static readonly Color DefaultProjGlow = new(200, 200, 200, 60);

    // Цвета индикаторов статус-эффектов
    private static readonly Color BurnIndicator = new(255, 140, 30, 180);
    private static readonly Color SlowIndicator = new(80, 160, 255, 180);

    public SpellRenderSystem(GameContext ctx, World world, GameConfig config)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
    }

    public void Tick(float dt)
    {
        DrawProjectiles();
        DrawAoEVisuals();
        DrawStatusEffectIndicators();
    }

    private void DrawProjectiles()
    {
        var world = _world;
        int radius = _config.ProjectileRenderRadius;

        world.QueryInto<Projectile, Position>(_projBuffer);
        for (int i = 0; i < _projBuffer.Count; i++)
        {
            int id = _projBuffer[i];
            ref var proj = ref world.Get<Projectile>(id);
            ref var pos = ref world.Get<Position>(id);

            // Снаряды с анимированным спрайтом отрисованы в Y-sorted pass — только glow
            bool hasSprite = world.Has<Animation>(id);

            var (color, glow) = GetProjectileColors((SpellId)proj.SpellId);
            int r = radius;
            if (proj.IsAoe) r = (int)(radius * 1.4f);

            if (!hasSprite)
            {
                Raylib.DrawCircle((int)pos.X, (int)pos.Y, r, color);
                Raylib.DrawCircle((int)pos.X, (int)pos.Y, r * 2, glow);
            }
        }
    }

    private void DrawAoEVisuals()
    {
        var world = _world;

        world.QueryInto<AoEVisual, Position>(_aoeBuffer);
        for (int i = 0; i < _aoeBuffer.Count; i++)
        {
            int id = _aoeBuffer[i];
            ref var aoe = ref world.Get<AoEVisual>(id);
            ref var pos = ref world.Get<Position>(id);

            float frac = aoe.Lifetime > 0 ? aoe.TimeRemaining / aoe.Lifetime : 0f;
            byte alpha = (byte)(aoe.A * Math.Clamp(frac, 0f, 1f));

            float currentRadius = aoe.Radius * (1f + (1f - frac) * 0.3f);

            var color = new Color(aoe.R, aoe.G, aoe.B, alpha);
            Raylib.DrawCircleLines((int)pos.X, (int)pos.Y, currentRadius, color);
            Raylib.DrawCircleLines((int)pos.X, (int)pos.Y, currentRadius * 0.7f, color);

            var fill = new Color(aoe.R, aoe.G, aoe.B, (byte)(alpha / 4));
            Raylib.DrawCircle((int)pos.X, (int)pos.Y, currentRadius, fill);
        }
    }

    private void DrawStatusEffectIndicators()
    {
        var world = _world;
        int ts = _config.ScaledTileSize;

        world.QueryInto<StatusEffects, Position>(_statusBuffer);
        for (int i = 0; i < _statusBuffer.Count; i++)
        {
            int id = _statusBuffer[i];
            ref var effects = ref world.Get<StatusEffects>(id);
            ref var pos = ref world.Get<Position>(id);

            float cx = pos.X + ts / 2f;
            float cy = pos.Y;

            for (int s = 0; s < effects.ActiveCount; s++)
            {
                var slot = effects.GetSlot(s);
                if (slot.Type == SpellEffectType.Burn)
                    Raylib.DrawCircle((int)(cx - 8 + s * 10), (int)(cy - 4), 3, BurnIndicator);
                else if (slot.Type == SpellEffectType.Slow)
                    Raylib.DrawCircle((int)(cx - 8 + s * 10), (int)(cy - 4), 3, SlowIndicator);
            }
        }
    }

    private static (Color solid, Color glow) GetProjectileColors(SpellId spellId)
    {
        return spellId switch
        {
            SpellId.MagicBolt => (MagicBoltColor, MagicBoltGlow),
            SpellId.Fireball => (FireballColor, FireballGlow),
            SpellId.ChainLightning => (LightningColor, LightningGlow),
            _ => (DefaultProjColor, DefaultProjGlow)
        };
    }
}
