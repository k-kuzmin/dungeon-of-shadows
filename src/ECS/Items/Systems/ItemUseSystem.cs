using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Combat;
using DungeonOfShadows.ECS.Magic;
using DungeonOfShadows.ECS.Magic.Components;
using DungeonOfShadows.UI;

namespace DungeonOfShadows.ECS.Items.Systems;

public class ItemUseSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly ItemDatabase _db;
    private readonly SpellDatabase _spellDb;
    private readonly UiContext _uiCtx;
    private readonly List<int> _playerBuffer = new();
    private readonly List<int> _enemyBuffer = new();

    public ItemUseSystem(GameContext ctx, World world, GameConfig config, ItemDatabase db,
        SpellDatabase spellDb, UiContext uiCtx)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
        _db = db;
        _spellDb = spellDb;
        _uiCtx = uiCtx;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _world;
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

        if (def.Type is ItemType.Scroll or ItemType.SpellScroll && quick.ScrollCooldown > 0)
        {
            ShowMessage("Scroll cooldown");
            return;
        }

        // Для SpellScroll — отдельная обработка через TryLearnOrUpgrade
        if (def.EffectType == ItemEffectType.TeachSpell)
        {
            HandleSpellScroll(playerId, ref inv, ref quick, definitionId, def);
            return;
        }

        if (!TryConsume(ref inv, definitionId))
        {
            ShowMessage("No items");
            return;
        }

        ApplyEffect(playerId, def);

        // Очистить быстрый слот если предмет закончился в инвентаре
        if (!HasInInventory(ref inv, definitionId))
            ClearQuickSlot(ref quick, definitionId);

        if (def.Type == ItemType.Potion)
            quick.PotionCooldown = _config.PotionCooldownSeconds;
        if (def.Type is ItemType.Scroll or ItemType.SpellScroll)
            quick.ScrollCooldown = _config.ScrollCooldownSeconds;
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
        var world = _world;

        if (def.EffectType == ItemEffectType.HealHp)
        {
            ref var hp = ref world.Get<Health>(playerId);
            hp.HP = Math.Min(hp.MaxHP, hp.HP + def.EffectPower);
            ShowMessage("Healed");
            return;
        }

        if (def.EffectType == ItemEffectType.NovaDamage)
        {
            int ts = _config.ScaledTileSize;
            float radius = _config.ScrollNovaRadiusTiles * ts;
            float radiusSq = radius * radius;

            ref var playerPos = ref world.Get<Position>(playerId);
            ref var playerStats = ref world.Get<Stats>(playerId);

            world.QueryInto<EnemyTag, Position>(_enemyBuffer);
            uint state = (uint)(_ctx.DungeonSeed ^ (_ctx.CurrentFloor * _config.FloorRngMixMultiplier) ^ playerId ^ def.Id);
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
                var (damage, isCrit) = DamageCalculator.ComputeDeterministic(ref playerStats, ref enemyStats, _config, ref state);

                damage += def.EffectPower;
                _ctx.DamageEvents.Add(new DamageEvent(playerId, enemyId, damage, isCrit, enemyPos.X, enemyPos.Y));
            }

            ShowMessage("Nova cast");
        }

    }

    private static bool HasInInventory(ref Inventory inv, int definitionId)
    {
        for (int i = 0; i < inv.Capacity; i++)
        {
            ref var slot = ref inv.Slots[i];
            if (slot.Occupied && slot.DefinitionId == definitionId)
                return true;
        }
        return false;
    }

    private static void ClearQuickSlot(ref QuickSlots qs, int definitionId)
    {
        if (qs.Slot1DefinitionId == definitionId) qs.Slot1DefinitionId = -1;
        if (qs.Slot2DefinitionId == definitionId) qs.Slot2DefinitionId = -1;
        if (qs.Slot3DefinitionId == definitionId) qs.Slot3DefinitionId = -1;
        if (qs.Slot4DefinitionId == definitionId) qs.Slot4DefinitionId = -1;
    }

    private void HandleSpellScroll(int playerId, ref Inventory inv, ref QuickSlots quick,
        int definitionId, ItemDefinition def)
    {
        if (!_world.Has<SpellSlots>(playerId)) { ShowMessage("No spell slots"); return; }
        ref var slots = ref _world.Get<SpellSlots>(playerId);

        var result = MagicHelper.TryLearnOrUpgrade(ref slots, def.SpellId, _config.MaxSpellLevel);

        switch (result)
        {
            case LearnResult.Learned:
                ConsumeAndClear(ref inv, ref quick, definitionId);
                ShowMessage("Learned spell");
                break;

            case LearnResult.Upgraded:
            {
                ConsumeAndClear(ref inv, ref quick, definitionId);
                int idx = slots.FindSpellIndex(def.SpellId);
                int lvl = idx >= 0 ? slots.Slots[idx].Level : 0;
                ShowMessage($"Spell Lv.{lvl}");
                break;
            }

            case LearnResult.AlreadyMaxLevel:
                ShowMessage("Already max level");
                break;

            case LearnResult.SlotsFull:
                OpenReplaceModal(playerId, ref inv, ref quick, definitionId, def);
                break;
        }
    }

    private void OpenReplaceModal(int playerId, ref Inventory inv, ref QuickSlots quick,
        int definitionId, ItemDefinition def)
    {
        ref var slots = ref _world.Get<SpellSlots>(playerId);
        int cap = slots.Capacity;
        var options = new string[cap];
        for (int i = 0; i < cap; i++)
        {
            var slot = slots.GetSlot(i);
            string spellName = "???";
            if (_spellDb.TryGet(slot.SpellId, out var spellDef))
                spellName = spellDef.Name;
            options[i] = $"{spellName} Lv.{slot.Level}";
        }

        string newSpellName = "spell";
        if (_spellDb.TryGet(def.SpellId, out var newDef))
            newSpellName = newDef.Name;
        string title = $"Replace with {newSpellName}?";

        // Захватываем ID (value types) для лямбды
        int capturedPlayerId = playerId;
        int capturedDefId = definitionId;
        int capturedSpellId = def.SpellId;

        var modal = new SelectionModalDescriptor(title, options,
            onConfirm: (chosenIdx) =>
            {
                if (!_world.Has<SpellSlots>(capturedPlayerId)) return;
                ref var s = ref _world.Get<SpellSlots>(capturedPlayerId);
                MagicHelper.ReplaceSpell(ref s, chosenIdx, capturedSpellId);

                if (_world.Has<Inventory>(capturedPlayerId) && _world.Has<QuickSlots>(capturedPlayerId))
                {
                    ref var invRef = ref _world.Get<Inventory>(capturedPlayerId);
                    ref var qsRef = ref _world.Get<QuickSlots>(capturedPlayerId);
                    ConsumeAndClear(ref invRef, ref qsRef, capturedDefId);
                }
                ShowMessage("Replaced spell");
            },
            onCancel: () => { ShowMessage("Cancelled"); });

        _uiCtx.SetSelectionModal(modal);
    }

    private void ConsumeAndClear(ref Inventory inv, ref QuickSlots quick, int definitionId)
    {
        TryConsume(ref inv, definitionId);
        if (!HasInInventory(ref inv, definitionId))
            ClearQuickSlot(ref quick, definitionId);

        if (_config.ScrollCooldownSeconds > 0)
            quick.ScrollCooldown = _config.ScrollCooldownSeconds;
    }

    private void ShowMessage(string text)
    {
        _ctx.UiMessage = text;
        _ctx.UiMessageTimer = _config.UiMessageSeconds;
    }
}
