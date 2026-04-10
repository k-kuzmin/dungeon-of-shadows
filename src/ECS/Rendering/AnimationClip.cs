using Raylib_cs;

namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Определение одного анимационного клипа: кадры, длительность, зацикленность.
/// Неизменяемый — может быть переиспользован несколькими сущностями.
/// </summary>
public sealed class AnimationClip
{
    /// <summary>Ключ текстуры в IAssetProvider.</summary>
    public readonly string TextureId;

    /// <summary>Исходные прямоугольники кадров в атласе (в пикселях атласа).</summary>
    public readonly Rectangle[] Frames;

    /// <summary>Длительность одного кадра в секундах.</summary>
    public readonly float FrameDuration;

    /// <summary>Зацикленный ли клип.</summary>
    public readonly bool Loop;

    public AnimationClip(string textureId, Rectangle[] frames, float frameDuration, bool loop = true)
    {
        TextureId = textureId;
        Frames = frames;
        FrameDuration = frameDuration;
        Loop = loop;
    }

    public int FrameCount => Frames.Length;
}
