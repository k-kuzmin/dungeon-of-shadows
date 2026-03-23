namespace DungeonOfShadows.ECS.Magic.Components;

public struct SpellSlots
{
    public int Slot0SpellId;
    public int Slot1SpellId;
    public int Slot2SpellId;
    public int ActiveSlotIndex;
    public float CastCooldown;

    public SpellSlots(bool init)
    {
        Slot0SpellId = -1;
        Slot1SpellId = -1;
        Slot2SpellId = -1;
        ActiveSlotIndex = 0;
        CastCooldown = 0f;
    }

    public int GetActiveSpellId()
    {
        return ActiveSlotIndex switch
        {
            0 => Slot0SpellId,
            1 => Slot1SpellId,
            2 => Slot2SpellId,
            _ => -1
        };
    }

    public void SetSlot(int index, int spellId)
    {
        switch (index)
        {
            case 0: Slot0SpellId = spellId; break;
            case 1: Slot1SpellId = spellId; break;
            case 2: Slot2SpellId = spellId; break;
        }
    }

    public int GetSlot(int index)
    {
        return index switch
        {
            0 => Slot0SpellId,
            1 => Slot1SpellId,
            2 => Slot2SpellId,
            _ => -1
        };
    }
}
