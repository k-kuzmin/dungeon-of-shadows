# Architecture: Система конфигурации аниматоров

> Last updated: 2026-04-05 | Status: final | Platform: Generic (C# 12 + Raylib-cs)

## Chosen Approach
Прагматичный (подход 3). Единый JSON с четырьмя типами аниматоров: `directional`, `simple`, `atlas`, `multi`. Все существующие аниматоры мигрируют на JSON. CharacterAnimationBuilder заменяется AnimatorFactory.

## Performance Profile
| Component | Hot Path | Allocs/call | Strategy |
|-----------|----------|-------------|----------|
| AnimatorDatabase.Start() | no | O(N) parse | one-time at startup |
| AnimatorFactory.Build() | no | 1 Dict + N clips | at entity spawn only |
| AnimationSystem.Tick() | yes | 0 | no changes needed |
| SpellRenderSystem (projectile draw) | yes | 0 | fallback check via Has<Animation> |

## Components

### AnimatorDefinition (data model)
- **File**: `src/ECS/Rendering/AnimatorDefinition.cs`
- **Responsibility**: Десериализуемая модель одного аниматора из JSON
- **Hot path**: no
- **Key fields**: Id, Type (directional/simple/atlas/multi), Directions[], FrameWidth, FrameHeight, InitialClip, Clips[]

### ClipDefinition (nested data model)
- **File**: `src/ECS/Rendering/AnimatorDefinition.cs` (nested class)
- **Responsibility**: Определение одного клипа внутри аниматора
- **Key fields**: Name, Texture, Frames (int или int[]), FrameDuration, Loop, StartX, StartY, StrideX, StrideY

### AnimatorsJsonContext (AOT)
- **File**: `src/ECS/Rendering/AnimatorsJsonContext.cs`
- **Responsibility**: Source-gen контекст для NativeAOT десериализации
- **Pattern**: `[JsonSerializable(typeof(AnimatorDefinition[]))]`

### AnimatorDatabase : IStartable
- **File**: `src/ECS/Rendering/AnimatorDatabase.cs`
- **Responsibility**: Загрузка animations.json + fallback. Хранит дефиниции по id.
- **Hot path**: no
- **API**: `bool TryGet(string id, out AnimatorDefinition def)`

### AnimatorFactory
- **File**: `src/ECS/Rendering/AnimatorFactory.cs`
- **Responsibility**: Строит `Dictionary<string, AnimationClip>` из AnimatorDefinition. Заменяет CharacterAnimationBuilder.
- **Hot path**: no (вызывается при спавне)
- **API**: `Dictionary<string, AnimationClip> Build(AnimatorDefinition def)`, `AnimationClip BuildSingleClip(AnimatorDefinition def)`

### Rotation (component)
- **File**: `src/ECS/Rendering/Rotation.cs`
- **Responsibility**: Угол поворота спрайта в градусах (для снарядов)
- **Hot path**: read in render

## Data Flow

### Загрузка (Start)
```
animations.json → AnimatorDatabase.Start() → Dictionary<string, AnimatorDefinition>
                                            ↓ (fallback если JSON нет)
                                  LoadFallback() → те же данные в коде
```

### Спавн сущности
```
SpawnPlayer/SpawnEnemy/SpawnTorches/SpawnChests/SpawnProjectile
  → animDb.TryGet("hero"/"orc1"/"torch"/"chest"/"fireball")
  → AnimatorFactory.Build(def) → Dictionary<string, AnimationClip>
  → world.Add(id, Animator/Animation)
```

### Спавн projectile (новое)
```
SpellCastSystem.SpawnProjectile()
  → animDb.TryGet("fireball") → AnimatorFactory.BuildSingleClip(def)
  → world.Add(id, Sprite(...))
  → world.Add(id, Animation(clip))
  → world.Add(id, Rotation(angleDeg))
```

### Рендер projectile
```
YSortedRenderSystem (Y-sort → DrawEntity → DrawAnimatedSprite)
  → if Has<Rotation> → DrawTexturePro с rotation
SpellRenderSystem.DrawProjectiles()
  → if Has<Animation> → skip (уже отрисован Y-sorted)
  → else → цветной круг (fallback для заклинаний без спрайта)
```

## Key Decisions
| Decision | Choice | Rationale |
|----------|--------|-----------|
| JSON format | Array of AnimatorDefinition | Паттерн SpellDatabase/ItemDatabase |
| Clip frames field | int (uniform) или int[] (per-direction) | Hero Idle имеет разное кол-во кадров по направлениям |
| Atlas frames | startX/Y + stride + count | Декларативно, не хрупкие pixel-массивы |
| Projectile render | YSortedRenderSystem + Rotation | Корректный Y-sort depth |
| Glow effect | Остаётся в SpellRenderSystem | Additive glow после Y-sorted pass |
| Texture registration | AssetProvider manifest пополняется вручную | Не меняем существующий паттерн |

## Constraints & Conventions
- JsonSerializerContext с PropertyNameCaseInsensitive для AOT
- Load() + LoadFallback() паттерн (игра работает без JSON)
- AnimatorDatabase регистрируется в ServiceRegistration после AssetProvider
- Все строковые ключи текстур совпадают с AssetProvider manifest

## Performance Rules
- Нет аллокаций в AnimationSystem.Tick() (не меняется)
- AnimatorFactory.Build() вызывается только при спавне (не hot path)
- Rotation компонент — value type (struct)
- Reuse буферов запросов в render systems

## Out of Scope
- Автоматическая загрузка текстур из JSON (AssetProvider manifest остаётся)
- Анимации для Chain Lightning, Frost Nova, Shadow Step, Heal (остаются кругами)
- Blend-переходы между анимациями
- JSON-editor или валидатор
