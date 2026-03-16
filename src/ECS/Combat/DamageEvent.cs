namespace DungeonOfShadows.ECS.Combat;

public readonly record struct DamageEvent(
    int AttackerId,
    int TargetId,
    int Damage,
    bool IsCrit,
    float WorldX,
    float WorldY
);
