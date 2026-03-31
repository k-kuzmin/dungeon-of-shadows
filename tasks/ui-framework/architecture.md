# Architecture: UI Framework

> Last updated: 2026-03-31 | Status: final | Platform: Generic (C# / Raylib-cs / .NET 8)

## Chosen Approach
**Pragmatic Balance** — immediate-mode drawing helpers с lightweight layout builder (`ref struct UiLayout`), который вычисляет `UiRect` struct'ы. Единый rect используется и для отрисовки (через `UiDraw.*`), и для hit-test (`UiRect.Contains`). Тема инжектится через DI. Модалки через modal stack на `UiContext`.

Существующие render-системы рефакторятся in-place, не переписываются.

## Performance Profile

| Component | Hot Path | Allocs/call | Strategy |
|-----------|----------|-------------|----------|
| `UiRect` | yes | 0 | readonly struct, stack-only |
| `UiLayout` | yes | 0 | ref struct, stack-only |
| `UiDraw.*` | yes | 0 | static methods, value-type params |
| `UiTheme` | yes (read) | 0 | class singleton, field access |
| `TextMeasureCache.Measure` (hit) | yes | 0 | Dictionary value lookup |
| `TextMeasureCache.Measure` (miss) | no | 1 string key | only on text change |
| `UiContext.ClearFrame` | yes | 0 | bool set |
| `UiContext.PushModal` | no | 1 Action delegate | rare event |
| `UiInputSystem.Tick` | yes | 0 | bool checks + Raylib calls |

## Components

### UiRect
- **File**: `src/UI/UiRect.cs`
- **Responsibility**: Screen-space bounding box для hit-test и render. Единый примитив, устраняющий дублирование layout/hit-test.
- **Hot path**: yes
- **Interfaces**: `Contains(Vector2)`, `Contains(int,int)`, `ToRaylibRect()`, конструктор `(int x, int y, int w, int h)`

### FlexDirection
- **File**: `src/UI/FlexDirection.cs`
- **Responsibility**: Enum `Row` / `Column` для layout direction.
- **Hot path**: no

### UiLayout
- **File**: `src/UI/UiLayout.cs`
- **Responsibility**: Stateless layout cursor. `ref struct` — компилятор гарантирует stack-only. `Take(w, h)` возвращает `UiRect` и двигает курсор. Поддерживает Row/Column direction и gap.
- **Hot path**: yes
- **Interfaces**:
  - `static UiLayout Row(int x, int y, int gap = 0)` — создаёт горизонтальный layout
  - `static UiLayout Column(int x, int y, int gap = 0)` — вертикальный
  - `UiRect Take(int w, int h)` — забирает прямоугольник, двигает курсор
  - `UiRect TakeSquare(int size)` — shortcut для квадратов (слоты)
  - `void Skip(int amount)` — пропуск (spacer)
  - `UiLayout Nested(int w, int h, FlexDirection dir, int gap)` — вложенный layout внутри текущего rect

### UiTheme
- **File**: `src/UI/UiTheme.cs`
- **Responsibility**: Единый источник всех цветов, размеров шрифтов, отступов. Инициализируется из `GameConfig`.
- **Hot path**: yes (чтение полей)
- **Interfaces**: public readonly поля для цветов по семантическому имени. Методы `HpInterpolated(float fraction)`, `RarityColor(ItemRarity)`, `SpellColor(SpellId)`.

### UiDraw
- **File**: `src/UI/UiDraw.cs`
- **Responsibility**: Статические методы отрисовки поверх Raylib. Каждый метод — переиспользуемый паттерн (bar, slot, panel, tooltip, icon).
- **Hot path**: yes
- **Interfaces**:
  - `Panel(UiRect, Color bg, Color border)`
  - `ProgressBar(UiRect, float fraction, Color bg, Color fill, Color border)`
  - `ProgressBarVertical(UiRect, float fraction, Color bg, Color fill)` — cooldown overlay
  - `Slot(UiRect, Color bg, Color border, bool selected)`
  - `Label(int x, int y, string text, int fontSize, Color color)`
  - `LabelCentered(UiRect, string text, int fontSize, Color color)`
  - `LabelCenteredCached(UiRect, int cachedWidth, string text, int fontSize, Color color)`
  - `Tooltip(UiRect, Color bg, Color border)` — фон тултипа
  - `ItemIcon(UiRect, Texture2D tex, Rectangle src, Color tint)`
  - `static Color RarityColor(ItemRarity)` — вспомогательный, если не через UiTheme

### TextMeasureCache
- **File**: `src/UI/TextMeasureCache.cs`
- **Responsibility**: Кэш `Raylib.MeasureText` результатов по ключу `(string, int fontSize)`. Устраняет повторные native-вызовы для статического текста.
- **Hot path**: yes (lookup), no (miss)
- **Interfaces**: `int Measure(string text, int fontSize)`, `void Invalidate(string text)`

### UiContext
- **File**: `src/UI/UiContext.cs`
- **Responsibility**: Runtime UI state — `InputConsumed` флаг и modal stack. Singleton через DI.
- **Hot path**: no (modal push/pop rare)
- **Interfaces**:
  - `bool InputConsumed` — set by UI, read by gameplay systems
  - `void ClearFrame()` — сброс в начале кадра
  - `void PushModal(ModalDescriptor)`, `void PopModal()`, `bool HasModal`, `ModalDescriptor TopModal`

### ModalDescriptor
- **File**: `src/UI/UiContext.cs` (nested struct)
- **Responsibility**: Данные модального окна — заголовок, текст, кнопки, callbacks.
- **Hot path**: no

### UiInputSystem
- **File**: `src/UI/UiInputSystem.cs`
- **Responsibility**: `ITickable`, регистрируется перед gameplay input системами. Вызывает `ClearFrame()`, обрабатывает modal keyboard input (Esc/Enter), ставит `InputConsumed`.
- **Hot path**: yes
- **Interfaces**: `ITickable`

### GameConfig.Ui
- **File**: `src/Core/Config/GameConfig.Ui.cs`
- **Responsibility**: Все UI magic numbers как `{get; init;}` свойства. Partial class расширение `GameConfig`.
- **Hot path**: no

## Data Flow

```
Frame start
│
├─ UiInputSystem.Tick(dt)                    [ITickable, before gameplay]
│   ├─ _uiCtx.ClearFrame()                  // InputConsumed = false
│   └─ if HasModal → Esc/Enter → InputConsumed = true
│
├─ InventoryInputSystem.Tick(dt)             [ITickable]
│   └─ Tab → toggle ShowInventory            // unchanged
│
├─ CombatInputSystem.Tick(dt)                [ITickable]
│   └─ if _uiCtx.InputConsumed → skip LMB   // NEW gate
│
├─ SpellInputSystem.Tick(dt)                 [ITickable]
│   └─ if _uiCtx.InputConsumed → skip RMB   // NEW gate
│
├─ ... other gameplay ticks ...
│
└─ RenderSystem.Tick(dt)
    └─ Screen phase:
        ├─ HudRenderSystem.Tick(dt)
        │   ├─ layout = UiLayout.Column(barX, barY, gap: 4)
        │   ├─ hpRect = layout.Take(barW, barH)
        │   ├─ UiDraw.ProgressBar(hpRect, hpFrac, theme.BarBg, hpColor, ...)
        │   └─ UiDraw.LabelCenteredCached(hpRect, cachedW, hpText, ...)
        │
        ├─ MagicHudRenderSystem.Tick(dt)
        │   ├─ layout = UiLayout.Row(startX, startY, gap: slotGap)
        │   └─ for i in 0..2: rect = layout.Take(slotW, slotH) → UiDraw.Slot(...)
        │
        └─ InventoryRenderSystem.Tick(dt)
            ├─ DrawQuickSlots: UiLayout.Row → UiDraw.Slot
            ├─ if ShowInventory:
            │   ├─ UiDraw.Panel(panelRect, ...)
            │   ├─ Grid: UiLayout → UiRect → UiDraw.Slot + rect.Contains(mouse) → InputConsumed
            │   ├─ Equip: UiLayout.Column → UiRect.Contains → InputConsumed
            │   └─ Tooltip: UiDraw.Tooltip(...)
            └─ if HasModal: DrawModal(...)
```

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Layout approach | `ref struct UiLayout` (immediate) | Zero alloc by language guarantee, покрывает все текущие use cases |
| Widget abstraction | Нет (статические `UiDraw` хелперы) | Минимальная сложность, существующий код рефакторится, а не переписывается |
| Theme storage | `UiTheme` class singleton (DI) | Позволяет инициализацию из GameConfig, единая точка правки |
| Input routing | `UiInputSystem` + `InputConsumed` на `UiContext` | Решает click-through; gameplay системы проверяют флаг |
| Click handling inventory | Остаётся в InventoryRenderSystem | Rect уже вычислен для draw — не дублируем; ставим InputConsumed |
| Modal stack | `UiContext` с `Stack<ModalDescriptor>` | Стек нужен (решение пользователя), UiContext — естественное место |
| Config | `GameConfig.Ui.cs` partial | Консистентно с существующим паттерном |
| MeasureText cache | `TextMeasureCache` singleton | Устраняет ~10 лишних native calls/frame |

## Constraints & Conventions

- `ref struct` не может быть полем класса и не захватывается лямбдами — это by design
- Передача через `ref UiLayout` в helper-методы render систем
- Все методы `UiDraw` — `public static`, без состояния
- `UiTheme` зависит от domain-типов (`ItemRarity`, `SpellId`) — UI зависит от домена, не наоборот
- Регистрация `UiInputSystem` ПЕРЕД `InventoryInputSystem` в `ServiceRegistration.cs`
- NativeAOT: без reflection, без `typeof(T)` на hot path, без delegates на hot path

## Performance Rules

- No allocations in: `UiLayout.*`, `UiDraw.*`, `UiRect.*`, `TextMeasureCache.Measure` (cache hit), `UiInputSystem.Tick`, `UiContext.ClearFrame`
- Value types preferred for: `UiRect`, `UiLayout`, `FlexDirection`, `ModalDescriptor`
- Cache text widths via `TextMeasureCache` — never call raw `MeasureText` from render systems
- No LINQ in any UI code
- No string concat on hot paths — pre-build strings on value change, pass to `UiDraw.Label*`

## Out of Scope

- ScrollList (явно исключён пользователем)
- Data-driven UI (JSON layout descriptions)
- Визуальный редактор
- Анимации/tweening виджетов
- Retained widget tree / virtual dispatch на hot path
- World-space UI (enemy HP bars, damage numbers) — остаются как есть в CombatRenderSystem/SpellRenderSystem
