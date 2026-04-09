using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.UI;

namespace DungeonOfShadows.ECS.Items.Systems;

public class InventoryInputSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly UiContext _uiCtx;

    public InventoryInputSystem(GameContext ctx, UiContext uiCtx)
    {
        _ctx = ctx;
        _uiCtx = uiCtx;
    }

    public void Tick(float dt)
    {
        if (!_uiCtx.InputConsumed)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.I))
            {
                _ctx.ShowInventory = !_ctx.ShowInventory;
                if (_ctx.State != GameState.Dead)
                    _ctx.State = (_ctx.ShowInventory || _ctx.ShowFullMap || _uiCtx.HasModal)
                        ? GameState.Paused : GameState.Playing;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Tab))
            {
                _ctx.ShowFullMap = !_ctx.ShowFullMap;
                if (_ctx.State != GameState.Dead)
                    _ctx.State = (_ctx.ShowInventory || _ctx.ShowFullMap || _uiCtx.HasModal)
                        ? GameState.Paused : GameState.Playing;
            }
        }

        if (_ctx.UiMessageTimer > 0)
            _ctx.UiMessageTimer = MathF.Max(0, _ctx.UiMessageTimer - dt);
    }
}
