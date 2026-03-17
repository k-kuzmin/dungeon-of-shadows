# Plan: Phase 8 Art — Tiles & Decor (8.1 + 8.2)

## Phase 1 — Foundation
- [ ] Create `TextureManager` (LoadAll/UnloadAll/Get)
- [ ] Add `Textures` field to `GameContext`
- [ ] Call LoadAll/UnloadAll in `Game.Run()`
- [ ] Add `AutotileIndex`, `FloorVariant`, `OverlayIndex` to `Tile` struct
- [ ] Add `TileAnimTimers[]`, `TileAnimFrames[]` to `TileMap`
- [ ] Create `AutotileComputer` with blob lookup table
- [ ] Call `AutotileComputer.ComputeAll()` in `DungeonGenerator`
- [ ] Create `GameConfig.Sprites.cs`

## Phase 2 — Walls & Floors
- [ ] Create `TileAtlas` with `GetWallSource()`, `GetFloorSource()`, `GetStairSource()`
- [ ] Rewrite `TileRenderSystem` to use `DrawTexturePro` for walls/floors/stairs
- [ ] Explored tiles: darkened tint via Color parameter
- [ ] Verify frustum culling

## Phase 3 — Crack Overlays
- [ ] Split `Crack` → `CrackFloor`/`CrackWall` in `DecorationType`
- [ ] Update `DecorationPainter` for contextual crack types
- [ ] `AutotileComputer` writes `OverlayIndex` from crack decorations
- [ ] Add `GetCrackSource()` to `TileAtlas`
- [ ] Draw overlays in `TileRenderSystem`

## Phase 4 — Torch Animation
- [ ] Add `GetTorchSource()` to `TileAtlas` for fire_animation.png
- [ ] Advance timers/frames in `TileRenderSystem` for visible torch tiles
- [ ] Draw 32x48 torch sprite overlapping wall above

## Phase 5 — Decoration Objects
- [ ] Create `DecorationObjectType` enum
- [ ] Create `DecorationObject` struct
- [ ] Create `DecorationObjectSpawner`
- [ ] Call spawner from `Game.Init()` and `FloorTransitionSystem`
- [ ] Add `GetDecoObjectSource()` to `TileAtlas`
- [ ] Create `DecorationObjectRenderSystem` with FOV check
- [ ] Register in `ServiceRegistration.cs`

## Phase 6 — Chest Animation
- [ ] Add `AnimTimer`, `AnimFrame`, `AnimDone` to `Chest`
- [ ] Initialize animation in `ChestSystem` on open
- [ ] Add `GetChestSource()` to `TileAtlas`
- [ ] Replace DrawRectangle with DrawTexturePro in `ItemRenderSystem`
- [ ] Add FOV check to chest rendering

## Files

| File | Action | Phase |
|------|--------|-------|
| src/ECS/Rendering/TextureManager.cs | CREATE | 1 |
| src/Dungeon/TileMap.cs | MODIFY | 1 |
| src/Dungeon/Generation/AutotileComputer.cs | CREATE | 1 |
| src/Core/Config/GameConfig.Sprites.cs | CREATE | 1 |
| src/Core/GameContext.cs | MODIFY | 1 |
| src/Core/Game.cs | MODIFY | 1 |
| src/ECS/Rendering/TileAtlas.cs | CREATE | 2 |
| src/ECS/Rendering/TileRenderSystem.cs | REWRITE | 2 |
| src/Dungeon/Generation/DecorationPainter.cs | MODIFY | 3 |
| src/Dungeon/DungeonGenerator.cs | MODIFY | 1,3 |
| src/ECS/Rendering/DecorationObjectType.cs | CREATE | 5 |
| src/ECS/Rendering/DecorationObject.cs | CREATE | 5 |
| src/ECS/Items/DecorationObjectSpawner.cs | CREATE | 5 |
| src/ECS/Rendering/DecorationObjectRenderSystem.cs | CREATE | 5 |
| src/Core/ServiceRegistration.cs | MODIFY | 5 |
| src/ECS/Items/Components/Chest.cs | MODIFY | 6 |
| src/ECS/Items/Systems/ChestSystem.cs | MODIFY | 6 |
| src/ECS/Items/Systems/ItemRenderSystem.cs | MODIFY | 6 |
| src/ECS/Exploration/FloorTransitionSystem.cs | MODIFY | 5 |
