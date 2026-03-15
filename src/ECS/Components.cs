using System.Numerics;

namespace DungeonOfShadows.ECS;

public struct Position
{
    public float X;
    public float Y;

    public Position(float x, float y) { X = x; Y = y; }

    public Vector2 ToVector2() => new(X, Y);
}

public struct Velocity
{
    public float X;
    public float Y;

    public Velocity(float x, float y) { X = x; Y = y; }
}

public struct Sprite
{
    public int TextureId;
    public int SrcX;
    public int SrcY;
    public int Width;
    public int Height;
    public Raylib_cs.Color Tint;

    public Sprite(Raylib_cs.Color tint, int width = 16, int height = 16)
    {
        TextureId = -1;
        SrcX = 0;
        SrcY = 0;
        Width = width;
        Height = height;
        Tint = tint;
    }
}

public struct Collider
{
    public float OffsetX;
    public float OffsetY;
    public float Width;
    public float Height;

    public Collider(float width, float height, float offsetX = 0, float offsetY = 0)
    {
        Width = width;
        Height = height;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }
}

public struct PlayerTag
{
    public float Speed;
    public int FacingX;
    public int FacingY;

    public PlayerTag(float speed)
    {
        Speed = speed;
        FacingX = 0;
        FacingY = 1;
    }
}
