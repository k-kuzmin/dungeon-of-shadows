namespace DungeonOfShadows.Dungeon.Generation;

internal static class DecorationPainter
{
    /// <summary>
    /// Заполняет слой декораций: факелы регулярно вдоль фасадов стен,
    /// случайные пропсы (трещины, кости, лужи) на остальных тайлах пола.
    /// </summary>
    internal static void Paint(TileMap map, int chancePercent, int torchSpacing, Random rng)
    {
        // Проход 1: факелы с регулярным интервалом вдоль фасадов стен (Floor где y-1 = Wall)
        PlaceTorches(map, torchSpacing, rng);

        // Проход 2: случайные декорации пола (без факелов)
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                if (map.Tiles[x, y].Type != TileType.Floor) continue;
                if (map.Decorations[x, y] != DecorationType.None) continue;

                if (rng.Next(100) >= chancePercent) continue;

                int roll = rng.Next(100);
                if (roll < 55)
                    map.Decorations[x, y] = DecorationType.CrackFloor;
                else if (roll < 80)
                    map.Decorations[x, y] = DecorationType.Bones;
                else
                    map.Decorations[x, y] = DecorationType.Puddle;
            }
        }
    }

    /// <summary>
    /// Сканирует горизонтальные прогоны wall-face тайлов и ставит факелы с интервалом.
    /// Wall-face = Floor-тайл, у которого сверху (y-1) стена.
    /// </summary>
    private static void PlaceTorches(TileMap map, int spacing, Random rng)
    {
        for (int y = 1; y < map.Height; y++)
        {
            int runStart = -1;

            for (int x = 0; x <= map.Width; x++)
            {
                bool isWallFace = x < map.Width
                    && map.Tiles[x, y].Type == TileType.Floor
                    && map.Tiles[x, y - 1].Type == TileType.Wall;

                if (isWallFace)
                {
                    if (runStart < 0) runStart = x;
                }
                else if (runStart >= 0)
                {
                    PlaceTorchesInRun(map, runStart, x, y, spacing, rng);
                    runStart = -1;
                }
            }
        }
    }

    private static void PlaceTorchesInRun(TileMap map, int startX, int endX, int y, int spacing, Random rng)
    {
        int runLen = endX - startX;
        if (runLen < 2) return;

        int offset = runLen >= spacing ? rng.Next(spacing) : rng.Next(runLen);
        for (int i = offset; i < runLen; i += spacing)
            map.Decorations[startX + i, y] = DecorationType.Torch;
    }
}
