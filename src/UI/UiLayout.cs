namespace DungeonOfShadows.UI;

/// <summary>
/// Stateless layout cursor. ref struct гарантирует stack-only (zero alloc).
/// Двигает курсор по Row или Column с заданным gap.
/// </summary>
public ref struct UiLayout
{
    private int _cursorX;
    private int _cursorY;
    private readonly FlexDirection _dir;
    private readonly int _gap;
    private bool _first;

    private UiLayout(int x, int y, FlexDirection dir, int gap)
    {
        _cursorX = x;
        _cursorY = y;
        _dir = dir;
        _gap = gap;
        _first = true;
    }

    public static UiLayout Row(int x, int y, int gap = 0)
        => new(x, y, FlexDirection.Row, gap);

    public static UiLayout Column(int x, int y, int gap = 0)
        => new(x, y, FlexDirection.Column, gap);

    /// <summary>
    /// Забирает прямоугольник (w × h) и двигает курсор.
    /// </summary>
    public UiRect Take(int w, int h)
    {
        if (!_first)
        {
            if (_dir == FlexDirection.Row)
                _cursorX += _gap;
            else
                _cursorY += _gap;
        }
        _first = false;

        var rect = new UiRect(_cursorX, _cursorY, w, h);

        if (_dir == FlexDirection.Row)
            _cursorX += w;
        else
            _cursorY += h;

        return rect;
    }

    public UiRect TakeSquare(int size) => Take(size, size);

    public void Skip(int amount)
    {
        if (_dir == FlexDirection.Row)
            _cursorX += amount;
        else
            _cursorY += amount;
        _first = false;
    }

    /// <summary>
    /// Создаёт вложенный layout внутри следующего слота текущего layout.
    /// </summary>
    public UiLayout Nested(int w, int h, FlexDirection dir, int gap = 0)
    {
        var rect = Take(w, h);
        return new UiLayout(rect.X, rect.Y, dir, gap);
    }

    /// <summary>
    /// Текущая позиция курсора.
    /// </summary>
    public readonly int CursorX => _cursorX;
    public readonly int CursorY => _cursorY;
}
