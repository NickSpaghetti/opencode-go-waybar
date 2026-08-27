using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace OpencodeGoWaybar.Ui.Converters;

/// <summary>Today's column is picked out from the rest of the week.</summary>
public sealed class BarBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        IBrush? brush = PaletteLookup.ResolveBrush(
            Application.Current?.Resources,
            Application.Current?.ActualThemeVariant,
            ToBarKey(value as bool?));

        return brush ?? AvaloniaProperty.UnsetValue;
    }

    internal static string ToBarKey(bool? isToday) =>
        isToday is true ? "BarFillToday" : "BarFill";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("A bar brush is never written back.");
}
