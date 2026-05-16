using System.Runtime.InteropServices;
using System.Windows;

namespace Wisp2Win.Services;

public sealed class PasteService
{
    public async Task PasteAsync(string text, Func<bool>? activateTarget = null, CancellationToken cancellationToken = default)
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
        activateTarget?.Invoke();
        await Task.Delay(220, cancellationToken);
        SendCtrlV();
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

    private static void SendCtrlV()
    {
        INPUT[] inputs =
        [
            KeyDown(VirtualKey.Control),
            KeyDown(VirtualKey.V),
            KeyUp(VirtualKey.V),
            KeyUp(VirtualKey.Control)
        ];
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT KeyDown(VirtualKey key) => new()
    {
        type = 1,
        U = new InputUnion { ki = new KEYBDINPUT { wVk = (ushort)key } }
    };

    private static INPUT KeyUp(VirtualKey key) => new()
    {
        type = 1,
        U = new InputUnion { ki = new KEYBDINPUT { wVk = (ushort)key, dwFlags = 0x0002 } }
    };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private enum VirtualKey : ushort
    {
        Control = 0x11,
        V = 0x56
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
