using System.Globalization;
using Avalonia.Data.Converters;

namespace OpencodeGoWaybar.Ui.Converters;

/// <summary>
/// Scales a 0-1 sparkline fraction into a pixel height. The track height arrives
/// as the converter parameter because the same converter serves a 44px popup
/// sparkline and a 104px dashboard chart.
/// </summary>
public sealed class BarHeightConverter : IValueConverter
{
    /// <summary>
    /// A day with usage still gets a visible sliver. Rounding a small fraction to
    /// zero would render an empty column, which reads as "no data" rather than
    /// "a little".
    /// </summary>
    internal const double MinimumVisibleHeight = 2d;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        ToHeight(value as double?, ToTrackHeight(parameter));

    internal static double ToHeight(double? fraction, double trackHeight)
    {
        if (fraction is not { } value || double.IsNaN(value) || value <= 0)
        {
            return 0d;
        }

        var height = Math.Clamp(value, 0d, 1d) * trackHeight;

        return Math.Max(height, MinimumVisibleHeight);
    }

    internal static double ToTrackHeight(object? parameter) => parameter switch
    {
        double trackHeight => trackHeight,
        // ConverterParameter arrives from XAML as a string.
        string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ => 0d,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("A bar height is never written back.");
}
