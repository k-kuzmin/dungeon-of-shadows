using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Systems;

public class RenderSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly List<int> _spriteBuffer = new();
    private readonly List<int> _colliderBuffer = new();

    public bool DebugMode { get; set; }

    private static readonly Color WallColor = new(50, 40, 60, 255);
    private static readonly Color FloorColor = new(90, 80, 70, 255);
    private static readonly Color FloorAltColor = new(85, 75, 65, 255);
    private static readonly Color GridColor = new(255, 255, 255, 30);

    public RenderSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        var world = _ctx.World;
        var map = _ctx.Map;
        var cam = _ctx.Camera;

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        Raylib.BeginMode2D(cam);

        DrawTiles(map, cam);
        DrawEntities(world);

        if (DebugMode)
            DrawDebug(world, map);

        Raylib.EndMode2D();

        DrawHUD();

        Raylib.EndDrawing();
    }

    private void DrawTiles(TileMap map, Camera2D cam)
    {
        int ts = _ctx.Config.ScaledTileSize;

        float left = cam.Target.X - cam.Offset.X / cam.Zoom;
        float top = cam.Target.Y - cam.Offset.Y / cam.Zoom;
        float right = cam.Target.X + cam.Offset.X / cam.Zoom;
        float bottom = cam.Target.Y + cam.Offset.Y / cam.Zoom;

        int minX = Math.Max(0, (int)(left / ts) - 1);
        int minY = Math.Max(0, (int)(top / ts) - 1);
        int maxX = Math.Min(map.Width - 1, (int)(right / ts) + 1);
        int maxY = Math.Min(map.Height - 1, (int)(bottom / ts) + 1);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var tile = map.Tiles[x, y];
                Color color = tile.Type switch
                {
                    TileType.Wall => WallColor,
                    TileType.Floor => ((x + y) % 2 == 0) ? FloorColor : FloorAltColor,
                    _ => Color.Black
                };
                Raylib.DrawRectangle(x * ts, y * ts, ts, ts, color);
            }
        }
    }

    private void DrawEntities(World world)
    {
        int scale = _ctx.Config.RenderScale;

        world.QueryInto<Sprite, Position>(_spriteBuffer);
        foreach (int id in _spriteBuffer)
        {
            ref var pos = ref world.Get<Position>(id);
            ref var sprite = ref world.Get<Sprite>(id);

            Raylib.DrawRectangle(
                (int)pos.X, (int)pos.Y,
                sprite.Width * scale,
                sprite.Height * scale,
                sprite.Tint
            );
        }
    }

    private void DrawDebug(World world, TileMap map)
    {
        int ts = _ctx.Config.ScaledTileSize;

        for (int x = 0; x <= map.Width; x++)
            Raylib.DrawLine(x * ts, 0, x * ts, map.Height * ts, GridColor);
        for (int y = 0; y <= map.Height; y++)
            Raylib.DrawLine(0, y * ts, map.Width * ts, y * ts, GridColor);

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

    private void DrawHUD()
    {
        Raylib.DrawFPS(10, 10);

        if (DebugMode)
            Raylib.DrawText("DEBUG MODE (F3)", 10, 30, 16, Color.Yellow);
    }
}
