# Plan: Система конфигурации аниматоров

## Tasks

### 1. Data model + JSON context
- [x] `AnimatorDefinition` + `ClipDefinition` — модели данных
- [x] `AnimatorsJsonContext` — AOT source-gen
- [x] `assets/data/animations.json` — конфигурация всех аниматоров

### 2. AnimatorDatabase
- [x] `AnimatorDatabase : IStartable` — загрузка JSON + fallback
- [x] Регистрация в `ServiceRegistration.cs`

### 3. AnimatorFactory
- [x] `AnimatorFactory` — построение clip dictionary из дефиниции
- [x] `Build()` для multi-clip (directional, multi)
- [x] `BuildSingleClip()` для simple/atlas

### 4. Миграция существующих аниматоров
- [x] Герой: `FloorLifecycleSystem.SpawnPlayer()` → AnimatorFactory
- [x] Враги: `EnemySpawner.SpawnEnemy()` → AnimatorFactory
- [x] Факелы: `TorchSpawner` → AnimatorFactory
- [x] Сундуки: `ChestSpawner` → AnimatorFactory
- [x] Удалить `CharacterAnimationBuilder`
- [x] Убрать неиспользуемые параметры из `GameConfig.CharacterAnim.cs` и `GameConfig.Sprites.cs`

### 5. Projectile анимации
- [x] Добавить текстуры `fireball` и `magic_bolt` в `AssetProvider`
- [x] `Rotation` компонент
- [x] `SpellCastSystem.SpawnProjectile()` — добавить Sprite + Animation + Rotation
- [x] `YSortedRenderSystem.DrawAnimatedSprite()` — поддержка Rotation
- [x] `SpellRenderSystem.DrawProjectiles()` — fallback (skip если есть Animation)

### 6. Сборка и проверка
- [x] `dotnet build` — без ошибок
- [x] Quality review (Phase 6)

## Files

| File | Action | Status |
|------|--------|--------|
| `src/ECS/Rendering/AnimatorDefinition.cs` | create | done |
| `src/ECS/Rendering/AnimatorsJsonContext.cs` | create | done |
| `src/ECS/Rendering/AnimatorDatabase.cs` | create | done |
| `src/ECS/Rendering/AnimatorFactory.cs` | create | done |
| `src/ECS/Rendering/Rotation.cs` | create | done |
| `assets/data/animations.json` | create | done |
| `src/Core/ServiceRegistration.cs` | modify | done |
| `src/Core/AssetProvider.cs` | modify | done |
| `src/ECS/Exploration/FloorLifecycleSystem.cs` | modify | done |
| `src/ECS/Combat/EnemySpawner.cs` | modify | done |
| `src/ECS/Rendering/TorchSpawner.cs` | modify | done |
| `src/ECS/Items/ChestSpawner.cs` | modify | done |
| `src/ECS/Magic/Systems/SpellCastSystem.cs` | modify | done |
| `src/ECS/Magic/Systems/SpellRenderSystem.cs` | modify | done |
| `src/ECS/Rendering/YSortedRenderSystem.cs` | modify | done |
| `src/ECS/Rendering/CharacterAnimationBuilder.cs` | delete | done |
| `src/Core/Config/GameConfig.CharacterAnim.cs` | modify (cleanup) | done |
| `src/Core/Config/GameConfig.Sprites.cs` | modify (cleanup) | done |
