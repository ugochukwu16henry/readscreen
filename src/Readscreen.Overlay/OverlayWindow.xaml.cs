using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Readscreen.Overlay;

public partial class OverlayWindow : Window
{
    private const uint WdaExcludeFromCapture = 0x11;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint affinity);

    public OverlayWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ApplyCaptureExclusion();
    }

    public void SetContent(string text) => ContentText.Text = text;

    public void AppendContent(string token) => ContentText.Text += token;

    public void ClearContent() => ContentText.Text = string.Empty;

    public void SetStatusLabel(string status) => StatusText.Text = status;

    private void ApplyCaptureExclusion()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
            SetWindowDisplayAffinity(hwnd, WdaExcludeFromCapture);
    }
}
