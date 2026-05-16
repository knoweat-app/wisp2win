using System.Diagnostics;

namespace Wisp2Win.Services;

public sealed class DiagnosticsService
{
    public void OpenLogsDirectory()
    {
        AppPaths.Ensure();
        Process.Start(new ProcessStartInfo
        {
            FileName = AppPaths.LogsDirectory,
            UseShellExecute = true
        });
    }
}
