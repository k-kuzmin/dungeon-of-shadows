using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;
using DungeonOfShadows.ECS.Combat;
using DungeonOfShadows.ECS.Items;
using DungeonOfShadows.ECS.Rendering;

namespace DungeonOfShadows.ECS.Exploration.Systems;

/// <summary>
/// Обнаруживает стояние игрока на лестнице и выполняет переход на следующий этаж.
/// </summary>
public class FloorTransitionSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly List<int> _queryBuffer = new();

    public FloorTransitionSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _ctx.World;
        var map = _ctx.Map;
        int ts = _ctx.Config.ScaledTileSize;

        world.QueryInto<PlayerTag, Position>(_queryBuffer);
        if (_queryBuffer.Count == 0) return;

        int playerId = _queryBuffer[0];
        ref var pos = ref world.Get<Position>(playerId);

        int tx = (int)((pos.X + ts / 2f) / ts);
        int ty = (int)((pos.Y + ts / 2f) / ts);

        if (!map.InBounds(tx, ty)) return;
        if (map.Tiles[tx, ty].Type != TileType.StairDown) return;

        if (!Raylib.IsKeyPressed(KeyboardKey.E) && !Raylib.IsKeyPressed(KeyboardKey.Space))
            return;

        DescendFloor(playerId);
    }

    private void DescendFloor(int playerId)
    {
        var world = _ctx.World;

        // Уничтожаем всех не-игроков
        var toDestroy = new List<int>();
        foreach (int id in world.AllEntities)
        {
            if (id == playerId) continue;
            if (!world.IsAlive(id)) continue;
            toDestroy.Add(id);
        }
        foreach (int id in toDestroy)
            world.DestroyEntity(id);

        _ctx.CurrentFloor++;

        var result = DungeonGenerator.Generate(
            _ctx.CurrentFloor, _ctx.Config, _ctx.DungeonSeed);

        _ctx.Map = result.Map;

        int ts = _ctx.Config.ScaledTileSize;
        ref var pos = ref world.Get<Position>(playerId);
        pos.X = result.SpawnRoom.CenterX * ts;
        pos.Y = result.SpawnRoom.CenterY * ts;

        ref var vel = ref world.Get<Velocity>(playerId);
        vel.X = 0;
        vel.Y = 0;

        // Спавн врагов, сундуков, декор-объектов на новом этаже
        EnemySpawner.SpawnEnemies(world, _ctx.Map, _ctx.Config, _ctx.CurrentFloor, _ctx.DungeonSeed);
        ChestSpawner.SpawnChests(world, _ctx.Map, _ctx.Config, _ctx.CurrentFloor, _ctx.DungeonSeed);
        DecorationObjectSpawner.SpawnObjects(world, _ctx.Map, _ctx.Config, _ctx.CurrentFloor, _ctx.DungeonSeed);
    }
}
