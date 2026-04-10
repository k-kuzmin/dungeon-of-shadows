# Phase 1: Foundation

## Tasks

- [ ] Создать `src/ECS/Magic/SpellEffectType.cs` — enum (None, Burn, Slow)
- [ ] Создать `src/ECS/Magic/SpellId.cs` — enum (MagicBolt=1..Heal=6)
- [ ] Создать `src/ECS/Magic/SpellDefinition.cs` — sealed class с параметрами заклинания
- [ ] Создать `src/ECS/Magic/SpellsJsonContext.cs` — JsonSerializable для NativeAOT
- [ ] Создать `src/ECS/Magic/SpellDatabase.cs` — IStartable, JSON + fallback
- [ ] Создать `assets/data/spells.json` — 6 заклинаний
- [ ] Создать `src/Core/Config/GameConfig.Magic.cs` — все числовые параметры магии
- [ ] Изменить `src/ECS/Combat/Components/Stats.cs` — добавить `int INT`, обновить конструктор
- [ ] Обновить все call-sites Stats конструктора (EnemySpawner, FloorLifecycleSystem)
- [ ] Добавить `SpellScroll` в `src/ECS/Items/ItemType.cs`
- [ ] Добавить `TeachSpell` в `src/ECS/Items/ItemEffectType.cs`
- [ ] Проверка: `dotnet build` компилируется

## Files

| File | Action | Status |
|------|--------|--------|
| src/ECS/Magic/SpellEffectType.cs | Create | ⬜ |
| src/ECS/Magic/SpellId.cs | Create | ⬜ |
| src/ECS/Magic/SpellDefinition.cs | Create | ⬜ |
| src/ECS/Magic/SpellsJsonContext.cs | Create | ⬜ |
| src/ECS/Magic/SpellDatabase.cs | Create | ⬜ |
| assets/data/spells.json | Create | ⬜ |
| src/Core/Config/GameConfig.Magic.cs | Create | ⬜ |
| src/ECS/Combat/Components/Stats.cs | Modify | ⬜ |
| src/ECS/Items/ItemType.cs | Modify | ⬜ |
| src/ECS/Items/ItemEffectType.cs | Modify | ⬜ |
| src/ECS/Combat/EnemySpawner.cs | Modify | ⬜ |
| src/ECS/Exploration/FloorLifecycleSystem.cs | Modify | ⬜ |
