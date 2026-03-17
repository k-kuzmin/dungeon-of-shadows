# Architecture: Phase 4 Items

> Last updated: 2026-03-17 | Status: final | Platform: Generic

## Chosen Approach
Pragmatic balance.

Rationale: best trade-off for full Phase 4 scope now (4.1-4.7), keeps ECS/DI conventions, and preserves low-allocation constraints on hot paths.

## Performance Profile
| Component | Hot Path | Allocs/call | Strategy |
|-----------|----------|-------------|----------|
| ItemPickupSystem | yes | 0 | QueryInto + reusable List<int> + manual nearest loop |
| ItemUseSystem | yes | 0 | type-keyed cooldown arrays/maps reused across ticks |
| ItemRenderSystem | yes | 0 | static color/lookup tables + reusable query buffers |
| LootDropSystem | no | pooled/rare | deterministic RNG and event-driven spawn |
| ChestSystem | no | pooled/rare | interaction only on key press in range |
| InventoryRenderSystem | no | bounded | modal UI allocations are acceptable (not hot path) |
| QuickSlotsHudRender | yes | low | lightweight label/cooldown formatting; no LINQ/new collections |

## Components
### Item Definition Layer
- **File**: `src/ECS/Items/ItemDefinition.cs`
- **Responsibility**: immutable item template from JSON/built-in data
- **Hot path**: no
- **Interfaces**: ItemId, ItemType, Rarity, base stats, effect id

### Inventory Component
- **File**: `src/ECS/Items/Components/Inventory.cs`
- **Responsibility**: slot-limited inventory and stack state
- **Hot path**: yes
- **Interfaces**: fixed-capacity slots, stack merge (potions/scrolls up to 10)

### Equipment Component
- **File**: `src/ECS/Items/Components/Equipment.cs`
- **Responsibility**: equipped gear (Weapon/Armor/Amulet/Ring1/Ring2)
- **Hot path**: yes
- **Interfaces**: equip/replace and emit drop of replaced item

### Quick Slots Component
- **File**: `src/ECS/Items/Components/QuickSlots.cs`
- **Responsibility**: 4 quick slots bound to item type
- **Hot path**: yes
- **Interfaces**: per-type cooldown tracking

### Ground Item + Chest Components
- **File**: `src/ECS/Items/Components/ItemOnGround.cs`
- **Responsibility**: world pickup entity and chest loot entities
- **Hot path**: yes
- **Interfaces**: pickup radius checks, render marker data

## Data Flow
Enemy death -> LootDropSystem rolls deterministic drop by enemy type -> spawn item entity on floor.
Player presses E -> ItemPickupSystem finds nearest item in radius -> if gear: auto-equip and drop replaced gear, else stack/add or show "max" text.
Inventory open (I) -> InventoryRenderSystem handles drag/drop, compare, tooltip, equip/unequip.
Quick slot 1-4 -> ItemUseSystem validates per-type cooldown -> apply potion/scroll effect -> consume stack.
Chest interaction -> ChestSystem opens chest entity and spawns guaranteed loot entities.

## Key Decisions
| Decision | Choice | Rationale |
|----------|--------|-----------|
| Architecture style | Pragmatic | full scope with manageable complexity |
| RNG | Deterministic from seed | reproducible generation |
| Stacks | potions/scrolls only, max 10 | clear UX and memory bounds |
| Full inventory pickup | gear replaces; others denied with text | matches requested behavior |
| Rings | 2 slots | requested |
| Quick slot binding | by item type | requested |
| Cooldown model | per item type | requested |
| Legendary | procedural | requested |
| Chests | real entities | requested |

## Changed Decisions During Implementation
- Quick slot HUD rendering kept inside `InventoryRenderSystem` screen pass instead of separate renderer.
- Non-hot inventory UI paths allow bounded temporary string formatting for readability.

## Constraints & Conventions
- DI registration in `src/Core/ServiceRegistration.cs`; order is behavior.
- Systems communicate through context/components/events, not direct system calls.
- Components are data-only structs in feature folders.
- Keep tunables in `src/Core/Config/GameConfig.Items.cs`.
- Comments/docs in Russian.

## Performance Rules
- No allocations in: `ItemPickupSystem.Tick`, `ItemUseSystem.Tick`, `ItemRenderSystem.Tick`, quick-slot HUD render tick.
- Use pool/event queue for: loot spawn requests, floating UI messages.
- Value types preferred for: item instances, stack entries, cooldown entries, event payloads.
- No LINQ and no per-frame string interpolation on hot paths.
- Cache query buffers (`List<int>` fields), cache lookups and UI text where possible.

## Out of Scope
- Save/load persistence.
- External economy/shop systems.
- Network/multiplayer sync.
