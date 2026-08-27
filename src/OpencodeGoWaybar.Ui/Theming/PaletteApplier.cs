using Avalonia.Controls;
using Avalonia.Media;
using OpencodeGoWaybar.Models.Themes;

namespace OpencodeGoWaybar.Ui.Theming;

/// <summary>
/// Writes an exposed palette into a resource dictionary as brushes. This is the
/// only place a ThemeColor becomes an Avalonia type, which is what lets the
/// module assembly stay free of any UI dependency and keep compiling to NativeAOT.
/// </summary>
public static class PaletteApplier
{
    /// <summary>
    /// Assigns every role by the resource key the windows bind to. Existing keys
    /// are overwritten, so Themes/Palette.axaml acts as the fallback for a machine
    /// with no stylesheet rather than as a competing source.
    /// </summary>
    public static void Apply(IResourceDictionary resources, ThemePalette palette)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(palette);

        Set(resources, "WindowBg", palette.WindowBg);
        Set(resources, "ChromeBg", palette.ChromeBg);
        Set(resources, "ToolbarBg", palette.ToolbarBg);
        Set(resources, "RowAltBg", palette.RowAltBg);
        Set(resources, "TrackBg", palette.TrackBg);
        Set(resources, "Hairline", palette.Hairline);
        Set(resources, "RowHairline", palette.RowHairline);

        Set(resources, "TextPrimary", palette.TextPrimary);
        Set(resources, "TextBody", palette.TextBody);
        Set(resources, "TextMuted", palette.TextMuted);
        Set(resources, "TextFaint", palette.TextFaint);

        Set(resources, "AccentLine", palette.AccentLine);
        Set(resources, "AccentText", palette.AccentText);
        Set(resources, "BarFill", palette.BarFill);
        Set(resources, "BarFillToday", palette.BarFillToday);

        Set(resources, "Ok", palette.Ok);
        Set(resources, "Caution", palette.Caution);
        Set(resources, "Danger", palette.Danger);
        Set(resources, "OkText", palette.OkText);
        Set(resources, "CautionText", palette.CautionText);

        Set(resources, "WarnBg", palette.WarnBg);
        Set(resources, "WarnBorder", palette.WarnBorder);
        Set(resources, "WarnText", palette.WarnText);

        if (palette.MonoFontFamily is { Length: > 0 } monoFontFamily)
        {
            // Kept as a list so a bar naming a font this machine lacks still
            // lands on something monospaced.
            resources["Mono"] = new FontFamily($"{monoFontFamily}, monospace");
        }
    }

    /// <summary>Every palette key the windows resolve, in declaration order.</summary>
    internal static IReadOnlyList<string> ResourceKeys { get; } =
    [
        "WindowBg", "ChromeBg", "ToolbarBg", "RowAltBg", "TrackBg", "Hairline", "RowHairline",
        "TextPrimary", "TextBody", "TextMuted", "TextFaint",
        "AccentLine", "AccentText", "BarFill", "BarFillToday",
        "Ok", "Caution", "Danger", "OkText", "CautionText",
        "WarnBg", "WarnBorder", "WarnText",
    ];

    private static void Set(IResourceDictionary resources, string key, ThemeColor color) =>
        resources[key] = new SolidColorBrush(ToColor(color));

    internal static Color ToColor(ThemeColor color) =>
        Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
}
