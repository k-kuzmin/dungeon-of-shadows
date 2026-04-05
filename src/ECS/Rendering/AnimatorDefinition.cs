namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Десериализуемая модель одного аниматора из animations.json.
/// Типы: directional (персонажи), simple (факелы), atlas (сундуки), multi (общий).
/// </summary>
public sealed class AnimatorDefinition
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "simple"; // directional | simple | atlas | multi
    public string[]? Directions { get; set; } // напр. ["Down","Left","Right","Up"]
    public int FrameWidth { get; set; } = 64;
    public int FrameHeight { get; set; } = 64;
    public string InitialClip { get; set; } = "Idle_Down";
    public ClipDefinition[] Clips { get; set; } = Array.Empty<ClipDefinition>();
}

/// <summary>
/// Определение одного клипа внутри аниматора.
/// </summary>
public sealed class ClipDefinition
{
    public string Name { get; set; } = "";
    public string Texture { get; set; } = "";

    /// <summary>Кол-во кадров (единое для всех направлений). Игнорируется если задан FramesPerDirection.</summary>
    public int Frames { get; set; }

    /// <summary>Кол-во кадров по направлениям (для directional с неравным числом). null = uniform.</summary>
    public int[]? FramesPerDirection { get; set; }

    public float FrameDuration { get; set; } = 0.1f;
    public bool Loop { get; set; } = true;

    // Atlas layout (для simple/atlas типов)
    public int StartX { get; set; }
    public int StartY { get; set; }
    public int StrideX { get; set; }
    public int StrideY { get; set; }
}
