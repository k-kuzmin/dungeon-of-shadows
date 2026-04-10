using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Рисует дебаг-оверлей: сетку и коллайдеры. World-space.
/// </summary>
public class DebugRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly List<int> _colliderBuffer = new();

    public RenderPhase Phase => RenderPhase.World;

    private static readonly Color GridColor = new(255, 255, 255, 30);

    public DebugRenderSystem(GameContext ctx, World world, GameConfig config)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
    }

    public void Tick(float dt)
    {
        if (!_ctx.DebugMode) return;

        var world = _world;
        var map = _ctx.Map;
        int ts = _config.ScaledTileSize;

        // Сетка
        for (int x = 0; x <= map.Width; x++)
            Raylib.DrawLine(x * ts, 0, x * ts, map.Height * ts, GridColor);
        for (int y = 0; y <= map.Height; y++)
            Raylib.DrawLine(0, y * ts, map.Width * ts, y * ts, GridColor);

        // Коллайдеры
        world.QueryInto<Collider, Position>(_colliderBuffer);
        foreach (int id in _colliderBuffer)
        {
            ref var pos = ref world.Get<Position>(id);
            ref var col = ref world.Get<Collider>(id);

            Raylib.DrawRectangleLines(
                (int)(pos.X + col.OffsetX),
                (int)(pos.Y + col.OffsetY),
                (int)col.Width,
                (int)col.Height,
                Color.Green
            );
        }
    }
}
