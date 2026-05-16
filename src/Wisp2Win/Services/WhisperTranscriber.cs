using System.Text;
using Whisper.net;
using Wisp2Win.Models;

namespace Wisp2Win.Services;

public sealed class WhisperTranscriber
{
    private readonly ModelManager _modelManager;

    public WhisperTranscriber(ModelManager modelManager)
    {
        _modelManager = modelManager;
    }

    public async Task<string> TranscribeAsync(
        string wavPath,
        ModelProfile model,
        string language,
        bool translateToEnglish,
        CancellationToken cancellationToken = default)
    {
        var modelPath = _modelManager.GetModelPath(model);
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("Model is not installed.", modelPath);
        }

        using var factory = WhisperFactory.FromPath(modelPath);
        var builder = factory.CreateBuilder()
            .WithLanguage(NormalizeLanguage(language));

        if (translateToEnglish)
        {
            builder.WithTranslate();
        }

        using var processor = builder.Build();
        await using var fileStream = File.OpenRead(wavPath);
        var transcript = new StringBuilder();

        await foreach (var result in processor.ProcessAsync(fileStream, cancellationToken))
        {
            var text = CleanSegment(result.Text);
            if (text.Length > 0)
            {
                transcript.Append(text);
                transcript.Append(' ');
            }
        }

        return CleanTranscript(transcript.ToString());
    }

    private static string NormalizeLanguage(string language) =>
        language.ToLowerInvariant() switch
        {
            "russian" or "ru" => "ru",
            "english" or "en" => "en",
            "auto" => "auto",
            _ => "auto"
        };

    private static string CleanSegment(string text) => text.Trim();

    private static string CleanTranscript(string text)
    {
        var normalized = string.Join(' ', text.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries));
        var lower = normalized.ToLowerInvariant();
        string[] hallucinations =
        [
            "thanks for watching",
            "thank you for watching",
            "спасибо за просмотр",
            "подписывайтесь на канал",
            "субтитры сделал",
            "продолжение следует"
        ];

        return hallucinations.Any(lower.Contains) ? string.Empty : normalized;
    }
}
