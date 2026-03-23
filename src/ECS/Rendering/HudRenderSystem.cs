using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;
using DungeonOfShadows.ECS.Magic.Components;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Рисует HUD: FPS, этаж, дебаг-метку, мини-карту, полноэкранную карту, HP игрока. Screen-space.
/// </summary>
public class HudRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly List<int> _queryBuffer = new();
    private int _cachedFloor = -1;
    private string _cachedFloorText = "";
    private string _cachedFullscreenTitle = "";
    private int _cachedHp = -1;
    private int _cachedMaxHp = -1;
    private string _cachedHpText = "";
    private int _cachedMp = -1;
    private int _cachedMaxMp = -1;
    private string _cachedMpText = "";

    public RenderPhase Phase => RenderPhase.Screen;

    // Цвета мини-карты
    private static readonly Color MinimapBg = new(0, 0, 0, 200);
    private static readonly Color MinimapWall = new(60, 50, 70, 255);
    private static readonly Color MinimapFloor = new(120, 110, 100, 255);
    private static readonly Color MinimapExplored = new(60, 55, 50, 255);
    private static readonly Color MinimapStair = new(220, 180, 50, 255);
    private static readonly Color MinimapPlayer = new(60, 220, 75, 255);
    private static readonly Color MinimapBorder = new(150, 140, 130, 200);

    // Цвета HP бара
    private static readonly Color HpBarBg = new(40, 40, 40, 200);
    private static readonly Color HpBarFull = new(50, 200, 60, 255);
    private static readonly Color HpBarLow = new(200, 50, 50, 255);

    // Цвет MP бара
    private static readonly Color MpBarColor = new(30, 100, 220, 255);

    public HudRenderSystem(GameContext ctx, World world, GameConfig config)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
    }

    public void Tick(float dt)
    {
        // FPS
        Raylib.DrawFPS(10, 10);

        if (_ctx.DebugMode)
            Raylib.DrawText("DEBUG MODE (F3)", 10, 30, 16, Color.Yellow);

        // HP бар игрока
        DrawPlayerHpBar();

        // MP бар игрока
        DrawPlayerMpBar();

        // Номер этажа
        if (_cachedFloor != _ctx.CurrentFloor)
        {
            _cachedFloor = _ctx.CurrentFloor;
            _cachedFloorText = "Floor " + _cachedFloor;
            _cachedFullscreenTitle = "Floor " + _cachedFloor + "  [Tab to close]";
        }
        Raylib.DrawText(_cachedFloorText, 10, _config.ScreenHeight - 30, 20, Color.White);

        // Полноэкранная карта (при паузе) или мини-карта
        if (_ctx.ShowFullMap)
            DrawFullscreenMap();
        else
            DrawMinimap();
    }

    private void DrawPlayerHpBar()
    {
        _world.QueryInto<PlayerTag, Combat.Health>(_queryBuffer);
        if (_queryBuffer.Count == 0) return;

        ref var health = ref _world.Get<Combat.Health>(_queryBuffer[0]);

        int barX = 10;
        int barY = _ctx.DebugMode ? 50 : 30;
        int barW = 200;
        int barH = 16;

        float fraction = health.MaxHP > 0 ? (float)health.HP / health.MaxHP : 0f;

        // Интерполяция цвета от красного к зелёному
        var barColor = new Raylib_cs.Color(
            (byte)(HpBarLow.R + (HpBarFull.R - HpBarLow.R) * fraction),
            (byte)(HpBarLow.G + (HpBarFull.G - HpBarLow.G) * fraction),
            (byte)(HpBarLow.B + (HpBarFull.B - HpBarLow.B) * fraction),
            (byte)255
        );

        Raylib.DrawRectangle(barX, barY, barW, barH, HpBarBg);
        Raylib.DrawRectangle(barX, barY, (int)(barW * fraction), barH, barColor);
        Raylib.DrawRectangleLines(barX, barY, barW, barH, Color.White);

        if (_cachedHp != health.HP || _cachedMaxHp != health.MaxHP)
        {
            _cachedHp = health.HP;
            _cachedMaxHp = health.MaxHP;
            _cachedHpText = _cachedHp + "/" + _cachedMaxHp;
        }

        int textW = Raylib.MeasureText(_cachedHpText, 14);
        Raylib.DrawText(_cachedHpText, barX + barW / 2 - textW / 2, barY + 1, 14, Color.White);
    }

    private void DrawPlayerMpBar()
    {
        _world.QueryInto<PlayerTag, Mana>(_queryBuffer);
        if (_queryBuffer.Count == 0) return;

        ref var mana = ref _world.Get<Mana>(_queryBuffer[0]);

        int barX = 10;
        int barY = _ctx.DebugMode ? 70 : 50;
        int barW = 200;
        int barH = 12;

        float fraction = mana.MaxMP > 0 ? (float)mana.MP / mana.MaxMP : 0f;

        Raylib.DrawRectangle(barX, barY, barW, barH, HpBarBg);
        Raylib.DrawRectangle(barX, barY, (int)(barW * fraction), barH, MpBarColor);
        Raylib.DrawRectangleLines(barX, barY, barW, barH, Color.White);

        if (_cachedMp != mana.MP || _cachedMaxMp != mana.MaxMP)
        {
            _cachedMp = mana.MP;
            _cachedMaxMp = mana.MaxMP;
            _cachedMpText = _cachedMp + "/" + _cachedMaxMp;
        }

        int textW = Raylib.MeasureText(_cachedMpText, 12);
        Raylib.DrawText(_cachedMpText, barX + barW / 2 - textW / 2, barY + 0, 12, Color.White);
    }

    private void DrawMinimap()
    {
        var map = _ctx.Map;
        int minimapW = _config.MinimapWidth;
        int minimapH = (int)(minimapW * ((float)map.Height / map.Width));

        int screenX = _config.ScreenWidth - minimapW - _config.MinimapMargin;
        int screenY = _config.MinimapMargin;

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

        Raylib.DrawRectangle(screenX - 2, screenY - 2, mapPixelW + 4, mapPixelH + 4, MinimapBorder);
        Raylib.DrawRectangle(screenX, screenY, mapPixelW, mapPixelH, MinimapBg);

        DrawMapTiles(map, screenX, screenY, scale, scale);
        DrawPlayerMarker(screenX, screenY, scale, scale);

        int titleW = Raylib.MeasureText(_cachedFullscreenTitle, 20);
        Raylib.DrawText(_cachedFullscreenTitle, _config.ScreenWidth / 2 - titleW / 2, margin, 20, Color.White);
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
            markerSize, markerSize, MinimapPlayer);
    }
}
