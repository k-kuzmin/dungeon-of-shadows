# Phase 4: Projectile System

## Tasks

- [ ] Создать `src/ECS/Magic/Components/Projectile.cs`
- [ ] Создать `src/ECS/Magic/Components/AoEVisual.cs`
- [ ] Создать `src/ECS/Magic/Systems/ProjectileSystem.cs`
  - [ ] Движение pos += vel * dt
  - [ ] Wall collision через TileMap.IsWalkable
  - [ ] AABB entity collision (Projectile center vs Enemy Collider)
  - [ ] DamageEvents.Add при попадании
  - [ ] AoE взрыв при IsAoe (Fireball) — QueryInto EnemyTag в радиусе
  - [ ] Lifetime → DestroyEntity
- [ ] Создать `src/ECS/Magic/Systems/SpellRenderSystem.cs` — DrawCircle projectiles, DrawCircleLines AoE
- [ ] Зарегистрировать ProjectileSystem, SpellRenderSystem в ServiceRegistration
- [ ] Проверка: Magic Bolt летит и бьёт врагов, Fireball взрывается AoE

## Files

| File | Action | Status |
|------|--------|--------|
| src/ECS/Magic/Components/Projectile.cs | Create | ⬜ |
| src/ECS/Magic/Components/AoEVisual.cs | Create | ⬜ |
| src/ECS/Magic/Systems/ProjectileSystem.cs | Create | ⬜ |
| src/ECS/Magic/Systems/SpellRenderSystem.cs | Create | ⬜ |
| src/Core/ServiceRegistration.cs | Modify | ⬜ |
