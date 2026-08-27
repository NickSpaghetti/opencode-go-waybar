using OpencodeGoWaybar.Models.Themes;
using OpencodeGoWaybar.Services.Foundations.Themes;

namespace OpencodeGoWaybar.Exposers.Themes;

/// <summary>
/// Pure mapping (§3.0.0.0): both routines forward to the service and nothing else.
/// The service decides when a palette has actually changed, so there is no null
/// check, no sequencing and no reload logic to do here.
/// </summary>
internal sealed class ThemeExposer(IThemeService themeService) : IThemeExposer
{
    public ValueTask<ThemePalette?> ExposePaletteAsync(CancellationToken cancellationToken) =>
        themeService.RetrievePaletteAsync(cancellationToken);

    public void WatchPalette(Action<ThemePalette> onChanged) =>
        themeService.WatchPalette(onChanged);
}
