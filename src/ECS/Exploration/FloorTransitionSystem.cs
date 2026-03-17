using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Exploration.Systems;

/// <summary>
/// Детектит стояние игрока на лестнице и нажатие E/Space.
/// Устанавливает флаг FloorTransitionRequested — обработку выполняет FloorLifecycleSystem.
/// </summary>
public class FloorTransitionSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly List<int> _queryBuffer = new();

    public FloorTransitionSystem(GameContext ctx, World world, GameConfig config)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _world;
        var map = _ctx.Map;
        int ts = _config.ScaledTileSize;

        world.QueryInto<PlayerTag, Position>(_queryBuffer);
        if (_queryBuffer.Count == 0) return;

        int playerId = _queryBuffer[0];
        ref var pos = ref world.Get<Position>(playerId);

        int tx = (int)((pos.X + ts / 2f) / ts);
        int ty = (int)((pos.Y + ts / 2f) / ts);

        if (!map.InBounds(tx, ty)) return;
        if (map.Tiles[tx, ty].Type != TileType.StairDown) return;

        if (!Raylib.IsKeyPressed(KeyboardKey.E) && !Raylib.IsKeyPressed(KeyboardKey.Space))
            return;

        _ctx.FloorTransitionRequested = true;
    }
}
