using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Items.Systems;

public class ChestSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly ItemDatabase _db;
    private readonly List<int> _playerBuffer = new();
    private readonly List<int> _chestBuffer = new();

    public ChestSystem(GameContext ctx, ItemDatabase db)
    {
        _ctx = ctx;
        _db = db;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;
        if (!Raylib.IsKeyPressed(KeyboardKey.E)) return;

        var world = _ctx.World;
        int ts = _ctx.Config.ScaledTileSize;
        float maxDist = _ctx.Config.ChestInteractRadiusTiles * ts;
        float maxDistSq = maxDist * maxDist;

        world.QueryInto<PlayerTag, Position>(_playerBuffer);
        if (_playerBuffer.Count == 0) return;

        int playerId = _playerBuffer[0];
        ref var playerPos = ref world.Get<Position>(playerId);

        world.QueryInto<Chest, Position>(_chestBuffer);

        int nearestId = -1;
        float nearestDistSq = maxDistSq;
        for (int i = 0; i < _chestBuffer.Count; i++)
        {
            int id = _chestBuffer[i];
            ref var chest = ref world.Get<Chest>(id);
            if (chest.Opened) continue;

            ref var pos = ref world.Get<Position>(id);
            float dx = pos.X - playerPos.X;
            float dy = pos.Y - playerPos.Y;
            float distSq = dx * dx + dy * dy;
            if (distSq <= nearestDistSq)
            {
                nearestDistSq = distSq;
                nearestId = id;
            }
        }

        if (nearestId < 0) return;

        ref var targetChest = ref world.Get<Chest>(nearestId);
        targetChest.Opened = true;

        if (_db.TryGetDefinition(targetChest.GuaranteedDefinitionId, out var def))
        {
            var rng = new Random(_ctx.DungeonSeed ^ (_ctx.CurrentFloor * _ctx.Config.FloorRngMixMultiplier) ^ nearestId);
            var stack = _db.CreateInstance(def, ItemRarity.Uncommon, rng, _ctx.Config.ItemMaxStackSize);

            int itemId = world.CreateEntity();
            world.Add(itemId, new Position(playerPos.X + ts * 0.35f, playerPos.Y + ts * 0.35f));
            world.Add(itemId, new ItemOnGround());
            world.Add(itemId, stack);
        }

        _ctx.UiMessage = "Chest opened";
        _ctx.UiMessageTimer = _ctx.Config.UiMessageSeconds;
    }
}
