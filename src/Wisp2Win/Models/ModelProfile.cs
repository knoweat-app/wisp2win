namespace Wisp2Win.Models;

public sealed record ModelProfile(
    string Id,
    string DisplayName,
    string FileName,
    string Url,
    string Description,
    long ApproxBytes)
{
    public static readonly IReadOnlyList<ModelProfile> All =
    [
        new("tiny", "Быстрая", "ggml-tiny.bin", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin", "Fastest, rough quality", 78L * 1024 * 1024),
        new("base", "Сбалансированная", "ggml-base.bin", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin", "Good default for short dictation", 148L * 1024 * 1024),
        new("small", "Точная", "ggml-small.bin", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin", "Better Russian quality, slower download", 488L * 1024 * 1024),
        new("medium", "Тяжелая", "ggml-medium.bin", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin", "High quality, heavy CPU load", 1533L * 1024 * 1024)
    ];

    public static ModelProfile ById(string id) =>
        All.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase)) ?? All[1];
}
