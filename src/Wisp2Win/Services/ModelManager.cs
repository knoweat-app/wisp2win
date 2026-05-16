using System.IO;
using System.Net.Http;
using Wisp2Win.Models;

namespace Wisp2Win.Services;

public sealed class ModelManager
{
    private static readonly HttpClient Http = new();

    public string GetModelPath(ModelProfile profile) => Path.Combine(AppPaths.ModelsDirectory, profile.FileName);

    public bool IsInstalled(ModelProfile profile)
    {
        var path = GetModelPath(profile);
        return File.Exists(path) && new FileInfo(path).Length > profile.ApproxBytes * 0.80;
    }

    public async Task<string> EnsureInstalledAsync(
        ModelProfile profile,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        AppPaths.Ensure();
        var finalPath = GetModelPath(profile);
        if (IsInstalled(profile))
        {
            progress?.Report(1);
            return finalPath;
        }

        var partialPath = finalPath + ".download";
        if (File.Exists(partialPath))
        {
            File.Delete(partialPath);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, profile.Url);
        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? profile.ApproxBytes;
        await using var remote = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var local = File.Create(partialPath);

        var buffer = new byte[1024 * 128];
        long readTotal = 0;
        while (true)
        {
            var read = await remote.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            await local.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            readTotal += read;
            progress?.Report(Math.Clamp((double)readTotal / total, 0, 1));
        }

        local.Close();
        if (File.Exists(finalPath))
        {
            File.Delete(finalPath);
        }

        File.Move(partialPath, finalPath);
        progress?.Report(1);
        return finalPath;
    }
}
