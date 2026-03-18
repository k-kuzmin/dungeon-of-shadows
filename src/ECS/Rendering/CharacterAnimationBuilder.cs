using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Строит наборы AnimationClip для персонажей из спрайтшитов.
/// Каждый спрайтшит: N столбцов (кадры) × 4 строки (направления).
/// Ключи клипов: "{AnimName}_{Direction}", напр. "Idle_Down", "Run_Left".
/// </summary>
public static class CharacterAnimationBuilder
{
    private const int DirectionCount = 4;
    private const int ClipCapacity = 5 * DirectionCount; // макс. 5 анимаций × 4 направления

    // Порядок направлений по строкам спрайтшита
    // Орки: Down, Up, Left, Right
    private static readonly string[] DirectionsStandard = { "Down", "Up", "Left", "Right" };
    // Герой: Down, Left, Right, Up (подтверждено визуально)
    private static readonly string[] DirectionsHero = { "Down", "Left", "Right", "Up" };

    /// <summary>
    /// Создаёт полный набор клипов для героя.
    /// </summary>
    public static Dictionary<string, AnimationClip> BuildHero(GameConfig config)
    {
        int fs = config.CharacterFrameSize;
        var clips = new Dictionary<string, AnimationClip>(ClipCapacity);

        // Hero idle: 768×256, ряды 0-2 по 12 кадров, ряд 3 — 4 кадра
        AddAnimation(clips, "Idle", "hero_idle", fs, fs,
            12, config.CharacterIdleFrameDuration, true, DirectionsHero,
            framesPerDir: new[] { 12, 12, 12, 4 });

        AddAnimation(clips, "Run", "hero_run", fs, fs,
            8, config.CharacterRunFrameDuration, true, DirectionsHero);

        AddAnimation(clips, "Attack", "hero_attack", fs, fs,
            8, config.CharacterAttackFrameDuration, false, DirectionsHero);

        AddAnimation(clips, "RunAttack", "hero_run_attack", fs, fs,
            8, config.CharacterRunAttackFrameDuration, false, DirectionsHero);

        AddAnimation(clips, "Death", "hero_death", fs, fs,
            7, config.CharacterDeathFrameDuration, false, DirectionsHero);

        return clips;
    }

    /// <summary>
    /// Создаёт полный набор клипов для врага по префиксу текстуры (orc1, orc2, orc3).
    /// </summary>
    public static Dictionary<string, AnimationClip> BuildEnemy(string spritePrefix, GameConfig config)
    {
        int fs = config.CharacterFrameSize;
        var clips = new Dictionary<string, AnimationClip>(ClipCapacity);

        AddAnimation(clips, "Idle", $"{spritePrefix}_idle", fs, fs,
            4, config.CharacterIdleFrameDuration, true, DirectionsStandard);

        AddAnimation(clips, "Run", $"{spritePrefix}_run", fs, fs,
            8, config.CharacterRunFrameDuration, true, DirectionsStandard);

        AddAnimation(clips, "Attack", $"{spritePrefix}_attack", fs, fs,
            8, config.EnemyAttackFrameDuration, false, DirectionsStandard);

        AddAnimation(clips, "RunAttack", $"{spritePrefix}_run_attack", fs, fs,
            8, config.CharacterRunAttackFrameDuration, false, DirectionsStandard);

        AddAnimation(clips, "Death", $"{spritePrefix}_death", fs, fs,
            8, config.CharacterDeathFrameDuration, false, DirectionsStandard);

        return clips;
    }

    /// <summary>
    /// Добавляет 4 направленных клипа в словарь: {animName}_Down, _Up, _Left, _Right.
    /// </summary>
    private static void AddAnimation(
        Dictionary<string, AnimationClip> clips,
        string animName, string textureId,
        int frameW, int frameH,
        int defaultFrameCount, float frameDuration, bool loop,
        string[] directions,
        int[]? framesPerDir = null)
    {
        for (int row = 0; row < 4; row++)
        {
            int frameCount = framesPerDir != null ? framesPerDir[row] : defaultFrameCount;
            var frames = new Rectangle[frameCount];

            for (int f = 0; f < frameCount; f++)
            {
                frames[f] = new Rectangle(
                    f * frameW,
                    row * frameH,
                    frameW,
                    frameH);
            }

            // directions[row] — имя направления для этой строки спрайтшита
            clips[$"{animName}_{directions[row]}"] = new AnimationClip(
                textureId, frames, frameDuration, loop);
        }
    }
}
