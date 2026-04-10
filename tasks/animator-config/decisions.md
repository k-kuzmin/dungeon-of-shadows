# Decisions: Система конфигурации аниматоров

## Clarifications (Phase 1)
### Формат конфигурации
- **Question**: JSON или код?
- **Answer**: JSON (как spells.json), если парсинг не утяжелит загрузку
- **Impact**: Нужен JSON-парсер + JsonContext для AOT, загрузка при Start()

### Спрайтшиты проджектайлов
- **Question**: Layout и размеры?
- **Answer**: Горизонтальная полоса 16×16 кадров. fireball.png — 8 кадров, magic_bolt.png — 5 кадров
- **Impact**: Layout "horizontal_strip" с настраиваемым frameWidth/frameHeight

### Остальные заклинания
- **Question**: Спрайты для Chain Lightning, Frost Nova и др.?
- **Answer**: Пока оставляем цветные круги
- **Impact**: SpellRenderSystem должен поддерживать fallback на круги для projectiles без Animation

### Масштаб миграции
- **Question**: Все аниматоры сразу или поэтапно?
- **Answer**: Мигрируем все сразу
- **Impact**: Герой, враги, факелы, сундуки — все переводятся на JSON-конфигурацию

## Architecture (Phase 4)
### Approach Selection
- **Options considered**: Minimal / Clean / Pragmatic
- **Chosen**: Pragmatic (подход 3)
- **Rationale**: Единый JSON, 4 типа аниматоров, полная миграция
- **Decided by**: user

## Implementation (Phase 5)
### AnimatorFactory.Build dispatch
- **Context**: "atlas" и "multi" типы имели идентичную логику
- **Decision**: Объединены в единый BuildNamedClips, directional отдельно
- **Rationale**: DRY — одинаковый код не дублируется
- **Date**: 2026-04-05

## Deferred (Phase 6)
### LoadFallback дублирует JSON
- **Issue**: AnimatorDatabase.LoadFallback() содержит копию animations.json в C#
- **Location**: src/ECS/Rendering/AnimatorDatabase.cs:43-93
- **Decision**: proceed as-is
- **Reason**: Паттерн проекта (SpellDatabase делает то же). Обеспечивает работу без JSON файла.

### Rotation в Rendering/ вместо Magic/Components/
- **Issue**: Rotation component размещён в src/ECS/Rendering/, хотя используется для снарядов
- **Location**: src/ECS/Rendering/Rotation.cs
- **Decision**: proceed as-is
- **Reason**: Rendering-концепт (угол поворота спрайта), потребляется YSortedRenderSystem

### Pre-existing: HashSet alloc per floor, AllEntities boxing
- **Issue**: FloorLifecycleSystem аллоцирует HashSet на каждый этаж, foreach по AllEntities боксит enumerator
- **Location**: src/ECS/Exploration/FloorLifecycleSystem.cs:116, :70
- **Decision**: proceed as-is
- **Reason**: Pre-existing код, не введено этой фичей. Spawn-only path.

### Pre-existing: SpellRenderSystem buffer capacities
- **Issue**: Буферы List<int> без начальной ёмкости
- **Location**: src/ECS/Magic/Systems/SpellRenderSystem.cs:16-18
- **Decision**: fix now
- **Reason**: Нарушает 0 allocs/call на hot path. Исправлено добавлением capacity.
