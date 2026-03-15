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
        services.AddTickable<CameraSystem>();
        services.AddTickable<RenderSystem>();

        // Game
        services.AddSingleton<Game>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Registers a system as both its concrete type and ITickable.
    /// One line per system, automatically picked up by the game loop.
    /// </summary>
    private static void AddTickable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : class, ITickable
    {
        services.AddSingleton<T>();
        services.AddSingleton<ITickable>(sp => sp.GetRequiredService<T>());
    }
}
