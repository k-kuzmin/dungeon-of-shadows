using System.Text.Json;

namespace DungeonOfShadows.ECS.Rendering;

/// <summary>
/// Загружает определения аниматоров из animations.json (с fallback на хардкод).
/// Используется AnimatorFactory и спавнерами для построения клипов.
/// </summary>
public sealed class AnimatorDatabase : IStartable
{
    private readonly Dictionary<string, AnimatorDefinition> _defs = new();

    public void Start()
    {
        Load();
        if (_defs.Count == 0)
            LoadFallback();
    }

    public bool TryGet(string id, out AnimatorDefinition def) => _defs.TryGetValue(id, out def!);

    private void Load()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "assets", "data", "animations.json");
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            var defs = JsonSerializer.Deserialize(json, AnimatorsJsonContext.Default.AnimatorDefinitionArray);
            if (defs == null) return;

            for (int i = 0; i < defs.Length; i++)
                _defs.TryAdd(defs[i].Id, defs[i]);
        }
        catch
        {
            // Fallback при ошибке загрузки
        }
    }

    private void LoadFallback()
    {
        AddDirectional("hero", new[] { "Down", "Left", "Right", "Up" }, 64, "Idle_Down",
            new ClipDefinition { Name = "Idle",      Texture = "hero_idle",       Frames = 12, FramesPerDirection = new[] { 12, 12, 12, 4 }, FrameDuration = 0.15f, Loop = true },
            new ClipDefinition { Name = "Run",       Texture = "hero_run",        Frames = 8,  FrameDuration = 0.10f, Loop = true },
            new ClipDefinition { Name = "Attack",    Texture = "hero_attack",     Frames = 8,  FrameDuration = 0.05f, Loop = false },
            new ClipDefinition { Name = "RunAttack", Texture = "hero_run_attack", Frames = 8,  FrameDuration = 0.07f, Loop = false },
            new ClipDefinition { Name = "Death",     Texture = "hero_death",      Frames = 7,  FrameDuration = 0.12f, Loop = false });

        string[] orcDirs = { "Down", "Up", "Left", "Right" };
        for (int n = 1; n <= 3; n++)
        {
            string prefix = $"orc{n}";
            AddDirectional(prefix, orcDirs, 64, "Idle_Down",
                new ClipDefinition { Name = "Idle",      Texture = $"{prefix}_idle",       Frames = 4, FrameDuration = 0.15f, Loop = true },
                new ClipDefinition { Name = "Run",       Texture = $"{prefix}_run",        Frames = 8, FrameDuration = 0.10f, Loop = true },
                new ClipDefinition { Name = "Attack",    Texture = $"{prefix}_attack",     Frames = 8, FrameDuration = 0.10f, Loop = false },
                new ClipDefinition { Name = "RunAttack", Texture = $"{prefix}_run_attack", Frames = 8, FrameDuration = 0.07f, Loop = false },
                new ClipDefinition { Name = "Death",     Texture = $"{prefix}_death",      Frames = 8, FrameDuration = 0.12f, Loop = false });
        }

        // Torch
        _defs.TryAdd("torch", new AnimatorDefinition
        {
            Id = "torch", Type = "simple", FrameWidth = 32, FrameHeight = 48, InitialClip = "idle",
            Clips = new[] { new ClipDefinition { Name = "idle", Texture = "fire_large", Frames = 6, FrameDuration = 0.15f, Loop = true, StartX = 0, StartY = 0, StrideX = 0, StrideY = 48 } }
        });

        // Chest
        _defs.TryAdd("chest", new AnimatorDefinition
        {
            Id = "chest", Type = "multi", FrameWidth = 32, FrameHeight = 32, InitialClip = "idle",
            Clips = new[]
            {
                new ClipDefinition { Name = "idle", Texture = "doors_chest", Frames = 1, FrameDuration = 1f, Loop = true,  StartX = 128, StartY = 16, StrideX = 0, StrideY = 0 },
                new ClipDefinition { Name = "open", Texture = "doors_chest", Frames = 5, FrameDuration = 0.1f, Loop = false, StartX = 128, StartY = 16, StrideX = 0, StrideY = 48 }
            }
        });

        // Projectiles
        _defs.TryAdd("fireball", new AnimatorDefinition
        {
            Id = "fireball", Type = "simple", FrameWidth = 16, FrameHeight = 16, InitialClip = "fly",
            Clips = new[] { new ClipDefinition { Name = "fly", Texture = "fireball", Frames = 8, FrameDuration = 0.08f, Loop = true, StartX = 0, StartY = 0, StrideX = 16, StrideY = 0 } }
        });

        _defs.TryAdd("magic_bolt", new AnimatorDefinition
        {
            Id = "magic_bolt", Type = "simple", FrameWidth = 16, FrameHeight = 16, InitialClip = "fly",
            Clips = new[] { new ClipDefinition { Name = "fly", Texture = "magic_bolt", Frames = 5, FrameDuration = 0.08f, Loop = true, StartX = 0, StartY = 0, StrideX = 16, StrideY = 0 } }
        });
    }

    private void AddDirectional(string id, string[] dirs, int frameSize, string initialClip, params ClipDefinition[] clips)
    {
        _defs.TryAdd(id, new AnimatorDefinition
        {
            Id = id, Type = "directional", Directions = dirs,
            FrameWidth = frameSize, FrameHeight = frameSize,
            InitialClip = initialClip, Clips = clips
        });
    }
}
