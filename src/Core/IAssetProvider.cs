using Raylib_cs;

namespace DungeonOfShadows.Core;

/// <summary>
/// Централизованный провайдер ассетов. Загружает ресурсы при Start(),
/// выдаёт по ключу, освобождает при Dispose().
/// </summary>
public interface IAssetProvider : IDisposable
{
    Texture2D GetTexture(string key);
}
