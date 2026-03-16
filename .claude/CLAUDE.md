# Dungeon of Shadows — Инструкции для агента

## Проект

Action Roguelike на C# 12 + Raylib-cs + NativeAOT (.NET 8). Пиксель-арт, процедурные подземелья, реалтайм-бой, permadeath.

## Сборка и запуск

```bash
dotnet build DungeonOfShadows.csproj
dotnet run --project DungeonOfShadows.csproj
```

## Архитектура

### DI-контейнер

Используется `Microsoft.Extensions.DependencyInjection`. Все сервисы регистрируются в `src/Core/ServiceRegistration.cs`.

**Добавление новой логической системы:**
```csharp
// В ServiceRegistration.Build():
services.AddTickable<MyNewSystem>();
```
Одна строка — система автоматически попадает в game loop. Порядок регистрации = порядок тика.

**Добавление новой рендер-подсистемы:**
```csharp
services.AddRenderTickable<MyRenderSystem>();
```
Рендер-подсистемы вызываются из `RenderSystem` в нужном контексте (World-space или Screen-space).

### ITickable

Единый интерфейс для всех систем, участвующих в game loop:
```csharp
public interface ITickable { void Tick(float dt); }
```

### IRenderTickable

Интерфейс для рендер-подсистем, вызываемых из `RenderSystem`:
```csharp
public interface IRenderTickable
{
    RenderPhase Phase { get; }  // World или Screen
    void Tick(float dt);
}
```
`RenderSystem` — обёртка, управляет `BeginDrawing/EndDrawing` и `BeginMode2D/EndMode2D`. Внутри вызывает `IRenderTickable` подсистемы по фазам.

### Принципы систем

- **Системы не вызывают друг друга** — общаются через данные (`GameContext`, компоненты, `DamageEvents`)
- **Исключение**: системы могут принимать другие системы через DI для вызова конкретных API (напр. `CameraSystem.TriggerShake()`)
- **Game.Run тикает все системы безусловно** — каждая система сама проверяет `GameState` если нужно
- **Рендер-подсистемы** регистрируются отдельно через `AddRenderTickable`, не через `AddTickable`

### ECS

Лёгкий ECS без внешних фреймворков:
- **Entity** — int ID
- **ComponentStore\<T\>** — генерик-хранилище. Новый компонент не требует правки `World`
- **World** — `Add<T>()`, `Get<T>()`, `Has<T>()`, `Remove<T>()`, `QueryInto<T>()`, `QueryInto<T1,T2>()`

**Добавление нового компонента:** просто создать struct и использовать `world.Add<MyComponent>(id, data)`.

### GameContext

Центральный контейнер зависимостей, передаётся всем системам через конструктор:
- `World` — ECS мир
- `Map` — текущая тайловая карта
- `Config` — все настраиваемые параметры
- `State` — текущее состояние игры (Playing, Paused, Menu, Dead)
- `CurrentFloor` — номер этажа
- `DungeonSeed` — сид генерации
- `DebugMode` — режим отладки
- `ShowFullMap` — полноэкранная карта
- `DamageEvents` — очередь событий урона (заполняется MeleeAttackSystem/AISystem, дренится HealthSystem)

### GameConfig

Все числовые параметры — в `GameConfig`. Не хардкодить магические числа в системах.

### DamageEvent очередь

Урон передаётся через `List<DamageEvent>` на `GameContext`:
- **Продьюсеры**: `MeleeAttackSystem`, `AISystem` — пишут события
- **Консьюмер**: `HealthSystem` — читает, применяет урон, спавнит DamageFlash/DamageNumber, дренит список

## Порядок тика систем

```
InputSystem           → движение WASD (пропускает если не Playing)
CombatInputSystem     → ЛКМ атака, Shift дэш (пропускает если не Playing)
DashSystem            → дэш-движение, i-frames, afterimage, кулдауны
PhysicsSystem         → движение + коллизии стен
MeleeAttackSystem     → сектор атаки, DamageEvent-ы, screen shake
AISystem              → state machine врагов, A* навигация, атака
HealthSystem          → дренит DamageEvents, смерть, DamageFlash
DamageNumberSystem    → float-up чисел урона
FloorTransitionSystem → переход между этажами
FovSystem             → туман войны
CameraSystem          → камера (тикает всегда)
RenderSystem          → обёртка рендера (тикает всегда)
  ├─ TileRenderSystem      (World) — тайлы + декор + FOV
  ├─ EntityRenderSystem    (World) — сущности + FOV + DamageFlash + afterimage
  ├─ CombatRenderSystem    (World) — дуга атаки, HP-бары врагов, числа урона
  ├─ DebugRenderSystem     (World) — сетка + коллайдеры
  └─ HudRenderSystem       (Screen) — FPS, этаж, HP игрока, мини-карта
```

## Структура каталогов

```
src/
├── Core/              — Game, GameConfig, GameContext, GameState, ServiceRegistration
├── ECS/
│   ├── Core/          — World, ComponentStore, ITickable, IRenderTickable
│   ├── Player/        — PlayerTag, InputSystem
│   ├── Physics/       — Position, Velocity, Collider, PhysicsSystem
│   ├── Rendering/     — Sprite, RenderSystem, CameraSystem,
│   │                    TileRenderSystem, EntityRenderSystem,
│   │                    DebugRenderSystem, HudRenderSystem
│   ├── Exploration/   — FovSystem, FloorTransitionSystem
│   └── Combat/
│       ├── Components/ — Health, Stats, EnemyTag, MeleeAttack, DashState,
│       │                 DashCooldown, Invincible, DamageFlash,
│       │                 DamageNumber, AfterimageParticle
│       ├── Systems/    — CombatInputSystem, DashSystem, MeleeAttackSystem,
│       │                 AISystem, HealthSystem, DamageNumberSystem,
│       │                 CombatRenderSystem
│       ├── DamageEvent.cs, DamageCalculator.cs
│       ├── EnemyTemplate.cs, EnemyRegistry.cs, EnemySpawner.cs
│       └── AStarPathfinder.cs
├── Dungeon/           — TileMap, Room, DungeonGenerator
│   └── Generation/    — BspTree, RoomPlacer, CorridorCarver, DecorationPainter
└── Program.cs         — точка входа
assets/
├── sprites/
├── sounds/
└── data/
docs/plans/            — планы по фазам разработки
```

## Конвенции кода

- **Язык кода:** C# 12, английские имена классов/методов/переменных
- **Язык комментариев и документации:** русский
- **Логические системы** наследуют `ITickable` и получают `GameContext` через конструктор
- **Рендер-подсистемы** наследуют `IRenderTickable`, указывают `RenderPhase`
- **Компоненты** — struct, без логики, каждый в отдельном файле
- **Системы** — class, вся логика в `Tick(float dt)`
- **Системы не зависят друг от друга** — только от `GameContext` и компонентов
- **Конфигурация** — через `GameConfig`, не через константы в классах
- **Новые зависимости** — регистрировать в `ServiceRegistration.cs`
- **Фиче-ориентированная структура** — компоненты и системы группируются по фичам в `src/ECS/`

## Планы разработки

Подробные планы по каждой фазе — в `docs/plans/`. Сводка — в `docs/plans/overview.md`.
TDD — в `docs/dungeon_of_shadows_tdd.md`.

## Горячие клавиши (в игре)

- WASD / стрелки — движение
- ЛКМ — атака мечом (в направлении мыши)
- L / Shift — дэш (в направлении движения)
- E / Space — спуск по лестнице
- Tab — полноэкранная карта (пауза)
- F3 — дебаг-режим (сетка + коллайдеры)
- ESC — выход
