namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Компонент декоративного объекта (бочка, ящик, мешок, камень).
/// Координаты атласа указывают на позицию в Objects.png (16x16 grid).
/// </summary>
public struct DecorationObject
{
    public DecorationObjectType Type;
    public int AtlasCol;
    public int AtlasRow;
    public int SrcWidth;
    public int SrcHeight;
}
