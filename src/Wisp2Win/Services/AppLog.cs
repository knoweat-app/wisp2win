using System;
using System.IO;

namespace Wisp2Win.Services;

public static class AppLog
{
    private static readonly object SyncRoot = new();

    public static void Info(string area, string message) => Write("INFO", area, message);

    public static void Error(string area, Exception exception) =>
        Write("ERROR", area, $"{exception.GetType().Name}: {exception.Message}\n{exception}");

    public static void Error(string area, string message) => Write("ERROR", area, message);

    private static void Write(string level, string area, string message)
    {
        try
        {
            AppPaths.Ensure();
            var line = $"{DateTimeOffset.Now:O} [{level}] {area}: {message}{Environment.NewLine}";
            lock (SyncRoot)
            {
                File.AppendAllText(AppPaths.LogPath, line);
            }
        }
        catch
        {
            // Logging must never break dictation.
        }
    }
}
