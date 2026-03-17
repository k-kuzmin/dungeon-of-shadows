using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Combat;

public static class DamageCalculator
{
    /// <summary>
    /// Рассчитывает урон по формуле: (ATK * weaponMult) - (DEF * 0.7) + random(-1, +2).
    /// Крит: damage * 1.8 при шансе = attacker.Crit.
    /// </summary>
    public static (int damage, bool isCrit) Compute(
        ref Stats attacker, ref Stats defender, GameConfig config, Random rng,
        float weaponMult = 1f)
    {
        float raw = attacker.ATK * weaponMult;
        float reduced = raw - defender.DEF * config.DefReduction;
        int jitter = rng.Next(-1, 3); // -1 до +2 включительно
        int damage = Math.Max(1, (int)(reduced + jitter));

        bool isCrit = rng.NextSingle() < attacker.Crit;
        if (isCrit)
            damage = (int)(damage * config.CritMultiplier);

        return (damage, isCrit);
    }

    /// <summary>
    /// Детерминированный расчёт урона без аллокаций Random.
    /// </summary>
    public static (int damage, bool isCrit) ComputeDeterministic(
        ref Stats attacker, ref Stats defender, GameConfig config, ref uint state,
        float weaponMult = 1f)
    {
        float raw = attacker.ATK * weaponMult;
        float reduced = raw - defender.DEF * config.DefReduction;
        int jitter = NextRange(ref state, -1, 3); // -1..2
        int damage = Math.Max(1, (int)(reduced + jitter));

        bool isCrit = NextFloat(ref state) < attacker.Crit;
        if (isCrit)
            damage = (int)(damage * config.CritMultiplier);

        return (damage, isCrit);
    }

    /// <summary>
    /// Скейлит целочисленный стат по этажу: base * (1 + floor * scale).
    /// </summary>
    public static int ScaleStat(int baseStat, int floor, float scale) =>
        (int)(baseStat * (1f + floor * scale));

    /// <summary>
    /// Скейлит дробный стат по этажу.
    /// </summary>
    public static float ScaleStat(float baseStat, int floor, float scale) =>
        baseStat * (1f + floor * scale);

    private static int NextRange(ref uint state, int minInclusive, int maxExclusive)
    {
        uint n = NextUInt(ref state);
        int span = maxExclusive - minInclusive;
        return minInclusive + (int)(n % (uint)span);
    }

    private static float NextFloat(ref uint state)
    {
        return (NextUInt(ref state) & 0xFFFFFF) / 16777216f;
    }

    private static uint NextUInt(ref uint state)
    {
        if (state == 0)
            state = 0x6E624EB7u;

        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }
}
