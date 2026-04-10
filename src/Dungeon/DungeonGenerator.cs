using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon.Generation;

namespace DungeonOfShadows.Dungeon;

public static class DungeonGenerator
{
    /// <summary>
    /// Генерирует этаж подземелья по номеру этажа и сиду.
    /// </summary>
    private const int MaxRetries = 50;

    public static GenerationResult Generate(int floorNumber, GameConfig config, int seed)
    {
        // Вычисляем размер карты с учётом роста
        int growth = ((floorNumber - 1) / config.DungeonGrowthInterval) * config.DungeonGrowthAmount;
        int mapW = config.DungeonBaseWidth + growth;
        int mapH = config.DungeonBaseHeight + growth;

        // Ретраи при недостаточном количестве комнат
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var rng = new Random(seed ^ (floorNumber * 1_000_003) ^ (attempt * 7_919 + 1));
            var result = TryGenerate(floorNumber, config, mapW, mapH, rng);
            if (result != null) return result.Value;
        }

        throw new InvalidOperationException(
            $"Failed to generate floor {floorNumber} after {MaxRetries} attempts");
    }

    private static GenerationResult? TryGenerate(
        int floorNumber, GameConfig config, int mapW, int mapH, Random rng)
    {
        // 1. Создаём карту, заливаем стенами
        var map = new TileMap(mapW, mapH);
        FillWithWalls(map);

        // 2. Строим BSP-дерево
        var root = BspTree.Build(0, 0, mapW, mapH, config.DungeonBspMinLeaf, rng);

        // 3. Собираем листья
        var leaves = new List<BspNode>();
        BspTree.CollectLeaves(root, leaves);

        // 4. Размещаем комнаты
        var leafToRoom = RoomPlacer.PlaceRooms(
            leaves,
            config.DungeonMinRoomW, config.DungeonMinRoomH,
            config.DungeonMaxRoomW, config.DungeonMaxRoomH,
            config.DungeonMinRooms, config.DungeonMaxRooms,
            rng);

        if (leafToRoom.Count < config.DungeonMinRooms)
            return null; // ретрай

        // 5. Назначаем типы комнат
        var rooms = leafToRoom.Values.ToList();
        AssignRoomTypes(rooms, floorNumber, rng);

        // 6. Вырезаем комнаты в карте
        foreach (var room in rooms)
            TileMap.CarveRoom(map, room.X, room.Y, room.Width, room.Height);

        // 7. Собираем пары для коридоров и прокладываем
        var leavesWithRooms = new HashSet<BspNode>(leafToRoom.Keys);
        var siblingPairs = new List<(BspNode A, BspNode B)>();
        BspTree.CollectSiblingPairs(root, leavesWithRooms, siblingPairs);

        var connections = new List<(Room A, Room B)>();
        foreach (var (leafA, leafB) in siblingPairs)
        {
            connections.Add((leafToRoom[leafA], leafToRoom[leafB]));
        }
        CorridorCarver.CarveCorridors(map, connections, rng);

        // 7.5. Убираем однорядные перегородки (стена с полом по обе стороны)
        RemoveThinWalls(map);

        // 8. Ставим лестницу
        var stairRoom = rooms.First(r => r.Type == RoomType.StairDown);
        map.Tiles[stairRoom.CenterX, stairRoom.CenterY] = new Tile(TileType.StairDown);

        // 9. Декорации
        DecorationPainter.Paint(map, config.DecorationChancePercent, config.TorchSpacing, rng);

        // 10. Автотайлинг — baked индексы стен, пола, оверлеев
        AutotileComputer.ComputeAll(map, config.FloorVariantCount, rng);

        // 11. Сохраняем список комнат на карте
        map.Rooms = rooms;

        var spawnRoom = rooms.First(r => r.Type == RoomType.Spawn);
        return new GenerationResult(map, spawnRoom, stairRoom);
    }

    /// <summary>
    /// Назначает типы: первая = Spawn, последняя = StairDown, остальные Normal.
    /// Для босс-этажей (кратно 5) лестница в большей комнате.
    /// </summary>
    private static void AssignRoomTypes(List<Room> rooms, int floorNumber, Random rng)
    {
        // Выбираем спавн — первая комната (уже перемешаны в RoomPlacer)
        rooms[0] = rooms[0] with { Type = RoomType.Spawn };

        // Выбираем комнату для лестницы — максимально удалённая от спавна
        int spawnCX = rooms[0].CenterX;
        int spawnCY = rooms[0].CenterY;
        int farthestIdx = 1;
        int farthestDist = 0;
        for (int i = 1; i < rooms.Count; i++)
        {
            int dist = Math.Abs(rooms[i].CenterX - spawnCX) + Math.Abs(rooms[i].CenterY - spawnCY);
            if (dist > farthestDist)
            {
                farthestDist = dist;
                farthestIdx = i;
            }
        }

        rooms[farthestIdx] = rooms[farthestIdx] with { Type = RoomType.StairDown };
    }

    /// <summary>
    /// Убирает стены толщиной в 1 тайл (пол с обеих сторон по горизонтали или вертикали).
    /// Итеративно — удаление стены может открыть новые тонкие перегородки.
    /// </summary>
    private static void RemoveThinWalls(TileMap map)
    {
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int x = 1; x < map.Width - 1; x++)
            {
                for (int y = 1; y < map.Height - 1; y++)
                {
                    if (map.Tiles[x, y].Type != TileType.Wall) continue;

                    bool floorN = map.Tiles[x, y - 1].Type != TileType.Wall;
                    bool floorS = map.Tiles[x, y + 1].Type != TileType.Wall;
                    bool floorE = map.Tiles[x + 1, y].Type != TileType.Wall;
                    bool floorW = map.Tiles[x - 1, y].Type != TileType.Wall;

                    // Стена с полом по обе стороны (N+S или E+W) — перегородка
                    if ((floorN && floorS) || (floorE && floorW))
                    {
                        map.Tiles[x, y] = new Tile(TileType.Floor);
                        changed = true;
                    }
                }
            }
        }
    }

    private static void FillWithWalls(TileMap map)
    {
        for (int x = 0; x < map.Width; x++)
            for (int y = 0; y < map.Height; y++)
                map.Tiles[x, y] = new Tile(TileType.Wall);
    }

    /// <summary>
    /// Результат генерации этажа.
    /// </summary>
    public readonly record struct GenerationResult(TileMap Map, Room SpawnRoom, Room StairRoom);
}
