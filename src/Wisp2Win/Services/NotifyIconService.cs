using System.Diagnostics;
using System.Drawing;
using Wisp2Win.Views;
using Forms = System.Windows.Forms;

namespace Wisp2Win.Services;

public sealed class NotifyIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;

    public NotifyIconService(MainWindow window, DictationCoordinator coordinator)
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Wisp2Win",
            Visible = true,
            ContextMenuStrip = BuildMenu(window, coordinator)
        };
        _notifyIcon.DoubleClick += (_, _) => window.ShowAndActivate();
    }

    private static Forms.ContextMenuStrip BuildMenu(MainWindow window, DictationCoordinator coordinator)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Открыть", null, (_, _) => window.ShowAndActivate());
        menu.Items.Add("Диктовка", null, async (_, _) => await coordinator.ToggleAsync());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => System.Windows.Application.Current.Shutdown());
        return menu;
    }

    private static Icon LoadIcon()
    {
        try
        {
            var path = Process.GetCurrentProcess().MainModule?.FileName;
            return string.IsNullOrWhiteSpace(path)
                ? SystemIcons.Application
                : Icon.ExtractAssociatedIcon(path) ?? SystemIcons.Application;
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
