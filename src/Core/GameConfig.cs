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

    // Dungeon generation
    public int DungeonBaseWidth { get; init; } = 60;
    public int DungeonBaseHeight { get; init; } = 50;
    public int DungeonGrowthInterval { get; init; } = 5;
    public int DungeonGrowthAmount { get; init; } = 5;
    public int DungeonMinRooms { get; init; } = 8;
    public int DungeonMaxRooms { get; init; } = 14;
    public int DungeonMinRoomW { get; init; } = 5;
    public int DungeonMinRoomH { get; init; } = 4;
    public int DungeonMaxRoomW { get; init; } = 12;
    public int DungeonMaxRoomH { get; init; } = 10;
    public int DungeonBspMinLeaf { get; init; } = 14; // MaxRoomW + 2
    public int DecorationChancePercent { get; init; } = 8;

    // FOV
    public int FovRadius { get; init; } = 7;
    public int FovRayCount { get; init; } = 360;

    // Minimap
    public int MinimapWidth { get; init; } = 200;
    public int MinimapMargin { get; init; } = 10;
    public float MinimapOpacity { get; init; } = 0.8f;

    // Camera
    public float CameraSmoothBase { get; init; } = 0.001f;
    public float ScreenShakeAmplitude { get; init; } = 3f;
    public float ScreenShakeDuration { get; init; } = 0.15f;
}
