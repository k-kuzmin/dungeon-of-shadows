using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.UI;

/// <summary>
/// Статический хелпер отрисовки модального окна.
/// </summary>
public static class UiModal
{
    public static void Draw(ModalDescriptor modal, GameConfig config, UiTheme theme, TextMeasureCache textCache)
    {
        int w = config.ModalWidth;
        int h = config.ModalHeight;
        int x = config.ScreenWidth / 2 - w / 2;
        int y = config.ScreenHeight / 2 - h / 2;

        // Затемнение фона
        Raylib.DrawRectangle(0, 0, config.ScreenWidth, config.ScreenHeight, new Color(0, 0, 0, 140));

        // Панель
        var rect = new UiRect(x, y, w, h);
        UiDraw.Panel(rect, theme.InvPanelBg, theme.InvPanelBorder);

        // Заголовок
        int titleW = textCache.Measure(modal.Title, config.ModalTitleFontSize);
        Raylib.DrawText(modal.Title, x + w / 2 - titleW / 2, y + 16, config.ModalTitleFontSize, theme.TextWhite);

        // Текст
        if (!string.IsNullOrEmpty(modal.Text))
        {
            int textW = textCache.Measure(modal.Text, config.ModalTextFontSize);
            Raylib.DrawText(modal.Text, x + w / 2 - textW / 2, y + 52, config.ModalTextFontSize, theme.HintColor);
        }

        // Кнопки: [Confirm]    [Cancel]
        int btnY = y + h - 44;
        string confirmLabel = modal.ConfirmLabel ?? "OK";
        string cancelLabel = modal.CancelLabel ?? "Cancel";

        int confirmW = textCache.Measure(confirmLabel, config.ModalButtonFontSize);
        int cancelW = textCache.Measure(cancelLabel, config.ModalButtonFontSize);

        // Confirm — слева от центра
        Raylib.DrawText(confirmLabel, x + w / 2 - confirmW - 20, btnY, config.ModalButtonFontSize, theme.SlotActive);
        // Cancel — справа от центра
        Raylib.DrawText(cancelLabel, x + w / 2 + 20, btnY, config.ModalButtonFontSize, theme.HintColor);

        // Хинт
        string hint = "[Enter] Confirm    [Esc] Cancel";
        int hintW = textCache.Measure(hint, 12);
        Raylib.DrawText(hint, x + w / 2 - hintW / 2, y + h - 20, 12, theme.HintColor);
    }
}
