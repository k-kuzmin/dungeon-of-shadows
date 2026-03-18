using DungeonOfShadows.Core;
using System.Numerics;

namespace DungeonOfShadows.ECS.Rendering.Systems;

public class CameraSystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly World _world;
    private readonly GameConfig _config;
    private readonly Random _rng = new();
    private readonly List<int> _queryBuffer = new();

    private float _shakeTimer;
    private float _shakeAmplitude;

    // Чистая позиция камеры без shake — lerp не заражается дрожанием
    private Vector2 _baseTarget;
    private bool _baseInitialized;

    public CameraSystem(GameContext ctx, World world, GameConfig config)
    {
        _ctx = ctx;
        _world = world;
        _config = config;
    }

    public void Tick(float dt)
    {
        _world.QueryInto<PlayerTag, Position>(_queryBuffer);
        if (_queryBuffer.Count > 0)
        {
            int id = _queryBuffer[0];
            ref var pos = ref _world.Get<Position>(id);
            float half = _config.ScaledTileSize / 2f;
            var targetPos = new Vector2(pos.X + half, pos.Y + half);

            if (!_baseInitialized)
            {
                _baseTarget = targetPos;
                _baseInitialized = true;
            }
            else
            {
                float smoothing = 1f - MathF.Pow(_config.CameraSmoothBase, dt);
                _baseTarget = Vector2.Lerp(_baseTarget, targetPos, smoothing);
            }
        }

        // Pixel-snap: округляем до целых пикселей чтобы убрать суб-пиксельные щели между тайлами
        float zoom = _ctx.Camera.Zoom;
        float snappedX = MathF.Round(_baseTarget.X * zoom) / zoom;
        float snappedY = MathF.Round(_baseTarget.Y * zoom) / zoom;
        var finalTarget = new Vector2(snappedX, snappedY);

        // Screen shake поверх snap — не загрязняет _baseTarget для следующего кадра
        if (_shakeTimer > 0)
        {
            _shakeTimer -= dt;
            float intensity = _shakeAmplitude * (_shakeTimer / _config.ScreenShakeDuration);
            finalTarget += new Vector2(
                (float)(_rng.NextDouble() * 2 - 1) * intensity,
                (float)(_rng.NextDouble() * 2 - 1) * intensity
            );
        }

        _ctx.Camera = _ctx.Camera with { Target = finalTarget };
    }

    public void TriggerShake(float? amplitude = null)
    {
        _shakeTimer = _config.ScreenShakeDuration;
        _shakeAmplitude = amplitude ?? _config.ScreenShakeAmplitude;
    }
}
