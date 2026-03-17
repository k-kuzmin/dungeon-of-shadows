# Context: Phase 4 Items

> status: done
> current_phase: 7
> last_updated: 2026-03-17 12:18:18+03:00
> platform: Generic

## Platform
- Detected: Generic
- Notes: `DungeonOfShadows.csproj` exists, but there are no Unity root markers (`Assets/`, `ProjectSettings/`). Project is a .NET 8 game with Raylib-cs.

## Current Phase
Summary - Done

## Last Action
Finalized all phase artifacts (architecture/plan/decisions/context) and completed build + smoke startup checks.

## Next Action
Task completed.

## Blockers
None.

## Files Read This Session
- docs/plans/phase_4_items.md
- .github/skills/feature-dev/SKILL.md
- .github/copilot-instructions.md
- .claude/CLAUDE.md
- src/Core/ServiceRegistration.cs
- src/Core/GameContext.cs
- src/Core/Game.cs
- src/Core/GameConfig.cs
- src/Core/Config/GameConfig.Combat.cs
- src/Program.cs
- src/ECS/Core/World.cs
- src/ECS/Combat/Systems/AISystem.cs
- src/ECS/Combat/Systems/CombatInputSystem.cs
- src/ECS/Combat/Systems/CombatRenderSystem.cs
- src/ECS/Combat/Systems/HealthSystem.cs
- src/ECS/Exploration/FloorTransitionSystem.cs
- src/ECS/Rendering/RenderSystem.cs
- src/ECS/Rendering/HudRenderSystem.cs
- src/ECS/Player/PlayerTag.cs
- src/ECS/Combat/Components/EnemyTag.cs
- src/ECS/Combat/EnemySpawner.cs
- src/ECS/Combat/Systems/DamageNumberSystem.cs

## Notes
Feature candidate is complex (multiple subsystems: data loading, ECS components/systems, rendering/HUD, input, loot flow, inventory UI). Likely requires architecture phase with alternatives before implementation.

Clarifications captured:
- Scope: full phase 4.1-4.7 in this feature track.
- Inventory UI: implement now.
- Persistence: runtime only (no save/load yet).
- Performance target: stable 60 FPS, minimal allocations.
- Libraries: only built-in .NET APIs.
- Hot-path interpretation: tick and render/HUD should follow strict low-allocation rules; inventory screen logic can be less strict because it is modal and not always active.

Phase 2 findings (validated):
- DI-first architecture with strict tick order in ServiceRegistration; RenderSystem orchestrates world/screen phases.
- Existing event-queue pattern through GameContext.DamageEvents (producer systems + HealthSystem consumer).
- Zero-allocation ECS query pattern exists and should be reused: World.QueryInto(...) with reusable List<int> fields.
- Confirmed allocation hotspots to account for during item rollout: string interpolation in HudRenderSystem, LINQ ToList/Where in AISystem and FloorTransitionSystem, per-frame text conversions in CombatRenderSystem.
- GameConfig currently instantiated in Program.cs with built-in defaults; JSON loading pipeline for external item definitions is not yet implemented.

Phase 5 progress:
- Added item config block (`GameConfig.Items`).
- Added `assets/data/items.json` plus AOT-safe source-generated JSON context.
- Added ECS item model/components (`Inventory`, `Equipment`, `QuickSlots`, `Chest`, `ItemOnGround`, `ItemStack`).
- Added systems: `InventoryInputSystem`, `ItemDropSystem`, `ItemPickupSystem`, `ChestSystem`, `ItemUseSystem`, `ItemRenderSystem`, `InventoryRenderSystem`.
- Wired systems in DI and render pipeline.
- Added loot enqueue on enemy death and chest spawn in init/floor transition.
- Build status: `dotnet build` successful.
