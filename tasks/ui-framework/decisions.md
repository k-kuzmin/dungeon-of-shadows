# Decisions: UI Framework

## Clarifications (Phase 1)

### Widget Scope
- **Question**: Минимальный или расширенный набор виджетов?
- **Answer**: Расширенный — Panel, ProgressBar, Slot, Label, Tooltip, Button (hover/click), ScrollList, Modal
- **Impact**: Нужна более продуманная иерархия виджетов и система событий

### Layout Approach
- **Question**: Якоря+отступы, flex-подобный, или data-driven?
- **Answer**: Flex-подобный (колонки/строки с auto-размерами)
- **Impact**: Нужен layout engine с поддержкой direction, gap, alignment, sizing

### Priority
- **Question**: Архитектурная чистота или визуальная консистентность первой?
- **Answer**: Оба параллельно
- **Impact**: Тема и layout разрабатываются одновременно как единый фреймворк

### Allocation Strategy
- **Question**: Насколько критична zero-allocation?
- **Answer**: Минимизировать насколько возможно
- **Impact**: Struct-based layout rects, кэширование метрик текста, пулинг где нужно, без LINQ на hot paths

## Clarifications (Phase 3)

### Input Routing
- **Question**: Вынести UI input в отдельный ITickable до gameplay систем?
- **Answer**: Да, согласен
- **Impact**: Создаём UiInputSystem : ITickable, регистрируем перед InputSystem. Рефакторим InventoryRenderSystem — убираем input логику из render pass. Добавляем InputConsumed флаг на GameContext.

### Migration Strategy
- **Question**: Мигрировать все 3 Screen-phase системы сразу или поэтапно?
- **Answer**: Сразу все
- **Impact**: Одна большая миграция HudRenderSystem + MagicHudRenderSystem + InventoryRenderSystem. Больше работы в одной итерации, но нет смешения подходов.

### Flex Layout Depth
- **Question**: Полный flex (nested, grow/shrink, wrap) или упрощённый?
- **Answer**: Упрощённый (direction + gap + align, без wrap/grow)
- **Impact**: ~150 строк layout engine. Покрывает все текущие use cases. Можно расширить позже.

### ScrollList
- **Question**: Нужен ли ScrollList сейчас?
- **Answer**: Нет, не нужен сейчас
- **Impact**: Убираем ScrollList из скоупа первой итерации. Можно добавить позже.

### Modal Windows
- **Question**: Какие сценарии модалок? Нужен ли стек?
- **Answer**: Выбросить/применить и прочее. Стек нужен.
- **Impact**: Реализуем modal stack — модалки могут открываться поверх друг друга. Модалка блокирует input для нижележащих слоёв. Сценарии: подтверждение действия, game over, пауза.

## Architecture (Phase 4)
### Approach Selection
- **Options considered**: Minimal (static helpers) / Clean Architecture (retained widget tree) / Pragmatic Balance (immediate-mode layout builder + draw helpers)
- **Chosen**: Pragmatic Balance
- **Rationale**: Решает все проблемы (дублирование, тема, layout, input routing, модалки) при умеренной сложности (~9 новых файлов, ~800-1000 строк). ref struct UiLayout даёт zero-alloc flex layout. Существующие системы рефакторятся in-place.
- **Decided by**: agent recommendation accepted

## Deferred (Phase 6)

### Input handling inside IRenderTickable (pre-existing)
- **Issue**: InventoryRenderSystem обрабатывает клики мыши и мутирует ECS внутри render phase
- **Location**: src/ECS/Items/Systems/InventoryRenderSystem.cs:HandleInventoryInput
- **Decision**: proceed as-is
- **Reason**: Pre-existing архитектурное решение, не введено этим PR. Рефакторинг в отдельную ITickable систему — отдельная задача.

### List<int> allocation in MeleeAttack (pre-existing)
- **Issue**: new List<int>() аллоцируется при каждой атаке мечом
- **Location**: src/ECS/Combat/Systems/CombatInputSystem.cs:97
- **Decision**: fix later
- **Reason**: Pre-existing, не связано с UI framework. Нужен переход на inline buffer или пул.

### Magic number 240 in inventory panel width (pre-existing)
- **Issue**: Дублирование магического числа 240 между расчётом panelW и equipment column X
- **Location**: src/ECS/Items/Systems/InventoryRenderSystem.cs:76,249
- **Decision**: fix later
- **Reason**: Pre-existing. Следует вынести в GameConfig при следующем рефакторинге инвентаря.
