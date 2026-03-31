# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Проект

Action Roguelike на C# 12 + Raylib-cs + NativeAOT (.NET 8). Пиксель-арт, процедурные подземелья, реалтайм-бой, permadeath.

## Сборка и запуск

```bash
dotnet build DungeonOfShadows.csproj
dotnet run --project DungeonOfShadows.csproj
```

NativeAOT публикация:
```bash
dotnet publish -c Release -r win-x64 -p:PublishAot=true
dotnet publish -c Release -r linux-x64 -p:PublishAot=true
dotnet publish -c Release -r osx-arm64 -p:PublishAot=true
```

- Если после крупного рефакторинга артефакты выглядят устаревшими — `dotnet clean` и пересборка.
- Автоматических тестов в проекте нет. Проверка — успешная сборка и ручной рантайм-чек.

## Архитектура

### Жизненный цикл систем

```
IStartable.Start()  →  ITickable.Tick(dt)  →  IDisposable.Dispose()
     ↑                       ↑                        ↑
  однократно            каждый кадр              при завершении
  после InitWindow      main loop               перед CloseWindow
```

`Game.Run()` — тонкий оркестратор, вызывает lifecycle-фазы по порядку. Никакой игровой логики в Game.

### DI-контейнер

Используется `Microsoft.Extensions.DependencyInjection`. Все сервисы регистрируются в `src/Core/ServiceRegistration.cs`.

**Добавление любой системы:**
```csharp
services.AddInterfaces<MyNewSystem>();
```
Одна строка — `AddInterfaces<T>()` автоматически детектит реализованные интерфейсы (`IStartable`, `ITickable`, `IRenderTickable`, `IDisposable`) и регистрирует всё нужное. Добавление `IDisposable` к системе **не требует** правки `ServiceRegistration`.

Порядок вызовов `AddSystem` = порядок Start/Tick/Draw/Dispose.

### IStartable

Однократная инициализация после сборки DI-контейнера:
```csharp
public interface IStartable { void Start(); }
```

### ITickable

Покадровое обновление в game loop:
```csharp
public interface ITickable { void Tick(float dt); }
```

### IRenderTickable

Рендер-подсистемы, вызываемые из `RenderSystem`:
```csharp
public interface IRenderTickable
{
    RenderPhase Phase { get; }  // World или Screen
    void Tick(float dt);
}
```
`RenderSystem` — обёртка, управляет `BeginDrawing/EndDrawing` и `BeginMode2D/EndMode2D`. World-space рисование — внутри `BeginMode2D/EndMode2D`; экранные элементы — в `RenderPhase.Screen`.

### IAssetProvider

Централизованная загрузка и доступ к ассетам по ключу:
```csharp
public interface IAssetProvider : IDisposable
{
    Texture2D GetTexture(string key);
}
```
`AssetProvider` реализует `IAssetProvider` + `IStartable`. Загружает текстуры при `Start()`, освобождает при `Dispose()`. Рендер-системы инжектят `IAssetProvider` напрямую через конструктор.

### FloorLifecycleSystem

Единственное место генерации уровня и спавна сущностей. Реализует `IStartable` (первый этаж) + `ITickable` (обработка переходов).

Поток данных:
- `FloorTransitionSystem` детектит E/Space на лестнице → ставит `_ctx.FloorTransitionRequested = true`
- `FloorLifecycleSystem.Tick()` обрабатывает флаг → очищает мир → генерирует этаж → спавнит сущности

### Принципы систем

- **Системы не вызывают друг друга** — общаются через данные (`GameContext`, компоненты, `DamageEvents`)
- **Исключение**: системы могут принимать другие системы через DI для вызова конкретных API (напр. `CameraSystem.TriggerShake()`)
- **Game.Run тикает все системы безусловно** — каждая система сама проверяет `GameState` если нужно
- **Рендер-подсистемы** регистрируются через `AddSystem` (автодетект `IRenderTickable`)

### ECS

Лёгкий ECS без внешних фреймворков:
- **Entity** — int ID
- **ComponentStore\<T\>** — генерик-хранилище. Новый компонент не требует правки `World`
- **World** — `Add<T>()`, `Get<T>()`, `Has<T>()`, `Remove<T>()`, `QueryInto<T>()`, `QueryInto<T1,T2>()`

**Добавление нового компонента:** просто создать struct и использовать `world.Add<MyComponent>(id, data)`.

### GameContext

Разделяемое мутабельное состояние игры. **Не содержит** `World` и `GameConfig` — они инжектятся напрямую через DI:
- `State` — текущее состояние игры (Playing, Paused, Menu, Dead)
- `Map` — текущая тайловая карта
- `Camera` — Raylib-камера
- `CurrentFloor`, `DungeonSeed` — параметры этажа
- `DebugMode`, `ShowFullMap`, `ShowInventory` — UI-флаги
- `FloorTransitionRequested` — флаг запроса перехода этажа
- `DamageEvents` — очередь событий урона
- `ItemDropRequests` — очередь запросов дропа
- `UiMessage`, `UiMessageTimer` — временные сообщения UI

### GameConfig

Все числовые параметры — в `GameConfig`. Не хардкодить магические числа в системах.

### DamageEvent очередь

Урон передаётся через `List<DamageEvent>` на `GameContext`:
- **Продьюсеры**: `MeleeAttackSystem`, `AISystem`, `ProjectileSystem`, `StatusEffectSystem` — пишут события
- **Консьюмер**: `HealthSystem` — читает, применяет урон, спавнит DamageFlash/DamageNumber, дренит список

### SpellDatabase

Загрузка заклинаний из JSON (`assets/data/spells.json`) с fallback на хардкод. Реализует `IStartable`. Используется системами каста для получения параметров заклинания по ID.

### MagicHelper

Статический класс с переиспользуемыми утилитами магической системы:
- `SpawnAoEVisual` — спавн визуала AoE-эффекта
- `ApplyStatusEffect` — добавление/обновление статус-эффекта на entity
- `AddOrRefreshEffect` / `RemoveEffectAt` — управление inline-слотами StatusEffects
- `CanLearnSpell` / `TryLearnSpell` — проверка/запись заклинания в SpellSlots

### SpellCastRequest — однокадровый запрос каста

Заклинания кастуются через компонент `SpellCastRequest` на entity игрока (не через очередь на GameContext):
- **Продьюсер**: `SpellInputSystem` — проверяет MP/кулдаун, добавляет компонент
- **Консьюмер**: `SpellCastSystem` — обрабатывает запрос, удаляет компонент в том же кадре

### UI Framework

Мини-фреймворк поверх Raylib для игрового интерфейса. Все файлы в `src/UI/`.

**Ключевые компоненты:**
- `UiRect` — readonly struct, screen-space bounding box для layout + hit-test + render
- `UiLayout` — ref struct (stack-only, zero alloc), flex-подобный layout cursor (Row/Column + gap)
- `UiDraw` — статические методы отрисовки (Panel, ProgressBar, Slot, Label, Tooltip, ItemIcon)
- `UiTheme` — singleton, единый источник всех цветов (`RarityColor`, `SpellColor`, `HpInterpolated`)
- `TextMeasureCache` — кэш `Raylib.MeasureText` по `(string, fontSize)`, устраняет повторные native-вызовы
- `UiContext` — `InputConsumed` флаг + modal stack (`PushModal`/`PopModal`)
- `UiInputSystem` — `ITickable`, сбрасывает per-frame state, обрабатывает modal Esc/Enter
- `UiModal` — статический хелпер отрисовки модальных окон

**Input routing:**
- `UiInputSystem` тикает ПЕРЕД всеми gameplay input системами
- `UiContext.InputConsumed` — ставится UI при кликах, проверяется `CombatInputSystem` и `SpellInputSystem`
- Modal → блокирует весь input через `InputConsumed = true`

**Конвенции UI кода:**
- Все UI magic numbers — в `GameConfig.Ui.cs`
- Строки на hot path кэшируются, пересоздаются только при изменении значений
- Нет LINQ, нет string concat на hot path, нет `Enum.ToString()` каждый кадр
- `ref struct UiLayout` не может быть полем класса — передаётся через `ref` в helper-методы

## Порядок тика систем

```
--- Start phase (однократно) ---
AssetProvider         → загрузка всех текстур
FloorLifecycleSystem  → генерация 1-го этажа, спавн игрока и сущностей

--- Tick phase (каждый кадр) ---
FloorLifecycleSystem  → обработка перехода этажа (если FloorTransitionRequested)
UiInputSystem         → ClearFrame, modal Esc/Enter, InputConsumed
InputSystem           → движение WASD (пропускает если не Playing)
InventoryInputSystem  → Tab инвентарь, клики по слотам, использование предметов
CombatInputSystem     → ЛКМ атака, Shift дэш (пропускает если InputConsumed/не Playing)
SpellInputSystem      → Z/X переключение spell slots, ПКМ каст (пропускает если InputConsumed), тик кулдауна
SpellCastSystem       → обработка SpellCastRequest → спавн projectile/AoE/heal/teleport
DashSystem            → дэш-движение, i-frames, afterimage, кулдауны
PhysicsSystem         → движение + коллизии стен + SlowDebuff масштабирование
ProjectileSystem      → коллизия снарядов со стенами/врагами, AoE взрывы, Chain Lightning
MeleeAttackSystem     → сектор атаки, DamageEvent-ы, screen shake
AISystem              → state machine врагов, A* навигация, атака
StatusEffectSystem    → Burn DoT → DamageEvents, Slow → SlowDebuff, снятие истёкших
HealthSystem          → дренит DamageEvents, смерть, DamageFlash
ManaSystem            → регенерация MP
ItemDropSystem        → дроп предметов при смерти врагов
ItemPickupSystem      → подбор предметов с земли
ChestSystem           → взаимодействие с сундуками
ItemUseSystem         → применение предметов (зелья, свитки, spell scrolls)
DamageNumberSystem    → float-up чисел урона
FloorTransitionSystem → детектит лестницу + ставит FloorTransitionRequested
FovSystem             → туман войны
AnimationSystem       → покадровая анимация спрайтов
CameraSystem          → камера (тикает всегда)
RenderSystem          → обёртка рендера (тикает всегда)
  ├─ TileRenderSystem      (World) — тайлы + декор + FOV
  ├─ EntityRenderSystem    (World) — сущности + FOV + DamageFlash + afterimage
  ├─ CombatRenderSystem    (World) — дуга атаки, HP-бары врагов, числа урона
  ├─ SpellRenderSystem     (World) — снаряды, AoE визуалы, индикаторы статус-эффектов
  ├─ ItemRenderSystem      (World) — предметы на земле, сундуки
  ├─ DecorationObjectRenderSystem (World) — декоративные объекты
  ├─ DebugRenderSystem     (World) — сетка + коллайдеры
  ├─ HudRenderSystem       (Screen) — FPS, этаж, HP/MP бары, мини-карта
  ├─ MagicHudRenderSystem  (Screen) — 3 spell slots UI, подсветка активного
  ├─ InventoryRenderSystem (Screen) — UI инвентаря, экипировка, быстрые слоты, статы героя
  └─ FullscreenMapRenderSystem (Screen) — полноэкранная карта (поверх всех HUD)

--- Dispose phase (при выходе) ---
AssetProvider         → выгрузка всех текстур
```

## Структура каталогов

```
src/
├── Core/              — Game, GameConfig, GameContext, GameState, ServiceRegistration,
│   │                    IAssetProvider, AssetProvider
│   └── Config/        — GameConfig.Combat.cs, GameConfig.Magic.cs, GameConfig.Ui.cs (partial classes)
├── ECS/
│   ├── Core/          — World, ComponentStore, IStartable, ITickable, IRenderTickable
│   ├── Player/        — PlayerTag, InputSystem
│   ├── Physics/       — Position, Velocity, Collider, PhysicsSystem
│   ├── Rendering/     — Sprite, RenderSystem, CameraSystem,
│   │                    TileRenderSystem, EntityRenderSystem,
│   │                    DebugRenderSystem, HudRenderSystem,
│   │                    FullscreenMapRenderSystem
│   ├── Exploration/   — FovSystem, FloorTransitionSystem, FloorLifecycleSystem
│   ├── Combat/
│   │   ├── Components/ — Health, Stats, EnemyTag, MeleeAttack, DashState,
│   │   │                 DashCooldown, Invincible, DamageFlash,
│   │   │                 DamageNumber, AfterimageParticle
│   │   ├── Systems/    — CombatInputSystem, DashSystem, MeleeAttackSystem,
│   │   │                 AISystem, HealthSystem, DamageNumberSystem,
│   │   │                 CombatRenderSystem
│   │   ├── DamageEvent.cs, DamageCalculator.cs
│   │   ├── EnemyTemplate.cs, EnemyRegistry.cs, EnemySpawner.cs
│   │   └── AStarPathfinder.cs
│   ├── Items/
│   │   ├── Components/ — Inventory, Equipment, QuickSlots, ItemStack,
│   │   │                 ItemOnGround, Chest
│   │   ├── Systems/    — InventoryInputSystem, ItemDropSystem, ItemPickupSystem,
│   │   │                 ChestSystem, ItemUseSystem, ItemRenderSystem,
│   │   │                 InventoryRenderSystem
│   │   ├── ItemDatabase.cs, ItemDefinition.cs, ItemAtlas.cs, ChestSpawner.cs
│   │   └── ItemType.cs, ItemRarity.cs, ItemEffectType.cs
│   └── Magic/
│       ├── Components/ — Mana, SpellSlots, SpellCastRequest, Projectile,
│       │                 AoEVisual, SlowDebuff, StatusEffects, StatusEffectSlot
│       ├── Systems/    — ManaSystem, SpellInputSystem, SpellCastSystem,
│       │                 ProjectileSystem, StatusEffectSystem,
│       │                 SpellRenderSystem, MagicHudRenderSystem
│       ├── SpellDatabase.cs, SpellDefinition.cs, MagicHelper.cs
│       ├── SpellId.cs, SpellEffectType.cs
│       └── SpellsJsonContext.cs
├── UI/                — UiRect, UiLayout, UiDraw, UiTheme, UiContext,
│                        UiInputSystem, UiModal, TextMeasureCache, FlexDirection
├── Dungeon/           — TileMap, Room, DungeonGenerator
│   └── Generation/    — BspTree, RoomPlacer, CorridorCarver, DecorationPainter
└── Program.cs         — точка входа
assets/
├── sprites/
│   ├── characters/    — анимации героя и врагов
│   ├── environment/   — тайлсеты, декор, сундуки, факелы
│   └── items/         — спрайты предметов + items.png спрайтшит
├── sounds/
└── data/
tools/                 — утилиты (pack_items.py — упаковка спрайтшитов)
docs/plans/            — планы по фазам разработки
```

## Конвенции кода

- **Язык кода:** C# 12, английские имена классов/методов/переменных
- **Язык комментариев и документации:** русский
- **Неймспейсы** соответствуют структуре папок (напр. `DungeonOfShadows.ECS.Physics.Systems` для файлов в `src/ECS/Physics/`)
- **Логические системы** наследуют `ITickable`, получают `World`, `GameConfig`, `GameContext` через DI-конструктор
- **Рендер-подсистемы** наследуют `IRenderTickable`, указывают `RenderPhase`
- **Компоненты** — struct, без логики, каждый в отдельном файле
- **Системы** — class, вся логика в `Tick(float dt)`
- **Системы не зависят друг от друга** — только от `World`, `GameConfig`, `GameContext` и компонентов
- **Конфигурация** — через `GameConfig`, не через константы в классах
- **Новые зависимости** — регистрировать в `ServiceRegistration.cs`
- **Фиче-ориентированная структура** — компоненты и системы группируются по фичам в `src/ECS/`
- **Hot paths** — переиспользовать буферы запросов (`List<int>` поля), избегать аллокаций на каждый тик. Предпочитать `World.QueryInto(...)` с буфером вместо LINQ

## Планы разработки

Подробные планы по каждой фазе — в `docs/plans/`. Сводка — в `docs/plans/overview.md`.
TDD — в `docs/dungeon_of_shadows_tdd.md`.

## Ключевые файлы

- Порядок тика и регистрация систем: `src/Core/ServiceRegistration.cs`
- ECS-ядро: `src/ECS/Core/World.cs`
- Оркестрация рендера: `src/ECS/Rendering/RenderSystem.cs`
- Конфигурация геймплея: `src/Core/GameConfig.cs` + `src/Core/Config/`
- Генерация уровней: `src/ECS/Exploration/FloorLifecycleSystem.cs`
- Атлас предметов: `src/ECS/Items/ItemAtlas.cs`
- Атлас тайлов: `src/ECS/Rendering/TileAtlas.cs`
- UI framework: `src/UI/` (UiLayout, UiDraw, UiTheme, UiContext, UiRect)
- UI конфигурация: `src/Core/Config/GameConfig.Ui.cs`
- Планы по фазам: `docs/plans/`
- Технический дизайн: `docs/dungeon_of_shadows_tdd.md`

## Горячие клавиши (в игре)

- WASD / стрелки — движение
- ЛКМ — атака мечом (в направлении мыши)
- ПКМ — каст активного заклинания (в направлении мыши)
- Z / X — переключение слотов заклинаний (предыдущий / следующий)
- Колесо мыши — переключение слотов заклинаний
- L / Shift — дэш (в направлении движения)
- E / Space — спуск по лестнице
- 1-4 — использование предмета из быстрых слотов
- I / Tab — инвентарь
- F3 — дебаг-режим (сетка + коллайдеры)
- ESC — выход
