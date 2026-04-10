using Raylib_cs;

namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Строит словарь AnimationClip из AnimatorDefinition.
/// Заменяет CharacterAnimationBuilder — единый фабричный метод для всех типов аниматоров.
/// </summary>
public static class AnimatorFactory
{
    /// <summary>
    /// Строит полный набор клипов для directional/multi аниматоров.
    /// Возвращает словарь и имя начального клипа.
    /// </summary>
    public static Dictionary<string, AnimationClip> Build(AnimatorDefinition def)
    {
        return def.Type switch
        {
            "directional" => BuildDirectional(def),
            _ => BuildNamedClips(def)
        };
    }

    /// <summary>
    /// Строит единственный клип (для simple/atlas типов — факелы, снаряды).
    /// </summary>
    public static AnimationClip BuildSingleClip(AnimatorDefinition def)
    {
        if (def.Clips.Length == 0)
            return new AnimationClip("", Array.Empty<Rectangle>(), 0.1f);

        var clip = def.Clips[0];
        return BuildClipFromAtlas(clip, def.FrameWidth, def.FrameHeight);
    }

    private static Dictionary<string, AnimationClip> BuildDirectional(AnimatorDefinition def)
    {
        var dirs = def.Directions ?? new[] { "Down", "Up", "Left", "Right" };
        int dirCount = dirs.Length;
        int capacity = def.Clips.Length * dirCount;
        var clips = new Dictionary<string, AnimationClip>(capacity);

        for (int c = 0; c < def.Clips.Length; c++)
        {
            var clipDef = def.Clips[c];
            for (int row = 0; row < dirCount; row++)
            {
                int frameCount = clipDef.FramesPerDirection != null && row < clipDef.FramesPerDirection.Length
                    ? clipDef.FramesPerDirection[row]
                    : clipDef.Frames;

                var frames = new Rectangle[frameCount];
                for (int f = 0; f < frameCount; f++)
                {
                    frames[f] = new Rectangle(
                        f * def.FrameWidth,
                        row * def.FrameHeight,
                        def.FrameWidth,
                        def.FrameHeight);
                }

                string key = $"{clipDef.Name}_{dirs[row]}";
                clips[key] = new AnimationClip(clipDef.Texture, frames, clipDef.FrameDuration, clipDef.Loop);
            }
        }

        return clips;
    }

    private static Dictionary<string, AnimationClip> BuildNamedClips(AnimatorDefinition def)
    {
        var clips = new Dictionary<string, AnimationClip>(def.Clips.Length);
        for (int i = 0; i < def.Clips.Length; i++)
        {
            var clipDef = def.Clips[i];
            clips[clipDef.Name] = BuildClipFromAtlas(clipDef, def.FrameWidth, def.FrameHeight);
        }
        return clips;
    }

    private static AnimationClip BuildClipFromAtlas(ClipDefinition clipDef, int frameW, int frameH)
    {
        int count = Math.Max(clipDef.Frames, 1);
        var frames = new Rectangle[count];
        for (int f = 0; f < count; f++)
        {
            frames[f] = new Rectangle(
                clipDef.StartX + f * clipDef.StrideX,
                clipDef.StartY + f * clipDef.StrideY,
                frameW,
                frameH);
        }
        return new AnimationClip(clipDef.Texture, frames, clipDef.FrameDuration, clipDef.Loop);
    }
}
