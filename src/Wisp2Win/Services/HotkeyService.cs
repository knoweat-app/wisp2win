using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace Wisp2Win.Services;

public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0x5757;
    private const int WmHotkey = 0x0312;
    private readonly HwndSource _source;
    private bool _registered;

    public event EventHandler? HotkeyPressed;

    public HotkeyService()
    {
        var parameters = new HwndSourceParameters("Wisp2WinHotkeySink")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0x800000
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    public bool Register(string keyName)
    {
        Unregister();
        var key = Enum.TryParse<Key>(keyName, ignoreCase: true, out var parsed) ? parsed : Key.Oem5;
        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        _registered = RegisterHotKey(_source.Handle, HotkeyId, 0, (uint)virtualKey);
        return _registered;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }
        return IntPtr.Zero;
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(_source.Handle, HotkeyId);
            _registered = false;
        }
    }

    public void Dispose()
    {
        Unregister();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
