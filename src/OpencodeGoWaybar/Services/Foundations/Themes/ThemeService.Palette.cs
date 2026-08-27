using OpencodeGoWaybar.Models.Themes;

namespace OpencodeGoWaybar.Services.Foundations.Themes;

internal sealed partial class ThemeService
{
    private static readonly ThemeColor White = ThemeColor.FromRgb(255, 255, 255);
    private static readonly ThemeColor Black = ThemeColor.FromRgb(0, 0, 0);

    // Used only when a stylesheet names neither a background nor a foreground.
    private static readonly ThemeColor FallbackBackground = ThemeColor.FromRgb(0x1E, 0x1E, 0x22);
    private static readonly ThemeColor FallbackForeground = ThemeColor.FromRgb(0xE7, 0xEA, 0xEE);

    /// <summary>
    /// Maps what the stylesheet named onto the roles a window needs, deriving
    /// only what it did not name.
    ///
    /// The key names come in two dialects. Catppuccin-style themes — which is
    /// what hand-written Waybar configs overwhelmingly are — say base, text,
    /// surface0 and overlay0. Palette files generated from a terminal theme say
    /// background and foreground. Both are accepted, in that order.
    /// </summary>
    private static ThemePalette CreatePalette(
        Dictionary<string, ThemeColor> definedColors,
        string? monoFontFamily)
    {
        ThemeColor background =
            Pick(definedColors, "base", "background", "crust", "mantle") ?? FallbackBackground;

        ThemeColor foreground =
            Pick(definedColors, "text", "foreground", "subtext1") ?? FallbackForeground;

        var isLight = background.Luminance > 0.5;

        ThemeColor caution =
            Pick(definedColors, "yellow", "peach") ?? ThemeColor.FromRgb(0xFF, 0xC2, 0x43);

        ThemeColor accentText =
            Pick(definedColors, "sky", "blue", "accent", "sapphire", "lavender") ?? foreground;

        ThemeColor accentLine =
            Pick(definedColors, "sapphire", "blue", "accent", "sky")
            ?? accentText.MixWith(background, 0.35);

        ThemeColor ok = Pick(definedColors, "green") ?? ThemeColor.FromRgb(0x9A, 0xBD, 0x3D);

        return new ThemePalette
        {
            IsLight = isLight,
            MonoFontFamily = monoFontFamily,

            WindowBg = background,
            ChromeBg = Pick(definedColors, "surface0", "mantle")
                ?? Shift(background, isLight ? 0.03 : 0.05),
            ToolbarBg = Pick(definedColors, "surface0", "mantle")
                ?? Shift(background, isLight ? 0.02 : 0.03),
            RowAltBg = Pick(definedColors, "surface1")
                ?? Shift(background, isLight ? -0.02 : 0.02),
            TrackBg = Pick(definedColors, "surface2")
                ?? background.MixWith(foreground, 0.12),
            Hairline = Pick(definedColors, "overlay0")
                ?? background.MixWith(foreground, 0.14),
            RowHairline = Pick(definedColors, "overlay1")
                ?? background.MixWith(foreground, 0.10),

            TextPrimary = foreground,
            TextBody = Pick(definedColors, "subtext1") ?? foreground.MixWith(background, 0.12),
            TextMuted = Pick(definedColors, "subtext0") ?? foreground.MixWith(background, 0.35),
            TextFaint = Pick(definedColors, "overlay2") ?? foreground.MixWith(background, 0.52),

            AccentLine = accentLine,
            AccentText = accentText,
            BarFill = accentLine,
            BarFillToday = accentText,

            Ok = ok,
            Caution = caution,
            Danger = Pick(definedColors, "red", "maroon") ?? ThemeColor.FromRgb(0xF4, 0x43, 0x36),
            OkText = ok.MixWith(foreground, 0.25),
            CautionText = caution.MixWith(foreground, 0.25),

            WarnBg = background.MixWith(caution, isLight ? 0.16 : 0.12),
            WarnBorder = background.MixWith(caution, 0.34),
            WarnText = caution.MixWith(foreground, 0.25),
        };
    }

    /// <summary>The first name the stylesheet actually defined, in preference order.</summary>
    private static ThemeColor? Pick(
        Dictionary<string, ThemeColor> definedColors,
        params string[] names)
    {
        foreach (var name in names)
        {
            if (definedColors.TryGetValue(name, out ThemeColor? color))
            {
                return color;
            }
        }

        return null;
    }

    /// <summary>Toward white for a positive amount, toward black for a negative one.</summary>
    private static ThemeColor Shift(ThemeColor color, double amount) =>
        color.MixWith(amount >= 0 ? White : Black, Math.Abs(amount));
}
