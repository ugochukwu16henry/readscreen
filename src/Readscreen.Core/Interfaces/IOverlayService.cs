using Readscreen.Core.Models;

namespace Readscreen.Core.Interfaces;

public interface IOverlayService
{
    void Show();
    void Hide();
    void Toggle();
    bool IsVisible { get; }
    void SetOpacity(double value);
    void SetClickThrough(bool enabled);
    void UpdateText(string text);
    void AppendStreamingToken(string token);
    void ClearText();
    void SetStatus(AssistantStatus status);
}
