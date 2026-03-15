namespace DungeonOfShadows.Dungeon;

public enum TileType
{
    Empty,
    Floor,
    Wall
}

public struct Tile
{
    public TileType Type;
    public bool Walkable;

    public Tile(TileType type)
    {
        Type = type;
        Walkable = type == TileType.Floor;
    }
}

public class TileMap
{
    public int Width { get; }
    public int Height { get; }
    public Tile[,] Tiles { get; }

    public TileMap(int width, int height)
    {
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];
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

    private static void CarveRoom(TileMap map, int x, int y, int w, int h)
    {
        for (int ix = x; ix < x + w && ix < map.Width; ix++)
            for (int iy = y; iy < y + h && iy < map.Height; iy++)
                map.Tiles[ix, iy] = new Tile(TileType.Floor);
    }

    private static void CarveHCorridor(TileMap map, int x1, int x2, int y)
    {
        for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
        {
            if (map.InBounds(x, y)) map.Tiles[x, y] = new Tile(TileType.Floor);
            if (map.InBounds(x, y + 1)) map.Tiles[x, y + 1] = new Tile(TileType.Floor);
        }
    }

    private static void CarveVCorridor(TileMap map, int x, int y1, int y2)
    {
        for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
        {
            if (map.InBounds(x, y)) map.Tiles[x, y] = new Tile(TileType.Floor);
            if (map.InBounds(x + 1, y)) map.Tiles[x + 1, y] = new Tile(TileType.Floor);
        }
    }
}
