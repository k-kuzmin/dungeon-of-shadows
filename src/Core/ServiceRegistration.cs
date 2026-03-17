using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using DungeonOfShadows.ECS;
using DungeonOfShadows.ECS.Combat;
using DungeonOfShadows.ECS.Combat.Systems;
using DungeonOfShadows.ECS.Player.Systems;
using DungeonOfShadows.ECS.Physics.Systems;
using DungeonOfShadows.ECS.Exploration.Systems;
using DungeonOfShadows.ECS.Items;
using DungeonOfShadows.ECS.Items.Systems;
using DungeonOfShadows.ECS.Rendering.Systems;

namespace DungeonOfShadows.Core;

public static class ServiceRegistration
{
    public static IServiceProvider Build(GameConfig config)
    {
        var services = new ServiceCollection();

        // Core
        services.AddSingleton(config);
        services.AddSingleton<World>();
        services.AddSingleton<GameContext>();

        // Assets
        services.AddInterfaces<AssetProvider>();

        // Services
        services.AddSingleton<AStarPathfinder>();
        services.AddInterfaces<ItemDatabase>();

        // Systems — registration order = tick/start/draw order
        services.AddInterfaces<FloorLifecycleSystem>();
        services.AddInterfaces<InputSystem>();
        services.AddInterfaces<InventoryInputSystem>();
        services.AddInterfaces<CombatInputSystem>();
        services.AddInterfaces<DashSystem>();
        services.AddInterfaces<PhysicsSystem>();
        services.AddInterfaces<MeleeAttackSystem>();
        services.AddInterfaces<AISystem>();
        services.AddInterfaces<HealthSystem>();
        services.AddInterfaces<ItemDropSystem>();
        services.AddInterfaces<ItemPickupSystem>();
        services.AddInterfaces<ChestSystem>();
        services.AddInterfaces<ItemUseSystem>();
        services.AddInterfaces<DamageNumberSystem>();
        services.AddInterfaces<FloorTransitionSystem>();
        services.AddInterfaces<FovSystem>();
        services.AddInterfaces<AnimationSystem>();
        services.AddInterfaces<CameraSystem>();
        services.AddInterfaces<RenderSystem>();

        // Render sub-systems — registration order = draw order
        services.AddInterfaces<TileRenderSystem>();
        services.AddInterfaces<EntityRenderSystem>();
        services.AddInterfaces<CombatRenderSystem>();
        services.AddInterfaces<ItemRenderSystem>();
        services.AddInterfaces<DecorationObjectRenderSystem>();
        services.AddInterfaces<DebugRenderSystem>();
        services.AddInterfaces<HudRenderSystem>();
        services.AddInterfaces<InventoryRenderSystem>();

        // Game
        services.AddSingleton<Game>();

        return services.BuildServiceProvider();
    }

    private static void AddInterfaces<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] T>(
        this IServiceCollection services) where T : class
    {
        services.AddSingleton<T>();

        foreach (var iface in typeof(T).GetInterfaces())
        {
            services.AddSingleton(iface, sp => sp.GetRequiredService<T>());
        }
    }
}
