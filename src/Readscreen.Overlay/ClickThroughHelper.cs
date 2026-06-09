using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Readscreen.Overlay;

public static class ClickThroughHelper
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExLayered = 0x00080000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    public static void SetClickThrough(Window window, bool enabled)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        var style = GetWindowLong(hwnd, GwlExstyle);
        if (enabled)
            SetWindowLong(hwnd, GwlExstyle, style | WsExTransparent | WsExLayered);
        else
            SetWindowLong(hwnd, GwlExstyle, (style | WsExLayered) & ~WsExTransparent);
    }
}
