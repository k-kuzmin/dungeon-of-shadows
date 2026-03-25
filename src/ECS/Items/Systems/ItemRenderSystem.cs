using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Rendering;

namespace DungeonOfShadows.ECS.Items.Systems;

public class ItemRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly IAssetProvider _assets;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly List<int> _itemBuffer = new();
    private readonly List<int> _chestBuffer = new();

    public RenderPhase Phase => RenderPhase.World;

    public ItemRenderSystem(GameContext ctx, World world, GameConfig config, IAssetProvider assets)
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
        var itemsTex = _assets.GetTexture("items");

        // Предметы на земле — спрайты из items.png
        _world.QueryInto<ItemOnGround, Position>(_itemBuffer);
        for (int i = 0; i < _itemBuffer.Count; i++)
        {
            int id = _itemBuffer[i];
            if (!_world.Has<ItemStack>(id))
                continue;

            ref var pos = ref _world.Get<Position>(id);

            // FOV check
            int tx = (int)((pos.X + ts / 2f) / ts);
            int ty = (int)((pos.Y + ts / 2f) / ts);
            if (map.InBounds(tx, ty) && map.Tiles[tx, ty].Visibility < 2)
                continue;

            ref var stack = ref _world.Get<ItemStack>(id);
            var src = ItemAtlas.GetSource(stack.DefinitionId, stack.Type, stack.Rarity);
            float destW = ts * 0.5f;
            float destH = ts * 0.5f;
            float destX = pos.X + ts / 2f - destW / 2f;
            float destY = pos.Y + ts / 2f - destH / 2f;
            var dest = new Rectangle(destX, destY, destW, destH);
            Raylib.DrawTexturePro(itemsTex, src, dest, System.Numerics.Vector2.Zero, 0f, Color.White);
        }

        // Сундуки — спрайтовые, анимация через Animation компонент

        _world.QueryInto<Chest, Position>(_chestBuffer);
        for (int i = 0; i < _chestBuffer.Count; i++)
        {
            int id = _chestBuffer[i];
            ref var pos = ref _world.Get<Position>(id);

            // FOV check
            int tx = (int)((pos.X + ts / 2f) / ts);
            int ty = (int)((pos.Y + ts / 2f) / ts);
            if (map.InBounds(tx, ty) && map.Tiles[tx, ty].Visibility < 2)
                continue;

            if (!_world.Has<Animation>(id)) continue;
            ref var anim = ref _world.Get<Animation>(id);
            if (anim.Clip == null) continue;

            var chestTex = _assets.GetTexture(anim.Clip.TextureId);
            var src = anim.Clip.Frames[anim.FrameIndex];
            float destW = src.Width * scale;
            float destH = src.Height * scale;
            float destX = pos.X + ts / 2f - destW / 2f;
            float destY = pos.Y + ts - destH;
            var dest = new Rectangle(destX, destY, destW, destH);
            Raylib.DrawTexturePro(chestTex, src, dest, System.Numerics.Vector2.Zero, 0f, Color.White);
        }
    }

}
