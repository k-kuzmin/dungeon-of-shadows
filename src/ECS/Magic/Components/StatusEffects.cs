namespace DungeonOfShadows.ECS.Magic.Components;

public struct StatusEffects
{
    public StatusEffectSlot Effect0;
    public StatusEffectSlot Effect1;
    public StatusEffectSlot Effect2;
    public StatusEffectSlot Effect3;
    public int ActiveCount;

    public StatusEffectSlot GetSlot(int index)
    {
        return index switch
        {
            0 => Effect0,
            1 => Effect1,
            2 => Effect2,
            _ => Effect3
        };
    }

    public void SetSlot(int index, StatusEffectSlot value)
    {
        switch (index)
        {
            case 0: Effect0 = value; break;
            case 1: Effect1 = value; break;
            case 2: Effect2 = value; break;
            default: Effect3 = value; break;
        }
    }
}
