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
}
