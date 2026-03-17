using Raylib_cs;
using DungeonOfShadows.ECS;

namespace DungeonOfShadows.Core;

/// <summary>
/// Реализация IAssetProvider. Загружает все ассеты при Start(),
/// предоставляет доступ по строковому ключу, освобождает при Dispose().
/// </summary>
public sealed class AssetProvider : IAssetProvider, IStartable
{
    private readonly Dictionary<string, Texture2D> _textures = new(16);
    private bool _disposed;

    private static readonly (string Key, string Path)[] TextureManifest =
    {
        ("walls_floor",  "assets/sprites/environment/walls_floor.png"),
        ("objects",      "assets/sprites/environment/Objects.png"),
        ("doors_chest",  "assets/sprites/environment/doors_lever_chest_animation.png"),
        ("fire_large",   "assets/sprites/environment/fire_animation.png"),
        ("fire_small",   "assets/sprites/environment/fire_animation2.png"),
        ("cracks_floor", "assets/sprites/environment/decorative_cracks_floor.png"),
        ("cracks_walls", "assets/sprites/environment/decorative_cracks_walls.png"),
    };

    public void Start()
    {
        foreach (var (key, path) in TextureManifest)
        {
            var tex = Raylib.LoadTexture(path);
            if (tex.Id == 0)
                throw new InvalidOperationException($"Failed to load texture '{key}' from '{path}'");
            _textures[key] = tex;
        }
    }

    public Texture2D GetTexture(string key) => _textures[key];

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var tex in _textures.Values)
            Raylib.UnloadTexture(tex);
        _textures.Clear();
    }
}
