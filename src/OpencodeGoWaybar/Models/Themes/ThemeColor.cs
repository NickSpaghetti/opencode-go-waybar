namespace OpencodeGoWaybar.Models.Themes;

/// <summary>
/// One colour, deliberately expressed without any UI framework type. This
/// assembly is the NativeAOT module and must not take a dependency on Avalonia,
/// so the consumer converts these channels into whatever brush it needs.
/// </summary>
public sealed record ThemeColor(byte Red, byte Green, byte Blue, byte Alpha)
{
    public static ThemeColor FromRgb(byte red, byte green, byte blue) =>
        new(red, green, blue, byte.MaxValue);

    /// <summary>
    /// Relative luminance, used to decide whether a palette reads as light or
    /// dark. Alpha is ignored: a translucent bar still has a nominal colour.
    /// </summary>
    public double Luminance =>
        ((0.2126 * Red) + (0.7152 * Green) + (0.0722 * Blue)) / 255d;

    /// <summary>How far this colour leans toward <paramref name="other"/>.</summary>
    public ThemeColor MixWith(ThemeColor other, double amount) =>
        new(MixChannel(Red, other.Red, amount),
            MixChannel(Green, other.Green, amount),
            MixChannel(Blue, other.Blue, amount),
            Alpha);

    private static byte MixChannel(byte from, byte to, double amount) =>
        (byte)Math.Clamp(from + ((to - from) * amount), 0, 255);
}
