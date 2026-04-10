namespace DungeonOfShadows.Core;

public partial class GameConfig
{
    // Combat — damage formula
    public float DefReduction { get; init; } = 0.7f;
    public float CritMultiplier { get; init; } = 1.8f;
    public float DamageFlashDuration { get; init; } = 0.12f;

    // Combat — enemies
    public float EnemyStatFloorScale { get; init; } = 0.15f;
    public int EnemiesPerRoomMin { get; init; } = 1;
    public int EnemiesPerRoomMax { get; init; } = 3;
    public float AiPathUpdateInterval { get; init; } = 0.35f;
    public float RatFleeHealthThreshold { get; init; } = 0.3f;
    public int AiPatrolSearchAttempts { get; init; } = 10;
    public int AiPatrolSearchRadius { get; init; } = 4;
    public float AiPatrolStopDistanceTiles { get; init; } = 0.5f;
    public float AiPatrolWaitMinSeconds { get; init; } = 1f;
    public float AiPatrolWaitMaxSeconds { get; init; } = 3f;
    public float AiPatrolSpeedMultiplier { get; init; } = 0.5f;
    public float AiChaseWaypointReachDistanceTiles { get; init; } = 0.3f;
    public float GoblinStrafeSpeedMultiplier { get; init; } = 0.9f;
    public float GoblinStrafeDurationMin { get; init; } = 0.35f;
    public float GoblinStrafeDurationMax { get; init; } = 0.75f;
    public float SkeletonHeavyWindup { get; init; } = 0.28f;
    public float SkeletonHeavyDamageMultiplier { get; init; } = 1.5f;

    // Combat — damage numbers
    public float DamageNumberLifetime { get; init; } = 0.9f;
    public float DamageNumberSpeed { get; init; } = 40f;

    // Combat — afterimage
    public float AfterimageSpawnInterval { get; init; } = 0.04f;
    public float AfterimageStartAlpha { get; init; } = 0.5f;
    public float AfterimageLifetime { get; init; } = 0.25f;

    // Combat — enemy death (увеличено для проигрывания полной анимации смерти)
    public float EnemyDeathAnimationDuration { get; init; } = 1.0f;
}
