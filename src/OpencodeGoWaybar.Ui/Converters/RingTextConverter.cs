using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace OpencodeGoWaybar.Ui.Converters;

/// <summary>
/// The number printed inside a ring needs a tinted, readable colour rather than
/// the saturated one used for the ring itself: "Danger" red on a dark card is
/// legible, but "Ok" green and "Caution" amber are not, so those get their softer
/// text variants.
/// </summary>
public sealed class RingTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var textKey = ToTextKey(value as string);

        IBrush? brush = PaletteLookup.ResolveBrush(
            Application.Current?.Resources,
            Application.Current?.ActualThemeVariant,
            textKey);

        return brush ?? AvaloniaProperty.UnsetValue;
    }

    internal static string ToTextKey(string? brushKey) => brushKey switch
    {
        "Ok" => "OkText",
        "Caution" => "CautionText",
        "Danger" => "Danger",
        _ => "TextPrimary",
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("A palette key is never written back from a brush.");
}
