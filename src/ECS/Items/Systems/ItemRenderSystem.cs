using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Items.Systems;

public class ItemRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly List<int> _itemBuffer = new();
    private readonly List<int> _chestBuffer = new();

    public RenderPhase Phase => RenderPhase.World;

    public ItemRenderSystem(GameContext ctx)
    {
        _ctx = ctx;
        _world = ctx.World;
    }

    public void Tick(float dt)
    {
        _world.QueryInto<ItemOnGround, Position>(_itemBuffer);
        for (int i = 0; i < _itemBuffer.Count; i++)
        {
            int id = _itemBuffer[i];
            if (!_world.Has<ItemStack>(id))
                continue;

            ref var pos = ref _world.Get<Position>(id);
            ref var stack = ref _world.Get<ItemStack>(id);
            Color color = GetRarityColor(stack.Rarity);
            int half = _ctx.Config.GroundItemDrawHalfSize;
            int size = _ctx.Config.GroundItemDrawSize;
            Raylib.DrawRectangle((int)pos.X - half, (int)pos.Y - half, size, size, color);
        }

        _world.QueryInto<Chest, Position>(_chestBuffer);
        for (int i = 0; i < _chestBuffer.Count; i++)
        {
            int id = _chestBuffer[i];
            ref var chest = ref _world.Get<Chest>(id);
            ref var pos = ref _world.Get<Position>(id);

            var color = chest.Opened
                ? new Color(120, 90, 50, 255)
                : new Color(170, 120, 50, 255);

            int offX = _ctx.Config.ChestDrawOffsetX;
            int offY = _ctx.Config.ChestDrawOffsetY;
            int w = _ctx.Config.ChestDrawWidth;
            int h = _ctx.Config.ChestDrawHeight;
            Raylib.DrawRectangle((int)pos.X - offX, (int)pos.Y - offY, w, h, color);
            Raylib.DrawRectangleLines((int)pos.X - offX, (int)pos.Y - offY, w, h, Color.Black);
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
