using System;
using System.Collections.Generic;
using System.Linq;

namespace Wisp2Win.Models;

public sealed record HotkeyOption(string Value, string DisplayName)
{
    public static readonly IReadOnlyList<HotkeyOption> All =
    [
        new("Oem5", @"\"),
        new("F8", "F8"),
        new("F9", "F9"),
        new("F10", "F10"),
        new("F11", "F11"),
        new("F12", "F12"),
        new("Pause", "Pause"),
        new("Insert", "Insert"),
        new("Ctrl+Alt+Space", "Ctrl+Alt+Space"),
        new("Ctrl+Alt+D", "Ctrl+Alt+D"),
        new("Ctrl+Alt+W", "Ctrl+Alt+W"),
        new("Ctrl+Shift+Space", "Ctrl+Shift+Space")
    ];

    public static HotkeyOption ByValue(string value) =>
        All.FirstOrDefault(h => string.Equals(h.Value, value, StringComparison.OrdinalIgnoreCase)) ?? All[0];
}
