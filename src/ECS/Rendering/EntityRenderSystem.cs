using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Рисует сущности (спрайты) с учётом FOV. World-space.
/// Для сущностей с Animation — рисует текстурные кадры.
/// Для остальных — fallback на цветные прямоугольники.
/// </summary>
public class EntityRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly IAssetProvider _assets;
    private readonly List<int> _spriteBuffer = new();

    public RenderPhase Phase => RenderPhase.World;

    public EntityRenderSystem(GameContext ctx, World world, GameConfig config, IAssetProvider assets)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
        _assets = assets;
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

            // Afterimage — рисуем полупрозрачно (прямоугольник, без текстуры)
            if (world.Has<Combat.AfterimageParticle>(id))
            {
                ref var after = ref world.Get<Combat.AfterimageParticle>(id);
                DrawAfterimage(id, ref pos, ref sprite, ref after, scale, ts);
                continue;
            }

            // Определяем tint
            var tint = Color.White;

            if (world.Has<Combat.DamageFlash>(id))
                tint = new Color(255, 80, 80, 255); // красный flash

            if (world.Has<Combat.EnemyDeathState>(id))
            {
                ref var death = ref world.Get<Combat.EnemyDeathState>(id);
                byte alpha = DeathAlpha(ref death);
                tint = new Color(tint.R, tint.G, tint.B, alpha);
            }

            // Анимированный спрайт — рисуем текстуру
            if (world.Has<Animation>(id))
            {
                ref var anim = ref world.Get<Animation>(id);
                if (anim.Clip != null)
                {
                    DrawAnimatedSprite(ref pos, ref anim, tint, scale, ts);
                    continue;
                }
            }

            // Fallback — цветной прямоугольник (для сущностей без анимации)
            var rectTint = sprite.Tint;
            if (world.Has<Combat.DamageFlash>(id))
                rectTint = Color.White;
            if (world.Has<Combat.EnemyDeathState>(id))
            {
                ref var death = ref world.Get<Combat.EnemyDeathState>(id);
                byte alpha = DeathAlpha(ref death);
                rectTint = new Color(rectTint.R, rectTint.G, rectTint.B, alpha);
            }
            Raylib.DrawRectangle(
                (int)pos.X, (int)pos.Y,
                sprite.Width * scale,
                sprite.Height * scale,
                rectTint
            );
        }
    }

    private void DrawAnimatedSprite(ref Position pos, ref Animation anim, Color tint, int scale, int ts)
    {
        var texture = _assets.GetTexture(anim.Clip.TextureId);
        var srcRect = anim.Clip.Frames[anim.FrameIndex];

        int destW = (int)(srcRect.Width * scale);
        int destH = (int)(srcRect.Height * scale);

        // Центрируем по горизонтали на тайле.
        // По вертикали: низ спрайта = низ тайла + offset (сдвиг вверх чтобы ноги совпадали с коллайдером).
        float drawX = pos.X + ts / 2f - destW / 2f;
        float drawY = pos.Y + ts - destH + _config.CharacterSpriteOffsetY * scale;

        var destRect = new Rectangle(drawX, drawY, destW, destH);

        Raylib.DrawTexturePro(texture, srcRect, destRect,
            System.Numerics.Vector2.Zero, 0f, tint);
    }

    private void DrawAfterimage(int id, ref Position pos, ref Sprite sprite,
        ref Combat.AfterimageParticle after, int scale, int ts)
    {
        byte alpha = (byte)(after.Alpha * 255);

        // Afterimage от анимированного спрайта
        if (_world.Has<Animation>(id))
        {
            ref var anim = ref _world.Get<Animation>(id);
            if (anim.Clip != null)
            {
                var tint = new Color((byte)255, (byte)255, (byte)255, alpha);
                DrawAnimatedSprite(ref pos, ref anim, tint, scale, ts);
                return;
            }
        }

        // Fallback — прямоугольник
        var dimTint = new Color(sprite.Tint.R, sprite.Tint.G, sprite.Tint.B, alpha);
        Raylib.DrawRectangle(
            (int)pos.X, (int)pos.Y,
            sprite.Width * scale, sprite.Height * scale, dimTint);
    }

    private static byte DeathAlpha(ref Combat.EnemyDeathState death)
        => death.Lifetime > 0f
            ? (byte)(255 * Math.Clamp(death.TimeRemaining / death.Lifetime, 0f, 1f))
            : (byte)0;
}
