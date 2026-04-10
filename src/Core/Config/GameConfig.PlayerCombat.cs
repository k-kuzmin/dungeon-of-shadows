namespace DungeonOfShadows.Core;

public partial class GameConfig
{
    // Player
    public float PlayerSpeed { get; init; } = 5.5f;

    // Combat — player melee
    public float PlayerMeleeRange { get; init; } = 2.5f;
    public float PlayerMeleeDuration { get; init; } = 0.18f;
    public float PlayerMeleeCooldown { get; init; } = 0.35f;
    public float PlayerMeleeArcRadians { get; init; } = 1.57f; // ~90°
    public int PlayerBaseATK { get; init; } = 8;
    public int PlayerBaseDEF { get; init; } = 2;
    public float PlayerBaseCrit { get; init; } = 0.05f;
    public int PlayerBaseHP { get; init; } = 100;

    // Combat — dash
    public float PlayerDashDuration { get; init; } = 0.14f;
    public float PlayerDashCooldown { get; init; } = 0.9f;
    public float PlayerDashSpeed { get; init; } = 14f;
    public float PlayerIFrameDuration { get; init; } = 0.25f;
}
