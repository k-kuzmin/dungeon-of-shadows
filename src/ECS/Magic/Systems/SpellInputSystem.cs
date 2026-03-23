using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Magic.Components;

namespace DungeonOfShadows.ECS.Magic.Systems;

/// <summary>
/// Обрабатывает ввод заклинаний: ПКМ — каст, Z/X — переключение слотов, колесо мыши.
/// Тик кулдауна каста заклинаний.
/// </summary>
public class SpellInputSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly SpellDatabase _spellDb;
    private readonly List<int> _buffer = new();

    public SpellInputSystem(GameContext ctx, World world, GameConfig config, SpellDatabase spellDb)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
        _spellDb = spellDb;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _world;
        world.QueryInto<PlayerTag, SpellSlots>(_buffer);
        if (_buffer.Count == 0) return;

        int playerId = _buffer[0];
        ref var slots = ref world.Get<SpellSlots>(playerId);

        // Тик кулдауна каста
        if (slots.CastCooldown > 0f)
            slots.CastCooldown = MathF.Max(0f, slots.CastCooldown - dt);

        if (_ctx.ShowInventory) return;

        // Z — предыдущий слот
        if (Raylib.IsKeyPressed(KeyboardKey.Z))
            slots.ActiveSlotIndex = (slots.ActiveSlotIndex + 2) % 3;

        // X — следующий слот
        if (Raylib.IsKeyPressed(KeyboardKey.X))
            slots.ActiveSlotIndex = (slots.ActiveSlotIndex + 1) % 3;

        // Колесо мыши — переключение слотов
        float wheel = Raylib.GetMouseWheelMove();
        if (wheel > 0)
            slots.ActiveSlotIndex = (slots.ActiveSlotIndex + 2) % 3;
        else if (wheel < 0)
            slots.ActiveSlotIndex = (slots.ActiveSlotIndex + 1) % 3;

        // ПКМ — каст активного заклинания
        if (Raylib.IsMouseButtonPressed(MouseButton.Right))
        {
            if (world.Has<SpellCastRequest>(playerId)) return;
            if (slots.CastCooldown > 0f) return;

            int spellId = slots.GetActiveSpellId();
            if (spellId < 0) return;

            if (!_spellDb.TryGet(spellId, out var def)) return;

            if (!world.Has<Mana>(playerId)) return;
            ref var mana = ref world.Get<Mana>(playerId);
            if (mana.MP < def.ManaCost)
            {
                _ctx.UiMessage = "Not enough mana";
                _ctx.UiMessageTimer = _config.UiMessageSeconds;
                return;
            }

            // Вычисляем цель в мировых координатах
            var mouseScreen = Raylib.GetMousePosition();
            var mouseWorld = Raylib.GetScreenToWorld2D(mouseScreen, _ctx.Camera);

            mana.MP -= def.ManaCost;
            slots.CastCooldown = def.CastCooldown;

            world.Add(playerId, new SpellCastRequest
            {
                SpellId = spellId,
                TargetX = mouseWorld.X,
                TargetY = mouseWorld.Y
            });
        }
    }
}
