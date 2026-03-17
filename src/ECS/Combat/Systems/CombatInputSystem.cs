using Raylib_cs;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Combat.Systems;

/// <summary>
/// Обрабатывает боевой ввод: ЛКМ — атака мечом в сторону мыши, Shift — дэш.
/// </summary>
public class CombatInputSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly List<int> _queryBuffer = new();

    public CombatInputSystem(GameContext ctx, World world, GameConfig config)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _world;
        var config = _config;
        int ts = config.ScaledTileSize;

        world.QueryInto<PlayerTag, Position>(_queryBuffer);
        if (_queryBuffer.Count == 0) return;

        int playerId = _queryBuffer[0];
        ref var pos = ref world.Get<Position>(playerId);

        float playerCenterX = pos.X + ts / 2f;
        float playerCenterY = pos.Y + ts / 2f;

        // ЛКМ — атака мечом
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) &&
            !world.Has<MeleeAttack>(playerId) &&
            !world.Has<MeleeCooldown>(playerId))
        {
            var mouseScreen = Raylib.GetMousePosition();
            var mouseWorld = Raylib.GetScreenToWorld2D(mouseScreen, _ctx.Camera);

            float dx = mouseWorld.X - playerCenterX;
            float dy = mouseWorld.Y - playerCenterY;
            float len = MathF.Sqrt(dx * dx + dy * dy);

            if (len > 0.01f)
            {
                dx /= len;
                dy /= len;
            }
            else
            {
                // Если мышь точно на игроке — атакуем в направлении взгляда
                ref var player = ref world.Get<PlayerTag>(playerId);
                dx = player.FacingX;
                dy = player.FacingY;
            }

            world.Add(playerId, new MeleeAttack
            {
                DirX = dx,
                DirY = dy,
                ArcRadians = config.PlayerMeleeArcRadians,
                Radius = config.PlayerMeleeRange * ts,
                TimeRemaining = config.PlayerMeleeDuration,
                Duration = config.PlayerMeleeDuration,
                HitScanned = false,
                AlreadyHit = new List<int>()
            });
            world.Add(playerId, new MeleeCooldown(config.PlayerMeleeCooldown));
        }

        // Shift — дэш
        if (Raylib.IsKeyPressed(KeyboardKey.LeftShift) || Raylib.IsKeyPressed(KeyboardKey.L))
        {
            if (!world.Has<DashState>(playerId) && !world.Has<DashCooldown>(playerId))
            {
                ref var player = ref world.Get<PlayerTag>(playerId);

                float dirX = player.FacingX;
                float dirY = player.FacingY;

                // Если нет направления — дэш вниз
                if (dirX == 0 && dirY == 0)
                    dirY = 1;

                world.Add(playerId, new DashState
                {
                    DirX = dirX,
                    DirY = dirY,
                    Speed = config.PlayerDashSpeed * ts,
                    DurationRemaining = config.PlayerDashDuration,
                    AfterimageTimer = 0
                });

                world.Add(playerId, new Invincible(config.PlayerIFrameDuration));
            }
        }
    }
}
