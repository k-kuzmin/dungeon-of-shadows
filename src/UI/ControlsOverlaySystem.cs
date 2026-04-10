using DungeonOfShadows.ECS;

namespace DungeonOfShadows.UI;

/// <summary>
/// Показывает оверлей управления при старте игры.
/// Ставит паузу до закрытия по Enter.
/// </summary>
public class ControlsOverlaySystem : IStartable
{
    private readonly UiContext _uiCtx;

    public ControlsOverlaySystem(UiContext uiCtx)
    {
        _uiCtx = uiCtx;
    }

    public void Start()
    {
        _uiCtx.OpenControlsOverlay();
    }
}
