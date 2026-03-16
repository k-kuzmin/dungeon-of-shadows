using DungeonOfShadows.Core;
using System.Numerics;

namespace DungeonOfShadows.ECS.Rendering.Systems;

public class CameraSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly Random _rng = new();
    private readonly List<int> _queryBuffer = new();

    private float _shakeTimer;
    private float _shakeAmplitude;

    public CameraSystem(GameContext ctx)
    {
        _ctx = ctx;
    }

    public void Tick(float dt)
    {
        var world = _ctx.World;

        world.QueryInto<PlayerTag, Position>(_queryBuffer);
        if (_queryBuffer.Count > 0)
        {
            int id = _queryBuffer[0];
            ref var pos = ref world.Get<Position>(id);
            float half = _ctx.Config.ScaledTileSize / 2f;
            var targetPos = new Vector2(pos.X + half, pos.Y + half);

            float smoothing = 1f - MathF.Pow(_ctx.Config.CameraSmoothBase, dt);
            _ctx.Camera = _ctx.Camera with
            {
                Target = System.Numerics.Vector2.Lerp(_ctx.Camera.Target, targetPos, smoothing)
            };
        }

        // Screen shake
        if (_shakeTimer > 0)
        {
            _shakeTimer -= dt;
            float intensity = _shakeAmplitude * (_shakeTimer / _ctx.Config.ScreenShakeDuration);
            _ctx.Camera = _ctx.Camera with
            {
                Target = _ctx.Camera.Target + new Vector2(
                    (float)(_rng.NextDouble() * 2 - 1) * intensity,
                    (float)(_rng.NextDouble() * 2 - 1) * intensity
                )
            };
        }
    }

    public void TriggerShake(float? amplitude = null)
    {
        _shakeTimer = _ctx.Config.ScreenShakeDuration;
        _shakeAmplitude = amplitude ?? _ctx.Config.ScreenShakeAmplitude;
    }
}
