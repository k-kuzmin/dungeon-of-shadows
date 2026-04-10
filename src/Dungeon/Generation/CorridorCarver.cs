namespace DungeonOfShadows.Dungeon.Generation;

internal static class CorridorCarver
{
    /// <summary>
    /// Прорезает L-образные коридоры между центрами комнат.
    /// Использует существующие хелперы TileMap.
    /// </summary>
    internal static void CarveCorridors(
        TileMap map,
        List<(Room A, Room B)> connections,
        Random rng)
    {
        foreach (var (a, b) in connections)
        {
            int ax = a.CenterX, ay = a.CenterY;
            int bx = b.CenterX, by = b.CenterY;

            // Случайный порядок: сначала по X потом по Y, или наоборот
            if (rng.Next(2) == 0)
            {
                // Горизонталь, потом вертикаль
                TileMap.CarveHCorridor(map, ax, bx, ay);
                TileMap.CarveVCorridor(map, bx, ay, by);
            }
            else
            {
                // Вертикаль, потом горизонталь
                TileMap.CarveVCorridor(map, ax, ay, by);
                TileMap.CarveHCorridor(map, ax, bx, by);
            }
        }
    }
}
