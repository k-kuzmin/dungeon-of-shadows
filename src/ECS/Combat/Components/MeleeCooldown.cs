namespace DungeonOfShadows.ECS.Combat;

public struct MeleeCooldown
{
    public float TimeRemaining;

    public MeleeCooldown(float time)
    {
        TimeRemaining = time;
    }
}
