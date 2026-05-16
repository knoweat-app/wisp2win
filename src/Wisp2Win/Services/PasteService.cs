using System.Runtime.InteropServices;
using System.Windows;

namespace Wisp2Win.Services;

public sealed class PasteService
{
    public async Task PasteAsync(
        string text,
        string insertMethod,
        Func<bool>? activateTarget = null,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(insertMethod, "type-text", StringComparison.OrdinalIgnoreCase))
        {
            var activatedForTyping = activateTarget?.Invoke();
            AppLog.Info("paste", $"Activate target result={activatedForTyping}");
            await Task.Delay(220, cancellationToken);
            var typed = TypeText(text);
            AppLog.Info("paste", $"Type text chars={text.Length}, result={typed}");
            return;
        }

        System.Windows.IDataObject? previous = null;
        try
        {
            previous = System.Windows.Clipboard.GetDataObject();
        }
        catch
        {
            previous = null;
        }

        System.Windows.Clipboard.SetText(text);
        AppLog.Info("paste", $"Clipboard set chars={text.Length}");
        var activated = activateTarget?.Invoke();
        AppLog.Info("paste", $"Activate target result={activated}");
        await Task.Delay(220, cancellationToken);
        var sent = SendPasteShortcut(insertMethod);
        AppLog.Info("paste", $"Send {insertMethod} result={sent}");
        await Task.Delay(700, cancellationToken);

        if (previous is not null)
        {
            try
            {
                System.Windows.Clipboard.SetDataObject(previous, true);
            }
            catch
            {
                // Clipboard ownership is best-effort because the target app can read it asynchronously.
            }
        }
    }

    private static bool SendPasteShortcut(string shortcut)
    {
        var useShift = string.Equals(shortcut, "ctrl-shift-v", StringComparison.OrdinalIgnoreCase);
        Input[] inputs = useShift
            ?
            [
                KeyDown(VirtualKey.Control),
                KeyDown(VirtualKey.Shift),
                KeyDown(VirtualKey.V),
                KeyUp(VirtualKey.V),
                KeyUp(VirtualKey.Shift),
                KeyUp(VirtualKey.Control)
            ]
            :
            [
                KeyDown(VirtualKey.Control),
                KeyDown(VirtualKey.V),
                KeyUp(VirtualKey.V),
                KeyUp(VirtualKey.Control)
            ];
        var inputSize = Marshal.SizeOf<Input>();
        AppLog.Info("paste", $"SendInput cbSize={inputSize}");
        var sent = SendInput((uint)inputs.Length, inputs, inputSize);
        if (sent != inputs.Length)
        {
            AppLog.Error("paste", $"SendInput sent {sent}/{inputs.Length}, win32={Marshal.GetLastWin32Error()}");
        }

        return sent == inputs.Length;
    }

    private static bool TypeText(string text)
    {
        foreach (var ch in text)
        {
            if (ch == '\r')
            {
                continue;
            }

            Input[] inputs =
            [
                UnicodeKey(ch, keyUp: false),
                UnicodeKey(ch, keyUp: true)
            ];
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
            if (sent != inputs.Length)
            {
                AppLog.Error("paste", $"Unicode SendInput sent {sent}/{inputs.Length}, char=U+{(int)ch:X4}, win32={Marshal.GetLastWin32Error()}");
                return false;
            }

            Thread.Sleep(2);
        }

        return true;
    }

    private static Input KeyDown(VirtualKey key) => new()
    {
        type = 1,
        ki = new KeyboardInput { wVk = (ushort)key }
    };

    private static Input KeyUp(VirtualKey key) => new()
    {
        type = 1,
        ki = new KeyboardInput { wVk = (ushort)key, dwFlags = 0x0002 }
    };

    private static Input UnicodeKey(char ch, bool keyUp) => new()
    {
        type = 1,
        ki = new KeyboardInput
        {
            wScan = ch,
            dwFlags = keyUp ? KeyEventUnicode | KeyEventKeyUp : KeyEventUnicode
        }
    };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    private enum VirtualKey : ushort
    {
        Control = 0x11,
        Shift = 0x10,
        V = 0x56
    }

    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct Input
    {
        [FieldOffset(0)]
        public int type;
        [FieldOffset(8)]
        public KeyboardInput ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
