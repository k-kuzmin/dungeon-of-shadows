using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Рисует декоративные объекты (бочки, ящики, мешки) из Objects.png с FOV-проверкой.
/// </summary>
public class DecorationObjectRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly List<int> _buffer = new();

    public RenderPhase Phase => RenderPhase.World;

    public DecorationObjectRenderSystem(GameContext ctx)
    {
        _ctx = ctx;
        _world = ctx.World;
    }

    public void Tick(float dt)
    {
        var map = _ctx.Map;
        int ts = _ctx.Config.ScaledTileSize;
        float scale = _ctx.Config.RenderScale;
        var tex = _ctx.Textures.Get("objects");

        _world.QueryInto<DecorationObject, Position>(_buffer);
        for (int i = 0; i < _buffer.Count; i++)
        {
            int id = _buffer[i];
            ref var pos = ref _world.Get<Position>(id);
            ref var deco = ref _world.Get<DecorationObject>(id);

            // FOV check
            int tx = (int)((pos.X + ts / 2f) / ts);
            int ty = (int)((pos.Y + ts / 2f) / ts);
            if (map.InBounds(tx, ty) && map.Tiles[tx, ty].Visibility < 2)
                continue;

            var src = TileAtlas.GetDecoObjectSource(deco.AtlasCol, deco.AtlasRow, deco.SrcWidth, deco.SrcHeight);
            float destW = deco.SrcWidth * scale;
            float destH = deco.SrcHeight * scale;
            // Центрируем объект на тайле
            float destX = pos.X + ts / 2f - destW / 2f;
            float destY = pos.Y + ts - destH; // привязка к нижнему краю тайла
            var dest = new Rectangle(destX, destY, destW, destH);
            Raylib.DrawTexturePro(tex, src, dest, System.Numerics.Vector2.Zero, 0f, Color.White);
        }
    }
}
