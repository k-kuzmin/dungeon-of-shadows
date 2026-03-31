using Raylib_cs;

namespace DungeonOfShadows.UI;

/// <summary>
/// Кэш результатов Raylib.MeasureText. Устраняет повторные native-вызовы
/// для статического текста (HP "20/30", floor label, spell names).
/// </summary>
public class TextMeasureCache
{
    private readonly Dictionary<(string text, int fontSize), int> _cache = new(64);

    /// <summary>
    /// Возвращает ширину текста в пикселях. При cache hit — zero alloc (кроме string key lookup).
    /// </summary>
    public int Measure(string text, int fontSize)
    {
        var key = (text, fontSize);
        if (_cache.TryGetValue(key, out int width))
            return width;

        width = Raylib.MeasureText(text, fontSize);
        _cache[key] = width;
        return width;
    }

    public void Invalidate(string text)
    {
        // Удаляем все записи с данным текстом (все fontSize)
        var toRemove = new List<(string, int)>();
        foreach (var key in _cache.Keys)
        {
            if (key.text == text)
                toRemove.Add(key);
        }
        foreach (var key in toRemove)
            _cache.Remove(key);
    }

    public void Clear() => _cache.Clear();
}
