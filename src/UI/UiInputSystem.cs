using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS;

namespace DungeonOfShadows.UI;

/// <summary>
/// Сбрасывает per-frame UI state и обрабатывает modal keyboard input.
/// Регистрируется ПЕРЕД gameplay input системами.
/// </summary>
public class UiInputSystem : ITickable
{
    private readonly UiContext _uiCtx;

    public UiInputSystem(UiContext uiCtx)
    {
        _uiCtx = uiCtx;
    }

    public void Tick(float dt)
    {
        _uiCtx.ClearFrame();

        // Controls overlay (приоритет — блокирует всё до закрытия)
        if (_uiCtx.ControlsOverlayActive)
        {
            _uiCtx.InputConsumed = true;
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                _uiCtx.CloseControlsOverlay();
            return;
        }

        // SelectionModal (приоритет над стандартной модалкой)
        if (_uiCtx.HasSelectionModal)
        {
            _uiCtx.InputConsumed = true;
            var sel = _uiCtx.SelectionModal!;

            if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
                sel.MoveUp();
            else if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
                sel.MoveDown();
            else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                int idx = sel.SelectedIndex;
                var onConfirm = sel.OnConfirm;
                _uiCtx.CloseSelectionModal();
                onConfirm?.Invoke(idx);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                var onCancel = sel.OnCancel;
                _uiCtx.CloseSelectionModal();
                onCancel?.Invoke();
            }
            return;
        }

        if (!_uiCtx.HasStandardModal) return;

        // Стандартная модалка блокирует весь input
        _uiCtx.InputConsumed = true;

        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            var modal = _uiCtx.TopModal;
            _uiCtx.PopModal();
            modal.OnCancel?.Invoke();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            var modal = _uiCtx.TopModal;
            _uiCtx.PopModal();
            modal.OnConfirm?.Invoke();
        }
    }
}
