using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Рисует декоративные объекты (бочки, ящики, мешки) и факелы с FOV-проверкой.
/// </summary>
public class DecorationObjectRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly IAssetProvider _assets;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly List<int> _buffer = new();
    private readonly List<int> _torchBuffer = new();

    public RenderPhase Phase => RenderPhase.World;

    public DecorationObjectRenderSystem(GameContext ctx, IAssetProvider assets, World world, GameConfig config)
    {
        _ctx = ctx;
        _assets = assets;
        _world = world;
        _config = config;
    }

    public void Tick(float dt)
    {
        var map = _ctx.Map;
        int ts = _config.ScaledTileSize;
        float scale = _config.RenderScale;

        // Декоративные объекты (бочки, ящики и т.д.)
        var tex = _assets.GetTexture("objects");
        _world.QueryInto<DecorationObject, Position>(_buffer);
        for (int i = 0; i < _buffer.Count; i++)
        {
            int id = _buffer[i];
            ref var pos = ref _world.Get<Position>(id);
            ref var deco = ref _world.Get<DecorationObject>(id);

            int tx = (int)((pos.X + ts / 2f) / ts);
            int ty = (int)((pos.Y + ts / 2f) / ts);
            if (map.InBounds(tx, ty) && map.Tiles[tx, ty].Visibility < 2)
                continue;

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

        // Факелы (Animation-based)
        _world.QueryInto<TorchTag, Position>(_torchBuffer);
        for (int i = 0; i < _torchBuffer.Count; i++)
        {
            int id = _torchBuffer[i];
            ref var pos = ref _world.Get<Position>(id);

            int tx = (int)((pos.X + ts / 2f) / ts);
            int ty = (int)((pos.Y + ts / 2f) / ts);
            if (map.InBounds(tx, ty) && map.Tiles[tx, ty].Visibility < 2)
                continue;

            if (!_world.Has<Animation>(id)) continue;
            ref var anim = ref _world.Get<Animation>(id);
            if (anim.Clip == null) continue;

            var fireTex = _assets.GetTexture(anim.Clip.TextureId);
            var src = anim.Clip.Frames[anim.FrameIndex];
            float destW = src.Width * scale;
            float destH = src.Height * scale;
            float destX = pos.X + ts / 2f - destW / 2f;
            float destY = pos.Y + ts - destH;
            var dest = new Rectangle(destX, destY, destW, destH);
            Raylib.DrawTexturePro(fireTex, src, dest, System.Numerics.Vector2.Zero, 0f, Color.White);
        }
    }
}
