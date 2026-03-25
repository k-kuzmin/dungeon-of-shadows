using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Combat;

namespace DungeonOfShadows.ECS.Items.Systems;

public class ItemPickupSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly ItemDatabase _db;
    private readonly List<int> _playerBuffer = new();
    private readonly List<int> _itemBuffer = new();

    public ItemPickupSystem(GameContext ctx, World world, GameConfig config, ItemDatabase db)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
        _db = db;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;
        if (!Raylib.IsKeyPressed(KeyboardKey.E)) return;

        var world = _world;
        int ts = _config.ScaledTileSize;
        float maxDist = _config.ItemPickupRadiusTiles * ts;
        float maxDistSq = maxDist * maxDist;

        world.QueryInto<PlayerTag, Position>(_playerBuffer);
        if (_playerBuffer.Count == 0) return;

        int playerId = _playerBuffer[0];
        ref var playerPos = ref world.Get<Position>(playerId);

        world.QueryInto<ItemOnGround, Position>(_itemBuffer);

        int nearestItemId = -1;
        float nearestDistSq = maxDistSq;
        for (int i = 0; i < _itemBuffer.Count; i++)
        {
            int id = _itemBuffer[i];
            ref var pos = ref world.Get<Position>(id);
            float dx = pos.X - playerPos.X;
            float dy = pos.Y - playerPos.Y;
            float distSq = dx * dx + dy * dy;
            if (distSq <= nearestDistSq)
            {
                nearestDistSq = distSq;
                nearestItemId = id;
            }
        }

        if (nearestItemId < 0) return;
        if (!world.Has<ItemStack>(nearestItemId))
        {
            world.DestroyEntity(nearestItemId);
            return;
        }

        ref var stack = ref world.Get<ItemStack>(nearestItemId);

        if (IsEquipment(stack.Type))
        {
            if (!world.Has<Equipment>(playerId) || !world.Has<Stats>(playerId) || !world.Has<Health>(playerId))
                return;

            ref var eq = ref world.Get<Equipment>(playerId);
            ref var stats = ref world.Get<Stats>(playerId);
            ref var hp = ref world.Get<Health>(playerId);

            ItemStack replaced = EquipAndGetReplaced(ref eq, stack);
            ApplyStatDelta(ref stats, ref hp, replaced, stack);

            if (replaced.Occupied)
                SpawnGroundItem(replaced, playerPos.X + ts * 0.25f, playerPos.Y + ts * 0.25f);

            world.DestroyEntity(nearestItemId);
            ShowMessage("Equipped");
            return;
        }

        if (!world.Has<Inventory>(playerId))
            return;

        ref var inv = ref world.Get<Inventory>(playerId);
        if (TryAddToInventory(ref inv, stack))
        {
            TryAutoBindQuickSlot(playerId, stack.DefinitionId);
            world.DestroyEntity(nearestItemId);
            ShowMessage("Picked up");
        }
        else
        {
            ShowMessage("Inventory full");
        }
    }

    private static bool IsEquipment(ItemType type)
    {
        return type is ItemType.Weapon or ItemType.Armor or ItemType.Amulet or ItemType.Ring;
    }

    private ItemStack EquipAndGetReplaced(ref Equipment eq, ItemStack incoming)
    {
        ItemStack replaced;
        if (incoming.Type == ItemType.Weapon)
        {
            replaced = eq.Weapon;
            eq.Weapon = incoming;
            return replaced;
        }

        if (incoming.Type == ItemType.Armor)
        {
            replaced = eq.Armor;
            eq.Armor = incoming;
            return replaced;
        }

        if (incoming.Type == ItemType.Amulet)
        {
            replaced = eq.Amulet;
            eq.Amulet = incoming;
            return replaced;
        }

        // Ring: first free, otherwise replace slot 1.
        if (!eq.Ring1.Occupied)
        {
            replaced = eq.Ring1;
            eq.Ring1 = incoming;
            return replaced;
        }

        if (!eq.Ring2.Occupied)
        {
            replaced = eq.Ring2;
            eq.Ring2 = incoming;
            return replaced;
        }

        replaced = eq.Ring1;
        eq.Ring1 = incoming;
        return replaced;
    }

    private static void ApplyStatDelta(ref Stats stats, ref Health hp, ItemStack oldItem, ItemStack newItem)
    {
        stats.ATK += newItem.BonusATK - oldItem.BonusATK;
        stats.DEF += newItem.BonusDEF - oldItem.BonusDEF;
        stats.Crit += newItem.BonusCrit - oldItem.BonusCrit;

        int hpDelta = newItem.BonusHP - oldItem.BonusHP;
        hp.MaxHP = Math.Max(1, hp.MaxHP + hpDelta);
        hp.HP = Math.Clamp(hp.HP + hpDelta, 0, hp.MaxHP);
    }

    private bool TryAddToInventory(ref Inventory inventory, ItemStack incoming)
    {
        int maxStack = _config.ItemMaxStackSize;

        if (incoming.Type is ItemType.Potion or ItemType.Scroll)
        {
            for (int i = 0; i < inventory.Capacity; i++)
            {
                ref var slot = ref inventory.Slots[i];
                if (!slot.Occupied || slot.DefinitionId != incoming.DefinitionId) continue;

                if (slot.Quantity >= maxStack) continue;
                slot.Quantity = Math.Min(maxStack, slot.Quantity + incoming.Quantity);
                return true;
            }
        }

        for (int i = 0; i < inventory.Capacity; i++)
        {
            ref var slot = ref inventory.Slots[i];
            if (slot.Occupied) continue;
            slot = incoming;
            slot.Quantity = Math.Max(1, incoming.Quantity);
            return true;
        }

        return false;
    }

    private void TryAutoBindQuickSlot(int playerId, int definitionId)
    {
        var world = _world;
        if (!world.Has<QuickSlots>(playerId)) return;
        if (!_db.TryGetDefinition(definitionId, out var def)) return;
        if (def.Type is not (ItemType.Potion or ItemType.Scroll or ItemType.SpellScroll)) return;

        ref var qs = ref world.Get<QuickSlots>(playerId);

        // Не дублируем — если уже привязан, выходим
        if (qs.Slot1DefinitionId == definitionId || qs.Slot2DefinitionId == definitionId ||
            qs.Slot3DefinitionId == definitionId || qs.Slot4DefinitionId == definitionId)
            return;

        if (qs.Slot1DefinitionId < 0) { qs.Slot1DefinitionId = definitionId; return; }
        if (qs.Slot2DefinitionId < 0) { qs.Slot2DefinitionId = definitionId; return; }
        if (qs.Slot3DefinitionId < 0) { qs.Slot3DefinitionId = definitionId; return; }
        if (qs.Slot4DefinitionId < 0) { qs.Slot4DefinitionId = definitionId; }
    }

    private void SpawnGroundItem(ItemStack stack, float x, float y)
    {
        var world = _world;
        int id = world.CreateEntity();
        world.Add(id, new Position(x, y));
        world.Add(id, new ItemOnGround());
        world.Add(id, stack);
    }

    private void ShowMessage(string text)
    {
        _ctx.UiMessage = text;
        _ctx.UiMessageTimer = _config.UiMessageSeconds;
    }
}
