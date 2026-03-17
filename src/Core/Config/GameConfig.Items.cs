namespace DungeonOfShadows.Core;

public partial class GameConfig
{
    // Items - inventory
    public int InventorySlots { get; init; } = 20;
    public int ItemMaxStackSize { get; init; } = 10;

    // Items - interaction
    public float ItemPickupRadiusTiles { get; init; } = 1.6f;
    public float ChestInteractRadiusTiles { get; init; } = 1.8f;

    // Items - drops
    public float RatDropChance { get; init; } = 0.35f;
    public float GoblinDropChance { get; init; } = 0.55f;
    public float SkeletonDropChance { get; init; } = 0.75f;

    // Items - rarity weights
    public int RarityWeightCommon { get; init; } = 62;
    public int RarityWeightUncommon { get; init; } = 24;
    public int RarityWeightRare { get; init; } = 10;
    public int RarityWeightEpic { get; init; } = 3;
    public int RarityWeightLegendary { get; init; } = 1;
    public float RarityScaleRare { get; init; } = 1.10f;
    public float RarityScaleEpic { get; init; } = 1.20f;
    public float RarityScaleLegendary { get; init; } = 1.30f;

    // Items - consumables
    public float PotionCooldownSeconds { get; init; } = 2.5f;
    public float ScrollCooldownSeconds { get; init; } = 6.0f;
    public float ScrollNovaRadiusTiles { get; init; } = 3.5f;

    // Items - chests
    public int ChestsPerFloorMin { get; init; } = 1;
    public int ChestsPerFloorMax { get; init; } = 3;
    public int FloorRngMixMultiplier { get; init; } = 97;

    // Items - world render
    public int GroundItemDrawHalfSize { get; init; } = 5;
    public int GroundItemDrawSize { get; init; } = 10;
    public int ChestDrawOffsetX { get; init; } = 8;
    public int ChestDrawOffsetY { get; init; } = 6;
    public int ChestDrawWidth { get; init; } = 16;
    public int ChestDrawHeight { get; init; } = 12;
    public float ItemDropOffsetPixels { get; init; } = 8f;

    // Items - inventory UI
    public int InventoryPanelX { get; init; } = 120;
    public int InventoryPanelY { get; init; } = 90;
    public int InventoryPanelPaddingX { get; init; } = 24;
    public int InventoryPanelPaddingY { get; init; } = 56;
    public int InventoryCellSize { get; init; } = 56;
    public int InventoryCellGap { get; init; } = 8;

    // UI messages
    public float UiMessageSeconds { get; init; } = 1.8f;
}
