using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using DungeonOfShadows.ECS;
using DungeonOfShadows.ECS.Combat;
using DungeonOfShadows.ECS.Combat.Systems;
using DungeonOfShadows.ECS.Player.Systems;
using DungeonOfShadows.ECS.Physics.Systems;
using DungeonOfShadows.ECS.Exploration.Systems;
using DungeonOfShadows.ECS.Rendering.Systems;

namespace DungeonOfShadows.Core;

public static class ServiceRegistration
{
    public static IServiceProvider Build(GameConfig config)
    {
        var services = new ServiceCollection();

        // Core
        services.AddSingleton(config);
        services.AddSingleton<GameContext>();
        services.AddSingleton<World>(sp => sp.GetRequiredService<GameContext>().World);

        // Combat services
        services.AddSingleton<AStarPathfinder>();

        // Logic systems — registration order = tick order
        services.AddTickable<InputSystem>();
        services.AddTickable<CombatInputSystem>();
        services.AddTickable<DashSystem>();
        services.AddTickable<PhysicsSystem>();
        services.AddTickable<MeleeAttackSystem>();
        services.AddTickable<AISystem>();
        services.AddTickable<HealthSystem>();
        services.AddTickable<DamageNumberSystem>();
        services.AddTickable<FloorTransitionSystem>();
        services.AddTickable<FovSystem>();
        services.AddTickable<CameraSystem>();
        services.AddTickable<RenderSystem>();

        // Render sub-systems — registration order = draw order
        services.AddRenderTickable<TileRenderSystem>();
        services.AddRenderTickable<EntityRenderSystem>();
        services.AddRenderTickable<CombatRenderSystem>();
        services.AddRenderTickable<DebugRenderSystem>();
        services.AddRenderTickable<HudRenderSystem>();

        // Game
        services.AddSingleton<Game>();

        return services.BuildServiceProvider();
    }

    private static void AddTickable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : class, ITickable
    {
        services.AddSingleton<T>();
        services.AddSingleton<ITickable>(sp => sp.GetRequiredService<T>());
    }

    private static void AddRenderTickable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : class, IRenderTickable
    {
        services.AddSingleton<T>();
        services.AddSingleton<IRenderTickable>(sp => sp.GetRequiredService<T>());
    }
}
