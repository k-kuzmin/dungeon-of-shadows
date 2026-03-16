# Фаза 2: Dungeon

## Описание

Процедурная генерация подземелий через BSP-алгоритм, система комнат и коридоров, туман войны (FOV) и мини-карта. После этой фазы каждый запуск игры будет создавать уникальный этаж подземелья.

## Статус: Завершено ✓

---

## Что нужно сделать

### 2.1 BSP-генератор
- [x] Реализовать BSP-дерево (рекурсивное разделение пространства)
- [x] Размер карты: 60×50 тайлов (базовый)
- [x] Масштабирование: +5×5 на каждые 5 этажей
- [x] Листья дерева → размещение комнат
- [x] Мин. размер комнаты: 5×4, макс: 12×10

### 2.2 Комнаты
- [x] Структура `Room` (прямоугольник, тип, содержимое)
- [x] 8–14 комнат на этаж
- [x] Типы комнат: обычная, стартовая (spawn), лестница вниз
- [x] Комната босса (каждые 5 этажей) — совмещена с лестницей
- [x] Декор: факелы, трещины, кости, лужи (не влияют на проходимость)

### 2.3 Коридоры
- [x] L-образные коридоры между смежными комнатами BSP-дерева
- [x] Ширина 2 тайла
- [ ] Двери на входах в комнаты (отложено)

### 2.4 Туман войны (FOV)
- [x] Raycasting на основе алгоритма Bresenham
- [x] 360 лучей (по 1 на градус)
- [x] Базовый радиус видимости: 7 тайлов
- [x] Три состояния тайла: неизвестный, исследованный, видимый
- [x] Стены блокируют луч, но сами видимы
- [x] Обновление при смене тайла игрока (оптимизация вместо каждого кадра)
- [x] Рендер: неизвестный — чёрный, исследованный — затемнение 60%
- [x] Пост-проход: стены рядом с видимым полом автоматически видимы

### 2.5 Мини-карта
- [x] Отображение в правом верхнем углу (~200px)
- [x] Исследованные комнаты и коридоры
- [x] Позиция игрока (маркер)
- [x] Позиция лестницы (если обнаружена)
- [x] Tab — полноэкранный режим мини-карты (с паузой)

### 2.6 Переход между этажами
- [x] Лестница вниз — интерактивный тайл (E/Space)
- [x] Генерация нового этажа при спуске (детерминированный сид)
- [x] Увеличение счётчика этажей
- [x] Размещение игрока в стартовой комнате нового этажа
- [x] Сущность игрока сохраняется между этажами

---

## Что сделано

### Архитектура рендера (рефакторинг)

```
RenderSystem (ITickable, обёртка)
  ├─ BeginMode2D
  │   TileRenderSystem      (IRenderTickable, World)
  │   EntityRenderSystem    (IRenderTickable, World)
  │   DebugRenderSystem     (IRenderTickable, World)
  ├─ EndMode2D
  │   HudRenderSystem       (IRenderTickable, Screen)
  └─ EndDrawing
```

- `IRenderTickable` — интерфейс рендер-подсистем с `RenderPhase` (World/Screen)
- `AddRenderTickable<T>()` — регистрация, порядок = порядок отрисовки
- Системы не вызывают друг друга, общаются через `GameContext`
- Game.Run тикает все системы безусловно, каждая сама проверяет `GameState`

### Генерация подземелий

```
DungeonGenerator.Generate(floor, config, seed)
  ├── BspTree.Build()          → рекурсивное деление
  ├── RoomPlacer.PlaceRooms()  → комнаты в листьях BSP
  ├── AssignRoomTypes()        → spawn + stair (макс. удалённая)
  ├── CarveRooms/Corridors     → вырезаем в TileMap
  ├── DecorationPainter.Paint()→ факелы, трещины, кости, лужи
  └── return GenerationResult
```

- Детерминированный сид: `seed ^ (floor * 1_000_003)`
- Ретраи (до 10) при недостатке комнат
- Коридоры только между листьями с комнатами (защита от разрывов)

### Файлы

| Файл | Назначение |
|------|-----------|
| `src/Dungeon/Room.cs` | `Room` struct, `RoomType` enum |
| `src/Dungeon/DungeonGenerator.cs` | Фасад генерации, `GenerationResult` |
| `src/Dungeon/Generation/BspTree.cs` | BSP-дерево, сбор листьев и пар |
| `src/Dungeon/Generation/RoomPlacer.cs` | Размещение комнат в листьях |
| `src/Dungeon/Generation/CorridorCarver.cs` | L-образные коридоры |
| `src/Dungeon/Generation/DecorationPainter.cs` | Случайные декорации |
| `src/ECS/IRenderTickable.cs` | Интерфейс рендер-подсистем + `RenderPhase` |
| `src/ECS/Systems/FovSystem.cs` | Bresenham FOV, пост-проход для стен |
| `src/ECS/Systems/FloorTransitionSystem.cs` | Переход между этажами |
| `src/ECS/Systems/TileRenderSystem.cs` | Рендер тайлов + декор + FOV |
| `src/ECS/Systems/EntityRenderSystem.cs` | Рендер сущностей + FOV |
| `src/ECS/Systems/DebugRenderSystem.cs` | Сетка + коллайдеры |
| `src/ECS/Systems/HudRenderSystem.cs` | FPS, этаж, мини-карта, fullscreen карта |
| `src/ECS/Systems/RenderSystem.cs` | Обёртка: BeginDrawing/EndDrawing |

### Изменённые файлы из Фазы 1

| Файл | Изменения |
|------|-----------|
| `src/Dungeon/TileMap.cs` | `StairDown`, `DecorationType`, `Visibility`, `Decorations[,]`, `Rooms` |
| `src/Core/GameConfig.cs` | Параметры генерации, FOV, мини-карты |
| `src/Core/GameContext.cs` | `CurrentFloor`, `DungeonSeed`, `DebugMode`, `ShowFullMap` |
| `src/Core/Game.cs` | Генерация вместо тестовой карты, безусловный тик систем |
| `src/Core/ServiceRegistration.cs` | Новые системы, `AddRenderTickable` |
| `src/Program.cs` | Исправлен `using` на `IServiceProvider` |

---

## Зависимости

- **Фаза 1 (Core)** ✓ — тайловый рендер, ECS, движение игрока, камера

## Результат фазы

Каждый запуск/спуск генерирует уникальное подземелье (8–14 комнат, BSP). Игрок исследует комнаты и коридоры, туман войны скрывает неизведанные области, мини-карта показывает прогресс исследования. E/Space на лестнице → спуск на следующий этаж. Tab → полноэкранная карта с паузой.
