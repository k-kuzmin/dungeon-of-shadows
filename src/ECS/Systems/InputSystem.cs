using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Systems;

public class InputSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly List<int> _queryBuffer = new();

    public InputSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _ctx.World;
        int scaledTile = _ctx.Config.ScaledTileSize;

        world.QueryInto<PlayerTag, Velocity>(_queryBuffer);
        foreach (int id in _queryBuffer)
        {
            ref var player = ref world.Get<PlayerTag>(id);
            ref var vel = ref world.Get<Velocity>(id);

            float moveX = 0;
            float moveY = 0;

            if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up)) moveY -= 1;
            if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down)) moveY += 1;
            if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) moveX -= 1;
            if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) moveX += 1;

            // Normalize diagonal movement
            float len = MathF.Sqrt(moveX * moveX + moveY * moveY);
            if (len > 0)
            {
                moveX /= len;
                moveY /= len;
                player.FacingX = (int)MathF.Round(moveX);
                player.FacingY = (int)MathF.Round(moveY);
            }

            float speed = player.Speed * scaledTile;
            vel.X = moveX * speed;
            vel.Y = moveY * speed;
        }
    }
}
