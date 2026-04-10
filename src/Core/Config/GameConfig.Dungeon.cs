namespace DungeonOfShadows.Core;

public partial class GameConfig
{
    // Dungeon generation
    public int DungeonBaseWidth { get; init; } = 60;
    public int DungeonBaseHeight { get; init; } = 50;
    public int DungeonGrowthInterval { get; init; } = 5;
    public int DungeonGrowthAmount { get; init; } = 5;
    public int DungeonMinRooms { get; init; } = 8;
    public int DungeonMaxRooms { get; init; } = 14;
    public int DungeonMinRoomW { get; init; } = 5;
    public int DungeonMinRoomH { get; init; } = 6;
    public int DungeonMaxRoomW { get; init; } = 12;
    public int DungeonMaxRoomH { get; init; } = 10;
    public int DungeonBspMinLeaf { get; init; } = 16; // MaxRoomW + 4 (отступ 2 тайла для стен)
    public int DecorationChancePercent { get; init; } = 8;
    public int TorchSpacing { get; init; } = 4;

    // FOV
    public int FovRadius { get; init; } = 7;
    public int FovRayCount { get; init; } = 360;
}
