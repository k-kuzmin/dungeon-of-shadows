namespace DungeonOfShadows.Core;

public partial class GameConfig
{
    public int ScreenWidth { get; init; } = 1280;
    public int ScreenHeight { get; init; } = 720;
    public string Title { get; init; } = "Dungeon of Shadows";
    public int TargetFPS { get; init; } = 60;
}
