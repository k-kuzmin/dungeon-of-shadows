namespace DungeonOfShadows.ECS;

/// <summary>
/// Сервис с однократной инициализацией. Вызывается после сборки DI-контейнера,
/// перед первым Tick. Порядок вызова = порядок регистрации.
/// </summary>
public interface IStartable
{
    void Start();
}
