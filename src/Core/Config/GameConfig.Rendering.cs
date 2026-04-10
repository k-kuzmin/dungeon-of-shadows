namespace DungeonOfShadows.Core;

public partial class GameConfig
{
    // Rendering
    public int TileSize { get; init; } = 16;
    public int RenderScale { get; init; } = 3;
    public int ScaledTileSize => TileSize * RenderScale;

    // Minimap
    public int MinimapWidth { get; init; } = 200;
    public int MinimapMargin { get; init; } = 10;
    public float MinimapOpacity { get; init; } = 0.8f;

    // FOV fade
    public float FovFadeDuration { get; init; } = 0.2f;
    public float FovExploredBrightness { get; init; } = 100f / 255f;

    // Camera
    public float CameraSmoothBase { get; init; } = 0.001f;
    public float ScreenShakeAmplitude { get; init; } = 3f;
    public float ScreenShakeDuration { get; init; } = 0.15f;
}
