using System;
using System.IO;

namespace Wisp2Win.Services;

public static class AppPaths
{
    public static string AppDataRoot { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wisp2Win");

    public static string ModelsDirectory { get; } = Path.Combine(AppDataRoot, "Models");

    public static string TempDirectory { get; } = Path.Combine(AppDataRoot, "Temp");

    public static string LogsDirectory { get; } = Path.Combine(AppDataRoot, "Logs");

    public static string SettingsPath { get; } = Path.Combine(AppDataRoot, "settings.json");

    public static string LogPath { get; } = Path.Combine(LogsDirectory, "wisp2win.log");

    public static void Ensure()
    {
        Directory.CreateDirectory(AppDataRoot);
        Directory.CreateDirectory(ModelsDirectory);
        Directory.CreateDirectory(TempDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}
