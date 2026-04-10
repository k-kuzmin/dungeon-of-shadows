# Phase 5: Status Effects

## Tasks

- [ ] Создать `src/ECS/Magic/Components/StatusEffects.cs` + StatusEffectSlot inner struct
- [ ] Создать `src/ECS/Magic/Components/SlowDebuff.cs`
- [ ] Создать `src/ECS/Magic/Systems/StatusEffectSystem.cs`
  - [ ] Tick Burn DoT → DamageEvents.Add каждые 1s
  - [ ] Manage SlowDebuff component (add/remove)
  - [ ] Duration expiry → удалить слот, сдвинуть массив
  - [ ] Refresh same-type effects (обновить Duration, не стаковать)
- [ ] Изменить `src/ECS/Physics/PhysicsSystem.cs` — SlowDebuff scale velocity
- [ ] Подключить StatusEffects apply в ProjectileSystem (Fireball → Burn, FrostNova → Slow)
- [ ] Зарегистрировать StatusEffectSystem в ServiceRegistration
- [ ] Проверка: враг горит (DamageNumbers тикают), замедление работает

## Files

| File | Action | Status |
|------|--------|--------|
| src/ECS/Magic/Components/StatusEffects.cs | Create | ⬜ |
| src/ECS/Magic/Components/SlowDebuff.cs | Create | ⬜ |
| src/ECS/Magic/Systems/StatusEffectSystem.cs | Create | ⬜ |
| src/ECS/Physics/PhysicsSystem.cs | Modify | ⬜ |
| src/ECS/Magic/Systems/ProjectileSystem.cs | Modify | ⬜ |
| src/ECS/Magic/Systems/SpellCastSystem.cs | Modify | ⬜ |
| src/Core/ServiceRegistration.cs | Modify | ⬜ |
