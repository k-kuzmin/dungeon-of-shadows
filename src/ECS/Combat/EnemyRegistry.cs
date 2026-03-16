using Raylib_cs;

namespace DungeonOfShadows.ECS.Combat;

/// <summary>
/// Определения базовых врагов. Стат-скейлинг применяется при спавне.
/// </summary>
public static class EnemyRegistry
{
    public static EnemyTemplate Rat => new(
        Type: EnemyType.Rat,
        BaseHP: 8,
        BaseATK: 2,
        BaseDEF: 0,
        Speed: 4.0f,
        DetectionRadius: 5.5f,
        AttackRange: 0.9f,
        AttackCooldown: 1.2f,
        Tint: new Color(140, 100, 80, 255) // коричневый
    );

    public static EnemyTemplate Goblin => new(
        Type: EnemyType.Goblin,
        BaseHP: 15,
        BaseATK: 4,
        BaseDEF: 1,
        Speed: 3.5f,
        DetectionRadius: 6.0f,
        AttackRange: 1.0f,
        AttackCooldown: 1.5f,
        Tint: new Color(80, 160, 60, 255) // зелёный
    );

    public static EnemyTemplate Skeleton => new(
        Type: EnemyType.Skeleton,
        BaseHP: 20,
        BaseATK: 6,
        BaseDEF: 2,
        Speed: 2.5f,
        DetectionRadius: 4.5f,
        AttackRange: 1.1f,
        AttackCooldown: 2.2f,
        Tint: new Color(200, 200, 190, 255) // бледный
    );

    public static EnemyTemplate Get(EnemyType type) => type switch
    {
        EnemyType.Rat => Rat,
        EnemyType.Goblin => Goblin,
        EnemyType.Skeleton => Skeleton,
        _ => Rat
    };

    /// <summary>
    /// Выбирает тип врага с учётом этажа.
    /// Нижние этажи — больше крыс, верхние — больше скелетов.
    /// </summary>
    public static EnemyType PickType(int floor, Random rng)
    {
        int roll = rng.Next(100);
        return floor switch
        {
            <= 3 => roll < 60 ? EnemyType.Rat : roll < 90 ? EnemyType.Goblin : EnemyType.Skeleton,
            <= 6 => roll < 30 ? EnemyType.Rat : roll < 70 ? EnemyType.Goblin : EnemyType.Skeleton,
            _    => roll < 15 ? EnemyType.Rat : roll < 45 ? EnemyType.Goblin : EnemyType.Skeleton,
        };
    }
}
