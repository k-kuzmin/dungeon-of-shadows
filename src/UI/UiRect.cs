using Raylib_cs;

namespace DungeonOfShadows.UI;

/// <summary>
/// Screen-space прямоугольник для layout, hit-test и render. Stack-only, zero-alloc.
/// </summary>
public readonly struct UiRect
{
    public readonly int X;
    public readonly int Y;
    public readonly int W;
    public readonly int H;

    public UiRect(int x, int y, int w, int h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    public bool Contains(int px, int py)
        => px >= X && px < X + W && py >= Y && py < Y + H;

    public bool Contains(System.Numerics.Vector2 p)
        => p.X >= X && p.X < X + W && p.Y >= Y && p.Y < Y + H;

    public Rectangle ToRaylib()
        => new(X, Y, W, H);

    public int Right => X + W;
    public int Bottom => Y + H;
    public int CenterX => X + W / 2;
    public int CenterY => Y + H / 2;
}
