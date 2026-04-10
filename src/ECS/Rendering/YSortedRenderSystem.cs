using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Combat;
using DungeonOfShadows.ECS.Items;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Единая Y-sorted отрисовка сущностей, декораций и сундуков.
/// Собирает все Y-сортируемые объекты, сортирует по нижнему краю тайла (pos.Y + ts),
/// рисует в правильном порядке глубины. Afterimage рисуется до sorted pass.
/// </summary>
public class YSortedRenderSystem : IRenderTickable
{
    private enum DrawKind : byte { Entity, Chest, Decoration }

    private struct SortEntry
    {
        public int Id;
        public float YKey;
        public DrawKind Kind;
    }

    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly IAssetProvider _assets;

    private readonly List<int> _entityBuffer = new(64);
    private readonly List<int> _chestBuffer = new(16);
    private readonly List<int> _decoBuffer = new(64);
    private readonly List<SortEntry> _sortBuffer = new(128);

    private static readonly Comparison<SortEntry> _byY =
        static (a, b) => a.YKey.CompareTo(b.YKey);

    public RenderPhase Phase => RenderPhase.World;

    public YSortedRenderSystem(GameContext ctx, World world, GameConfig config, IAssetProvider assets)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
        _assets = assets;
    }

    public void Tick(float dt)
    {
        var world = _world;
        var map = _ctx.Map;
        int ts = _config.ScaledTileSize;
        int scale = _config.RenderScale;

        // --- Afterimage pass (всегда под живыми сущностями) ---
        world.QueryInto<Sprite, Position>(_entityBuffer);
        for (int i = 0; i < _entityBuffer.Count; i++)
        {
            int id = _entityBuffer[i];
            if (!world.Has<AfterimageParticle>(id))
                continue;

            ref var pos = ref world.Get<Position>(id);
            ref var sprite = ref world.Get<Sprite>(id);

            int tx = (int)((pos.X + ts / 2f) / ts);
            int ty = (int)((pos.Y + ts / 2f) / ts);
            if (map.InBounds(tx, ty) && map.Tiles[tx, ty].Visibility < 2)
                continue;

            ref var after = ref world.Get<AfterimageParticle>(id);
            DrawAfterimage(id, ref pos, ref sprite, ref after, scale, ts);
        }

        // --- Сбор Y-сортируемых объектов ---
        _sortBuffer.Clear();

        // Сущности (кроме afterimage)
        for (int i = 0; i < _entityBuffer.Count; i++)
        {
            int id = _entityBuffer[i];
            if (world.Has<AfterimageParticle>(id))
                continue;

            ref var pos = ref world.Get<Position>(id);

            int tx = (int)((pos.X + ts / 2f) / ts);
            int ty = (int)((pos.Y + ts / 2f) / ts);
            if (map.InBounds(tx, ty) && map.Tiles[tx, ty].Visibility < 2)
                continue;

            _sortBuffer.Add(new SortEntry { Id = id, YKey = pos.Y + ts, Kind = DrawKind.Entity });
        }

        // Сундуки — пивот скорректирован: тело сундука заканчивается выше нижнего края спрайта
        int chestSrcH = 32; // высота спрайта сундука в source px (2 * 16)
        int chestBodyBottom = _config.ChestDrawOffsetY + _config.ChestDrawHeight;
        float chestFootGap = (chestSrcH - chestBodyBottom) * scale;

        world.QueryInto<Chest, Position>(_chestBuffer);
        for (int i = 0; i < _chestBuffer.Count; i++)
        {
            int id = _chestBuffer[i];
            ref var pos = ref world.Get<Position>(id);

            int tx = (int)((pos.X + ts / 2f) / ts);
            int ty = (int)((pos.Y + ts / 2f) / ts);
            if (map.InBounds(tx, ty) && map.Tiles[tx, ty].Visibility < 2)
                continue;

            _sortBuffer.Add(new SortEntry { Id = id, YKey = pos.Y + ts - chestFootGap, Kind = DrawKind.Chest });
        }

        // Декоративные объекты
        world.QueryInto<DecorationObject, Position>(_decoBuffer);
        for (int i = 0; i < _decoBuffer.Count; i++)
        {
            int id = _decoBuffer[i];
            ref var pos = ref world.Get<Position>(id);

            int tx = (int)((pos.X + ts / 2f) / ts);
            int ty = (int)((pos.Y + ts / 2f) / ts);
            if (map.InBounds(tx, ty) && map.Tiles[tx, ty].Visibility < 2)
                continue;

            _sortBuffer.Add(new SortEntry { Id = id, YKey = pos.Y + ts, Kind = DrawKind.Decoration });
        }

        // --- Сортировка по Y ---
        _sortBuffer.Sort(_byY);

        // --- Отрисовка в отсортированном порядке ---
        for (int i = 0; i < _sortBuffer.Count; i++)
        {
            var entry = _sortBuffer[i];
            switch (entry.Kind)
            {
                case DrawKind.Entity:
                    DrawEntity(entry.Id, scale, ts);
                    break;
                case DrawKind.Chest:
                    DrawChest(entry.Id, scale, ts);
                    break;
                case DrawKind.Decoration:
                    DrawDecoration(entry.Id, scale, ts);
                    break;
            }
        }
    }

    private void DrawEntity(int id, int scale, int ts)
    {
        var world = _world;
        ref var pos = ref world.Get<Position>(id);

        // Определяем tint
        var tint = Color.White;

        if (world.Has<DamageFlash>(id))
            tint = new Color(255, 80, 80, 255);

        if (world.Has<EnemyDeathState>(id))
        {
            ref var death = ref world.Get<EnemyDeathState>(id);
            byte alpha = DeathAlpha(ref death);
            tint = new Color(tint.R, tint.G, tint.B, alpha);
        }

        // Анимированный спрайт
        if (world.Has<Animation>(id))
        {
            ref var anim = ref world.Get<Animation>(id);
            if (anim.Clip != null)
            {
                DrawAnimatedSprite(id, ref pos, ref anim, tint, scale, ts);
                return;
            }
        }

        // Fallback — цветной прямоугольник
        ref var sprite = ref world.Get<Sprite>(id);
        var rectTint = sprite.Tint;
        if (world.Has<DamageFlash>(id))
            rectTint = Color.White;
        if (world.Has<EnemyDeathState>(id))
        {
            ref var death = ref world.Get<EnemyDeathState>(id);
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

    private void DrawChest(int id, int scale, int ts)
    {
        if (!_world.Has<Animation>(id)) return;
        ref var anim = ref _world.Get<Animation>(id);
        if (anim.Clip == null) return;

        ref var pos = ref _world.Get<Position>(id);
        var chestTex = _assets.GetTexture(anim.Clip.TextureId);
        var src = anim.Clip.Frames[anim.FrameIndex];
        float destW = src.Width * scale;
        float destH = src.Height * scale;
        float destX = pos.X + ts / 2f - destW / 2f;
        float destY = pos.Y + ts - destH;
        var dest = new Rectangle(destX, destY, destW, destH);
        Raylib.DrawTexturePro(chestTex, src, dest, System.Numerics.Vector2.Zero, 0f, Color.White);
    }

    private void DrawDecoration(int id, int scale, int ts)
    {
        ref var pos = ref _world.Get<Position>(id);
        ref var deco = ref _world.Get<DecorationObject>(id);

        var tex = _assets.GetTexture("objects");
        var src = TileAtlas.GetDecoObjectSource(deco.SrcX, deco.SrcY, deco.SrcWidth, deco.SrcHeight);
        float destW = deco.SrcWidth * scale;
        float destH = deco.SrcHeight * scale;
        float destX = pos.X + ts / 2f - destW / 2f;
        float destY = pos.Y + ts - destH;
        var dest = new Rectangle(destX, destY, destW, destH);
        Raylib.DrawTexturePro(tex, src, dest, System.Numerics.Vector2.Zero, 0f, Color.White);

        if (_ctx.DebugMode)
        {
            string label = $"{deco.Type} [{deco.SrcX},{deco.SrcY},{deco.SrcWidth}x{deco.SrcHeight}]";
            Raylib.DrawText(label, (int)destX - 4, (int)destY - 12, 10, Color.Yellow);
        }
    }

    private void DrawAnimatedSprite(int entityId, ref Position pos, ref Animation anim, Color tint, int scale, int ts)
    {
        var texture = _assets.GetTexture(anim.Clip.TextureId);
        var srcRect = anim.Clip.Frames[anim.FrameIndex];

        int destW = (int)(srcRect.Width * scale);
        int destH = (int)(srcRect.Height * scale);

        // Снаряды с Rotation рисуются по центру позиции, без character offset
        if (_world.Has<Rotation>(entityId))
        {
            ref var rot = ref _world.Get<Rotation>(entityId);
            var origin = new System.Numerics.Vector2(destW / 2f, destH / 2f);
            // origin в DrawTexturePro — точка привязки; (destRect.X, destRect.Y) = позиция origin
            var destRect = new Rectangle(pos.X, pos.Y, destW, destH);
            Raylib.DrawTexturePro(texture, srcRect, destRect, origin, rot.Degrees, tint);
            return;
        }

        float drawX = pos.X + ts / 2f - destW / 2f;
        float drawY = pos.Y + ts - destH + _config.CharacterSpriteOffsetY * scale;

        var destRectChar = new Rectangle(drawX, drawY, destW, destH);

        Raylib.DrawTexturePro(texture, srcRect, destRectChar,
            System.Numerics.Vector2.Zero, 0f, tint);
    }

    private void DrawAfterimage(int id, ref Position pos, ref Sprite sprite,
        ref AfterimageParticle after, int scale, int ts)
    {
        byte alpha = (byte)(after.Alpha * 255);

        if (_world.Has<Animation>(id))
        {
            ref var anim = ref _world.Get<Animation>(id);
            if (anim.Clip != null)
            {
                var tint = new Color((byte)255, (byte)255, (byte)255, alpha);
                DrawAnimatedSprite(id, ref pos, ref anim, tint, scale, ts);
                return;
            }
        }

        var dimTint = new Color(sprite.Tint.R, sprite.Tint.G, sprite.Tint.B, alpha);
        Raylib.DrawRectangle(
            (int)pos.X, (int)pos.Y,
            sprite.Width * scale, sprite.Height * scale, dimTint);
    }

    private static byte DeathAlpha(ref EnemyDeathState death)
        => death.Lifetime > 0f
            ? (byte)(255 * Math.Clamp(death.TimeRemaining / death.Lifetime, 0f, 1f))
            : (byte)0;
}
