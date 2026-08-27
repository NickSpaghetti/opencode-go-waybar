namespace OpencodeGoWaybar.Brokers.Themes;

internal sealed class WaybarThemeBroker : IWaybarThemeBroker
{
    public async ValueTask<string> ReadTextAsync(string path, CancellationToken cancellationToken) =>
        await File.ReadAllTextAsync(path, cancellationToken);

    public bool StyleSheetDirectoryExists(string directoryPath) =>
        Directory.Exists(directoryPath);

    public IDisposable WatchStyleSheets(string directoryPath, Action onChanged)
    {
        var watcher = new FileSystemWatcher(directoryPath, "*.css")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };

        watcher.Changed += (_, _) => onChanged();
        watcher.Created += (_, _) => onChanged();
        watcher.Deleted += (_, _) => onChanged();
        watcher.Renamed += (_, _) => onChanged();

        return watcher;
    }
}
