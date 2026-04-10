# Plan: Phase 4 Items

## Status
- ✅ Done

## Tasks
- ✅ Add item domain models and enums (ItemType, Rarity, effects)
- ✅ Add item config section (`GameConfig.Items`) and tunables
- ✅ Add JSON item definitions and loader (built-in .NET JSON)
- ✅ Add ECS item components (inventory, equipment, quick slots, ground item, chest)
- ✅ Implement rarity + procedural bonus generation (deterministic seed)
- ✅ Implement enemy-type loot drop tables and loot roll system
- ✅ Integrate loot trigger with enemy death flow
- ✅ Implement nearest-in-radius pickup logic with full-inventory behavior
- ✅ Implement auto-equip replacement + drop old gear
- ✅ Implement unequip-to-floor fallback when no slot
- ✅ Implement chest entity spawn/open and guaranteed loot
- ✅ Implement item usage system (potions + full scroll effects)
- ✅ Implement cooldowns per item type
- ✅ Implement quick slots (1-4) bound to item type
- ✅ Implement inventory screen (I) with max UX: grid, tooltip, compare, drag/drop
- ✅ Add HUD quick slots and feedback text for failed pickup
- ✅ Register all new systems/render systems in DI with correct order
- ✅ Fix pre-existing hot-path allocation hotspots touched by this phase
- ✅ Build and run validation (`dotnet build`, smoke run)
- ✅ Apply Phase 6 review fixes (High + Medium)

## Files
| File | State | Notes |
|------|-------|-------|
| src/Core/ServiceRegistration.cs | done | item systems/renderers registered in order |
| src/Core/GameContext.cs | done | item event queue + inventory/UI state fields |
| src/Core/Config/GameConfig.Items.cs | new | item tunables |
| src/ECS/Items/... | new | components/systems/renderers/data |
| assets/data/items.json | new | item definitions |
| src/ECS/Combat/Systems/HealthSystem.cs | done | emit loot drops on enemy death |
| src/ECS/Exploration/FloorTransitionSystem.cs | done | spawn chests on new floor + reduced LINQ usage |
| src/Core/Game.cs | done | player item components + chest spawn on init |
| src/ECS/Rendering/HudRenderSystem.cs | partial | quick slots/message rendered in InventoryRenderSystem |
