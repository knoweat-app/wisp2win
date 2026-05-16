using System.Runtime.InteropServices;
using System.Windows;

namespace Wisp2Win.Services;

public sealed class PasteService
{
    public async Task PasteAsync(
        string text,
        string shortcut,
        Func<bool>? activateTarget = null,
        CancellationToken cancellationToken = default)
    {
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
        var sent = SendPasteShortcut(shortcut);
        AppLog.Info("paste", $"Send {shortcut} result={sent}");
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    private enum VirtualKey : ushort
    {
        Control = 0x11,
        Shift = 0x10,
        V = 0x56
    }

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
