using OpencodeGoWaybar.Models.Themes;

namespace OpencodeGoWaybar.Services.Foundations.Themes;

internal interface IThemeService
{
    /// <summary>
    /// The palette the bar is currently painted with, or null when this machine
    /// has no readable Waybar stylesheet.
    /// </summary>
    ValueTask<ThemePalette?> RetrievePaletteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Delivers a palette whenever the stylesheets change it. Only actual changes
    /// arrive, so a consumer may apply every one without checking.
    /// </summary>
    void WatchPalette(Action<ThemePalette> onChanged);
}
