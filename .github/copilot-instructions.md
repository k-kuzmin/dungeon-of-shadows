# Project Guidelines

## Build and Run
- Build: `dotnet build DungeonOfShadows.csproj`
- Run: `dotnet run --project DungeonOfShadows.csproj`
- NativeAOT release publish: `dotnet publish -c Release -r win-x64 -p:PublishAot=true`
- NativeAOT release publish (Linux): `dotnet publish -c Release -r linux-x64 -p:PublishAot=true`
- NativeAOT release publish (macOS ARM64): `dotnet publish -c Release -r osx-arm64 -p:PublishAot=true`
- If build artifacts look stale after big refactors, run `dotnet clean` and build again.
- There is currently no automated test suite in this repository. Validate changes with a successful build and focused runtime checks.

## Architecture
- The project is DI-first. Register all systems in `src/Core/ServiceRegistration.cs`.
- Registration order in `ServiceRegistration` is the game tick order. Treat order changes as behavior changes.
- `Game.Run` ticks all systems unconditionally; each system should guard by `GameState` when needed.
- ECS is custom and lightweight:
  - Entity = `int`
  - Components = `struct` (data-only)
  - `World` APIs: `Add<T>`, `Get<T>`, `Has<T>`, `Remove<T>`, `QueryInto<T>`, `QueryInto<T1,T2>`
- Systems should communicate through data (`GameContext`, components, queues), not by directly calling each other.
- Damage flow uses `GameContext.DamageEvents` queue:
  - Producers: combat/AI systems
  - Consumer: `HealthSystem`
- Rendering uses `RenderSystem` + `IRenderTickable` with two phases:
  - `RenderPhase.World`
  - `RenderPhase.Screen`
- Keep world-space drawing inside `BeginMode2D/EndMode2D`; screen-space rendering belongs to `RenderPhase.Screen`.

## Code Style
- Language: C# 12
- Identifiers: English (class/method/variable names)
- Comments/docs: Russian
- Keep components data-only and in separate files.
- Keep gameplay tuning values in `src/Core/GameConfig.cs` (avoid magic numbers in systems).

## Conventions
- For new logic systems, use `services.AddTickable<MySystem>();`.
- For new render subsystems, use `services.AddRenderTickable<MyRenderSystem>();`.
- Feature-oriented structure is required under `src/ECS/`:
  - `Core`, `Player`, `Physics`, `Exploration`, `Rendering`, `Combat`
- Match namespace to folder structure (for example, `DungeonOfShadows.ECS.Physics.Systems` for files in `src/ECS/Physics/`).
- In hot paths, reuse query buffers (`List<int>` fields) and avoid per-tick allocations.
- Prefer `World.QueryInto(...)` with reusable buffers over LINQ/materialized query allocations in tick methods.
- `HealthSystem` is the consumer of `GameContext.DamageEvents`; producers should append events and not drain the queue.

## Key References
- Architecture and conventions: `.claude/CLAUDE.md`
- Phase plans: `docs/plans/`
- Technical design: `docs/dungeon_of_shadows_tdd.md`
- Tick/render registration order: `src/Core/ServiceRegistration.cs`
- ECS core patterns: `src/ECS/Core/World.cs`
- Render phase orchestration: `src/ECS/Rendering/RenderSystem.cs`
