using System.Text.Json;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Combat;

namespace DungeonOfShadows.ECS.Items;

public sealed class ItemDatabase
{
    private readonly GameConfig _config;
    private readonly Dictionary<int, ItemDefinition> _defs = new();
    private readonly List<ItemDefinition> _all = new();

    public ItemDatabase(GameConfig config)
    {
        _config = config;
        Load();
        if (_all.Count == 0)
            LoadFallback();
    }

    public bool TryGetDefinition(int id, out ItemDefinition def) => _defs.TryGetValue(id, out def!);

    public IEnumerable<ItemDefinition> AllDefinitions => _all;

    public bool TryRollDrop(EnemyType enemyType, int floor, Random rng, out ItemStack item)
    {
        item = default;

        int sum = 0;
        for (int i = 0; i < _all.Count; i++)
        {
            var def = _all[i];
            if (def.MinFloor > floor) continue;
            sum += GetWeight(def, enemyType);
        }

        if (sum <= 0)
            return false;

        int roll = rng.Next(sum);
        for (int i = 0; i < _all.Count; i++)
        {
            var def = _all[i];
            if (def.MinFloor > floor) continue;

            int w = GetWeight(def, enemyType);
            if (w <= 0) continue;

            if (roll < w)
            {
                var rarity = RollRarity(rng);
                item = CreateInstance(def, rarity, rng, _config.ItemMaxStackSize);
                return true;
            }

            roll -= w;
        }

        return false;
    }

    public ItemStack CreateInstance(ItemDefinition def, ItemRarity rarity, Random rng, int maxStack)
    {
        var stack = new ItemStack
        {
            Occupied = true,
            DefinitionId = def.Id,
            Type = def.Type,
            Rarity = rarity,
            Quantity = 1,
            BonusATK = def.BaseATK,
            BonusDEF = def.BaseDEF,
            BonusHP = def.BaseHP,
            BonusCrit = def.BaseCrit,
            EffectType = def.EffectType,
            EffectPower = def.EffectPower
        };

        if (def.IsStackable)
            stack.Quantity = 1;

        int rolls = rarity switch
        {
            ItemRarity.Common => 0,
            ItemRarity.Uncommon => 1,
            ItemRarity.Rare => 2,
            ItemRarity.Epic => 3,
            ItemRarity.Legendary => 4,
            _ => 0
        };

        for (int i = 0; i < rolls; i++)
        {
            int statRoll = rng.Next(4);
            if (statRoll == 0) stack.BonusATK += 1;
            else if (statRoll == 1) stack.BonusDEF += 1;
            else if (statRoll == 2) stack.BonusHP += 2;
            else stack.BonusCrit += 0.01f;
        }

        if (rarity == ItemRarity.Rare) Scale(ref stack, _config.RarityScaleRare);
        if (rarity == ItemRarity.Epic) Scale(ref stack, _config.RarityScaleEpic);
        if (rarity == ItemRarity.Legendary)
        {
            Scale(ref stack, _config.RarityScaleLegendary);
            stack.EffectPower = (int)(stack.EffectPower * 1.2f);
        }

        if (def.IsStackable)
            stack.Quantity = Math.Clamp(stack.Quantity, 1, Math.Min(maxStack, Math.Max(1, def.MaxStack)));

        return stack;
    }

    private static void Scale(ref ItemStack stack, float multiplier)
    {
        stack.BonusATK = (int)MathF.Round(stack.BonusATK * multiplier);
        stack.BonusDEF = (int)MathF.Round(stack.BonusDEF * multiplier);
        stack.BonusHP = (int)MathF.Round(stack.BonusHP * multiplier);
        stack.BonusCrit *= multiplier;
    }

    private int GetWeight(ItemDefinition def, EnemyType enemyType)
    {
        return enemyType switch
        {
            EnemyType.Rat => def.DropWeightRat,
            EnemyType.Goblin => def.DropWeightGoblin,
            EnemyType.Skeleton => def.DropWeightSkeleton,
            _ => 0
        };
    }

    private ItemRarity RollRarity(Random rng)
    {
        int common = _config.RarityWeightCommon;
        int uncommon = _config.RarityWeightUncommon;
        int rare = _config.RarityWeightRare;
        int epic = _config.RarityWeightEpic;
        int legendary = _config.RarityWeightLegendary;

        int total = common + uncommon + rare + epic + legendary;
        int roll = rng.Next(total);

        if ((roll -= common) < 0) return ItemRarity.Common;
        if ((roll -= uncommon) < 0) return ItemRarity.Uncommon;
        if ((roll -= rare) < 0) return ItemRarity.Rare;
        if ((roll -= epic) < 0) return ItemRarity.Epic;
        return ItemRarity.Legendary;
    }

    private void Load()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "assets", "data", "items.json");
            if (!File.Exists(path))
                return;

            string json = File.ReadAllText(path);
            var defs = JsonSerializer.Deserialize(json, ItemsJsonContext.Default.ItemDefinitionArray);

            if (defs == null)
                return;

            for (int i = 0; i < defs.Length; i++)
                Add(defs[i]);
        }
        catch
        {
            // В случае ошибки загрузки остаёмся на fallback определениях.
        }
    }

    private void LoadFallback()
    {
        Add(new ItemDefinition { Id = 100, Name = "Iron Sword", Type = ItemType.Weapon, BaseATK = 3, DropWeightRat = 6, DropWeightGoblin = 15, DropWeightSkeleton = 14 });
        Add(new ItemDefinition { Id = 101, Name = "Leather Armor", Type = ItemType.Armor, BaseDEF = 2, BaseHP = 4, DropWeightRat = 4, DropWeightGoblin = 12, DropWeightSkeleton = 10 });
        Add(new ItemDefinition { Id = 102, Name = "Hunter Ring", Type = ItemType.Ring, BaseCrit = 0.03f, DropWeightRat = 8, DropWeightGoblin = 10, DropWeightSkeleton = 8 });
        Add(new ItemDefinition { Id = 103, Name = "Copper Amulet", Type = ItemType.Amulet, BaseHP = 8, DropWeightRat = 3, DropWeightGoblin = 7, DropWeightSkeleton = 9 });
        Add(new ItemDefinition { Id = 200, Name = "Health Potion", Type = ItemType.Potion, MaxStack = 10, EffectType = ItemEffectType.HealHp, EffectPower = 30, DropWeightRat = 20, DropWeightGoblin = 20, DropWeightSkeleton = 12 });
        Add(new ItemDefinition { Id = 300, Name = "Scroll of Nova", Type = ItemType.Scroll, MaxStack = 10, EffectType = ItemEffectType.NovaDamage, EffectPower = 18, MinFloor = 2, DropWeightRat = 2, DropWeightGoblin = 9, DropWeightSkeleton = 16 });
    }

    private void Add(ItemDefinition def)
    {
        if (_defs.ContainsKey(def.Id))
            return;

        _defs.Add(def.Id, def);
        _all.Add(def);
    }
}
