# Context: Spell System Rework
> status: done
> current_phase: 7
> last_updated: 2026-04-09

## Current Phase — Phase 7: Complete
## Last Action — Two rounds of code review, all fixes applied, runtime verified
## Next Action — N/A

## Requirements
1. **Dynamic spell slots** — начинаем с 3, разблокируются по мере прогресса ✓
2. **Spell levels via duplicate scrolls** — повторный свиток = повышение уровня ✓
3. **Spell replacement UI** — модалка выбора при полных слотах ✓
4. **Auto-upgrade known spells** — автоматический апгрейд ✓
5. **Minimal hot-path allocations** — zero alloc на tick/cast ✓
6. **Scope** — магия, предметы, UI, HUD ✓

## Files Modified
- `src/ECS/Magic/Components/SpellSlot.cs` — NEW
- `src/ECS/Magic/Components/SpellSlots.cs` — REWRITE
- `src/ECS/Magic/Components/SpellCastRequest.cs` — MODIFY
- `src/ECS/Magic/LearnResult.cs` — NEW
- `src/ECS/Magic/SpellDefinition.cs` — MODIFY
- `src/ECS/Magic/MagicHelper.cs` — REWRITE
- `src/ECS/Magic/Systems/SpellInputSystem.cs` — MODIFY
- `src/ECS/Magic/Systems/SpellCastSystem.cs` — MODIFY
- `src/ECS/Magic/Systems/MagicHudRenderSystem.cs` — REWRITE
- `src/ECS/Items/Systems/ItemUseSystem.cs` — REWRITE TeachSpell handling
- `src/ECS/Items/Systems/InventoryRenderSystem.cs` — MODIFY
- `src/ECS/Exploration/FloorLifecycleSystem.cs` — MODIFY
- `src/Core/Config/GameConfig.Magic.cs` — MODIFY
- `src/Core/Config/GameConfig.Ui.cs` — MODIFY
- `src/UI/SelectionModalDescriptor.cs` — NEW
- `src/UI/UiSelectionModal.cs` — NEW
- `src/UI/UiContext.cs` — MODIFY
- `src/UI/UiInputSystem.cs` — MODIFY
