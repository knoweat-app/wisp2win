using System.IO;
using System.Text.Json;
using Wisp2Win.Models;

namespace Wisp2Win.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppSettings Current { get; private set; }

    public SettingsService()
    {
        AppPaths.Ensure();
        Current = Load();
    }

    public void Save()
    {
        AppPaths.Ensure();
        File.WriteAllText(AppPaths.SettingsPath, JsonSerializer.Serialize(Current, JsonOptions));
    }

    private static AppSettings Load()
    {
        if (!File.Exists(AppPaths.SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(AppPaths.SettingsPath)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }
}
