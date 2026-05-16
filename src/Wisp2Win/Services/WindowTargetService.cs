using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Wisp2Win.Services;

public sealed class WindowTargetService : IDisposable
{
    private readonly int _currentProcessId = Environment.ProcessId;
    private readonly System.Threading.Timer _timer;
    private IntPtr _lastExternalWindow = IntPtr.Zero;
    private IntPtr _lastLoggedWindow = IntPtr.Zero;

    public WindowTargetService()
    {
        _timer = new System.Threading.Timer(_ => CaptureForegroundWindow(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(300));
    }

    public void CaptureForegroundWindow()
    {
        var foreground = GetForegroundWindow();
        if (IsUsableTarget(foreground))
        {
            _lastExternalWindow = foreground;
            if (_lastLoggedWindow != _lastExternalWindow)
            {
                _lastLoggedWindow = _lastExternalWindow;
                AppLog.Info("target", $"Captured {DescribeWindow(_lastExternalWindow)}");
            }
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
            AppLog.Error("target", "No usable external window to activate");
            return false;
        }

        if (IsIconic(_lastExternalWindow))
        {
            ShowWindow(_lastExternalWindow, ShowWindowRestore);
        }

        ForceForegroundWindow(_lastExternalWindow);
        var active = GetForegroundWindow();
        var ok = active == _lastExternalWindow;
        AppLog.Info("target", $"Activated ok={ok}; target={DescribeWindow(_lastExternalWindow)}; foreground={DescribeWindow(active)}");
        return ok;
    }

    private static void ForceForegroundWindow(IntPtr window)
    {
        var currentThread = GetCurrentThreadId();
        var foreground = GetForegroundWindow();
        var foregroundThread = foreground == IntPtr.Zero ? 0 : GetWindowThreadProcessId(foreground, out _);
        var targetThread = GetWindowThreadProcessId(window, out _);

        if (foregroundThread != 0 && foregroundThread != currentThread)
        {
            AttachThreadInput(currentThread, foregroundThread, true);
        }

        if (targetThread != 0 && targetThread != currentThread)
        {
            AttachThreadInput(currentThread, targetThread, true);
        }

        try
        {
            BringWindowToTop(window);
            SetForegroundWindow(window);
            SetActiveWindow(window);
            SetFocus(window);
        }
        finally
        {
            if (targetThread != 0 && targetThread != currentThread)
            {
                AttachThreadInput(currentThread, targetThread, false);
            }

            if (foregroundThread != 0 && foregroundThread != currentThread)
            {
                AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
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

    public string DescribeForegroundWindow() => DescribeWindow(GetForegroundWindow());

    public string DescribeLastExternalWindow() => DescribeWindow(_lastExternalWindow);

    public string LastExternalWindowTitle => GetWindowTitle(_lastExternalWindow);

    private static string DescribeWindow(IntPtr window)
    {
        if (window == IntPtr.Zero)
        {
            return "0x0";
        }

        GetWindowThreadProcessId(window, out var processId);
        return $"0x{window.ToInt64():X}, pid={processId}, title=\"{GetWindowTitle(window)}\"";
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
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetActiveWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

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
