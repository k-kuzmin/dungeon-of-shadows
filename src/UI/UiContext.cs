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
    private SelectionModalDescriptor? _selectionModal;

    public bool HasModal => _modalStack.Count > 0 || _selectionModal != null;
    public bool HasStandardModal => _modalStack.Count > 0;
    public bool HasSelectionModal => _selectionModal != null;
    public ModalDescriptor TopModal => _modalStack.Peek();
    public SelectionModalDescriptor? SelectionModal => _selectionModal;

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

    public void SetSelectionModal(SelectionModalDescriptor modal)
    {
        _selectionModal = modal;
        InputConsumed = true;
    }

    public void CloseSelectionModal()
    {
        _selectionModal = null;
    }

    public void ClearModals()
    {
        _modalStack.Clear();
        _selectionModal = null;
    }
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
