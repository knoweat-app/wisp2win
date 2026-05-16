namespace Wisp2Win.Models;

public sealed class AppSettings
{
    public string ModelId { get; set; } = "base";
    public string Language { get; set; } = "ru";
    public string Hotkey { get; set; } = "Oem5";
    public bool PasteAfterTranscription { get; set; } = true;
    public bool TranslateToEnglish { get; set; }
    public bool StartMinimized { get; set; }
}
