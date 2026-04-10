namespace DungeonOfShadows.ECS.Items;

public struct Inventory
{
    public int Capacity;
    public ItemStack[] Slots;

    public Inventory(int capacity)
    {
        Capacity = capacity;
        Slots = new ItemStack[capacity];
    }
}
