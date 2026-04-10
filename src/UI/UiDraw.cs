using Raylib_cs;

namespace DungeonOfShadows.UI;

/// <summary>
/// Статические методы отрисовки UI виджетов поверх Raylib.
/// Все методы — value-type params, zero alloc.
/// </summary>
public static class UiDraw
{
    public static void Panel(UiRect rect, Color bg, Color border)
    {
        Raylib.DrawRectangle(rect.X, rect.Y, rect.W, rect.H, bg);
        Raylib.DrawRectangleLines(rect.X, rect.Y, rect.W, rect.H, border);
    }

    public static void PanelFilled(UiRect rect, Color bg)
    {
        Raylib.DrawRectangle(rect.X, rect.Y, rect.W, rect.H, bg);
    }

    public static void ProgressBar(UiRect rect, float fraction, Color bg, Color fill, Color border)
    {
        Raylib.DrawRectangle(rect.X, rect.Y, rect.W, rect.H, bg);
        int fillW = (int)(rect.W * fraction);
        if (fillW > 0)
            Raylib.DrawRectangle(rect.X, rect.Y, fillW, rect.H, fill);
        Raylib.DrawRectangleLines(rect.X, rect.Y, rect.W, rect.H, border);
    }

    public static void ProgressBarVertical(UiRect rect, float fraction, Color bg, Color fill)
    {
        Raylib.DrawRectangle(rect.X, rect.Y, rect.W, rect.H, bg);
        int fillH = (int)(rect.H * fraction);
        if (fillH > 0)
            Raylib.DrawRectangle(rect.X, rect.Y + rect.H - fillH, rect.W, fillH, fill);
    }

    public static void Slot(UiRect rect, Color bg, Color border, bool selected, Color selectedColor)
    {
        Raylib.DrawRectangle(rect.X, rect.Y, rect.W, rect.H, bg);
        Raylib.DrawRectangleLines(rect.X, rect.Y, rect.W, rect.H, selected ? selectedColor : border);
    }

    public static void Label(int x, int y, string text, int fontSize, Color color)
    {
        Raylib.DrawText(text, x, y, fontSize, color);
    }

    public static void LabelCentered(UiRect rect, string text, int fontSize, Color color, TextMeasureCache cache)
    {
        int w = cache.Measure(text, fontSize);
        Raylib.DrawText(text, rect.X + rect.W / 2 - w / 2, rect.Y + rect.H / 2 - fontSize / 2, fontSize, color);
    }

    /// <summary>
    /// Отрисовка центрированного текста с уже посчитанной шириной (без обращения к кэшу).
    /// </summary>
    public static void LabelCenteredCached(UiRect rect, int cachedWidth, string text, int fontSize, Color color)
    {
        Raylib.DrawText(text, rect.X + rect.W / 2 - cachedWidth / 2, rect.Y + rect.H / 2 - fontSize / 2, fontSize, color);
    }

    public static void Tooltip(UiRect rect, Color bg, Color border)
    {
        Raylib.DrawRectangle(rect.X, rect.Y, rect.W, rect.H, bg);
        Raylib.DrawRectangleLines(rect.X, rect.Y, rect.W, rect.H, border);
    }

    public static void ItemIcon(UiRect rect, Texture2D tex, Rectangle src, Color tint, int padding = 2)
    {
        int iconSize = rect.W - padding * 2;
        var dest = new Rectangle(rect.X + padding, rect.Y + padding, iconSize, iconSize);
        Raylib.DrawTexturePro(tex, src, dest, System.Numerics.Vector2.Zero, 0f, tint);
    }
}
