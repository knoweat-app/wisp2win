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
            Icon = SystemIcons.Application,
            Text = "Wisp2Win",
            Visible = true,
            ContextMenuStrip = BuildMenu(window, coordinator)
        };
        _notifyIcon.DoubleClick += (_, _) => window.ShowAndActivate();
    }

    private static Forms.ContextMenuStrip BuildMenu(MainWindow window, DictationCoordinator coordinator)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => window.ShowAndActivate());
        menu.Items.Add("Toggle dictation", null, async (_, _) => await coordinator.ToggleAsync());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => System.Windows.Application.Current.Shutdown());
        return menu;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
