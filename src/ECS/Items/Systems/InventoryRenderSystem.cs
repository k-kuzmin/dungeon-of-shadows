using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Combat;
using DungeonOfShadows.ECS.Magic;
using DungeonOfShadows.ECS.Magic.Components;
using DungeonOfShadows.UI;

namespace DungeonOfShadows.ECS.Items.Systems;

public class InventoryRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly IAssetProvider _assets;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly ItemDatabase _db;
    private readonly UiTheme _theme;
    private readonly TextMeasureCache _textCache;
    private readonly UiContext _uiCtx;
    private readonly List<int> _playerBuffer = new();
    private readonly List<int> _quickSlotsBuffer = new();
    private Texture2D _itemsTex;
    private int _selectedInvSlot = -1;
    private bool _selectedEquip;
    private int _selectedEquipSlot = -1;

    // Кеш тултипа: обновляем только при смене hover слота
    private int _hoveredSlot = -1;
    private string _cachedTooltip = string.Empty;
    private string _cachedCompare = string.Empty;

    // Кеш строк для quick slots и inventory (избегаем аллокаций на hot path)
    private static readonly string[] QuickSlotLabels = ["1", "2", "3", "4"];
    private readonly string[] _cachedQuantityStrings = new string[20];
    private readonly int[] _cachedQuantityValues = new int[20];
    private string _cachedCooldownText = string.Empty;
    private float _cachedPotionCd = -1f;
    private float _cachedScrollCd = -1f;

    // Кеш строк для панели статов
    private int _statCacheAtk = -1, _statCacheDef = -1, _statCacheInt = -1;
    private int _statCacheHp = -1, _statCacheMaxHp = -1;
    private int _statCacheMp = -1, _statCacheMaxMp = -1;
    private float _statCacheSpd = -1f, _statCacheCrit = -1f;
    private string _statStrAtk = "", _statStrDef = "", _statStrSpd = "";
    private string _statStrCrit = "", _statStrInt = "";
    private string _statStrHp = "", _statStrMp = "";

    // Кеш строк для статус-эффектов (макс 4 слота)
    private readonly string[] _cachedEffectLines = new string[4];
    private readonly SpellEffectType[] _cachedEffectTypes = new SpellEffectType[4];
    private readonly int[] _cachedEffectSecs = new int[4];

    public RenderPhase Phase => RenderPhase.Screen;

    public InventoryRenderSystem(GameContext ctx, World world, GameConfig config,
        ItemDatabase db, IAssetProvider assets, UiTheme theme, TextMeasureCache textCache, UiContext uiCtx)
    {
        _ctx = ctx;
        _assets = assets;
        _world = world;
        _config = config;
        _db = db;
        _theme = theme;
        _textCache = textCache;
        _uiCtx = uiCtx;
    }

    public void Tick(float dt)
    {
        _itemsTex = _assets.GetTexture("items");
        DrawQuickSlots();

        if (_ctx.UiMessageTimer > 0 && !string.IsNullOrWhiteSpace(_ctx.UiMessage))
        {
            int textW = _textCache.Measure(_ctx.UiMessage, _config.UiMessageFontSize);
            int x = _config.ScreenWidth / 2 - textW / 2;
            Raylib.DrawText(_ctx.UiMessage, x, 16, _config.UiMessageFontSize, Color.Gold);
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
        int panelW = _config.ScreenWidth - _config.InventoryPanelWidthOffset;
        int panelH = _config.ScreenHeight - _config.InventoryPanelHeightOffset;

        var panelRect = new UiRect(panelX, panelY, panelW, panelH);
        UiDraw.Panel(panelRect, _theme.InvPanelBg, _theme.InvPanelBorder);
        Raylib.DrawText("Inventory (I to close)", panelX + 20, panelY + 14, 24, _theme.TextWhite);

        int cols = 5;
        int cellSize = _config.InventoryCellSize;
        int gap = _config.InventoryCellGap;
        int startX = panelX + _config.InventoryPanelPaddingX;
        int startY = panelY + _config.InventoryPanelPaddingY;

        HandleInventoryInput(playerId, ref inv, ref eq, ref stats, ref hp, startX, startY, cols, cellSize, gap);

        int newHovered = -1;
        var mouse = Raylib.GetMousePosition();

        for (int i = 0; i < inv.Capacity; i++)
        {
            int cx = i % cols;
            int cy = i / cols;
            int x = startX + cx * (cellSize + gap);
            int y = startY + cy * (cellSize + gap);

            var cellRect = new UiRect(x, y, cellSize, cellSize);
            bool isSelected = i == _selectedInvSlot && !_selectedEquip;
            UiDraw.Slot(cellRect, _theme.InvCellBg, _theme.InvCellBorder, isSelected, _theme.InvSelected);

            ref var slot = ref inv.Slots[i];
            if (!slot.Occupied) continue;

            DrawItemIcon(slot, cellRect);

            if (slot.Quantity > 1)
            {
                if (i < _cachedQuantityStrings.Length && _cachedQuantityValues[i] != slot.Quantity)
                {
                    _cachedQuantityValues[i] = slot.Quantity;
                    _cachedQuantityStrings[i] = "x" + slot.Quantity;
                }
                string qtyStr = i < _cachedQuantityStrings.Length ? _cachedQuantityStrings[i] : "x" + slot.Quantity;
                Raylib.DrawText(qtyStr, x + 4, y + cellSize - 16, 14, _theme.InvQuantity);
            }

            if (cellRect.Contains(mouse))
                newHovered = i;
        }

        // Equipment slots
        int ex = panelX + panelW - 270;
        int ey = panelY + _config.InventoryPanelPaddingY;
        DrawEquipSlot("Weapon", eq.Weapon, ex, ey, 0);
        DrawEquipSlot("Armor", eq.Armor, ex, ey + 38, 1);
        DrawEquipSlot("Amulet", eq.Amulet, ex, ey + 76, 2);
        DrawEquipSlot("Ring1", eq.Ring1, ex, ey + 114, 3);
        DrawEquipSlot("Ring2", eq.Ring2, ex, ey + 152, 4);

        // Статы героя — под equipment slots
        DrawPlayerStats(playerId, ref stats, ref hp, ex, ey + 200);

        // Tooltip — обновляем кеш только при смене hover
        if (newHovered != _hoveredSlot)
        {
            _hoveredSlot = newHovered;
            if (newHovered >= 0 && inv.Slots[newHovered].Occupied)
            {
                _cachedTooltip = BuildItemTooltip(inv.Slots[newHovered]);
                _cachedCompare = BuildCompareText(ref eq, inv.Slots[newHovered]);
            }
            else
            {
                _cachedTooltip = string.Empty;
                _cachedCompare = string.Empty;
            }
        }

        DrawTooltip(_cachedTooltip, _cachedCompare, panelX + 360, panelY + 56);

        // Модальное окно поверх всего
        if (_uiCtx.HasModal)
            UiModal.Draw(_uiCtx.TopModal, _config, _theme, _textCache);
    }

    private void DrawQuickSlots()
    {
        var world = _world;
        world.QueryInto<PlayerTag, QuickSlots>(_quickSlotsBuffer);
        if (_quickSlotsBuffer.Count == 0)
            return;

        int playerId = _quickSlotsBuffer[0];
        ref var quick = ref world.Get<QuickSlots>(playerId);

        int slotSize = _config.QuickSlotSize;
        int gap = _config.QuickSlotGap;
        int totalW = slotSize * 4 + gap * 3;
        int baseX = _config.ScreenWidth / 2 - totalW / 2;
        int baseY = _config.ScreenHeight - _config.QuickSlotBottomOffset;

        var layout = UiLayout.Row(baseX, baseY, gap);

        DrawQuickSlot(ref layout, 1, quick.Slot1DefinitionId, slotSize);
        DrawQuickSlot(ref layout, 2, quick.Slot2DefinitionId, slotSize);
        DrawQuickSlot(ref layout, 3, quick.Slot3DefinitionId, slotSize);
        DrawQuickSlot(ref layout, 4, quick.Slot4DefinitionId, slotSize);

        if (quick.PotionCooldown > 0 || quick.ScrollCooldown > 0)
        {
            // Кеш строки кулдауна — обновляем только при изменении (с точностью 0.1)
            float pRound = MathF.Round(quick.PotionCooldown, 1);
            float sRound = MathF.Round(quick.ScrollCooldown, 1);
            if (pRound != _cachedPotionCd || sRound != _cachedScrollCd)
            {
                _cachedPotionCd = pRound;
                _cachedScrollCd = sRound;
                _cachedCooldownText = "P:" + pRound.ToString("0.0") + " S:" + sRound.ToString("0.0");
            }
            Raylib.DrawText(_cachedCooldownText, baseX, baseY - 20, 14, Color.LightGray);
        }
    }

    private void DrawQuickSlot(ref UiLayout layout, int key, int definitionId, int slotSize)
    {
        var rect = layout.TakeSquare(slotSize);
        UiDraw.Slot(rect, _theme.QuickSlotBg, _theme.QuickSlotBorder, false, _theme.InvSelected);
        Raylib.DrawText(QuickSlotLabels[key - 1], rect.X + 2, rect.Y + 2, _config.QuickSlotKeyFontSize, _theme.TextWhite);

        if (definitionId < 0)
            return;

        if (_db.TryGetDefinition(definitionId, out var def))
        {
            var src = ItemAtlas.GetSource(definitionId, def.Type, ItemRarity.Common);
            int iconSize = _config.QuickSlotIconSize;
            int ix = rect.X + (slotSize - iconSize) / 2;
            int iy = rect.Y + (slotSize - iconSize) / 2 + 2;
            var dest = new Rectangle(ix, iy, iconSize, iconSize);
            Raylib.DrawTexturePro(_itemsTex, src, dest, System.Numerics.Vector2.Zero, 0f, Color.White);
        }
    }

    private void DrawEquipSlot(string name, ItemStack slot, int x, int y, int index)
    {
        bool selected = _selectedEquip && _selectedEquipSlot == index;
        var borderRect = new UiRect(x - 4, y - 2, 260, 30);
        Raylib.DrawRectangleLines(borderRect.X, borderRect.Y, borderRect.W, borderRect.H,
            selected ? _theme.InvSelected : _theme.EquipBorder);

        Raylib.DrawText(name, x, y + 4, 16, Color.SkyBlue);
        if (slot.Occupied)
        {
            var iconRect = new UiRect(x + 80, y - 2, 28, 28);
            DrawItemIcon(slot, iconRect);
            string value = slot.DefinitionId.ToString();
            if (_db.TryGetDefinition(slot.DefinitionId, out var def))
                value = def.Name;
            Raylib.DrawText(value, x + 114, y + 4, 16, _theme.TextWhite);
        }
        else
        {
            Raylib.DrawText("-", x + 100, y + 4, 16, _theme.TextWhite);
        }
    }

    private void DrawPlayerStats(int playerId, ref Stats stats, ref Health hp, int x, int y)
    {
        // Заголовок
        Raylib.DrawText("Stats", x, y, 16, _theme.SlotActive);

        // Кеш строк — обновляем только при смене значений
        if (_statCacheAtk != stats.ATK)
        { _statCacheAtk = stats.ATK; _statStrAtk = "ATK  " + stats.ATK; }

        if (_statCacheDef != stats.DEF)
        { _statCacheDef = stats.DEF; _statStrDef = "DEF  " + stats.DEF; }

        float spdRound = MathF.Round(stats.SPD, 1);
        if (_statCacheSpd != spdRound)
        { _statCacheSpd = spdRound; _statStrSpd = "SPD  " + spdRound.ToString("0.0"); }

        float critPct = MathF.Round(stats.Crit * 100f, 1);
        if (_statCacheCrit != critPct)
        { _statCacheCrit = critPct; _statStrCrit = "CRIT " + critPct.ToString("0.0") + "%"; }

        if (_statCacheInt != stats.INT)
        { _statCacheInt = stats.INT; _statStrInt = "INT  " + stats.INT; }

        if (_statCacheHp != hp.HP || _statCacheMaxHp != hp.MaxHP)
        { _statCacheHp = hp.HP; _statCacheMaxHp = hp.MaxHP; _statStrHp = "HP   " + hp.HP + "/" + hp.MaxHP; }

        bool hasMana = _world.Has<Mana>(playerId);
        if (hasMana)
        {
            ref var mana = ref _world.Get<Mana>(playerId);
            if (_statCacheMp != mana.MP || _statCacheMaxMp != mana.MaxMP)
            { _statCacheMp = mana.MP; _statCacheMaxMp = mana.MaxMP; _statStrMp = "MP   " + mana.MP + "/" + mana.MaxMP; }
        }

        // Отрисовка
        int lineH = 18;
        int sy = y + 22;
        var layout = UiLayout.Column(x, sy, 2);

        DrawStatLine(ref layout, _statStrHp, _theme.HpBarFull, lineH);
        if (hasMana)
            DrawStatLine(ref layout, _statStrMp, _theme.MpBarFill, lineH);
        DrawStatLine(ref layout, _statStrAtk, new Color(240, 120, 80, 255), lineH);
        DrawStatLine(ref layout, _statStrDef, new Color(100, 160, 240, 255), lineH);
        DrawStatLine(ref layout, _statStrSpd, new Color(120, 220, 160, 255), lineH);
        DrawStatLine(ref layout, _statStrCrit, new Color(240, 200, 80, 255), lineH);
        DrawStatLine(ref layout, _statStrInt, new Color(180, 120, 240, 255), lineH);

        // Активные статус-эффекты
        if (_world.Has<StatusEffects>(playerId))
        {
            ref var effects = ref _world.Get<StatusEffects>(playerId);
            if (effects.ActiveCount > 0)
            {
                layout.Skip(4);
                var effectLabelRect = layout.Take(200, lineH);
                Raylib.DrawText("Effects", effectLabelRect.X, effectLabelRect.Y, 14, _theme.SlotActive);

                for (int i = 0; i < effects.ActiveCount; i++)
                {
                    var slot = effects.GetSlot(i);
                    int secs = (int)MathF.Ceiling(slot.Duration);
                    if (_cachedEffectTypes[i] != slot.Type || _cachedEffectSecs[i] != secs)
                    {
                        _cachedEffectTypes[i] = slot.Type;
                        _cachedEffectSecs[i] = secs;
                        _cachedEffectLines[i] = slot.Type.ToString() + "  " + secs + "s";
                    }
                    var lineRect = layout.Take(200, lineH);
                    Color effectColor = slot.Type switch
                    {
                        SpellEffectType.Burn => new Color(255, 100, 20, 255),
                        SpellEffectType.Slow => new Color(100, 180, 255, 255),
                        _ => _theme.TextWhite
                    };
                    Raylib.DrawText(_cachedEffectLines[i], lineRect.X + 8, lineRect.Y, 14, effectColor);
                }
            }
        }
    }

    private static void DrawStatLine(ref UiLayout layout, string text, Color color, int lineH)
    {
        var rect = layout.Take(200, lineH);
        Raylib.DrawText(text, rect.X + 8, rect.Y, 14, color);
    }

    private void HandleInventoryInput(int playerId, ref Inventory inv, ref Equipment eq,
        ref Stats stats, ref Health hp,
        int startX, int startY, int cols, int cellSize, int gap)
    {
        if (_uiCtx.InputConsumed || _uiCtx.HasModal)
            return;

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

            var cellRect = new UiRect(x, y, cellSize, cellSize);
            if (!cellRect.Contains(mouse))
                continue;

            _uiCtx.InputConsumed = true;
            HandleInventorySlotClick(ref inv, ref eq, ref stats, ref hp, i);
            return;
        }

        // Equipment click area
        int ex = _config.InventoryPanelX + (_config.ScreenWidth - _config.InventoryPanelWidthOffset) - 270;
        int ey = _config.InventoryPanelY + _config.InventoryPanelPaddingY;
        for (int i = 0; i < 5; i++)
        {
            int rowY = ey + i * 38;
            var equipRect = new UiRect(ex - 4, rowY - 2, 260, 30);
            if (!equipRect.Contains(mouse))
                continue;

            _uiCtx.InputConsumed = true;
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

        int w = _config.TooltipWidth;
        int h = string.IsNullOrEmpty(compare) ? _config.TooltipBaseHeight : _config.TooltipCompareHeight;
        var rect = new UiRect(x, y, w, h);
        UiDraw.Tooltip(rect, _theme.TooltipBg, _theme.TooltipBorder);

        Raylib.DrawText(tooltip, x + _config.TooltipPadding, y + 8, _config.TooltipFontSize, _theme.TextWhite);
        if (!string.IsNullOrEmpty(compare))
            Raylib.DrawText(compare, x + _config.TooltipPadding, y + 62, _config.TooltipCompareFontSize, _theme.TooltipCompare);
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

    private void DrawItemIcon(ItemStack item, UiRect rect)
    {
        var src = ItemAtlas.GetSource(item.DefinitionId, item.Type, item.Rarity);
        Color border = UiTheme.RarityColor(item.Rarity);
        Raylib.DrawRectangleLines(rect.X, rect.Y, rect.W, rect.H, border);
        UiDraw.ItemIcon(rect, _itemsTex, src, Color.White);
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
