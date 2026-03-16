namespace DungeonOfShadows.ECS;

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
