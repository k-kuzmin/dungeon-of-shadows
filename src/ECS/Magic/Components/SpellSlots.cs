namespace DungeonOfShadows.ECS.Magic.Components;

public struct SpellSlots
{
    public SpellSlot[] Slots;
    public int Capacity;
    public int ActiveSlotIndex;
    public float CastCooldown;
    public float MaxCastCooldown;

    public SpellSlots(int capacity)
    {
        Capacity = capacity;
        Slots = new SpellSlot[capacity];
        for (int i = 0; i < capacity; i++)
            Slots[i] = SpellSlot.Empty;
        ActiveSlotIndex = 0;
        CastCooldown = 0f;
        MaxCastCooldown = 1f;
    }

    public int GetActiveSpellId()
    {
        if (ActiveSlotIndex < 0 || ActiveSlotIndex >= Capacity) return -1;
        return Slots[ActiveSlotIndex].SpellId;
    }

    public int GetActiveLevel()
    {
        if (ActiveSlotIndex < 0 || ActiveSlotIndex >= Capacity) return 0;
        return Slots[ActiveSlotIndex].Level;
    }

    public SpellSlot GetSlot(int index)
    {
        if (index < 0 || index >= Capacity) return SpellSlot.Empty;
        return Slots[index];
    }

    public void SetSlot(int index, int spellId, int level)
    {
        if (index < 0 || index >= Capacity) return;
        Slots[index].SpellId = spellId;
        Slots[index].Level = level;
    }

    public int FindSpellIndex(int spellId)
    {
        for (int i = 0; i < Capacity; i++)
            if (Slots[i].SpellId == spellId) return i;
        return -1;
    }

    public int TryFindFreeSlot()
    {
        for (int i = 0; i < Capacity; i++)
            if (Slots[i].IsEmpty) return i;
        return -1;
    }

    public void Expand(int newCapacity)
    {
        if (newCapacity <= Capacity) return;
        var newSlots = new SpellSlot[newCapacity];
        for (int i = 0; i < Capacity; i++)
            newSlots[i] = Slots[i];
        for (int i = Capacity; i < newCapacity; i++)
            newSlots[i] = SpellSlot.Empty;
        Slots = newSlots;
        Capacity = newCapacity;
    }
}
