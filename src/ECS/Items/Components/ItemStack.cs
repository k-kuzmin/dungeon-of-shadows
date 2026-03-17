namespace DungeonOfShadows.ECS.Items;

public struct ItemStack
{
    public bool Occupied;
    public int DefinitionId;
    public ItemType Type;
    public ItemRarity Rarity;
    public int Quantity;

    public int BonusATK;
    public int BonusDEF;
    public int BonusHP;
    public float BonusCrit;

    public ItemEffectType EffectType;
    public int EffectPower;

    public static ItemStack Empty => default;
}
