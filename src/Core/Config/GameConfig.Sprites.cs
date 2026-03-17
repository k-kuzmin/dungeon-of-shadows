namespace DungeonOfShadows.Core;

public partial class GameConfig
{
    // Torch animation (6 кадров вертикально, 150ms per frame)
    public int TorchFrameCount { get; init; } = 6;
    public float TorchFrameDuration { get; init; } = 0.15f;

    // Chest open animation
    public int ChestOpenFrameCount { get; init; } = 5;
    public float ChestOpenFrameDuration { get; init; } = 0.1f;

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
