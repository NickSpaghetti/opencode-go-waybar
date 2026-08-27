using System.Globalization;
using System.Text.RegularExpressions;
using OpencodeGoWaybar.Models.Themes;

namespace OpencodeGoWaybar.Services.Foundations.Themes;

internal sealed partial class ThemeService
{
    /// <summary>
    /// Comments are removed before anything else is looked for. A real config
    /// keeps an inactive palette one comment above the live one:
    ///
    ///     /* @import "mocha.css"; */
    ///     @import "honkadaloonga.css";
    ///
    /// Matching imports first would silently load the wrong palette — the window
    /// would come out fully themed, in colours the bar is not using.
    /// </summary>
    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex CommentPattern();

    [GeneratedRegex(@"@import\s+(?:url\(\s*)?[""']([^""']+)[""']\s*\)?\s*;")]
    private static partial Regex ImportPattern();

    [GeneratedRegex(@"@define-color\s+([A-Za-z0-9_-]+)\s+([^;]+);")]
    private static partial Regex DefineColorPattern();

    [GeneratedRegex(@"font-family\s*:\s*([^;}]+)")]
    private static partial Regex FontFamilyPattern();

    [GeneratedRegex(@"[0-9]*\.?[0-9]+")]
    private static partial Regex NumberPattern();

    private static string StripComments(string styleSheet) =>
        CommentPattern().Replace(styleSheet, " ");

    private static IEnumerable<string> ParseImports(string styleSheet) =>
        ImportPattern().Matches(styleSheet).Select(match => match.Groups[1].Value);

    /// <summary>
    /// Later definitions win, which is what CSS does and why the sheets arrive in
    /// cascade order. Values that cannot be parsed are skipped rather than
    /// failing the whole palette: one unsupported colour function should not cost
    /// the window its theme.
    /// </summary>
    private static Dictionary<string, ThemeColor> ParseDefinedColors(
        IReadOnlyList<string> styleSheets)
    {
        var definedColors = new Dictionary<string, ThemeColor>(StringComparer.OrdinalIgnoreCase);

        foreach (var styleSheet in styleSheets)
        {
            foreach (Match match in DefineColorPattern().Matches(styleSheet))
            {
                if (TryParseColor(match.Groups[2].Value, out ThemeColor color))
                {
                    definedColors[match.Groups[1].Value] = color;
                }
            }
        }

        return definedColors;
    }

    /// <summary>
    /// The first family the root sheet names, which in practice is the one on its
    /// universal selector. Null when the sheet names none, letting the consumer
    /// keep whatever default it shipped with.
    /// </summary>
    private static string? ParseMonoFontFamily(string rootStyleSheet)
    {
        Match match = FontFamilyPattern().Match(rootStyleSheet);

        if (!match.Success)
        {
            return null;
        }

        var firstFamily = match.Groups[1].Value.Split(',')[0].Trim();

        return firstFamily.Trim('"', '\'') is { Length: > 0 } family ? family : null;
    }

    private static bool TryParseColor(string rawValue, out ThemeColor color)
    {
        var value = rawValue.Trim();

        if (value.StartsWith('#'))
        {
            return TryParseHexColor(value[1..], out color);
        }

        if (value.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseRgbColor(value, out color);
        }

        color = ThemeColor.FromRgb(0, 0, 0);

        return false;
    }

    private static bool TryParseHexColor(string digits, out ThemeColor color)
    {
        color = ThemeColor.FromRgb(0, 0, 0);

        // #rgb is expanded the way CSS expands it, by doubling each digit.
        if (digits.Length == 3)
        {
            digits = string.Concat(digits.Select(digit => new string(digit, 2)));
        }

        if (digits.Length is not (6 or 8))
        {
            return false;
        }

        foreach (var digit in digits)
        {
            if (!Uri.IsHexDigit(digit))
            {
                return false;
            }
        }

        color = new ThemeColor(
            Convert.ToByte(digits[..2], 16),
            Convert.ToByte(digits[2..4], 16),
            Convert.ToByte(digits[4..6], 16),
            digits.Length == 8 ? Convert.ToByte(digits[6..8], 16) : byte.MaxValue);

        return true;
    }

    private static bool TryParseRgbColor(string value, out ThemeColor color)
    {
        color = ThemeColor.FromRgb(0, 0, 0);

        double[] channels = [.. NumberPattern().Matches(value)
            .Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture))];

        if (channels.Length < 3)
        {
            return false;
        }

        color = new ThemeColor(
            ToChannel(channels[0]),
            ToChannel(channels[1]),
            ToChannel(channels[2]),
            // CSS writes alpha as a 0-1 fraction; tolerate a 0-255 value too.
            channels.Length >= 4
                ? ToChannel(channels[3] <= 1 ? channels[3] * 255 : channels[3])
                : byte.MaxValue);

        return true;
    }

    private static byte ToChannel(double value) => (byte)Math.Clamp(value, 0, 255);
}
