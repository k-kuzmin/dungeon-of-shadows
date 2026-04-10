namespace DungeonOfShadows.ECS.Combat;

public struct EnemyDeathState
{
    public float TimeRemaining;
    public float Lifetime;

    public EnemyDeathState(float lifetime)
    {
        TimeRemaining = lifetime;
        Lifetime = lifetime;
    }
}
