namespace DungeonOfShadows.ECS.Items;

public struct Chest
{
    public bool Opened;
    public int GuaranteedDefinitionId;

    /// <summary>Время с момента открытия (для анимации).</summary>
    public float AnimTimer;
    /// <summary>Текущий кадр анимации открытия (0-4).</summary>
    public byte AnimFrame;
    /// <summary>Анимация завершена — больше не обновлять.</summary>
    public bool AnimDone;
}
