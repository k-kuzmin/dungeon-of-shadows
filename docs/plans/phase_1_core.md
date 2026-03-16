# Фаза 1: Core

## Описание

Создание фундамента проекта: окно Raylib, тайловый рендер, движение игрока, коллизии со стенами и плавная камера. Это базовый каркас, на который будут надстраиваться все последующие системы.

## Статус: Завершено ✓

---

## Что нужно сделать

### 1.1 Инициализация проекта
- [x] Создать .NET 8 проект (`DungeonOfShadows.csproj`)
- [x] Подключить NuGet-пакеты: `Raylib-cs 6.1.1`, `Microsoft.Extensions.DependencyInjection 10.0.5`
- [x] Настроить NativeAOT (`PublishAot=true`)
- [x] Создать структуру каталогов (`src/Core/`, `src/ECS/`, `src/Dungeon/`, `assets/`)
- [x] Реализовать `Program.cs` — точка входа с DI-контейнером

### 1.2 Окно и Game Loop
- [x] Инициализация окна Raylib (1280×720, заголовок, целевой FPS 60)
- [x] Основной игровой цикл (`while (!WindowShouldClose())`)
- [x] Система GameState (Menu, Playing, Paused, Dead)
- [x] Расчёт delta time через `GetFrameTime()`

### 1.3 DI-архитектура
- [x] `GameConfig` — все настраиваемые параметры в одном месте
- [x] `GameContext` — контейнер зависимостей (World, Map, Config, State)
- [x] `ITickable` — единый интерфейс для всех тикающих систем
- [x] `ServiceRegistration` — централизованная регистрация модулей
- [x] `AddTickable<T>()` — одна строка = одна система в game loop
- [x] Все системы получают зависимости через конструктор (constructor injection)

### 1.4 ECS-каркас
- [x] Entity — int ID с генератором
- [x] `ComponentStore<T>` — генерик-хранилище (новый компонент без правки World)
- [x] Базовые компоненты-структуры: `Position`, `Velocity`, `Sprite`, `Collider`, `PlayerTag`
- [x] `World` — менеджер сущностей (создание, удаление, `Query<T>`, `Query<T1,T2>`)

### 1.5 Тайловый рендер
- [x] Структура `Tile` (тип, проходимость)
- [x] Карта тайлов (`Tile[,]`) — тестовая карта с 6 комнатами + 7 коридоров
- [x] Рендер тайлов 16×16 px с масштабом ×3 (48×48 на экране)
- [x] Culling по камере (отрисовка только видимых тайлов)
- [x] Placeholder-спрайты (цветные прямоугольники)

### 1.6 Игрок — движение
- [x] Сущность Player с компонентами `Position`, `Velocity`, `Sprite`, `Collider`, `PlayerTag`
- [x] InputSystem: WASD / стрелки — 8 направлений, нормализация диагонали
- [x] Плавное движение (SPD = 4.5 тайл/с)
- [x] Направление последнего движения (для будущей атаки)

### 1.7 Коллизии со стенами
- [x] PhysicsSystem: проверка тайлов на проходимость
- [x] Скользящие коллизии (раздельная проверка по X и Y)
- [x] Collider на основе AABB

### 1.8 Камера
- [x] Плавное следование за игроком (lerp, фактор delta time)
- [x] Camera2D Raylib с offset на центр экрана
- [x] Подготовка для screen-shake (амплитуда 3px, затухание 0.15с)

### 1.9 Отладка
- [x] Отрисовка FPS
- [x] Дебаг-режим (F3): отображение коллайдеров, тайловой сетки

---

## Что сделано

### Архитектура

```
Program.cs
  └── ServiceRegistration.Build(config)  — DI-контейнер
        ├── GameConfig                    — все параметры
        ├── GameContext                   — World + Map + State
        ├── InputSystem    : ITickable    — ввод
        ├── PhysicsSystem  : ITickable    — движение + коллизии
        ├── CameraSystem   : ITickable    — камера + screen-shake
        ├── RenderSystem   : ITickable    — отрисовка
        └── Game                          — game loop (итерирует ITickable)
```

- **DI** — `Microsoft.Extensions.DependencyInjection`, constructor injection
- **Регистрация системы** — одна строка: `services.AddTickable<MySystem>()`
- **ECS** — генерик `ComponentStore<T>`, новый компонент = `world.Add<T>(id, data)` без правки World
- **Скорость** — 4.5 тайл/с (увеличена ×1.5 от базовых 3.0)
- **Карта** — 6 комнат (40×30 тайлов): spawn, armory, library, crypt, great hall, throne room

### Файлы

| Файл | Назначение |
|------|-----------|
| `DungeonOfShadows.csproj` | .NET 8, Raylib-cs, DI, NativeAOT |
| `src/Program.cs` | Точка входа, сборка DI-контейнера |
| `src/Core/GameConfig.cs` | Все настраиваемые параметры |
| `src/Core/GameContext.cs` | Контейнер зависимостей |
| `src/Core/GameState.cs` | Enum состояний игры |
| `src/Core/ServiceRegistration.cs` | Регистрация всех модулей |
| `src/Core/Game.cs` | Game loop, спавн игрока |
| `src/ECS/IGameSystem.cs` | Интерфейс `ITickable` |
| `src/ECS/ComponentStore.cs` | Генерик-хранилище компонентов |
| `src/ECS/Components.cs` | Position, Velocity, Sprite, Collider, PlayerTag |
| `src/ECS/World.cs` | Entity manager, Query<T>, Query<T1,T2> |
| `src/ECS/Systems/InputSystem.cs` | WASD/стрелки, 8 направлений |
| `src/ECS/Systems/PhysicsSystem.cs` | AABB коллизии, скольжение |
| `src/ECS/Systems/CameraSystem.cs` | Плавный lerp, screen-shake |
| `src/ECS/Systems/RenderSystem.cs` | Обёртка рендера (BeginDrawing/EndDrawing) — рефакторинг в Фазе 2 |
| `src/Dungeon/TileMap.cs` | Тайловая карта, тестовый уровень |

---

## Зависимости

- Нет зависимостей от других фаз (это фундамент)

## Результат фазы

Запускаемое приложение с DI-архитектурой: окно 1280×720, 6 комнат с коридорами, игрок двигается WASD (4.5 тайл/с), скользящие коллизии со стенами, плавная камера, дебаг-режим F3.

Запуск: `dotnet run --project DungeonOfShadows.csproj`
