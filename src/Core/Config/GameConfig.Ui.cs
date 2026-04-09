namespace DungeonOfShadows.Core;

public partial class GameConfig
{
    // --- HP / MP bars ---
    public int HpBarX { get; init; } = 10;
    public int HpBarWidth { get; init; } = 200;
    public int HpBarHeight { get; init; } = 16;
    public int HpBarFontSize { get; init; } = 14;
    public int MpBarHeight { get; init; } = 12;
    public int MpBarFontSize { get; init; } = 12;
    public int BarGap { get; init; } = 4;

    // --- Spell slots ---
    public int SpellSlotWidth { get; init; } = 52;
    public int SpellSlotHeight { get; init; } = 38;
    public int SpellSlotGap { get; init; } = 6;
    public int SpellSlotBottomOffset { get; init; } = 68;
    public int SpellHintFontSize { get; init; } = 12;
    public int SpellNameFontSize { get; init; } = 11;
    public int SpellCostFontSize { get; init; } = 10;
    public int SpellTypeBarHeight { get; init; } = 4;
    public int SpellTypeBarPadding { get; init; } = 2;

    // --- Quick slots ---
    public int QuickSlotSize { get; init; } = 52;
    public int QuickSlotGap { get; init; } = 8;
    public int QuickSlotBottomOffset { get; init; } = 62;
    public int QuickSlotIconSize { get; init; } = 32;
    public int QuickSlotKeyFontSize { get; init; } = 12;

    // --- General UI ---
    public int DebugLabelY { get; init; } = 30;
    public int FloorLabelFontSize { get; init; } = 20;
    public int FloorLabelBottomOffset { get; init; } = 30;
    public int UiMessageFontSize { get; init; } = 22;

    // --- Tooltip ---
    public int TooltipWidth { get; init; } = 300;
    public int TooltipBaseHeight { get; init; } = 72;
    public int TooltipCompareHeight { get; init; } = 100;
    public int TooltipPadding { get; init; } = 10;
    public int TooltipFontSize { get; init; } = 16;
    public int TooltipCompareFontSize { get; init; } = 14;

    // --- Inventory panel ---
    public int InventoryPanelWidthOffset { get; init; } = 240;
    public int InventoryPanelHeightOffset { get; init; } = 180;

    // --- Modal ---
    public int ModalWidth { get; init; } = 320;
    public int ModalHeight { get; init; } = 160;
    public int ModalTitleFontSize { get; init; } = 22;
    public int ModalTextFontSize { get; init; } = 16;
    public int ModalButtonFontSize { get; init; } = 16;

    // --- Selection Modal ---
    public int SelectionModalOptionHeight { get; init; } = 24;
    public int SelectionModalOptionFontSize { get; init; } = 16;
    public int SelectionModalTitleFontSize { get; init; } = 20;
    public int SelectionModalPaddingTop { get; init; } = 44;
    public int SelectionModalPaddingX { get; init; } = 16;
    public int SelectionModalHintFontSize { get; init; } = 12;
}
