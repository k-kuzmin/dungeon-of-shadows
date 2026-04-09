# Architecture: Spell System Rework
> Last updated: 2026-04-09 | Status: final

## Chosen Approach — Pragmatic + SpellSlot sub-struct

Managed array `SpellSlot[]` внутри `SpellSlots` (прецедент: `Inventory.Slots`).
SelectionModal на UiContext для замены заклинаний. Lambda callbacks. 0 новых ECS систем.

**Обоснование:**
- Inline 8 полей (Minimal) — нельзя расширить без перекомпиляции, копипаста switch
- Отдельный SpellBook + 2 системы (Clean) — избыточно для текущего масштаба
- Managed array — один heap-аллок при спавне, zero-alloc на hot path, динамический размер

## Performance Profile

| Component | Hot Path | Allocs/call | Strategy |
|-----------|----------|-------------|----------|
| SpellSlots.GetSlot/SetSlot | YES (каждый кадр) | 0 | Array index |
| SpellSlots.HasSpellId | YES (при касте) | 0 | Linear scan, max 8 |
| SpellInputSystem.Tick | YES | 0 | _buffer reused |
| MagicHudRenderSystem.Tick | YES | 0 | Fixed [MaxSlots] cache arrays, SpellId enum cached |
| SpellCastSystem.CalcDamage | YES | 0 | Arithmetic |
| SelectionModal open | NO (per item use) | ~3 | string[], lambda closure |
| SpellSlots construction | NO (player spawn) | 1 | new SpellSlot[capacity] |

## Components

### SpellSlot (NEW)
- **File:** `src/ECS/Magic/Components/SpellSlot.cs`
- **Responsibility:** Value-type хранение SpellId + Level для одного слота
- **Hot path:** YES (sub-value внутри SpellSlots)
- **Interfaces:** none (struct)

### SpellSlots (REWRITE)
- **File:** `src/ECS/Magic/Components/SpellSlots.cs`
- **Responsibility:** Managed array слотов, ActiveSlotIndex, CastCooldown, Capacity
- **Hot path:** YES
- **Interfaces:** none (struct component)

### SpellCastRequest (MODIFY)
- **File:** `src/ECS/Magic/Components/SpellCastRequest.cs`
- **Change:** добавлен `int SpellLevel`

### LearnResult (NEW)
- **File:** `src/ECS/Magic/LearnResult.cs`
- **Values:** Learned, Upgraded, AlreadyMaxLevel, SlotsFull

### SelectionModalDescriptor (NEW)
- **File:** `src/UI/SelectionModalDescriptor.cs`
- **Responsibility:** Данные модалки с N вариантами выбора

## Data Flow

### Learn (свободный слот есть)
```
ItemUseSystem.HandleSpellScroll
  → MagicHelper.TryLearnOrUpgrade → Learned
  → ConsumeAndClear → ShowMessage
```

### Auto-upgrade (заклинание уже известно, level < max)
```
MagicHelper.TryLearnOrUpgrade → Upgraded
  → ConsumeAndClear → ShowMessage("Spell Lv.N")
```

### Max level (level == max)
```
MagicHelper.CanUseSpellScroll → false
  → ShowMessage("Already max level"), НЕ потребляем
```

### Replace (все слоты заняты, новое заклинание)
```
MagicHelper.TryLearnOrUpgrade → SlotsFull
  → OpenReplaceModal → PushSelectionModal
  → [player picks via W/S + Enter]
  → OnConfirm: ReplaceSpell → ConsumeAndClear → ShowMessage
```

### Cast with level
```
SpellInputSystem → req.SpellLevel = slots.GetActiveLevel()
  → cooldown scaling для utility spells
SpellCastSystem.CalcDamage(def, level):
  attack spells: damage * (1 + (level-1) * DamageBonus)
CastHeal:
  heal * (1 + (level-1) * HealBonus)
```

### Slot unlock on floor descent
```
FloorLifecycleSystem.DescendFloor → TryExpandSpellSlots
  → if currentFloor >= threshold → slots.Expand(configCapacity)
  → ShowMessage("New spell slot!")
```

## Key Decisions

1. **Managed array vs inline fields** — managed array: динамический размер, один аллок при спавне
2. **SpellSlot sub-struct** — SpellId+Level в одном value type
3. **Нет нового компонента SpellBook** — уровни прямо в SpellSlots.Slots[i].Level
4. **SelectionModal на UiContext** — переиспользуемый UI паттерн
5. **Lambda callbacks в модалке** — один аллок на открытие
6. **Отдельные config для heal/damage scaling** — SpellLevelHealBonus / SpellLevelDamageBonus
7. **Cooldown overlay на всех слотах** — CastCooldown общий, отображается на каждом слоте
8. **SpellId enum cached в HudRender** — zero TryGet на hot path

## Constraints & Conventions
- Zero-alloc на Tick/Draw (managed array аллоцируется один раз при спавне)
- Системы не вызывают друг друга — коммуникация через GameContext/компоненты
- Все magic numbers в GameConfig.Magic.cs / GameConfig.Ui.cs
- ref var access для in-place mutation компонентов
- Нет LINQ, нет string concat на hot path

## Performance Rules
- `SpellSlots.Slots[]` — array index, zero alloc
- `SpellSlot.Empty` — static readonly field, не property
- `MagicHudRenderSystem` — cache arrays фиксированного размера [MaxSlots], SpellId enum cached
- `SelectionModal.Draw` — все данные из descriptor, textCache.Measure с кешем
- `Expand()` — re-alloc только при переходе на новый этаж (не hot path)

## Out of Scope
- Пассивная книга заклинаний (знать больше чем экипировано)
- Per-spell cooldown (один CastCooldown на все слоты — как сейчас)
- Визуальные эффекты при апгрейде заклинания
- Tooltip на спеллах в HUD
