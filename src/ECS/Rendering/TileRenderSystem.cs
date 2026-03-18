using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Рисует тайлы карты спрайтами с учётом FOV, автотайлинга и оверлеев трещин.
/// </summary>
public class TileRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly IAssetProvider _assets;
    private readonly GameConfig _config;

    public RenderPhase Phase => RenderPhase.World;

    private static readonly Color ExploredTint = new(100, 100, 100, 255);
    private static readonly Color FullTint = Color.White;

    public TileRenderSystem(GameContext ctx, IAssetProvider assets, GameConfig config)
    {
        _ctx = ctx;
        _assets = assets;
        _config = config;
    }

    public void Tick(float dt)
    {
        var map = _ctx.Map;
        var cam = _ctx.Camera;
        var config = _config;
        int ts = config.ScaledTileSize;
        float scale = config.RenderScale;

        // Текстуры — кешируем в локальные переменные (zero dict lookups в цикле)
        var wallsTex = _assets.GetTexture("walls_floor");
        var crackFloorTex = _assets.GetTexture("cracks_floor");
        var objectsTex = _assets.GetTexture("objects");

        // Frustum culling
        float left = cam.Target.X - cam.Offset.X / cam.Zoom;
        float top = cam.Target.Y - cam.Offset.Y / cam.Zoom;
        float right = cam.Target.X + cam.Offset.X / cam.Zoom;
        float bottom = cam.Target.Y + cam.Offset.Y / cam.Zoom;

        int minX = Math.Max(0, (int)(left / ts) - 1);
        int minY = Math.Max(0, (int)(top / ts) - 1);
        int maxX = Math.Min(map.Width - 1, (int)(right / ts) + 1);
        int maxY = Math.Min(map.Height - 1, (int)(bottom / ts) + 1);

        // Проход 1: стены, полы, оверлеи (всё кроме лестниц)
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                ref var tile = ref map.Tiles[x, y];
                if (tile.Visibility == 0) continue;

                Color tint = tile.Visibility == 1 ? ExploredTint : FullTint;
                float destX = x * ts;
                float destY = y * ts;

                switch (tile.Type)
                {
                    case TileType.Wall:
                        if (tile.AutotileIndex < 31)
                        {
                            // Стык боковой стены с фасадом: стена сверху + пол сбоку + стена сверху-сбоку
                            bool floorRight = !IsWallTile(map, x + 1, y) && IsWallTile(map, x + 1, y - 1);
                            bool floorLeft = !IsWallTile(map, x - 1, y) && IsWallTile(map, x - 1, y - 1);
                            bool cornerJunction = IsWallTile(map, x, y - 1) && (floorRight || floorLeft);

                            if (cornerJunction)
                            {
                                // Подложка пола → стыковый тайл (5,3 / 7,3) поверх
                                DrawFloorBase(wallsTex, destX, destY, ts, scale, tint);
                                var jSrc = TileAtlas.GetCornerJunctionSource(floorRight);
                                var jDest = new Rectangle(destX, destY, ts, ts);
                                Raylib.DrawTexturePro(wallsTex, jSrc, jDest, System.Numerics.Vector2.Zero, 0f, tint);
                            }
                            else
                            {
                                DrawFloorBase(wallsTex, destX, destY, ts, scale, tint);
                                DrawWall(wallsTex, ref tile, destX, destY, ts, scale, tint);
                            }
                        }
                        else
                        {
                            DrawWall(wallsTex, ref tile, destX, destY, ts, scale, tint);
                        }
                        break;
                    case TileType.Floor:
                        DrawFloor(wallsTex, ref tile, destX, destY, ts, scale, tint);
                        break;
                    case TileType.StairDown:
                        DrawFloorBase(wallsTex, destX, destY, ts, scale, tint);
                        break;
                    default:
                        Raylib.DrawRectangle((int)destX, (int)destY, ts, ts, Color.Black);
                        break;
                }

                // Оверлей трещин пола (2x2 тайла, центрируется на текущем тайле)
                if (tile.Type == TileType.Floor && tile.Visibility == 2 && tile.OverlayIndex > 0)
                {
                    var crackSrc = TileAtlas.GetCrackFloorSource(tile.OverlayIndex);
                    float crackW = crackSrc.Width * scale;
                    float crackH = crackSrc.Height * scale;
                    float crackX = destX + ts / 2f - crackW / 2f;
                    float crackY = destY + ts / 2f - crackH / 2f;
                    var destRect = new Rectangle(crackX, crackY, crackW, crackH);
                    Raylib.DrawTexturePro(crackFloorTex, crackSrc, destRect, System.Numerics.Vector2.Zero, 0f, FullTint);
                }
            }
        }

        // Проход 2: фасады стен (wall face) — на тайлах пола, у которых сверху стена
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                ref var tile = ref map.Tiles[x, y];
                if (tile.Type != TileType.Floor && tile.Type != TileType.StairDown) continue;
                if (tile.Visibility == 0) continue;
                if (!IsWallTile(map, x, y - 1)) continue;

                Color tint = tile.Visibility == 1 ? ExploredTint : FullTint;
                int variant = GetWallFaceVariant(map, x, y);
                var src = TileAtlas.GetWallFaceSource(variant);
                var dest = new Rectangle(x * ts, y * ts, ts, ts);
                Raylib.DrawTexturePro(wallsTex, src, dest, System.Numerics.Vector2.Zero, 0f, tint);
            }
        }

        // Проход 3: лестницы (поверх всех тайлов, чтобы соседние полы не обрезали)
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                ref var tile = ref map.Tiles[x, y];
                if (tile.Type != TileType.StairDown) continue;
                if (tile.Visibility == 0) continue;

                Color tint = tile.Visibility == 1 ? ExploredTint : FullTint;
                DrawStair(objectsTex, x * ts, y * ts, ts, scale, tint);
            }
        }
    }

    private static void DrawWall(Texture2D tex, ref Tile tile, float dx, float dy,
        int ts, float scale, Color tint)
    {
        var src = TileAtlas.GetWallSource(tile.AutotileIndex);
        var dest = new Rectangle(dx, dy, ts, ts);
        Raylib.DrawTexturePro(tex, src, dest, System.Numerics.Vector2.Zero, 0f, tint);
    }

    private static void DrawFloor(Texture2D tex, ref Tile tile, float dx, float dy,
        int ts, float scale, Color tint)
    {
        var src = TileAtlas.GetFloorSource(tile.FloorVariant);
        var dest = new Rectangle(dx, dy, ts, ts);
        Raylib.DrawTexturePro(tex, src, dest, System.Numerics.Vector2.Zero, 0f, tint);
    }

    private static void DrawFloorBase(Texture2D tex, float dx, float dy,
        int ts, float scale, Color tint)
    {
        var src = TileAtlas.GetFloorSource(0);
        var dest = new Rectangle(dx, dy, ts, ts);
        Raylib.DrawTexturePro(tex, src, dest, System.Numerics.Vector2.Zero, 0f, tint);
    }

    private static void DrawStair(Texture2D tex, float dx, float dy,
        int ts, float scale, Color tint)
    {
        var src = TileAtlas.GetStairSource();
        float destW = src.Width * scale;
        float destH = src.Height * scale;
        // Центрируем на тайле
        float destX = dx + ts / 2f - destW / 2f;
        float destY = dy + ts / 2f - destH / 2f;
        var dest = new Rectangle(destX, destY, destW, destH);
        Raylib.DrawTexturePro(tex, src, dest, System.Numerics.Vector2.Zero, 0f, tint);
    }

    private static bool IsWallTile(TileMap map, int x, int y)
    {
        if (!map.InBounds(x, y)) return true; // за пределами карты = стена
        return map.Tiles[x, y].Type == TileType.Wall;
    }

    /// <summary>
    /// Определяет вариант фасада стены для тайла пола (x,y) у которого (x, y-1) — стена.
    /// 0=fill, 1=внешний угол лево, 2=внешний угол право,
    /// 3=внутренний угол лево, 4=внутренний угол право.
    /// </summary>
    private static int GetWallFaceVariant(TileMap map, int x, int y)
    {
        bool wallLeft = IsWallTile(map, x - 1, y);
        bool wallRight = IsWallTile(map, x + 1, y);

        // Внутренние углы: стена и сверху и сбоку
        if (wallLeft && wallRight) return 0; // окружён стенами с трёх сторон — fill
        if (wallLeft) return 3;  // внутренний угол лево
        if (wallRight) return 4; // внутренний угол право

        // Внешние углы: определяем по продолжению стены сверху
        bool wallAboveLeft = IsWallTile(map, x - 1, y - 1);
        bool wallAboveRight = IsWallTile(map, x + 1, y - 1);

        if (!wallAboveLeft && !wallAboveRight) return 0; // одиночная стена сверху
        if (!wallAboveLeft) return 1;  // внешний угол лево
        if (!wallAboveRight) return 2; // внешний угол право

        return 0; // fill
    }

    internal static Color DarkenColor(Color c, float factor)
    {
        return new Color(
            (byte)(c.R * factor),
            (byte)(c.G * factor),
            (byte)(c.B * factor),
            c.A
        );
    }
}
