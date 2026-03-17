using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Рисует сущности (спрайты) с учётом FOV. World-space.
/// </summary>
public class EntityRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly List<int> _spriteBuffer = new();

    public RenderPhase Phase => RenderPhase.World;

    public EntityRenderSystem(GameContext ctx, World world, GameConfig config)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
    }

    public void Tick(float dt)
    {
        var world = _world;
        int scale = _config.RenderScale;
        int ts = _config.ScaledTileSize;
        var map = _ctx.Map;

        world.QueryInto<Sprite, Position>(_spriteBuffer);
        foreach (int id in _spriteBuffer)
        {
            ref var pos = ref world.Get<Position>(id);
            ref var sprite = ref world.Get<Sprite>(id);

            // Рисуем сущность только если тайл под ней видим (Visible = 2)
            int tx = (int)((pos.X + ts / 2f) / ts);
            int ty = (int)((pos.Y + ts / 2f) / ts);
            if (map.InBounds(tx, ty) && map.Tiles[tx, ty].Visibility < 2)
                continue;

            // Afterimage — рисуем полупрозрачно
            if (world.Has<Combat.AfterimageParticle>(id))
            {
                ref var after = ref world.Get<Combat.AfterimageParticle>(id);
                var dimTint = new Color(sprite.Tint.R, sprite.Tint.G, sprite.Tint.B,
                    (byte)(after.Alpha * 255));
                Raylib.DrawRectangle(
                    (int)pos.X, (int)pos.Y,
                    sprite.Width * scale, sprite.Height * scale, dimTint);
                continue;
            }

            // DamageFlash — белый оверлей при попадании
            var tint = sprite.Tint;
            if (world.Has<Combat.DamageFlash>(id))
                tint = Color.White;

            // Enemy death — плавное затухание спрайта
            if (world.Has<Combat.EnemyDeathState>(id))
            {
                ref var death = ref world.Get<Combat.EnemyDeathState>(id);
                float lifeFraction = death.Lifetime > 0f
                    ? Math.Clamp(death.TimeRemaining / death.Lifetime, 0f, 1f)
                    : 0f;
                tint = new Color(tint.R, tint.G, tint.B, (byte)(255 * lifeFraction));
            }

            Raylib.DrawRectangle(
                (int)pos.X, (int)pos.Y,
                sprite.Width * scale,
                sprite.Height * scale,
                tint
            );
        }
    }
}
