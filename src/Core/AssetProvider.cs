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
        // Environment
        ("walls_floor",  "assets/sprites/environment/walls_floor.png"),
        ("objects",      "assets/sprites/environment/Objects.png"),
        ("doors_chest",  "assets/sprites/environment/doors_lever_chest_animation.png"),
        ("fire_large",   "assets/sprites/environment/fire_animation.png"),
        ("fire_small",   "assets/sprites/environment/fire_animation2.png"),
        ("cracks_floor", "assets/sprites/environment/decorative_cracks_floor.png"),
        ("cracks_walls", "assets/sprites/environment/decorative_cracks_walls.png"),

        // Hero
        ("hero_idle",       "assets/sprites/characters/hero_lvl2/Swordsman_lvl2_Idle_with_shadow.png"),
        ("hero_run",        "assets/sprites/characters/hero_lvl2/Swordsman_lvl2_Run_with_shadow.png"),
        ("hero_attack",     "assets/sprites/characters/hero_lvl2/Swordsman_lvl2_attack_with_shadow.png"),
        ("hero_run_attack", "assets/sprites/characters/hero_lvl2/Swordsman_lvl2_Run_Attack_with_shadow.png"),
        ("hero_death",      "assets/sprites/characters/hero_lvl2/Swordsman_lvl2_Death_with_shadow.png"),

        // Orc1 (Rat)
        ("orc1_idle",       "assets/sprites/characters/orc_lvl1/orc1_idle_with_shadow.png"),
        ("orc1_run",        "assets/sprites/characters/orc_lvl1/orc1_run_with_shadow.png"),
        ("orc1_attack",     "assets/sprites/characters/orc_lvl1/orc1_attack_with_shadow.png"),
        ("orc1_run_attack", "assets/sprites/characters/orc_lvl1/orc1_run_attack_front_with_shadow.png"),
        ("orc1_death",      "assets/sprites/characters/orc_lvl1/orc1_death_with_shadow.png"),

        // Orc2 (Goblin)
        ("orc2_idle",       "assets/sprites/characters/orc_lvl2/orc2_idle_with_shadow.png"),
        ("orc2_run",        "assets/sprites/characters/orc_lvl2/orc2_run_with_shadow.png"),
        ("orc2_attack",     "assets/sprites/characters/orc_lvl2/orc2_attack_with_shadow.png"),
        ("orc2_run_attack", "assets/sprites/characters/orc_lvl2/orc2_run_attack_with_shadow.png"),
        ("orc2_death",      "assets/sprites/characters/orc_lvl2/orc2_death_with_shadow.png"),

        // Orc3 (Skeleton)
        ("orc3_idle",       "assets/sprites/characters/orc_lvl3/orc3_idle_with_shadow.png"),
        ("orc3_run",        "assets/sprites/characters/orc_lvl3/orc3_run_with_shadow.png"),
        ("orc3_attack",     "assets/sprites/characters/orc_lvl3/orc3_attack_with_shadow.png"),
        ("orc3_run_attack", "assets/sprites/characters/orc_lvl3/orc3_run_attack_with_shadow.png"),
        ("orc3_death",      "assets/sprites/characters/orc_lvl3/orc3_death_with_shadow.png"),
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
