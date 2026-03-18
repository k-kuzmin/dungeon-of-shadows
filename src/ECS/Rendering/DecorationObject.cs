namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Компонент декоративного объекта (бочка, ящик, мешок, камень).
/// Source-координаты задаются в пикселях, чтобы поддерживать нестрогую сетку в атласе.
/// </summary>
public struct DecorationObject
{
    public DecorationObjectType Type;
    public int SrcX;
    public int SrcY;
    public int SrcWidth;
    public int SrcHeight;
}
