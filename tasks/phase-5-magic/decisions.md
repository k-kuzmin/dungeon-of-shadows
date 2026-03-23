# Decisions: Magic System

## Clarifications (Phase 3)

### Implementation Order
- **Question**: В каком порядке реализовывать?
- **Answer**: Мана + Magic Bolt (базовый projectile) первыми, потом наращивать
- **Impact**: Инкрементальная сборка — каждый шаг тестируемый

### INT Stat
- **Question**: Есть ли INT в Stats?
- **Answer**: Нет, нужно добавить. Базовое значение 5 для игрока.
- **Impact**: Добавить поле INT в Stats struct, использовать в формулах магического урона

### Spell Scrolls as Items
- **Question**: Свитки заклинаний — новый ItemType или существующий Scroll?
- **Answer**: Новый тип (SpellScroll)
- **Impact**: Добавить SpellScroll в ItemType enum, новый EffectType для обучения заклинанию

### Spell Slots — Separate Panel
- **Question**: QuickSlots расширять или отдельная панель?
- **Answer**: Отдельный компонент SpellSlots (вариант A). QuickSlots остаются для расходников.
- **Impact**: Новый компонент SpellSlots, клавиши ПКМ (каст) + Q/E (переключение) + колесо мыши

### Projectile-Enemy Collision
- **Question**: Radius check или AABB коллизия?
- **Answer**: AABB коллизия (entity-to-entity)
- **Impact**: ProjectileSystem проверяет center-vs-AABB перекрытие с Collider врагов

### Projectile Behavior
- **Question**: Pierce / explode behavior?
- **Answer**: Magic Bolt уничтожается при первом попадании. Fireball взрывается при столкновении (враг ИЛИ стена).
- **Impact**: Projectile компонент имеет флаг IsAoe

### Status Effect Stacking
- **Question**: Поджог + замедление одновременно?
- **Answer**: Да, эффекты стакаются и накладываются друг на друга. Однотипные — refresh duration.
- **Impact**: StatusEffects хранит до 4 эффектов в inline слотах

### Visuals
- **Question**: Спрайты для заклинаний?
- **Answer**: Спрайтов пока нет, рисуем цветными примитивами
- **Impact**: DrawCircle/DrawCircleLines с цветовой кодировкой по типу заклинания

## Architecture (Phase 4)
### Approach Selection
- **Options considered**: Minimal / Clean / Pragmatic
- **Chosen**: Pragmatic Balance
- **Rationale**: Баланс между минимальным diff и расширяемостью. SpellDatabase через JSON, раздельные input/cast системы, inline StatusEffects, SlowDebuff через PhysicsSystem.
- **Decided by**: user confirmation

### StatusEffects Storage
- **Context**: Нужен hot-path компонент для хранения активных эффектов
- **Decision**: Inline 4 слота (Effect0-3) в struct
- **Rationale**: Zero heap alloc, проще отладка чем InlineArray, 4 слота достаточно для 6 заклинаний

### Slow Implementation
- **Context**: Как замедлять врагов без coupling между системами
- **Decision**: SlowDebuff component + PhysicsSystem масштабирует velocity
- **Rationale**: Одна правка в одном месте (PhysicsSystem) вместо InputSystem + AISystem

### Projectile Movement
- **Context**: Кто двигает снаряды
- **Decision**: PhysicsSystem двигает (Position+Velocity без Collider), ProjectileSystem проверяет коллизии
- **Rationale**: Переиспользование существующего движения, ProjectileSystem только валидирует

### Spell Slot Switching Keys
- **Context**: Q/E конфликтует с E для взаимодействия (лестница, сундуки)
- **Decision**: Z/X вместо Q/E для переключения spell slots
- **Rationale**: E уже используется для взаимодействия на сцене
- **Decided by**: user

## Quality Review (Phase 6)

### Fixes Applied
- NullRef в MagicHudRenderSystem: cooldown overlay перенесён внутрь TryGet блока
- Дубликат SpawnAoEVisual/ApplyStatusEffect: вынесены в MagicHelper
- Namespace компонентов: .Magic → .Magic.Components
- Логика из компонентов: AddOrRefresh/RemoveAt/TryLearnSpell → MagicHelper
- StatusEffectSlot: вынесен в отдельный файл
- String concat на hot path: кеш строк в MagicHudRenderSystem
- HP bar div/0: добавлена защита MaxHP > 0
- Transient компоненты: очистка при переходе этажа
- Двойная валидация TeachSpell: CanLearnSpell + TryLearnSpell
- Dead field _chainHitBuffer: удалён из SpellCastSystem
- Dead code IsSelfTarget: удалён из SpellDefinition
- Unused SpellGlobalCooldown: удалён из GameConfig
- MeasureText hint: кеш ширины
- Кулдаун каста: перенесён из ManaSystem в SpellInputSystem

## Deferred
### SlowDebuff как derived cache
- **Issue**: SlowDebuff компонент является production кешем StatusEffects.Slow
- **Location**: StatusEffectSystem.cs:82-97
- **Decision**: proceed as-is
- **Reason**: Архитектурное решение — PhysicsSystem не зависит от StatusEffects напрямую

### JSON casing inconsistency
- **Issue**: items.json (camelCase) vs spells.json (PascalCase)
- **Decision**: fix later
- **Reason**: Оба работают через CaseInsensitive, косметическое несоответствие
