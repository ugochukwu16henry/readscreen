using System.Windows;
using Readscreen.Core.Interfaces;
using Readscreen.Core.Models;

namespace Readscreen.Overlay;

public sealed class OverlayService : IOverlayService
{
    private readonly OverlayWindow _window;
    private bool _isVisible = true;

    public OverlayService()
    {
        _window = new OverlayWindow();
    }

    public bool IsVisible => _isVisible;

    public void Show()
    {
        RunOnUi(() =>
        {
            _window.Show();
            _window.EnsureTopmost();
            _isVisible = true;
        });
    }

    public void Hide()
    {
        RunOnUi(() =>
        {
            _window.Hide();
            _isVisible = false;
        });
    }

    public void Toggle()
    {
        if (_isVisible) Hide();
        else Show();
    }

    public void SetOpacity(double value)
    {
        RunOnUi(() => _window.Opacity = Math.Clamp(value, 0.1, 1.0));
    }

    public void SetClickThrough(bool enabled)
    {
        RunOnUi(() =>
        {
            ClickThroughHelper.SetClickThrough(_window, enabled);
            _window.EnsureTopmost();
        });
    }

    public void UpdateText(string text)
    {
        RunOnUi(() => _window.SetContent(text));
    }

    public void AppendStreamingToken(string token)
    {
        RunOnUi(() => _window.AppendContent(token));
    }

    public void ClearText()
    {
        RunOnUi(() => _window.ClearContent());
    }

    public void SetStatus(AssistantStatus status)
    {
        var label = status switch
        {
            AssistantStatus.Listening => "Listening",
            AssistantStatus.Reading => "Reading screen",
            AssistantStatus.Thinking => "Thinking",
            AssistantStatus.Answering => "Answering",
            AssistantStatus.Paused => "Paused",
            AssistantStatus.Blocked => "Capture blocked",
            AssistantStatus.Error => "Error",
            _ => "Idle"
        };
        RunOnUi(() => _window.SetStatusLabel(label));
    }

    public OverlayWindow GetWindow() => _window;

    private static void RunOnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
            action();
        else
            dispatcher.Invoke(action);
    }
}
