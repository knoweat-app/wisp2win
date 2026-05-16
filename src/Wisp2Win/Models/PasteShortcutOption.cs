namespace Wisp2Win.Models;

public sealed record PasteShortcutOption(string Value, string DisplayName)
{
    public static readonly IReadOnlyList<PasteShortcutOption> All =
    [
        new("auto", "Auto"),
        new("ctrl-v", "Ctrl+V"),
        new("ctrl-shift-v", "Ctrl+Shift+V")
    ];

    public static PasteShortcutOption ByValue(string value) =>
        All.FirstOrDefault(option => string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase)) ?? All[0];
}
