using Raylib_cs;

namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Загрузка и хранение текстур. Вызывать LoadAll() после InitWindow(), UnloadAll() перед CloseWindow().
/// </summary>
public class TextureManager
{
    private readonly Dictionary<string, Texture2D> _textures = new(16);

    private static readonly (string Key, string Path)[] _manifest =
    {
        ("walls_floor",  "assets/sprites/environment/walls_floor.png"),
        ("objects",      "assets/sprites/environment/Objects.png"),
        ("doors_chest",  "assets/sprites/environment/doors_lever_chest_animation.png"),
        ("fire_large",   "assets/sprites/environment/fire_animation.png"),
        ("fire_small",   "assets/sprites/environment/fire_animation2.png"),
        ("cracks_floor", "assets/sprites/environment/decorative_cracks_floor.png"),
        ("cracks_walls", "assets/sprites/environment/decorative_cracks_walls.png"),
    };

    public void LoadAll()
    {
        foreach (var (key, path) in _manifest)
        {
            var tex = Raylib.LoadTexture(path);
            if (tex.Id == 0)
                throw new InvalidOperationException($"Failed to load texture '{key}' from '{path}'");
            _textures[key] = tex;
        }
    }

    public Texture2D Get(string key) => _textures[key];

    public void UnloadAll()
    {
        foreach (var tex in _textures.Values)
            Raylib.UnloadTexture(tex);
        _textures.Clear();
    }
}
