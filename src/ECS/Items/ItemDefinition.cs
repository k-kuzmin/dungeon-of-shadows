using System.Text.Json.Serialization;

namespace DungeonOfShadows.ECS.Items;

public sealed class ItemDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ItemType Type { get; set; }
    public int MinFloor { get; set; } = 1;
    public int MaxStack { get; set; } = 1;

    public int BaseATK { get; set; }
    public int BaseDEF { get; set; }
    public int BaseHP { get; set; }
    public float BaseCrit { get; set; }

    public ItemEffectType EffectType { get; set; } = ItemEffectType.None;
    public int EffectPower { get; set; }

    /// <summary>ID заклинания для SpellScroll (EffectType == TeachSpell).</summary>
    public int SpellId { get; set; }

    public int DropWeightRat { get; set; }
    public int DropWeightGoblin { get; set; }
    public int DropWeightSkeleton { get; set; }

    [JsonIgnore]
    public bool IsStackable => Type is ItemType.Potion or ItemType.Scroll or ItemType.SpellScroll;
}
