using Raylib_cs;
using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;
using DungeonOfShadows.ECS.Items;
using DungeonOfShadows.UI;

namespace DungeonOfShadows.ECS.Rendering;

public class InteractionPromptRenderSystem : IRenderTickable
{
    public RenderPhase Phase => RenderPhase.World;

    private readonly World _world;
    private readonly GameContext _ctx;
    private readonly GameConfig _config;
    private readonly TextMeasureCache _textCache;

    private readonly List<int> _playerBuf = new();
    private readonly List<int> _chestBuf = new();
    private readonly List<int> _itemBuf = new();

    private const string HintOpen = "[E] Open";
    private const string HintPickup = "[E] Pick up";
    private const string HintDescend = "[E] Descend";

    private static readonly Color BgColor = new(0, 0, 0, 160);
    private static readonly Color TextColor = new(255, 255, 255, 230);

    public InteractionPromptRenderSystem(World world, GameContext ctx, GameConfig config, TextMeasureCache textCache)
    {
        _world = world;
        _ctx = ctx;
        _config = config;
        _textCache = textCache;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _world;
        int ts = _config.ScaledTileSize;

        world.QueryInto<PlayerTag, Position>(_playerBuf);
        if (_playerBuf.Count == 0) return;

        int playerId = _playerBuf[0];
        ref var playerPos = ref world.Get<Position>(playerId);

        float bestDistSq = float.MaxValue;
        float bestX = 0f;
        float bestY = 0f;
        string? bestHint = null;

        // --- Сундуки ---
        float chestMax = _config.ChestInteractRadiusTiles * ts;
        float chestMaxSq = chestMax * chestMax;

        world.QueryInto<Chest, Position>(_chestBuf);
        for (int i = 0; i < _chestBuf.Count; i++)
        {
            int id = _chestBuf[i];
            ref var chest = ref world.Get<Chest>(id);
            if (chest.Opened) continue;

            ref var pos = ref world.Get<Position>(id);
            float dx = pos.X - playerPos.X;
            float dy = pos.Y - playerPos.Y;
            float distSq = dx * dx + dy * dy;
            if (distSq <= chestMaxSq && distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestX = pos.X;
                bestY = pos.Y;
                bestHint = HintOpen;
            }
        }

        // --- Предметы на земле ---
        float itemMax = _config.ItemPickupRadiusTiles * ts;
        float itemMaxSq = itemMax * itemMax;

        world.QueryInto<ItemOnGround, Position>(_itemBuf);
        for (int i = 0; i < _itemBuf.Count; i++)
        {
            int id = _itemBuf[i];
            ref var pos = ref world.Get<Position>(id);
            float dx = pos.X - playerPos.X;
            float dy = pos.Y - playerPos.Y;
            float distSq = dx * dx + dy * dy;
            if (distSq <= itemMaxSq && distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestX = pos.X;
                bestY = pos.Y;
                bestHint = HintPickup;
            }
        }

        // --- Лестница (тайловая проверка) ---
        int ptx = (int)((playerPos.X + ts / 2f) / ts);
        int pty = (int)((playerPos.Y + ts / 2f) / ts);
        var map = _ctx.Map;

        if (map.InBounds(ptx, pty) && map.Tiles[ptx, pty].Type == TileType.StairDown)
        {
            // Дистанция 0 — лестница всегда выигрывает при прочих равных
            float stairX = ptx * ts;
            float stairY = pty * ts;
            float dx = stairX - playerPos.X;
            float dy = stairY - playerPos.Y;
            float distSq = dx * dx + dy * dy;
            if (distSq < bestDistSq)
            {
                bestX = stairX;
                bestY = stairY;
                bestHint = HintDescend;
            }
        }

        if (bestHint == null) return;

        // Проверка видимости (FOV)
        int tx = (int)((bestX + ts / 2f) / ts);
        int ty = (int)((bestY + ts / 2f) / ts);
        if (map.InBounds(tx, ty) && map.Tiles[tx, ty].Visibility < 2)
            return;

        // Отрисовка подсказки над объектом
        int fontSize = _config.InteractionHintFontSize;
        int padding = _config.InteractionHintPadding;
        int textW = _textCache.Measure(bestHint, fontSize);

        int drawX = (int)(bestX + ts / 2f - (textW + padding * 2) / 2f);
        int drawY = (int)(bestY - fontSize - padding * 2 - _config.InteractionHintOffsetY);

        Raylib.DrawRectangle(drawX, drawY, textW + padding * 2, fontSize + padding * 2, BgColor);
        Raylib.DrawText(bestHint, drawX + padding, drawY + padding, fontSize, TextColor);
    }
}
