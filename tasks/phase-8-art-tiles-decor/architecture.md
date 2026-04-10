# Architecture: Phase 8 Art — Tiles & Decor (8.1 + 8.2)

> Last updated: 2026-03-17 | Status: final | Platform: Generic (C# 12 + Raylib-cs + NativeAOT)

## Chosen Approach
Layered Texture System с baked autotile indices. Автотайл вычисляется при генерации, не per-frame.

## Performance Profile
| Component | Hot Path | Allocs/call | Strategy |
|-----------|----------|-------------|----------|
| TileRenderSystem (rewrite) | yes | 0 | Texture2D cached in locals, static Rectangle[] lookups |
| TileAtlas (static) | yes | 0 | static readonly Rectangle[] arrays |
| AutotileComputer (static) | no | 0 | Runs once per floor generation |
| TextureManager (singleton) | lookup: yes | 0 | Dictionary<string,Texture2D>, cached in locals per Tick |
| DecorationObjectRenderSystem | yes | 0 | Reusable List<int> buffer |
| Chest animation fields | no | 0 | Fields in existing struct |

## Components

### TextureManager — `src/ECS/Rendering/TextureManager.cs` (NEW)
- Singleton on GameContext (not DI)
- `LoadAll()` / `UnloadAll()` / `Get(string key) → Texture2D`
- Keys: "walls_floor", "objects", "doors_chest", "fire_large", "fire_small", "cracks_floor", "cracks_walls"
- Hot path: lookup only, zero alloc (returns Texture2D struct by value)

### TileAtlas — `src/ECS/Rendering/TileAtlas.cs` (NEW)
- Static class, no instances
- `GetWallSource(byte autotileIndex) → Rectangle`
- `GetFloorSource(byte floorVariant) → Rectangle`
- `GetStairSource() → Rectangle`
- `GetCrackSource(byte overlayIndex, bool isWall) → Rectangle`
- `GetTorchSource(byte frame) → Rectangle`
- `GetChestSource(byte frame, bool opened) → Rectangle`
- `GetDecoObjectSource(int col, int row) → Rectangle`
- Internal: `static readonly Rectangle[]` arrays populated in static constructor

### AutotileComputer — `src/Dungeon/Generation/AutotileComputer.cs` (NEW)
- Static class
- `ComputeAll(TileMap map, Random rng)` — writes AutotileIndex, FloorVariant, OverlayIndex
- `static readonly byte[] _blobLookup = new byte[256]` — 8-bit → 47 blob index
- Bitmask bits: N=1, NE=2, E=4, SE=8, S=16, SW=32, W=64, NW=128
- Corner masking: corner zeroed if adjacent cardinals not both walls

### Tile struct (MODIFY) — `src/Dungeon/TileMap.cs`
- Add: `byte AutotileIndex` (0-46), `byte FloorVariant` (0-N), `byte OverlayIndex` (0=none)

### TileMap (MODIFY) — `src/Dungeon/TileMap.cs`
- Add: `float[] TileAnimTimers`, `byte[] TileAnimFrames` (indexed by y*Width+x)
- Only meaningful for Torch tiles

### DecorationType (MODIFY) — `src/Dungeon/TileMap.cs`
- Split `Crack` → `CrackFloor`, `CrackWall`

### DecorationObject — `src/ECS/Rendering/DecorationObject.cs` (NEW)
- Struct: `DecorationObjectType Type`, `int AtlasCol`, `int AtlasRow`, `int SrcWidth`, `int SrcHeight`

### DecorationObjectType — `src/ECS/Rendering/DecorationObjectType.cs` (NEW)
- Enum: Barrel, BarrelBroken, Crate, CrateStack, Sack, Rock, RockSmall

### DecorationObjectSpawner — `src/ECS/Items/DecorationObjectSpawner.cs` (NEW)
- Static class, `SpawnObjects(World, TileMap, GameConfig, int floor, int seed)`
- Creates ECS entities: Position + DecorationObject
- Sets `map.Tiles[tx,ty].Walkable = false`

### DecorationObjectRenderSystem — `src/ECS/Rendering/DecorationObjectRenderSystem.cs` (NEW)
- IRenderTickable, RenderPhase.World
- FOV check per entity, DrawTexturePro with scale

### Chest (MODIFY) — `src/ECS/Items/Components/Chest.cs`
- Add: `float AnimTimer`, `byte AnimFrame`, `bool AnimDone`

### GameConfig.Sprites — `src/Core/Config/GameConfig.Sprites.cs` (NEW)
- TorchFrameCount, TorchFrameDuration, ChestOpenFrameCount, ChestOpenFrameDuration
- FloorVariantCount, DecoBarrelChance, DecoCrateChance, DecoSackChance

## Data Flow

### Generation (once per floor)
```
DungeonGenerator.TryGenerate()
  → DecorationPainter.Paint()         writes DecorationType[]
  → AutotileComputer.ComputeAll()     writes Tile.AutotileIndex/FloorVariant/OverlayIndex

Game.Init() / FloorTransitionSystem.DescendFloor()
  → DecorationObjectSpawner.SpawnObjects()  creates entities, marks tiles non-walkable
```

### Per-frame render
```
TileRenderSystem.Tick(dt)
  cache textures in locals
  frustum loop:
    Wall → DrawTexturePro(wallsTex, TileAtlas.GetWallSource(autotileIndex))
    Floor → DrawTexturePro(wallsTex, TileAtlas.GetFloorSource(floorVariant))
    Overlay → DrawTexturePro(crackTex, TileAtlas.GetCrackSource(overlayIndex))
    Torch → advance timer/frame, DrawTexturePro(fireTex, TileAtlas.GetTorchSource(frame))
    Explored → tint with darkened color

DecorationObjectRenderSystem.Tick(dt)
  QueryInto → FOV check → DrawTexturePro(objectsTex)

ItemRenderSystem.Tick(dt)
  Chest → advance AnimFrame → DrawTexturePro(chestTex) + FOV check
```

## Key Decisions
| Decision | Choice | Rationale |
|----------|--------|-----------|
| Autotile storage | Baked in Tile struct at generation | Zero per-frame neighbor reads |
| Blob method | 8-bit with corner masking → 47 tiles | Standard, beautiful corners |
| Decoration blocking | Tile.Walkable = false | Consistent with existing wall collision model |
| Torch animation storage | Parallel arrays on TileMap | Cache-coherent, no struct boxing |
| Texture access | TextureManager on GameContext | All systems get zero-copy access |

## Performance Rules
- No allocations in: TileRenderSystem.Tick, DecorationObjectRenderSystem.Tick, ItemRenderSystem.Tick
- Cache Texture2D in local at start of Tick, not per-tile lookup
- Use static readonly Rectangle[] for atlas lookups
- Torch animation: advance only if Visibility == 2 (in frustum AND visible)

## Out of Scope
- Doors (deferred)
- Water tiles and coast animation
- Trap animation
- Player/enemy sprite animation
- UI polish
