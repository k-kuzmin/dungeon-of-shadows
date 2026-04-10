namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Оркестратор анимаций: хранит набор именованных клипов и имя текущего.
/// Системы игровой логики меняют CurrentClip, AnimationSystem применяет переключение.
/// </summary>
public struct Animator
{
    /// <summary>Набор доступных клипов по имени (Idle, Walk, Attack и т.д.).</summary>
    public Dictionary<string, AnimationClip> Clips;

    /// <summary>Имя текущего активного клипа.</summary>
    public string CurrentClip;

    public Animator(Dictionary<string, AnimationClip> clips, string initialClip)
    {
        Clips = clips;
        CurrentClip = initialClip;
    }
}
