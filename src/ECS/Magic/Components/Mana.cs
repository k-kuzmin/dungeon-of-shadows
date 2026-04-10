namespace DungeonOfShadows.ECS.Magic.Components;

public struct Mana
{
    public int MP;
    public int MaxMP;
    public float RegenAccumulator;

    public Mana(int mp, int maxMp)
    {
        MP = mp;
        MaxMP = maxMp;
        RegenAccumulator = 0f;
    }
}
