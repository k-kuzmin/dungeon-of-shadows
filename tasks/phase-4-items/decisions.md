# Decisions: Phase 4 Items

## Clarifications (Phase 3)

### Scope and core rules
- **Question**: Full scope or partial for this iteration?
- **Answer**: Full Phase 4.1-4.7.
- **Impact**: Design and implementation include items, rarity, drops, inventory/equipment, quick slots, potions, chests.

### Inventory capacity and stacking
- **Question**: How is inventory limited and what stacks?
- **Answer**: Limit only by slot count; potions and scrolls stack up to 10; ammunition auto-equips and old gear is dropped.
- **Impact**: Inventory model uses slot cap + stack logic by item type; gear replacement flow required.

### Full inventory behavior
- **Question**: What happens on pickup when inventory is full?
- **Answer**: Gear is replaced; non-gear is not picked up and show max-capacity text.
- **Impact**: Pickup system needs type-based branching and message output.

### Equipment slots
- **Question**: Ring slots count?
- **Answer**: Two ring slots.
- **Impact**: Equipment component/UI must expose two ring entries.

### Unequip behavior
- **Question**: What if no free inventory slot on unequip?
- **Answer**: Drop to floor.
- **Impact**: Unequip always succeeds; spawns floor item entity.

### Quick slots binding
- **Question**: Quick slots bound to item instance or type?
- **Answer**: Item type.
- **Impact**: Quick slot data stores ItemId/type key, not entity/item instance id.

### Cooldowns
- **Question**: Shared or separate cooldowns?
- **Answer**: Separate by item type.
- **Impact**: Cooldown store keyed by consumable type.

### Scrolls
- **Question**: Placeholder or full effects?
- **Answer**: Full effects.
- **Impact**: Item use system must execute real scroll effect handlers.

### Legendary generation
- **Question**: Fixed unique or procedural legendary?
- **Answer**: Procedural.
- **Impact**: Rarity pipeline includes procedural bonus/effect roll for legendary.

### RNG determinism
- **Question**: Deterministic from seed?
- **Answer**: Deterministic.
- **Impact**: Use seeded RNG path from dungeon/game seed for item generation/drop/bonuses.

### Enemy drops
- **Question**: Guaranteed or enemy-type-based probability?
- **Answer**: Probability by enemy type.
- **Impact**: Drop table keyed by enemy type.

### Chests
- **Question**: Simulated or real chest entities?
- **Answer**: Real entities.
- **Impact**: Add chest component/system and guaranteed loot interaction.

### Pickup input
- **Question**: Pickup underfoot or nearest in radius?
- **Answer**: Nearest in radius.
- **Impact**: Pickup query finds nearest eligible item around player.

### Inventory UX level
- **Question**: Minimal or max UX?
- **Answer**: Maximum.
- **Impact**: Inventory UI includes rich interactions (compare, drag/drop, tooltip).

### Performance constraints
- **Question**: Strict no-LINQ/no-allocation on hot paths?
- **Answer**: Confirmed.
- **Impact**: Tick/render loops must avoid LINQ and avoid heap allocations except state-change events.

## Architecture (Phase 4)

### Approach Selection
- **Options considered**: Minimal / Clean / Pragmatic
- **Chosen**: Pragmatic (3)
- **Rationale**: Balances delivery speed and maintainability for full 4.1-4.7 scope while preserving strict hot-path performance constraints and existing ECS conventions.
- **Decided by**: user

## Implementation (Phase 5)

### Review-driven fixes
- **Context**: Phase 6 found high/medium issues in item UI and performance paths.
- **Decision**: Applied all high and medium issues immediately.
- **Rationale**: Keep behavior consistent and avoid carrying correctness/perf debt.
- **Date**: 2026-03-17 12:14:01+03:00

## Deferred (Phase 6)

### None
- **Issue**: No deferred issues after applying High + Medium fixes.
- **Location**: n/a
- **Decision**: fixed now
- **Reason**: all identified issues were in scope and resolved.
