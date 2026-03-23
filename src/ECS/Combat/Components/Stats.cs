namespace DungeonOfShadows.ECS.Combat;

public struct Stats
{
    public int ATK;
    public int DEF;
    public float SPD;
    public float Crit;
    public int INT;

    public Stats(int atk, int def, float spd, float crit, int mInt = 0)
    {
        ATK = atk;
        DEF = def;
        SPD = spd;
        Crit = crit;
        INT = mInt;
    }
}
