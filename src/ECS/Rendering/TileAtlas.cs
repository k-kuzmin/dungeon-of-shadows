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
    private const byte N = 1;
    private const byte NE = 2;
    private const byte E = 4;
    private const byte SE = 8;
    private const byte S = 16;
    private const byte SW = 32;
    private const byte W = 64;
    private const byte NW = 128;

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
        (2, 8),   // 1: базовый пол
        (2, 8),   // 2: базовый пол
        (2, 8),   // 3: базовый пол
        (7, 13),  // 4: вариант
        (7, 14),  // 5: вариант
        (8, 13),  // 6: вариант
        (8, 14),  // 7: вариант
    };

    // ── Тёмный пол (у стен) ──
    // Row 14 col 12 = база, edges вокруг
    private static readonly (int Col, int Row)[] _darkFloorCoords =
    {
        (12, 14), // 0: база
        (9, 14),  // 1: вариант
        (10, 14), // 2: вариант
        (9, 13),  // 3: вариант
        (10, 13), // 4: вариант
        (9, 13),  // 5: top-left corner
        (10, 13), // 6: top-right corner
        (9, 14),  // 7: left inner corner
        (10, 14), // 8: right inner corner
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
    // Координаты определены вручную по atlas_viewer.html.
    private static readonly Rectangle _stairSrc = new(16, 80, 31, 31);

    // ── Трещины пола: decorative_cracks_floor.png (8 cols x 15 rows) ──
    // 4 варианта, каждый 2x2 тайла (32x32 px). OverlayIndex 1-4.
    private static readonly Rectangle[] _crackFloorRects =
    {
        new(0 * T, 12 * T, T * 2, T * 2),  // 1: cols 0-1, rows 12-13
        new(2 * T, 12 * T, T * 2, T * 2),  // 2: cols 2-3, rows 12-13
        new(4 * T, 12 * T, T * 2, T * 2),  // 3: cols 4-5, rows 12-13
        new(6 * T, 12 * T, T * 2, T * 2),  // 4: cols 6-7, rows 12-13
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
    // В TMX анимация сундука (tileid=0) идёт вертикально с шагом 30 tileid:
    // 0 -> 30 -> 60 -> 90 -> 120.
    // Это соответствует y: 0, 48, 96, 144, 192 (по 3 строки на кадр).
    private static readonly Rectangle[] _chestClosedFrames =
    {
        new(8 * T, 1 * T, T * 2, T * 2),
    };
    private static readonly Rectangle[] _chestOpenFrames =
    {
        new(8 * T, 1 * T, T * 2, T * 2),    // кадр 0 (18/19 + 28/29)
        new(8 * T, 4 * T, T * 2, T * 2),    // кадр 1 (48/49 + 58/59)
        new(8 * T, 7 * T, T * 2, T * 2),    // кадр 2 (78/79 + 88/89)
        new(8 * T, 10 * T, T * 2, T * 2),   // кадр 3 (108/109 + 118/119)
        new(8 * T, 13 * T, T * 2, T * 2),   // кадр 4 (138/139 + 148/149)
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
        byte idx = Math.Min(autotileIndex, (byte)46);
        var (col, row) = _wallCoords[idx];
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

    /// <summary>Возвращает source rect для трещины пола (32x32, 2x2 тайла).</summary>
    public static Rectangle GetCrackFloorSource(byte overlayIndex)
    {
        if (overlayIndex == 0) return default;
        int idx = Math.Min(overlayIndex - 1, _crackFloorRects.Length - 1);
        return _crackFloorRects[idx];
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
    public static Rectangle GetDecoObjectSource(int srcX, int srcY, int srcWidth, int srcHeight)
    {
        return new Rectangle(srcX, srcY, srcWidth, srcHeight);
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
        coords[6] = (2, 2);

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
        coords[16] = (2, 2);

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
        //   (7, 1) = inner corner NE (выемка в правом верхнем углу)
        //   (5, 1) = inner corner NW (выемка в левом верхнем углу)
        //   (7, 4) = inner corner SE (выемка в правом нижнем углу)
        //   (5, 4) = inner corner SW (выемка в левом нижнем углу)
        //
        // "Missing corner X" означает что X-сосед = пол → рисуем выемку в этом углу.
        // Если не хватает нескольких углов — используем solid fill (этот тайлсет не имеет
        // комбинированных inner corner тайлов).

        // 31: all cardinal, NO corners — 4 выемки.
        coords[31] = (2, 2);

        // Для частичных заполнений внутренних углов используем ближайшие corner-тайлы,
        // чтобы не терять силуэт внутренних выемок.

        // 32: +NE (SE/SW/NW пустые)
        coords[32] = (2, 2);

        // 33: +SE (NE/NW/SW пустые)
        coords[33] = (2, 2);

        // 34: +SW (NE/SE/NW пустые)
        coords[34] = (2, 2);

        // 35: +NW (NE/SE/SW пустые)
        coords[35] = (2, 2);

        // 36: +NE+SE — NW и SW пустые
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

        // 42: +NE+SE+SW — только NW пустой → inner corner NW
        coords[42] = (7, 4);

        // 43: +NE+SE+NW — только SW пустой → inner corner SW
        coords[43] = (7, 1);

        // 44: +NE+SW+NW — только SE пустой → inner corner SE
        coords[44] = (5, 1);

        // 45: +SE+SW+NW — только NE пустой → inner corner NE
        coords[45] = (5, 4);

        // 46: fully surrounded (all 8 neighbors = wall) — solid fill
        coords[46] = (2, 2);

        return coords;
    }
}
