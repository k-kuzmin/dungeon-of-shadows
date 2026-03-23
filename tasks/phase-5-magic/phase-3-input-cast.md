# Phase 3: Spell Input + Cast System

## Tasks

- [ ] Создать `src/ECS/Magic/Components/SpellCastRequest.cs`
- [ ] Создать `src/ECS/Magic/Systems/SpellInputSystem.cs` — ПКМ каст, Q/E/wheel переключение, проверка MP/кулдауна
- [ ] Создать `src/ECS/Magic/Systems/SpellCastSystem.cs` — дренирует SpellCastRequest, спавн эффектов
  - [ ] CastMagicBolt — спавн Projectile entity
  - [ ] CastHeal — Health.HP += heal amount
  - [ ] CastShadowStep — Position teleport + Invincible(0.3s)
  - [ ] CastFrostNova — мгновенный AoE + Slow
  - [ ] CastFireball — спавн Projectile с IsAoe=true
  - [ ] CastChainLightning — итеративный поиск ближайших врагов
- [ ] Создать `src/ECS/Magic/Systems/MagicHudRenderSystem.cs` — 3 spell slots UI
- [ ] Зарегистрировать SpellInputSystem, SpellCastSystem, MagicHudRenderSystem в ServiceRegistration
- [ ] Проверка: Q/E меняют активный слот, ПКМ тратит ману

## Files

| File | Action | Status |
|------|--------|--------|
| src/ECS/Magic/Components/SpellCastRequest.cs | Create | ⬜ |
| src/ECS/Magic/Systems/SpellInputSystem.cs | Create | ⬜ |
| src/ECS/Magic/Systems/SpellCastSystem.cs | Create | ⬜ |
| src/ECS/Magic/Systems/MagicHudRenderSystem.cs | Create | ⬜ |
| src/Core/ServiceRegistration.cs | Modify | ⬜ |
