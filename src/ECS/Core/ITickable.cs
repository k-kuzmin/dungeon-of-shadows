namespace DungeonOfShadows.ECS;

/// <summary>
/// Any service that needs to tick each frame.
/// Registered as singleton — automatically picked up by the game loop.
/// </summary>
public interface ITickable
{
    void Tick(float dt);
}
