using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;
using DungeonOfShadows.UI;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Полноэкранная карта. Регистрируется последней среди Screen-phase систем,
/// чтобы рисоваться поверх всех HUD элементов.
/// </summary>
public class FullscreenMapRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly UiTheme _theme;
    private readonly TextMeasureCache _textCache;
    private readonly List<int> _queryBuffer = new();

    private int _cachedFloor = -1;
    private string _cachedTitle = "";

    public RenderPhase Phase => RenderPhase.Screen;

    public FullscreenMapRenderSystem(GameContext ctx, World world, GameConfig config,
        UiTheme theme, TextMeasureCache textCache)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
        _theme = theme;
        _textCache = textCache;
    }

    public void Tick(float dt)
    {
        if (!_ctx.ShowFullMap) return;

        var map = _ctx.Map;

        // Затемнение
        Raylib.DrawRectangle(0, 0, _config.ScreenWidth, _config.ScreenHeight, new Color(0, 0, 0, 180));

        int margin = 40;
        int availW = _config.ScreenWidth - margin * 2;
        int availH = _config.ScreenHeight - margin * 2 - 40;

        float scaleX = (float)availW / map.Width;
        float scaleY = (float)availH / map.Height;
        float scale = MathF.Min(scaleX, scaleY);

        int mapPixelW = (int)(map.Width * scale);
        int mapPixelH = (int)(map.Height * scale);

        int screenX = (_config.ScreenWidth - mapPixelW) / 2;
        int screenY = margin + 30;

        var borderRect = new UiRect(screenX - 2, screenY - 2, mapPixelW + 4, mapPixelH + 4);
        UiDraw.PanelFilled(borderRect, _theme.MinimapBorder);
        var bgRect = new UiRect(screenX, screenY, mapPixelW, mapPixelH);
        UiDraw.PanelFilled(bgRect, _theme.MinimapBg);

        DrawMapTiles(map, screenX, screenY, scale, scale);
        DrawPlayerMarker(screenX, screenY, scale, scale);

        // Заголовок
        if (_cachedFloor != _ctx.CurrentFloor)
        {
            _cachedFloor = _ctx.CurrentFloor;
            _cachedTitle = "Floor " + _cachedFloor + "  [Tab to close]";
        }
        int titleW = _textCache.Measure(_cachedTitle, _config.FloorLabelFontSize);
        Raylib.DrawText(_cachedTitle, _config.ScreenWidth / 2 - titleW / 2, margin,
            _config.FloorLabelFontSize, _theme.TextWhite);
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
                    color = _theme.MinimapStair;
                else if (tile.Visibility == 1)
                    color = tile.Type == TileType.Wall ? _theme.MinimapWall : _theme.MinimapExplored;
                else
                    color = tile.Type == TileType.Wall ? _theme.MinimapWall : _theme.MinimapFloor;

                Raylib.DrawRectangle(px, py, pixW, pixH, color);
            }
        }
    }

    private void DrawPlayerMarker(int offsetX, int offsetY, float scaleX, float scaleY)
    {
        int ts = _config.ScaledTileSize;

        _world.QueryInto<PlayerTag, Position>(_queryBuffer);
        if (_queryBuffer.Count == 0) return;

        ref var pos = ref _world.Get<Position>(_queryBuffer[0]);
        float playerTX = (pos.X + ts / 2f) / ts;
        float playerTY = (pos.Y + ts / 2f) / ts;

        int px = offsetX + (int)(playerTX * scaleX);
        int py = offsetY + (int)(playerTY * scaleY);

        int markerSize = Math.Max(3, (int)(scaleX * 1.5f));
        Raylib.DrawRectangle(px - markerSize / 2, py - markerSize / 2,
            markerSize, markerSize, _theme.MinimapPlayer);
    }
}
