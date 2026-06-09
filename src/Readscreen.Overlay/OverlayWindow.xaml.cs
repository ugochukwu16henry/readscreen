using System.Windows;

namespace Readscreen.Overlay;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
    }

    public void SetContent(string text) => ContentText.Text = text;

    public void AppendContent(string token) => ContentText.Text += token;

    public void ClearContent() => ContentText.Text = string.Empty;

    public void SetStatusLabel(string status) => StatusText.Text = status;
}
