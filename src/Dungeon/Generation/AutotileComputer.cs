namespace DungeonOfShadows.Dungeon.Generation;

/// <summary>
/// Вычисляет autotile-индексы для стен (8-bit blob → 47 уникальных тайлов),
/// варианты пола и индексы оверлеев. Запускается один раз при генерации этажа.
/// </summary>
internal static class AutotileComputer
{
    // Биты для 8 соседей (по часовой стрелке начиная с севера)
    private const byte N  = 1;
    private const byte NE = 2;
    private const byte E  = 4;
    private const byte SE = 8;
    private const byte S  = 16;
    private const byte SW = 32;
    private const byte W  = 64;
    private const byte NW = 128;

    /// <summary>
    /// Таблица 256 → 0-46. Углы, не поддержанные кардинальными соседями, обнуляются.
    /// </summary>
    private static readonly byte[] _blobLookup = BuildBlobLookup();

    /// <summary>
    /// Вычисляет AutotileIndex, FloorVariant и OverlayIndex для всех тайлов карты.
    /// </summary>
    internal static void ComputeAll(TileMap map, int floorVariantCount, Random rng)
    {
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                ref var tile = ref map.Tiles[x, y];

                switch (tile.Type)
                {
                    case TileType.Wall:
                        tile.AutotileIndex = ComputeWallIndex(map, x, y);
                        break;
                    case TileType.Floor:
                        tile.FloorVariant = (byte)rng.Next(floorVariantCount);
                        break;
                    case TileType.StairDown:
                        tile.FloorVariant = 0;
                        break;
                }

                // Оверлеи для трещин
                var deco = map.Decorations[x, y];
                if (deco == DecorationType.CrackFloor)
                    tile.OverlayIndex = (byte)(1 + rng.Next(12)); // 12 вариантов трещин пола
                else if (deco == DecorationType.CrackWall)
                    tile.OverlayIndex = (byte)(1 + rng.Next(10)); // 10 вариантов трещин стен
            }
        }
    }

    private static byte ComputeWallIndex(TileMap map, int x, int y)
    {
        byte mask = 0;

        bool n  = IsWall(map, x, y - 1);
        bool ne = IsWall(map, x + 1, y - 1);
        bool e  = IsWall(map, x + 1, y);
        bool se = IsWall(map, x + 1, y + 1);
        bool s  = IsWall(map, x, y + 1);
        bool sw = IsWall(map, x - 1, y + 1);
        bool w  = IsWall(map, x - 1, y);
        bool nw = IsWall(map, x - 1, y - 1);

        if (n) mask |= N;
        if (e) mask |= E;
        if (s) mask |= S;
        if (w) mask |= W;

        // Углы учитываются только если оба смежных кардинала — стены
        if (ne && n && e) mask |= NE;
        if (se && s && e) mask |= SE;
        if (sw && s && w) mask |= SW;
        if (nw && n && w) mask |= NW;

        return _blobLookup[mask];
    }

    private static bool IsWall(TileMap map, int x, int y)
    {
        if (!map.InBounds(x, y)) return true; // за пределами карты = стена
        return map.Tiles[x, y].Type == TileType.Wall;
    }

    /// <summary>
    /// Строит таблицу маппинга 256 масок → 47 blob-индексов.
    /// Маски с невалидными углами (угол без обоих кардиналов) приводятся к канонической форме.
    /// </summary>
    private static byte[] BuildBlobLookup()
    {
        // 47 канонических масок blob-тайлинга
        byte[] canonical = new byte[]
        {
            0,                                          // 0:  одиночная стена (всё Floor вокруг)
            N,                                          // 1:  только N
            E,                                          // 2:  только E
            N | E,                                      // 3:  N+E (без угла NE)
            N | NE | E,                                 // 4:  N+NE+E
            S,                                          // 5:  только S
            N | S,                                      // 6:  N+S (вертикаль)
            E | S,                                      // 7:  E+S (без SE)
            E | SE | S,                                 // 8:  E+SE+S
            N | E | S,                                  // 9:  N+E+S (без углов)
            N | NE | E | S,                             // 10: N+NE+E+S
            N | E | SE | S,                             // 11: N+E+SE+S
            N | NE | E | SE | S,                        // 12: N+NE+E+SE+S
            W,                                          // 13: только W
            N | W,                                      // 14: N+W (без NW)
            N | W | NW,                                 // 15: N+W+NW
            E | W,                                      // 16: E+W (горизонталь)
            N | E | W,                                  // 17: N+E+W (без углов)
            N | NE | E | W,                             // 18: N+NE+E+W
            N | E | W | NW,                             // 19: N+E+W+NW
            N | NE | E | W | NW,                        // 20: N+NE+E+W+NW
            S | W,                                      // 21: S+W (без SW)
            S | SW | W,                                 // 22: S+SW+W
            N | S | W,                                  // 23: N+S+W (без углов)
            N | S | SW | W,                             // 24: N+S+SW+W
            N | S | W | NW,                             // 25: N+S+W+NW
            N | S | SW | W | NW,                        // 26: N+S+SW+W+NW
            E | S | W,                                  // 27: E+S+W (без углов)
            E | SE | S | W,                             // 28: E+SE+S+W
            E | S | SW | W,                             // 29: E+S+SW+W
            E | SE | S | SW | W,                        // 30: E+SE+S+SW+W
            N | E | S | W,                              // 31: все кардиналы (без углов)
            N | NE | E | S | W,                         // 32: +NE
            N | E | SE | S | W,                         // 33: +SE
            N | E | S | SW | W,                         // 34: +SW
            N | E | S | W | NW,                         // 35: +NW
            N | NE | E | SE | S | W,                    // 36: +NE+SE
            N | NE | E | S | SW | W,                    // 37: +NE+SW
            N | NE | E | S | W | NW,                    // 38: +NE+NW
            N | E | SE | S | SW | W,                    // 39: +SE+SW
            N | E | SE | S | W | NW,                    // 40: +SE+NW
            N | E | S | SW | W | NW,                    // 41: +SW+NW
            N | NE | E | SE | S | SW | W,               // 42: +NE+SE+SW
            N | NE | E | SE | S | W | NW,               // 43: +NE+SE+NW
            N | NE | E | S | SW | W | NW,               // 44: +NE+SW+NW
            N | E | SE | S | SW | W | NW,               // 45: +SE+SW+NW
            N | NE | E | SE | S | SW | W | NW,          // 46: все (полностью окружена стенами)
        };

        var lookup = new byte[256];
        // Для каждой маски 0-255 нормализуем (убираем невалидные углы) и ищем индекс
        var canonicalMap = new Dictionary<byte, byte>(47);
        for (byte i = 0; i < canonical.Length; i++)
            canonicalMap[canonical[i]] = i;

        for (int raw = 0; raw < 256; raw++)
        {
            byte mask = NormalizeCorners((byte)raw);
            if (canonicalMap.TryGetValue(mask, out byte idx))
                lookup[raw] = idx;
            else
                lookup[raw] = 46; // fallback: полностью окружена
        }

        return lookup;
    }

    /// <summary>
    /// Убирает угловые биты, если смежные кардиналы не установлены.
    /// </summary>
    private static byte NormalizeCorners(byte mask)
    {
        if ((mask & NE) != 0 && ((mask & N) == 0 || (mask & E) == 0)) mask &= unchecked((byte)~NE);
        if ((mask & SE) != 0 && ((mask & S) == 0 || (mask & E) == 0)) mask &= unchecked((byte)~SE);
        if ((mask & SW) != 0 && ((mask & S) == 0 || (mask & W) == 0)) mask &= unchecked((byte)~SW);
        if ((mask & NW) != 0 && ((mask & N) == 0 || (mask & W) == 0)) mask &= unchecked((byte)~NW);
        return mask;
    }
}
