using Raylib_cs;

namespace DungeonOfShadows.ECS.Items;

/// <summary>
/// Маппинг предметов на координаты в спрайтшите items.png (8 cols × 8 rows, 16×16 grid).
///
/// Layout:
///   Row 0: swords     (col = rarity 0-4)
///   Row 1: armors     (col = rarity 0-4)
///   Row 2: rings #1   (col = rarity 0-4)
///   Row 3: rings #2   (col = rarity 0-4)
///   Row 4: amulets    (col = rarity 0-4)
///   Row 5: potions    (col 0-1: HP small a/b, col 2-3: HP big a/b, col 4-5: MP a/b)
///   Row 6: scrolls    (8 вариантов)
///   Row 7: spell scrolls (8 вариантов)
/// </summary>
public static class ItemAtlas
{
    private const int T = 32;

    // Экипировка: defId → row. Колонка = (int)rarity (0=Common..4=Legendary).
    private static readonly Dictionary<int, int> _equipRow = new()
    {
        { 100, 0 }, // Iron Sword
        { 101, 1 }, // Leather Armor
        { 102, 2 }, // Hunter Ring
        { 103, 4 }, // Copper Amulet
    };

    // Расходники: defId → (row, col). Не зависят от редкости.
    private static readonly Dictionary<int, (int Row, int Col)> _fixedSprite = new()
    {
        { 200, (5, 2) }, // Health Potion → potion_02a (большая бутылка HP)
        { 300, (6, 0) }, // Scroll of Nova → scroll_01a

        // Spell scrolls — каждому своя иконка
        { 400, (7, 0) }, // Magic Bolt
        { 401, (7, 1) }, // Fireball
        { 402, (7, 2) }, // Frost Nova
        { 403, (7, 3) }, // Chain Lightning
        { 404, (7, 4) }, // Shadow Step
        { 405, (7, 5) }, // Heal
    };

    // Fallback по ItemType → row (для предметов, не зарегистрированных явно)
    private static readonly Dictionary<ItemType, int> _typeRow = new()
    {
        { ItemType.Weapon, 0 },
        { ItemType.Armor, 1 },
        { ItemType.Ring, 2 },
        { ItemType.Amulet, 4 },
        { ItemType.Potion, 5 },
        { ItemType.Scroll, 6 },
        { ItemType.SpellScroll, 7 },
    };

    /// <summary>
    /// Возвращает source rectangle в спрайтшите items.png для данного предмета.
    /// </summary>
    public static Rectangle GetSource(int definitionId, ItemType type, ItemRarity rarity)
    {
        // Фиксированный спрайт (расходники)
        if (_fixedSprite.TryGetValue(definitionId, out var fix))
            return new Rectangle(fix.Col * T, fix.Row * T, T, T);

        // Экипировка — колонка = редкость
        int col = (int)rarity;
        if (_equipRow.TryGetValue(definitionId, out int row))
            return new Rectangle(col * T, row * T, T, T);

        // Fallback по типу
        if (_typeRow.TryGetValue(type, out int fallbackRow))
            return new Rectangle(col * T, fallbackRow * T, T, T);

        // Абсолютный fallback — первый спрайт
        return new Rectangle(0, 0, T, T);
    }
}
