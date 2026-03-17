using Raylib_cs;
using DungeonOfShadows.Dungeon;
using DungeonOfShadows.ECS;
using DungeonOfShadows.ECS.Combat;
using DungeonOfShadows.ECS.Items;
using DungeonOfShadows.ECS.Rendering;

namespace DungeonOfShadows.Core;

public class Game
{
    private readonly GameConfig _config;
    private readonly GameContext _ctx;
    private readonly IReadOnlyList<ITickable> _tickables;

    public Game(
        GameConfig config,
        GameContext ctx,
        IEnumerable<ITickable> tickables)
    {
        _config = config;
        _ctx = ctx;
        _tickables = tickables.ToList();
    }

    public void Run()
    {
        Raylib.InitWindow(_config.ScreenWidth, _config.ScreenHeight, _config.Title);
        Raylib.SetTargetFPS(_config.TargetFPS);
        _ctx.Textures.LoadAll();

        Init();

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            // Глобальные переключатели
            if (Raylib.IsKeyPressed(KeyboardKey.F3))
                _ctx.DebugMode = !_ctx.DebugMode;

            if (Raylib.IsKeyPressed(KeyboardKey.Tab))
            {
                _ctx.ShowFullMap = !_ctx.ShowFullMap;
                _ctx.State = (_ctx.ShowFullMap || _ctx.ShowInventory) ? GameState.Paused : GameState.Playing;
            }

            // Все системы тикают всегда — каждая сама решает, реагировать ли на состояние
            for (int i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick(dt);
        }

        _ctx.Textures.UnloadAll();
        Raylib.CloseWindow();
    }

    private void Init()
    {
        _ctx.DungeonSeed = Environment.TickCount;
        _ctx.CurrentFloor = 1;

        var result = DungeonGenerator.Generate(1, _config, _ctx.DungeonSeed);
        _ctx.Map = result.Map;

        SpawnPlayer(result.SpawnRoom);
        EnemySpawner.SpawnEnemies(_ctx.World, _ctx.Map, _config, _ctx.CurrentFloor, _ctx.DungeonSeed);
        ChestSpawner.SpawnChests(_ctx.World, _ctx.Map, _config, _ctx.CurrentFloor, _ctx.DungeonSeed);
        DecorationObjectSpawner.SpawnObjects(_ctx.World, _ctx.Map, _config, _ctx.CurrentFloor, _ctx.DungeonSeed);
        SnapCameraToPlayer();
    }

    private void SpawnPlayer(Room spawnRoom)
    {
        var world = _ctx.World;
        int ts = _config.ScaledTileSize;
        int playerId = world.CreateEntity();

        float spawnX = spawnRoom.CenterX * ts;
        float spawnY = spawnRoom.CenterY * ts;

        world.Add(playerId, new Position(spawnX, spawnY));
        world.Add(playerId, new Velocity(0, 0));
        world.Add(playerId, new Sprite(new Raylib_cs.Color(60, 180, 75, 255)));
        world.Add(playerId, new Collider(
            ts * 0.8f, ts * 0.8f,
            ts * 0.1f, ts * 0.1f
        ));
        world.Add(playerId, new PlayerTag(_config.PlayerSpeed));
        world.Add(playerId, new Health(_config.PlayerBaseHP, _config.PlayerBaseHP));
        world.Add(playerId, new Stats(_config.PlayerBaseATK, _config.PlayerBaseDEF,
            _config.PlayerSpeed, _config.PlayerBaseCrit));
        world.Add(playerId, new Inventory(_config.InventorySlots));
        world.Add(playerId, new Equipment());
        world.Add(playerId, new QuickSlots(init: true));
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
