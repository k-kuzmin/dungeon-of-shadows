using DungeonOfShadows.Core;

namespace DungeonOfShadows.ECS.Exploration.Systems;

/// <summary>
/// Плавно интерполирует яркость тайлов (FovBrightness) к целевому значению,
/// определяемому Visibility (FOV). Замораживается при GameState != Playing.
/// </summary>
public class TileFadeSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly GameConfig _config;

    public TileFadeSystem(GameContext ctx, GameConfig config)
    {
        _ctx = ctx;
        _config = config;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var map = _ctx.Map;
        float step = _config.FovFadeDuration > 0f
            ? dt / _config.FovFadeDuration
            : float.PositiveInfinity;
        float exploredTarget = _config.FovExploredBrightness;
        int w = map.Width;
        int h = map.Height;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                ref var tile = ref map.Tiles[x, y];

                float target = tile.Visibility switch
                {
                    2 => 1.0f,
                    1 => exploredTarget,
                    _ => 0.0f
                };

                float current = tile.FovBrightness;
                if (current == target) continue;

                float delta = target - current;
                tile.FovBrightness = MathF.Abs(delta) <= step
                    ? target
                    : current + MathF.Sign(delta) * step;
            }
        }
    }
}
