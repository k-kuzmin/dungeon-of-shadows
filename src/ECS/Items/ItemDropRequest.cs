using DungeonOfShadows.ECS.Combat;

namespace DungeonOfShadows.ECS.Items;

public readonly record struct ItemDropRequest(
    EnemyType EnemyType,
    float WorldX,
    float WorldY,
    int Floor
);
