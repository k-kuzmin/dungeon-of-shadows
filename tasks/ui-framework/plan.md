# Plan: UI Framework (Pragmatic Balance)

## Phase 1 — Foundation (no behavior change)
- [x] Create `src/UI/UiRect.cs` — readonly struct with Contains, ToRaylibRect
- [x] Create `src/UI/FlexDirection.cs` — enum Row/Column
- [x] Create `src/UI/UiLayout.cs` — ref struct layout builder (Row, Column, Take, TakeSquare, Skip, Nested)
- [x] Create `src/UI/UiTheme.cs` — all colors + font sizes, RarityColor, SpellColor, HpInterpolated
- [x] Create `src/UI/TextMeasureCache.cs` — Dictionary-based MeasureText cache
- [x] Create `src/UI/UiContext.cs` — InputConsumed + ModalDescriptor + modal stack
- [x] Create `src/UI/UiDraw.cs` — all static draw helpers (Panel, ProgressBar, Slot, Label, Tooltip, ItemIcon)
- [x] Create `src/UI/UiInputSystem.cs` — ITickable, ClearFrame + modal keys
- [x] Create `src/Core/Config/GameConfig.Ui.cs` — все UI magic numbers из 3 систем
- [x] Register in ServiceRegistration: UiTheme, TextMeasureCache, UiContext (singletons) + UiInputSystem (before InventoryInputSystem)
- [x] Add InputConsumed gates to CombatInputSystem and SpellInputSystem
- [x] `dotnet build` passes

## Phase 2 — Migrate HudRenderSystem
- [x] Add UiTheme, TextMeasureCache to constructor
- [x] Replace HP bar drawing → UiLayout.Column + UiDraw.ProgressBar + LabelCenteredCached
- [x] Replace MP bar drawing → same pattern
- [x] Replace floor label → UiDraw.Label
- [x] Move all Color fields → UiTheme.*
- [x] Replace magic numbers → GameConfig.Ui.*
- [x] Replace MeasureText calls → TextMeasureCache
- [x] `dotnet build` passes

## Phase 3 — Migrate MagicHudRenderSystem
- [x] Add UiTheme, TextMeasureCache to constructor
- [x] Replace slot loop → UiLayout.Row + UiDraw per slot
- [x] Replace cooldown overlay → UiDraw.ProgressBarVertical
- [x] Remove GetSpellColor → UiTheme.SpellColor
- [x] Replace MeasureText patterns → TextMeasureCache / LabelCenteredCached
- [x] Replace magic numbers → GameConfig.Ui.*
- [x] `dotnet build` passes

## Phase 4 — Migrate InventoryRenderSystem
- [x] Add UiTheme, TextMeasureCache, UiContext to constructor
- [x] Refactor DrawQuickSlots → UiLayout.Row + UiDraw.Slot
- [x] Refactor inventory grid → UiLayout per row + UiRect.Contains for hit-test
- [x] Refactor equipment slots → UiRect.Contains
- [x] Replace DrawItemIcon → UiDraw.ItemIcon
- [x] Replace GetRarityColor → UiTheme.RarityColor
- [x] Replace PointInRect → UiRect.Contains
- [x] Replace inline new Color(...) → UiTheme.*
- [x] Cache tooltip strings on hover change (not every frame)
- [x] All clicks set _uiCtx.InputConsumed = true
- [x] Replace panel/tooltip drawing → UiDraw.Panel / UiDraw.Tooltip
- [x] `dotnet build` passes

## Phase 5 — Modal support
- [x] Create UiModal.Draw static helper
- [x] Wire modal rendering from InventoryRenderSystem
- [x] UiInputSystem handles Esc/Enter for modals (pop before callback)
- [x] `dotnet build` passes

## Phase 6 — Cleanup + Quality Review
- [x] Fix: PopModal before callback (re-entrant safety)
- [x] Fix: Guard HandleInventoryInput against InputConsumed/HasModal
- [x] Fix: Use playerId directly for Mana query (avoid redundant QueryInto)
- [x] Fix: Cache quantity strings, cooldown text, quick slot labels (zero alloc hot path)
- [x] Fix: InputConsumed gate for Z/X/wheel in SpellInputSystem
- [x] `dotnet build` clean (0 warnings, 0 errors)

## Files Table

| File | Status | Phase |
|------|--------|-------|
| `src/UI/UiRect.cs` | ✅ Created | 1 |
| `src/UI/FlexDirection.cs` | ✅ Created | 1 |
| `src/UI/UiLayout.cs` | ✅ Created | 1 |
| `src/UI/UiTheme.cs` | ✅ Created | 1 |
| `src/UI/TextMeasureCache.cs` | ✅ Created | 1 |
| `src/UI/UiContext.cs` | ✅ Created | 1 |
| `src/UI/UiDraw.cs` | ✅ Created | 1 |
| `src/UI/UiInputSystem.cs` | ✅ Created | 1 |
| `src/UI/UiModal.cs` | ✅ Created | 5 |
| `src/Core/Config/GameConfig.Ui.cs` | ✅ Created | 1 |
| `src/Core/ServiceRegistration.cs` | ✅ Modified | 1 |
| `src/ECS/Combat/Systems/CombatInputSystem.cs` | ✅ Modified | 1 |
| `src/ECS/Magic/Systems/SpellInputSystem.cs` | ✅ Modified | 1 |
| `src/ECS/Rendering/HudRenderSystem.cs` | ✅ Modified | 2 |
| `src/ECS/Magic/Systems/MagicHudRenderSystem.cs` | ✅ Modified | 3 |
| `src/ECS/Items/Systems/InventoryRenderSystem.cs` | ✅ Modified | 4 |
