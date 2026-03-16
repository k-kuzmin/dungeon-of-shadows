using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Рисует тайлы карты с учётом FOV и декорации. World-space.
/// </summary>
public class TileRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;

    public RenderPhase Phase => RenderPhase.World;

    private static readonly Color WallColor = new(50, 40, 60, 255);
    private static readonly Color FloorColor = new(90, 80, 70, 255);
    private static readonly Color FloorAltColor = new(85, 75, 65, 255);
    private static readonly Color StairColor = new(200, 160, 50, 255);

    private static readonly Color TorchColor = new(255, 180, 50, 200);
    private static readonly Color CrackColor = new(60, 55, 50, 180);
    private static readonly Color BonesColor = new(200, 190, 170, 180);
    private static readonly Color PuddleColor = new(40, 60, 100, 160);

    public TileRenderSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        var map = _ctx.Map;
        var cam = _ctx.Camera;
        int ts = _ctx.Config.ScaledTileSize;

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
                var tile = map.Tiles[x, y];
                if (tile.Visibility == 0) continue;

                Color color = tile.Type switch
                {
                    TileType.Wall => WallColor,
                    TileType.Floor => ((x + y) % 2 == 0) ? FloorColor : FloorAltColor,
                    TileType.StairDown => StairColor,
                    _ => Color.Black
                };

                if (tile.Visibility == 1)
                    color = DarkenColor(color, 0.4f);

                Raylib.DrawRectangle(x * ts, y * ts, ts, ts, color);

                // Декорации — только на видимых тайлах
                if (tile.Visibility == 2)
                {
                    var deco = map.Decorations[x, y];
                    if (deco != DecorationType.None)
                    {
                        int decoSize = ts / 4;
                        int decoX = x * ts + ts / 2 - decoSize / 2;
                        int decoY = y * ts + ts / 2 - decoSize / 2;
                        Color decoColor = deco switch
                        {
                            DecorationType.Torch => TorchColor,
                            DecorationType.Crack => CrackColor,
                            DecorationType.Bones => BonesColor,
                            DecorationType.Puddle => PuddleColor,
                            _ => Color.Blank
                        };
                        Raylib.DrawRectangle(decoX, decoY, decoSize, decoSize, decoColor);
                    }
                }
            }
        }
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
