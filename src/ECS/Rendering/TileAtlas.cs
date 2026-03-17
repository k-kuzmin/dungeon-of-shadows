using System.Numerics;
using Raylib_cs;

namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Маппинг blob autotile индексов и прочих тайлов на координаты в спрайтшитах.
/// walls_floor.png: 17 cols x 29 rows, 16x16 grid.
/// Все методы возвращают Rectangle в пиксельных координатах спрайтшита (не масштабированные).
/// </summary>
public static class TileAtlas
{
    private const int T = 16; // размер тайла в спрайтшите

    // ── walls_floor.png: blob autotile для стен ──
    // Раскладка стен в этом тайлсете многослойная:
    //   Row 0: top edge (крышка стены)
    //   Row 1: top caps (верхние края)
    //   Row 2: wall face (основной фасад) — col 2 = base fill
    //   Row 3: wall bottom (нижний край)
    //   Row 4: wall base surface (горизонтальная поверхность)
    //   Row 5: wall lower face
    //
    // Для blob autotile маппим 47 blob-индексов на (col, row) пары.
    // Бит-маска: N=1, NE=2, E=4, SE=8, S=16, SW=32, W=64, NW=128
    //
    // Blob index → (col, row) в walls_floor.png:
    // Маппинг построен по анализу TMX + визуальному расположению тайлов.

    private static readonly (int Col, int Row)[] _wallCoords = BuildWallCoords();

    // ── Пол ──
    // Row 8, col 2 — базовый пол. Используем один и тот же тайл для всех вариантов,
    // визуальное разнообразие обеспечивается оверлеями трещин и декор-объектами.
    private static readonly (int Col, int Row)[] _floorCoords =
    {
        (2, 8),   // 0: базовый пол
        (2, 8),   // 1: базовый пол (дубликат для рандомизации)
        (2, 8),   // 2: базовый пол
        (2, 8),   // 3: базовый пол
        (2, 8),   // 4: базовый пол
        (7, 13),  // 5: декор камешки (редкий)
        (8, 13),  // 6: декор камешки вариант (редкий)
        (2, 8),   // 7: базовый пол
    };

    // ── Тёмный пол (у стен) ──
    // Row 14 col 12 = база, edges вокруг
    private static readonly (int Col, int Row)[] _darkFloorCoords =
    {
        (12, 14), // 0: база
        (11, 14), // 1: left edge
        (13, 14), // 2: right edge
        (12, 13), // 3: top edge
        (12, 15), // 4: bottom edge
        (10, 13), // 5: top-left corner
        (14, 13), // 6: top-right corner
        (10, 14), // 7: left inner corner
        (14, 14), // 8: right inner corner
    };

    // ── Стена-верхушка (wall top surface, вид сверху) ──
    // Rows 9-10 — поверхность стены при взгляде сверху
    private static readonly (int Col, int Row)[] _wallTopCoords =
    {
        (2, 9),   // center
        (1, 9),   // left
        (3, 9),   // right
        (2, 10),  // center row 2
        (1, 10),  // left row 2
        (3, 10),  // right row 2
    };

    // ── Лестница ──
    // Objects.png col 0, rows 0-1 (каменная лестница 16x32)
    private static readonly Rectangle _stairSrc = new(0, 0, T, T * 2);

    // ── Трещины пола: decorative_cracks_floor.png (8 cols x 15 rows) ──
    // OverlayIndex 1-12 → координаты в спрайтшите
    private static readonly (int Col, int Row)[] _crackFloorCoords =
    {
        (0, 0), (1, 0), (2, 0), (3, 0),
        (4, 0), (5, 0), (6, 0), (7, 0),
        (0, 1), (1, 1), (2, 1), (3, 1),
    };

    // ── Трещины стен: decorative_cracks_walls.png (8 cols x 32 rows) ──
    private static readonly (int Col, int Row)[] _crackWallCoords =
    {
        (0, 0), (1, 0), (2, 0), (3, 0),
        (4, 0), (5, 0), (6, 0), (7, 0),
        (0, 1), (1, 1),
    };

    // ── Факелы: fire_animation.png (11 cols x 18 rows, 16x16 grid) ──
    // Большой факел: 2x3 тайла (32x48 px).
    // Анимация идёт ВЕРТИКАЛЬНО: каждый кадр через 3 строки (48 px).
    // Кадры для первого варианта (cols 0-1): y = 0, 48, 96, 144, 192, 240
    // 6 кадров анимации, 150ms на кадр.
    private static readonly Rectangle[] _torchFrames =
    {
        new(0, 0,   32, 48),   // кадр 0
        new(0, 48,  32, 48),   // кадр 1
        new(0, 96,  32, 48),   // кадр 2
        new(0, 144, 32, 48),   // кадр 3
        new(0, 192, 32, 48),   // кадр 4
        new(0, 240, 32, 48),   // кадр 5
    };

    // ── Сундуки: doors_lever_chest_animation.png (10 cols x 15 rows) ──
    // Сундуки начинаются примерно с row 8 (y=128).
    // Простой сундук: 5 кадров по 16x16 начиная с (0, 8*16)
    private static readonly Rectangle[] _chestClosedFrames =
    {
        new(0, 128, T, T),
    };
    private static readonly Rectangle[] _chestOpenFrames =
    {
        new(0, 128, T, T),   // закрытый
        new(T, 128, T, T),   // приоткрытый
        new(T*2, 128, T, T), // полуоткрытый
        new(T*3, 128, T, T), // почти открытый
        new(T*4, 128, T, T), // полностью открытый
    };

    // ── Objects.png: декоративные объекты (24 cols x 9 rows) ──
    // Бочки: col 4-5, row 0-1 (16x16 и 16x32)
    // Камни: col 8-10, row 0 (разных размеров)
    // Ящики: col 14-16, row 0-1
    // Мешки: col 19-20, row 0-1

    // ═══════════════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════════════

    /// <summary>Возвращает source rect для стены по blob autotile индексу (0-46).</summary>
    public static Rectangle GetWallSource(byte autotileIndex)
    {
        var (col, row) = _wallCoords[Math.Min(autotileIndex, (byte)46)];
        return new Rectangle(col * T, row * T, T, T);
    }

    /// <summary>Возвращает source rect для верхней поверхности стены (wall-top cap).</summary>
    public static Rectangle GetWallTopSource(int variant)
    {
        var (col, row) = _wallTopCoords[Math.Clamp(variant, 0, _wallTopCoords.Length - 1)];
        return new Rectangle(col * T, row * T, T, T);
    }

    /// <summary>Возвращает source rect для пола по варианту.</summary>
    public static Rectangle GetFloorSource(byte floorVariant)
    {
        int idx = Math.Min(floorVariant, _floorCoords.Length - 1);
        var (col, row) = _floorCoords[idx];
        return new Rectangle(col * T, row * T, T, T);
    }

    /// <summary>Возвращает source rect для тёмного пола (у стен).</summary>
    public static Rectangle GetDarkFloorSource(int variant)
    {
        int idx = Math.Clamp(variant, 0, _darkFloorCoords.Length - 1);
        var (col, row) = _darkFloorCoords[idx];
        return new Rectangle(col * T, row * T, T, T);
    }

    /// <summary>Возвращает source rect для лестницы в Objects.png (16x32).</summary>
    public static Rectangle GetStairSource() => _stairSrc;

    /// <summary>Возвращает source rect для трещины-оверлея.</summary>
    public static Rectangle GetCrackSource(byte overlayIndex, bool isWall)
    {
        if (overlayIndex == 0) return default;
        int idx = overlayIndex - 1;
        if (isWall)
        {
            idx = Math.Min(idx, _crackWallCoords.Length - 1);
            var (col, row) = _crackWallCoords[idx];
            return new Rectangle(col * T, row * T, T, T);
        }
        else
        {
            idx = Math.Min(idx, _crackFloorCoords.Length - 1);
            var (col, row) = _crackFloorCoords[idx];
            return new Rectangle(col * T, row * T, T, T);
        }
    }

    /// <summary>Возвращает source rect для кадра анимации факела (32x48).</summary>
    public static Rectangle GetTorchSource(byte frame)
    {
        return _torchFrames[Math.Min(frame, _torchFrames.Length - 1)];
    }

    /// <summary>Возвращает source rect для кадра сундука.</summary>
    public static Rectangle GetChestSource(byte frame, bool opened)
    {
        if (!opened)
            return _chestClosedFrames[0];
        return _chestOpenFrames[Math.Min(frame, _chestOpenFrames.Length - 1)];
    }

    /// <summary>Возвращает source rect для декоративного объекта из Objects.png.</summary>
    public static Rectangle GetDecoObjectSource(int atlasCol, int atlasRow, int srcWidth, int srcHeight)
    {
        return new Rectangle(atlasCol * T, atlasRow * T, srcWidth, srcHeight);
    }

    // ═══════════════════════════════════════════════════
    //  WALL BLOB MAPPING (47 indices → spritesheet coords)
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Строит маппинг 47 blob-индексов → (col, row) в walls_floor.png.
    ///
    /// Стены в этом тайлсете:
    ///   Rows 0-1: Верхние края и углы стен (wall caps)
    ///   Row 2: Фасад стены (solid fill + edges)
    ///   Row 3: Нижний край стены
    ///   Row 4: Горизонтальная поверхность стены
    ///   Row 5: Нижний фасад стены
    ///
    /// Blob bitmasking: N=bit0, NE=bit1, E=bit2, SE=bit3, S=bit4, SW=bit5, W=bit6, NW=bit7
    /// Index 0 = isolated (no neighbors), Index 46 = fully surrounded.
    /// </summary>
    private static (int Col, int Row)[] BuildWallCoords()
    {
        var coords = new (int Col, int Row)[47];

        // Маппинг основан на анализе TMX (Dungeon1.tmx) и визуальном расположении тайлов.
        // Каждый blob-индекс описывает какие соседние тайлы являются стенами.
        //
        // Ключевые тайлы:
        //   (2,2) = solid wall fill (все 4 кардинала = стена)
        //   (2,1) = wall top edge cap (S сосед = пол)
        //   (2,3) = wall bottom edge (N сосед = пол)
        //   (1,2) = wall left edge
        //   (3,2) = wall right edge
        //   (2,4) = wall horizontal surface

        // 0:  isolated (нет соседей-стен) — одиночный столб
        coords[0] = (6, 1);

        // 1:  N only — стена снизу (пол с трёх сторон)
        coords[1] = (2, 3);

        // 2:  E only — стена слева
        coords[2] = (1, 2);

        // 3:  N+E (no NE corner) — нижний-левый внешний угол
        coords[3] = (1, 3);

        // 4:  N+NE+E — нижний-левый с внутренним углом
        coords[4] = (1, 3);

        // 5:  S only — стена сверху
        coords[5] = (2, 1);

        // 6:  N+S — вертикальная стена (пол слева и справа)
        coords[6] = (2, 4);

        // 7:  E+S (no SE) — верхний-левый внешний угол
        coords[7] = (1, 1);

        // 8:  E+SE+S — верхний-левый с внутренним углом
        coords[8] = (1, 1);

        // 9:  N+E+S (no corners) — левая стена (пол слева)
        coords[9] = (1, 2);

        // 10: N+NE+E+S — левая стена с верхним углом
        coords[10] = (1, 2);

        // 11: N+E+SE+S — левая стена с нижним углом
        coords[11] = (1, 2);

        // 12: N+NE+E+SE+S — левая стена с обоими углами
        coords[12] = (1, 2);

        // 13: W only — стена справа
        coords[13] = (3, 2);

        // 14: N+W (no NW) — нижний-правый внешний угол
        coords[14] = (3, 3);

        // 15: N+W+NW — нижний-правый с внутренним углом
        coords[15] = (3, 3);

        // 16: E+W — горизонтальная стена
        coords[16] = (2, 4);

        // 17: N+E+W (no corners) — стена T-образная сверху, пол снизу
        coords[17] = (2, 3);

        // 18: N+NE+E+W — T с углом
        coords[18] = (2, 3);

        // 19: N+E+W+NW — T с углом NW
        coords[19] = (2, 3);

        // 20: N+NE+E+W+NW — T с обоими углами
        coords[20] = (2, 3);

        // 21: S+W (no SW) — верхний-правый внешний угол
        coords[21] = (3, 1);

        // 22: S+SW+W — верхний-правый с внутренним углом
        coords[22] = (3, 1);

        // 23: N+S+W (no corners) — правая стена (пол справа)
        coords[23] = (3, 2);

        // 24: N+S+SW+W — правая стена с нижним углом
        coords[24] = (3, 2);

        // 25: N+S+W+NW — правая стена с верхним углом
        coords[25] = (3, 2);

        // 26: N+S+SW+W+NW — правая стена с обоими углами
        coords[26] = (3, 2);

        // 27: E+S+W (no corners) — стена T-образная снизу, пол сверху
        coords[27] = (2, 1);

        // 28: E+SE+S+W — T с углом SE
        coords[28] = (2, 1);

        // 29: E+S+SW+W — T с углом SW
        coords[29] = (2, 1);

        // 30: E+SE+S+SW+W — T с обоими углами
        coords[30] = (2, 1);

        // Все 4 кардинала = стена. Разница — какие углы заполнены.
        // Внутренние углы (выемки) показываются через тайлы из cols 9-15, rows 0-3.
        // Для данного тайлсета внутренние углы:
        //   (9, 1) = inner corner NE (выемка в правом верхнем углу)
        //   (7, 1) = inner corner NW (выемка в левом верхнем углу)
        //   (9, 2) = inner corner SE (выемка в правом нижнем углу)
        //   (7, 0) = inner corner SW (выемка в левом нижнем углу)
        //
        // "Missing corner X" означает что X-сосед = пол → рисуем выемку в этом углу.
        // Если не хватает нескольких углов — используем solid fill (этот тайлсет не имеет
        // комбинированных inner corner тайлов).

        // 31: all cardinal, NO corners — 4 выемки. Используем solid (нет такого тайла).
        coords[31] = (2, 2);

        // 32: +NE (NE заполнен, SE/SW/NW пустые) — 3 выемки → solid fill
        coords[32] = (2, 2);

        // 33: +SE — 3 выемки
        coords[33] = (2, 2);

        // 34: +SW — 3 выемки
        coords[34] = (2, 2);

        // 35: +NW — 3 выемки
        coords[35] = (2, 2);

        // 36: +NE+SE — NW и SW пустые → solid fill (2 выемки, нет тайла)
        coords[36] = (2, 2);

        // 37: +NE+SW — NW и SE пустые
        coords[37] = (2, 2);

        // 38: +NE+NW — SE и SW пустые
        coords[38] = (2, 2);

        // 39: +SE+SW — NE и NW пустые
        coords[39] = (2, 2);

        // 40: +SE+NW — NE и SW пустые
        coords[40] = (2, 2);

        // 41: +SW+NW — NE и SE пустые
        coords[41] = (2, 2);

        // 42: +NE+SE+SW — только NW пустой → выемка NW
        coords[42] = (7, 1);

        // 43: +NE+SE+NW — только SW пустой → выемка SW
        coords[43] = (7, 0);

        // 44: +NE+SW+NW — только SE пустой → выемка SE
        coords[44] = (9, 2);

        // 45: +SE+SW+NW — только NE пустой → выемка NE
        coords[45] = (9, 1);

        // 46: fully surrounded (all 8 neighbors = wall) — solid fill
        coords[46] = (2, 2);

        return coords;
    }
}
