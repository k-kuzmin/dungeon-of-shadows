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

        // Предметы на земле — пока цветовые прямоугольники
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
            Color color = GetRarityColor(stack.Rarity);
            int half = _config.GroundItemDrawHalfSize;
            int size = _config.GroundItemDrawSize;
            Raylib.DrawRectangle((int)pos.X - half, (int)pos.Y - half, size, size, color);
        }

        // Сундуки — спрайтовые с анимацией открытия
        var chestTex = _assets.GetTexture("doors_chest");
        float scale = _config.RenderScale;

        _world.QueryInto<Chest, Position>(_chestBuffer);
        for (int i = 0; i < _chestBuffer.Count; i++)
        {
            int id = _chestBuffer[i];
            ref var chest = ref _world.Get<Chest>(id);
            ref var pos = ref _world.Get<Position>(id);

            // FOV check
            int tx = (int)((pos.X + ts / 2f) / ts);
            int ty = (int)((pos.Y + ts / 2f) / ts);
            if (map.InBounds(tx, ty) && map.Tiles[tx, ty].Visibility < 2)
                continue;

            // Обновляем анимацию открытия
            if (chest.Opened && !chest.AnimDone)
            {
                chest.AnimTimer += dt;
                if (chest.AnimTimer >= _config.ChestOpenFrameDuration)
                {
                    chest.AnimTimer -= _config.ChestOpenFrameDuration;
                    chest.AnimFrame++;
                    if (chest.AnimFrame >= _config.ChestOpenFrameCount)
                    {
                        chest.AnimFrame = (byte)(_config.ChestOpenFrameCount - 1);
                        chest.AnimDone = true;
                    }
                }
            }

            var src = TileAtlas.GetChestSource(chest.AnimFrame, chest.Opened);
            float destW = _config.ChestSpriteSize * scale;
            float destH = _config.ChestSpriteSize * scale;
            float destX = pos.X + ts / 2f - destW / 2f;
            float destY = pos.Y + ts - destH;
            var dest = new Rectangle(destX, destY, destW, destH);
            Raylib.DrawTexturePro(chestTex, src, dest, System.Numerics.Vector2.Zero, 0f, Color.White);
        }
    }

    private static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => new Color(230, 230, 230, 255),
            ItemRarity.Uncommon => new Color(90, 220, 110, 255),
            ItemRarity.Rare => new Color(90, 140, 240, 255),
            ItemRarity.Epic => new Color(210, 80, 220, 255),
            ItemRarity.Legendary => new Color(245, 200, 70, 255),
            _ => Color.White
        };
    }
}
