using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Readscreen.Overlay;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private readonly Dictionary<int, Action> _handlers = new();
    private IntPtr _hwnd;
    private HwndSource? _source;
    private int _nextId = 1;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public void Initialize(Window window)
    {
        var helper = new WindowInteropHelper(window);
        window.SourceInitialized += (_, _) =>
        {
            _hwnd = helper.Handle;
            _source = HwndSource.FromHwnd(_hwnd);
            _source?.AddHook(WndProc);
        };
    }

    public int Register(uint modifiers, uint key, Action handler)
    {
        var id = _nextId++;
        if (_hwnd != IntPtr.Zero)
            RegisterHotKey(_hwnd, id, modifiers, key);
        _handlers[id] = handler;
        return id;
    }

    public void Unregister(int id)
    {
        if (_hwnd != IntPtr.Zero)
            UnregisterHotKey(_hwnd, id);
        _handlers.Remove(id);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && _handlers.TryGetValue(wParam.ToInt32(), out var handler))
        {
            handler();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        foreach (var id in _handlers.Keys.ToList())
            Unregister(id);
        _source?.RemoveHook(WndProc);
    }
}

public static class HotkeyModifiers
{
    public const uint Alt = 0x0001;
    public const uint Control = 0x0002;
    public const uint Shift = 0x0004;
    public const uint Win = 0x0008;
}
