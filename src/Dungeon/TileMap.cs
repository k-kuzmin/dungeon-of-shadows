namespace DungeonOfShadows.Dungeon;

public enum TileType
{
    Empty,
    Floor,
    Wall,
    StairDown
}

public enum DecorationType
{
    None,
    Torch,
    CrackFloor,
    CrackWall,
    Bones,
    Puddle
}

public struct Tile
{
    public TileType Type;
    public bool Walkable;
    /// <summary>0 = Unknown, 1 = Explored, 2 = Visible (для FOV)</summary>
    public byte Visibility;
    /// <summary>Индекс blob autotile (0-46) для стен. Вычисляется при генерации.</summary>
    public byte AutotileIndex;
    /// <summary>Вариант пола (0-N). Вычисляется при генерации.</summary>
    public byte FloorVariant;
    /// <summary>Индекс трещины-оверлея (0 = нет). Вычисляется при генерации.</summary>
    public byte OverlayIndex;
    /// <summary>Текущая яркость для плавного FOV-перехода. 0 = чёрный, ~0.39 = explored, 1.0 = видимый.</summary>
    public float FovBrightness;

    public Tile(TileType type)
    {
        Type = type;
        Walkable = type is TileType.Floor or TileType.StairDown;
        Visibility = 0;
        AutotileIndex = 0;
        FloorVariant = 0;
        OverlayIndex = 0;
        FovBrightness = 0f;
    }
}

public class TileMap
{
    public int Width { get; }
    public int Height { get; }
    public Tile[,] Tiles { get; }
    public DecorationType[,] Decorations { get; }
    public IReadOnlyList<Room> Rooms { get; internal set; } = Array.Empty<Room>();

    public TileMap(int width, int height)
    {
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];
        Decorations = new DecorationType[width, height];
    }

    public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public bool IsWalkable(int x, int y) => InBounds(x, y) && Tiles[x, y].Walkable;

    public bool IsWalkableWorld(float worldX, float worldY, int scaledTileSize)
    {
        int tx = (int)(worldX / scaledTileSize);
        int ty = (int)(worldY / scaledTileSize);
        return IsWalkable(tx, ty);
    }

    /// <summary>
    /// Generates a test map with 6 rooms connected by corridors.
    /// </summary>
    public static TileMap CreateTestMap()
    {
        const int w = 40, h = 30;
        var map = new TileMap(w, h);

        // Fill with walls
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                map.Tiles[x, y] = new Tile(TileType.Wall);

        // Room 1: spawn room (top-left)
        CarveRoom(map, 1, 1, 8, 6);

        // Room 2: armory (top-center)
        CarveRoom(map, 14, 1, 7, 5);

        // Room 3: library (top-right)
        CarveRoom(map, 26, 1, 12, 7);

        // Room 4: crypt (bottom-left)
        CarveRoom(map, 1, 14, 10, 8);

        // Room 5: great hall (bottom-center)
        CarveRoom(map, 15, 12, 12, 10);

        // Room 6: throne room (bottom-right)
        CarveRoom(map, 30, 12, 8, 10);

        // Corridors
        // Room 1 → Room 2 (horizontal)
        CarveHCorridor(map, 9, 13, 3);

        // Room 2 → Room 3 (horizontal)
        CarveHCorridor(map, 21, 25, 3);

        // Room 1 → Room 4 (vertical)
        CarveVCorridor(map, 4, 7, 13);

        // Room 2 → Room 5 (vertical)
        CarveVCorridor(map, 17, 6, 11);

        // Room 3 → Room 6 (vertical)
        CarveVCorridor(map, 31, 8, 11);

        // Room 4 → Room 5 (horizontal)
        CarveHCorridor(map, 11, 14, 17);

        // Room 5 → Room 6 (horizontal)
        CarveHCorridor(map, 27, 29, 17);

        return map;
    }

    internal static void CarveRoom(TileMap map, int x, int y, int w, int h)
    {
        for (int ix = x; ix < x + w && ix < map.Width; ix++)
            for (int iy = y; iy < y + h && iy < map.Height; iy++)
                map.Tiles[ix, iy] = new Tile(TileType.Floor);
    }

    internal static void CarveHCorridor(TileMap map, int x1, int x2, int y)
    {
        for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
        {
            for (int dy = -1; dy <= 1; dy++)
                if (map.InBounds(x, y + dy)) map.Tiles[x, y + dy] = new Tile(TileType.Floor);
        }
    }

    internal static void CarveVCorridor(TileMap map, int x, int y1, int y2)
    {
        for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
        {
            for (int dx = -1; dx <= 1; dx++)
                if (map.InBounds(x + dx, y)) map.Tiles[x + dx, y] = new Tile(TileType.Floor);
        }
    }
}
