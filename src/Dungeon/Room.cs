namespace DungeonOfShadows.Dungeon;

public enum RoomType
{
    Normal,
    Spawn,
    StairDown
}

public readonly struct Room
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public RoomType Type { get; init; }

    public int CenterX => X + Width / 2;
    public int CenterY => Y + Height / 2;

    public bool Contains(int tx, int ty) =>
        tx >= X && tx < X + Width && ty >= Y && ty < Y + Height;
}
