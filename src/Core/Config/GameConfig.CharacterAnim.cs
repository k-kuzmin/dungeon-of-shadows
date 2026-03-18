namespace DungeonOfShadows.Core;

public partial class GameConfig
{
    // Размер кадра спрайтшита персонажа (в пикселях исходного арта)
    public int CharacterFrameSize { get; init; } = 64;

    // Смещение отрисовки спрайта по Y (в пикселях исходного арта, вверх > 0).
    // Корректирует положение спрайта относительно коллайдера.
    public int CharacterSpriteOffsetY { get; init; } = 20;

    // Длительности кадров анимаций (секунды)
    public float CharacterIdleFrameDuration { get; init; } = 0.15f;
    public float CharacterRunFrameDuration { get; init; } = 0.1f;
    public float CharacterAttackFrameDuration { get; init; } = 0.05f;
    public float EnemyAttackFrameDuration { get; init; } = 0.1f;
    public float CharacterRunAttackFrameDuration { get; init; } = 0.07f;
    public float CharacterDeathFrameDuration { get; init; } = 0.12f;
}
