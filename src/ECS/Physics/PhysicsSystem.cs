using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Physics.Systems;

public class PhysicsSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly List<int> _queryBuffer = new();

    public PhysicsSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _ctx.World;
        var map = _ctx.Map;
        int tileSize = _ctx.Config.ScaledTileSize;

        world.QueryInto<Position, Velocity>(_queryBuffer);
        foreach (int id in _queryBuffer)
        {
            ref var pos = ref world.Get<Position>(id);
            ref var vel = ref world.Get<Velocity>(id);

            if (vel.X == 0 && vel.Y == 0) continue;

            if (world.Has<Collider>(id))
            {
                ref var col = ref world.Get<Collider>(id);

                float newX = pos.X + vel.X * dt;
                if (!CollidesWithWalls(map, newX, pos.Y, ref col, tileSize))
                    pos.X = newX;

                float newY = pos.Y + vel.Y * dt;
                if (!CollidesWithWalls(map, pos.X, newY, ref col, tileSize))
                    pos.Y = newY;
            }
            else
            {
                pos.X += vel.X * dt;
                pos.Y += vel.Y * dt;
            }
        }
    }

    private static bool CollidesWithWalls(TileMap map, float posX, float posY, ref Collider col, int tileSize)
    {
        float left = posX + col.OffsetX;
        float top = posY + col.OffsetY;
        float right = left + col.Width;
        float bottom = top + col.Height;

        int minTX = (int)(left / tileSize);
        int minTY = (int)(top / tileSize);
        int maxTX = (int)((right - 0.01f) / tileSize);
        int maxTY = (int)((bottom - 0.01f) / tileSize);

        for (int tx = minTX; tx <= maxTX; tx++)
            for (int ty = minTY; ty <= maxTY; ty++)
                if (!map.IsWalkable(tx, ty))
                    return true;

        return false;
    }
}
