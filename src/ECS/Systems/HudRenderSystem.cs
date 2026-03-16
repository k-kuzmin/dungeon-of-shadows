using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Systems;

/// <summary>
/// Рисует HUD: FPS, этаж, дебаг-метку, мини-карту и полноэкранную карту. Screen-space.
/// </summary>
public class HudRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly List<int> _queryBuffer = new();

    public RenderPhase Phase => RenderPhase.Screen;

    // Цвета мини-карты
    private static readonly Color MinimapBg = new(0, 0, 0, 200);
    private static readonly Color MinimapWall = new(60, 50, 70, 255);
    private static readonly Color MinimapFloor = new(120, 110, 100, 255);
    private static readonly Color MinimapExplored = new(60, 55, 50, 255);
    private static readonly Color MinimapStair = new(220, 180, 50, 255);
    private static readonly Color MinimapPlayer = new(60, 220, 75, 255);
    private static readonly Color MinimapBorder = new(150, 140, 130, 200);

    public HudRenderSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        var config = _ctx.Config;

        // FPS
        Raylib.DrawFPS(10, 10);

        if (_ctx.DebugMode)
            Raylib.DrawText("DEBUG MODE (F3)", 10, 30, 16, Color.Yellow);

        // Номер этажа
        Raylib.DrawText($"Floor {_ctx.CurrentFloor}", 10, config.ScreenHeight - 30, 20, Color.White);

        // Полноэкранная карта (при паузе) или мини-карта
        if (_ctx.ShowFullMap)
            DrawFullscreenMap();
        else
            DrawMinimap();
    }

    private void DrawMinimap()
    {
        var map = _ctx.Map;
        var config = _ctx.Config;

        int minimapW = config.MinimapWidth;
        int minimapH = (int)(minimapW * ((float)map.Height / map.Width));

        int screenX = config.ScreenWidth - minimapW - config.MinimapMargin;
        int screenY = config.MinimapMargin;

        Raylib.DrawRectangle(screenX - 2, screenY - 2, minimapW + 4, minimapH + 4, MinimapBorder);
        Raylib.DrawRectangle(screenX, screenY, minimapW, minimapH, MinimapBg);

        float scaleX = (float)minimapW / map.Width;
        float scaleY = (float)minimapH / map.Height;

        DrawMapTiles(map, screenX, screenY, scaleX, scaleY);
        DrawPlayerMarker(screenX, screenY, scaleX, scaleY);
    }

    private void DrawFullscreenMap()
    {
        var map = _ctx.Map;
        var config = _ctx.Config;

        Raylib.DrawRectangle(0, 0, config.ScreenWidth, config.ScreenHeight, new Color(0, 0, 0, 180));

        int margin = 40;
        int availW = config.ScreenWidth - margin * 2;
        int availH = config.ScreenHeight - margin * 2 - 40;

        float scaleX = (float)availW / map.Width;
        float scaleY = (float)availH / map.Height;
        float scale = MathF.Min(scaleX, scaleY);

        int mapPixelW = (int)(map.Width * scale);
        int mapPixelH = (int)(map.Height * scale);

        int screenX = (config.ScreenWidth - mapPixelW) / 2;
        int screenY = margin + 30;

        Raylib.DrawRectangle(screenX - 2, screenY - 2, mapPixelW + 4, mapPixelH + 4, MinimapBorder);
        Raylib.DrawRectangle(screenX, screenY, mapPixelW, mapPixelH, MinimapBg);

        DrawMapTiles(map, screenX, screenY, scale, scale);
        DrawPlayerMarker(screenX, screenY, scale, scale);

        string title = $"Floor {_ctx.CurrentFloor}  [Tab to close]";
        int titleW = Raylib.MeasureText(title, 20);
        Raylib.DrawText(title, config.ScreenWidth / 2 - titleW / 2, margin, 20, Color.White);
    }

    private void DrawMapTiles(TileMap map, int offsetX, int offsetY, float scaleX, float scaleY)
    {
        int pixW = Math.Max(1, (int)MathF.Ceiling(scaleX));
        int pixH = Math.Max(1, (int)MathF.Ceiling(scaleY));

        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                var tile = map.Tiles[x, y];
                if (tile.Visibility == 0) continue;

                int px = offsetX + (int)(x * scaleX);
                int py = offsetY + (int)(y * scaleY);

                Color color;
                if (tile.Type == TileType.StairDown && tile.Visibility >= 1)
                    color = MinimapStair;
                else if (tile.Visibility == 1)
                    color = tile.Type == TileType.Wall ? MinimapWall : MinimapExplored;
                else
                    color = tile.Type == TileType.Wall ? MinimapWall : MinimapFloor;

                Raylib.DrawRectangle(px, py, pixW, pixH, color);
            }
        }
    }

    private void DrawPlayerMarker(int offsetX, int offsetY, float scaleX, float scaleY)
    {
        var world = _ctx.World;
        int ts = _ctx.Config.ScaledTileSize;

        world.QueryInto<PlayerTag, Position>(_queryBuffer);
        if (_queryBuffer.Count == 0) return;

        ref var pos = ref world.Get<Position>(_queryBuffer[0]);
        float playerTX = (pos.X + ts / 2f) / ts;
        float playerTY = (pos.Y + ts / 2f) / ts;

        int px = offsetX + (int)(playerTX * scaleX);
        int py = offsetY + (int)(playerTY * scaleY);

        int markerSize = Math.Max(3, (int)(scaleX * 1.5f));
        Raylib.DrawRectangle(px - markerSize / 2, py - markerSize / 2,
            markerSize, markerSize, MinimapPlayer);
    }
}
