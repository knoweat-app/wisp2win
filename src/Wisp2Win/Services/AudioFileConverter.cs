using System.IO;
using NAudio.Wave;

namespace Wisp2Win.Services;

public sealed class AudioFileConverter
{
    private static readonly WaveFormat WhisperInputFormat = new(16000, 16, 1);

    public static string DialogFilter =>
        "Audio files|*.wav;*.mp3;*.m4a;*.aac;*.wma;*.flac|WAV files|*.wav|MP3 files|*.mp3|All files|*.*";

    public string ConvertToWhisperWav(string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Audio file was not found.", sourcePath);
        }

        AppPaths.Ensure();
        var outputPath = Path.Combine(AppPaths.TempDirectory, $"import-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}.wav");

        try
        {
            AppLog.Info("import", $"Converting audio source={sourcePath}");
            using var reader = new MediaFoundationReader(sourcePath);
            using var resampler = new MediaFoundationResampler(reader, WhisperInputFormat)
            {
                ResamplerQuality = 60
            };

            WaveFileWriter.CreateWaveFile(outputPath, resampler);
            AppLog.Info("import", $"Converted wav={outputPath}");
            return outputPath;
        }
        catch
        {
            TryDelete(outputPath);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Temp audio cleanup is best-effort.
        }
    }
}
