namespace DungeonOfShadows.ECS;

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
