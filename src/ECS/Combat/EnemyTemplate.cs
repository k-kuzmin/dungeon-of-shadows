namespace DungeonOfShadows.ECS.Combat;

public readonly record struct EnemyTemplate(
    EnemyType Type,
    int BaseHP,
    int BaseATK,
    int BaseDEF,
    float Speed,
    float DetectionRadius,
    float AttackRange,
    float AttackCooldown,
    Raylib_cs.Color Tint
);
