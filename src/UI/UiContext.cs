using System;
using DungeonOfShadows.Core;

namespace DungeonOfShadows.UI;

/// <summary>
/// Runtime UI state: InputConsumed флаг, modal stack, controls overlay, pause management.
/// Singleton через DI. ClearFrame вызывается UiInputSystem в начале кадра.
/// </summary>
public class UiContext
{
    private readonly GameContext _gameCtx;

    /// <summary>
    /// Если true — gameplay input системы (combat, spell) должны пропустить клики мыши.
    /// </summary>
    public bool InputConsumed { get; set; }

    private readonly Stack<ModalDescriptor> _modalStack = new();
    private SelectionModalDescriptor? _selectionModal;

    /// <summary>
    /// Оверлей управления на старте игры. Закрывается по Enter.
    /// </summary>
    public bool ControlsOverlayActive { get; set; }

    public bool HasModal => _modalStack.Count > 0 || _selectionModal != null || ControlsOverlayActive;
    public bool HasStandardModal => _modalStack.Count > 0;
    public bool HasSelectionModal => _selectionModal != null;
    public ModalDescriptor TopModal => _modalStack.Peek();
    public SelectionModalDescriptor? SelectionModal => _selectionModal;

    public UiContext(GameContext gameCtx)
    {
        _gameCtx = gameCtx;
    }

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
        SetPaused();
    }

    public void PopModal()
    {
        if (_modalStack.Count > 0)
            _modalStack.Pop();
        RecalcPauseState();
    }

    public void SetSelectionModal(SelectionModalDescriptor modal)
    {
        _selectionModal = modal;
        InputConsumed = true;
        SetPaused();
    }

    public void CloseSelectionModal()
    {
        _selectionModal = null;
        RecalcPauseState();
    }

    public void OpenControlsOverlay()
    {
        ControlsOverlayActive = true;
        InputConsumed = true;
        SetPaused();
    }

    public void CloseControlsOverlay()
    {
        ControlsOverlayActive = false;
        RecalcPauseState();
    }

    public void ClearModals()
    {
        _modalStack.Clear();
        _selectionModal = null;
        ControlsOverlayActive = false;
        RecalcPauseState();
    }

    private void SetPaused()
    {
        if (_gameCtx.State != GameState.Dead)
            _gameCtx.State = GameState.Paused;
    }

    private void RecalcPauseState()
    {
        if (_gameCtx.State == GameState.Dead) return;
        _gameCtx.State = (HasModal || _gameCtx.ShowInventory || _gameCtx.ShowFullMap)
            ? GameState.Paused
            : GameState.Playing;
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
