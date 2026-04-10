namespace DungeonOfShadows.ECS.Combat;

public struct DashCooldown
{
    public float TimeRemaining;

    public DashCooldown(float time) { TimeRemaining = time; }
}
