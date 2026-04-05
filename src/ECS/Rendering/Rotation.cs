namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Угол поворота спрайта в градусах (для снарядов).
/// </summary>
public struct Rotation
{
    public float Degrees;

    public Rotation(float degrees)
    {
        Degrees = degrees;
    }
}
