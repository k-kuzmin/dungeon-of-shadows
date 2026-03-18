using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;
using DungeonOfShadows.ECS.Combat;
using DungeonOfShadows.ECS.Items;
using DungeonOfShadows.ECS.Rendering;

namespace DungeonOfShadows.ECS.Exploration.Systems;

/// <summary>
/// Управляет жизненным циклом этажа: генерация карты, спавн сущностей,
/// переходы между этажами. Единственное место, где происходит создание уровня.
/// </summary>
public class FloorLifecycleSystem : IStartable, ITickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly List<int> _queryBuffer = new();

    public FloorLifecycleSystem(GameContext ctx, World world, GameConfig config)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
    }

    /// <summary>
    /// Первичная инициализация: генерация первого этажа и спавн всех сущностей.
    /// </summary>
    public void Start()
    {
        _ctx.DungeonSeed = Environment.TickCount;
        _ctx.CurrentFloor = 1;

        var result = DungeonGenerator.Generate(1, _config, _ctx.DungeonSeed);
        _ctx.Map = result.Map;

        SpawnPlayer(result.SpawnRoom);
        SpawnFloorEntities();
        SnapCameraToPlayer();
    }

    /// <summary>
    /// Проверяет запрос на переход этажа и выполняет его.
    /// </summary>
    public void Tick(float dt)
    {
        if (!_ctx.FloorTransitionRequested) return;
        _ctx.FloorTransitionRequested = false;

        DescendFloor();
    }

    private void DescendFloor()
    {
        var world = _world;

        // Находим игрока
        world.QueryInto<PlayerTag, Position>(_queryBuffer);
        if (_queryBuffer.Count == 0) return;
        int playerId = _queryBuffer[0];

        // Уничтожаем всех не-игроков (переиспользуем буфер)
        _queryBuffer.Clear();
        foreach (int id in world.AllEntities)
        {
            if (id == playerId || !world.IsAlive(id)) continue;
            _queryBuffer.Add(id);
        }
        foreach (int id in _queryBuffer)
            world.DestroyEntity(id);

        // Генерация нового этажа
        _ctx.CurrentFloor++;
        var result = DungeonGenerator.Generate(_ctx.CurrentFloor, _config, _ctx.DungeonSeed);
        _ctx.Map = result.Map;

        // Перемещаем игрока в спавн-комнату
        int ts = _config.ScaledTileSize;
        ref var pos = ref world.Get<Position>(playerId);
        pos.X = result.SpawnRoom.CenterX * ts;
        pos.Y = result.SpawnRoom.CenterY * ts;

        ref var vel = ref world.Get<Velocity>(playerId);
        vel.X = 0;
        vel.Y = 0;

        SpawnFloorEntities();
    }

    private void SpawnFloorEntities()
    {
        var world = _world;
        int ts = _config.ScaledTileSize;

        EnemySpawner.SpawnEnemies(world, _ctx.Map, _config, _ctx.CurrentFloor, _ctx.DungeonSeed);
        ChestSpawner.SpawnChests(world, _ctx.Map, _config, _ctx.CurrentFloor, _ctx.DungeonSeed);

        // Собираем тайлы, занятые уже заспавненными entity (враги, сундуки),
        // чтобы декор не спавнился поверх них.
        var occupied = new HashSet<(int, int)>();
        world.QueryInto<Position>(_queryBuffer);
        for (int i = 0; i < _queryBuffer.Count; i++)
        {
            ref var pos = ref world.Get<Position>(_queryBuffer[i]);
            occupied.Add(((int)(pos.X / ts), (int)(pos.Y / ts)));
        }

        DecorationObjectSpawner.SpawnObjects(world, _ctx.Map, _config, _ctx.CurrentFloor, _ctx.DungeonSeed, occupied);
        TorchSpawner.SpawnTorches(world, _ctx.Map, _config);
    }

    private void SpawnPlayer(Room spawnRoom)
    {
        var world = _world;
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

        // Анимация героя
        var clips = CharacterAnimationBuilder.BuildHero(_config);
        var initialClip = clips["Idle_Down"];
        world.Add(playerId, new Animator(clips, "Idle_Down"));
        world.Add(playerId, new Animation(initialClip));
    }

    private void SnapCameraToPlayer()
    {
        var world = _world;
        world.QueryInto<PlayerTag, Position>(_queryBuffer);
        if (_queryBuffer.Count == 0) return;

        ref var pos = ref world.Get<Position>(_queryBuffer[0]);
        float half = _config.ScaledTileSize / 2f;
        _ctx.Camera = _ctx.Camera with
        {
            Target = new System.Numerics.Vector2(pos.X + half, pos.Y + half)
        };
    }
}
