using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Magic.Components;

namespace DungeonOfShadows.ECS.Magic;

/// <summary>
/// Статические утилиты магической системы — переиспользуемые операции над компонентами.
/// </summary>
public static class MagicHelper
{
    public static void SpawnAoEVisual(World world, GameConfig config,
        float x, float y, float radius, byte r, byte g, byte b, byte a)
    {
        float lifetime = config.AoEVisualDuration;
        int id = world.CreateEntity();
        world.Add(id, new Position(x, y));
        world.Add(id, new AoEVisual
        {
            Radius = radius,
            TimeRemaining = lifetime,
            Lifetime = lifetime,
            R = r, G = g, B = b, A = a
        });
    }

    public static void ApplyStatusEffect(World world, int targetId,
        SpellEffectType type, float duration, int dmgPerTick, float slowFactor)
    {
        if (type == SpellEffectType.None) return;

        if (!world.Has<StatusEffects>(targetId))
            world.Add(targetId, new StatusEffects());

        ref var effects = ref world.Get<StatusEffects>(targetId);
        AddOrRefreshEffect(ref effects, type, duration, dmgPerTick, slowFactor);
    }

    public static void AddOrRefreshEffect(ref StatusEffects effects,
        SpellEffectType type, float duration, int damagePerTick, float slowFactor)
    {
        if (type == SpellEffectType.None) return;

        // Поиск существующего эффекта того же типа — refresh
        for (int i = 0; i < effects.ActiveCount; i++)
        {
            var slot = effects.GetSlot(i);
            if (slot.Type == type)
            {
                slot.Duration = MathF.Max(slot.Duration, duration);
                slot.DamagePerTick = Math.Max(slot.DamagePerTick, damagePerTick);
                if (type == SpellEffectType.Slow)
                    slot.SlowFactor = MathF.Min(slot.SlowFactor, slowFactor);
                effects.SetSlot(i, slot);
                return;
            }
        }

        // Добавление нового эффекта
        if (effects.ActiveCount >= 4) return;

        var newSlot = new StatusEffectSlot
        {
            Type = type,
            Duration = duration,
            TickAccumulator = 0f,
            DamagePerTick = damagePerTick,
            SlowFactor = slowFactor
        };
        effects.SetSlot(effects.ActiveCount, newSlot);
        effects.ActiveCount++;
    }

    public static void RemoveEffectAt(ref StatusEffects effects, int index)
    {
        for (int i = index; i < effects.ActiveCount - 1; i++)
            effects.SetSlot(i, effects.GetSlot(i + 1));

        effects.ActiveCount--;
        effects.SetSlot(effects.ActiveCount, default);
    }

    /// <summary>Проверяет, можно ли выучить заклинание (не выучено + есть свободный слот).</summary>
    public static bool CanLearnSpell(ref SpellSlots slots, int spellId)
    {
        if (slots.Slot0SpellId == spellId || slots.Slot1SpellId == spellId || slots.Slot2SpellId == spellId)
            return false;
        return slots.Slot0SpellId < 0 || slots.Slot1SpellId < 0 || slots.Slot2SpellId < 0;
    }

    /// <summary>Находит первый свободный слот и записывает spellId. Возвращает true если успешно.</summary>
    public static bool TryLearnSpell(ref SpellSlots slots, int spellId)
    {
        // Проверяем, не выучено ли уже
        if (slots.Slot0SpellId == spellId || slots.Slot1SpellId == spellId || slots.Slot2SpellId == spellId)
            return false;

        if (slots.Slot0SpellId < 0) { slots.Slot0SpellId = spellId; return true; }
        if (slots.Slot1SpellId < 0) { slots.Slot1SpellId = spellId; return true; }
        if (slots.Slot2SpellId < 0) { slots.Slot2SpellId = spellId; return true; }
        return false;
    }
}
