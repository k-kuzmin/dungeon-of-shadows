namespace DungeonOfShadows.Core;

public partial class GameConfig
{
    // Mana
    public int PlayerBaseMana { get; init; } = 30;
    public int PlayerBaseINT { get; init; } = 5;
    public float ManaRegenInterval { get; init; } = 3.0f;

    // Spells — general
    public int ProjectileRenderRadius { get; init; } = 6;
    public float ProjectileCollisionRadius { get; init; } = 0.3f;

    // Status effects
    public float BurnTickInterval { get; init; } = 1.0f;
    public float SlowDuration { get; init; } = 3.0f;
    public float SlowFactor { get; init; } = 0.4f;

    // Shadow Step
    public int ShadowStepTiles { get; init; } = 5;
    public float ShadowStepIFrames { get; init; } = 0.3f;

    // AoE visuals
    public float AoEVisualDuration { get; init; } = 0.3f;

    // Spell levels
    public int MaxSpellLevel { get; init; } = 5;
    public float SpellLevelDamageBonus { get; init; } = 0.15f;
    public float SpellLevelHealBonus { get; init; } = 0.15f;
    public float SpellLevelCooldownReduction { get; init; } = 0.08f;

    // Spell slot unlocks (этаж → новый capacity)
    public int SpellSlotInitialCapacity { get; init; } = 3;
    public int SpellSlotUnlockFloor4 { get; init; } = 4;
    public int SpellSlotCapacityAtFloor4 { get; init; } = 4;
    public int SpellSlotUnlockFloor7 { get; init; } = 7;
    public int SpellSlotCapacityAtFloor7 { get; init; } = 5;
    public int SpellSlotMaxCapacity { get; init; } = 5;
}
