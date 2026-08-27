namespace OpencodeGoWaybar.Exposers.Waybar;

internal interface IWaybarExposer
{
    /// <summary>The single JSON line Waybar reads from stdout.</summary>
    ValueTask<string> ExposeAsync(CancellationToken cancellationToken);
}
