using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Magic.Components;
using DungeonOfShadows.ECS.Rendering;
using DungeonOfShadows.UI;

namespace DungeonOfShadows.ECS.Magic.Systems;

/// <summary>
/// Рисует панель заклинаний: 3 слота внизу экрана, подсветка активного. Screen-phase.
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

    private static readonly string[] SlotLabels = ["1", "2", "3"];
    private const string HintText = "Z/X switch  |  RMB cast";
    private int _hintTextWidth = -1;

    // Кеш строк (обновляется при смене спелла в слоте)
    private readonly int[] _cachedSlotSpellIds = [-2, -2, -2];
    private readonly string[] _cachedSlotNames = ["", "", ""];
    private readonly string[] _cachedSlotCosts = ["", "", ""];
    private readonly float[] _cachedSlotMaxCd = [1f, 1f, 1f];
    private readonly int[] _cachedNameWidths = [0, 0, 0];
    private readonly int[] _cachedCostWidths = [0, 0, 0];

    public MagicHudRenderSystem(GameContext ctx, World world, GameConfig config,
        SpellDatabase spellDb, UiTheme theme, TextMeasureCache textCache)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
        _spellDb = spellDb;
        _theme = theme;
        _textCache = textCache;
    }

    public void Tick(float dt)
    {
        _world.QueryInto<PlayerTag, SpellSlots>(_buffer);
        if (_buffer.Count == 0) return;

        int playerId = _buffer[0];
        ref var slots = ref _world.Get<SpellSlots>(playerId);

        int slotW = _config.SpellSlotWidth;
        int slotH = _config.SpellSlotHeight;
        int gap = _config.SpellSlotGap;
        int totalW = slotW * 3 + gap * 2;
        int startX = _config.ScreenWidth / 2 - totalW / 2;
        int startY = _config.ScreenHeight - slotH - _config.SpellSlotBottomOffset;

        // Подсказка по клавишам
        if (_hintTextWidth < 0)
            _hintTextWidth = _textCache.Measure(HintText, _config.SpellHintFontSize);
        Raylib.DrawText(HintText, _config.ScreenWidth / 2 - _hintTextWidth / 2,
            startY - 16, _config.SpellHintFontSize, _theme.HintColor);

        var layout = UiLayout.Row(startX, startY, gap);

        for (int i = 0; i < 3; i++)
        {
            var rect = layout.Take(slotW, slotH);
            bool isActive = slots.ActiveSlotIndex == i;
            int spellId = slots.GetSlot(i);

            // Фон слота
            UiDraw.PanelFilled(rect, _theme.SlotBg);

            if (spellId < 0)
            {
                // Пустой слот
                Raylib.DrawRectangleLines(rect.X, rect.Y, rect.W, rect.H,
                    isActive ? _theme.SlotActive : _theme.SlotEmpty);
                UiDraw.LabelCentered(rect, SlotLabels[i], 14, _theme.SlotEmpty, _textCache);
            }
            else
            {
                // Обновляем кеш строк при смене спелла
                if (_cachedSlotSpellIds[i] != spellId)
                {
                    _cachedSlotSpellIds[i] = spellId;
                    if (_spellDb.TryGet(spellId, out var cachedDef))
                    {
                        _cachedSlotNames[i] = cachedDef.Name.Length > 8 ? cachedDef.Name[..8] : cachedDef.Name;
                        _cachedSlotCosts[i] = cachedDef.ManaCost + " MP";
                        _cachedSlotMaxCd[i] = cachedDef.CastCooldown > 0 ? cachedDef.CastCooldown : 1f;
                        _cachedNameWidths[i] = _textCache.Measure(_cachedSlotNames[i], _config.SpellNameFontSize);
                        _cachedCostWidths[i] = _textCache.Measure(_cachedSlotCosts[i], _config.SpellCostFontSize);
                    }
                }

                // Цветовой индикатор типа
                if (_spellDb.TryGet(spellId, out var def))
                {
                    Color typeColor = UiTheme.SpellColor(def.SpellId);
                    int pad = _config.SpellTypeBarPadding;
                    Raylib.DrawRectangle(rect.X + pad, rect.Y + pad,
                        rect.W - pad * 2, _config.SpellTypeBarHeight, typeColor);
                }

                // Имя (центрировано)
                Raylib.DrawText(_cachedSlotNames[i],
                    rect.CenterX - _cachedNameWidths[i] / 2, rect.Y + 10,
                    _config.SpellNameFontSize, _theme.TextWhite);

                // Стоимость маны (центрировано)
                Raylib.DrawText(_cachedSlotCosts[i],
                    rect.CenterX - _cachedCostWidths[i] / 2, rect.Y + 24,
                    _config.SpellCostFontSize, _theme.ManaCostColor);

                // Кулдаун оверлей (снизу вверх)
                if (isActive && slots.CastCooldown > 0)
                {
                    float cdFrac = Math.Clamp(slots.CastCooldown / _cachedSlotMaxCd[i], 0f, 1f);
                    UiDraw.ProgressBarVertical(rect, cdFrac, Color.Blank, _theme.CooldownOverlay);
                }

                Raylib.DrawRectangleLines(rect.X, rect.Y, rect.W, rect.H,
                    isActive ? _theme.SlotActive : _theme.SlotBorder);
            }
        }
    }
}
