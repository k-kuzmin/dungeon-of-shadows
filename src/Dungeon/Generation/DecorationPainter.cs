namespace DungeonOfShadows.Dungeon.Generation;

internal static class DecorationPainter
{
    /// <summary>
    /// Заполняет слой декораций случайными пропсами на тайлах пола.
    /// </summary>
    internal static void Paint(TileMap map, int chancePercent, Random rng)
    {
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                if (map.Tiles[x, y].Type != TileType.Floor) continue;

                if (rng.Next(100) >= chancePercent) continue;

                // Факелы — только у стен
                bool nearWall = IsAdjacentToWall(map, x, y);

                int roll = rng.Next(100);
                if (nearWall && roll < 30)
                    map.Decorations[x, y] = DecorationType.Torch;
                else if (roll < 55)
                    map.Decorations[x, y] = DecorationType.CrackFloor;
                else if (roll < 80)
                    map.Decorations[x, y] = DecorationType.Bones;
                else
                    map.Decorations[x, y] = DecorationType.Puddle;
            }
        }
    }

    private static bool IsAdjacentToWall(TileMap map, int x, int y)
    {
        return (map.InBounds(x - 1, y) && map.Tiles[x - 1, y].Type == TileType.Wall) ||
               (map.InBounds(x + 1, y) && map.Tiles[x + 1, y].Type == TileType.Wall) ||
               (map.InBounds(x, y - 1) && map.Tiles[x, y - 1].Type == TileType.Wall) ||
               (map.InBounds(x, y + 1) && map.Tiles[x, y + 1].Type == TileType.Wall);
    }
}
