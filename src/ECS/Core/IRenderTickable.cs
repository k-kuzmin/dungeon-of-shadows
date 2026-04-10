namespace DungeonOfShadows.ECS;

/// <summary>
/// Фаза рендеринга: World — между BeginMode2D/EndMode2D, Screen — после EndMode2D.
/// </summary>
public enum RenderPhase
{
    World,
    Screen
}

/// <summary>
/// Рендер-подсистема, вызывается из RenderSystem в нужном контексте рисования.
/// Регистрируется через AddRenderTickable в ServiceRegistration.
/// Порядок регистрации = порядок отрисовки.
/// </summary>
public interface IRenderTickable
{
    RenderPhase Phase { get; }
    void Tick(float dt);
}
