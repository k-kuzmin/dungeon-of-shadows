namespace DungeonOfShadows.ECS.Combat;

public enum EnemyType
{
    Rat,
    Goblin,
    Skeleton
}

public enum AiState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Flee
}

public struct EnemyTag
{
    public EnemyType Type;
    public AiState State;
    public float DetectionRadius;
    public float AttackRange;
    public float AttackCooldown;
    public float AttackCooldownMax;

    // AI навигация
    public float PathUpdateTimer;
    public int PatrolTargetTX;
    public int PatrolTargetTY;
    public float PatrolWaitTimer;

    // Спец-паттерны
    public float BehaviorTimer;
    public int StrafeDir;
    public float AttackWindupTimer;
}
