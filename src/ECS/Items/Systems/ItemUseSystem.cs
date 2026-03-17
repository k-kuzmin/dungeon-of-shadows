using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Combat;

namespace DungeonOfShadows.ECS.Items.Systems;

public class ItemUseSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly ItemDatabase _db;
    private readonly List<int> _playerBuffer = new();
    private readonly List<int> _enemyBuffer = new();

    public ItemUseSystem(GameContext ctx, ItemDatabase db)
    {
        _ctx = ctx;
        _db = db;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _ctx.World;
        world.QueryInto<PlayerTag, Inventory>(_playerBuffer);
        if (_playerBuffer.Count == 0) return;

        int playerId = _playerBuffer[0];
        if (!world.Has<QuickSlots>(playerId) || !world.Has<Health>(playerId) || !world.Has<Stats>(playerId) || !world.Has<Position>(playerId))
            return;

        ref var inv = ref world.Get<Inventory>(playerId);
        ref var quick = ref world.Get<QuickSlots>(playerId);

        if (quick.PotionCooldown > 0) quick.PotionCooldown = MathF.Max(0, quick.PotionCooldown - dt);
        if (quick.ScrollCooldown > 0) quick.ScrollCooldown = MathF.Max(0, quick.ScrollCooldown - dt);

        if (Raylib.IsKeyPressed(KeyboardKey.One)) TryUseQuickSlot(playerId, ref inv, ref quick, quick.Slot1DefinitionId);
        if (Raylib.IsKeyPressed(KeyboardKey.Two)) TryUseQuickSlot(playerId, ref inv, ref quick, quick.Slot2DefinitionId);
        if (Raylib.IsKeyPressed(KeyboardKey.Three)) TryUseQuickSlot(playerId, ref inv, ref quick, quick.Slot3DefinitionId);
        if (Raylib.IsKeyPressed(KeyboardKey.Four)) TryUseQuickSlot(playerId, ref inv, ref quick, quick.Slot4DefinitionId);
    }

    private void TryUseQuickSlot(int playerId, ref Inventory inv, ref QuickSlots quick, int definitionId)
    {
        if (definitionId < 0)
            return;

        if (!_db.TryGetDefinition(definitionId, out var def))
        {
            ShowMessage("Unknown item");
            return;
        }

        if (def.Type == ItemType.Potion && quick.PotionCooldown > 0)
        {
            ShowMessage("Potion cooldown");
            return;
        }

        if (def.Type == ItemType.Scroll && quick.ScrollCooldown > 0)
        {
            ShowMessage("Scroll cooldown");
            return;
        }

        if (!TryConsume(ref inv, definitionId))
        {
            ShowMessage("No items");
            return;
        }

        ApplyEffect(playerId, def);

        if (def.Type == ItemType.Potion)
            quick.PotionCooldown = _ctx.Config.PotionCooldownSeconds;
        if (def.Type == ItemType.Scroll)
            quick.ScrollCooldown = _ctx.Config.ScrollCooldownSeconds;
    }

    private bool TryConsume(ref Inventory inv, int definitionId)
    {
        for (int i = 0; i < inv.Capacity; i++)
        {
            ref var slot = ref inv.Slots[i];
            if (!slot.Occupied || slot.DefinitionId != definitionId)
                continue;

            slot.Quantity -= 1;
            if (slot.Quantity <= 0)
                slot = ItemStack.Empty;

            return true;
        }

        return false;
    }

    private void ApplyEffect(int playerId, ItemDefinition def)
    {
        var world = _ctx.World;

        if (def.EffectType == ItemEffectType.HealHp)
        {
            ref var hp = ref world.Get<Health>(playerId);
            hp.HP = Math.Min(hp.MaxHP, hp.HP + def.EffectPower);
            ShowMessage("Healed");
            return;
        }

        if (def.EffectType == ItemEffectType.NovaDamage)
        {
            int ts = _ctx.Config.ScaledTileSize;
            float radius = _ctx.Config.ScrollNovaRadiusTiles * ts;
            float radiusSq = radius * radius;

            ref var playerPos = ref world.Get<Position>(playerId);
            ref var playerStats = ref world.Get<Stats>(playerId);

            world.QueryInto<EnemyTag, Position>(_enemyBuffer);
            uint state = (uint)(_ctx.DungeonSeed ^ (_ctx.CurrentFloor * _ctx.Config.FloorRngMixMultiplier) ^ playerId ^ def.Id);
            for (int i = 0; i < _enemyBuffer.Count; i++)
            {
                int enemyId = _enemyBuffer[i];
                ref var enemyPos = ref world.Get<Position>(enemyId);

                float dx = enemyPos.X - playerPos.X;
                float dy = enemyPos.Y - playerPos.Y;
                if (dx * dx + dy * dy > radiusSq)
                    continue;

                if (!world.Has<Stats>(enemyId))
                    continue;

                ref var enemyStats = ref world.Get<Stats>(enemyId);
                state ^= (uint)enemyId;
                var (damage, isCrit) = DamageCalculator.ComputeDeterministic(ref playerStats, ref enemyStats, _ctx.Config, ref state);

                damage += def.EffectPower;
                _ctx.DamageEvents.Add(new DamageEvent(playerId, enemyId, damage, isCrit, enemyPos.X, enemyPos.Y));
            }

            ShowMessage("Nova cast");
        }
    }

    private void ShowMessage(string text)
    {
        _ctx.UiMessage = text;
        _ctx.UiMessageTimer = _ctx.Config.UiMessageSeconds;
    }
}
