using System.Text.Json.Serialization;

namespace DungeonOfShadows.ECS.Magic;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(SpellDefinition[]))]
public partial class SpellsJsonContext : JsonSerializerContext
{
}
