using System.Numerics;

namespace DungeonOfShadows.ECS;

public struct Position
{
    public float X;
    public float Y;

    public Position(float x, float y) { X = x; Y = y; }

    public Vector2 ToVector2() => new(X, Y);
}
