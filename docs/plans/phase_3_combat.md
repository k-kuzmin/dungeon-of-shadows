# Фаза 3: Combat

## Описание

Реалтайм-боёвая система: атака мечом (дуга 90°), первые 3 типа врагов с AI-поведением, система дэша с i-frames, полоски HP и базовые формулы урона.

## Статус: Завершено ✓

---

## Что нужно сделать

### 3.1 Атака мечом
- [x] Дуга атаки в направлении курсора (fallback на направление взгляда)
- [x] Длительность атаки настраивается (`PlayerMeleeDuration`, сейчас 0.18с)
- [x] Кулдаун melee атаки (базовый, через `MeleeCooldown`)
- [x] Проверка попадания: секторная hit-проверка по врагам
- [x] Визуальный эффект слэша

### 3.2 Формулы урона
- [x] Физический урон: `(ATK × weaponMult) − (target.DEF × 0.7) + random(−1, +2)`
- [x] Крит: `damage × 1.8`, шанс = `attacker.Crit`
- [x] Числа урона (entity + lifetime + float-up)
- [x] Компонент `Health` (HP, maxHP)
- [x] Компонент `Stats` (ATK, DEF, SPD, Crit)
- [x] Магический контур (`INT`, `RES`) перенесён в фазу Magic (вне скоупа Combat)

### 3.3 Dash (рывок)
- [x] Клавиша: L / Shift
- [x] i-frames (настраиваемо, сейчас 0.25с)
- [x] Кулдаун дэша (настраиваемо, сейчас 0.9с)
- [x] Визуальный след (afterimage)

### 3.4 Враги — базовые типы
- [x] **Rat** (HP: 8, ATK: 2, DEF: 0) — flee при низком HP
- [x] **Goblin** (HP: 15, ATK: 4, DEF: 1) — strafe-поведение в среднем радиусе
- [x] **Skeleton** (HP: 20, ATK: 6, DEF: 2) — heavy attack с windup + множитель урона
- [x] Уникальные спец-паттерны для Goblin/Skeleton

### 3.5 AI-система
- [x] AISystem: обработка поведения врагов
- [x] Обнаружение игрока по радиусу
- [x] Состояния: Idle, Patrol, Chase, Attack, Flee
- [x] Навигация: A* + fallback прямое движение
- [x] Атака по кулдауну в радиусе атаки

### 3.6 Скейлинг врагов
- [x] Формула: `stat = base × (1 + floor × scale)`
- [x] Количество: `EnemiesPerRoomMin..Max` (сейчас 1–3)
- [x] Спавн врагов при генерации этажа
- [x] Модель спавна зафиксирована: генерация всего этажа при загрузке

### 3.7 HUD — полоски HP
- [x] Полоска HP игрока
- [x] Полоски HP врагов
- [x] Фидбэк урона: flash, damage numbers, screen-shake

### 3.8 Смерть
- [x] Анимация смерти врагов (fade-out через `EnemyDeathState`)
- [x] Удаление мёртвых сущностей
- [x] Смерть игрока → `GameState.Dead`

---

## Что сделано

### Реализованные системы и компоненты

- Введена очередь `DamageEvents` в `GameContext` (продьюсеры: melee/AI, консьюмер: health).
- Добавлены combat-компоненты: `Health`, `Stats`, `MeleeAttack`, `EnemyTag`, `DashState`, `DashCooldown`, `Invincible`, `DamageFlash`, `DamageNumber`, `AfterimageParticle`.
- Добавлены combat-компоненты: `Health`, `Stats`, `MeleeAttack`, `MeleeCooldown`, `EnemyTag`, `EnemyDeathState`, `DashState`, `DashCooldown`, `Invincible`, `DamageFlash`, `DamageNumber`, `AfterimageParticle`.
- Реализованы системы: `CombatInputSystem`, `MeleeAttackSystem`, `AISystem`, `DashSystem`, `HealthSystem`, `DamageNumberSystem`, `CombatRenderSystem`.
- Добавлен `AStarPathfinder`, `EnemyRegistry`, `EnemySpawner`, `DamageCalculator`.

### Тик-порядок (с учётом combat)

1. `InputSystem`
2. `CombatInputSystem`
3. `DashSystem`
4. `PhysicsSystem`
5. `MeleeAttackSystem`
6. `AISystem`
7. `HealthSystem`
8. `DamageNumberSystem`
9. `FloorTransitionSystem`
10. `FovSystem`
11. `CameraSystem`
12. `RenderSystem`

### Ключевые файлы

| Файл | Назначение |
|------|-----------|
| `src/ECS/Combat/Systems/CombatInputSystem.cs` | Ввод атаки и дэша |
| `src/ECS/Combat/Systems/MeleeAttackSystem.cs` | Секторный melee hit-scan |
| `src/ECS/Combat/Systems/AISystem.cs` | AI состояния и навигация |
| `src/ECS/Combat/Systems/DashSystem.cs` | Дэш, i-frames, afterimage |
| `src/ECS/Combat/Systems/HealthSystem.cs` | Применение урона, death, flash |
| `src/ECS/Combat/Systems/DamageNumberSystem.cs` | Жизненный цикл damage numbers |
| `src/ECS/Combat/Systems/CombatRenderSystem.cs` | Слэш-дуга, enemy HP bars, damage numbers |
| `src/ECS/Combat/EnemySpawner.cs` | Спавн врагов на этаж |
| `src/Core/GameConfig.cs` | Параметры combat/balance |
| `src/Core/ServiceRegistration.cs` | DI и порядок тика систем |

---

## Зависимости

- **Фаза 1 (Core)** — ECS, движение, коллизии
- **Фаза 2 (Dungeon)** — генерация комнат для размещения врагов

## Результат фазы

Игрок может атаковать мечом с кулдауном, уклоняться дэшем, сражаться с Rat/Goblin/Skeleton, получать визуальный и числовой фидбэк урона, видеть HP игрока и врагов. Враги имеют AI-состояния, pathfinding и уникальные паттерны поведения (Goblin strafe, Skeleton heavy attack). Смерть врагов сопровождается анимацией fade-out и корректным удалением сущностей.
