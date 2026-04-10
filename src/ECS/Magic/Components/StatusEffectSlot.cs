namespace DungeonOfShadows.ECS.Magic.Components;

public struct StatusEffectSlot
{
    public SpellEffectType Type;
    public float Duration;
    public float TickAccumulator;
    public int DamagePerTick;
    public float SlowFactor;
}
