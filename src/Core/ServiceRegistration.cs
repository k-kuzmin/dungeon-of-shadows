using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using DungeonOfShadows.ECS;
using DungeonOfShadows.ECS.Systems;

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

        // Systems — registration order = tick order
        services.AddTickable<InputSystem>();
        services.AddTickable<PhysicsSystem>();
        services.AddTickable<FloorTransitionSystem>();
        services.AddTickable<FovSystem>();
        services.AddTickable<CameraSystem>();
        services.AddTickable<RenderSystem>();

        // Render sub-systems — registration order = draw order
        services.AddRenderTickable<TileRenderSystem>();
        services.AddRenderTickable<EntityRenderSystem>();
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
