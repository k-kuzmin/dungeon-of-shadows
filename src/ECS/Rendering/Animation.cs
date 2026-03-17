namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Состояние проигрывания текущего анимационного клипа.
/// Обновляется AnimationSystem каждый кадр.
/// </summary>
public struct Animation
{
    /// <summary>Текущий клип (ссылка на неизменяемый AnimationClip).</summary>
    public AnimationClip Clip;

    /// <summary>Индекс текущего кадра.</summary>
    public int FrameIndex;

    /// <summary>Накопленное время с последней смены кадра.</summary>
    public float Timer;

    /// <summary>true если не-loop клип доиграл до последнего кадра.</summary>
    public bool Finished;

    public Animation(AnimationClip clip)
    {
        Clip = clip;
        FrameIndex = 0;
        Timer = 0f;
        Finished = false;
    }
}
