using OpencodeGoWaybar.Models.Themes;

namespace OpencodeGoWaybar.Exposers.Themes;

/// <summary>
/// The published contract for the desktop's palette. Public because the Avalonia
/// head lives in another assembly and must reach the filesystem through an
/// exposer rather than reading stylesheets itself.
/// </summary>
public interface IThemeExposer
{
    /// <summary>The current palette, or null when there is no stylesheet to read.</summary>
    ValueTask<ThemePalette?> ExposePaletteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Delivers a fresh palette each time the stylesheets are saved. The callback
    /// arrives on a background thread; a UI consumer must marshal it itself.
    /// </summary>
    void WatchPalette(Action<ThemePalette> onChanged);
}
