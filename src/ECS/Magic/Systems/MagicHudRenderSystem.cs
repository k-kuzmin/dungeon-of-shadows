using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Magic.Components;
using DungeonOfShadows.ECS.Rendering;

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
    private readonly List<int> _buffer = new();

    public RenderPhase Phase => RenderPhase.Screen;

    private static readonly Color SlotBg = new(30, 30, 40, 200);
    private static readonly Color SlotBorder = new(120, 120, 140, 200);
    private static readonly Color SlotActive = new(220, 180, 50, 255);
    private static readonly Color SlotEmpty = new(80, 80, 80, 150);
    private static readonly Color CooldownOverlay = new(0, 0, 0, 140);
    private static readonly Color HintColor = new(180, 180, 180, 200);
    private static readonly Color ManaCostColor = new(100, 160, 255, 220);
    private static readonly string[] SlotLabels = ["1", "2", "3"];

    // Кеш строк (обновляется при смене спелла в слоте)
    private readonly int[] _cachedSlotSpellIds = [-2, -2, -2];
    private readonly string[] _cachedSlotNames = ["", "", ""];
    private readonly string[] _cachedSlotCosts = ["", "", ""];
    private readonly float[] _cachedSlotMaxCd = [1f, 1f, 1f];

    private const string HintText = "Z/X switch  |  RMB cast";
    private int _hintTextWidth = -1;

    public MagicHudRenderSystem(GameContext ctx, World world, GameConfig config, SpellDatabase spellDb)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
        _spellDb = spellDb;
    }

    public void Tick(float dt)
    {
        _world.QueryInto<PlayerTag, SpellSlots>(_buffer);
        if (_buffer.Count == 0) return;

        int playerId = _buffer[0];
        ref var slots = ref _world.Get<SpellSlots>(playerId);

        int slotW = 52;
        int slotH = 38;
        int gap = 6;
        int totalW = slotW * 3 + gap * 2;
        int startX = _config.ScreenWidth / 2 - totalW / 2;
        int startY = _config.ScreenHeight - slotH - 68;

        // Подсказка по клавишам (кеш ширины)
        if (_hintTextWidth < 0)
            _hintTextWidth = Raylib.MeasureText(HintText, 12);
        Raylib.DrawText(HintText, _config.ScreenWidth / 2 - _hintTextWidth / 2, startY - 16, 12, HintColor);

        for (int i = 0; i < 3; i++)
        {
            int x = startX + i * (slotW + gap);
            int y = startY;
            bool isActive = slots.ActiveSlotIndex == i;
            int spellId = slots.GetSlot(i);

            // Фон слота
            Raylib.DrawRectangle(x, y, slotW, slotH, SlotBg);

            if (spellId < 0)
            {
                // Пустой слот
                Raylib.DrawRectangleLines(x, y, slotW, slotH, isActive ? SlotActive : SlotEmpty);
                int ew = Raylib.MeasureText(SlotLabels[i], 14);
                Raylib.DrawText(SlotLabels[i], x + slotW / 2 - ew / 2, y + slotH / 2 - 7, 14, SlotEmpty);
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
                    }
                }

                // Цветовой индикатор типа
                if (_spellDb.TryGet(spellId, out var def))
                {
                    Color typeColor = GetSpellColor(def.SpellId);
                    Raylib.DrawRectangle(x + 2, y + 2, slotW - 4, 4, typeColor);
                }

                // Имя
                int nw = Raylib.MeasureText(_cachedSlotNames[i], 11);
                Raylib.DrawText(_cachedSlotNames[i], x + slotW / 2 - nw / 2, y + 10, 11, Color.White);

                // Стоимость маны
                int cw = Raylib.MeasureText(_cachedSlotCosts[i], 10);
                Raylib.DrawText(_cachedSlotCosts[i], x + slotW / 2 - cw / 2, y + 24, 10, ManaCostColor);

                // Кулдаун оверлей
                if (isActive && slots.CastCooldown > 0)
                {
                    float cdFrac = Math.Clamp(slots.CastCooldown / _cachedSlotMaxCd[i], 0f, 1f);
                    int cdH = (int)(slotH * cdFrac);
                    Raylib.DrawRectangle(x, y + slotH - cdH, slotW, cdH, CooldownOverlay);
                }

                Raylib.DrawRectangleLines(x, y, slotW, slotH, isActive ? SlotActive : SlotBorder);
            }
        }
    }

    private static Color GetSpellColor(SpellId spellId)
    {
        return spellId switch
        {
            SpellId.MagicBolt => new Color((byte)160, (byte)80, (byte)220, (byte)255),
            SpellId.Fireball => new Color((byte)255, (byte)100, (byte)20, (byte)255),
            SpellId.FrostNova => new Color((byte)100, (byte)180, (byte)255, (byte)255),
            SpellId.ChainLightning => new Color((byte)80, (byte)200, (byte)255, (byte)255),
            SpellId.ShadowStep => new Color((byte)120, (byte)60, (byte)160, (byte)255),
            SpellId.Heal => new Color((byte)50, (byte)220, (byte)80, (byte)255),
            _ => Color.White
        };
    }
}
