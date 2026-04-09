using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Magic.Components;
using DungeonOfShadows.ECS.Rendering;
using DungeonOfShadows.UI;

namespace DungeonOfShadows.ECS.Magic.Systems;

/// <summary>
/// Рисует панель заклинаний: N слотов внизу экрана, подсветка активного, уровень. Screen-phase.
/// </summary>
public class MagicHudRenderSystem : IRenderTickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly SpellDatabase _spellDb;
    private readonly UiTheme _theme;
    private readonly TextMeasureCache _textCache;
    private readonly List<int> _buffer = new();

    public RenderPhase Phase => RenderPhase.Screen;

    private const string HintText = "Z/X switch  |  RMB cast";
    private int _hintTextWidth = -1;

    // Кеш строк — фиксированный массив по MaxCapacity
    private readonly int _maxSlots;
    private readonly int[] _cachedSlotSpellIds;
    private readonly int[] _cachedSlotLevels;
    private readonly string[] _cachedSlotNames;
    private readonly string[] _cachedSlotCosts;
    private readonly string[] _cachedSlotLevelStrs;
    private readonly int[] _cachedNameWidths;
    private readonly int[] _cachedCostWidths;
    private readonly int[] _cachedLevelWidths;
    private readonly SpellId[] _cachedSlotSpellEnum;

    public MagicHudRenderSystem(GameContext ctx, World world, GameConfig config,
        SpellDatabase spellDb, UiTheme theme, TextMeasureCache textCache)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
        _spellDb = spellDb;
        _theme = theme;
        _textCache = textCache;

        _maxSlots = config.SpellSlotMaxCapacity;
        _cachedSlotSpellIds = new int[_maxSlots];
        _cachedSlotLevels = new int[_maxSlots];
        _cachedSlotNames = new string[_maxSlots];
        _cachedSlotCosts = new string[_maxSlots];
        _cachedSlotLevelStrs = new string[_maxSlots];
        _cachedNameWidths = new int[_maxSlots];
        _cachedCostWidths = new int[_maxSlots];
        _cachedLevelWidths = new int[_maxSlots];
        _cachedSlotSpellEnum = new SpellId[_maxSlots];

        for (int i = 0; i < _maxSlots; i++)
        {
            _cachedSlotSpellIds[i] = -2;
            _cachedSlotLevels[i] = -1;
            _cachedSlotNames[i] = "";
            _cachedSlotCosts[i] = "";
            _cachedSlotLevelStrs[i] = "";
            _cachedSlotSpellEnum[i] = SpellId.None;
        }
    }

    public void Tick(float dt)
    {
        _world.QueryInto<PlayerTag, SpellSlots>(_buffer);
        if (_buffer.Count == 0) return;

        int playerId = _buffer[0];
        ref var slots = ref _world.Get<SpellSlots>(playerId);
        int cap = slots.Capacity;
        if (cap <= 0) return;

        int slotW = _config.SpellSlotWidth;
        int slotH = _config.SpellSlotHeight;
        int gap = _config.SpellSlotGap;
        int totalW = slotW * cap + gap * (cap - 1);
        int startX = _config.ScreenWidth / 2 - totalW / 2;
        int startY = _config.ScreenHeight - slotH - _config.SpellSlotBottomOffset;

        // Подсказка по клавишам
        if (_hintTextWidth < 0)
            _hintTextWidth = _textCache.Measure(HintText, _config.SpellHintFontSize);
        Raylib.DrawText(HintText, _config.ScreenWidth / 2 - _hintTextWidth / 2,
            startY - 16, _config.SpellHintFontSize, _theme.HintColor);

        var layout = UiLayout.Row(startX, startY, gap);

        for (int i = 0; i < cap; i++)
        {
            var rect = layout.Take(slotW, slotH);
            bool isActive = slots.ActiveSlotIndex == i;
            var slot = slots.GetSlot(i);
            int spellId = slot.SpellId;
            int level = slot.Level;

            // Фон слота
            UiDraw.PanelFilled(rect, _theme.SlotBg);

            if (spellId < 0)
            {
                // Пустой слот
                Raylib.DrawRectangleLines(rect.X, rect.Y, rect.W, rect.H,
                    isActive ? _theme.SlotActive : _theme.SlotEmpty);
            }
            else
            {
                // Обновляем кеш строк при смене спелла или уровня
                if (_cachedSlotSpellIds[i] != spellId || _cachedSlotLevels[i] != level)
                {
                    _cachedSlotSpellIds[i] = spellId;
                    _cachedSlotLevels[i] = level;
                    if (_spellDb.TryGet(spellId, out var cachedDef))
                    {
                        _cachedSlotNames[i] = cachedDef.Name.Length > 8 ? cachedDef.Name[..8] : cachedDef.Name;
                        _cachedSlotCosts[i] = $"{cachedDef.ManaCost} MP";
                        _cachedSlotLevelStrs[i] = level > 0 ? $"Lv.{level}" : "";
                        _cachedSlotSpellEnum[i] = cachedDef.SpellId;
                        _cachedNameWidths[i] = _textCache.Measure(_cachedSlotNames[i], _config.SpellNameFontSize);
                        _cachedCostWidths[i] = _textCache.Measure(_cachedSlotCosts[i], _config.SpellCostFontSize);
                        _cachedLevelWidths[i] = _textCache.Measure(_cachedSlotLevelStrs[i], _config.SpellCostFontSize);
                    }
                }

                // Цветовой индикатор типа (из кеша, без TryGet на hot path)
                {
                    Color typeColor = UiTheme.SpellColor(_cachedSlotSpellEnum[i]);
                    int pad = _config.SpellTypeBarPadding;
                    Raylib.DrawRectangle(rect.X + pad, rect.Y + pad,
                        rect.W - pad * 2, _config.SpellTypeBarHeight, typeColor);
                }

                // Имя (центрировано)
                Raylib.DrawText(_cachedSlotNames[i],
                    rect.CenterX - _cachedNameWidths[i] / 2, rect.Y + 8,
                    _config.SpellNameFontSize, _theme.TextWhite);

                // Стоимость маны (слева от центра)
                int infoY = rect.Y + 20;
                int totalInfoW = _cachedCostWidths[i];
                if (_cachedLevelWidths[i] > 0)
                    totalInfoW += 4 + _cachedLevelWidths[i];
                int infoX = rect.CenterX - totalInfoW / 2;

                Raylib.DrawText(_cachedSlotCosts[i], infoX, infoY,
                    _config.SpellCostFontSize, _theme.ManaCostColor);

                // Уровень (справа от маны)
                if (_cachedLevelWidths[i] > 0)
                {
                    Raylib.DrawText(_cachedSlotLevelStrs[i],
                        infoX + _cachedCostWidths[i] + 4, infoY,
                        _config.SpellCostFontSize, Color.Gold);
                }

                // Кулдаун оверлей на всех слотах (общий CastCooldown / MaxCastCooldown)
                if (slots.CastCooldown > 0)
                {
                    float cdFrac = Math.Clamp(slots.CastCooldown / slots.MaxCastCooldown, 0f, 1f);
                    UiDraw.ProgressBarVertical(rect, cdFrac, Color.Blank, _theme.CooldownOverlay);
                }

                Raylib.DrawRectangleLines(rect.X, rect.Y, rect.W, rect.H,
                    isActive ? _theme.SlotActive : _theme.SlotBorder);
            }
        }
    }
}
