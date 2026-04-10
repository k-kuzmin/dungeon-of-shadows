# Phase 2: Mana + HUD

## Tasks

- [ ] Создать `src/ECS/Magic/Components/Mana.cs`
- [ ] Создать `src/ECS/Magic/Components/SpellSlots.cs`
- [ ] Создать `src/ECS/Magic/Systems/ManaSystem.cs` — регенерация MP, тик CastCooldown
- [ ] Изменить `src/ECS/Exploration/FloorLifecycleSystem.cs` — SpawnPlayer добавить Mana + SpellSlots + INT=5
- [ ] Изменить `src/ECS/Rendering/HudRenderSystem.cs` — DrawPlayerMpBar() под HP баром
- [ ] Зарегистрировать ManaSystem в `src/Core/ServiceRegistration.cs`
- [ ] Проверка: `dotnet run` — MP бар виден, регенерируется

## Files

| File | Action | Status |
|------|--------|--------|
| src/ECS/Magic/Components/Mana.cs | Create | ⬜ |
| src/ECS/Magic/Components/SpellSlots.cs | Create | ⬜ |
| src/ECS/Magic/Systems/ManaSystem.cs | Create | ⬜ |
| src/ECS/Exploration/FloorLifecycleSystem.cs | Modify | ⬜ |
| src/ECS/Rendering/HudRenderSystem.cs | Modify | ⬜ |
| src/Core/ServiceRegistration.cs | Modify | ⬜ |
