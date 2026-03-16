namespace DungeonOfShadows.ECS.Combat;

public struct Health
{
    public int HP;
    public int MaxHP;

    public Health(int hp, int maxHp) { HP = hp; MaxHP = maxHp; }
}
