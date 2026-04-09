using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.UI;

/// <summary>
/// Статический хелпер отрисовки оверлея управления на старте игры.
/// </summary>
public static class UiControlsOverlay
{
    private static readonly (string Key, string Action)[] Lines =
    [
        ("WASD / Arrows", "Move"),
        ("Left Click", "Melee attack"),
        ("Right Click", "Cast spell"),
        ("Z / X", "Switch spell slot"),
        ("Mouse Wheel", "Switch spell slot"),
        ("1 - 4", "Use quick slot item"),
        ("L / Shift", "Dash"),
        ("E / Space", "Descend stairs"),
        ("I", "Inventory"),
        ("Tab", "Full map"),
        ("F3", "Debug mode"),
        ("Esc", "Quit"),
    ];

    public static void Draw(GameConfig config, UiTheme theme, TextMeasureCache textCache)
    {
        int w = config.ControlsOverlayWidth;
        int h = config.ControlsOverlayHeight;
        int x = config.ScreenWidth / 2 - w / 2;
        int y = config.ScreenHeight / 2 - h / 2;

        // Затемнение фона
        Raylib.DrawRectangle(0, 0, config.ScreenWidth, config.ScreenHeight, new Color(0, 0, 0, 160));

        // Панель
        var rect = new UiRect(x, y, w, h);
        UiDraw.Panel(rect, theme.InvPanelBg, theme.InvPanelBorder);

        // Заголовок
        int titleFontSize = config.ControlsOverlayTitleFontSize;
        string title = "Controls";
        int titleW = textCache.Measure(title, titleFontSize);
        Raylib.DrawText(title, x + w / 2 - titleW / 2, y + 14, titleFontSize, theme.TextWhite);

        // Строки управления: две колонки
        int lineFontSize = config.ControlsOverlayLineFontSize;
        int lineH = config.ControlsOverlayLineHeight;
        int padX = config.ControlsOverlayPaddingX;
        int keyColW = config.ControlsOverlayKeyColumnWidth;
        int lineY = y + config.ControlsOverlayPaddingTop;

        for (int i = 0; i < Lines.Length; i++)
        {
            var (key, action) = Lines[i];
            Raylib.DrawText(key, x + padX, lineY, lineFontSize, theme.SlotActive);
            Raylib.DrawText(action, x + padX + keyColW, lineY, lineFontSize, theme.HintColor);
            lineY += lineH;
        }

        // Хинт внизу
        int hintFontSize = config.ControlsOverlayHintFontSize;
        string hint = "[Enter] Start";
        int hintW = textCache.Measure(hint, hintFontSize);
        Raylib.DrawText(hint, x + w / 2 - hintW / 2, y + h - 28, hintFontSize, theme.HintColor);
    }
}
