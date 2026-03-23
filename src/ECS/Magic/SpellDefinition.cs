using System.Text.Json.Serialization;

namespace DungeonOfShadows.ECS.Magic;

public sealed class SpellDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SpellId SpellId { get; set; }
    public int ManaCost { get; set; }
    public float CastCooldown { get; set; }

    /// <summary>Базовый урон заклинания (до INT scaling).</summary>
    public int BaseDamage { get; set; }

    /// <summary>Множитель INT для урона: finalDamage = BaseDamage + INT * IntScaling.</summary>
    public float IntScaling { get; set; } = 2f;

    /// <summary>Скорость снаряда (в тайлах/с). 0 = мгновенное заклинание.</summary>
    public float ProjectileSpeed { get; set; }

    /// <summary>Радиус AoE в тайлах. 0 = single target.</summary>
    public float AoeRadius { get; set; }

    /// <summary>Дальность полёта снаряда в тайлах (определяет lifetime).</summary>
    public float Range { get; set; } = 10f;

    /// <summary>Взрывается ли снаряд при попадании (AoE).</summary>
    public bool ExplodeOnHit { get; set; }

    /// <summary>Тип статус-эффекта при попадании.</summary>
    public SpellEffectType OnHitEffect { get; set; }

    /// <summary>Длительность статус-эффекта.</summary>
    public float EffectDuration { get; set; }

    /// <summary>Урон за тик (для Burn).</summary>
    public int EffectDamagePerTick { get; set; }

    /// <summary>Множитель замедления (для Slow: 0.4 = 60% slow).</summary>
    public float SlowFactor { get; set; }

    /// <summary>Базовое лечение (для Heal).</summary>
    public int BaseHeal { get; set; }

    /// <summary>Множитель INT для лечения: finalHeal = BaseHeal + INT * HealIntScaling.</summary>
    public float HealIntScaling { get; set; }

    /// <summary>Дальность телепорта в тайлах (для ShadowStep).</summary>
    public int TeleportRange { get; set; }

    /// <summary>Максимум целей для Chain Lightning.</summary>
    public int ChainMaxTargets { get; set; }

    /// <summary>Радиус поиска целей для Chain Lightning (в тайлах).</summary>
    public float ChainRadius { get; set; }

    /// <summary>Минимальный этаж для нахождения свитка этого заклинания.</summary>
    public int MinFloor { get; set; } = 1;

    /// <summary>Является ли заклинание проджектайлом.</summary>
    [JsonIgnore]
    public bool IsProjectile => ProjectileSpeed > 0;

    /// <summary>Является ли заклинание мгновенным AoE (не проджектайл).</summary>
    [JsonIgnore]
    public bool IsInstantAoe => ProjectileSpeed <= 0 && AoeRadius > 0;

}
