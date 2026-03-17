using Raylib_cs;
using DungeonOfShadows.ECS;

namespace DungeonOfShadows.Core;

public class Game
{
    private readonly GameConfig _config;
    private readonly GameContext _ctx;
    private readonly IReadOnlyList<IStartable> _startables;
    private readonly IReadOnlyList<ITickable> _tickables;
    private readonly IReadOnlyList<IDisposable> _disposables;

    public Game(
        GameConfig config,
        GameContext ctx,
        IEnumerable<IStartable> startables,
        IEnumerable<ITickable> tickables,
        IEnumerable<IDisposable> disposables)
    {
        _config = config;
        _ctx = ctx;
        _startables = startables.ToList();
        _tickables = tickables.ToList();
        _disposables = disposables.ToList();
    }

    public void Run()
    {
        Raylib.InitWindow(_config.ScreenWidth, _config.ScreenHeight, _config.Title);
        Raylib.SetTargetFPS(_config.TargetFPS);

        // Lifecycle: Start
        for (int i = 0; i < _startables.Count; i++)
            _startables[i].Start();

        // Lifecycle: Tick
        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            if (Raylib.IsKeyPressed(KeyboardKey.F3))
                _ctx.DebugMode = !_ctx.DebugMode;

            if (Raylib.IsKeyPressed(KeyboardKey.Tab))
            {
                _ctx.ShowFullMap = !_ctx.ShowFullMap;
                _ctx.State = (_ctx.ShowFullMap || _ctx.ShowInventory) ? GameState.Paused : GameState.Playing;
            }

            for (int i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick(dt);
        }

        // Lifecycle: Dispose
        for (int i = 0; i < _disposables.Count; i++)
            _disposables[i].Dispose();

        Raylib.CloseWindow();
    }
}
