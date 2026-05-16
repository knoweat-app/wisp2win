using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace Wisp2Win.Services;

public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0x5757;
    private const int ProbeHotkeyId = 0x5758;
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
        var parsedHotkey = ParseHotkey(keyName);
        if (parsedHotkey is null)
        {
            AppLog.Error("hotkey", $"Cannot parse hotkey: {keyName}");
            return false;
        }

        var (key, modifiers) = parsedHotkey.Value;
        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        const uint modNoRepeat = 0x4000;
        if (!RegisterHotKey(_source.Handle, ProbeHotkeyId, modifiers | modNoRepeat, (uint)virtualKey))
        {
            AppLog.Error("hotkey", $"Hotkey probe failed: {keyName}, vk={virtualKey}, modifiers={modifiers}");
            return false;
        }

        UnregisterHotKey(_source.Handle, ProbeHotkeyId);
        Unregister();
        _registered = RegisterHotKey(_source.Handle, HotkeyId, modifiers | modNoRepeat, (uint)virtualKey);
        AppLog.Info("hotkey", $"Register {keyName}: {_registered}, vk={virtualKey}, modifiers={modifiers}");
        return _registered;
    }

    private static (Key Key, uint Modifiers)? ParseHotkey(string hotkey)
    {
        const uint modAlt = 0x0001;
        const uint modControl = 0x0002;
        const uint modShift = 0x0004;
        const uint modWin = 0x0008;

        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        uint modifiers = 0;
        foreach (var part in parts.Take(parts.Length - 1))
        {
            modifiers |= part.ToLowerInvariant() switch
            {
                "alt" => modAlt,
                "ctrl" or "control" => modControl,
                "shift" => modShift,
                "win" or "windows" => modWin,
                _ => 0
            };
        }

        var keyName = parts[^1] switch
        {
            @"\" => "Oem5",
            "Backslash" => "Oem5",
            var value => value
        };

        return Enum.TryParse<Key>(keyName, ignoreCase: true, out var key)
            ? (key, modifiers)
            : null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            AppLog.Info("hotkey", "Hotkey pressed");
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
