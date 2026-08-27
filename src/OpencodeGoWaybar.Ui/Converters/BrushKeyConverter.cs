using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace OpencodeGoWaybar.Ui.Converters;

/// <summary>
/// Turns a palette key carried on a view model into the brush it names. The view
/// model deals in keys rather than brushes so it stays free of any drawing type,
/// and so a live theme change repaints simply by re-raising the property.
/// </summary>
public sealed class BrushKeyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        IBrush? brush = PaletteLookup.ResolveBrush(
            Application.Current?.Resources,
            Application.Current?.ActualThemeVariant,
            value);

        return brush ?? AvaloniaProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("A palette key is never written back from a brush.");
}
