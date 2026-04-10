using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Обёртка рендеринга. Управляет BeginDrawing/EndDrawing и BeginMode2D/EndMode2D.
/// Внутри вызывает зарегистрированные IRenderTickable подсистемы в порядке регистрации.
/// </summary>
public class RenderSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly IReadOnlyList<IRenderTickable> _worldSystems;
    private readonly IReadOnlyList<IRenderTickable> _screenSystems;

    public RenderSystem(GameContext ctx, IEnumerable<IRenderTickable> renderSystems)
    {
        _ctx = ctx;
        var all = renderSystems.ToList();
        _worldSystems = all.Where(s => s.Phase == RenderPhase.World).ToList();
        _screenSystems = all.Where(s => s.Phase == RenderPhase.Screen).ToList();
    }

    public void Tick(float dt)
    {
        var cam = _ctx.Camera;

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        Raylib.BeginMode2D(cam);
        for (int i = 0; i < _worldSystems.Count; i++)
            _worldSystems[i].Tick(dt);
        Raylib.EndMode2D();

        for (int i = 0; i < _screenSystems.Count; i++)
            _screenSystems[i].Tick(dt);

        Raylib.EndDrawing();
    }
}
