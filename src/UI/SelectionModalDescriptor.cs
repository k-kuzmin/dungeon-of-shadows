using System;

namespace DungeonOfShadows.UI;

/// <summary>
/// Данные модалки выбора из N вариантов (Up/Down + Enter/Esc).
/// </summary>
public class SelectionModalDescriptor
{
    public string Title;
    public string[] Options;
    public string[] SelectedLabels;
    public string[] NormalLabels;
    public int SelectedIndex;
    public Action<int>? OnConfirm;
    public Action? OnCancel;

    public SelectionModalDescriptor(string title, string[] options, Action<int>? onConfirm, Action? onCancel)
    {
        Title = title;
        Options = options;
        SelectedIndex = 0;
        OnConfirm = onConfirm;
        OnCancel = onCancel;

        // Кеш prefixed строк — zero alloc при отрисовке
        SelectedLabels = new string[options.Length];
        NormalLabels = new string[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            SelectedLabels[i] = $"> {options[i]}";
            NormalLabels[i] = $"  {options[i]}";
        }
    }

    public void MoveUp()
    {
        if (Options.Length == 0) return;
        SelectedIndex = (SelectedIndex + Options.Length - 1) % Options.Length;
    }

    public void MoveDown()
    {
        if (Options.Length == 0) return;
        SelectedIndex = (SelectedIndex + 1) % Options.Length;
    }
}
