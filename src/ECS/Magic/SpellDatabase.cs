using System.Text.Json;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Magic;

public sealed class SpellDatabase : IStartable
{
    private readonly Dictionary<int, SpellDefinition> _defs = new();

    public void Start()
    {
        Load();
        if (_defs.Count == 0)
            LoadFallback();
    }

    public bool TryGet(int id, out SpellDefinition def) => _defs.TryGetValue(id, out def!);

    public SpellDefinition? Get(int id) => _defs.GetValueOrDefault(id);

    private void Load()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "assets", "data", "spells.json");
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            var defs = JsonSerializer.Deserialize(json, SpellsJsonContext.Default.SpellDefinitionArray);
            if (defs == null) return;

            for (int i = 0; i < defs.Length; i++)
                Add(defs[i]);
        }
        catch
        {
            // Fallback при ошибке загрузки
        }
    }

    private void LoadFallback()
    {
        Add(new SpellDefinition
        {
            Id = 1, Name = "Magic Bolt", SpellId = SpellId.MagicBolt,
            ManaCost = 3, CastCooldown = 0.3f,
            BaseDamage = 10, IntScaling = 2f,
            ProjectileSpeed = 8f, Range = 10f
        });

        Add(new SpellDefinition
        {
            Id = 2, Name = "Fireball", SpellId = SpellId.Fireball,
            ManaCost = 8, CastCooldown = 1.5f,
            BaseDamage = 18, IntScaling = 2.5f,
            ProjectileSpeed = 6f, Range = 8f,
            ExplodeOnHit = true, AoeRadius = 1.5f,
            OnHitEffect = SpellEffectType.Burn, EffectDuration = 3f, EffectDamagePerTick = 5
        });

        Add(new SpellDefinition
        {
            Id = 3, Name = "Frost Nova", SpellId = SpellId.FrostNova,
            ManaCost = 6, CastCooldown = 2.5f,
            BaseDamage = 8, IntScaling = 1f,
            AoeRadius = 2.5f,
            OnHitEffect = SpellEffectType.Slow, EffectDuration = 3f, SlowFactor = 0.4f
        });

        Add(new SpellDefinition
        {
            Id = 4, Name = "Chain Lightning", SpellId = SpellId.ChainLightning,
            ManaCost = 10, CastCooldown = 2.0f,
            BaseDamage = 15, IntScaling = 2f,
            ProjectileSpeed = 12f, Range = 8f,
            ChainMaxTargets = 4, ChainRadius = 4f
        });

        Add(new SpellDefinition
        {
            Id = 5, Name = "Shadow Step", SpellId = SpellId.ShadowStep,
            ManaCost = 5, CastCooldown = 4.0f,
            TeleportRange = 5
        });

        Add(new SpellDefinition
        {
            Id = 6, Name = "Heal", SpellId = SpellId.Heal,
            ManaCost = 7, CastCooldown = 5.0f,
            BaseHeal = 20, HealIntScaling = 1.5f
        });
    }

    private void Add(SpellDefinition def)
    {
        _defs.TryAdd(def.Id, def);
    }
}
