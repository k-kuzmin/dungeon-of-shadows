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

        if (!_uiCtx.HasModal) return;

        // Модалка блокирует весь input
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
