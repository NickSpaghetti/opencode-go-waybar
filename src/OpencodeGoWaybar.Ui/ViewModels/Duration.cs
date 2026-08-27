using System.Globalization;

namespace OpencodeGoWaybar.Ui.ViewModels;

/// <summary>
/// One named concept — a span rendered the way a countdown or an age reads —
/// shared by the gauge and the window. Not a grab-bag: if it grows a second
/// responsibility it should be split rather than extended.
/// </summary>
internal static class Duration
{
    /// <summary>
    /// Coarse on purpose: the largest two units are all anyone reads off a
    /// countdown, and a value that changes every second invites a redraw loop.
    /// </summary>
    internal static string Humanise(TimeSpan span) => span switch
    {
        { TotalDays: >= 1 } => span.Hours == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{(int)span.TotalDays}d")
            : string.Create(CultureInfo.InvariantCulture, $"{(int)span.TotalDays}d {span.Hours}h"),
        { TotalHours: >= 1 } =>
            string.Create(CultureInfo.InvariantCulture, $"{(int)span.TotalHours}h {span.Minutes}m"),
        { TotalMinutes: >= 1 } =>
            string.Create(CultureInfo.InvariantCulture, $"{(int)span.TotalMinutes}m"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{(int)span.TotalSeconds}s"),
    };
}
