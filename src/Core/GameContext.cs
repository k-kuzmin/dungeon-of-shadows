using Raylib_cs;
using DungeonOfShadows.Dungeon;
using DungeonOfShadows.ECS.Combat;
using DungeonOfShadows.ECS.Items;

namespace DungeonOfShadows.Core;

/// <summary>
/// Разделяемое мутабельное состояние игры.
/// World и GameConfig инжектятся напрямую через DI — здесь только рантайм-состояние.
/// </summary>
public class GameContext
{
    public GameState State { get; set; } = GameState.Playing;
    public TileMap Map { get; set; } = null!;
    public Camera2D Camera { get; set; }
    public int CurrentFloor { get; set; } = 1;
    public int DungeonSeed { get; set; }
    public bool DebugMode { get; set; }
    public bool ShowFullMap { get; set; }
    public bool ShowInventory { get; set; }
    public string UiMessage { get; set; } = string.Empty;
    public float UiMessageTimer { get; set; }

    /// <summary>
    /// Флаг запроса перехода на следующий этаж. Устанавливается FloorTransitionSystem,
    /// обрабатывается FloorLifecycleSystem.
    /// </summary>
    public bool FloorTransitionRequested { get; set; }

    /// <summary>
    /// Очередь событий урона. Заполняется MeleeAttackSystem/AISystem, дренится HealthSystem.
    /// </summary>
    public List<DamageEvent> DamageEvents { get; } = new(32);

    /// <summary>
    /// Очередь запросов на дроп предметов при смерти врагов.
    /// </summary>
    public List<ItemDropRequest> ItemDropRequests { get; } = new(32);

    public GameContext(GameConfig config)
    {
        Camera = new Camera2D
        {
            Offset = new System.Numerics.Vector2(config.ScreenWidth / 2f, config.ScreenHeight / 2f),
            Target = System.Numerics.Vector2.Zero,
            Rotation = 0f,
            Zoom = 1f
        };
    }
}
