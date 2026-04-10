namespace DungeonOfShadows.ECS;

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
