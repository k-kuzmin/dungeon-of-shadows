using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;
using DungeonOfShadows.ECS.Magic.Components;
using DungeonOfShadows.UI;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Рисует HUD: FPS, этаж, дебаг-метку, мини-карту, полноэкранную карту, HP/MP игрока. Screen-space.
/// </summary>
public class HudRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly UiTheme _theme;
    private readonly TextMeasureCache _textCache;
    private readonly List<int> _queryBuffer = new();
    private int _cachedFloor = -1;
    private string _cachedFloorText = "";
    private int _cachedHp = -1;
    private int _cachedMaxHp = -1;
    private string _cachedHpText = "";
    private int _cachedHpTextW = 0;
    private int _cachedMp = -1;
    private int _cachedMaxMp = -1;
    private string _cachedMpText = "";
    private int _cachedMpTextW = 0;

    public RenderPhase Phase => RenderPhase.Screen;

    public HudRenderSystem(GameContext ctx, World world, GameConfig config, UiTheme theme, TextMeasureCache textCache)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
        _theme = theme;
        _textCache = textCache;
    }

    public void Tick(float dt)
    {
        // FPS
        Raylib.DrawFPS(10, 10);

        if (_ctx.DebugMode)
            Raylib.DrawText("DEBUG MODE (F3)", 10, _config.DebugLabelY, 16, Color.Yellow);

        // HP + MP бары
        DrawPlayerBars();

        // Номер этажа
        if (_cachedFloor != _ctx.CurrentFloor)
        {
            _cachedFloor = _ctx.CurrentFloor;
            _cachedFloorText = "Floor " + _cachedFloor;
        }
        Raylib.DrawText(_cachedFloorText, 10, _config.ScreenHeight - _config.FloorLabelBottomOffset,
            _config.FloorLabelFontSize, _theme.TextWhite);

        // Мини-карта (fullscreen карта — в FullscreenMapRenderSystem, рисуется поверх всего)
        if (!_ctx.ShowFullMap)
            DrawMinimap();
    }

    private void DrawPlayerBars()
    {
        _world.QueryInto<PlayerTag, Combat.Health>(_queryBuffer);
        if (_queryBuffer.Count == 0) return;

        int playerId = _queryBuffer[0];
        ref var health = ref _world.Get<Combat.Health>(playerId);

        int barX = _config.HpBarX;
        int barY = _ctx.DebugMode ? _config.DebugLabelY + 20 : _config.DebugLabelY;
        int barW = _config.HpBarWidth;

        // Layout: HP bar → gap → MP bar
        var layout = UiLayout.Column(barX, barY, _config.BarGap);

        // HP bar
        {
            var hpRect = layout.Take(barW, _config.HpBarHeight);
            float hpFrac = health.MaxHP > 0 ? (float)health.HP / health.MaxHP : 0f;
            Color hpColor = _theme.HpInterpolated(hpFrac);

            UiDraw.ProgressBar(hpRect, hpFrac, _theme.HpBarBg, hpColor, _theme.BarBorder);

            if (_cachedHp != health.HP || _cachedMaxHp != health.MaxHP)
            {
                _cachedHp = health.HP;
                _cachedMaxHp = health.MaxHP;
                _cachedHpText = _cachedHp + "/" + _cachedMaxHp;
                _cachedHpTextW = _textCache.Measure(_cachedHpText, _config.HpBarFontSize);
            }
            UiDraw.LabelCenteredCached(hpRect, _cachedHpTextW, _cachedHpText, _config.HpBarFontSize, _theme.TextWhite);
        }

        // MP bar
        {
            if (!_world.Has<Mana>(playerId)) return;

            ref var mana = ref _world.Get<Mana>(playerId);
            var mpRect = layout.Take(barW, _config.MpBarHeight);
            float mpFrac = mana.MaxMP > 0 ? (float)mana.MP / mana.MaxMP : 0f;

            UiDraw.ProgressBar(mpRect, mpFrac, _theme.HpBarBg, _theme.MpBarFill, _theme.BarBorder);

            if (_cachedMp != mana.MP || _cachedMaxMp != mana.MaxMP)
            {
                _cachedMp = mana.MP;
                _cachedMaxMp = mana.MaxMP;
                _cachedMpText = _cachedMp + "/" + _cachedMaxMp;
                _cachedMpTextW = _textCache.Measure(_cachedMpText, _config.MpBarFontSize);
            }
            UiDraw.LabelCenteredCached(mpRect, _cachedMpTextW, _cachedMpText, _config.MpBarFontSize, _theme.TextWhite);
        }
    }

    private void DrawMinimap()
    {
        var map = _ctx.Map;
        int minimapW = _config.MinimapWidth;
        int minimapH = (int)(minimapW * ((float)map.Height / map.Width));

        int screenX = _config.ScreenWidth - minimapW - _config.MinimapMargin;
        int screenY = _config.MinimapMargin;

        var borderRect = new UiRect(screenX - 2, screenY - 2, minimapW + 4, minimapH + 4);
        UiDraw.PanelFilled(borderRect, _theme.MinimapBorder);
        var bgRect = new UiRect(screenX, screenY, minimapW, minimapH);
        UiDraw.PanelFilled(bgRect, _theme.MinimapBg);

        float scaleX = (float)minimapW / map.Width;
        float scaleY = (float)minimapH / map.Height;

        DrawMapTiles(map, screenX, screenY, scaleX, scaleY);
        DrawPlayerMarker(screenX, screenY, scaleX, scaleY);
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
