namespace DungeonOfShadows.ECS.Items;

public struct QuickSlots
{
    public int Slot1DefinitionId;
    public int Slot2DefinitionId;
    public int Slot3DefinitionId;
    public int Slot4DefinitionId;

    public float PotionCooldown;
    public float ScrollCooldown;

    public QuickSlots(bool init)
    {
        Slot1DefinitionId = -1;
        Slot2DefinitionId = -1;
        Slot3DefinitionId = -1;
        Slot4DefinitionId = -1;
        PotionCooldown = 0f;
        ScrollCooldown = 0f;
    }
}
