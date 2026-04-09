using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.UI;

/// <summary>
/// Статический хелпер отрисовки модалки выбора из N вариантов.
/// </summary>
public static class UiSelectionModal
{
    public static void Draw(SelectionModalDescriptor modal, GameConfig config, UiTheme theme, TextMeasureCache textCache)
    {
        int optionH = config.SelectionModalOptionHeight;
        int padTop = config.SelectionModalPaddingTop;
        int padX = config.SelectionModalPaddingX;
        int optionCount = modal.Options.Length;
        int contentH = padTop + optionCount * optionH + 32;
        int w = config.ModalWidth;
        int h = Math.Max(config.ModalHeight, contentH);
        int x = config.ScreenWidth / 2 - w / 2;
        int y = config.ScreenHeight / 2 - h / 2;

        // Затемнение фона
        Raylib.DrawRectangle(0, 0, config.ScreenWidth, config.ScreenHeight, new Color(0, 0, 0, 140));

        // Панель
        var rect = new UiRect(x, y, w, h);
        UiDraw.Panel(rect, theme.InvPanelBg, theme.InvPanelBorder);

        // Заголовок
        int titleFontSize = config.SelectionModalTitleFontSize;
        int titleW = textCache.Measure(modal.Title, titleFontSize);
        Raylib.DrawText(modal.Title, x + w / 2 - titleW / 2, y + 14, titleFontSize, theme.TextWhite);

        // Варианты
        int optFontSize = config.SelectionModalOptionFontSize;
        int optY = y + padTop;
        for (int i = 0; i < optionCount; i++)
        {
            bool selected = i == modal.SelectedIndex;

            if (selected)
            {
                Raylib.DrawRectangle(x + padX - 4, optY, w - padX * 2 + 8, optionH,
                    new Color(60, 60, 100, 180));
            }

            Color textColor = selected ? theme.SlotActive : theme.HintColor;
            string label = selected ? modal.SelectedLabels[i] : modal.NormalLabels[i];
            Raylib.DrawText(label, x + padX, optY + 4, optFontSize, textColor);

            optY += optionH;
        }

        // Хинт
        int hintFontSize = config.SelectionModalHintFontSize;
        string hint = "[W/S] Select    [Enter] Confirm    [Esc] Cancel";
        int hintW = textCache.Measure(hint, hintFontSize);
        Raylib.DrawText(hint, x + w / 2 - hintW / 2, y + h - 20, hintFontSize, theme.HintColor);
    }
}
