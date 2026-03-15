using Raylib_cs;
using DungeonOfShadows.Dungeon;
using DungeonOfShadows.ECS;
using DungeonOfShadows.ECS.Systems;

namespace DungeonOfShadows.Core;

public class Game
{
    private readonly GameConfig _config;
    private readonly GameContext _ctx;
    private readonly IReadOnlyList<ITickable> _tickables;
    private readonly RenderSystem _renderSystem;

    public Game(
        GameConfig config,
        GameContext ctx,
        IEnumerable<ITickable> tickables,
        RenderSystem renderSystem)
    {
        _config = config;
        _ctx = ctx;
        _tickables = tickables.ToList();
        _renderSystem = renderSystem;
    }

    public void Run()
    {
        Raylib.InitWindow(_config.ScreenWidth, _config.ScreenHeight, _config.Title);
        Raylib.SetTargetFPS(_config.TargetFPS);

        Init();

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            if (Raylib.IsKeyPressed(KeyboardKey.F3))
                _renderSystem.DebugMode = !_renderSystem.DebugMode;

            if (_ctx.State == GameState.Playing)
            {
                for (int i = 0; i < _tickables.Count; i++)
                    _tickables[i].Tick(dt);
            }
        }

        Raylib.CloseWindow();
    }

    private void Init()
    {
        _ctx.Map = TileMap.CreateTestMap();
        SpawnPlayer();
        SnapCameraToPlayer();
    }

    private void SpawnPlayer()
    {
        var world = _ctx.World;
        int ts = _config.ScaledTileSize;
        int playerId = world.CreateEntity();

        float spawnX = 4 * ts;
        float spawnY = 3 * ts;

        world.Add(playerId, new Position(spawnX, spawnY));
        world.Add(playerId, new Velocity(0, 0));
        world.Add(playerId, new Sprite(new Color(60, 180, 75, 255)));
        world.Add(playerId, new Collider(
            ts * 0.8f, ts * 0.8f,
            ts * 0.1f, ts * 0.1f
        ));
        world.Add(playerId, new PlayerTag(_config.PlayerSpeed));
    }

    private void SnapCameraToPlayer()
    {
        var world = _ctx.World;
        var buffer = new List<int>();
        world.QueryInto<PlayerTag, Position>(buffer);
        if (buffer.Count > 0)
        {
            ref var pos = ref world.Get<Position>(buffer[0]);
            float half = _config.ScaledTileSize / 2f;
            _ctx.Camera = _ctx.Camera with
            {
                Target = new System.Numerics.Vector2(pos.X + half, pos.Y + half)
            };
        }
    }
}
