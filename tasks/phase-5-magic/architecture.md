# Architecture: Magic System

> Last updated: 2026-03-23 | Status: final | Platform: Generic (C# 12 + Raylib-cs)

## Chosen Approach
Pragmatic Balance — SpellDatabase через JSON, раздельные SpellInputSystem/SpellCastSystem, inline StatusEffects struct, SlowDebuff через PhysicsSystem, MP bar в HudRenderSystem, отдельный MagicHudRenderSystem для spell slots.

## Performance Profile
| Component | Hot Path | Allocs/call | Strategy |
|-----------|----------|-------------|----------|
| Mana | yes | 0 | struct, cached query buffer |
| SpellSlots | no | 0 | struct |
| Projectile | yes | 0 | struct, entity-per-projectile |
| StatusEffects | yes | 0 | struct, inline 4 slots (Effect0-3) |
| SlowDebuff | yes | 0 | struct marker |
| AoEVisual | no | 0 | struct, short-lived entity |
| SpellCastRequest | no | 0 | struct component on entity |

## Components

### Mana — `src/ECS/Magic/Components/Mana.cs`
- **Responsibility**: MP/MaxMP ресурс, аккумулятор регенерации
- **Hot path**: yes
- **Fields**: `int MP, MaxMP; float RegenAccumulator`

### SpellSlots — `src/ECS/Magic/Components/SpellSlots.cs`
- **Responsibility**: 3 слота заклинаний, индекс активного, кулдаун каста
- **Hot path**: no
- **Fields**: `int Slot0SpellId, Slot1SpellId, Slot2SpellId; int ActiveSlotIndex; float CastCooldown`

### Projectile — `src/ECS/Magic/Components/Projectile.cs`
- **Responsibility**: Данные снаряда — владелец, урон, тип, поведение при попадании
- **Hot path**: yes
- **Fields**: `int OwnerId, SpellId, Damage; float Radius, LifeRemaining; bool IsAoe; SpellEffectType OnHitEffect; float EffectDuration`

### StatusEffects — `src/ECS/Magic/Components/StatusEffects.cs`
- **Responsibility**: До 4 активных эффектов на entity (Burn DoT, Slow)
- **Hot path**: yes
- **Fields**: `StatusEffectSlot Effect0, Effect1, Effect2, Effect3; int ActiveCount`

### StatusEffectSlot — `src/ECS/Magic/Components/StatusEffects.cs` (inner struct)
- **Responsibility**: Один слот эффекта — тип, длительность, параметры
- **Fields**: `SpellEffectType Type; float Duration, TickAccumulator; int DamagePerTick; float SlowFactor`

### SlowDebuff — `src/ECS/Magic/Components/SlowDebuff.cs`
- **Responsibility**: Маркер замедления, PhysicsSystem умножает velocity
- **Hot path**: yes
- **Fields**: `float Factor` (0.4 = 60% slow)

### SpellCastRequest — `src/ECS/Magic/Components/SpellCastRequest.cs`
- **Responsibility**: Запрос на каст, живёт 1 кадр на entity игрока
- **Hot path**: no
- **Fields**: `int SpellId; float TargetX, TargetY`

### AoEVisual — `src/ECS/Magic/Components/AoEVisual.cs`
- **Responsibility**: Временный визуал AoE-эффекта (круг, 0.3s)
- **Hot path**: no
- **Fields**: `float Radius, TimeRemaining; byte R, G, B, A`

## Systems

### ManaSystem — `src/ECS/Magic/Systems/ManaSystem.cs`
- **Type**: ITickable
- **Responsibility**: Регенерация MP (1 MP / 3s), тик CastCooldown
- **Tick order**: После HealthSystem, до ItemDropSystem

### SpellInputSystem — `src/ECS/Magic/Systems/SpellInputSystem.cs`
- **Type**: ITickable
- **Responsibility**: ПКМ каст, Z/X/wheel переключение слотов, проверка MP/кулдауна, Add SpellCastRequest, тик CastCooldown
- **Tick order**: После CombatInputSystem, до DashSystem
- **Note**: Z/X вместо Q/E — E используется для взаимодействия на сцене

### SpellCastSystem — `src/ECS/Magic/Systems/SpellCastSystem.cs`
- **Type**: ITickable
- **Responsibility**: Читает SpellCastRequest → спавн projectile / мгновенный AoE / телепорт / хил
- **Tick order**: После SpellInputSystem, до DashSystem

### ProjectileSystem — `src/ECS/Magic/Systems/ProjectileSystem.cs`
- **Type**: ITickable
- **Responsibility**: Движение снарядов, wall collision (IsWalkable), AABB entity collision, DamageEvents, AoE взрыв, StatusEffects на попадание
- **Tick order**: После PhysicsSystem, до AISystem

### StatusEffectSystem — `src/ECS/Magic/Systems/StatusEffectSystem.cs`
- **Type**: ITickable
- **Responsibility**: Tick Burn DoT → DamageEvents, управление SlowDebuff компонентом, снятие истёкших эффектов
- **Tick order**: После AISystem, до HealthSystem

### SpellRenderSystem — `src/ECS/Magic/Systems/SpellRenderSystem.cs`
- **Type**: IRenderTickable, RenderPhase.World
- **Responsibility**: DrawCircle для projectiles, DrawCircleLines для AoE visuals, визуал статус-эффектов на врагах

### MagicHudRenderSystem — `src/ECS/Magic/Systems/MagicHudRenderSystem.cs`
- **Type**: IRenderTickable, RenderPhase.Screen
- **Responsibility**: 3 spell slots UI внизу экрана, подсветка активного, имена заклинаний

## Data Flow

```
[ПКМ] → SpellInputSystem
  → проверка MP/кулдаун
  → world.Add(playerId, SpellCastRequest)
  → Mana.MP -= cost
  → SpellSlots.CastCooldown = def.CastCooldown

SpellCastSystem (тот же или следующий кадр)
  → world.Remove<SpellCastRequest>(playerId)
  → Projectile spells: world.CreateEntity(Projectile + Position + Velocity)
  → AoE spells: QueryInto<EnemyTag,Position> → DamageEvents + StatusEffects
  → ShadowStep: Position teleport + world.Add(Invincible)
  → Heal: Health.HP += heal amount

ProjectileSystem
  → движение pos += vel * dt
  → wall check → destroy (+ AoE взрыв если IsAoe)
  → enemy AABB → DamageEvents.Add + StatusEffects apply → destroy

StatusEffectSystem
  → Burn: TickAccumulator → DamageEvents.Add каждые 1s
  → Slow: Add/Remove SlowDebuff на entity
  → Duration <= 0: удалить слот

PhysicsSystem (модифицирован)
  → если Has<SlowDebuff>: vel *= SlowDebuff.Factor

HealthSystem (без изменений)
  → дренирует DamageEvents (от снарядов и Burn)

ManaSystem
  → RegenAccumulator += dt → MP++ каждые 3s
```

## Key Decisions
| Decision | Choice | Rationale |
|----------|--------|-----------|
| StatusEffects storage | Inline 4 slots (Effect0-3) | Zero heap alloc, простая отладка |
| Slow implementation | SlowDebuff component + PhysicsSystem | Одна правка в одном месте vs InputSystem+AISystem |
| Spell definitions | SpellDatabase (JSON + fallback) | Масштабируемость, NativeAOT через JsonSerializable |
| MP bar | В HudRenderSystem | Минимальный diff, аналогия с HP bar |
| Spell slots UI | Отдельный MagicHudRenderSystem | Не раздувать HudRenderSystem |
| Projectile movement | PhysicsSystem двигает (Position+Velocity без Collider), ProjectileSystem проверяет коллизии | Переиспользование существующего движения |
| SpellCast request | Component на entity игрока | Нет дополнительных очередей на GameContext |

## Constraints & Conventions
- Все новые компоненты — struct, каждый в отдельном файле
- Query-буферы как List<int> поля в системах, без LINQ
- GameConfig параметры в отдельном partial class файле
- Namespace: DungeonOfShadows.ECS.Magic / .Components / .Systems
- NativeAOT: SpellsJsonContext для JSON десериализации
- Снаряды — обычные entities, уничтожаются FloorLifecycleSystem при переходе этажа

## Performance Rules
- No allocations in: ProjectileSystem.Tick, StatusEffectSystem.Tick, ManaSystem.Tick, SpellRenderSystem.Tick
- Use inline struct slots for: StatusEffects (Effect0-3)
- Value types preferred for: все компоненты, SpellCastRequest, StatusEffectSlot
- ref var access для всех Get<T> в hot-path системах
- Reusable List<int> query buffers в каждой системе

## Out of Scope
- Спрайты заклинаний (будущая фаза Art)
- Маг-враги с заклинаниями
- Spell upgrades / skill tree
- Spatial hashing для projectile collision
- Стакинг урона Burn (refresh duration only)
