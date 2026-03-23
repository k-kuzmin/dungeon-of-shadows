# Phase 6: SpellScroll Loot + Polish

## Tasks

- [ ] Изменить `src/ECS/Items/ItemDefinition.cs` — добавить `int SpellId` property
- [ ] Изменить `src/ECS/Items/Systems/ItemUseSystem.cs` — ветка TeachSpell в ApplyEffect
- [ ] Добавить SpellScroll предметы в `assets/data/items.json` или LoadFallback (id 400-405)
- [ ] Добавить визуальные цвета для каждого заклинания в SpellRenderSystem
- [ ] Добавить визуал статус-эффектов на врагах в SpellRenderSystem (оранжевое мигание = Burn, синий оверлей = Slow)
- [ ] Проверка: подобрать SpellScroll → использовать → заклинание в слоте → каст работает
- [ ] Финальная проверка всех 6 заклинаний

## Files

| File | Action | Status |
|------|--------|--------|
| src/ECS/Items/ItemDefinition.cs | Modify | ⬜ |
| src/ECS/Items/Systems/ItemUseSystem.cs | Modify | ⬜ |
| src/ECS/Items/ItemDatabase.cs | Modify | ⬜ |
| src/ECS/Magic/Systems/SpellRenderSystem.cs | Modify | ⬜ |
