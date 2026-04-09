# Decisions: Spell System Rework

## Clarifications (Phase 3)

### Spell Level Scaling
- **Question:** Что скейлится с уровнем?
- **Answer:** Атакующие заклинания — урон. Утилитарные (Heal, Shadow Step) — уменьшение кулдауна.
- **Impact:** Нужен per-spell флаг или вывод из типа заклинания (damage > 0 → scale damage, else → scale cooldown)

### Max Spell Level
- **Question:** Максимальный уровень?
- **Answer:** 5
- **Impact:** Константа в GameConfig.Magic, влияет на UI (отображение уровня) и формулу скейлинга

### Slot Unlock Mechanism
- **Question:** Как разблокируются слоты?
- **Answer:** По номеру этажа
- **Impact:** FloorLifecycleSystem или отдельная система проверяет CurrentFloor и обновляет MaxSlots в SpellSlots

### Replacement UI Format
- **Question:** Модалка или HUD-клик?
- **Answer:** Модальное окно со списком текущих заклинаний + кнопки замены
- **Impact:** Расширение UiModal или новый SpellReplaceModal, блокировка input через UiContext

### Max Level Scroll Handling
- **Question:** Что делать со свитком если заклинание уже макс уровня?
- **Answer:** Не применять (выкинуть / не использовать)
- **Impact:** CanLearnSpell/CanUpgradeSpell должен проверять Level < MaxLevel

## Architecture (Phase 4)

### Approach — Pragmatic + SpellSlot sub-struct
- **Options:** Inline 8 полей / SpellBook + 2 системы / Managed array
- **Chosen:** Managed array
- **Rationale:** Один heap-аллок при спавне, zero-alloc hot path, динамический размер

## Implementation (Phase 5)

### Heal scaling — отдельная константа
- **Context:** Code review выявил что CastHeal использовал SpellLevelDamageBonus
- **Decision:** Добавлен SpellLevelHealBonus в GameConfig.Magic
- **Rationale:** Развязка тюнинга heal и damage scaling
- **Date:** 2026-04-09

### Slot unlock capacities — вынесены в config
- **Context:** Code review: хардкод 4/5 в TryExpandSpellSlots
- **Decision:** SpellSlotCapacityAtFloor4/7 в GameConfig.Magic, else if → два независимых if
- **Rationale:** Нет magic numbers в системах + корректная работа при пропуске этажей
- **Date:** 2026-04-09

### Cooldown overlay — на всех слотах
- **Context:** Code review: CastCooldown общий, overlay только на active слоте
- **Decision:** Убрана проверка isActive для cooldown overlay
- **Rationale:** UX — игрок видит кулдаун на всех слотах после переключения
- **Date:** 2026-04-09

## Deferred (Phase 6)

### Managed array vs InlineArray
- **Description:** SpellSlot[] heap-аллокация в struct создаёт aliasing risk при копировании
- **Location:** SpellSlots.cs
- **Decision:** Оставить as-is
- **Reason:** Явное архитектурное решение, все call sites используют ref, InlineArray требует compile-time const размер что конфликтует с конфигурируемым MaxCapacity
