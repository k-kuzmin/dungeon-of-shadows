using System;

namespace DungeonOfShadows.UI;

/// <summary>
/// Runtime UI state: InputConsumed флаг и modal stack.
/// Singleton через DI. ClearFrame вызывается UiInputSystem в начале кадра.
/// </summary>
public class UiContext
{
    /// <summary>
    /// Если true — gameplay input системы (combat, spell) должны пропустить клики мыши.
    /// </summary>
    public bool InputConsumed { get; set; }

    private readonly Stack<ModalDescriptor> _modalStack = new();

    public bool HasModal => _modalStack.Count > 0;
    public ModalDescriptor TopModal => _modalStack.Peek();

    /// <summary>
    /// Сброс per-frame состояния. Вызывается в начале каждого кадра.
    /// </summary>
    public void ClearFrame()
    {
        InputConsumed = false;
    }

    public void PushModal(ModalDescriptor modal)
    {
        _modalStack.Push(modal);
        InputConsumed = true;
    }

    public void PopModal()
    {
        if (_modalStack.Count > 0)
            _modalStack.Pop();
    }

    public void ClearModals() => _modalStack.Clear();
}

/// <summary>
/// Данные модального окна — заголовок, текст, callbacks.
/// </summary>
public struct ModalDescriptor
{
    public string Title;
    public string Text;
    public string ConfirmLabel;
    public string CancelLabel;
    public Action? OnConfirm;
    public Action? OnCancel;
}
