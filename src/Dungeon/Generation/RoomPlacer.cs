namespace DungeonOfShadows.Dungeon.Generation;

internal static class RoomPlacer
{
    /// <summary>
    /// Размещает комнаты внутри листьев BSP-дерева.
    /// Возвращает словарь лист → комната для связывания коридоров.
    /// </summary>
    internal static Dictionary<BspNode, Room> PlaceRooms(
        List<BspNode> leaves,
        int minW, int minH, int maxW, int maxH,
        int minRooms, int maxRooms,
        Random rng)
    {
        var result = new Dictionary<BspNode, Room>();

        // Перемешиваем листья для случайного выбора
        Shuffle(leaves, rng);

        // Ограничиваем количество комнат
        int targetCount = Math.Min(leaves.Count, maxRooms);

        foreach (var leaf in leaves)
        {
            if (result.Count >= targetCount) break;

            // Отступ 1 тайл от границ листа для стен
            int availW = leaf.Width - 2;
            int availH = leaf.Height - 2;

            if (availW < minW || availH < minH) continue;

            int roomW = rng.Next(minW, Math.Min(maxW, availW) + 1);
            int roomH = rng.Next(minH, Math.Min(maxH, availH) + 1);

            // Случайная позиция внутри листа (с отступом)
            int roomX = leaf.X + 1 + rng.Next(availW - roomW + 1);
            int roomY = leaf.Y + 1 + rng.Next(availH - roomH + 1);

            result[leaf] = new Room
            {
                X = roomX,
                Y = roomY,
                Width = roomW,
                Height = roomH,
                Type = RoomType.Normal
            };
        }

        return result;
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
