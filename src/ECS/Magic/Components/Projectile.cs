namespace DungeonOfShadows.ECS.Magic.Components;

public struct Projectile
{
    public int OwnerId;
    public int SpellId;
    public int Damage;
    public float LifeRemaining;
    public bool IsAoe;
    public float AoeRadius;
    public SpellEffectType OnHitEffect;
    public float EffectDuration;
    public int EffectDamagePerTick;
    public float SlowFactor;
    public int ChainMaxTargets;
    public float ChainRadius;
    public int ChainCount;
}
