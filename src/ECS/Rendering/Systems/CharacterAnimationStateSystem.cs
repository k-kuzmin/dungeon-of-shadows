using DungeonOfShadows.Core;
using DungeonOfShadows.ECS.Combat;

namespace DungeonOfShadows.ECS.Rendering.Systems;

/// <summary>
/// Маппит игровое состояние (AI, ввод, боевые компоненты) на имя клипа в Animator.
/// Тикает ПЕРЕД AnimationSystem, чтобы смена клипа применилась в том же кадре.
/// </summary>
public class CharacterAnimationStateSystem : ITickable
{
    private readonly World _world;
    private readonly GameContext _ctx;
    private readonly GameConfig _config;
    private readonly List<int> _buffer = new();

    // Pre-interned clip names — без аллокаций на hot path
    private static readonly string[] Directions = { "Down", "Up", "Left", "Right" };
    private static readonly string[] AnimNames = { "Idle", "Run", "Attack", "RunAttack", "Death" };
    private static readonly Dictionary<(string anim, string dir), string> ClipNames = BuildClipNames();

    private static Dictionary<(string, string), string> BuildClipNames()
    {
        var dict = new Dictionary<(string, string), string>(AnimNames.Length * Directions.Length);
        foreach (var anim in AnimNames)
            foreach (var dir in Directions)
                dict[(anim, dir)] = $"{anim}_{dir}";
        return dict;
    }

    public CharacterAnimationStateSystem(World world, GameContext ctx, GameConfig config)
    {
        _world = world;
        _ctx = ctx;
        _config = config;
    }

    public void Tick(float dt)
    {
        var world = _world;

        world.QueryInto<Animator, Animation>(_buffer);
        for (int i = 0; i < _buffer.Count; i++)
        {
            int id = _buffer[i];
            ref var animator = ref world.Get<Animator>(id);

            if (world.Has<PlayerTag>(id))
            {
                UpdatePlayer(id, ref animator);
            }
            else if (world.Has<EnemyDeathState>(id))
            {
                // Враг умирает — EnemyTag уже удалён HealthSystem
                string dir = ExtractDirection(animator.CurrentClip);
                animator.CurrentClip = ClipNames[("Death", dir)];
            }
            else if (world.Has<EnemyTag>(id))
            {
                UpdateEnemy(id, ref animator);
            }
        }
    }

    private void UpdatePlayer(int id, ref Animator animator)
    {
        var world = _world;
        ref var player = ref world.Get<PlayerTag>(id);

        // Определяем анимацию по приоритету
        string anim;
        string dir;

        bool hasAttack = world.Has<MeleeAttack>(id);
        bool hasDash = world.Has<DashState>(id);
        bool hasVelocity = false;

        if (world.Has<Velocity>(id))
        {
            ref var vel = ref world.Get<Velocity>(id);
            hasVelocity = vel.X != 0 || vel.Y != 0;
        }

        // Если анимация атаки ещё проигрывается (не Finished) — не переключаем
        if (!hasAttack && IsAttackClip(animator.CurrentClip))
        {
            ref var animState = ref world.Get<Animation>(id);
            if (!animState.Finished)
                return;
        }

        if (_ctx.State == GameState.Dead)
        {
            anim = "Death";
            dir = DirectionFromVector(player.FacingX, player.FacingY);
        }
        else if (hasAttack && hasVelocity)
        {
            // RunAttack — бежим и бьём, направление по движению
            anim = "RunAttack";
            ref var vel = ref world.Get<Velocity>(id);
            dir = DirectionFromVector(vel.X, vel.Y);
        }
        else if (hasAttack)
        {
            anim = "Attack";
            ref var atk = ref world.Get<MeleeAttack>(id);
            dir = DirectionFromVector(atk.DirX, atk.DirY);
        }
        else if (hasDash)
        {
            anim = "Run";
            ref var dash = ref world.Get<DashState>(id);
            dir = DirectionFromVector(dash.DirX, dash.DirY);
        }
        else if (hasVelocity)
        {
            anim = "Run";
            ref var vel = ref world.Get<Velocity>(id);
            dir = DirectionFromVector(vel.X, vel.Y);
        }
        else
        {
            anim = "Idle";
            dir = DirectionFromVector(player.FacingX, player.FacingY);
        }

        animator.CurrentClip = ClipNames[(anim, dir)];
    }

    private void UpdateEnemy(int id, ref Animator animator)
    {
        var world = _world;
        ref var enemy = ref world.Get<EnemyTag>(id);

        // Определяем анимацию: AI переключает стейт на Chase сразу после удара,
        // но Attack клип доигрывает до конца (non-loop → Finished = true).
        string anim;
        bool enemyHasVelocity = false;
        float velX = 0, velY = 0;
        if (world.Has<Velocity>(id))
        {
            ref var vel = ref world.Get<Velocity>(id);
            velX = vel.X;
            velY = vel.Y;
            enemyHasVelocity = vel.X != 0 || vel.Y != 0;
        }

        if (enemy.State == AiState.Attack)
        {
            anim = enemyHasVelocity ? "RunAttack" : "Attack";
        }
        else if (IsAttackClip(animator.CurrentClip) && world.Has<Animation>(id))
        {
            ref var animState = ref world.Get<Animation>(id);
            if (animState.Finished)
                anim = enemy.State is AiState.Chase or AiState.Patrol or AiState.Flee ? "Run" : "Idle";
            else
                anim = enemyHasVelocity ? "RunAttack" : "Attack";
        }
        else
        {
            anim = enemy.State switch
            {
                AiState.Chase or AiState.Patrol or AiState.Flee => "Run",
                _ => "Idle"
            };
        }

        // Направление из скорости, при отсутствии — сохраняем предыдущее
        string dir = enemyHasVelocity
            ? DirectionFromVector(velX, velY)
            : ExtractDirection(animator.CurrentClip);

        animator.CurrentClip = ClipNames[(anim, dir)];
    }

    private static string DirectionFromVector(float x, float y)
    {
        if (MathF.Abs(y) >= MathF.Abs(x))
            return y >= 0 ? "Down" : "Up";
        return x >= 0 ? "Right" : "Left";
    }

    private static bool IsAttackClip(string clipName)
    {
        if (clipName == null) return false;
        return clipName.StartsWith("Attack_") || clipName.StartsWith("RunAttack_");
    }

    /// <summary>
    /// Извлекает направление из текущего имени клипа (напр. "Run_Left" → "Left").
    /// Без аллокаций — сравнивает суффиксы напрямую.
    /// </summary>
    private static string ExtractDirection(string clipName)
    {
        if (clipName == null) return "Down";
        if (clipName.EndsWith("Left")) return "Left";
        if (clipName.EndsWith("Right")) return "Right";
        if (clipName.EndsWith("Up")) return "Up";
        return "Down";
    }
}
