using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Readscreen.Overlay;

public partial class OverlayWindow : Window
{
    private const uint WdaExcludeFromCapture = 0x11;
    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNomove = 0x0002;
    private const uint SwpNosize = 0x0001;
    private const uint SwpNoactivate = 0x0010;
    private const uint SwpShowwindow = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint affinity);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    public OverlayWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            ApplyCaptureExclusion();
            EnsureTopmost();
        };
    }

    public void SetContent(string text) => ContentText.Text = text;

    public void AppendContent(string token) => ContentText.Text += token;

    public void ClearContent() => ContentText.Text = string.Empty;

    public void SetStatusLabel(string status) => StatusText.Text = status;

    public void SetHint(string hint) => HintText.Text = hint;

    public void EnsureTopmost()
    {
        Topmost = true;
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            SetWindowPos(
                hwnd,
                HwndTopmost,
                0,
                0,
                0,
                0,
                SwpNomove | SwpNosize | SwpNoactivate | SwpShowwindow);
        }
    }

    private void ApplyCaptureExclusion()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
            SetWindowDisplayAffinity(hwnd, WdaExcludeFromCapture);
    }
}
