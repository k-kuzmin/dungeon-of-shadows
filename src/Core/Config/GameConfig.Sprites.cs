namespace DungeonOfShadows.Core;

public partial class GameConfig
{
    // Floor variants (из walls_floor.png rows 6-8)
    public int FloorVariantCount { get; init; } = 8;

    // Decoration object spawn chances (% на подходящий тайл пола)
    public int DecoObjectChancePercent { get; init; } = 4;

    // Torch sprite (32x48 centered on tile, overlaps wall above)
    public int TorchSpriteWidth { get; init; } = 32;
    public int TorchSpriteHeight { get; init; } = 48;

    // Chest sprite size
    public int ChestSpriteSize { get; init; } = 16;
}
