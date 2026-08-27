using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Themes;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Models.Themes;

namespace OpencodeGoWaybar.Services.Foundations.Themes;

/// <summary>
/// Turns the bar's stylesheet into a palette. All the understanding lives here
/// rather than in the broker: following an @import means parsing CSS, deciding
/// what a missing file means is a decision, and collapsing an editor's event burst
/// is policy — none of which a broker may hold (§1.2.1).
/// </summary>
internal sealed partial class ThemeService(
    IWaybarThemeBroker themeBroker,
    ILoggingBroker loggingBroker,
    OpenCodeGoOptions options) : IThemeService, IDisposable
{
    private readonly Lock gate = new();

    private IDisposable? subscription;
    private ThemePalette? lastPalette;

    public ValueTask<ThemePalette?> RetrievePaletteAsync(CancellationToken cancellationToken) =>
        TryCatchAsync(async () =>
        {
            ThemePalette? palette = await LoadPaletteAsync(cancellationToken);

            lock (this.gate)
            {
                this.lastPalette = palette;
            }

            return palette;
        });

    /// <summary>
    /// Only the directory holding the root stylesheet is watched. Every import in
    /// a normal Waybar config is a sibling of style.css, and watching arbitrary
    /// parent directories would cost an inotify handle per level for no gain.
    /// </summary>
    public void WatchPalette(Action<ThemePalette> onChanged)
    {
        var rootPath = ExpandHomeRelativePath(options.WaybarStylePath);
        var directoryPath = Path.GetDirectoryName(rootPath);

        // A machine with no Waybar config is a supported state, not a failure.
        if (string.IsNullOrEmpty(directoryPath)
            || !themeBroker.StyleSheetDirectoryExists(directoryPath))
        {
            return;
        }

        this.subscription = themeBroker.WatchStyleSheets(
            directoryPath,
            () => _ = RaiseWhenChangedAsync(onChanged));
    }

    /// <summary>
    /// An editor saves through a temporary file and a rename, so one save arrives
    /// as several events. Rather than guess a coalescing window, every event
    /// re-reads and the consumer hears about it only when the palette actually
    /// differs. Re-parsing two small stylesheets costs less than owning a magic
    /// number, and a burst collapses to exactly one notification with no timer.
    ///
    /// Swallows: this runs on a watcher's thread, where an escaping exception takes
    /// the process with it. The failure is already logged, and keeping the current
    /// palette is the right outcome for a stylesheet saved mid-edit.
    /// </summary>
    private async Task RaiseWhenChangedAsync(Action<ThemePalette> onChanged)
    {
        try
        {
            ThemePalette? palette = await LoadPaletteAsync(CancellationToken.None);

            if (palette is null)
            {
                return;
            }

            lock (this.gate)
            {
                if (palette == this.lastPalette)
                {
                    return;
                }

                this.lastPalette = palette;
            }

            onChanged(palette);
        }
        catch (Exception exception)
        {
            await loggingBroker.LogErrorAsync(MapException(exception));
        }
    }

    private async ValueTask<ThemePalette?> LoadPaletteAsync(CancellationToken cancellationToken)
    {
        var rootPath = ExpandHomeRelativePath(options.WaybarStylePath);

        var styleSheets = new List<string>();
        var visitedPaths = new HashSet<string>(StringComparer.Ordinal);

        var rootStyleSheet = await CollectStyleSheetsAsync(
            rootPath,
            styleSheets,
            visitedPaths,
            cancellationToken);

        if (rootStyleSheet is null)
        {
            return null;
        }

        Dictionary<string, ThemeColor> definedColors = ParseDefinedColors(styleSheets);

        if (definedColors.Count == 0)
        {
            return null;
        }

        return CreatePalette(definedColors, ParseMonoFontFamily(rootStyleSheet));
    }

    /// <summary>
    /// Reads one stylesheet and everything it imports, appending contents in
    /// cascade order. An import is applied where it appears, so an imported sheet
    /// lands before the sheet that imported it and therefore loses to it.
    /// </summary>
    private async ValueTask<string?> CollectStyleSheetsAsync(
        string path,
        List<string> styleSheets,
        HashSet<string> visitedPaths,
        CancellationToken cancellationToken)
    {
        // Also the cycle guard: two sheets importing each other would otherwise
        // recurse until the stack gives out.
        if (!visitedPaths.Add(path))
        {
            return null;
        }

        string rawStyleSheet;

        try
        {
            rawStyleSheet = await themeBroker.ReadTextAsync(path, cancellationToken);
        }
        catch (Exception exception)
            when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            // Localising the broker's native answer (§1.7.2): a stylesheet that is
            // not there is an ordinary state, so it reads as no theme rather than
            // as a failure.
            return null;
        }

        var styleSheet = StripComments(rawStyleSheet);
        var directoryPath = Path.GetDirectoryName(path) ?? string.Empty;

        foreach (var importedPath in ParseImports(styleSheet))
        {
            await CollectStyleSheetsAsync(
                Path.GetFullPath(Path.Combine(directoryPath, importedPath)),
                styleSheets,
                visitedPaths,
                cancellationToken);
        }

        styleSheets.Add(styleSheet);

        return styleSheet;
    }

    /// <summary>
    /// Turns the documented `~/...` default into a real path. Deliberately its own
    /// copy rather than shared with the secrets service: The Standard asks a
    /// component to own its utilities so it stays extractable (0.2.0.0.2).
    /// </summary>
    private static string ExpandHomeRelativePath(string path)
    {
        if (!path.StartsWith("~/", StringComparison.Ordinal))
        {
            return path;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return string.IsNullOrEmpty(home) ? path : Path.Combine(home, path[2..]);
    }

    public void Dispose()
    {
        this.subscription?.Dispose();
        this.subscription = null;
    }
}
