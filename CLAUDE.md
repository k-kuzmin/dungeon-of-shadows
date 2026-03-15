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

**Добавление новой системы:**
```csharp
// В ServiceRegistration.Build():
services.AddTickable<MyNewSystem>();
```
Одна строка — система автоматически попадает в game loop. Порядок регистрации = порядок тика.

### ITickable

Единый интерфейс для всех систем, участвующих в game loop:
```csharp
public interface ITickable { void Tick(float dt); }
```
Не создавать отдельные интерфейсы для update/draw/etc. Всё через `ITickable`.

### ECS

Лёгкий ECS без внешних фреймворков:
- **Entity** — int ID
- **ComponentStore\<T\>** — генерик-хранилище. Новый компонент не требует правки `World`
- **World** — `Add<T>()`, `Get<T>()`, `Has<T>()`, `Query<T>()`, `Query<T1,T2>()`

**Добавление нового компонента:** просто создать struct и использовать `world.Add<MyComponent>(id, data)`.

### GameContext

Центральный контейнер зависимостей, передаётся всем системам через конструктор:
- `World` — ECS мир
- `Map` — текущая тайловая карта
- `Config` — все настраиваемые параметры
- `State` — текущее состояние игры

### GameConfig

Все числовые параметры — в `GameConfig`. Не хардкодить магические числа в системах.

## Структура каталогов

```
src/
├── Core/          — Game, GameConfig, GameContext, GameState, ServiceRegistration
├── ECS/           — World, ComponentStore, Components, ITickable
│   └── Systems/   — InputSystem, PhysicsSystem, CameraSystem, RenderSystem
├── Dungeon/       — TileMap, (будущее: генератор, FOV)
├── Rendering/     — (будущее: спрайты, частицы)
└── Program.cs     — точка входа
assets/
├── sprites/
├── sounds/
└── data/
docs/plans/        — планы по фазам разработки
```

## Конвенции кода

- **Язык кода:** C# 12, английские имена классов/методов/переменных
- **Язык комментариев и документации:** русский
- **Все системы** наследуют `ITickable` и получают `GameContext` через конструктор
- **Компоненты** — struct, без логики
- **Системы** — class, вся логика в `Tick(float dt)`
- **Конфигурация** — через `GameConfig`, не через константы в классах
- **Новые зависимости** — регистрировать в `ServiceRegistration.cs`

## Планы разработки

Подробные планы по каждой фазе — в `docs/plans/`. Сводка — в `docs/plans/overview.md`.
TDD — в `docs/dungeon_of_shadows_tdd.md`.

## Горячие клавиши (в игре)

- WASD / стрелки — движение
- F3 — дебаг-режим (сетка + коллайдеры)
- ESC — выход
