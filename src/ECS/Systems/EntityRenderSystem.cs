using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Systems;

/// <summary>
/// Рисует сущности (спрайты) с учётом FOV. World-space.
/// </summary>
public class EntityRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly List<int> _spriteBuffer = new();

    public RenderPhase Phase => RenderPhase.World;

    public EntityRenderSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        var world = _ctx.World;
        int scale = _ctx.Config.RenderScale;
        int ts = _ctx.Config.ScaledTileSize;
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

            Raylib.DrawRectangle(
                (int)pos.X, (int)pos.Y,
                sprite.Width * scale,
                sprite.Height * scale,
                sprite.Tint
            );
        }
    }
}
