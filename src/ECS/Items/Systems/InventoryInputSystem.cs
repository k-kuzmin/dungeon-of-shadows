using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Items.Systems;

public class InventoryInputSystem : ITickable
{
    private readonly GameContext _ctx;

    public InventoryInputSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.I))
        {
            _ctx.ShowInventory = !_ctx.ShowInventory;
            _ctx.State = (_ctx.ShowInventory || _ctx.ShowFullMap) ? GameState.Paused : GameState.Playing;
        }

        if (_ctx.UiMessageTimer > 0)
            _ctx.UiMessageTimer = MathF.Max(0, _ctx.UiMessageTimer - dt);
    }
}
