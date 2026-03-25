using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Combat;

namespace DungeonOfShadows.ECS.Items.Systems;

public class InventoryRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly IAssetProvider _assets;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly ItemDatabase _db;
    private readonly List<int> _playerBuffer = new();
    private readonly List<int> _quickSlotsBuffer = new();
    private Texture2D _itemsTex;
    private int _selectedInvSlot = -1;
    private bool _selectedEquip;
    private int _selectedEquipSlot = -1;

    public RenderPhase Phase => RenderPhase.Screen;

    public InventoryRenderSystem(GameContext ctx, World world, GameConfig config, ItemDatabase db, IAssetProvider assets)
    {
        _ctx = ctx;
        _assets = assets;
        _world = world;
        _config = config;
        _db = db;
    }

    public void Tick(float dt)
    {
        _itemsTex = _assets.GetTexture("items");
        DrawQuickSlots();

        if (_ctx.UiMessageTimer > 0 && !string.IsNullOrWhiteSpace(_ctx.UiMessage))
        {
            int textW = Raylib.MeasureText(_ctx.UiMessage, 22);
            int x = _config.ScreenWidth / 2 - textW / 2;
            Raylib.DrawText(_ctx.UiMessage, x, 16, 22, Color.Gold);
        }

        if (!_ctx.ShowInventory)
            return;

        var world = _world;
        world.QueryInto<PlayerTag, Inventory>(_playerBuffer);
        if (_playerBuffer.Count == 0)
            return;

        int playerId = _playerBuffer[0];
        ref var inv = ref world.Get<Inventory>(playerId);
        ref var eq = ref world.Get<Equipment>(playerId);
        if (!world.Has<Stats>(playerId) || !world.Has<Health>(playerId))
            return;

        ref var stats = ref world.Get<Stats>(playerId);
        ref var hp = ref world.Get<Health>(playerId);

        int panelX = _config.InventoryPanelX;
        int panelY = _config.InventoryPanelY;
        int panelW = _config.ScreenWidth - 240;
        int panelH = _config.ScreenHeight - 180;

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, new Color(20, 20, 24, 235));
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, new Color(180, 180, 180, 255));
        Raylib.DrawText("Inventory (I to close)", panelX + 20, panelY + 14, 24, Color.White);

        int cols = 5;
        int cellSize = _config.InventoryCellSize;
        int gap = _config.InventoryCellGap;
        int startX = panelX + _config.InventoryPanelPaddingX;
        int startY = panelY + _config.InventoryPanelPaddingY;

        string tooltip = string.Empty;
        string compare = string.Empty;
        HandleInventoryInput(playerId, ref inv, ref eq, ref stats, ref hp, startX, startY, cols, cellSize, gap);

        for (int i = 0; i < inv.Capacity; i++)
        {
            int cx = i % cols;
            int cy = i / cols;
            int x = startX + cx * (cellSize + gap);
            int y = startY + cy * (cellSize + gap);

            bool isSelected = i == _selectedInvSlot && !_selectedEquip;
            Raylib.DrawRectangle(x, y, cellSize, cellSize, new Color(45, 45, 54, 255));
            Raylib.DrawRectangleLines(x, y, cellSize, cellSize, isSelected
                ? new Color(255, 220, 120, 255)
                : new Color(140, 140, 150, 255));

            ref var slot = ref inv.Slots[i];
            if (!slot.Occupied) continue;

            DrawItemIcon(slot, x, y, cellSize);

            if (slot.Quantity > 1)
                Raylib.DrawText($"x{slot.Quantity}", x + 4, y + cellSize - 16, 14, new Color(230, 230, 140, 255));

            var mouse = Raylib.GetMousePosition();
            if (PointInRect(mouse, x, y, cellSize, cellSize))
            {
                tooltip = BuildItemTooltip(slot);
                compare = BuildCompareText(ref eq, slot);
            }
        }

        int ex = panelX + panelW - 270;
        int ey = panelY + _config.InventoryPanelPaddingY;
        DrawEquipSlot("Weapon", eq.Weapon, ex, ey, 0);
        DrawEquipSlot("Armor", eq.Armor, ex, ey + 38, 1);
        DrawEquipSlot("Amulet", eq.Amulet, ex, ey + 76, 2);
        DrawEquipSlot("Ring1", eq.Ring1, ex, ey + 114, 3);
        DrawEquipSlot("Ring2", eq.Ring2, ex, ey + 152, 4);

        DrawTooltip(tooltip, compare, panelX + 360, panelY + 56);
    }

    private void DrawQuickSlots()
    {
        var world = _world;
        world.QueryInto<PlayerTag, QuickSlots>(_quickSlotsBuffer);
        if (_quickSlotsBuffer.Count == 0)
            return;

        int playerId = _quickSlotsBuffer[0];
        ref var quick = ref world.Get<QuickSlots>(playerId);

        int baseY = _config.ScreenHeight - 62;
        int baseX = _config.ScreenWidth / 2 - 120;

        DrawQuickSlot(1, quick.Slot1DefinitionId, baseX + 0, baseY);
        DrawQuickSlot(2, quick.Slot2DefinitionId, baseX + 60, baseY);
        DrawQuickSlot(3, quick.Slot3DefinitionId, baseX + 120, baseY);
        DrawQuickSlot(4, quick.Slot4DefinitionId, baseX + 180, baseY);

        if (quick.PotionCooldown > 0 || quick.ScrollCooldown > 0)
        {
            string cd = $"P:{quick.PotionCooldown:0.0} S:{quick.ScrollCooldown:0.0}";
            Raylib.DrawText(cd, baseX, baseY - 20, 14, Color.LightGray);
        }
    }

    private void DrawQuickSlot(int key, int definitionId, int x, int y)
    {
        Raylib.DrawRectangle(x, y, 52, 52, new Color(32, 32, 40, 220));
        Raylib.DrawRectangleLines(x, y, 52, 52, new Color(160, 160, 170, 255));
        Raylib.DrawText(key.ToString(), x + 2, y + 2, 12, Color.White);

        if (definitionId < 0)
            return;

        if (_db.TryGetDefinition(definitionId, out var def))
        {
            var src = ItemAtlas.GetSource(definitionId, def.Type, ItemRarity.Common);
            int iconSize = 32;
            int ix = x + (52 - iconSize) / 2;
            int iy = y + (52 - iconSize) / 2 + 2;
            var dest = new Rectangle(ix, iy, iconSize, iconSize);
            Raylib.DrawTexturePro(_itemsTex, src, dest, System.Numerics.Vector2.Zero, 0f, Color.White);
        }
    }

    private void DrawEquipSlot(string name, ItemStack slot, int x, int y)
    {
        Raylib.DrawText(name, x, y + 4, 16, Color.SkyBlue);
        if (slot.Occupied)
        {
            DrawItemIcon(slot, x + 80, y - 2, 28);
            string value = slot.DefinitionId.ToString();
            if (_db.TryGetDefinition(slot.DefinitionId, out var def))
                value = def.Name;
            Raylib.DrawText(value, x + 114, y + 4, 16, Color.White);
        }
        else
        {
            Raylib.DrawText("-", x + 100, y + 4, 16, Color.White);
        }
    }

    private void DrawEquipSlot(string name, ItemStack slot, int x, int y, int index)
    {
        bool selected = _selectedEquip && _selectedEquipSlot == index;
        Raylib.DrawRectangleLines(x - 4, y - 2, 260, 30, selected
            ? new Color(255, 220, 120, 255)
            : new Color(120, 120, 130, 180));
        DrawEquipSlot(name, slot, x, y);
    }

    private void HandleInventoryInput(int playerId, ref Inventory inv, ref Equipment eq,
        ref Stats stats, ref Health hp,
        int startX, int startY, int cols, int cellSize, int gap)
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.Left))
            return;

        var mouse = Raylib.GetMousePosition();

        // Inventory grid click
        for (int i = 0; i < inv.Capacity; i++)
        {
            int cx = i % cols;
            int cy = i / cols;
            int x = startX + cx * (cellSize + gap);
            int y = startY + cy * (cellSize + gap);

            if (!PointInRect(mouse, x, y, cellSize, cellSize))
                continue;

            HandleInventorySlotClick(ref inv, ref eq, ref stats, ref hp, i);
            return;
        }

        // Equipment click area
        int ex = _config.InventoryPanelX + (_config.ScreenWidth - 240) - 270;
        int ey = _config.InventoryPanelY + _config.InventoryPanelPaddingY;
        for (int i = 0; i < 5; i++)
        {
            int rowY = ey + i * 38;
            if (!PointInRect(mouse, ex - 4, rowY - 2, 260, 30))
                continue;

            HandleEquipmentSlotClick(playerId, ref inv, ref eq, ref stats, ref hp, i);
            return;
        }

        _selectedInvSlot = -1;
        _selectedEquip = false;
        _selectedEquipSlot = -1;
    }

    private void HandleInventorySlotClick(ref Inventory inv, ref Equipment eq, ref Stats stats, ref Health hp, int clicked)
    {
        ref var slot = ref inv.Slots[clicked];

        if (_selectedEquip)
        {
            if (slot.Occupied)
            {
                ShowMessage("Target slot busy");
                return;
            }

            ref var equipSlot = ref GetEquipmentSlot(ref eq, _selectedEquipSlot);
            slot = equipSlot;
            ApplyEquipmentStatDelta(ref stats, ref hp, equipSlot, ItemStack.Empty);
            equipSlot = ItemStack.Empty;
            _selectedEquip = false;
            _selectedEquipSlot = -1;
            return;
        }

        if (_selectedInvSlot >= 0 && _selectedInvSlot != clicked)
        {
            ref var src = ref inv.Slots[_selectedInvSlot];
            var temp = src;
            src = slot;
            slot = temp;
            _selectedInvSlot = -1;
            return;
        }

        _selectedInvSlot = slot.Occupied ? clicked : -1;
    }

    private void HandleEquipmentSlotClick(int playerId, ref Inventory inv, ref Equipment eq,
        ref Stats stats, ref Health hp, int equipSlotIndex)
    {
        ref var equipSlot = ref GetEquipmentSlot(ref eq, equipSlotIndex);

        // From inventory to equipment
        if (_selectedInvSlot >= 0)
        {
            ref var src = ref inv.Slots[_selectedInvSlot];
            if (!src.Occupied)
            {
                _selectedInvSlot = -1;
                return;
            }

            if (!IsCompatible(src.Type, equipSlotIndex))
            {
                ShowMessage("Wrong slot");
                return;
            }

            var replaced = equipSlot;
            equipSlot = src;
            src = replaced;
            ApplyEquipmentStatDelta(ref stats, ref hp, replaced, equipSlot);
            _selectedInvSlot = -1;
            return;
        }

        // Select equipment slot
        if (equipSlot.Occupied && !_selectedEquip)
        {
            _selectedEquip = true;
            _selectedEquipSlot = equipSlotIndex;
            return;
        }

        // Unequip with fallback drop to ground
        if (_selectedEquip && _selectedEquipSlot == equipSlotIndex && equipSlot.Occupied)
        {
            if (TryAddToInventory(ref inv, equipSlot))
            {
                ApplyEquipmentStatDelta(ref stats, ref hp, equipSlot, ItemStack.Empty);
                equipSlot = ItemStack.Empty;
                _selectedEquip = false;
                _selectedEquipSlot = -1;
                return;
            }

            DropToGroundNearPlayer(playerId, equipSlot);
            ApplyEquipmentStatDelta(ref stats, ref hp, equipSlot, ItemStack.Empty);
            equipSlot = ItemStack.Empty;
            _selectedEquip = false;
            _selectedEquipSlot = -1;
            ShowMessage("Dropped on floor");
        }
    }

    private bool TryAddToInventory(ref Inventory inv, ItemStack stack)
    {
        int maxStack = _config.ItemMaxStackSize;
        if (stack.Type is ItemType.Potion or ItemType.Scroll)
        {
            for (int i = 0; i < inv.Capacity; i++)
            {
                ref var slot = ref inv.Slots[i];
                if (!slot.Occupied || slot.DefinitionId != stack.DefinitionId) continue;
                if (slot.Quantity >= maxStack) continue;

                slot.Quantity = Math.Min(maxStack, slot.Quantity + stack.Quantity);
                return true;
            }
        }

        for (int i = 0; i < inv.Capacity; i++)
        {
            if (inv.Slots[i].Occupied) continue;
            inv.Slots[i] = stack;
            return true;
        }

        return false;
    }

    private void DropToGroundNearPlayer(int playerId, ItemStack stack)
    {
        var world = _world;
        if (!world.Has<Position>(playerId))
            return;

        ref var playerPos = ref world.Get<Position>(playerId);
        int id = world.CreateEntity();
        float offset = _config.ItemDropOffsetPixels;
        world.Add(id, new Position(playerPos.X + offset, playerPos.Y + offset));
        world.Add(id, new ItemOnGround());
        world.Add(id, stack);
    }

    private static void ApplyEquipmentStatDelta(ref Stats stats, ref Health hp, ItemStack oldItem, ItemStack newItem)
    {
        stats.ATK += newItem.BonusATK - oldItem.BonusATK;
        stats.DEF += newItem.BonusDEF - oldItem.BonusDEF;
        stats.Crit += newItem.BonusCrit - oldItem.BonusCrit;

        int hpDelta = newItem.BonusHP - oldItem.BonusHP;
        hp.MaxHP = Math.Max(1, hp.MaxHP + hpDelta);
        hp.HP = Math.Clamp(hp.HP + hpDelta, 0, hp.MaxHP);
    }

    private static bool IsCompatible(ItemType type, int equipSlotIndex)
    {
        return equipSlotIndex switch
        {
            0 => type == ItemType.Weapon,
            1 => type == ItemType.Armor,
            2 => type == ItemType.Amulet,
            3 or 4 => type == ItemType.Ring,
            _ => false
        };
    }

    private string BuildItemTooltip(ItemStack item)
    {
        string name = item.DefinitionId.ToString();
        if (_db.TryGetDefinition(item.DefinitionId, out var def))
            name = def.Name;

        return name + "\n" +
            "ATK +" + item.BonusATK + "  DEF +" + item.BonusDEF + "\n" +
            "HP +" + item.BonusHP + "  CRIT +" + (item.BonusCrit * 100f).ToString("0.0") + "%";
    }

    private string BuildCompareText(ref Equipment eq, ItemStack hovered)
    {
        if (!IsEquipmentType(hovered.Type))
            return string.Empty;

        ItemStack current = hovered.Type switch
        {
            ItemType.Weapon => eq.Weapon,
            ItemType.Armor => eq.Armor,
            ItemType.Amulet => eq.Amulet,
            ItemType.Ring => eq.Ring1.Occupied ? eq.Ring1 : eq.Ring2,
            _ => ItemStack.Empty
        };

        if (!current.Occupied)
            return "Compare: slot empty";

        int dAtk = hovered.BonusATK - current.BonusATK;
        int dDef = hovered.BonusDEF - current.BonusDEF;
        int dHp = hovered.BonusHP - current.BonusHP;
        float dCrit = hovered.BonusCrit - current.BonusCrit;
        return "Delta  A:" + FormatDelta(dAtk) + " D:" + FormatDelta(dDef) +
            " HP:" + FormatDelta(dHp) + " C:" + FormatDeltaPercent(dCrit);
    }

    private static bool IsEquipmentType(ItemType type)
    {
        return type is ItemType.Weapon or ItemType.Armor or ItemType.Amulet or ItemType.Ring;
    }

    private void DrawTooltip(string tooltip, string compare, int x, int y)
    {
        if (string.IsNullOrEmpty(tooltip))
            return;

        int w = 300;
        int h = string.IsNullOrEmpty(compare) ? 72 : 100;
        Raylib.DrawRectangle(x, y, w, h, new Color(10, 10, 15, 225));
        Raylib.DrawRectangleLines(x, y, w, h, new Color(190, 190, 210, 255));

        Raylib.DrawText(tooltip, x + 10, y + 8, 16, Color.White);
        if (!string.IsNullOrEmpty(compare))
            Raylib.DrawText(compare, x + 10, y + 62, 14, new Color(220, 210, 120, 255));
    }

    private void ShowMessage(string text)
    {
        _ctx.UiMessage = text;
        _ctx.UiMessageTimer = _config.UiMessageSeconds;
    }

    private static string FormatDelta(int value)
    {
        return value >= 0 ? "+" + value : value.ToString();
    }

    private static string FormatDeltaPercent(float value)
    {
        float pct = value * 100f;
        return (pct >= 0 ? "+" : "") + pct.ToString("0.0") + "%";
    }

    private void DrawItemIcon(ItemStack item, int x, int y, int size)
    {
        var src = ItemAtlas.GetSource(item.DefinitionId, item.Type, item.Rarity);

        // Рамка редкости (под иконкой)
        Color border = GetRarityColor(item.Rarity);
        Raylib.DrawRectangleLines(x, y, size, size, border);

        // Иконка с отступом 2px внутри рамки
        int pad = 2;
        int iconSize = size - pad * 2;
        var dest = new Rectangle(x + pad, y + pad, iconSize, iconSize);
        Raylib.DrawTexturePro(_itemsTex, src, dest, System.Numerics.Vector2.Zero, 0f, Color.White);
    }

    private static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => new Color(160, 160, 170, 255),
            ItemRarity.Uncommon => new Color(90, 220, 110, 255),
            ItemRarity.Rare => new Color(90, 140, 240, 255),
            ItemRarity.Epic => new Color(210, 80, 220, 255),
            ItemRarity.Legendary => new Color(245, 200, 70, 255),
            _ => Color.White
        };
    }

    private static bool PointInRect(System.Numerics.Vector2 p, int x, int y, int w, int h)
    {
        return p.X >= x && p.X <= x + w && p.Y >= y && p.Y <= y + h;
    }

    private static ref ItemStack GetEquipmentSlot(ref Equipment eq, int equipSlotIndex)
    {
        if (equipSlotIndex == 0) return ref eq.Weapon;
        if (equipSlotIndex == 1) return ref eq.Armor;
        if (equipSlotIndex == 2) return ref eq.Amulet;
        if (equipSlotIndex == 3) return ref eq.Ring1;
        return ref eq.Ring2;
    }
}
