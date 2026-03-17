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
        var crackWallTex = _assets.GetTexture("cracks_walls");
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
        var dest = new Rectangle(dx, dy - ts, ts, ts * 2);
        Raylib.DrawTexturePro(tex, src, dest, System.Numerics.Vector2.Zero, 0f, tint);
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
