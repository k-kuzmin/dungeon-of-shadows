namespace DungeonOfShadows.ECS.Magic.Components;

public struct SpellSlot
{
    public int SpellId;  // -1 = пусто
    public int Level;    // 0 = пусто, 1..MaxLevel

    public bool IsEmpty => SpellId < 0;

    public static readonly SpellSlot Empty = new() { SpellId = -1, Level = 0 };
}
