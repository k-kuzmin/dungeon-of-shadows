namespace DungeonOfShadows.ECS.Combat;

public struct MeleeAttack
{
    public float DirX;
    public float DirY;
    public float ArcRadians;
    public float Radius;
    public float TimeRemaining;
    public float Duration;
    public bool HitScanned;

    /// <summary>
    /// ID сущностей, уже получивших урон от этой атаки (для однократного попадания).
    /// </summary>
    public List<int>? AlreadyHit;
}
