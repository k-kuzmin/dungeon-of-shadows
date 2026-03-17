using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Рисует тайлы карты спрайтами с учётом FOV, автотайлинга, оверлеев трещин и анимации факелов.
/// </summary>
public class TileRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;

    public RenderPhase Phase => RenderPhase.World;

    private static readonly Color ExploredTint = new(100, 100, 100, 255);
    private static readonly Color FullTint = Color.White;

    public TileRenderSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        var map = _ctx.Map;
        var cam = _ctx.Camera;
        var config = _ctx.Config;
        int ts = config.ScaledTileSize;
        float scale = config.RenderScale;

        // Текстуры — кешируем в локальные переменные (zero dict lookups в цикле)
        var wallsTex = _ctx.Textures.Get("walls_floor");
        var crackFloorTex = _ctx.Textures.Get("cracks_floor");
        var crackWallTex = _ctx.Textures.Get("cracks_walls");
        var fireTex = _ctx.Textures.Get("fire_large");
        var objectsTex = _ctx.Textures.Get("objects");

        // Frustum culling
        float left = cam.Target.X - cam.Offset.X / cam.Zoom;
        float top = cam.Target.Y - cam.Offset.Y / cam.Zoom;
        float right = cam.Target.X + cam.Offset.X / cam.Zoom;
        float bottom = cam.Target.Y + cam.Offset.Y / cam.Zoom;

        int minX = Math.Max(0, (int)(left / ts) - 1);
        int minY = Math.Max(0, (int)(top / ts) - 1);
        int maxX = Math.Min(map.Width - 1, (int)(right / ts) + 1);
        int maxY = Math.Min(map.Height - 1, (int)(bottom / ts) + 1);

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
                        DrawWall(wallsTex, ref tile, destX, destY, ts, scale, tint);
                        break;
                    case TileType.Floor:
                        DrawFloor(wallsTex, ref tile, destX, destY, ts, scale, tint);
                        break;
                    case TileType.StairDown:
                        // Пол под лестницей + спрайт лестницы
                        DrawFloorBase(wallsTex, destX, destY, ts, scale, tint);
                        DrawStair(objectsTex, destX, destY, ts, scale, tint);
                        break;
                    default:
                        Raylib.DrawRectangle((int)destX, (int)destY, ts, ts, Color.Black);
                        break;
                }

                // Оверлей трещин (только на видимых тайлах)
                if (tile.Visibility == 2 && tile.OverlayIndex > 0)
                {
                    bool isWall = tile.Type == TileType.Wall;
                    var crackSrc = TileAtlas.GetCrackSource(tile.OverlayIndex, isWall);
                    var crackTex = isWall ? crackWallTex : crackFloorTex;
                    var destRect = new Rectangle(destX, destY, ts, ts);
                    Raylib.DrawTexturePro(crackTex, crackSrc, destRect, System.Numerics.Vector2.Zero, 0f, FullTint);
                }

                // Декорации — только на видимых тайлах
                if (tile.Visibility == 2)
                {
                    var deco = map.Decorations[x, y];
                    if (deco == DecorationType.Torch)
                    {
                        DrawTorch(map, fireTex, x, y, dt, ts, scale, config);
                    }
                    // Bones и Puddle — пока цветовые заглушки
                    else if (deco == DecorationType.Bones)
                    {
                        int sz = ts / 4;
                        Raylib.DrawRectangle((int)destX + ts / 2 - sz / 2,
                            (int)destY + ts / 2 - sz / 2, sz, sz,
                            new Color(200, 190, 170, 180));
                    }
                    else if (deco == DecorationType.Puddle)
                    {
                        int sz = ts / 4;
                        Raylib.DrawRectangle((int)destX + ts / 2 - sz / 2,
                            (int)destY + ts / 2 - sz / 2, sz, sz,
                            new Color(40, 60, 100, 160));
                    }
                }
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
        // Базовый пол (variant 0) под лестницей
        var src = TileAtlas.GetFloorSource(0);
        var dest = new Rectangle(dx, dy, ts, ts);
        Raylib.DrawTexturePro(tex, src, dest, System.Numerics.Vector2.Zero, 0f, tint);
    }

    private static void DrawStair(Texture2D tex, float dx, float dy,
        int ts, float scale, Color tint)
    {
        // Лестница из Objects.png — 16x32 → масштабированная ts x ts*2
        // Рисуем так, чтобы нижняя часть лестницы была на тайле
        var src = TileAtlas.GetStairSource();
        var dest = new Rectangle(dx, dy - ts, ts, ts * 2);
        Raylib.DrawTexturePro(tex, src, dest, System.Numerics.Vector2.Zero, 0f, tint);
    }

    private void DrawTorch(TileMap map, Texture2D fireTex, int x, int y, float dt,
        int ts, float scale, GameConfig config)
    {
        int idx = y * map.Width + x;

        // Обновляем анимацию
        map.TileAnimTimers[idx] += dt;
        if (map.TileAnimTimers[idx] >= config.TorchFrameDuration)
        {
            map.TileAnimTimers[idx] -= config.TorchFrameDuration;
            map.TileAnimFrames[idx] = (byte)((map.TileAnimFrames[idx] + 1) % config.TorchFrameCount);
        }

        var src = TileAtlas.GetTorchSource(map.TileAnimFrames[idx]);
        // Факел 32x48 рисуется с центром на тайле, выходя за пределы вверх
        float torchW = config.TorchSpriteWidth * scale;
        float torchH = config.TorchSpriteHeight * scale;
        float destX = x * ts + ts / 2f - torchW / 2f;
        float destY = y * ts + ts - torchH; // основание факела на нижнем крае тайла
        var dest = new Rectangle(destX, destY, torchW, torchH);
        Raylib.DrawTexturePro(fireTex, src, dest, System.Numerics.Vector2.Zero, 0f, Color.White);
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
