namespace DungeonOfShadows.Core;

public partial class GameConfig
{
    // Смещение отрисовки спрайта по Y (в пикселях исходного арта, вверх > 0).
    // Корректирует положение спрайта относительно коллайдера.
    public int CharacterSpriteOffsetY { get; init; } = 20;
}
