using System.Text.Json.Serialization;

namespace DungeonOfShadows.ECS.Rendering;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AnimatorDefinition[]))]
public partial class AnimatorsJsonContext : JsonSerializerContext
{
}
