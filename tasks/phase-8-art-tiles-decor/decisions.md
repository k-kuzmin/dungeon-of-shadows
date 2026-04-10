# Decisions: Phase 8 Art — Tiles & Decor

## Clarifications (Phase 1)
### Autotiling method
- **Question**: 4-bit (16 вариантов) или 8-bit blob (47/256)?
- **Answer**: 8-bit blob — красивее углы
- **Impact**: нужна bitmask-таблица для маппинга 256 -> ~47 уникальных тайлов

### Animation culling
- **Question**: лимит анимированных тайлов?
- **Answer**: не анимировать то, что вне камеры
- **Impact**: анимация только для видимых тайлов, проверка frustum перед обновлением кадра

### Decoration expansion
- **Question**: сохранить текущие типы декора или расширить?
- **Answer**: расширить — добавить бочки, ящики, мешки из Objects.png
- **Impact**: расширить DecorationType enum, обновить DecorationPainter

### Chest animation
- **Question**: добавлять анимацию открытия сундуков?
- **Answer**: да, из doors_lever_chest_animation.png
- **Impact**: добавить состояние анимации в Chest компонент, обновить рендер

### Autotile mapping source
- **Question**: откуда брать маппинг bitmask → тайл?
- **Answer**: TMX файл из Tiled (Dungeon1.tmx) + PSD исходники
- **Impact**: walls_floor firstgid=377, 17 cols. Маппинг строим вручную по визуальному анализу спрайтшита

### Spritesheet versions
- **Question**: какие версии спрайтов использовать?
- **Answer**: Tiled-версии из исходного asset pack (полные, не обрезанные)
- **Impact**: walls_floor 272x464 (17x29), Objects 384x144 (24x9), doors_lever_chest 160x240 (10x15). Нужно скопировать Tiled-версии в проект

### Decoration objects collision
- **Question**: бочки, ящики, мешки проходимые или блокирующие?
- **Answer**: с коллайдерами (блокирующие)
- **Impact**: декор-объекты становятся ECS-сущностями с Position+Collider, а не записями в DecorationType[,]

### Torch rendering
- **Question**: факелы 32x48 поверх стены или 16x16?
- **Answer**: поверх стены (32x48 из fire_animation.png)
- **Impact**: факелы рисуются с overflow за пределы тайла, нужен отдельный слой рендера поверх стен

### Explored tile decorations
- **Question**: рисовать декор на explored тайлах?
- **Answer**: нет, оставляем как есть
- **Impact**: без изменений в логике visibility

### Doors
- **Question**: включать двери в scope?
- **Answer**: на потом
- **Impact**: исключаем из текущей задачи
