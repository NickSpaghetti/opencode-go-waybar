namespace OpencodeGoWaybar.Brokers.Themes;

/// <summary>
/// A shim over the files that hold the bar's theme. Primitives only: it reads,
/// it answers whether a directory is there, and it hands back a subscription.
///
/// It does not understand CSS, does not decide what a missing file means, and does
/// not coalesce anything — §1.2.1 leaves a broker no flow control to do any of it
/// with. Resolving imports, localising a missing file and collapsing an editor's
/// event burst all belong to the service above.
/// </summary>
internal interface IWaybarThemeBroker
{
    /// <summary>
    /// The file's text. Throws the native exception when it is not there, for the
    /// broker-neighbouring service to localise (§1.7.2).
    /// </summary>
    ValueTask<string> ReadTextAsync(string path, CancellationToken cancellationToken);

    bool StyleSheetDirectoryExists(string directoryPath);

    /// <summary>
    /// Raises <paramref name="onChanged"/> for every stylesheet write in
    /// <paramref name="directoryPath"/> — every raw event, uncollapsed. The caller
    /// owns the returned subscription.
    /// </summary>
    IDisposable WatchStyleSheets(string directoryPath, Action onChanged);
}
