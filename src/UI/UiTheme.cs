using Raylib_cs;
using DungeonOfShadows.ECS.Items;
using DungeonOfShadows.ECS.Magic;

namespace DungeonOfShadows.UI;

/// <summary>
/// Единый источник цветов и размеров шрифтов для всего UI.
/// Singleton через DI, инициализируется один раз.
/// </summary>
public class UiTheme
{
    // --- HP bar ---
    public readonly Color HpBarBg = new(40, 40, 40, 200);
    public readonly Color HpBarFull = new(50, 200, 60, 255);
    public readonly Color HpBarLow = new(200, 50, 50, 255);

    // --- MP bar ---
    public readonly Color MpBarFill = new(30, 100, 220, 255);

    // --- Spell slots ---
    public readonly Color SlotBg = new(30, 30, 40, 200);
    public readonly Color SlotBorder = new(120, 120, 140, 200);
    public readonly Color SlotActive = new(220, 180, 50, 255);
    public readonly Color SlotEmpty = new(80, 80, 80, 150);
    public readonly Color CooldownOverlay = new(0, 0, 0, 140);
    public readonly Color HintColor = new(180, 180, 180, 200);
    public readonly Color ManaCostColor = new(100, 160, 255, 220);

    // --- Inventory ---
    public readonly Color InvPanelBg = new(20, 20, 24, 235);
    public readonly Color InvPanelBorder = new(180, 180, 180, 255);
    public readonly Color InvCellBg = new(45, 45, 54, 255);
    public readonly Color InvCellBorder = new(140, 140, 150, 255);
    public readonly Color InvSelected = new(255, 220, 120, 255);
    public readonly Color InvQuantity = new(230, 230, 140, 255);
    public readonly Color EquipBorder = new(120, 120, 130, 180);

    // --- Quick slots ---
    public readonly Color QuickSlotBg = new(32, 32, 40, 220);
    public readonly Color QuickSlotBorder = new(160, 160, 170, 255);

    // --- Tooltip ---
    public readonly Color TooltipBg = new(10, 10, 15, 225);
    public readonly Color TooltipBorder = new(190, 190, 210, 255);
    public readonly Color TooltipCompare = new(220, 210, 120, 255);

    // --- Minimap ---
    public readonly Color MinimapBg = new(0, 0, 0, 200);
    public readonly Color MinimapWall = new(60, 50, 70, 255);
    public readonly Color MinimapFloor = new(120, 110, 100, 255);
    public readonly Color MinimapExplored = new(60, 55, 50, 255);
    public readonly Color MinimapStair = new(220, 180, 50, 255);
    public readonly Color MinimapPlayer = new(60, 220, 75, 255);
    public readonly Color MinimapBorder = new(150, 140, 130, 200);

    // --- General ---
    public readonly Color BarBorder = Color.White;
    public readonly Color TextWhite = Color.White;

    /// <summary>
    /// Интерполяция цвета HP бара от Low к Full.
    /// </summary>
    public Color HpInterpolated(float fraction)
    {
        return new Color(
            (byte)(HpBarLow.R + (HpBarFull.R - HpBarLow.R) * fraction),
            (byte)(HpBarLow.G + (HpBarFull.G - HpBarLow.G) * fraction),
            (byte)(HpBarLow.B + (HpBarFull.B - HpBarLow.B) * fraction),
            (byte)255);
    }

    public static Color RarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => new Color(160, 160, 170, 255),
        ItemRarity.Uncommon => new Color(90, 220, 110, 255),
        ItemRarity.Rare => new Color(90, 140, 240, 255),
        ItemRarity.Epic => new Color(210, 80, 220, 255),
        ItemRarity.Legendary => new Color(245, 200, 70, 255),
        _ => Color.White
    };

    public static Color SpellColor(SpellId spellId) => spellId switch
    {
        SpellId.MagicBolt => new Color(160, 80, 220, 255),
        SpellId.Fireball => new Color(255, 100, 20, 255),
        SpellId.FrostNova => new Color(100, 180, 255, 255),
        SpellId.ChainLightning => new Color(80, 200, 255, 255),
        SpellId.ShadowStep => new Color(120, 60, 160, 255),
        SpellId.Heal => new Color(50, 220, 80, 255),
        _ => Color.White
    };
}
