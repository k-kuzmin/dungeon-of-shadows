# Фаза 5: Magic

## Описание

Система магии: ресурс маны, 6 заклинаний с различными механиками (проджектайлы, AoE, телепорт, хил), нахождение заклинаний через свитки, слоты заклинаний.

## Статус: Завершено ✓

---

## Что нужно сделать

### 5.1 Система маны
- [x] Компонент `Mana` (MP, maxMP, RegenAccumulator)
- [x] Базовая регенерация: 1 MP / 3с (ManaSystem)
- [x] Полоска MP в HUD (HudRenderSystem)
- [x] Проверка достаточности маны перед кастом (SpellInputSystem)

### 5.2 Система заклинаний
- [x] SpellDefinition (JSON) + SpellDatabase (IStartable, JSON + fallback)
- [x] SpellSlots — 3 слота заклинаний, переключение Z/X/wheel
- [x] Каст заклинания: ПКМ (активное заклинание)
- [x] Кулдаун между кастами (SpellInputSystem тикает CastCooldown)
- [x] Нахождение заклинаний через SpellScroll (ItemType.SpellScroll + ItemEffectType.TeachSpell)

### 5.3 Проджектайлы
- [x] ProjectileSystem: коллизии со стенами/врагами, lifetime, destroy
- [x] Компонент `Projectile` (OwnerId, Damage, IsAoe, OnHitEffect, Chain...)
- [x] Движение через PhysicsSystem (Position+Velocity без Collider)
- [x] AABB коллизия (центр снаряда vs Collider врага)

### 5.4 Заклинания

#### Magic Bolt (3 MP)
- [x] Проджектайл в направлении мыши
- [x] Урон = BaseDamage + INT × IntScaling
- [x] Визуал: фиолетовый круг + glow

#### Fireball (8 MP)
- [x] Проджектайл → AoE взрыв при попадании (враг ИЛИ стена)
- [x] AoE урон (50% от прямого) + Burn DoT
- [x] Визуал: оранжевый круг + AoE кольцо

#### Frost Nova (6 MP)
- [x] Мгновенный AoE вокруг игрока
- [x] Урон + Slow 60% на 3с
- [x] Визуал: голубое расширяющееся кольцо

#### Chain Lightning (10 MP)
- [x] Проджектайл → прыжок между ближайшими врагами (до ChainMaxTargets)
- [x] Поиск ближайших целей в ChainRadius
- [x] Визуал: голубой круг

#### Shadow Step (5 MP)
- [x] Телепорт на ShadowStepTiles в направлении мыши
- [x] i-frames 0.3с (Invincible компонент)
- [x] Проверка на проходимость (ray march по тайлам)
- [x] Визуал: фиолетовое кольцо в точке старта

#### Heal (7 MP)
- [x] Восстановление BaseHeal + INT × HealIntScaling HP
- [x] Визуал: зелёное расширяющееся кольцо

### 5.5 Статус-эффекты
- [x] StatusEffectSystem: Burn DoT (каждые BurnTickInterval), Slow
- [x] StatusEffects — inline 4 слота (Effect0–Effect3), zero alloc
- [x] SlowDebuff компонент → PhysicsSystem масштабирует velocity
- [x] Refresh same-type эффектов (не стак)
- [x] Визуал: цветные индикаторы над врагами (оранжевый=Burn, голубой=Slow)

### 5.6 HUD
- [x] MagicHudRenderSystem — 3 spell slots, подсветка активного, cooldown overlay
- [x] MP бар в HudRenderSystem (под HP)
- [x] Hint клавиш (Z/X switch | RMB cast)

---

## Что сделано

**Полная реализация** — 19 новых файлов, 15 модифицированных.

Ключевые архитектурные решения:
- **Pragmatic подход**: SpellDatabase (JSON + fallback), inline StatusEffects, SlowDebuff через PhysicsSystem
- **Z/X для spell slots** (E занята взаимодействием)
- **SpellCastRequest как компонент** на entity игрока
- **MagicHelper** — статические утилиты (SpawnAoEVisual, ApplyStatusEffect, TryLearnSpell)
- **Снаряды — обычные entities** (Position+Velocity без Collider), PhysicsSystem двигает

Quality review (Phase 6) применён: namespace fix, component logic refactor, shared helpers, bug fixes (NullRef, div/0, transient cleanup), perf fixes (string caching).

Подробная документация: `tasks/phase-5-magic/` (architecture.md, decisions.md, context.md).

---

## Зависимости

- **Фаза 1 (Core)** — ECS, game loop
- **Фаза 3 (Combat)** — формулы урона, статы, DamageEvents
- **Фаза 4 (Items)** — свитки как лут, быстрые слоты, ItemUseSystem

## Результат фазы

Игрок находит и использует 6 заклинаний, тратит ману, применяет AoE и проджектайлы. Враги получают статус-эффекты (поджог, замедление). Полоска маны и слоты заклинаний в HUD.
