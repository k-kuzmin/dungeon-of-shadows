using DungeonOfShadows.Core;
using DungeonOfShadows.Dungeon;

namespace DungeonOfShadows.ECS.Combat.Systems;

/// <summary>
/// AI врагов: state machine (Idle, Patrol, Chase, Attack, Flee), A* навигация.
/// </summary>
public class AISystem : ITickable
{
    private readonly GameContext _ctx;
    private readonly AStarPathfinder _pathfinder;
    private readonly List<int> _enemyBuffer = new();
    private readonly List<int> _playerBuffer = new();
    private readonly Random _rng = new();

    // Кеш путей (entity id → путь)
    private readonly Dictionary<int, List<(int x, int y)>> _paths = new();
    private readonly List<(int x, int y)> _tempPath = new();
    private readonly List<int> _deadPathIds = new();

    public AISystem(GameContext ctx, AStarPathfinder pathfinder)
    {
        _ctx = ctx;
        _pathfinder = pathfinder;
    }

    public void Tick(float dt)
    {
        if (_ctx.State != GameState.Playing) return;

        var world = _ctx.World;
        var config = _ctx.Config;
        var map = _ctx.Map;
        int ts = config.ScaledTileSize;

        // Получаем позицию игрока
        world.QueryInto<PlayerTag, Position>(_playerBuffer);
        if (_playerBuffer.Count == 0) return;

        ref var playerPos = ref world.Get<Position>(_playerBuffer[0]);
        int playerTX = (int)((playerPos.X + ts / 2f) / ts);
        int playerTY = (int)((playerPos.Y + ts / 2f) / ts);
        float playerCX = playerPos.X + ts / 2f;
        float playerCY = playerPos.Y + ts / 2f;

        world.QueryInto<EnemyTag, Position>(_enemyBuffer);
        foreach (int id in _enemyBuffer)
        {
            if (!world.Has<Velocity>(id) || !world.Has<Stats>(id) || !world.Has<Health>(id))
            {
                _paths.Remove(id);
                continue;
            }

            ref var enemy = ref world.Get<EnemyTag>(id);
            ref var pos = ref world.Get<Position>(id);
            ref var vel = ref world.Get<Velocity>(id);
            ref var stats = ref world.Get<Stats>(id);
            ref var health = ref world.Get<Health>(id);

            float enemyCX = pos.X + ts / 2f;
            float enemyCY = pos.Y + ts / 2f;
            int enemyTX = (int)(enemyCX / ts);
            int enemyTY = (int)(enemyCY / ts);

            float distToPlayer = MathF.Sqrt(
                (enemyCX - playerCX) * (enemyCX - playerCX) +
                (enemyCY - playerCY) * (enemyCY - playerCY));
            float distInTiles = distToPlayer / ts;

            // Тикаем кулдаун атаки
            if (enemy.AttackCooldown > 0)
                enemy.AttackCooldown -= dt;

            // --- State transitions ---
            // Rat: flee при HP < 30%
            if (enemy.Type == EnemyType.Rat &&
                (float)health.HP / health.MaxHP < config.RatFleeHealthThreshold &&
                distInTiles <= enemy.DetectionRadius)
            {
                enemy.State = AiState.Flee;
            }
            else if (distInTiles <= enemy.AttackRange && enemy.AttackCooldown <= 0)
            {
                enemy.State = AiState.Attack;
            }
            else if (distInTiles <= enemy.DetectionRadius)
            {
                enemy.State = AiState.Chase;
            }
            else if (enemy.State == AiState.Chase || enemy.State == AiState.Attack)
            {
                enemy.State = AiState.Patrol;
                enemy.PatrolTargetTX = -1;
            }
            else if (enemy.State == AiState.Idle)
            {
                enemy.PatrolWaitTimer -= dt;
                if (enemy.PatrolWaitTimer <= 0)
                {
                    enemy.State = AiState.Patrol;
                    enemy.PatrolTargetTX = -1;
                }
            }

            // --- State execution ---
            switch (enemy.State)
            {
                case AiState.Idle:
                    vel.X = 0;
                    vel.Y = 0;
                    break;

                case AiState.Patrol:
                    ExecutePatrol(ref enemy, ref pos, ref vel, stats.SPD, ts, map);
                    break;

                case AiState.Chase:
                    ExecuteChase(id, ref enemy, ref pos, ref vel, stats.SPD, ts, map,
                        enemyTX, enemyTY, playerTX, playerTY,
                        enemyCX, enemyCY, playerCX, playerCY, distInTiles,
                        dt, config);
                    break;

                case AiState.Attack:
                    vel.X = 0;
                    vel.Y = 0;
                    ExecuteAttack(id, ref enemy, ref stats, _playerBuffer[0], ref playerPos, ts, dt);
                    break;

                case AiState.Flee:
                    ExecuteFlee(ref pos, ref vel, stats.SPD, ts,
                        enemyCX, enemyCY, playerCX, playerCY);
                    break;
            }
        }

        // Чистим кеш путей от уже удалённых сущностей
        _deadPathIds.Clear();
        foreach (int cachedId in _paths.Keys)
            if (!world.IsAlive(cachedId))
                _deadPathIds.Add(cachedId);

        foreach (int deadId in _deadPathIds)
            _paths.Remove(deadId);
    }

    private void ExecutePatrol(ref EnemyTag enemy, ref Position pos, ref Velocity vel,
        float spd, int ts, TileMap map)
    {
        var config = _ctx.Config;

        // Выбираем случайную цель для патруля
        if (enemy.PatrolTargetTX < 0)
        {
            for (int attempt = 0; attempt < config.AiPatrolSearchAttempts; attempt++)
            {
                int tx = (int)(pos.X / ts) + _rng.Next(-config.AiPatrolSearchRadius, config.AiPatrolSearchRadius + 1);
                int ty = (int)(pos.Y / ts) + _rng.Next(-config.AiPatrolSearchRadius, config.AiPatrolSearchRadius + 1);
                if (map.IsWalkable(tx, ty))
                {
                    enemy.PatrolTargetTX = tx;
                    enemy.PatrolTargetTY = ty;
                    break;
                }
            }
            if (enemy.PatrolTargetTX < 0)
            {
                // Не нашли цель — стоим
                vel.X = 0;
                vel.Y = 0;
                enemy.State = AiState.Idle;
                enemy.PatrolWaitTimer = config.AiPatrolWaitMinSeconds +
                    (float)_rng.NextDouble() * (config.AiPatrolWaitMaxSeconds - config.AiPatrolWaitMinSeconds);
                return;
            }
        }

        float targetX = enemy.PatrolTargetTX * ts + ts / 2f;
        float targetY = enemy.PatrolTargetTY * ts + ts / 2f;
        float cx = pos.X + ts / 2f;
        float cy = pos.Y + ts / 2f;

        float dx = targetX - cx;
        float dy = targetY - cy;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist < ts * config.AiPatrolStopDistanceTiles)
        {
            // Дошли — переходим в Idle
            vel.X = 0;
            vel.Y = 0;
            enemy.State = AiState.Idle;
            enemy.PatrolWaitTimer = config.AiPatrolWaitMinSeconds +
                (float)_rng.NextDouble() * (config.AiPatrolWaitMaxSeconds - config.AiPatrolWaitMinSeconds);
            enemy.PatrolTargetTX = -1;
            return;
        }

        float speed = spd * ts * config.AiPatrolSpeedMultiplier; // Патруль — медленнее
        vel.X = dx / dist * speed;
        vel.Y = dy / dist * speed;
    }

    private void ExecuteChase(int id, ref EnemyTag enemy, ref Position pos, ref Velocity vel,
        float spd, int ts, TileMap map,
        int enemyTX, int enemyTY, int playerTX, int playerTY,
        float enemyCX, float enemyCY, float playerCX, float playerCY, float distInTiles,
        float dt, GameConfig config)
    {
        // Goblin: в среднем боевом радиусе двигается по окружности (strafe), а не только в лоб.
        if (enemy.Type == EnemyType.Goblin &&
            distInTiles > enemy.AttackRange * 1.2f &&
            distInTiles < enemy.DetectionRadius * 0.9f)
        {
            enemy.BehaviorTimer -= dt;
            if (enemy.BehaviorTimer <= 0)
            {
                enemy.BehaviorTimer = config.GoblinStrafeDurationMin +
                    (float)_rng.NextDouble() * (config.GoblinStrafeDurationMax - config.GoblinStrafeDurationMin);
                enemy.StrafeDir = _rng.Next(2) == 0 ? -1 : 1;
            }

            float toPlayerX = playerCX - enemyCX;
            float toPlayerY = playerCY - enemyCY;
            float len = MathF.Sqrt(toPlayerX * toPlayerX + toPlayerY * toPlayerY);
            if (len > 0.01f)
            {
                // Перпендикуляр к вектору на игрока
                float perpX = -toPlayerY / len * enemy.StrafeDir;
                float perpY = toPlayerX / len * enemy.StrafeDir;
                float speed = spd * ts * config.GoblinStrafeSpeedMultiplier;
                vel.X = perpX * speed;
                vel.Y = perpY * speed;
                return;
            }
        }

        enemy.PathUpdateTimer -= dt;

        if (enemy.PathUpdateTimer <= 0)
        {
            enemy.PathUpdateTimer = config.AiPathUpdateInterval;

            if (_pathfinder.TryFindPath(map, enemyTX, enemyTY, playerTX, playerTY, _tempPath))
            {
                if (!_paths.ContainsKey(id))
                    _paths[id] = new List<(int, int)>();
                _paths[id].Clear();
                _paths[id].AddRange(_tempPath);
            }
        }

        if (_paths.TryGetValue(id, out var path) && path.Count > 0)
        {
            float targetX = path[0].x * ts + ts / 2f;
            float targetY = path[0].y * ts + ts / 2f;
            float cx = pos.X + ts / 2f;
            float cy = pos.Y + ts / 2f;

            float dx = targetX - cx;
            float dy = targetY - cy;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist < ts * config.AiChaseWaypointReachDistanceTiles)
            {
                path.RemoveAt(0);
            }
            else
            {
                float speed = spd * ts;
                vel.X = dx / dist * speed;
                vel.Y = dy / dist * speed;
                return;
            }
        }

        // Фоллбэк — прямое движение к игроку
        float pcx = playerTX * ts + ts / 2f;
        float pcy = playerTY * ts + ts / 2f;
        float ecx = pos.X + ts / 2f;
        float ecy = pos.Y + ts / 2f;
        float ddx = pcx - ecx;
        float ddy = pcy - ecy;
        float ddist = MathF.Sqrt(ddx * ddx + ddy * ddy);
        if (ddist > 0.01f)
        {
            float speed = spd * ts;
            vel.X = ddx / ddist * speed;
            vel.Y = ddy / ddist * speed;
        }

    }

    private void ExecuteAttack(int enemyId, ref EnemyTag enemy, ref Stats stats,
        int playerId, ref Position playerPos, int ts, float dt)
    {
        if (enemy.AttackCooldown > 0) return;
        if (!_ctx.World.Has<Stats>(playerId)) return;

        // Skeleton: тяжёлая атака с коротким windup.
        if (enemy.Type == EnemyType.Skeleton)
        {
            if (enemy.AttackWindupTimer <= 0)
            {
                enemy.AttackWindupTimer = _ctx.Config.SkeletonHeavyWindup;
                return;
            }

            enemy.AttackWindupTimer -= dt;
            if (enemy.AttackWindupTimer > 0)
                return;

            enemy.AttackWindupTimer = 0;
        }

        enemy.AttackCooldown = enemy.AttackCooldownMax;
        enemy.State = AiState.Chase;

        ref var playerStats = ref _ctx.World.Get<Stats>(playerId);
        var (damage, isCrit) = DamageCalculator.Compute(
            ref stats, ref playerStats, _ctx.Config, _rng);

        if (enemy.Type == EnemyType.Skeleton)
            damage = Math.Max(1, (int)(damage * _ctx.Config.SkeletonHeavyDamageMultiplier));

        float px = playerPos.X + ts / 2f;
        float py = playerPos.Y + ts / 2f;

        _ctx.DamageEvents.Add(new DamageEvent(enemyId, playerId, damage, isCrit, px, py));
    }

    private static void ExecuteFlee(ref Position pos, ref Velocity vel, float spd, int ts,
        float ecx, float ecy, float pcx, float pcy)
    {
        float dx = ecx - pcx;
        float dy = ecy - pcy;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist > 0.01f)
        {
            float speed = spd * ts;
            vel.X = dx / dist * speed;
            vel.Y = dy / dist * speed;
        }
    }
}
