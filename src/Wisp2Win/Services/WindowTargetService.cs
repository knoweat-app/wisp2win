using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Wisp2Win.Services;

public sealed class WindowTargetService : IDisposable
{
    private readonly int _currentProcessId = Environment.ProcessId;
    private readonly Timer _timer;
    private IntPtr _lastExternalWindow = IntPtr.Zero;

    public WindowTargetService()
    {
        _timer = new Timer(_ => CaptureForegroundWindow(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(300));
    }

    public void CaptureForegroundWindow()
    {
        var foreground = GetForegroundWindow();
        if (IsUsableTarget(foreground))
        {
            _lastExternalWindow = foreground;
        }
    }

    public bool ActivateLastExternalWindow()
    {
        if (!IsUsableTarget(_lastExternalWindow))
        {
            CaptureForegroundWindow();
        }

        if (!IsUsableTarget(_lastExternalWindow))
        {
            return false;
        }

        if (IsIconic(_lastExternalWindow))
        {
            ShowWindow(_lastExternalWindow, ShowWindowRestore);
        }

        return SetForegroundWindow(_lastExternalWindow);
    }

    private bool IsUsableTarget(IntPtr window)
    {
        if (window == IntPtr.Zero || !IsWindow(window) || !IsWindowVisible(window))
        {
            return false;
        }

        GetWindowThreadProcessId(window, out var processId);
        if (processId == 0 || processId == _currentProcessId)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(GetWindowTitle(window));
    }

    private static string GetWindowTitle(IntPtr window)
    {
        var length = GetWindowTextLength(window);
        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        GetWindowText(window, builder, builder.Capacity);
        return builder.ToString();
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    private const int ShowWindowRestore = 9;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);
}
