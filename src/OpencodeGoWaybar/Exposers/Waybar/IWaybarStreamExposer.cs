namespace OpencodeGoWaybar.Exposers.Waybar;

internal interface IWaybarStreamExposer
{
    /// <summary>
    /// Yields a Waybar payload whenever what the bar should show has changed, and
    /// does not return until cancelled. One line per payload, which is the format
    /// a Waybar custom module reads when it is given no interval.
    /// </summary>
    IAsyncEnumerable<string> ExposeStreamAsync(CancellationToken cancellationToken);
}
