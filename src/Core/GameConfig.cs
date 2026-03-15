namespace DungeonOfShadows.Core;

public class GameConfig
{
    public int ScreenWidth { get; init; } = 1280;
    public int ScreenHeight { get; init; } = 720;
    public string Title { get; init; } = "Dungeon of Shadows";
    public int TargetFPS { get; init; } = 60;

    // Rendering
    public int TileSize { get; init; } = 16;
    public int RenderScale { get; init; } = 3;
    public int ScaledTileSize => TileSize * RenderScale;

    // Player
    public float PlayerSpeed { get; init; } = 4.5f;

    // Camera
    public float CameraSmoothBase { get; init; } = 0.001f;
    public float ScreenShakeAmplitude { get; init; } = 3f;
    public float ScreenShakeDuration { get; init; } = 0.15f;
}
