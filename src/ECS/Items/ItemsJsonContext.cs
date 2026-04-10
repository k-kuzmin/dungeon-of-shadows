using System.Text.Json.Serialization;

namespace DungeonOfShadows.ECS.Items;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ItemDefinition[]))]
public partial class ItemsJsonContext : JsonSerializerContext
{
}
